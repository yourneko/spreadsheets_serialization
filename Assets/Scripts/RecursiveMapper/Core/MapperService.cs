using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;

namespace RecursiveMapper
{
    public class MapperService : IDisposable
    {
        internal const string FirstCellOfValueRange = "A2";

        private readonly SheetsService service;
        private readonly Dictionary<int, IReadOnlyList<FieldInfo>> cachedFields = new Dictionary<int, IReadOnlyList<FieldInfo>> ();
        private readonly Dictionary<int, Type[]> cachedArrays = new Dictionary<int, Type[]> ();  // todo - make it thread-safe

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

            Predicate<RecursiveMap<bool>> condition = availableSheets.HasRequiredSheets;
            var hierarchy = MapTypeHierarchyRecursive (condition, true, new Meta (sheet, typeof(T)));

            //if (!hierarchy.All(match => match))               // todo - replace with better check
            //   return new Either<T, Exception> (new Exception ("Spreadsheet doesn't contain enough data to assemble the requested object."));

            var ranges = new List<string> ();
            hierarchy.ListExistingSheetsRecursive (ranges);
            var valueRanges = await service.GetValueRanges (spreadsheet, ranges.ToArray());

            var dictionaryValues = valueRanges.ToDictionary (range => range.Range.Split ('!')[0].Trim ('\''),
                                                             range => new ValueRangeReader (range).Read ());
            var map = dictionaryValues.JoinRecursive (hierarchy.Collection, hierarchy.Meta);
            return (T)UnmapObjectRecursive (typeof(T), map);
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

        // creating value ranges from the given object (writing)
        RecursiveMap<string> SerializeRecursive(object obj, Meta meta)
        {
            var type = obj.GetType ();
            return type.GetMappedAttribute () != null
                       ? meta.MakeMap (GetMappedFields (type).Select (field => new RecursiveMap<object> (field.GetValue (obj),
                                                                                                         meta.CreateChildMeta (GetArrayTypes (field)))))
                             .Cast (ExpandCollectionRecursive)
                             .Cast (SerializeRecursive)
                       : new RecursiveMap<string> (SerializationUtility.SerializeValue (obj), Meta.Point);
        }

        static RecursiveMap<object> ExpandCollectionRecursive(object obj, Meta meta)
        {
            return meta.IsObject
                       ? new RecursiveMap<object> (obj, meta)
                       : meta.MakeMap (AsCollection (obj, meta))
                             .Cast(ExpandCollectionRecursive);
        }

        static IEnumerable<RecursiveMap<object>> AsCollection(object obj, Meta parentMeta)
        {
            int index = -1;
            if (obj is ICollection collection) // type checking seems unnecessary
                foreach (var element in collection)
                    yield return new RecursiveMap<object>(element, new Meta(parentMeta, ++index));
        }

        static IEnumerable<ValueRange> ToValueRangesRecursive(RecursiveMap<string> data)
        {
            var ranges = data.Collection.Where (e => e.Meta.ContentType == ContentType.Sheet)
                             .SelectMany (ToValueRangesRecursive);
            if (data.Collection.Any (element => element.Meta.ContentType != ContentType.Sheet))
                ranges = ranges.Append (new ValueRangeBuilder (data).ToValueRange (FirstCellOfValueRange));
            return ranges;
        }

        // preparing a map of the requested type (reading)
        RecursiveMap<bool> MapTypeHierarchyRecursive(Predicate<RecursiveMap<bool>> condition, bool isCompactType, Meta meta)
        {
            return isCompactType
                       ? meta.MakeMap (GetMappedFields (meta.FrontType).Select (field => UnwrapFieldTypes (field, meta)))
                             .Cast (condition.FillIndicesRecursive)
                             .Cast ((v, m) => MapTypeHierarchyRecursive (condition, v, m))
                       : new RecursiveMap<bool> (false, meta);
        }

        RecursiveMap<bool> UnwrapFieldTypes(FieldInfo field, Meta parentMeta)
        {
            return new RecursiveMap<bool> (field.FieldType.GetMappedAttribute ()?.IsCompact ?? true,
                                           parentMeta.CreateChildMeta (GetArrayTypes (field)));
        }

        // making objects from a map of strings (reading)
        object UnmapObjectRecursive(Type type, RecursiveMap<string> ranges)
        {
            var result = Activator.CreateInstance(type);
            bool fieldsGo, mapsGo;

            using var ef = GetMappedFields (type).GetEnumerator ();
            using var em = ranges.Collection.GetEnumerator ();
            while ((fieldsGo = ef.MoveNext()) && (mapsGo = em.MoveNext()))
            {
                if (fieldsGo != mapsGo)
                    throw new Exception ("two lengths are not equal");

                var types = GetArrayTypes (ef.Current);
                ef.Current?.SetValue (result,
                                      em.Current != null && em.Current.IsValue
                                          ? (types.Length > 1)
                                                ? throw new Exception ("array is missing?")
                                                : (types[0].GetMappedAttribute () != null)
                                                    ? throw new Exception ("an attempt to deserialize a mapped type from a single string value")
                                                    : types[0].DeserializeValue (em.Current.Value)
                                          : UnmapRecursive (types, em.Current));
            }
            return result;
        }

        object UnmapRecursive(Type[] types, RecursiveMap<string> map) => types.Length == 1
                                                                             ? UnmapObjectRecursive (types[0], map)
                                                                             : map.Collection.Select (e => UnmapRecursive (types.Skip (1).ToArray (), e))
                                                                                  .CreateCollection (types);

        // caching
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

            var newArray = ReflectionUtility.GetEnumeratedTypes (field).ToArray();  // todo - maybe during the test check if they are related to each other
            cachedArrays.Add (hash, newArray);
            return newArray;
        }
    }
}