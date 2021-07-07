using System;

namespace RecursiveMapper
{
    readonly struct V2Int
    {
        public static readonly V2Int Zero = new V2Int (0, 0);

        public readonly int X, Y;

        public V2Int(int x, int y)
        {
            X = x;
            Y = y;
        }

        public V2Int Join(V2Int other) => new V2Int (Math.Max (X, other.X), Math.Max (Y, other.Y));
        public V2Int Add(V2Int other) => new V2Int (X + other.X, Y + other.Y);
        public V2Int Next(V2Int last, bool isY) =>  Join (new V2Int (isY ? -1  : last.X + 1 , isY ? last.Y + 1: -1 ));
        public V2Int JoinDown(V2Int otherSize) => new V2Int (Math.Max (X, otherSize.X), Y + otherSize.Y);
        public V2Int JoinRight(V2Int otherSize) => new V2Int (X + otherSize.X, Math.Max (Y, otherSize.Y));
        public V2Int Scale(V2Int other) => new V2Int (X * other.X,Y * other.Y);
        public V2Int Split(V2Int parts) => new V2Int (X / parts.X, Y / parts.Y);
    }
}