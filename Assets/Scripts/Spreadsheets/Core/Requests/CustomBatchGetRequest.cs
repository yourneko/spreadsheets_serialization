using System;
using System.Collections.Generic;
using System.Linq;
using Google.Apis.Sheets.v4.Data;

namespace Mimimi.SpreadsheetsSerialization.Core
{
    public class CustomBatchGetRequest : CustomBatchRequest
    {
        public event Action OnCompleted;
        
        public List<ValueRange> ValueRanges { get; protected set; }
        private readonly Dictionary<FieldAssembler, object> requests;
        private List<string> ranges;

        public IEnumerable<string> SheetRanges => ranges.Distinct ();
        public override string Description => $"BatchGet request. Contains {requests.Count} objects, {ranges.Count} sheets total";

        public void SetResponse(ValueRange[] _response)
        {
            foreach (var r in requests)
                ClassMapping.InvokeGetCallback (r.Key.titleType, r.Value, r.Key.GetStringArray (_response));

            requests.Clear ();
            ranges.Clear ();
            OnCompleted?.Invoke ();
        }

        public CustomBatchGetRequest(string spreadsheetID, bool containsIndexedSheets)
        {
            UnityEngine.Debug.Log ($"Creating GET request. Indexing = {containsIndexedSheets}");
            SpreadsheetID = spreadsheetID;
            requests = new Dictionary<FieldAssembler, object> ();
            ranges = new List<string> ();
        }

        /// <summary> 
        /// After the response is received and read, requested objects will be sent back via callbacks. 
        /// If the request will fail, you'll see the error message, but none of callbacks will be called.
        /// </summary>
        /// <remarks> Don't keep references to enqueued request. </remarks>
        public void Add<T>(Action<T> _callback)
        {
            if (!Locked)
                requests.Add (new FieldAssembler (typeof (T)), _callback);
            else
                throw new Exception ("New requests cannot be included to enqueued batched request.");
            // the cancellation token for callbacks might be introduced. 
            // but personally I have never run in situation where it would happen to be any useful
        }

        internal void Add(Type _type, object _action)
        {
            if (!Locked)
                requests.Add (new FieldAssembler (_type), _action);
            else
                throw new Exception ("New requests cannot be included to enqueued batched request.");
        }

        private void OnSpreadsheetReceived(Spreadsheet _spreadsheet)
        {
            string[] sheetsExist = _spreadsheet.Sheets.Select (x => x.Properties.Title).ToArray ();
            StartQueue (sheetsExist);
        }

        private void StartQueue(string[] _existingSheetNames)
        {
            ranges = requests.SelectMany (rq => rq.Key
                                                  .GetSpreadsheetRanges (_existingSheetNames)
                                                  .GetValues ())
                             .Select (placement => placement.Range)
                             .Distinct ()
                             .ToList ();
            Enqueue ();
        }
    }
/*
    public class CustomGetValueRequest : CustomRequest
    {
        private object callbackObject;
        private SheetInfo sheet;

        internal static SheetInfo GetSheetInfo(Type _sheetType, string _parametrizedName, params int[] _indices)
        {
            UnityEngine.Debug.Assert (ClassNaming.IsSheet(_sheetType) || ClassNaming.IsGroup (_sheetType));
            return new SheetInfo (_sheetType, _parametrizedName, _indices);

        }

        internal static void GetValue<T>(string _spreadsheetId, SheetInfo _sheet, Action<T> _callback)
        {
            UnityEngine.Debug.Assert (_sheet.ContainsFieldOfType (typeof (T)));
            var rq = new CustomGetValueRequest ()
            {
                callbackObject = _callback,
                sheet = _sheet,
            };

            CustomBatchGetRequest getrq = new CustomBatchGetRequest (_spreadsheetId, false);
            rq.Add(_sheet.type, )
        }
    }*/
}