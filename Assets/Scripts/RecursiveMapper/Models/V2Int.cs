using System;

namespace SpreadsheetsMapper
{
    readonly struct V2Int
    {
        public readonly int X, Y;

        public V2Int(int x, int y)
        {
            X = x;
            Y = y;
        }

        public V2Int Max(V2Int other) => new V2Int (Math.Max (X, other.X), Math.Max (Y, other.Y));
        public V2Int Add(V2Int other) => new V2Int (X + other.X, Y + other.Y);
        public V2Int Scale(int x, int y) => new V2Int (X * x,Y * y);
    }
}
