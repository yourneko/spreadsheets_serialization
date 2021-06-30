using System;
using System.Collections.Generic;
using System.Linq;
using Google.Apis.Sheets.v4.Data;

namespace RecursiveMapper
{
    struct ValueRangeBuilder
    {
        private readonly System.Text.StringBuilder sb;
        private readonly List<List<object>> output;
        private readonly string firstCell;
        private int x, y;

        public ValueRangeBuilder(string a1FirstCell)
        {
            sb        = new System.Text.StringBuilder ();
            output    = new List<List<object>> {new List<object> ()};
            firstCell = a1FirstCell;
            x         = 1;
            y         = 0;
        }

        public ValueRange ToValueRange(RecursiveMap<string> content)
        {
            (int x1, int y1) = SpreadsheetsUtility.ReadA1 (firstCell);
            var p = ProcessMapRecursive (content);
            output[0].Add(sb.ToString ());   // todo - custom formatting, like grey color (extend to attribute later)
            return new ValueRange {
                                      MajorDimension = "COLUMNS",
                                      Values         = output.Cast<IList<object>> ().ToList (),
                                      Range          = $"'{content.Meta.FullName}'!{firstCell}:{SpreadsheetsUtility.WriteA1 (x1 + p.X2, y1 + p.Y2)}",
                                  };
        }

        MapRegion ProcessMapRecursive(RecursiveMap<string> map)
        {
            if (map.IsValue)
            {
                EnsureCellIsReachable ();
                output[x].Add (map.Value);
                sb.Append ('.');
                return new MapRegion {X2 = x, Y2 = y};
            }

            var point = new MapRegion {X1 = x, Y1 = y, Vertical = !map.Meta.IsSingleObject && ((map.Meta.Rank & 1) == 0)};
            sb.Append (map.Meta.IsSingleObject ? '(' : (map.Meta.Rank & 1) == 0 ? '<' : '[');

            foreach (var element in map.Collection.Where(e => e.Meta.ContentType != ContentType.Sheet))
            {
                var resultRegion = ProcessMapRecursive (element);
                point.X2 = Math.Max (point.X2, resultRegion.X2);
                point.Y2 = Math.Max (point.Y2, resultRegion.Y2);
                x        = point.Vertical ? point.X1 : point.X2 + 1;
                y        = point.Vertical ? point.Y2 + 1 : point.Y1;
            }
            sb.Append (map.Meta.IsSingleObject ? ')' : (map.Meta.Rank & 1) == 0 ? '>' : ']');
            return point;
        }

        void EnsureCellIsReachable()
        {
            if (output.Count <= x)
                output.AddRange(Enumerable.Repeat(new List<object> (), x + 1 - output.Count));
            if (output[x].Count < y)
                output[x].AddRange (Enumerable.Repeat<object> (null, y - output[x].Count));
        }
    }
}