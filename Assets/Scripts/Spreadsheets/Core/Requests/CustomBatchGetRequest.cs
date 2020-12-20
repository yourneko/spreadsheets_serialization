using System;
using System.Collections.Generic;
using System.Linq;
using Google.Apis.Sheets.v4.Data;

namespace Mimimi.SpreadsheetsSerialization.Core
{
    public class CustomBatchGetRequest : CustomBatchRequest
    {
        private readonly bool containsSheetArray;

        public override List<ValueRange> ValueRanges { get; protected set; }
        private readonly Dictionary<FieldAssembler, object> requests;
        private readonly List<string> ranges;

        public IEnumerable<string> SheetRanges => ranges.Distinct ();
        public override string Description => $"BatchGet request. Contains {requests.Count} objects, {ranges.Count} sheets total";

        public void SetResponse(ValueRange[] _response)
        {
            foreach (var r in requests)
                ClassMapping.InvokeGetCallback (r.Key.titleType, r.Value, r.Key.GetStringArray (_response));

            requests.Clear ();
            ranges.Clear ();
        }

        public CustomBatchGetRequest(string spreadsheetID, bool containsIndexedSheets)
        {
            UnityEngine.Debug.Log ($"Creating GET request. Indexing = {containsIndexedSheets}");
            SpreadsheetID = spreadsheetID;
            requests = new Dictionary<FieldAssembler, object> ();
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
                requests.Add (new FieldAssembler (typeof (T)), _callback);
            else
                throw new Exception ("New requests cannot be included to enqueued batched request.");
            // the cancellation token for callbacks might be introduced. 
            // but personally I have never run in situation where it would happen to be any useful
        }

        public void Enqueue()
        {
            locked = true;
            if (containsSheetArray)
                SpreadsheetRequest.Send (SpreadsheetID, OnSpreadsheetReceived);
            else
                StartQueue (null);
        }

        private void OnSpreadsheetReceived(Spreadsheet _spreadsheet)
        {
            string[] sheetsExist = _spreadsheet.Sheets.Select (x => x.Properties.Title).ToArray ();
            StartQueue (sheetsExist);
        }

        private void StartQueue(string[] _existingSheetNames)
        {
            foreach (var r in requests)
            {
                var required = r.Key.GetSpreadsheetRanges (_existingSheetNames).GetValues()
                                    .Select(x => x.Range)
                                    .Distinct();
                foreach (var e in required)
                    ranges.Add (e);
            }
            UnityEngine.Debug.Log (string.Join (Environment.NewLine, ranges));
            SerializationService.Enqueue (this);
        }
    }
}