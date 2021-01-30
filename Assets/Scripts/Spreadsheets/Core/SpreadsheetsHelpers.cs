using System;
using System.Collections.Generic;
using System.Linq;
using Google.Apis.Sheets.v4.Data;

namespace Mimimi.SpreadsheetsSerialization.Core
{
    static class SpreadsheetsHelpers
    {
        internal const string DEFAULT_RANGE_PIVOT = "B2";
        internal const string DEFAULT_RANGE_END = "ZZ999";

        internal static string GetSheetName(this ValueRange _range) => _range.Range.Split ('!')[0].Trim ('\'');

        internal static bool MatchSheetName(this string _match, ValueRange _range) => StringComparer.OrdinalIgnoreCase.Equals (_range.GetSheetName (), _match);
        internal static bool MatchStrings(this IEnumerable<string> _sheets, string _compared) => _sheets.Any (x => StringComparer.OrdinalIgnoreCase.Equals (_compared, x));
        
#region EITHER

        internal static FlexibleArray<IDataPlacementInfo> ToSheetsArray(this string[] _sheetsExist, Either<GroupInfo, SheetInfo> _target)
        {
            return _target.IsLeft ? 
                   _target.Left.GetCachedRanges (_sheetsExist) : 
                   new FlexibleArray<IDataPlacementInfo> (_target.Right);
        }

        internal static bool MatchesInfoRanges(this string[] _sheetNames, Either<GroupInfo, SheetInfo> _target)
        {
            return _target.IsLeft ?
                   _target.Left.HasRequiredSheets(_sheetNames) :
                   _sheetNames.AnyMatches (_target.Right);
        }

#endregion

        internal static bool AnyMatches(this IEnumerable<string> _names, IDataPlacementInfo _info) => _names.MatchStrings (_info.SheetName);

    }
}