namespace RecursiveMapper
{
    readonly struct IntRect
    {
        public readonly V2Int From, Till;

        public V2Int Size => new V2Int (Till.X - From.X, Till.Y - From.Y);
        public IntRect(V2Int from, V2Int till)
        {
            From = from;
            Till = till;
        }

        public IntRect(int x, int y, V2Int size)
        {
            From = new V2Int (x, y);
            Till = new V2Int (size.X + x, size.Y + y);
        }
    }
}