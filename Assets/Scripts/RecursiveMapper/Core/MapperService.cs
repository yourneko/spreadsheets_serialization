using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;

namespace RecursiveMapper
{
    enum ContentType
    {
        /// <summary> Represents a single cell content. </summary>
        Value,

        /// <summary> Represents a MapClass instance smaller than a Sheet. May contain Value, Object, HorizontalArray and VerticalArray entities. </summary>
        Object,

        /// <summary>  Sheet may contain content of any type. </summary>
        Sheet,
    }

    public class MapperService : IDisposable
    {
        const string FirstCell = "A2";
        static string MaxCellRange(string sheet) => $"'{sheet}'!{FirstCell}:ZZ999";
        static V2Int FirstCellV2 => SpreadsheetsUtility.ReadA1 (FirstCell);

        private readonly SheetsService service;
        private readonly IValueSerializer serializer;

        /// <summary>
        /// Read the data from a spreadsheet and deserialize it to object of type T.
        /// </summary>
        /// <param name="spreadsheet">Spreadsheet ID.</param>
        /// <param name="sheet">Specify the name of the sheet, if the spreadsheet contains multiple sheets of a type.</param>
        /// <typeparam name="T">Type of object to read from the spreadsheet data.</typeparam>
        /// <returns>Object of type T.</returns>
        public async Task<T> ReadAsync<T>(string spreadsheet, string sheet = "")
            where T : new ()
        {
            var sheetsList = await service.GetSheetsListAsync (spreadsheet);
            var availableSheets = new HashSet<string> (sheetsList);
            var type = typeof(T).MapAttribute ();
            var context = new DeserializationContext
                          {
                              Serializer = serializer,
                              Requests = availableSheets.IsSupersetOf (type.RequiredSheets.Select (sheet.JoinSheetNames))
                                             ? new List<RequestedObject> {new RequestedObject (type, sheet)}
                                             : throw new Exception ()
                          };
            FindCollections (availableSheets, context.Requests, type, sheet);

            var valueRanges = await service.GetValueRanges (spreadsheet, context.Requests.SelectMany (x => x.FullNames.Select (MaxCellRange)));
            context.Values = valueRanges.ToDictionary (r => r.Range.Split ('!')[0].Trim ('\''), r => r.Values);
            return (T)context.MakeSheets (typeof(T).MapAttribute (), sheet);
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
            var valueRangeList = new List<ValueRange> ();
            MakeValueRanges (obj, typeof(T).MapAttribute (), sheet, valueRangeList);
            return service.WriteRangesAsync (spreadsheet, valueRangeList);
        }

        /// <summary>
        /// Creates a new ready-to-use instance of the serializer.
        /// </summary>
        /// <param name="initializer"> An initializer for Google Sheets service. </param>
        /// <param name="valueSerializer"> Replaces the default serialization strategy. </param>
        public MapperService(BaseClientService.Initializer initializer, IValueSerializer valueSerializer = null)
        {
            // If you got an issue with missing System.IO.Compression lib, set GZipEnabled = FALSE in your initializer.
            service    = new SheetsService (initializer ?? throw new ArgumentException ("SheetsService can't be null"));
            serializer = valueSerializer ?? new DefaultValueSerializer ();
        }

        public void Dispose()
        {
            service.Dispose ();
        }

        void MakeValueRanges(object obj, MapClassAttribute type, string parentName, ICollection<ValueRange> results)
        {
            string name = parentName.JoinSheetNames (type.SheetName);
            foreach (var field in type.SheetsFields)
            foreach ((object o, string s) in (field.Field.GetValue (obj), name).UnwrapArray (field, 1, (s, _, i) => $"{s} {i}"))
                MakeValueRanges (o, field.FrontType, s, results);

            if (type.CompactFields.Count == 0)
                return;

            IList<IList<object>> data = new List<IList<object>> ();
            WriteValueRange (data, type, obj, FirstCellV2);
            results.Add (new ValueRange
                         {
                             Values = data,
                             MajorDimension = "COLUMNS",
                             Range = $"'{name}'!{FirstCell}:{SpreadsheetsUtility.WriteA1 (new V2Int (data.Count, data.Max (x => x.Count)).Add (FirstCellV2))}",
                         });
        }

        void WriteValueRange(IList<IList<object>> values, MapClassAttribute type, object source, V2Int fromPoint)
        {
            if (type == null)
                WriteSingleValue (values, serializer.Serialize (source), fromPoint);
            else
                foreach (var field in type.CompactFields)
                foreach ((object o, V2Int p) in (field.Field.GetValue (source), fromPoint).UnwrapArray(field, 1, field.GetV2InArray))
                    WriteValueRange (values, field.FrontType, o, p);
        }

        void WriteSingleValue(IList<IList<object>> values, object value, V2Int pos)
        {
            for (int i = values.Count; i < pos.X; i++)
                values.Add (new List<object> ());
            for (int i = values[pos.X].Count; i < pos.Y; i++)
                values[pos.X].Add (null);
            values[pos.X].Add (serializer.Serialize (value));
        }

        // RequestedObject must contain all fitting sheet names, keeping their structure (multi-dimensional array)
        static void FindCollections(HashSet<string> available, List<RequestedObject> results, MapClassAttribute type, string name)
        {
            Predicate<int[]> validate = indices =>
            {
                RequestedObject result = new RequestedObject (type, name.JoinSheetNames (type.SheetName), indices);// todo - Fixed size arrays are required too
                bool comparison = available.IsSupersetOf (result.FullNames);
                if (comparison) results.Add (result);
                return comparison;
            };

            foreach (var field in type.SheetsFields.Where (f => f.Rank == 0)) // todo - still haven't adapted RequestedObject to changes
                FindCollections (available, results, field.FrontType, name.JoinSheetNames (type.SheetName));
            foreach (var field in type.SheetsFields.Where (f => f.Rank > 0))
                validate.FindValidArrayIndices (field.Rank);
        }
    }
}