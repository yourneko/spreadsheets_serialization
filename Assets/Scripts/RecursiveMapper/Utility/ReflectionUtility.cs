using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RecursiveMapper.Utility
{
    // Operations with Type and Attributes
    static class ReflectionUtility
    {
        public static bool IsMapped(this Type type) => type.GetCustomAttribute<MappedClassAttribute> () != null;
        public static bool IsCompact(this Type type) => !type.IsMapped () || type.GetMappedAttribute ().IsCompact;
        public static MappedAttribute GetMappedAttribute(this FieldInfo info) => info.GetCustomAttribute<MappedAttribute> ();
        public static MappedClassAttribute GetMappedAttribute(this Type type) => type.GetCustomAttribute<MappedClassAttribute> ();

        public static IEnumerable<FieldInfo> GetFieldsWithMappedAttribute(Type type)
        {
            return IsMapped (type)
                       ? type.GetFields ().Where (info => GetMappedAttribute (info) != null)
                       : throw new InvalidOperationException ($"Type {type.Name} doesn't have a {nameof(MappedClassAttribute)}.");
        }

        public static IEnumerable<Type> GetEnumeratedTypes(FieldInfo info)
        {
            var numeratedType = info.FieldType;
            int counter = info.GetMappedAttribute ().DimensionCount;

            yield return numeratedType;
            while (counter-- > 0)
            {
                numeratedType = numeratedType.GetTypeInfo ().GetInterfaces ().FirstOrDefault (x => x.IsGenericType)?.GetGenericArguments ()[0];
                yield return numeratedType;
            }
        }
    }
}