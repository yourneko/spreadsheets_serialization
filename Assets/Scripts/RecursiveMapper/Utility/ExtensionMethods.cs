using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SpreadsheetsMapper
{
    static class ExtensionMethods
    {
        static readonly MethodInfo addMethodInfo = typeof(ICollection<>).GetMethod ("Add", BindingFlags.Instance | BindingFlags.Public);

        public static MapFieldAttribute MapAttribute(this FieldInfo field)
        {
            var attribute = (MapFieldAttribute)Attribute.GetCustomAttribute (field, typeof(MapFieldAttribute));
            if (!attribute?.Initialized ?? false)
                attribute.CacheMeta (field);
            return attribute;
        }

        public static MapClassAttribute MapAttribute(this Type type)
        {
            var attribute = (MapClassAttribute)Attribute.GetCustomAttribute (type, typeof(MapClassAttribute));
            if (!attribute?.Initialized ?? false)
                attribute.CacheMeta (type);
            return attribute;
        }

        public static IEnumerable<Type> GetArrayTypes(this Type fieldType, int max)
        {
            yield return fieldType;
            int rank = 0;
            var t = fieldType;
            while (++rank <= max && t != typeof(string)     // never go for char[]
                                 && (t = t.GetTypeInfo().GetInterfaces()
                                          .FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                                         ?.GetGenericArguments()[0]) != null)
                yield return t;
        }

        public static object AddChild(this MapFieldAttribute field, object parent, int rank, object child = null)
        {
            var childToAdd = child ?? Activator.CreateInstance (field.ArrayTypes[rank]);
            if (rank == 0)
                field.Field.SetValue (parent, childToAdd);
            else
                addMethodInfo.MakeGenericMethod (field.ArrayTypes[rank]).Invoke (parent, new[] {childToAdd});
            return childToAdd;
        }

        public static string GetReadRange(this MapClassAttribute type, string sheet, string a2First) =>
            $"'{sheet}'!{a2First}:{SpreadsheetsUtility.WriteA1 (type.Size.Add (SpreadsheetsUtility.ReadA1 (a2First)))}";
    }
}
