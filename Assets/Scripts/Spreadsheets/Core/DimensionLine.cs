using Mimimi.Tools.A1Notation;

namespace Mimimi.SpreadsheetsSerialization.Core
{
    class DimensionLine
    {
        // the line where every element of this dimension lays on
        public readonly A1Line line;
        public A1Point Next { get; private set; }

        // FarPoint is 'last' boundary point of the corresponding range
        public A1Point FarPoint { get; private set; }

        public void MovePointer()
        {
            FarPoint = FarPoint.Max (Next);
            Next = Next.Translate (line.direction, 1);
        }

        public void MovePointer(DimensionLine _other)
        {
            FarPoint = FarPoint.Max (_other.FarPoint);
            Next = line.GetProjection (FarPoint, 1);
        }


        public DimensionLine(A1Point _originPoint, A1Direction _direction)
        {
            line = _originPoint.CreateLine (_direction);
            FarPoint = A1Point.zero;
            Next = _originPoint;
        }
    }
}