using System;

namespace Mimimi.SpreadsheetsSerialization.Core
{
    public class AddSheetsRequest : CustomRequest
    {
        private Action<bool> callback;
        public string[] sheetNames;

        private AddSheetsRequest () { }

        public override void Terminate()
        {
            callback.Invoke (false);
        }

        public void SetResponse(bool _result)
        {
            if (_result)
                UnityEngine.Debug.Log ($"New sheets created: {string.Join (" ", sheetNames)}");
            callback.Invoke (_result);
        }

        public static void Send(string _spreadsheetID, Action<bool> _callback, params string[] _sheetNames)
        {
            var rq = new AddSheetsRequest
            {
                callback = _callback,
                sheetNames = _sheetNames,
                SpreadsheetID = _spreadsheetID
            };
            SerializationService.Enqueue (rq);
        }
    }
}