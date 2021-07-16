using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

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

        public static bool TryGetElement<T>(this IList<T> target, int index, out T result)
        {
            bool exists = (target?.Count ?? 0) > index;
            result = exists ? target[index] : default;
            return exists;
        }
        
#region Google Sheets A1 Notation
        public static string GetSheetName(this string range) => range.Split('!')[0].Replace("''", "'").Trim('\'', ' ');
        public static string GetFirstCell(this string range) => range.Split('!')[1].Split(':')[0];
        public static string GetA1Range(this IOMetaAttribute type, string sheet, string a2First) =>
            $"'{sheet.Trim()}'!{a2First}:{WriteA1 (type.Size.Add (ReadA1 (a2First)).Add(new V2Int(-1,-1)))}";

        static V2Int ReadA1(string a1) => new V2Int(Evaluate(a1.Where(char.IsLetter).Select(char.ToUpperInvariant), '@', SheetsIO.A1LettersCount),
                                                    Evaluate(a1.Where(char.IsDigit), '0', 10));

        static string WriteA1(V2Int a1) => (a1.X >= 999 ? string.Empty : new string(ToLetters (a1.X).ToArray())) 
                                         + (a1.Y >= 999 ? string.Empty : (a1.Y + 1).ToString());

        static IEnumerable<char> ToLetters(int number) => number < SheetsIO.A1LettersCount
                                                              ? new[]{(char)('A' + number)}
                                                              : ToLetters (number / SheetsIO.A1LettersCount - 1).Append ((char)('A' + number % SheetsIO.A1LettersCount));

        static int Evaluate(IEnumerable<char> digits, char zero, int @base)
        {
            int result = (int)digits.Reverse ().Select ((c, i) => (c - zero) * Math.Pow (@base, i)).Sum ();
            return result-- > 0 ? result : 999; // In Google Sheets notation, upper boundary of the range may be missing - it means "up to a big number"
        }
#endregion
#region IOPointer
        public static object CreateObject(this IOPointer p, object parent) => p.AddChild(parent, Activator.CreateInstance(p.TargetType)); // todo array

        public static object CreateFixedSizeArray(this IOPointer p, object parent) => NewArray(p, parent, p.Field.CollectionSize[p.Rank]);

        public static object CreateFreeSizeArray(this IOPointer p, object parent, IList<IList<object>> values) =>
            NewArray(p, parent, (p.Rank & 1) == 0
                                    ? (values.Count - p.Pos.X) / p.Field.TypeSizes[p.Rank + 1].X
                                    : (values.Skip(p.Pos.X).Take(p.Field.TypeSizes[p.Rank + 1].X).Max(v2 => v2.Count) - p.Pos.Y) / p.Field.TypeSizes[p.Rank + 1].Y);

        static object NewArray(IOPointer p, object parent, int size) => p.AddChild(parent, Array.CreateInstance(p.Field.ArrayTypes[p.Rank + 1], size));

        public static IEnumerable<int> EnumerateIndices(this IOPointer p) =>
            Enumerable.Range(0, p.Rank < p.Field.CollectionSize.Count ? p.Field.CollectionSize[p.Rank] : SheetsIO.MaxFreeSizeArrayElements);

        public static object AddChild(this IOPointer p, object parent, object child)//todo: do not use directly. include it to 'create child' method
        {
            if (p.Rank == 0)
                p.Field.Field.SetValue(parent, child);
            else if (parent is IList array)
                if (p.IsArray) array[p.Index] = child;
                else           array.Add(child);
            else
                addMethodInfo.MakeGenericMethod(p.Field.ArrayTypes[p.Rank]).Invoke(parent, new[] {child});
            return child;
        }
        
        public static IEnumerable<IOPointer> GetSheetPointers(this IOMetaAttribute type, string name) =>
            type.SheetsFields.Select((f, i) => new IOPointer(f, 0, i, V2Int.Zero, $"{name} {f.FrontType.SheetName}".Trim()));
        public static IEnumerable<IOPointer> GetPointers(this IOMetaAttribute type, V2Int pos) =>
            type.CompactFields.Select((f, i) => new IOPointer(f, 0, i, pos.Add(f.PosInType), ""));
#endregion
#region Writing 
        public static void ForEachChild(this object parent, IEnumerable<IOPointer> pointers, Action<IOPointer, object> action)
        {
            using var e = pointers.GetEnumerator();
            while (e.MoveNext() && parent.TryGetChild(e.Current, out var child))
                action.Invoke(e.Current, child);
        }

        static bool TryGetChild(this object parent, IOPointer p, out object child)
        {
            if (parent != null && p.Rank == 0)
            {
                child = p.Field.Field.GetValue(parent);
                return true;
            }
            if (parent is IList list && list.Count > p.Index)
            {
                child = list[p.Index];
                return true;
            }
            child = null;
            return !p.IsFreeSize;
        }
#endregion
    }
}
