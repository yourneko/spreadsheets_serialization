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
    public class MapperService : IDisposable
    {
        public const string FirstCell = "A2";

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

            var requests = availableSheets.IsSupersetOf (type.RequiredSheets.Select (sheet.JoinSheetNames))
                               ? new List<RequestedObject> {new RequestedObject (type, sheet)}
                               : throw new Exception ();
            FindCollections (availableSheets, type, sheet, requests);

            var valueRanges = await service.GetValueRanges (spreadsheet, requests.SelectMany (x => x.FullNames.Select (MaxCellRange)));
            return (T)AssembleSheet (type, sheet, requests, valueRanges.ToDictionary (r => r.Range.Split ('!')[0].Trim ('\''), r => r.Values));
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
            service = new SheetsService (initializer
                                      ?? throw new ArgumentException ("SheetsService can't be null"));
            serializer = valueSerializer ?? new DefaultValueSerializer ();
        }

        public void Dispose()
        {
            service.Dispose ();
        }

#region Write

        void ToRanges(object obj, MappedClassAttribute type, string parentName, ICollection<ValueRange> results)
        {
            string name = parentName.JoinSheetNames (type.SheetName);
            foreach (var field in type.SheetsFields)
                foreach (var pair in field.Field.GetValue (obj).ToCollection (name, field.DimensionCount))
                    ToRanges (pair.Value, field.FrontType, pair.Key, results);

            if (type.CompactFields.Count == 0)
                return;

            (int x1, int y1) = SpreadsheetsUtility.ReadA1 (FirstCell);
            var context = new ArrayAssemblingContext ();
            var size = DoCollection (context, obj, type, 0);
            context.Values[0][0] = context.SB.ToString ();
            results.Add (new ValueRange
                         {
                             Values         = context.Values,
                             MajorDimension = "COLUMNS",
                             Range          = $"'{name}'!{FirstCell}:{SpreadsheetsUtility.WriteA1 (size.X2 + x1, size.Y2 + y1)}",
                         });
        }

        MapRegion DoCollection(ArrayAssemblingContext context, object obj, MappedClassAttribute type, int repeats, bool vertical = true)
        {
            if (repeats == 0 && type is null)
                return WriteValue (context, obj);

            var region = new MapRegion {X1 = context.X, Y1 = context.Y};
            context.SB.Append (vertical ? '[' : '(');

            var cc = repeats == 0
                         ? type.CompactFields.Select (e => DoCollection (context, e.Field.GetValue (obj), e.FrontType, e.DimensionCount))
                         : obj is ICollection c
                             ? c.Cast<object> ().Select (e => DoCollection (context, e, type, repeats - 1, repeats == 0 || !vertical))
                             : throw new Exception ();
            foreach (var result in cc)
            {
                region.Add (result);
                context.X = vertical ? region.X1 : region.X2 + 1;
                context.X = vertical ? region.Y2 + 1 : region.Y1;
            }
            context.SB.Append ('.');
            return region;
        }

        MapRegion WriteValue(ArrayAssemblingContext context, object value)
        {
            context.SB.Append ('*');
            for (int i = context.Values.Count; i < context.X + 1; i++)
                context.Values.Add(new List<object> ());
            for (int i = context.Values[context.X].Count; i < context.Y; i++)
                context.Values[context.X].Add(null);
            context.Values[context.X][context.Y] = serializer.Serialize (value);
            return new MapRegion {X2 = context.X, Y2 = context.Y};
        }

#endregion
#region Read: making the request

        static string MaxCellRange(string sheet) => $"'{sheet}'!{FirstCell}:ZZ999";
        void FindCollections(HashSet<string> available, MappedClassAttribute type, string name, List<RequestedObject> results)
        {
            var thisName = name.JoinSheetNames (type.SheetName);
            foreach (var field in type.SheetsFields.Where (x => x.DimensionCount == 0))
                FindCollections (available, field.FrontType, thisName, results);

            var arrays = type.SheetsFields.Where (x => x.DimensionCount == 0).ToArray();
            results.AddRange (arrays.SelectMany (f => GetValidIndices (available, f.FrontType, thisName, f.DimensionCount)));
        }

        IEnumerable<RequestedObject> GetValidIndices(HashSet<string> available, MappedClassAttribute type, string name, int dimensions)
        {
            var indices = Enumerable.Repeat(1, dimensions).ToArray(); // start indexing with 1, not 0
            int index = 0;

            while (index >= 0)
            {
                RequestedObject result = new RequestedObject(type, name, indices);
                if (available.IsSupersetOf (result.RequestedSheets))
                {
                    yield return result;
                    index          =  dimensions - 1;
                    indices[index] += 1;
                }
                else
                {
                    indices[index - 1] += 1;
                    indices[index--]   =  1;
                }
            }
        }

#endregion
#region Read: assembling the object from ValueRange[]

        object GroupSheets(List<RequestedObject> allRq, Dictionary<string, IList<IList<object>>> values, RequestedObject[] requests, IReadOnlyList<Type> types)
        {
            if (types.Count == 1)
                return AssembleSheet (requests[0].Type, requests[0].ParentName, allRq, values); // i expect to have exactly 1 request in this case

            var nextStepTypes = types.Skip (1).ToArray ();
            int indexNumber = requests[0].Index.Length - nextStepTypes.Length;
            var result = Activator.CreateInstance (types[0]);
            result.AddContent (nextStepTypes[0],
                               requests.GroupBy (x => x.Index[indexNumber]).OrderBy (x => x.Key)
                                       .Select (x => GroupSheets (allRq, values, x.ToArray (), nextStepTypes)));
            return result;
        }

        object AssembleSheet(MappedClassAttribute type, string name, List<RequestedObject> requests, Dictionary<string, IList<IList<object>>> values)
        {
            var thisName = name.JoinSheetNames (type.SheetName);
            var result = Activator.CreateInstance (type.Type);
            foreach (var f in type.SheetsFields)
                f.Field.SetValue (result, f.DimensionCount == 0
                                              ? AssembleSheet (f.FrontType, thisName, requests, values)
                                              : GroupSheets (requests, values, requests.Where (x => x.Type.Equals (f.FrontType)).ToArray (), f.ArrayTypes));
            if (type.CompactFields.Count > 0)
                DeserializeValueRange (result, values[thisName], type);
            return result;
        }

        void DeserializeValueRange(object parent, IList<IList<object>> values, MappedClassAttribute type)/* position? context? */
        {
            // first 3 - method1, last 2 - method2

            // SHEETS
                // sheet array  >> AssembleSheetGroup
                // sheet        >> AssembleSheet
            // COMPACT
                // object/array >> move pointer (or split string?)
                // value        >> read
                // todo -  do shit
        }

#endregion
    }
}