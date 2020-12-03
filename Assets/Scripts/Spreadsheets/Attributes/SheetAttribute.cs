using Mimimi.Tools.A1Notation;

namespace Mimimi.SpreadsheetsSerialization
{
    public class SheetAttribute : MapSpaceAttribute
    {
        public override SpaceRequired RequiredSpace => SpaceRequired.Sheet;
        public string SheetName { get; private set; }
        public A1Point? CustomRangeAnchor { get; private set; }

        /// <param name="name"> Sheet name should contain only letters, digits, empty space and underscore.  </param>
        public SheetAttribute(string name)
        {
            SheetName = name;
        }

        public SheetAttribute(string name, int startFromRow, int startFromColumn)
        {
            SheetName = name;
            CustomRangeAnchor = new A1Point (startFromColumn, startFromRow);
        }
    }
}
