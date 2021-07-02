using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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

        public static void AddContent(this object parent, Type type, IEnumerable<object> children)
        {
            var addMethod = AddMethodInfo.MakeGenericMethod (type);
            foreach (var element in children)
                addMethod.Invoke (parent, new[] {element});
        }

        public static Dictionary<string, object> ToCollection(this object o, string name, int repeats)  // todo - use wider
        {
            return repeats == 0
                       ? new Dictionary<string, object>{{name,  o}}
                       : (o is ICollection c
                              ? repeats > 1
                                    ? c.Cast<object> ().SelectMany ((e, i) => ToCollection (e, $"{name} {i}", repeats - 1))
                                    : c.Cast<object> ().Select ((e, i) => new KeyValuePair<string, object> ($"{name} {i}", e))
                              : throw new Exception ())
                      .ToDictionary (pair => pair.Key, pair => pair.Value);
        }

        public static string JoinSheetNames(this string parent, string child) => child.Contains ("{0}")
                                                                                     ? string.Format (child, parent)
                                                                                     : parent + child;
    }
}