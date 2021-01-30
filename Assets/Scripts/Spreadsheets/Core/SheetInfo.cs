using System.Linq;
using System;
using Google.Apis.Sheets.v4.Data;

namespace Mimimi.SpreadsheetsSerialization.Core
{
    class SheetInfo : IDataPlacementInfo
    {
        public readonly string name;
        public readonly string pivot;
        public readonly string end;
        public readonly Type type;
        public readonly int[] indices;

        protected ValueRange source = null;

        public string SheetName => name;
        public string Range => $"'{name}'!{pivot}:{end}";

        public SheetInfo(Type _type, string _parametrizedName, params int[] _indices)
        {
            type = _type;
            indices = _indices;
            name = ClassNaming.AssembleSheetName (_type, _parametrizedName, indices);
            pivot = ClassMapping.GetPivotPoint (_type).A1;
            end = SpreadsheetsHelpers.DEFAULT_RANGE_END;
        }

        public FlexibleArray<string> SelectRead(ValueRange[] _ranges)
        {
            var match = _ranges.FirstOrDefault (name.MatchSheetName).Values;
            return match is null ? null : SpreadsheetRangePath.ReadSheet (match);
        }
    }
}