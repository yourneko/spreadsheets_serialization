using System;

namespace RecursiveMapper
{
    readonly struct V2Int
    {
        public readonly int X, Y;
        public V2Int(int x, int y) { X = x; Y = y; }
        public V2Int Join(V2Int other) => new V2Int (Math.Max (X, other.X), Math.Max (Y, other.Y));
        public V2Int Add(V2Int other) => new V2Int (X + other.X, Y + other.Y);
        public V2Int Next(V2Int last, bool isY) =>  Join (new V2Int (isY ? -1  : last.X + 1 , isY ? last.Y + 1: -1 ));
    }
}