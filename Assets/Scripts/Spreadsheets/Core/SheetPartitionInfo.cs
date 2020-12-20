using System;
using System.Linq;
using Google.Apis.Sheets.v4.Data;

namespace Mimimi.SpreadsheetsSerialization.Core
{
    internal class SheetPartitionInfo : IDataPlacementInfo
    {
        private readonly SheetInfo sheet;
        private readonly Type type;

        private FlexibleArray<string>[] results;
        private int index;

        public string SheetName => sheet.SheetName;
        public string Range => sheet.Range;

        public SheetPartitionInfo(SheetInfo _source, Type _type)
        {
            sheet = _source;
            type = _type;

        }

        public FlexibleArray<string> SelectRead(ValueRange[] _ranges)
        {
            if (results is null)
                results = ClassMapping.SmallElementsArray (type)
                                       .Associate (sheet.SelectRead (_ranges))
                                       .Bind (x => x.Item2)
                                       .GetValues ()
                                       .ToArray ();
            return results[index++ % results.Length];
        }
    }
}