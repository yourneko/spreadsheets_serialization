namespace Mimimi.SpreadsheetsSerialization.Core
{
    enum PathAction
    {
        OpenX = '(',
        CloseX = ')',
        OpenY = '[',
        CloseY = ']',
        Header = '*',
        Value = ',',
    }
}