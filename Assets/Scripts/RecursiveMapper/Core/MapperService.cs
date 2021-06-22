using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using RecursiveMapper.Utility;

namespace RecursiveMapper
{
    public class MapperService : IDisposable
    {
        const string FirstCellOfValueRange = "A2";

        private SheetsService Service { get; }

        /// <summary>
        /// Read the data from a spreadsheet and deserialize it to object of type T.
        /// </summary>
        /// <param name="spreadsheet">Spreadsheet ID.</param>
        /// <param name="sheet">Specify the name of the sheet, if the spreadsheet contains multiple sheets of a type.</param>
        /// <typeparam name="T">Type of object to read from the spreadsheet data.</typeparam>
        /// <returns>Object of type T.</returns>
        public async Task<Either<T, Exception>> ReadAsync<T>(string spreadsheet, string sheet = "")
        {
            var availableSheets = await Service.GetSheetsListAsync (spreadsheet);
            var hierarchy = availableSheets.MapSheetsHierarchy (typeof(T), sheet.CreateDimensionInfo (typeof(T)));

            if (!hierarchy.All(match => match))               // todo - replace with better check
                return new Either<T, Exception> (new Exception ("Spreadsheet doesn't contain enough data to assemble the requested object."));

            var valueRanges = await Service.GetValueRanges (spreadsheet, ListExistingSheets (hierarchy));
            var result = UnmapObject<T> (hierarchy.AssembleMap (valueRanges.Select (range => range.ToMap ())));
            return new Either<T, Exception>(result);
        }

        /// <summary>
        /// Write a serialized object of type T to the spreadsheet.
        /// </summary>
        /// <param name="obj">Object to serialize.</param>
        /// <param name="spreadsheet">Spreadsheet ID.</param>
        /// <param name="sheet">Specify the name of the sheet, if the spreadsheet contains multiple sheets of a type.</param>
        /// <typeparam name="T">Type of serialized object.</typeparam>
        public Task<bool> WriteAsync<T>(T obj, string spreadsheet, string sheet = "")
        {
            var map = MappingUtility.Serialize (obj, sheet.CreateDimensionInfo (typeof(T)));
            return Service.WriteRangesAsync (spreadsheet,  AssembleValueRanges (map).ToList ());
        }

        /// <summary>
        /// Creates a new ready-to-use instance of the serializer.
        /// </summary>
        /// <param name="initializer"> An initializer for Google Sheets service. </param>
        public MapperService(BaseClientService.Initializer initializer)
        {
            // If you got an issue with missing System.IO.Compression lib, set GZipEnabled = FALSE in your initializer.
            Service = new SheetsService (initializer
                                      ?? throw new ArgumentException ("SheetsService can't be null"));
        }

        public void Dispose()
        {
            Service.Dispose ();
        }

        static IEnumerable<ValueRange> AssembleValueRanges(RecursiveMap<string> data)
        {
            var ranges = data.Right.Where (e => !e.DimensionInfo.IsCompact)
                             .SelectMany (AssembleValueRanges);
            return (data.CreateValueRange (FirstCellOfValueRange, out var range))
                       ? ranges.Append (range)
                       : ranges;
        }

        static string[] ListExistingSheets(RecursiveMap<bool> sheetsHierarchy)
        {
            // from a built hierarchy, get the list of value ranges to GET from the spreadsheets
            throw new NotImplementedException ();
        }

        static T UnmapObject<T>(RecursiveMap<string> ranges)
        {
            // recursively assemble the object from map
            throw new NotImplementedException ();
        }
    }
}