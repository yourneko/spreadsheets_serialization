using System.Linq;
using Google.Apis.Sheets.v4.Data;

namespace Mimimi.SpreadsheetsSerialization.Core
{
    internal class SheetInfo : IDataPlacementInfo
    {
        public readonly string name;
        public readonly string pivot;
        public readonly string end;

        protected ValueRange source;

        public string SheetName => name;
        public string Range => $"'{name}'!{pivot}:{end}";

        public SheetInfo(string _name, string _pivot, string _end)
        {
            name = _name;
            pivot = _pivot;
            end = _end;
        }

        public FlexibleArray<string> SelectRead(ValueRange[] _ranges)
        {
            var match = _ranges.FirstOrDefault (name.MatchSheetName).Values;
            return match is null ? null : SpreadsheetRangePath.ReadSheet (match);
        }
    }
}