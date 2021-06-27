using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Google.Apis.Sheets.v4.Data;

namespace RecursiveMapper
{
    struct ValueRangeBuilder
    {
        private readonly RecursiveMap<string> reference;
        private readonly StringBuilder sb;

        public ValueRangeBuilder(RecursiveMap<string> content)
        {
            sb        = new StringBuilder ();
            reference = content;
        }

        public ValueRange ToValueRange(string firstCell)
        {
            var data = ArrangeRecursive (reference);
            data.Insert (0, new List<object> {sb.ToString ()});

            (int x, int y) = SpreadsheetsUtility.ReadA1 (firstCell);
            var a1LastCell = SpreadsheetsUtility.WriteA1 (x + data.Count, y + data.Max (column => column.Count));
            return new ValueRange {
                                      MajorDimension = "COLUMN",
                                      Values         = data,
                                      Range          = $"'{reference.Meta.Sheet}'!{firstCell}:{a1LastCell}"
                                  };
        }

        IList<IList<object>> ArrangeRecursive(RecursiveMap<string> values)
        {
            if (!values.Meta.IsObject)
            {
                bool horizontal = (values.Meta.Rank & 1) > 0;
                sb.Append(horizontal ? '[' : '<');
                var result = horizontal
                                 ? values.Collection.SelectMany (ArrangeRecursive).ToList ()
                                 : ListVerticalArrayRecursive (values);
                sb.Append (horizontal ? ']' : '>');
                return result;
            }

            switch (values.Meta.ContentType)
            {
                case ContentType.Value:
                    sb.Append ('.');
                    return new IList<object>[] {new object[] {values.Value}};
                case ContentType.Object:
                    sb.Append ('(');
                    var objArranged = values.Collection.SelectMany (ArrangeRecursive).ToList ();
                    sb.Append (')');
                    return objArranged;
                default:
                    return new IList<object>[]{new List<object>()};
            }
        }

        IList<IList<object>> ListVerticalArrayRecursive(RecursiveMap<string> values)
        {
            var elements = values.Collection.Select (ArrangeRecursive).ToList();
            EqualizeLengths(elements, () => new List<object>());
            foreach (var element in elements)
                EqualizeLengths (element, () => new object ());
            return Enumerable.Range (0, elements.Count)
                             .Select (i => (IList<object>)elements.SelectMany (e => e[i]).ToList())
                             .ToList ();
        }

        static void EqualizeLengths<T>(IList<IList<T>> lists, Func<T> get)
        {
            int height = lists.Max (e => e.Count);
            foreach (var list in lists)
                for (int i = 0; i < height - list.Count; i++)
                    list.Add (get.Invoke());
        }
    }
}