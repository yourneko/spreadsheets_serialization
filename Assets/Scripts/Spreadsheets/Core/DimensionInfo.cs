using Mimimi.Tools.A1Notation;

namespace Mimimi.SpreadsheetsSerialization.Core
{
    public struct DimensionInfo
    {
        public readonly A1Direction direction;

        public DimensionInfo(A1Direction _direction)
        {
            direction = _direction;
        }
    }
}
