using System.Collections.Generic;
using System.Text;

namespace RecursiveMapper
{
    class ValueRangeAssembleContext
    {
        public readonly IList<IList<object>> Values = new List<IList<object>> (new List<object>[1]);
        public readonly StringBuilder Sb = new StringBuilder (); }
}