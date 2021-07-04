using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace RecursiveMapper
{
    static class ExtensionMethods
    {
        static readonly MethodInfo AddMethodInfo = typeof(ICollection<>).GetMethod ("Add", BindingFlags.Instance | BindingFlags.Public);

        public static MappedAttribute MapAttribute(this FieldInfo field)
        {
            var attribute = (MappedAttribute)Attribute.GetCustomAttribute (field, typeof(MappedAttribute));
            if (attribute != null && !attribute.Initialized)
                attribute.CacheMeta (field);
            return attribute;
        }

        public static MappedClassAttribute MapAttribute(this Type type)
        {
            var attribute = (MappedClassAttribute)Attribute.GetCustomAttribute (type, typeof(MappedClassAttribute));
            if (!attribute.Initialized)
                attribute.CacheMeta (type);
            return attribute;
        }

        public static IEnumerable<(string name, object  obj)> ToCollection(this object o, string name, int repeats)
        {
            return repeats == 0
                       ? new[]{(name,  o)}
                       : o is ICollection c
                              ? repeats > 1
                                    ? c.Cast<object> ().SelectMany ((e, i) => ToCollection (e, $"{name} {i}", repeats - 1))
                                    : c.Cast<object> ().Select ((e, i) => ($"{name} {i}", e))
                              : throw new Exception ();
        }

        public static string JoinSheetNames(this string parent, string child) => child.Contains ("{0}") ? string.Format (child, parent) : parent + child;

        public static Action<object> AddContent(this object parent, Type type)
        {
            var addMethod = AddMethodInfo.MakeGenericMethod (type);
            return element => addMethod.Invoke (parent, new[] {element});
        }

        public static void FindValidArrayIndices(this Predicate<int[]> validate, int count)   // IMPORTANT: indices start from 1, not 0
        {
            var indices = Enumerable.Repeat (1, count + 1).ToArray (); // 0 element is a pointer to current value.
            while (indices[0] >= 0)
            {
                if (validate (indices.Skip(1).ToArray()))
                    indices[0] = indices.Length - 1;
                else
                    indices[indices[0]--] = 1;
                indices[indices[0]] += 1;
            }
        }
    }
}