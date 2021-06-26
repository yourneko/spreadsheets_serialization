using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RecursiveMapper.Utility
{
    // Operations with Type and Attributes
    static class ReflectionUtility
    {
        public static readonly MethodInfo ExpandMethodInfo = typeof(RecursiveMapUtility).GetMethod ("ExpandCollection",
                                                                                                    BindingFlags.Static | BindingFlags.NonPublic);

        private static readonly MethodInfo AddMethodInfo = typeof(ICollection<>).GetMethod ("Add",
                                                                                            BindingFlags.Instance | BindingFlags.Public);

        public static MappedAttribute GetMappedAttribute(this FieldInfo info) => info.GetCustomAttribute<MappedAttribute> ();
        public static MappedClassAttribute GetMappedAttribute(this Type type) => type.GetCustomAttribute<MappedClassAttribute> ();

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

        public static object CreateArray(Type arrayType, IEnumerable<object> arrayContent, Type contentType)
        {
            var result = Activator.CreateInstance(arrayType);
            var addMethod = ReflectionUtility.AddMethodInfo.MakeGenericMethod (contentType);
            foreach (object o in arrayContent)
                addMethod.Invoke (result, new[] {o});
            return result;
        }
    }
}