using System;
using Google.Apis.Sheets.v4.Data;

namespace Mimimi.SpreadsheetsSerialization.Core
{
    public class SpreadsheetRequest : CustomRequest
    {
        private Action<Spreadsheet> callback;

        public static void Send(string _spreadsheetID, Action<Spreadsheet> _callback)
        {
            var rq = new SpreadsheetRequest ()
            {
                SpreadsheetID = _spreadsheetID,
                callback = _callback,
            };
            SerializationService.Enqueue (rq);
        }

        private SpreadsheetRequest () { }

        public void SetResponse(Spreadsheet _spreadsheet)
        {
            UnityEngine.Debug.Log ("Requested spreadsheet received.");
            callback.Invoke (_spreadsheet);
        }

        public override void Terminate()
        {
            UnityEngine.Debug.LogError ("Spreadsheet request was cancelled due to error.");
            callback.Invoke (null);
        }
    }
}