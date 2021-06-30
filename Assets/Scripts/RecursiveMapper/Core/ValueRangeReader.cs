using System;
using System.Collections.Generic;
using Google.Apis.Sheets.v4.Data;

namespace RecursiveMapper
{
    struct ValueRangeReader
    {
        private readonly IList<IList<object>> range;
        private readonly Stack<MapRegion> points;
        private int x, y;

        public ValueRangeReader(ValueRange values)
        {
            points = new Stack<MapRegion> ();
            range  = values.Values;
            x = 1;
            y = 0;
        }

        public IList<RecursiveMap<string>> Read()
        {
            points.Push (new MapRegion {X1 = 1, Y1 = 0, X2 = 1, Y2 = 0, Vertical = false, Maps = new List<RecursiveMap<string>> ()});
            IEnumerable<char> path = (string)range[0][0];
            foreach (var c in path)
                Read (c);
            return points.Pop().Maps;
        }

        void Read(char c)
        {
            MapRegion point = points.Peek ();
            switch (c)
            {
                case '[':
                case '(':
                case '<':
                    point    = new MapRegion {X1 = x, Y1 = y, Maps = new List<RecursiveMap<string>> (), Vertical = c == '<'};
                    points.Push (point);
                    break;
                case '.':
                    point.Maps.Add(new RecursiveMap<string>((string)range[x][y], Meta.Point));
                    point.X2 = Math.Max (point.X2, x);
                    point.Y2 = Math.Max (point.Y2, y);
                    break;
                case ']':
                case ')':
                case '>':
                    var closedPoint = points.Pop ();
                    point = points.Peek ();
                    point.Maps.Add(new RecursiveMap<string> (closedPoint.Maps, null));
                    point.X2 = Math.Max (point.X2, closedPoint.X2);
                    point.Y2 = Math.Max (point.Y2, closedPoint.Y2);
                    break;
                default: return;
            }
            x = point.Vertical
                    ? point.X1
                    : Math.Max (point.X1, point.X2 + 1);
            y = point.Vertical
                    ? Math.Max (point.Y1, point.Y2 + 1)
                    : point.Y1;
        }
    }
}