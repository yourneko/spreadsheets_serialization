using System.Collections.Generic;

namespace Mimimi.SpreadsheetsSerialization.Core
{
    public interface IGetRequestInfo
    {
        string Name { get; }
        IEnumerable<string> GetUnindexedSheetList();
        IEnumerable<string> GetSheetsList(string[] _sheets);
        void SetRequestedValues(Google.Apis.Sheets.v4.Data.ValueRange[] _values);
    }
}