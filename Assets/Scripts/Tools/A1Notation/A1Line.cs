namespace Mimimi.Tools.A1Notation
{
    public struct A1Line
    {
        public readonly A1Direction direction;
        public readonly int? x;
        public readonly int? y;
        public int Index => x ?? y ?? 0;

        public A1Line(A1Direction _direction, int _index)
        {
            y = _direction == A1Direction.Row ? (System.Nullable<int>)_index : null;
            x = _direction == A1Direction.Row ? null : (System.Nullable<int>)_index;
            direction = _direction;
        }

        public override string ToString()
        {
            switch (direction)
            {
                case A1Direction.Row:    return "*" + A1Notation.ToDigits (Index);
                case A1Direction.Column: return A1Notation.ToLetters (Index) + "*";
                default:                 throw new System.Exception ();
            }
        }
    }
}
