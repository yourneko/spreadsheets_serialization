using System;
using Google.Apis.Sheets.v4.Data;

namespace Mimimi.SpreadsheetsSerialization.Core
{
    class SpreadsheetsDataRequest : CustomRequest
    {
        private Action<Spreadsheet> callback;

        public static SpreadsheetsDataRequest Create(string _spreadsheetID, Action<Spreadsheet> _callback)
        {
            return new SpreadsheetsDataRequest ()
            {
                SpreadsheetID = _spreadsheetID,
                callback = _callback,
            };
        }

        private SpreadsheetsDataRequest () { }

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