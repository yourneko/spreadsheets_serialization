using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Google.Apis.Sheets.v4.Data;

namespace SheetsIO
{
    static class ExtensionMethods
    {
        static readonly MethodInfo addMethodInfo = typeof(ICollection<>).GetMethod ("Add", BindingFlags.Instance | BindingFlags.Public);
        public static IOFieldAttribute GetIOAttribute(this FieldInfo field)
        {
            var attribute = (IOFieldAttribute)Attribute.GetCustomAttribute (field, typeof(IOFieldAttribute));
            if (attribute != null && !attribute.Initialized)
                attribute.CacheMeta (field);
            return attribute;
        }

        public static IOMetaAttribute GetIOAttribute(this Type type)
        {
            var attribute = (IOMetaAttribute)Attribute.GetCustomAttribute (type, typeof(IOMetaAttribute));
            if (attribute != null && !attribute.Initialized)
                attribute.CacheMeta (type);
            return attribute;
        }

        public static IEnumerable<Type> GetArrayTypes(this Type fieldType, int max)
        {
            int rank = max;
            var t = fieldType;
            do yield return t;
            while (--rank >= 0 && t != typeof(string) &&
                   (t = t.GetTypeInfo().GetInterfaces().FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(ICollection<>))
                        ?.GetGenericArguments()[0]) != null);
        }

        public static object AddChild(this IOFieldAttribute field, object parent, int rank, int index, object child)
        {
            if (rank == 0)
                field.Field.SetValue(parent, child);
            else if (parent is IList array)
                if (array.Count > index)
                    array[index] = child;
                else
                    array.Add(child);
            else
                addMethodInfo.MakeGenericMethod(field.ArrayTypes[rank]).Invoke(parent, new[] {child});
            return child;
        }

        public static string GetA1Range(this IOMetaAttribute type, string sheet, string a2First) =>
            $"'{sheet.Trim()}'!{a2First}:{SpreadsheetsUtility.WriteA1 (type.Size.Add (SpreadsheetsUtility.ReadA1 (a2First)).Add(new V2Int(-1,-1)))}";

        public static bool IsSameRange(this ValueRange range, string name) =>
            StringComparer.Ordinal.Equals(range.Range.GetSheetFromRange(), name.GetSheetFromRange())
         && StringComparer.OrdinalIgnoreCase.Equals(range.Range.GetFirstCellFromRange(), name.GetFirstCellFromRange());

        public static string GetSheetFromRange(this string range) => range.Split('!')[0].Replace("''", "'").Trim('\'', ' ');
        static string GetFirstCellFromRange(this string range) => range.Split('!')[1].Split(':')[0];

        public static bool TryGetElement<T>(this IList<T> target, int index, out T result)
        {
            bool exists = (target?.Count ?? 0) > index;
            result = exists ? target[index] : default;
            return exists;
        }
    }
}
