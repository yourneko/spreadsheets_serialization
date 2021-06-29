using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;

namespace RecursiveMapper
{
    // Assembling and sending requests to Google Spreadsheets API
    static class SpreadsheetsUtility
    {
        const int A1_LETTERS_COUNT = 26;

#region Sending async Google Sheets API Requests to SheetsService

        public static async Task<bool> WriteRangesAsync(this SheetsService service, string spreadsheet, IList<ValueRange> values)
        {
            var sheets = values.Select (range => range.Range.Split ('!')[0].Trim ('\''));
            var hasRequiredSheets = await service.CreateSheetsAsync (spreadsheet, sheets.ToArray ());
            if (!hasRequiredSheets)
                return false;

            var result = await service.Spreadsheets.Values
                                      .BatchUpdate (CreateBatchUpdateValueRequest(values), spreadsheet)
                                      .ExecuteAsync ();
            return result.TotalUpdatedCells > 0;
        }

        public static async Task<string[]> GetSheetsListAsync(this SheetsService service, string spreadsheetID)
        {
            var spreadsheet = await service.Spreadsheets.Get (spreadsheetID).ExecuteAsync ();
            return spreadsheet.Sheets.Select (sheet => sheet.Properties.Title).ToArray ();
        }

        public static async Task<IList<ValueRange>> GetValueRanges(this SheetsService service, string spreadsheet, string[] ranges)
        {
            var request = service.Spreadsheets.Values.BatchGet (spreadsheet);
            request.Ranges = ranges;
            var result = await request.ExecuteAsync ();
            return result.ValueRanges;
        }

        static async Task<bool> CreateSheetsAsync(this SheetsService service, string spreadsheet, ICollection<string> requiredSheets)
        {
            if (!requiredSheets.Any ()) throw new Exception ("Writing 0 sheets");

            var existingSheets = await service.GetSheetsListAsync (spreadsheet);
            var sheetsToAdd = requiredSheets.Except (existingSheets).ToArray ();
            if (sheetsToAdd.Length == 0) return true;

            var result = await service.Spreadsheets.BatchUpdate (CreateAddSheetsRequest (sheetsToAdd), spreadsheet).ExecuteAsync ();
            return result.Replies.All(reply => reply.AddSheet.Properties != null);
        }

#endregion
#region Making instances of Google Sheets API Requests

        static BatchUpdateValuesRequest CreateBatchUpdateValueRequest(IList<ValueRange> values) => new BatchUpdateValuesRequest {
                                                                                                                                    Data = values,
                                                                                                                                    ValueInputOption = "USER_ENTERED",
                                                                                                                                };

        static BatchUpdateSpreadsheetRequest CreateAddSheetsRequest(ICollection<string> sheetsToCreate) =>
            new BatchUpdateSpreadsheetRequest {Requests = sheetsToCreate.Select (CreateAddSheetRequest).ToList ()};

        static Request CreateAddSheetRequest(string sheetNme)
        {
            var innerRequest = new AddSheetRequest {Properties = CreateSheetProperties (sheetNme)};
            return new Request {AddSheet = innerRequest};
        }

        static SheetProperties CreateSheetProperties(string sheetName) => new SheetProperties {Title = sheetName};

#endregion
#region Google Spreadsheets A1 notation

        // IMPORTANT! Indices 'x' and 'y' are counted from 0. The point (0,0) corresponds to A1 cell
        public static (int x, int y) ReadA1(string a1) => (Evaluate (a1.Where (char.IsLetter).Select (char.ToUpperInvariant), '@', A1_LETTERS_COUNT),
                                                            Evaluate (a1.Where (char.IsDigit), '0', 10));

        public static string WriteA1(int x, int y) => new string(ToLetters (x).ToArray()) + (y + 1);

        static IEnumerable<char> ToLetters(int number) => number < A1_LETTERS_COUNT
                                                              ? new[]{(char)('A' + number)}
                                                              : ToLetters (number / A1_LETTERS_COUNT - 1).Append ((char)('A' + number % A1_LETTERS_COUNT));

        static int Evaluate(IEnumerable<char> digits, char zero, int @base)
        {
            int result = (int)digits.Reverse ().Select ((c, i) => (c - zero) * Math.Pow (@base, i)).Sum ();
            return result > 0
                       ? result - 1
                       : 999; // In Google Spreadsheets notation, upper boundary of the range may be missing - it means 'up to a big number'
        }

#endregion
    }
}