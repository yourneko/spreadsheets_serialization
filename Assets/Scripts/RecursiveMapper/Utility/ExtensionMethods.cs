using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Google.Apis.Sheets.v4.Data;

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

        public static string GetRange(this MapClassAttribute type, string sheet, string a2First) =>
            $"'{sheet.Trim()}'!{a2First}:{SpreadsheetsUtility.WriteA1 (type.Size.Add (SpreadsheetsUtility.ReadA1 (a2First)))}";

        public static bool MatchRange(this ValueRange range, string name) =>
            StringComparer.Ordinal.Equals(range.Range.GetSheetFromRange(), name.GetSheetFromRange())
         && StringComparer.OrdinalIgnoreCase.Equals(range.Range.GetFirstCellFromRange(), name.GetFirstCellFromRange());

        public static string GetSheetFromRange(this string range) => range.Split('!')[0].Replace("''", "'").Trim('\'', ' ');
        static string GetFirstCellFromRange(this string range) => range.Split('!')[1].Split(':')[0];
    }
}
