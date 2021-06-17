using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Google.Apis.Sheets.v4.Data;

namespace RecursiveMapper.Utility
{
    static class ValueRangeUtility
    {
        private static readonly IList<IList<object>> Empty = new List<IList<object>> ();

        public static bool CreateValueRange(this RecursiveMap<string> content, string sheet, string firstCell, out ValueRange range)
        {
            StringBuilder sb = new StringBuilder ();
            var values = content.Arrange (ref sb);
            values.Insert (0, new object[] {sb.ToString ()});

            range = Create (values, sheet, firstCell);
            return values.Count > 1;
        }

        static ValueRange Create(IList<IList<object>> data, string sheet, string a1FirstCell)
        {
            (int x, int y) = A1Notation.Read (a1FirstCell);
            var a1LastCell = A1Notation.Write (x + data.Count, y + data.Max (column => column.Count));
            return new ValueRange {
                                      MajorDimension = "COLUMN",
                                      Values = data,
                                      Range = $"'{sheet}'!{a1FirstCell}:{a1LastCell}"
                                  };
        }

        static IList<IList<object>> Arrange(this RecursiveMap<string> values, ref StringBuilder sb)
        {
            switch (values.DimensionInfo.ContentType)
            {
                case ContentType.Value:
                    sb.Append ('.');
                    return new IList<object>[] {new object[] {values.Left}};
                case ContentType.Object:
                    sb.Append ('(');
                    var objArranged = values.ArrangeChildren (AddToRight, ref sb);
                    sb.Append (')');
                    return objArranged;
                case ContentType.HorizontalArray:
                    sb.Append ('[');
                    var hArrayArranged = values.ArrangeChildren (AddToRight, ref sb);
                    sb.Append (']');
                    return hArrayArranged;
                case ContentType.VerticalArray:
                    sb.Append ('<');
                    var vArrayArranged = values.ArrangeChildren (AddToBottom, ref sb);
                    sb.Append ('>');
                    return vArrayArranged;
                case ContentType.Sheet:
                    sb.Append ('S');
                    return Empty;
                case  ContentType.SheetsArray:
                    sb.Append ('A');
                    return Empty;
                default:
                    throw new InvalidOperationException ();
            }
        }

        static IList<IList<object>> ArrangeChildren(this RecursiveMap<string> values, Action<IList<IList<object>>, IList<IList<object>>> add, ref StringBuilder sb)
        {
            var result = new List<IList<object>> ();
            foreach (var element in values.Right)
                add.Invoke(result, element.Arrange (ref sb));
            return result;
        }

        static void AddToRight(IList<IList<object>> target, IList<IList<object>> added)
        {
            foreach (var column in added)
                target.Add (column);
        }

        static void AddToBottom(IList<IList<object>> target, IList<IList<object>> added)
        {
            int height = target.Any()
                             ? target.Max (column => column.Count)
                             : 0;

            for (int i = target.Count; i < Math.Max (target.Count, added.Count); i++)
                target.Add (new List<object> ());

            foreach (var column in target)
                while (column.Count < height)
                    column.Add (string.Empty);

            for (int i = 0; i < added.Count; i++)
                foreach (var value in added[i])
                    target[i].Add (value);
        }
    }
}