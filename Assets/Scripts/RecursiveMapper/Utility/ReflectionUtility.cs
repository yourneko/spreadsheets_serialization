using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RecursiveMapper.Utility
{
    static class ReflectionUtility
    {
        public static bool IsSerializedToValue(Type type) => type.GetCustomAttribute<MappedClassAttribute> () != null;

        public static IEnumerable<FieldInfo> GetFieldsWithMappedAttribute(Type type)
        {
            return IsSerializedToValue (type)
                       ? throw new InvalidOperationException ($"Type {type.Name} doesn't have a {nameof(MappedClassAttribute)}.")
                       : type.GetFields ().Where (info => GetMappedAttribute (info) != null);
        }

        public static MappedAttribute GetMappedAttribute(this FieldInfo info) => info.GetCustomAttribute<MappedAttribute> ();

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