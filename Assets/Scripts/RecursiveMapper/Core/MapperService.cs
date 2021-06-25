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

            // its <bool> cuz i got no idea what to write there :c
            Predicate<RecursiveMap<bool>> condition = availableSheets.SpawnCondition;
            var hierarchy = condition.MapTypeHierarchyRecursive (true, new Meta (sheet, typeof(T)));

            //if (!hierarchy.All(match => match))               // todo - replace with better check
            //   return new Either<T, Exception> (new Exception ("Spreadsheet doesn't contain enough data to assemble the requested object."));

            var valueRanges = await Service.GetValueRanges (spreadsheet, ListExistingSheets (hierarchy).ToArray());
            var dictionaryValues = valueRanges.ToDictionary (range => range.Range.Split ('!')[0].Trim ('\''),
                                                             range => new ValueRangeReader (range).Read ());
            var result = UnmapObject<T> (dictionaryValues.JoinRecursive(hierarchy.Right, hierarchy.Meta));
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
            var map = MappingUtility.SerializeRecursive (obj, new Meta (sheet, typeof(T)));
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
            var ranges = data.Right.Where (e => e.Meta.ContentType == ContentType.Sheet)
                             .SelectMany (AssembleValueRanges);
            if (data.Right.Any (element => element.Meta.ContentType != ContentType.Sheet))
                ranges = ranges.Append (new ValueRangeBuilder (data).ToValueRange (FirstCellOfValueRange));
            return ranges;
        }

        static IEnumerable<string> ListExistingSheets(RecursiveMap<bool> sheetsHierarchy)
        {
            bool shouldReturn = false;
            foreach (var element in sheetsHierarchy.Right)
            {
                if (element.IsLeft)
                    shouldReturn = true;
                else
                    foreach (var sheet in ListExistingSheets (element))
                        yield return sheet;
            }

            if (shouldReturn)
                yield return sheetsHierarchy.Meta.Sheet;
        }

        static T UnmapObject<T>(RecursiveMap<string> ranges)
        {
            // recursively assemble the object from map
            throw new NotImplementedException ();
        }
    }
}