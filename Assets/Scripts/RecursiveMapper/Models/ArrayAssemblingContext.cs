using System.Collections.Generic;
using System.Text;

namespace RecursiveMapper
{
    class ArrayAssemblingContext
    {
        public readonly IList<IList<object>> Values = new List<IList<object>>(new List<object>[1]);
        public readonly StringBuilder SB = new StringBuilder();
        public int X = 1, Y = 0;
    }
}