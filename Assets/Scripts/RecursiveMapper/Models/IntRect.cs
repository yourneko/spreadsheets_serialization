namespace RecursiveMapper
{
    readonly struct IntRect
    {
        public readonly V2Int TopLeft, BottomRight;

        public V2Int Size => new V2Int (BottomRight.X - TopLeft.X, BottomRight.Y - TopLeft.Y);

        public IntRect(int x, int y, V2Int size)
        {
            TopLeft = new V2Int (x, y);
            BottomRight = new V2Int (size.X + x, size.Y + y);
        }
    }
}
