
using Google.Apis.Sheets.v4.Data;

namespace Mimimi.SpreadsheetsSerialization.Core
{
    interface IDataPlacementInfo
    {
        string SheetName { get; }
        string Range { get; } 
        FlexibleArray<string> SelectRead(ValueRange[] _ranges);
    }
}