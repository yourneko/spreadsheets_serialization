using System;

namespace Mimimi.Tools.A1Notation
{
    public struct A1Point : IEquatable<A1Point>
    {
        public readonly static A1Point zero = new A1Point (0, 0);

        public readonly string A1;
        public readonly int x;
        public readonly int y;

        public A1Point (string _A1)
        {
            A1 = _A1;
            var coordinates = A1Notation.Read (A1);
            x = coordinates.x;
            y = coordinates.y;
        }

        public A1Point (int x, int y)
        {
            this.x = x;
            this.y = y;
            A1 = A1Notation.Write (x, y);
        }

        public bool Equals(A1Point other) => x == other.x && y == other.y;

        public override string ToString() => A1;
    }
}
