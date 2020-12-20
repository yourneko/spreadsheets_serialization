using System.Collections.Generic;
using Google.Apis.Sheets.v4.Data;

namespace Mimimi.SpreadsheetsSerialization.Core
{
    public abstract class CustomBatchRequest : CustomRequest 
    {
        protected bool locked;
        public abstract List<ValueRange> ValueRanges { get; protected set; }
    }
}