using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using UnityEngine;

namespace RecursiveMapper
{
    public class MapperService : IDisposable
    {
        public const string FirstCellOfValueRange = "A2";

        readonly MethodInfo addMethodInfo = typeof(ICollection<>).GetMethod ("Add", BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);
        readonly MethodInfo unpackMethod = typeof(RecursiveMapUtility).GetMethod ("Unpack", BindingFlags.Static | BindingFlags.NonPublic);

        private readonly SheetsService service;
        private readonly IValueSerializer serializer;

        /// <summary>
        /// Read the data from a spreadsheet and deserialize it to object of type T.
        /// </summary>
        /// <param name="spreadsheet">Spreadsheet ID.</param>
        /// <param name="sheet">Specify the name of the sheet, if the spreadsheet contains multiple sheets of a type.</param>
        /// <typeparam name="T">Type of object to read from the spreadsheet data.</typeparam>
        /// <returns>Object of type T.</returns>
        public async Task<T> ReadAsync<T>(string spreadsheet, string sheet = "")
            where T : new()
        {
            var availableSheets = await service.GetSheetsListAsync (spreadsheet);
            var availableSheetHashes = new HashSet<string> (availableSheets);
            List<string> ranges = new List<string>();
            var hierarchy = MapSheetsRecursive (availableSheetHashes.HasRequiredSheets, CreateInitialMeta<T>(sheet), ranges);

            Debug.LogError ($"sheets exist: {string.Join (Environment.NewLine, availableSheets)}");
            Debug.LogError ($"sheets required: {string.Join (Environment.NewLine, ranges)}");

            if (!availableSheetHashes.IsSupersetOf(ranges))
                return default(T);

            var valueRanges = await service.GetValueRanges (spreadsheet, ranges.ToArray());
            Debug.Log ($"Received {valueRanges.Count} ranges in response.");
            Debug.Log (string.Join (Environment.NewLine, valueRanges.Select (r => $"range > {r.Range}, dimension > {r.MajorDimension}")));
            var dictionaryValues = valueRanges.ToDictionary (range => range.Range.Split ('!')[0].Trim ('\''),
                                                             range => new ValueRangeReader (range).Read ());
            return (T)UnmapObjectRecursive (typeof(T), dictionaryValues.JoinRecursive (hierarchy));
        }

        /// <summary>
        /// Write a serialized object of type T to the spreadsheet.
        /// </summary>
        /// <param name="obj">Object to serialize.</param>
        /// <param name="spreadsheet">Spreadsheet ID.</param>
        /// <param name="sheet">Specify the name of the sheet, if the spreadsheet contains multiple sheets of a type.</param>
        /// <typeparam name="T">Type of serialized object.</typeparam>
        public Task<bool> WriteAsync<T>(T obj, string spreadsheet, string sheet = "")
        {
            var valueRangeList = new List<ValueRange> ();
            ListRangesRecursive (SerializeRecursive (obj, CreateInitialMeta<T> (sheet)), valueRangeList);
            return service.WriteRangesAsync (spreadsheet, valueRangeList);
        }

        /// <summary>
        /// Creates a new ready-to-use instance of the serializer.
        /// </summary>
        /// <param name="initializer"> An initializer for Google Sheets service. </param>
        /// <param name="valueSerializer"> Replaces the default serialization strategy. </param>
        public MapperService(BaseClientService.Initializer initializer, IValueSerializer valueSerializer = null)
        {
            // If you got an issue with missing System.IO.Compression lib, set GZipEnabled = FALSE in your initializer.
            service = new SheetsService (initializer
                                      ?? throw new ArgumentException ("SheetsService can't be null"));
            serializer = valueSerializer ?? new DefaultValueSerializer ();
        }

        public void Dispose()
        {
            service.Dispose ();
        }

        static Meta CreateInitialMeta<T>(string input) => new Meta (input.JoinSheetNames(typeof(T).GetSheetName()), new[]{typeof(T)});

        RecursiveMap<object> UnpackCollection(object collection, Meta meta)
        {
            var elements = (IEnumerable<object>)unpackMethod.MakeGenericMethod (meta.UnpackType).Invoke (null, new[] {collection});
            return new RecursiveMap<object> (elements.Select ((e, i) => new Meta (meta, i + 1).Map (e)), meta);
        }

        RecursiveMap<string> SerializeRecursive(object obj, Meta meta) => meta.IsSingleObject
                                                                              ? meta.ContentType == ContentType.Value
                                                                                    ? meta.Map (serializer.Serialize (obj))
                                                                                    : meta.MapTypeFields (f => f.GetValue (obj)).Cast (SerializeRecursive)
                                                                              : UnpackCollection (obj, meta).Cast (SerializeRecursive);

        static RecursiveMap<string> MapSheetsRecursive(Predicate<RecursiveMap<string>> func, Meta meta, List<string> sheets)
        {
            return meta.MapTypeFields (_ => string.Empty)
                       .KeepSheetsOnly(sheets)
                       .Cast (func.FillIndicesRecursive)
                       .Cast ((_, m) => MapSheetsRecursive (func, m, sheets));
        }

        static void ListRangesRecursive(RecursiveMap<string> data, List<ValueRange> list)
        {
            foreach (var map in data.Collection.Where (e => e.Meta.ContentType == ContentType.Sheet))
                ListRangesRecursive (map, list);
            if (data.Collection.Any (element => element.Meta.ContentType != ContentType.Sheet))
                list.Add (new ValueRangeBuilder (FirstCellOfValueRange).ToValueRange (data));
        }

        object UnmapObjectRecursive(Type type, RecursiveMap<string> ranges)
        {
            var result = Activator.CreateInstance(type);
            using var maps = ranges.Collection.GetEnumerator (); // todo - here we can find out that number of mapped fields changed
            foreach (var field in  type.FieldsMap())
                field.SetValue (result,
                                maps.MoveNext () && maps.Current != null
                                    ? maps.Current.IsValue
                                          ? serializer.Deserialize (field.FieldType, maps.Current.Value)
                                          : field.GetMappedAttribute ().DimensionCount == 0
                                              ? UnmapObjectRecursive (field.FieldType, maps.Current)
                                              : UnmapCollectionRecursive (field.GetArrayTypes(), maps.Current)
                                    : throw new Exception ("sadly, the map is gone"));
            return result;
        }

        object UnmapCollectionRecursive(IReadOnlyList<Type> types, RecursiveMap<string> map)
        {
            var result = Activator.CreateInstance (types[0]);
            var addMethod = addMethodInfo.MakeGenericMethod (types[1]);
            var elements = types.Count > 2
                               ? map.Collection.Select (element => UnmapCollectionRecursive (types.Skip (1).ToArray (), element))
                               : map.Collection.Select (element => UnmapObjectRecursive (types[1], element));
            foreach (var element in elements)
                addMethod.Invoke (result, new[] {element});
            return result;
        }
    }
}