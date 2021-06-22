using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RecursiveMapper.Utility
{
    // Operations with RecursiveMap
    static class MappingUtility
    {
        private static readonly MethodInfo ExpandMethodInfo = typeof(MappingUtility).GetMethod ("ExpandCollection");

        public static IEnumerable<FieldInfo> GetMappedFields(this Type type) => ReflectionUtility.GetFieldsWithMappedAttribute (type)
                                                                                                 .OrderBy (x => x.GetMappedAttribute ().Position);

        public static RecursiveMap<string> Serialize(object obj, DimensionInfo info)
        {
            var type = obj.GetType ();
            return type.IsMapped ()
                       ? new RecursiveMap<object> (type.GetMappedFields ().Select (field => MapFieldValue(field, obj, info.Sheet)), info)
                          .Cast (Serialize)
                       : new RecursiveMap<string> (SerializeValue (obj), DimensionInfo.Point);
        }

        public static RecursiveMap<bool> MapSheetsHierarchy(this string[] availableSheets, Type type, DimensionInfo info)
        {
            // this goes 1 level deeper than we need. it will become useful later, when ValueRanges will be attached
            return type.IsCompact ()
                       ? new RecursiveMap<bool> (availableSheets.Contains(info.Sheet), info.Sheet.CreateDimensionInfo (type))
                       : new RecursiveMap<Type> (type.GetMappedFields ().Select (field => UnwrapFieldTypes(field, info.Sheet)), info)
                          .Cast (availableSheets.MapSheetsHierarchy);
        }

        static RecursiveMap<Type> UnwrapFieldTypes(FieldInfo field, string parentSheet)
        {
            var finalType = ReflectionUtility.GetEnumeratedTypes (field).Last ();
            var map = new RecursiveMap<Type> (finalType, parentSheet.CreateDimensionInfo (finalType));

            // todo - pass not evaluated enumerable? (to keep available sheets inside of MapperService)
            return map;
        }


        public static RecursiveMap<string> AssembleMap(this RecursiveMap<bool> sheetsHierarchy, IEnumerable<RecursiveMap<string>> values)
        {
            // from ugly hierarchy and pieces of map restore the full map (in correct order!)
            throw new NotImplementedException ();
        }

        public static DimensionInfo CreateDimensionInfo(this string parentSheet, Type type,  params int[] indices)
        {
            var mapRegionAttribute = type.GetCustomAttribute<MappedClassAttribute> ();
            return new DimensionInfo (indices.Length > 0
                                          ? (ContentType)(3 + indices.Length & 1)
                                          : mapRegionAttribute is null
                                              ? ContentType.Value
                                              : mapRegionAttribute.IsCompact
                                                  ? ContentType.Object
                                                  : ContentType.Sheet,
                                      ValueRangeUtility.GetFullSheetName(parentSheet, mapRegionAttribute?.SheetName ?? string.Empty),
                                      indices);
        }

        static RecursiveMap<object> MapFieldValue(FieldInfo field, object obj, string parentSheet)
        {
            var numeratedTypes = ReflectionUtility.GetEnumeratedTypes (field).ToArray ();
            var map = new RecursiveMap<object> (field.GetValue (obj), parentSheet.CreateDimensionInfo (numeratedTypes[0]));

            for (int i = 1; i < numeratedTypes.Length; i++)
                map = map.Cast (numeratedTypes[i].ExpandCollection);
            return map;
        }

        static RecursiveMap<object> ExpandCollection(this Type type, object obj, DimensionInfo info)
        {
            var elements = (IEnumerable<object>)ExpandMethodInfo.MakeGenericMethod (type)
                                                                .Invoke (null, new[] {obj});
            var map = elements.Select ((e, i) => new RecursiveMap<object> (e, info.Sheet.CreateDimensionInfo (type, info.Indices.Append (i).ToArray ())));
            return new RecursiveMap<object> (map, info);
        }

        static string SerializeValue<T>(T target) => target switch
                                                     {
                                                         int i    => i.ToString (),
                                                         bool b   => b ? "true" : "false",
                                                         string s => s,
                                                         float f  => f.ToString ("0.000"),
                                                         null     => string.Empty,
                                                         _        => target.ToString (),
                                                     };

        // used via Reflection
        static IEnumerable<object> ExpandCollection<T>(object value) => (value is IEnumerable<T> collection)
                                                                            ? collection.Cast<object> ()
                                                                            : throw new InvalidCastException ();
    }
}