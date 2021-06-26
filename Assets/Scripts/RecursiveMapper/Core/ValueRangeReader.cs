using System;
using System.Collections.Generic;
using Google.Apis.Sheets.v4.Data;

namespace RecursiveMapper
{
    struct ValueRangeReader  // i don't like you!
    {
        private readonly IList<IList<object>> range;
        private readonly Stack<ControlPoint> points;
        private int row, column;
        private IList<RecursiveMap<string>> result;

        public ValueRangeReader(ValueRange values)
        {
            points = new Stack<ControlPoint> ();
            range  = values.Values;
            row    = 0;
            column = 1;
            result = null;
        }

        public IList<RecursiveMap<string>> Read()
        {
            using var e = ((string)range[0][0]).GetEnumerator ();
            while (e.MoveNext() && Read(e.Current)) {}
            return result;
        }

        bool Read(char c)
        {
            switch (c)
            {
                case '[':
                case '(':
                case '<':
                    points.Push (new ControlPoint (row, column, c));
                    return true;
                case '.':
                    points.Push (new ControlPoint (row, column, c));
                    return MoveToNext (c);
                case ']':
                case ')':
                case '>':
                    return MoveToNext (c);
                default:
                    return true;
            }// ignoring the Sheet marker
        }

        bool MoveToNext(char c)
        {
            var lastPoint = points.Pop ();
            if (c - lastPoint.OpenChar < 0 || c - lastPoint.OpenChar > 2)
                throw new Exception ();                                   // todo - for debug purposes

            row    = c == '>' ? row + 1 : lastPoint.Row;
            column = c == '>' ? lastPoint.Column : column + 1;

            if (points.Count > 0)
            {
                points.Peek ().maps.Add (c == '.'
                                             ? new RecursiveMap<string> ((string)range[row][column], Meta.Point)
                                             : new RecursiveMap<string> (lastPoint.maps, null));
                return true;
            }

            result = lastPoint.maps;
            return false;
        }

        private readonly struct ControlPoint
        {
            public readonly int Row, Column;
            public readonly char OpenChar;
            public readonly IList<RecursiveMap<string>> maps;

            public ControlPoint(int r, int c, char ch)
            {
                Row      = r;
                Column   = c;
                OpenChar = ch;
                maps     = new List<RecursiveMap<string>> ();
            }
        }
    }
}