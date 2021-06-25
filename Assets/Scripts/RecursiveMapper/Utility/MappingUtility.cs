using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RecursiveMapper.Utility
{
    // Operations with RecursiveMap, Type & FieldInfo classes
    static class MappingUtility
    {
        static readonly MethodInfo ExpandMethodInfo = typeof(MappingUtility).GetMethod ("ExpandCollection");

        static RecursiveMap<T> MapTypeFields<T>(this Type type, Meta meta, Func<FieldInfo, RecursiveMap<T>> valueFunc)
        {
            return new RecursiveMap<T> (type.GetFields ()
                                            .Where (info => info.GetMappedAttribute () != null)
                                            .OrderBy (x => x.GetMappedAttribute ().Position)        // todo - sort moves to reflection util
                                            .Select (valueFunc.Invoke),
                                        meta);
        }

#region Mapping objects

        public static RecursiveMap<string> SerializeRecursive(object obj, Meta meta)
        {
            var type = obj.GetType ();
            return type.IsMapped ()
                       ? type.MapTypeFields(meta, field => MapFieldValue(field, obj, meta))
                             .Cast (SerializeRecursive)
                       : new RecursiveMap<string> (Helpers.SerializeValue (obj), Meta.Point);
        }

        static RecursiveMap<object> MapFieldValue(FieldInfo field, object obj, Meta parentMeta)   // feels far from perfect
        {
            var map = new RecursiveMap<object> (field.GetValue (obj), parentMeta.CreateChildMeta (ReflectionUtility.GetEnumeratedTypes (field).ToArray ()));

            for (int i = 1; i < map.Meta.Types.Count; i++)
                map = map.Cast (map.Meta.Types[i].ExpandCollection);
            return map;
        }

        static RecursiveMap<object> ExpandCollection(this Type type, object obj, Meta meta)
        {
            var elements = (IEnumerable<object>)ExpandMethodInfo.MakeGenericMethod (type)
                                                                .Invoke (null, new[] {obj});
            var map = elements.Select ((e, i) => new RecursiveMap<object> (e, new Meta(meta, i + 1)));
            return new RecursiveMap<object> (map, meta);
        }

#endregion

        public static RecursiveMap<bool> MapTypeHierarchyRecursive(this Predicate<RecursiveMap<bool>> condition, bool isCompactType, Meta meta)
        {
            return isCompactType
                       ? meta.FrontType.MapTypeFields (meta, field => UnwrapFieldTypes (field, meta))
                             .Cast (condition.FillIndicesRecursive)
                             .Cast (condition.MapTypeHierarchyRecursive)
                       : new RecursiveMap<bool> (false, meta);
        }

        static RecursiveMap<bool> UnwrapFieldTypes(FieldInfo field, Meta parentMeta) =>
            new RecursiveMap<bool> (field.FieldType.IsCompact(),
                                    parentMeta.CreateChildMeta (ReflectionUtility.GetEnumeratedTypes (field).ToArray ()));

        static RecursiveMap<bool> FillIndicesRecursive(this Predicate<RecursiveMap<bool>> condition, bool value, Meta meta)
        {
            return meta.IsObject
                       ? new RecursiveMap<bool> (value, meta)
                       : new RecursiveMap<bool> (Helpers.SpawnWhile (condition, i => new RecursiveMap<bool> (true, new Meta (meta, i))), meta)
                          .Cast (condition.FillIndicesRecursive);
        }

        // used via Reflection
        static IEnumerable<object> ExpandCollection<T>(object value) => (value is IEnumerable<T> collection)
                                                                            ? collection.Cast<object> ()
                                                                            : throw new InvalidCastException ();
    }
}