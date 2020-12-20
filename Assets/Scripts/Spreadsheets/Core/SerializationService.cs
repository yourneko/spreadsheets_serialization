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
    internal static class SerializationService
    {
        private const string APP_NAME = "SheetsSerialization";
        private static string[] SpreadsheetsScopes => new[] { "https://spreadsheets.google.com/feeds", "https://docs.google.com/feeds" };

        private static SheetsService service;
        private static Queue<CustomRequest> queue;
        private static readonly FailHandler failHandler;
        private static CustomRequest current;

        public static bool Active => service != null;
        public static bool Running => current != null;

        static SerializationService()
        {
            failHandler = new FailHandler ();
            failHandler.OnFailHandled += OnRequestFailed;
            ServicePointManager.ServerCertificateValidationCallback = AlwaysTrueValidator.ReturnTrue;
        }

#region Start / Stop the instance

        public static void StartService(string _jsonKeys)
        {
            Debug.Assert (!string.IsNullOrEmpty (_jsonKeys));
            Debug.Assert (service == null);

            var initializer = new BaseClientService.Initializer ()
            {
                HttpClientInitializer = GoogleCredential.FromJson (_jsonKeys)
                                                        .CreateScoped (SpreadsheetsScopes),
                ApplicationName = APP_NAME,
                GZipEnabled = Application.isEditor ||           // System.IO.Compression не робит на Android, ну или Unity компилятор хочет юморить
                              Application.platform != RuntimePlatform.Android,
            };
            service = new SheetsService (initializer);
            queue = new Queue<CustomRequest> ();
            UnityEngine.Debug.Log ("Serialization service started.");
        }

        public static void StopCurrentService()
        {
            UnityEngine.Debug.Assert (service != null);

            service.Dispose ();
            service = null;

            current = null;
            while (queue.Count > 0)
                queue.Dequeue ().Terminate ();
            queue = null;
            UnityEngine.Debug.Log ("Serialization service stopped.");
        }

#endregion
#region custom requests -> api requests

        private static SpreadsheetsResource.ValuesResource.BatchUpdateRequest AssembleUpdateRequest(CustomBatchUpdateRequest _rq)
        {
            var request = new BatchUpdateValuesRequest ()
            {
                Data = _rq.ValueRanges,
                ValueInputOption = "USER_ENTERED",
            };
            return service.Spreadsheets.Values.BatchUpdate (request, _rq.SpreadsheetID);
        }

        private static void Execute()
        {
            UnityEngine.Debug.Log ($"Executing {current.Description}");
            switch (current)
            {
                case CustomBatchGetRequest getrq:
                    ExecuteGet (getrq);
                    return;
                case CustomBatchUpdateRequest updrq:
                    ExecuteUpdateAsync (updrq);
                    return;
                case SpreadsheetRequest spreadsheetRequest:
                    ExecuteSpreadsheetGetAsync (spreadsheetRequest);
                    return;
                case AddSheetsRequest addSheetsRequest:
                    ExecuteAddSheetsAsync (addSheetsRequest);
                    return;
                default:
                    throw new NotImplementedException ();
            }
        }

        private static async void ExecuteSpreadsheetGetAsync(SpreadsheetRequest _request)
        {
            var result = await service.Spreadsheets.Get (_request.SpreadsheetID).ExecuteAsync ();
            _request.SetResponse(result);
            RequestCompleted ();
        }

        private static async void ExecuteAddSheetsAsync(AddSheetsRequest _request)
        {
            BatchUpdateSpreadsheetRequest body = new BatchUpdateSpreadsheetRequest () { Requests = new List<Request> (), };
            foreach (var s in _request.sheetNames.Select (x => new SheetProperties () { Title = x, }))
            {
                body.Requests.Add (new Request ()
                {
                    AddSheet = new AddSheetRequest () { Properties = s, }
                });
            }
            var response = await service.Spreadsheets.BatchUpdate (body, _request.SpreadsheetID).ExecuteAsync();
            _request.SetResponse (true);
            RequestCompleted ();
        }

        private async static void ExecuteGet(CustomBatchGetRequest _rq)
        {
            var ranges = _rq.SheetRanges.ToList ();
            // creating and sending a spreadsheet request
            var spreadsheetsRQ = service.Spreadsheets.Values.BatchGet (_rq.SpreadsheetID);
            spreadsheetsRQ.Ranges = ranges;
            BatchGetValuesResponse result = await spreadsheetsRQ.ExecuteAsync (); // TODO: HANDLE ERRORS

            _rq.SetResponse (result.ValueRanges.ToArray());
            RequestCompleted ();
        }

        private async static void ExecuteUpdateAsync(CustomBatchUpdateRequest _rq) 
        {
            var spreadsheetsRQ = AssembleUpdateRequest (_rq);
            var response = await spreadsheetsRQ.ExecuteAsync (); // TODO: HANDLE ERRORS
            _rq.SetResponse (response.TotalUpdatedCells ?? 0);
            RequestCompleted ();
        }

        private static void OnRequestFailed()
        {
            current.Terminate ();
            RequestCompleted ();
        }

#endregion
#region Queue

        public static void Enqueue(CustomRequest _request)
        {
            UnityEngine.Debug.Assert (Active);
            queue.Enqueue (_request);
            if (!Running)
                Run ();
        }

        private static void Run() // По одному за раз! Мы стоим - и ты стой. Тебе шо, больше всех надо? Ишь какой!
        {
            Debug.Assert (!Running);
            current = queue.Peek ();
            Execute ();
        }

        private static void RequestCompleted()
        {
            queue.Dequeue ();
            current = queue.Any () ? queue.Peek () : null;
            if (current != null)
                Execute ();
        }

#endregion
    }
}