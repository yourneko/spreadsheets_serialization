using System;
using System.Linq;

namespace Mimimi.Tools.A1Notation
{
    public struct A1Range : IEquatable<A1Range>
    {
        public readonly A1Point first, last;

        public int Width => last.x - first.x + 1;
        public int Height => last.y - first.y + 1;

        public override string ToString() => $"{first.A1}:{last.A1}";

        public A1Range(A1Point a, A1Point b)
        {
            first = a.Min (b);
            last = a.Max (b);
        }

        public bool Equals(A1Range other) => first.Equals (other.first) && last.Equals (other.last);
    }
}