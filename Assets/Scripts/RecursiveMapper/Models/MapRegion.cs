using System.Collections.Generic;

namespace RecursiveMapper
{
    class MapRegion
    {
        public int X1, Y1, X2, Y2;
        public bool Vertical;
        public List<RecursiveMap<string>> Maps;
    }
}