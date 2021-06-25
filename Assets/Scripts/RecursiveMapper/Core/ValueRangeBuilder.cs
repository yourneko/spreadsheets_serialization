using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Google.Apis.Sheets.v4.Data;

namespace RecursiveMapper.Utility
{
    struct ValueRangeBuilder
    {
        private readonly IList<IList<object>> data;
        private readonly StringBuilder sb;
        private readonly string sheetName;

        public ValueRangeBuilder(RecursiveMap<string> content)
        {
            data = null;
            sb   = new StringBuilder ();
            sheetName = content.Meta.Sheet;
            data      = ArrangeRecursive (content);
            data.Insert (0, new List<object> {sb.ToString ()});
        }

        public ValueRange ToValueRange(string firstCell)
        {
            (int x, int y) = A1Notation.Read (firstCell);
            var a1LastCell = A1Notation.Write (x + data.Count, y + data.Max (column => column.Count));
            return new ValueRange {
                                      MajorDimension = "COLUMN",
                                      Values         = data,
                                      Range          = $"'{sheetName}'!{firstCell}:{a1LastCell}"
                                  };
        }

        IList<IList<object>> ArrangeRecursive(RecursiveMap<string> values)
        {
            if (values.Meta.IsObject)
            {
                if ((values.Meta.Rank & 1) > 0) // for array, odd ranks are horizontal, even ranks are vertical
                {
                    sb.Append ('[');
                    List<IList<object>> horizontalArray = values.Right.SelectMany (ArrangeRecursive).ToList ();
                    sb.Append (']');
                    return horizontalArray;
                }

                sb.Append ('<');
                List<IList<object>> array = new List<IList<object>> ();
                foreach (var part in values.Right.Select (ArrangeRecursive))
                {
                    for (int i = array.Count; i < Math.Max (array.Count, part.Count); i++)
                        array.Add (new List<object> ());

                    int height = array.Any() ? array.Max (column => column.Count) : 0;
                    foreach (var column in array)
                        while (column.Count < height)
                            column.Add (string.Empty);

                    for (int i = 0; i < part.Count; i++)
                        foreach (var value in part[i])
                            array[i].Add (value);
                }

                sb.Append ('>');
                return array;
            }

            switch (values.Meta.ContentType)
            {
                case ContentType.Value:
                    sb.Append ('.');
                    return new IList<object>[] {new object[] {values.Left}};
                case ContentType.Object:
                    sb.Append ('(');
                    var objArranged = values.Right.SelectMany (ArrangeRecursive).ToList ();
                    sb.Append (')');
                    return objArranged;
                case ContentType.Sheet:
                    sb.Append ('*');
                    return new IList<object>[]{new List<object>()};
                default:
                    throw new InvalidOperationException ();
            }
        }
    }
}