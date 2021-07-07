using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RecursiveMapper
{
    static class ExtensionMethods
    {
        private static readonly MethodInfo AddMethodInfo = typeof(ICollection<>).GetMethod ("Add", BindingFlags.Instance | BindingFlags.Public);

        public static MapFieldAttribute MapAttribute(this FieldInfo field)
        {
            var attribute = (MapFieldAttribute)Attribute.GetCustomAttribute (field, typeof(MapFieldAttribute));
            if (attribute != null && !attribute.Initialized)
                attribute.CacheMeta (field);
            return attribute;
        }

        public static MapClassAttribute MapAttribute(this Type type)
        {
            var attribute = (MapClassAttribute)Attribute.GetCustomAttribute (type, typeof(MapClassAttribute));
            if (!attribute.Initialized)
                attribute.CacheMeta (type);
            return attribute;
        }

        public static Type GetEnumeratedType(this Type t) => t.GetTypeInfo ().GetInterfaces ()
                                                              .FirstOrDefault (x => x.IsGenericType && x.GetGenericTypeDefinition () == typeof(IEnumerable<>))
                                                             ?.GetGenericArguments ()[0];

        public static string JoinSheetNames(this string parent, string child) => child.Contains ("{0}") ? string.Format (child, parent) : parent + child;

        public static Chunked<T> ToChunks<T>(this IEnumerable<T> source, int chunkSize) where T : class => new Chunked<T> (source, chunkSize);

        public static object AddChild(this MapFieldAttribute field, object parent, int rank, object child = null)
        {
            var childToAdd = child ?? Activator.CreateInstance (field.ArrayTypes[rank]);
            if (rank == 0)
                field.Field.SetValue (parent, childToAdd);
            else
                AddMethodInfo.MakeGenericMethod (field.ArrayTypes[rank]).Invoke (parent, new[] {childToAdd});
            return childToAdd;
        }

    }
}