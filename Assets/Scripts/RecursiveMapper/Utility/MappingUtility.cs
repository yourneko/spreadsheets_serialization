using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RecursiveMapper.Utility
{
    static class MappingUtility
    {
        private static readonly MethodInfo ExpandMethodInfo = typeof(MappingUtility).GetMethod ("ExpandCollection");

        public static RecursiveMap<FieldInfo> GetMappedFields(Type type)
        {
            return new RecursiveMap<FieldInfo> (ReflectionUtility.GetFieldsWithMappedAttribute (type)
                                                                 .OrderBy (x => x.GetMappedAttribute ().Position)
                                                                 .Select (info => new RecursiveMap<FieldInfo> (info, DimensionInfo.None)),
                                                DimensionInfo.None /*GetDimensionInfoForType(type)*/);
        }

        public static RecursiveMap<object> GetExpandedValue(this object obj, FieldInfo info)
        {
            var numeratedTypes = ReflectionUtility.GetEnumeratedTypes (info).ToArray ();

            var dimensions = Enumerable.Range (0, numeratedTypes.Length - 1)
                                       .Select (index => new DimensionInfo ((ContentType)(3 + (index & 1)))) // todo - use attributes for this
                                       .Append(GetDimensionInfoForType(info.FieldType))
                                       .ToArray();

            var map = new RecursiveMap<object> (info.GetValue (obj), dimensions[0]);
            for (int i = 1; i < numeratedTypes.Length; i++)
                map.Expand (numeratedTypes[i].ToCollection, dimensions[i]);

            return map;
        }

        public static RecursiveMap<T> Simplify<T>(this RecursiveMap<RecursiveMap<T>> map)
        {
            return map.IsLeft
                       ? map.Left
                       : new RecursiveMap<T> (map.Right.Select (element => element.Simplify ()), map.DimensionInfo);
        }

        static DimensionInfo GetDimensionInfoForType(Type type)
        {
            var mapRegionAttribute = type.GetCustomAttribute<MappedClassAttribute> ();
            return new DimensionInfo (mapRegionAttribute is null
                                          ? ContentType.Value
                                          : mapRegionAttribute.IsCompact
                                              ? ContentType.Object
                                              : ContentType.Sheet);
        }

        static IEnumerable<object> ToCollection(this Type type, object element) => (IEnumerable<object>)ExpandMethodInfo.MakeGenericMethod (type)
                                                                                                                        .Invoke (null, new[] {element});

        // used via Reflection
        static IEnumerable<object> ExpandCollection<T>(object value) => (value is IEnumerable<T> collection)
                                                                            ? collection.Cast<object> ()
                                                                            : throw new InvalidCastException ();
    }
}