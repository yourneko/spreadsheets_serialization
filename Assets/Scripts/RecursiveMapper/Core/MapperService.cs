using System;
using System.Collections;
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
            ToRanges (obj, typeof(T).MapAttribute (), sheet, valueRangeList);
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

        void ToRanges(object obj, MapClassAttribute type, string parentName, ICollection<ValueRange> results)
        {
            string name = parentName.JoinSheetNames (type.SheetName);
            foreach (var field in type.SheetsFields)
            foreach ((string s, object o) in field.Field.GetValue (obj).ToCollection (name, field.Rank))
                ToRanges (o, field.FrontType, s, results);

            if (type.CompactFields.Count == 0)
                return;

            IList<IList<object>> values = new List<IList<object>> ();
            var size = DoRange (values, obj, type, 0, new V2Int (0, 0), false);
            results.Add (new ValueRange
                         {
                             Values         = values,
                             MajorDimension = "COLUMNS",
                             Range          = $"'{name}'!{FirstCell}:{SpreadsheetsUtility.WriteA1 (size.Add (SpreadsheetsUtility.ReadA1 (FirstCell)))}",
                         });
        }

        V2Int DoRange(IList<IList<object>> values, object obj, MapClassAttribute type, int i, V2Int pos, bool vertical)
        {
            if (i == 0 && type is null)
                return WriteValue (values, obj, pos);

            var v = new V2Int (-1, -1); // todo - rewrite, as for now i know all positions in the world
            v = i == 0
                    ? type.CompactFields.Aggregate (
                        v, (v2, f) => v2.Join (DoRange (values, f.Field.GetValue (obj), f.FrontType, f.Rank, pos.Next (v2, vertical), f.Rank > 0)))
                    : obj is ICollection cc
                        ? cc.Cast<object> ().Aggregate (v, (v2, o) => v2.Join (DoRange (values, o, type, i - 1, pos.Next (v2, vertical), !vertical)))
                        : throw new Exception ();
            return v;
        }

        V2Int WriteValue(IList<IList<object>> values, object value, V2Int pos)
        {
            if (values.Count == pos.X)
                values.Add (new List<object> ());
            for (int i = values[pos.X].Count; i < pos.Y; i++)
                values[pos.X].Add (null);
            values[pos.X].Add (serializer.Serialize (value));
            return pos;
        }

        // RequestedObject must contain all fitting sheet names, keeping their structure (multi-dimensional array)
        static void FindCollections(HashSet<string> available, List<RequestedObject> results, MapClassAttribute type, string name)
        {
            Predicate<int[]> validate = indices =>
            {
                RequestedObject result = new RequestedObject (type, name.JoinSheetNames (type.SheetName), indices);
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