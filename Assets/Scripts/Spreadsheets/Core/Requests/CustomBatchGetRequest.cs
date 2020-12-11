using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Google.Apis.Sheets.v4.Data;

namespace Mimimi.SpreadsheetsSerialization.Core
{
    public class CustomBatchGetRequest : CustomBatchRequest
    {
        private bool locked;
        private readonly bool containsSheetArray;

        public override List<ValueRange> ValueRanges { get; protected set; }
        private readonly List<IGetRequestInfo> requests;
        private readonly List<string> ranges;

        public IEnumerable<string> SheetRanges => ranges.Distinct ();
        public override string Description => $"BatchGet request. Contains {requests.Count} objects, {ranges.Count} sheets total";

        public void SetResponse(List<ValueRange> _response)
        {
            foreach (var r in requests)
                r.SetRequestedValues (_response.ToArray ());
            requests.Clear ();
            ranges.Clear ();
        }

        public CustomBatchGetRequest(string spreadsheetID, bool containsIndexedSheets)
        {
            SpreadsheetID = spreadsheetID;
            requests = new List<IGetRequestInfo> ();
            ranges = new List<string> ();
            containsSheetArray = containsIndexedSheets;
        }

        /// <summary> 
        /// After the response is received and read, requested objects will be sent back via callbacks. 
        /// If the request will fail, you'll see the error message, but none of callbacks will be called.
        /// </summary>
        /// <remarks> Don't keep references to enqueued request. </remarks>
        public void Add<T>(Action<T> _callback)
        {
            if (!locked)
            {
                switch (ClassMapping.GetTypeSpaceRequirement (typeof (T)))
                {
                    case SpaceRequired.Sheet:
                        requests.Add (new SingleSheetInfo (typeof (T)) { callbackObject = _callback });
                        break;
                    case SpaceRequired.SheetsGroup:
                        requests.Add (new SheetsGroupInfo (typeof (T)) { callbackObject = _callback }); 
                        break;
                    default:
                        throw new Exception ("Can't request a type with no specified range");
                }
            }
            else throw new Exception ("New requests cannot be included to enqueued batched request.");
            // the cancellation token for callbacks might be introduced. 
            // but personally I have never run in situation where it would happen to be any useful
        }

        public void Enqueue()
        {
            locked = true;
            if (containsSheetArray)
                SpreadsheetRequest.Send (SpreadsheetID, OnSpreadsheetReceived);
            else
                CalculateRangesNoArrays ();
        }

        private void OnSpreadsheetReceived(Spreadsheet _spreadsheet)
        {
            string[] sheetsExist = _spreadsheet.Sheets.Select (x => x.Properties.Title).ToArray ();
            CalculateRanges (sheetsExist);
        }

        // I failed to find a way around this split. From one side, additional SpreadsheetRequest slows things down.
        private void CalculateRanges(params string[] _sheetsExist)
        {
            foreach (var r in requests)
            {
                var list = r.GetSheetsList (_sheetsExist);
                if (!list.Any ())
                    throw new Exception ($"Request was cancelled. {r.Name} has no valid spreadsheets ranges");
                foreach (var e in list)
                    ranges.Add (e);
            }
            SerializationService.Enqueue (this);
        }

        private void CalculateRangesNoArrays()
        {
            foreach (var r in requests)
            {
                var list = r.GetUnindexedSheetList ();
                if (!list.Any ())
                    throw new Exception ($"Request was cancelled. {r.Name} has no valid spreadsheets ranges");
                foreach (var e in list)
                    ranges.Add (e);
            }
            SerializationService.Enqueue (this);
        }
    }
}