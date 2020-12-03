using System;
using System.Linq;
using System.Collections.Generic;
using Google.Apis.Sheets.v4.Data;

namespace Mimimi.SpreadsheetsSerialization.Core
{
    internal class SingleSheetInfo : IGetRequestInfo
    {
        private readonly Type type;
        private readonly string sheet;

        public object callbackObject;

        public string Name => $"Requested sheet '{type.Name}'";

        public SingleSheetInfo (Type _type)
        {
            type = _type;
            sheet = ClassMapping.GetSheetName (type);
        }

        public void SetRequestedValues(ValueRange[] _values)
        {
            var range = _values.First (x => x.MatchSheetName(sheet));
            ClassMapping.InvokeGetCallback (type, 
                                            callbackObject, 
                                            SpreadsheetRangePath.ReadSheet (range.Values));
        }

        public IEnumerable<string> GetSheetsList(string[] _sheets) => GetUnindexedSheetList ();

        public IEnumerable<string> GetUnindexedSheetList()
        {
            yield return SerializationHelpers.Range (sheet, type);
        }
    }
}