using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using UnityEngine;

namespace Mimimi.SpreadsheetsSerialization.Core
{
    public class SerializationService
    {
        private const string APP_NAME = "SheetsSerialization";
        private static readonly string[] SpreadsheetsScopes = new[] { "https://spreadsheets.google.com/feeds", "https://docs.google.com/feeds" };

        private SheetsService m_googleApiService;
        private Queue<CustomRequest> queue;
        private FailHandler failHandler;
        private CustomRequest current;

        public bool Active => m_googleApiService != null;
        public bool Running => current != null;

#region Start / Stop the instance

        public static SerializationService StartService(string _jsonKeys)
        {
            Debug.Assert (!string.IsNullOrEmpty (_jsonKeys));
            
            var initializer = new BaseClientService.Initializer ()
            {
                HttpClientInitializer = GoogleCredential.FromJson (_jsonKeys)
                                                        .CreateScoped (SpreadsheetsScopes),
                ApplicationName = APP_NAME,
                // System.IO.Compression не робит на Android, ну или Unity компилятор хочет юморить
                GZipEnabled = Application.isEditor ||           
                              Application.platform != RuntimePlatform.Android,
            };
            
            return new SerializationService ()
                   {
                       m_googleApiService = new SheetsService (initializer),
                       queue = new Queue<CustomRequest> (),
                   };
        }

        private SerializationService()
        {
            failHandler = new FailHandler ();
            failHandler.OnFailHandled += OnRequestFailed;
            ServicePointManager.ServerCertificateValidationCallback = AlwaysTrueValidator.ReturnTrue;
        }

        public void StopService()
        {
            UnityEngine.Debug.Assert (m_googleApiService != null);

            m_googleApiService.Dispose ();
            m_googleApiService = null;

            current = null;
            while (queue.Count > 0)
                queue.Dequeue ().Terminate ();
            queue = null;
            UnityEngine.Debug.Log ("Serialization service stopped.");
        }

        public CustomBatchGetRequest BatchGet(string spreadsheetID)
        {
            return new CustomBatchGetRequest (spreadsheetID, true) { Service = this };
        }

        public CustomBatchUpdateRequest BatchUpdate(string spreadsheetID)
        {
            return new CustomBatchUpdateRequest (spreadsheetID) { Service = this };
        }

#endregion
#region custom requests -> api requests

        private SpreadsheetsResource.ValuesResource.BatchUpdateRequest AssembleUpdateRequest(CustomBatchUpdateRequest _rq)
        {
            var request = new BatchUpdateValuesRequest ()
            {
                Data = _rq.ValueRanges,
                ValueInputOption = "USER_ENTERED",
            };
            return m_googleApiService.Spreadsheets.Values.BatchUpdate (request, _rq.SpreadsheetID);
        }

        private void Execute()
        {
            Debug.Log ($"Executing {current.Description}");
            switch (current)
            {
                case CustomBatchGetRequest getrq:
                    ExecuteGet (getrq);
                    return;
                case CustomBatchUpdateRequest updrq:
                    ExecuteUpdateAsync (updrq);
                    return;
                case SpreadsheetsDataRequest spreadsheetRequest:
                    ExecuteSpreadsheetGetAsync (spreadsheetRequest);
                    return;
                case AddSheetsRequest addSheetsRequest:
                    ExecuteAddSheetsAsync (addSheetsRequest);
                    return;
                default:
                    throw new NotImplementedException ();
            }
        }

        private async void ExecuteSpreadsheetGetAsync(SpreadsheetsDataRequest _request)
        {
            var result = await m_googleApiService.Spreadsheets.Get (_request.SpreadsheetID).ExecuteAsync ();
            _request.SetResponse(result);
            RequestCompleted ();
        }

        private async void ExecuteAddSheetsAsync(AddSheetsRequest _request)
        {
            BatchUpdateSpreadsheetRequest body = new BatchUpdateSpreadsheetRequest () { Requests = new List<Request> (), };
            foreach (var s in _request.sheetNames.Select (x => new SheetProperties () { Title = x, }))
            {
                body.Requests.Add (new Request ()
                {
                    AddSheet = new AddSheetRequest () { Properties = s, }
                });
            }
            var response = await m_googleApiService.Spreadsheets.BatchUpdate (body, _request.SpreadsheetID).ExecuteAsync();
            _request.SetResponse (true);
            RequestCompleted ();
        }

        private async void ExecuteGet(CustomBatchGetRequest _rq)
        {
            var ranges = _rq.SheetRanges.ToList ();
            // creating and sending a spreadsheet request
            var spreadsheetsRQ = m_googleApiService.Spreadsheets.Values.BatchGet (_rq.SpreadsheetID);
            spreadsheetsRQ.Ranges = ranges;
            BatchGetValuesResponse result = await spreadsheetsRQ.ExecuteAsync (); // TODO: HANDLE ERRORS

            _rq.SetResponse (result.ValueRanges.ToArray());
            RequestCompleted ();
        }

        private async void ExecuteUpdateAsync(CustomBatchUpdateRequest _rq) 
        {
            var spreadsheetsRQ = AssembleUpdateRequest (_rq);
            var response = await spreadsheetsRQ.ExecuteAsync (); // TODO: HANDLE ERRORS
            _rq.SetResponse (response.TotalUpdatedCells ?? 0);
            RequestCompleted ();
        }

        private void OnRequestFailed()
        {
            current.Terminate ();
            RequestCompleted ();
        }

#endregion
#region Queue

        internal void Enqueue(CustomRequest _request)
        {
            UnityEngine.Debug.Assert (Active);
            queue.Enqueue (_request);
            if (!Running)
                Run ();
        }

        private void Run() // По одному за раз! Мы стоим - и ты стой. Тебе шо, больше всех надо? Ишь какой!
        {
            Debug.Assert (!Running);
            current = queue.Peek ();
            Execute ();
        }

        private void RequestCompleted()
        {
            queue.Dequeue ();
            current = queue.Any () ? queue.Peek () : null;
            if (current != null)
                Execute ();
        }

#endregion
    }
}