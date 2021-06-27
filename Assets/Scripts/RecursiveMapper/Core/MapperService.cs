using System;
using System.Collections;
using System.Collections.Concurrent;
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
        readonly MethodInfo addMethodInfo = typeof(ICollection<>).GetMethod ("Add", BindingFlags.Instance | BindingFlags.Public);

        private readonly SheetsService service;
        private readonly ConcurrentDictionary<int, IReadOnlyList<FieldInfo>> cachedFields = new ConcurrentDictionary<int, IReadOnlyList<FieldInfo>> ();
        private readonly ConcurrentDictionary<int, Type[]> cachedArrays = new ConcurrentDictionary<int, Type[]> ();
        private readonly Action<string> logAction;

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
            Log ("sheets available:");
            Log (string.Join (Environment.NewLine, availableSheets));
            var hs = new HashSet<string> (availableSheets);
            var hierarchy = MapTypeHierarchyRecursive (hs.HasRequiredSheets, true, CreateInitialMeta<T>(sheet));
            var ranges = new List<string> ();
            hierarchy.ListExistingSheetsRecursive (ranges);

            if (!new HashSet<string> (ranges).IsSubsetOf (hs)) throw new Exception();
            Log ("ranges selected:");
            Log (string.Join (Environment.NewLine, ranges));

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
            var values = ToValueRangesRecursive (SerializeRecursive (obj, CreateInitialMeta<T>(sheet))).ToList ();
            Log ("ranges created:");
            Log (string.Join (Environment.NewLine, values.Select (x => x.Range)));
            return service.WriteRangesAsync (spreadsheet, values);
        }

        /// <summary>
        /// Creates a new ready-to-use instance of the serializer.
        /// </summary>
        /// <param name="initializer"> An initializer for Google Sheets service. </param>
        public MapperService(BaseClientService.Initializer initializer, Action<string> log = null)
        {
            // If you got an issue with missing System.IO.Compression lib, set GZipEnabled = FALSE in your initializer.
            service = new SheetsService (initializer
                                      ?? throw new ArgumentException ("SheetsService can't be null"));
            logAction = log;
            Log ("mapper started");
        }

        public void Dispose()
        {
            service.Dispose ();
        }

        void Log(string msg) => logAction?.Invoke (msg);

        static Meta CreateInitialMeta<T>(string input) => new Meta (input.Contains ("{0}")
                                                                        ? string.Format (input, typeof(T).GetSheetName ())
                                                                        : input + typeof(T).GetSheetName (), typeof(T));

        // creating value ranges from the given object (writing)
        RecursiveMap<string> SerializeRecursive(object obj, Meta meta)
        {
            var type = obj.GetType ();
            return type.GetMappedAttribute () != null
                       ? meta.MakeMap (GetFields (type).Select (field => new RecursiveMap<object> (field.GetValue (obj),
                                                                                                         meta.CreateChildMeta (GetArrayTypes (field)))))
                             .Cast (ExpandCollectionRecursive)
                             .Cast (SerializeRecursive)
                       : new RecursiveMap<string> (SerializationUtility.SerializeValue (obj), Meta.Point);
        }

        static RecursiveMap<object> ExpandCollectionRecursive(object obj, Meta meta)
        {
            return !meta.IsObject &&obj is ICollection collection
                       ? meta.MakeMap (collection.Cast<object> ().Select ((element, index) => new RecursiveMap<object> (element, new Meta (meta, index))))
                             .Cast(ExpandCollectionRecursive)
                       : new RecursiveMap<object> (obj, meta);
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
                       ? new RecursiveMap<bool> (true, meta)
                       : meta.MakeMap (GetFields (meta.FrontType).Select (f => new RecursiveMap<bool> (f.FieldType.GetMappedAttribute ()?.IsCompact ?? true,
                                                                                                       meta.CreateChildMeta (GetArrayTypes (f)))))
                             .Cast (condition.FillIndicesRecursive)
                             .Cast ((v, m) => MapTypeHierarchyRecursive (condition, v, m));
        }

        // making objects from a map of strings (reading)
        object UnmapObjectRecursive(Type type, RecursiveMap<string> ranges)
        {
            var result = Activator.CreateInstance(type);
            bool fieldsGo, mapsGo;

            using var ef = GetFields (type).GetEnumerator ();
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
                                          : UnmapFieldRecursive (types, em.Current));
            }
            return result;
        }

        object UnmapFieldRecursive(IReadOnlyList<Type> types, RecursiveMap<string> map)
        {
            if (types.Count == 1)
                return UnmapObjectRecursive (types[0], map);

            var elements = map.Collection.Select (e => UnmapFieldRecursive (types.Skip (1).ToArray (), e));
            var result = Activator.CreateInstance (types[0]);
            var addMethod = addMethodInfo.MakeGenericMethod (types[1]);
            foreach (var element in elements)
                addMethod.Invoke (result, new[] {element});
            return result;
        }

        // caching
        IEnumerable<FieldInfo> GetFields(Type type)
        {
            var hash = type.GetHashCode();
            if (cachedFields.TryGetValue (hash, out var result))
                return result;

            var fields = type.GetFields (BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance)
                             .Where (info => info.GetMappedAttribute () != null)
                             .OrderBy (x => x.GetMappedAttribute ().Position)
                             .ToList ();
            cachedFields.TryAdd (hash, fields);
            Log ($"mapping fields of class {type.Name}");
            Log (string.Join (Environment.NewLine, fields.Select (f => $"{f.Name} : type {f.FieldType.Name}")));
            return fields;
        }

        Type[] GetArrayTypes(FieldInfo field)
        {
            int hash = field.GetHashCode ();
            if (cachedArrays.TryGetValue (hash, out var result))
                return result;

            var array = new Type[field.GetMappedAttribute ().DimensionCount];
            array[0] = field.FieldType;
            for (int i = 1; i < array.Length; i++)                        // todo - look specifically for IEnumerable<>
                array[i] = array[i - 1].GetTypeInfo ().GetInterfaces ().FirstOrDefault (x => x.IsGenericType)?.GetGenericArguments ()[0];

            cachedArrays.TryAdd (hash, array);
            Log (string.Join (" > ", array.Select (f => f.Name)));
            return array;
        }
    }
}