using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using RecursiveMapper.Utility;

namespace RecursiveMapper
{
    public class MapperService : IDisposable
    {
        const string FirstCellOfValueRange = "A2";

        private readonly SheetsService service;
        private readonly Dictionary<int, IReadOnlyList<FieldInfo>> cachedFields = new Dictionary<int, IReadOnlyList<FieldInfo>> ();
        private readonly Dictionary<int, Type[]> cachedArrays = new Dictionary<int, Type[]> ();

        /// <summary>
        /// Read the data from a spreadsheet and deserialize it to object of type T.
        /// </summary>
        /// <param name="spreadsheet">Spreadsheet ID.</param>
        /// <param name="sheet">Specify the name of the sheet, if the spreadsheet contains multiple sheets of a type.</param>
        /// <typeparam name="T">Type of object to read from the spreadsheet data.</typeparam>
        /// <returns>Object of type T.</returns>
        public async Task<Either<T, Exception>> ReadAsync<T>(string spreadsheet, string sheet = "")
            where T : new()
        {
            var availableSheets = await service.GetSheetsListAsync (spreadsheet);

            // its <bool> cuz i got no idea what to write there :c
            Predicate<RecursiveMap<bool>> condition = availableSheets.HasRequiredSheets;
            var hierarchy = MapTypeHierarchyRecursive (condition, true, new Meta (sheet, typeof(T)));

            //if (!hierarchy.All(match => match))               // todo - replace with better check
            //   return new Either<T, Exception> (new Exception ("Spreadsheet doesn't contain enough data to assemble the requested object."));

            var valueRanges = await service.GetValueRanges (spreadsheet, ListExistingSheetsRecursive (hierarchy).ToArray());
            var dictionaryValues = valueRanges.ToDictionary (range => range.Range.Split ('!')[0].Trim ('\''),
                                                             range => new ValueRangeReader (range).Read ());
            var result = (T)UnmapObjectRecursive (typeof(T), dictionaryValues.JoinRecursive(hierarchy.Right, hierarchy.Meta));
            return new Either<T, Exception>(result);
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
            var map = SerializeRecursive (obj, new Meta (sheet, typeof(T)));
            return service.WriteRangesAsync (spreadsheet,  ToValueRangesRecursive (map).ToList ());
        }

        /// <summary>
        /// Creates a new ready-to-use instance of the serializer.
        /// </summary>
        /// <param name="initializer"> An initializer for Google Sheets service. </param>
        public MapperService(BaseClientService.Initializer initializer)
        {
            // If you got an issue with missing System.IO.Compression lib, set GZipEnabled = FALSE in your initializer.
            service = new SheetsService (initializer
                                      ?? throw new ArgumentException ("SheetsService can't be null"));
        }

        public void Dispose()
        {
            service.Dispose ();
        }

        // caching mapped fields in classes stay the same
        IEnumerable<FieldInfo> GetMappedFields(Type type)
        {
            var hash = type.GetHashCode();
            if (cachedFields.TryGetValue (hash, out var result))
                return result;

            var fields = type.GetFields ()
                             .Where (info => info.GetMappedAttribute () != null)
                             .OrderBy (x => x.GetMappedAttribute ().Position)
                             .ToList ();
            cachedFields.Add (hash, fields);
            return fields;
        }

        Type[] GetArrayTypes(FieldInfo field)
        {
            int hash = field.GetHashCode ();
            if (cachedArrays.TryGetValue (hash, out var result))
                return result;

            var newArray = ReflectionUtility.GetEnumeratedTypes (field).ToArray();
            cachedArrays.Add (hash, newArray);
            return newArray;
        }

        RecursiveMap<string> SerializeRecursive(object obj, Meta meta)
        {
            var type = obj.GetType ();
            return type.GetMappedAttribute () != null
                       ? new RecursiveMap<object> (GetMappedFields (type).Select (field => MapValueInField (obj, field, meta)), meta)
                          .Cast (SerializeRecursive)
                       : new RecursiveMap<string> (Helpers.SerializeValue (obj), Meta.Point);
        }

        RecursiveMap<bool> MapTypeHierarchyRecursive(Predicate<RecursiveMap<bool>> condition, bool isCompactType, Meta meta)
        {
            return isCompactType
                       ? new RecursiveMap<bool> (GetMappedFields (meta.FrontType).Select (field => UnwrapFieldTypes (field, meta)), meta)
                             .Cast (condition.FillIndicesRecursive)
                             .Cast ((v, m) => MapTypeHierarchyRecursive(condition, v, m))
                       : new RecursiveMap<bool> (false, meta);
        }

        RecursiveMap<bool> UnwrapFieldTypes(FieldInfo field, Meta parentMeta) =>
            new RecursiveMap<bool> (field.FieldType.GetMappedAttribute ()?.IsCompact ?? true,
                                    parentMeta.CreateChildMeta (GetArrayTypes (field)));

        RecursiveMap<object> MapValueInField(object obj, FieldInfo field, Meta parentMeta)
        {
            return parentMeta.Types.Aggregate (new RecursiveMap<object> (field.GetValue (obj),
                                                                         parentMeta.CreateChildMeta (GetArrayTypes (field))),
                                               (map, type) => map.Cast (type.ExpandCollection));
        }

        static IEnumerable<ValueRange> ToValueRangesRecursive(RecursiveMap<string> data)
        {
            var ranges = data.Right.Where (e => e.Meta.ContentType == ContentType.Sheet)
                             .SelectMany (ToValueRangesRecursive);
            if (data.Right.Any (element => element.Meta.ContentType != ContentType.Sheet))
                ranges = ranges.Append (new ValueRangeBuilder (data).ToValueRange (FirstCellOfValueRange));
            return ranges;
        }

        static IEnumerable<string> ListExistingSheetsRecursive(RecursiveMap<bool> sheetsHierarchy)
        {
            bool shouldReturn = false;
            foreach (var element in sheetsHierarchy.Right)
            {
                if (element.IsLeft)
                    shouldReturn = true;
                else
                    foreach (var sheet in ListExistingSheetsRecursive (element))
                        yield return sheet;
            }

            if (shouldReturn)
                yield return sheetsHierarchy.Meta.Sheet;
        }

        object UnmapObjectRecursive(Type type, RecursiveMap<string> ranges)
        {
            var result = Activator.CreateInstance(type);
            bool fieldsGo, mapsGo;

            using var ef = GetMappedFields (type).GetEnumerator ();
            using var em = ranges.Right.GetEnumerator ();
            while ((fieldsGo = ef.MoveNext()) && (mapsGo = em.MoveNext()))
            {
                if (fieldsGo != mapsGo)
                    throw new Exception ("two lengths are not equal");

                var types = GetArrayTypes (ef.Current);
                ef.Current?.SetValue (result,
                                      em.Current != null && em.Current.IsLeft
                                          ? (types.Length > 1)
                                                ? throw new Exception ("array is missing?")
                                                : (types[0].GetMappedAttribute () != null)
                                                    ? throw new Exception ("an attempt to deserialize a mapped type from a single string value")
                                                    : types[0].DeserializeValue (em.Current.Left)
                                          : UnmapCollectionRecursive (types, em.Current));
            }
            return result;
        }

        object UnmapCollectionRecursive(Type[] types, RecursiveMap<string> map)
        {
            return types.Length == 1
                       ? UnmapObjectRecursive (types[0], map)
                       : ReflectionUtility.CreateArray (types[0],
                                                        map.Right.Select (element => UnmapCollectionRecursive (types.Skip (1).ToArray (), element)),
                                                        types[1]);
        }
    }
}