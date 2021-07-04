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

            var requests = availableSheets.IsSupersetOf (type.RequiredSheets.Select (sheet.JoinSheetNames))
                               ? new List<RequestedObject> {new RequestedObject (type, sheet)}
                               : throw new Exception ();
            FindCollections (availableSheets, requests, type, sheet);

            var valueRanges = await service.GetValueRanges (spreadsheet, requests.SelectMany (x => x.FullNames.Select (MaxCellRange)));
            // todo - cast all RequestedObject to Lazy<object>. then, assemble
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
            service    = new SheetsService (initializer ?? throw new ArgumentException ("SheetsService can't be null"));
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
            foreach ((string s, object o) in field.Field.GetValue (obj).ToCollection (name, field.Rank))
                ToRanges (o, field.FrontType, s, results);

            if (type.CompactFields.Count == 0)
                return;

            var context = new ValueRangeAssembleContext ();
            var size     = DoRange (context, obj, type, 0, new V2Int(1, 0), false);
            context.Values[0][0] = context.Sb.ToString ();
            results.Add (new ValueRange
                         {
                             Values         = context.Values,
                             MajorDimension = "COLUMNS",
                             Range          = $"'{name}'!{FirstCell}:{SpreadsheetsUtility.WriteA1 (size.Add(SpreadsheetsUtility.ReadA1 (FirstCell)))}",
                         });
        }

        V2Int DoRange(ValueRangeAssembleContext ctx, object obj, MappedClassAttribute type, int i, V2Int pos, bool vertical)
        {
            if (i == 0 && type is null)
                return WriteValue (ctx, obj, pos);

            ctx.Sb.Append (vertical ? '[' : '(');
            var v = new V2Int (-1, -1);
            v = i == 0
                    ? type.CompactFields.Aggregate (v, (v2, f) => v2.Join (DoRange (ctx, f.Field.GetValue (obj), f.FrontType, f.Rank, pos.Next (v2, vertical), f.Rank > 0)))
                    : obj is ICollection cc
                        ? cc.Cast<object> ().Aggregate (v, (v2, o) => v2.Join (DoRange (ctx, o, type, i - 1, pos.Next (v2, vertical), !vertical)))
                        : throw new Exception ();
            ctx.Sb.Append ('.');
            return v;
        }

        V2Int WriteValue(ValueRangeAssembleContext context, object value, V2Int pos)
        {
            context.Sb.Append ('*');
            if (context.Values.Count == pos.X)
                context.Values.Add (new List<object> ());
            for (int i = context.Values[pos.X].Count; i < pos.Y; i++)
                context.Values[pos.X].Add (null);
            context.Values[pos.X].Add (serializer.Serialize (value));
            return pos;
        }

#endregion
#region Read

        static void FindCollections(HashSet<string> available, List<RequestedObject> results, MappedClassAttribute type, string name)
        {
            Predicate<int[]> validate = indices =>
            {
                RequestedObject result = new RequestedObject (type, name.JoinSheetNames (type.SheetName), indices);
                bool comparison = available.IsSupersetOf (result.FullNames);
                if (comparison) results.Add (result);
                return comparison;
            };

            foreach (var field in type.SheetsFields.Where (f => f.Rank == 0))
                FindCollections (available, results, field.FrontType, name.JoinSheetNames (type.SheetName));
            foreach (var field in type.SheetsFields.Where (f => f.Rank > 0))
                validate.FindValidArrayIndices (field.Rank);
        }

        //  todo - (lazy) assemble each RequestedObject before making a group.
        object GroupSheets(List<RequestedObject> allRq, Dictionary<string, IList<IList<object>>> values, RequestedObject[] requests, IReadOnlyList<Type> types)
        {
            if (types.Count == 1) // todo - it seems kinda unnecessary, if sheets  are assembled before
                return AssembleSheet (requests[0].Type, requests[0].ParentName, allRq, values); // i expect to have exactly 1 request in this case

            var nextStepTypes = types.Skip (1).ToArray ();
            int indexNumber = requests[0].Index.Length - nextStepTypes.Length;
            var result = Activator.CreateInstance (types[0]);
            var addContentAction = result.AddContent (nextStepTypes[0]);
            foreach (var request in requests.GroupBy (x => x.Index[indexNumber]).OrderBy (x => x.Key)
                                            .Select (x => GroupSheets (allRq, values, x.ToArray (), nextStepTypes)))
                addContentAction.Invoke (request);
            return result;
        }

        object AssembleSheet(MappedClassAttribute type, string name, List<RequestedObject> requests, Dictionary<string, IList<IList<object>>> values)
        {
            var thisName = name.JoinSheetNames (type.SheetName);
            var result = Activator.CreateInstance (type.Type);
            foreach (var f in type.SheetsFields)
                f.Field.SetValue (result, f.Rank == 0
                                              ? AssembleSheet (f.FrontType, thisName, requests, values)
                                              : GroupSheets (requests, values, requests.Where (x => x.Type.Equals (f.FrontType)).ToArray (), f.ArrayTypes));
            if (type.CompactFields.Count == 0)
                return result;

            var sheet = values[type.SheetName];
            string S(V2Int pos) => (string)sheet[pos.X][pos.Y];
            object GetDeserializedValue(ReadContext b) => serializer.Deserialize (b.Field.FrontType.Type, S (b.First.Next (b.Last, !b.Horizontal)));
            ((string)sheet[0][0]).Aggregate (new ReadContext (i => type.CompactFields[i], i => type.CompactFields[i].ArrayTypes[0])
                                             {IsObject = true, First = new V2Int (1, 0), Horizontal = true, TargetObject = result},
                                             (b, c) => c switch
                                                       {
                                                           '.' => FinishObject (b),
                                                           '*' => ReadValue (GetDeserializedValue(b), b),
                                                           '(' => CreateObject (true, b),
                                                           '[' => CreateObject (false, b),
                                                           _   => b
                                                       });
            return result;
        }

        static ReadContext FinishObject(ReadContext b)
        {
            b.Back.Last = b.Back.Last.Join (b.Last);
           return b.Back;
        }

        static ReadContext ReadValue(object value, ReadContext b)
        {
            (b.IsObject ? b.addToObj : b.addToArray).Invoke (value);
            b.Last = b.Last.Join (b.First.Next (b.Last, !b.Horizontal));
            return b;
        }

        static ReadContext CreateObject(bool horizontal, ReadContext b) => new ReadContext
                                                                           {
                                                                               IsObject     = b.IsObject
                                                                                                  ? b.getNextField (b.Index).Rank == 0
                                                                                                  : b.Field.Rank == (b.Depth + 1),
                                                                               Field        = b.IsObject ? b.getNextField (b.Index) : b.Field,
                                                                               Depth        = b.IsObject ? 0 : b.Depth + 1,
                                                                               TargetObject = b.NextObject (),
                                                                               Back         = b,
                                                                               First        = b.First.Next (b.Last, !b.Horizontal),
                                                                               Horizontal   = horizontal,
                                                                           };
#endregion
    }
}