using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Apis.Http;
using Google.Apis.Requests;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util;

namespace RecursiveMapper
{
    // Assembling and sending requests to Google Spreadsheets API
    static class SpreadsheetsUtility
    {
        const int A1_LETTERS_COUNT = 26;

        static ClientServiceRequest<T> AddBackOffHandler<T>(this ClientServiceRequest<T> request, BackOffHandler handler = null)
        {
            request.AddExceptionHandler (handler ?? new BackOffHandler (new ExponentialBackOff (TimeSpan.FromMilliseconds(250), 5)));
            return request;
        }

#region Sending async Google Sheets API Requests to SheetsService

        public static async Task<bool> WriteRangesAsync(this SheetsService service, string spreadsheet, IList<ValueRange> values)
        {
            var hashset = new HashSet<string> (values.Select (range => range.Range.Split ('!')[0].Trim ('\'')));
            var hasRequiredSheets = await service.CreateSheetsAsync (spreadsheet, hashset);
            if (!hasRequiredSheets)
                return false;

            var result = await service.Spreadsheets.Values.BatchUpdate (UpdateRequest (values), spreadsheet).AddBackOffHandler().ExecuteAsync ();
            return result.TotalUpdatedCells > 0;
        }

        public static async Task<string[]> GetSheetsListAsync(this SheetsService service, string spreadsheetID)
        {
            var spreadsheet = await service.Spreadsheets.Get (spreadsheetID).AddBackOffHandler ().ExecuteAsync ();
            return spreadsheet.Sheets.Select (sheet => sheet.Properties.Title).ToArray ();
        }

        public static async Task<IList<ValueRange>> GetValueRanges(this SheetsService service, string spreadsheet, IEnumerable<string> ranges)
        {
            var request = service.Spreadsheets.Values.BatchGet (spreadsheet);
            request.Ranges         = ranges.ToArray();
            request.MajorDimension = SpreadsheetsResource.ValuesResource.BatchGetRequest.MajorDimensionEnum.COLUMNS;
            var result = await request.AddBackOffHandler().ExecuteAsync ();
            return result.ValueRanges;
        }

        static async Task<bool> CreateSheetsAsync(this SheetsService service, string spreadsheet, HashSet<string> requiredSheets)
        {
            var existingSheets = await service.GetSheetsListAsync (spreadsheet);
            if ((existingSheets = requiredSheets.Except (existingSheets).ToArray ()).Length == 0)
                return true;
            var result = await service.Spreadsheets.BatchUpdate (CreateAddSheetsRequest (existingSheets), spreadsheet).AddBackOffHandler().ExecuteAsync ();
            return result.Replies.All(reply => reply.AddSheet.Properties != null);
        }

#endregion
#region Making instances of Google Sheets API Requests

        static BatchUpdateSpreadsheetRequest CreateAddSheetsRequest(ICollection<string> sheetsToCreate) =>
            new BatchUpdateSpreadsheetRequest {Requests = sheetsToCreate.Select (CreateAddSheetRequest).ToList ()};

        static Request CreateAddSheetRequest(string sheetName)
        {
            var innerRequest = new AddSheetRequest {Properties = new SheetProperties {Title = sheetName}};
            return new Request {AddSheet = innerRequest};
        }

        static BatchUpdateValuesRequest UpdateRequest(IList<ValueRange> data) => new BatchUpdateValuesRequest { Data = data, ValueInputOption = "USER_ENTERED" };

#endregion
#region Google Spreadsheets A1 notation

        // IMPORTANT! Indices 'x' and 'y' are counted from 0. The point (0,0) corresponds to A1 cell
        public static V2Int ReadA1(string a1) => new V2Int (Evaluate (a1.Where (char.IsLetter).Select (char.ToUpperInvariant), '@', A1_LETTERS_COUNT),
                                                          Evaluate (a1.Where (char.IsDigit), '0', 10));

        public static string WriteA1(V2Int a1) => new string(ToLetters (a1.X).ToArray()) + (a1.Y + 1);

        static IEnumerable<char> ToLetters(int number) => number < A1_LETTERS_COUNT
                                                              ? new[]{(char)('A' + number)}
                                                              : ToLetters (number / A1_LETTERS_COUNT - 1).Append ((char)('A' + number % A1_LETTERS_COUNT));

        static int Evaluate(IEnumerable<char> digits, char zero, int @base)
        {
            int result = (int)digits.Reverse ().Select ((c, i) => (c - zero) * Math.Pow (@base, i)).Sum ();
            return result-- > 0 ? result : 999; // In Google Spreadsheets notation, upper boundary of the range may be missing - it means 'up to a big number'
        }
#endregion
    }
}