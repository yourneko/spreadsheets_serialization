using System;
using System.Collections.Generic;
using System.Linq;

namespace RecursiveMapper
{
    // Operations with RecursiveMap, Type & FieldInfo classes
    static class RecursiveMapUtility
    {

        public static RecursiveMap<bool> FillIndicesRecursive(this Predicate<RecursiveMap<bool>> condition, bool value, Meta meta)
        {
            return meta.IsObject
                       ? new RecursiveMap<bool> (value, meta)
                       : new RecursiveMap<bool> (Helpers.SpawnWhile (condition, i => new RecursiveMap<bool> (true, new Meta (meta, i))), meta)
                          .Cast (condition.FillIndicesRecursive);
        }

        public static RecursiveMap<object> ExpandCollection(this Type type, object obj, Meta meta)
        {
            return new RecursiveMap<object> (obj.AsCollection (type)
                                                .Select ((e, i) => new RecursiveMap<object> (e, new Meta (meta, i + 1))),
                                             meta);
        }

        static IEnumerable<object> AsCollection(this object obj, Type type) => (IEnumerable<object>)ReflectionUtility.ExpandMethodInfo.MakeGenericMethod (type)
                                                                                                                     .Invoke (null, new[] {obj});

        // used via Reflection
        static IEnumerable<object> ExpandCollection<T>(object value) => (value is ICollection<T> collection)      // todo - maybe just ICollection ??
                                                                            ? collection.Cast<object> ()
                                                                            : throw new InvalidCastException ();
    }
}