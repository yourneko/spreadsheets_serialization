using System;
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

            var request = Service.Spreadsheets.Values.BatchGet (spreadsheet);
            request.Ranges = GetRangesToRead (typeof(T), sheet, availableSheets);
            var result = await request.ExecuteAsync ();

            return AssembleObjectFromValueRanges<T> (result.ValueRanges.ToArray (), sheet);
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
            return Service.WriteRangesAsync (spreadsheet,
                                             AssembleValueRanges (SerializeToMap (obj), sheet));
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

        static RecursiveMap<string> SerializeToMap(object mapped)
        {
            var type = mapped.GetType ();
            return ReflectionUtility.IsSerializedToValue (type)
                       ? new RecursiveMap<string> (mapped.ToString (), // todo - add more relevant serialization
                                                   DimensionInfo.Point)
                       : MappingUtility.GetMappedFields (type)
                                       .Apply (mapped.GetExpandedValue).Simplify ()
                                       .Apply (SerializeToMap).Simplify ();
        }

        ValueRange[] AssembleValueRanges(RecursiveMap<string> data, string sheetName)
        {
            // 1. start from a single Sheet object provided by user
            // 2. if has Object/Value members, include a name of the sheet to the list
            // 3. add indexed sheets for every Sheet Array element contained
            // 4. for contained Sheet members, repeat from (1)
            //
            // compare the assembled list of sheets with the one from (0) spreadsheet. make a Create request for every missing sheet
            // Await Create sheet requests
            // assemble content of each sheet
            // count sizes of content, set ranges
            // Await Update request
            throw new NotImplementedException ();
        }

        string[] GetRangesToRead(Type type, string sheet, string[] availableSheets)
        {
            // 1. start from single sheet
            // 2. if has Object/Value members, include a name of the sheet
            // 3. for Sheet Array members, include as many elements as possible (indices start from 1), 0 is valid amount   
            // 4. for each Sheet member, add sheet name to its name and repeat from (1)
            //
            // Now we have a list of Sheet objects, and required sheets for each of them
            // ** It is possible to limit requested range for some sheets, by looking at Object/Value Array directions in each of those 
            // ** Array size is not limited, so requested range will have no upper bound in the array extension direction
            // Calculate the exact ranges, no non-existing sheets allowed.
            //
            // Await requested ValueRange's from the service. 
            // Return the content of ranges to a list of Sheet objects, and assemble them.
            // Return the requested T object to a user.

            throw new NotImplementedException ();
            //  this thing should probably return a recursive map
        }

        Either<T, Exception> AssembleObjectFromValueRanges<T>(ValueRange[] ranges, string sheet)
        {
            throw new NotImplementedException ();
        }
    }
}