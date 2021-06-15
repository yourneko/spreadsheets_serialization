using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;

namespace RecursiveMapper.Utility
{
    static class SpreadsheetsUtility
    {
#region Extension methods for SheetsService

        public static async Task<bool> WriteRangesAsync(this SheetsService service, string spreadsheet, IList<ValueRange> values)
        {
            var hasRequiredSheets = await service.CreateSheetsAsync (spreadsheet, values.Select (GetSheetName).ToArray ());
            if (!hasRequiredSheets)
                return false;

            var result = await service.Spreadsheets.Values
                                      .BatchUpdate (CreateBatchUpdateValueRequest(values), spreadsheet)
                                      .ExecuteAsync ();
            return result.TotalUpdatedCells > 0;
        }

        private static async Task<bool> CreateSheetsAsync(this SheetsService service, string spreadsheet, ICollection<string> requiredSheets)
        {
            if (!requiredSheets.Any ()) throw new Exception ("Writing 0 sheets");

            var existingSheets = await service.GetSheetsListAsync (spreadsheet);
            var sheetsToAdd = requiredSheets.Except (existingSheets).ToArray ();
            if (sheetsToAdd.Length == 0) return true;

            var result = await service.Spreadsheets.BatchUpdate (CreateAddSheetsRequest (sheetsToAdd), spreadsheet).ExecuteAsync ();
            return result.Replies.All(reply => reply.AddSheet.Properties != null);
        }

        public static async Task<string[]> GetSheetsListAsync(this SheetsService service, string spreadsheetID)
        {
            var spreadsheet = await service.Spreadsheets.Get (spreadsheetID).ExecuteAsync ();
            return spreadsheet.Sheets.Select (sheet => sheet.Properties.Title).ToArray ();
        }

#endregion
#region Assembling requests

        static BatchUpdateSpreadsheetRequest CreateAddSheetsRequest(ICollection<string> sheetsToCreate) =>
            new BatchUpdateSpreadsheetRequest {Requests = sheetsToCreate.Select (CreateAddSheetRequest).ToList ()};

        static SheetProperties CreateSheetProperties(string sheetName) => new SheetProperties {Title = sheetName};

        static Request CreateAddSheetRequest(string sheetNme)
        {
            var innerRequest = new AddSheetRequest {Properties = CreateSheetProperties (sheetNme)};
            return new Request {AddSheet = innerRequest};
        }

        static BatchUpdateValuesRequest CreateBatchUpdateValueRequest(IList<ValueRange> values) => new BatchUpdateValuesRequest
                                                                                                   {
                                                                                                       Data = values,
                                                                                                       ValueInputOption = "USER_ENTERED",
                                                                                                   };

#endregion

        static string GetSheetName(ValueRange range) => range.Range.Split ('!')[0].Trim (new[] {'\'', '"'});
    }
}