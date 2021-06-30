using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RecursiveMapper
{
    // Operations with RecursiveMap, Type & FieldInfo classes
    static class RecursiveMapUtility
    {
        public static RecursiveMap<T> Map<T>(this Meta meta, IEnumerable<RecursiveMap<T>> collection) => new RecursiveMap<T> (collection, meta);
        public static RecursiveMap<T> Map<T>(this Meta meta, T value) => new RecursiveMap<T> (value, meta);

        public static MappedAttribute GetMappedAttribute(this FieldInfo info) => info.GetCustomAttribute<MappedAttribute> ();
        public static MappedClassAttribute GetMappedAttribute(this Type type) => type.GetCustomAttribute<MappedClassAttribute> ();

        public static string GetSheetName(this Type type) => type.GetMappedAttribute ()?.SheetName ?? string.Empty;

        public static RecursiveMap<T> MapTypeFields<T>(this Meta meta, Func<FieldInfo, T> eval)
        {
            return meta.Map (meta.FrontType.FieldsMap ().Select (f => Map (meta.CreateChildMeta (f.GetMappedAttribute ()), eval.Invoke (f))));
        }

        private static void Separate<T>(this IEnumerable<T> collection, Predicate<T> condition, out IList<T> onTrue, out IList<T> onFalse)
        {
            onTrue = new List<T> ();
            onFalse = new List<T> ();
            foreach (var element in collection)
                (condition.Invoke (element) ? onTrue : onFalse).Add (element);
        }


        public static bool HasRequiredSheets(this HashSet<string> sheets, RecursiveMap<string> map) => map.IsValue && sheets.Contains (map.Meta.Sheet)
                                                                                           || !map.IsValue && map.Collection.Any ();  // todo - so bad

        // Map was already built. This method just connects Map<string> from different ValueRange. DONT TRY to adapt it for CAST - it doesn't fit
        public static RecursiveMap<string> JoinRecursive(this Dictionary<string, IList<RecursiveMap<string>>> values, RecursiveMap<string> map)
        {
            var elements = map.Collection.ToList ();
            var compactElementsSheet = elements.FirstOrDefault (x => x.IsValue);
            if (compactElementsSheet is null)
                return map.Meta.Map (elements.Select (values.JoinRecursive));

            elements.Remove (compactElementsSheet);
            var compactElements = values[compactElementsSheet.Value];
            int sheetIndex = -1;
            int compactIndex = -1;
            return map.Meta.Map (map.Meta.FrontType.FieldsMap ().Select (f => !f.GetArrayTypes().Last().GetMappedAttribute().IsCompact
                                                                                        ? values.JoinRecursive (elements[++sheetIndex])
                                                                                        : compactElements[++compactIndex]));
        }

        public static RecursiveMap<string> KeepSheetsOnly(this RecursiveMap<string> target, List<string> list)
        {
            List<RecursiveMap<string>> resultCollection = new List<RecursiveMap<string>> ();
            target.Collection.Separate (e => e.Meta.ContentType == ContentType.Sheet, out var onTrue, out var onFalse);
            if (onFalse.Count > 0)
            {
                var name = target.Meta.FullName;
                list.Add (name);
                resultCollection.Add (new RecursiveMap<string> (name, Meta.Point));
            }

            resultCollection.AddRange (onTrue.Select (e => KeepSheetsOnly(e, list)));
            return new RecursiveMap<string> (resultCollection, target.Meta);
        }

        public static RecursiveMap<string> FillIndicesRecursive(this Predicate<RecursiveMap<string>> condition, string _, Meta meta)
        {
            return meta.ContentType == ContentType.Sheet
                       ? meta.Map (SpawnWhile (condition, i => new Meta (meta, i).Map (string.Empty)))
                             .Cast (condition.FillIndicesRecursive)
                       : meta.Map (string.Empty);
        }

        static IEnumerable<T> SpawnWhile<T>(Predicate<T> condition, Func<int, T> produce)
        {
            int index = 0;
            T result;
            while (condition.Invoke (result = produce (++index)))
                yield return result;
        }

        // Used with Reflection           todo :  why not an IEnumerable?
        static IEnumerable<object> Unpack<T>(object collection) => collection is IEnumerable<T> enumerable
                                                                       ? enumerable.Cast<object> ()
                                                                       : throw new InvalidCastException();

        public static IReadOnlyList<Type> GetArrayTypes(this FieldInfo field)
        {
            var attribute = field.GetMappedAttribute ();
            if (attribute.ArrayTypes is null)
            {
                var types = new List<Type> {field.FieldType};
                for (int i = 0; i < attribute.DimensionCount; i++) // todo - look specifically for IEnumerable<>
                    types.Add (types.Last ().GetTypeInfo ().GetInterfaces ().FirstOrDefault (x => x.IsGenericType)?.GetGenericArguments ()[0]);
                attribute.ArrayTypes = types;
            }
            return attribute.ArrayTypes;
        }

        public static IList<FieldInfo> FieldsMap(this Type t) => t.GetMappedAttribute ().Fields ??=
                                                                     t.GetFields (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                                                                      .Where (info => info.GetMappedAttribute () != null)
                                                                      .OrderBy (x => x.GetMappedAttribute ().Position)
                                                                      .ToList ();
    }
}