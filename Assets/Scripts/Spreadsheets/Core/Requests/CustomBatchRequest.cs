using System;
using System.Collections.Generic;
using Google.Apis.Sheets.v4.Data;

namespace Mimimi.SpreadsheetsSerialization.Core
{
    public abstract class CustomBatchRequest : CustomRequest // in the name of holy batching
    {
        protected Action callback;

        public abstract List<ValueRange> ValueRanges { get; protected set; }
    }
}