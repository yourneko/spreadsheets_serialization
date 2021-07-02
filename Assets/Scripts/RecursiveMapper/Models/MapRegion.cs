namespace RecursiveMapper
{
    class MapRegion
    {
        public int X1 = 0, Y1 = 0, X2 = -1, Y2 = -1;

        public void Add(MapRegion other)
        {
            X2 = System.Math.Max (X2, other.X2);
            Y2 = System.Math.Max (Y2, other.Y2);
        }
    }
}