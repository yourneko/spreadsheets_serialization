using System.Collections.Generic;

namespace RecursiveMapper
{
    class MapRegion
    {
        public int X1 = 0, Y1 = 0, X2 = -1, Y2 = -1;
        public bool Vertical;
        public List<RecursiveMap<string>> Maps;
    }
}