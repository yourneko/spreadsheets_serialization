using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RecursiveMapper
{
    // Operations with RecursiveMap, Type & FieldInfo classes
    static class RecursiveMapUtility
    {
        public static MappedAttribute GetMappedAttribute(this FieldInfo info) => info.GetCustomAttribute<MappedAttribute> ();
        public static MappedClassAttribute GetMappedAttribute(this Type type) => type.GetCustomAttribute<MappedClassAttribute> ();
        public static bool IsMapped(this object obj) => obj.GetType ().GetCustomAttribute<MappedClassAttribute> () != null;
        public static string GetSheetName(this Type type) => type.GetCustomAttribute<MappedClassAttribute> ()?.SheetName ?? string.Empty;

        public static void ListExistingSheetsRecursive(this RecursiveMap<bool> sheetsHierarchy, ICollection<string> results)
        {
            var maps = sheetsHierarchy.Collection.ToArray ();
            if (maps.Any(x => x.Value))
                results.Add($"'{sheetsHierarchy.Meta.FullName}'!{MapperService.FirstCellOfValueRange}:");
            foreach (var element in maps.Where(x => x.IsCollection))
                ListExistingSheetsRecursive (element, results);
        }

        public static bool HasRequiredSheets(this HashSet<string> sheets, RecursiveMap<bool> map) => map.IsValue && sheets.Contains (map.Meta.Sheet)
                                                                                           || map.IsCollection && map.Collection.Any ();

        public static RecursiveMap<string> JoinRecursive<T>(this IReadOnlyDictionary<string, IList<RecursiveMap<string>>> values,
            IEnumerable<RecursiveMap<T>> right, Meta meta)
        {
            var rightArray = right.ToArray ();
            RecursiveMap<string>[] result = new RecursiveMap<string>[rightArray.Length];
            var dictValues = rightArray.Any (map => map.IsValue)
                                 ? values[meta.FullName]  // If the current Map should have its own ValueRange, looking for it in the dictionary
                                 : null;

            // IsValue elements are replaced by Maps from the dictionary, keeping their order. IsCollection elements are going recursive
            int dictValuesIndex = -1;
            for (int i = 0; i < rightArray.Length; i++)
                result[i] = rightArray[i].IsValue
                                ? dictValues?[++dictValuesIndex]
                                : values.JoinRecursive (rightArray[i].Collection, rightArray[i].Meta);

            return new RecursiveMap<string> (result, meta);
        }

        public static RecursiveMap<bool> FillIndicesRecursive(this Predicate<RecursiveMap<bool>> condition, bool value, Meta meta)
        {
            return meta.IsSingleObject
                       ? new RecursiveMap<bool> (value, meta)
                       : meta.MakeMap (SpawnWhile (condition, i => new RecursiveMap<bool> (true, new Meta (meta, i))))
                             .Cast (condition.FillIndicesRecursive);
        }

        static IEnumerable<T> SpawnWhile<T>(Predicate<T> condition, Func<int, T> produce)
        {
            int index = 0;
            T result;
            while (condition.Invoke (result = produce (++index)))
                yield return result;
        }

        public static RecursiveMap<T> MakeMap<T>(this Meta meta, IEnumerable<RecursiveMap<T>> collection) => new RecursiveMap<T> (collection, meta);

        // Used with Reflection
        static IEnumerable<object> Unpack<T>(object collection) => collection is IEnumerable<T> enumerable
                                                                       ? enumerable.Cast<object> ()
                                                                       : throw new InvalidCastException();
    }
}