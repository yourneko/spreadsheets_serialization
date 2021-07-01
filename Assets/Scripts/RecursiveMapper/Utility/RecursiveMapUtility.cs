using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RecursiveMapper
{
    // Operations with RecursiveMap, Type & FieldInfo classes
    static class RecursiveMapUtility
    {
        public static RecursiveMap<T> Map<T>(this Meta meta, T value) => new RecursiveMap<T> (value, meta);
        public static RecursiveMap<T> Map<T>(this Meta meta, IEnumerable<RecursiveMap<T>> collection) => new RecursiveMap<T> (collection, meta);

        public static MappedAttribute GetMappedAttribute(this FieldInfo info) => info.GetCustomAttribute<MappedAttribute> ();
        public static MappedClassAttribute GetMappedAttribute(this Type type) => type.GetCustomAttribute<MappedClassAttribute> ();

        public static string GetSheetName(this Type type) => type.GetMappedAttribute ()?.SheetName ?? string.Empty;

        public static RecursiveMap<T> MapFieldsOfClass<T>(this Meta meta, Func<FieldInfo, T> eval)
        {
            return meta.Map (meta.FrontType.FieldsMap ().Select (f => new RecursiveMap<T> (eval.Invoke (f), meta.CreateChildMeta (f))));
        }

        // todo - modify it and its usages so it actually do something
        public static bool HasRequiredSheets(this HashSet<string> sheets, RecursiveMap<string> map) => map.IsValue && sheets.Contains (map.Meta.Sheet)
                                                                                                    || !map.IsValue && map.Collection.Any ();

        public static RecursiveMap<string> FillIndicesRecursive(this Predicate<RecursiveMap<string>> condition, string _, Meta meta)
        {
            return meta.ContentType == ContentType.Sheet
                       ? meta.Map (RecursiveMapUtility.SpawnWhile (condition, i => new Meta (meta, i).Map (string.Empty)))
                             .Cast (condition.FillIndicesRecursive)
                       : meta.Map (string.Empty);
        }

        public static IReadOnlyList<Type> GetArrayTypes(this FieldInfo field)
        {
            var attribute = field.GetMappedAttribute ();
            if (attribute.ArrayTypes is { })
                return attribute.ArrayTypes;

            var types = new List<Type> {field.FieldType};
            for (int i = 0; i < attribute.DimensionCount; i++)
                types.Add (types[i].GetTypeInfo ().GetInterfaces ()
                                   .FirstOrDefault (t => t.IsGenericType && t.GetGenericTypeDefinition () == typeof(IEnumerable<>))
                                  ?.GetGenericArguments ()[0]);
            return (attribute.ArrayTypes = types);
        }

        public static IList<FieldInfo> FieldsMap(this Type t) => t.GetMappedAttribute ().Fields ??=
                                                                     t.GetFields (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                                                                      .Where (info => info.GetMappedAttribute () != null)
                                                                      .OrderBy (x => x.GetMappedAttribute ().Position)
                                                                      .ToList ();

        static IEnumerable<T> SpawnWhile<T>(Predicate<T> condition, Func<int, T> produce)
        {
            int index = 0; // has to start from 1
            T result;
            while (condition.Invoke (result = produce (++index)))
                yield return result;
        }
    }
}