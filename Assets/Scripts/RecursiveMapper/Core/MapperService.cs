using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            foreach ((string s, object o) in field.Field.GetValue (obj).ToCollection (name, field.DimensionCount))
                ToRanges (o, field.FrontType, s, results);

            if (type.CompactFields.Count == 0)
                return;

            var context = new ValueRangeAssembleContext ();
            var size     = DoCollection (context, obj, type, 0);
            context.Values[0][0] = context.Sb.ToString ();
            results.Add (new ValueRange
                         {
                             Values         = context.Values,
                             MajorDimension = "COLUMNS",
                             Range          = $"'{name}'!{FirstCell}:{SpreadsheetsUtility.WriteA1 (size.Add(SpreadsheetsUtility.ReadA1 (FirstCell)))}",
                         });
        }

        Pair DoCollection(ValueRangeAssembleContext context, object obj, MappedClassAttribute type, int repeats, bool vertical = true)
        {
            if (repeats == 0 && type is null)
                return WriteValue (context, obj);

            var first = context.Pos;
            var last = new Pair (-1, -1);
            context.Sb.Append (vertical ? '[' : '(');         // todo - possibly can remove Pair from context

            var collection = repeats == 0
                                 ? type.CompactFields.Select (e => DoCollection (context, e.Field.GetValue (obj), e.FrontType, e.DimensionCount))
                                 : obj is ICollection c
                                     ? c.Cast<object> ().Select (e => DoCollection (context, e, type, repeats - 1, repeats == 0 || !vertical))
                                     : throw new Exception ();
            foreach (var pair in collection)
                context.Pos = Pair.NextPosition (first, (last = last.Join (pair)), vertical);
            context.Sb.Append ('.');
            return last;
        }

        Pair WriteValue(ValueRangeAssembleContext context, object value)
        {
            context.Sb.Append ('*');
            if (context.Values.Count == context.Pos.X)
                context.Values.Add (new List<object> ());
            for (int i = context.Values[context.Pos.X].Count; i < context.Pos.Y; i++)
                context.Values[context.Pos.X].Add (null);
            context.Values[context.Pos.X].Add (serializer.Serialize (value));
            return context.Pos;
        }

#endregion
#region Read

        void FindCollections(HashSet<string> available, List<RequestedObject> results, MappedClassAttribute type, string name)
        {
            Predicate<int[]> validate = indices =>
            {
                RequestedObject result = new RequestedObject (type, name.JoinSheetNames (type.SheetName), indices);
                bool comparison = available.IsSupersetOf (result.FullNames);
                if (comparison) results.Add (result);
                return comparison;
            };

            foreach (var field in type.SheetsFields.Where (f => f.DimensionCount == 0))
                FindCollections (available, results, field.FrontType, name.JoinSheetNames (type.SheetName));
            foreach (var field in type.SheetsFields.Where (f => f.DimensionCount > 0))
                validate.FindValidArrayIndices (field.DimensionCount);
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
                f.Field.SetValue (result, f.DimensionCount == 0
                                              ? AssembleSheet (f.FrontType, thisName, requests, values)
                                              : GroupSheets (requests, values, requests.Where (x => x.Type.Equals (f.FrontType)).ToArray (), f.ArrayTypes));
            if (type.CompactFields.Count == 0)
                return result;

            var sheet = values[type.SheetName];
            string S(Pair pos) => (string)sheet[pos.X][pos.Y];
            object D(ValueBuffer b) => serializer.Deserialize (b.Field.FrontType.Type, S (Pair.NextPosition (b.First, b.Last, !b.Horizontal)));
            ((string)sheet[0][0]).Aggregate (new ValueBuffer (i => type.CompactFields[i], i => type.CompactFields[i].ArrayTypes[0])
                                             {IsObject = true, First = new Pair (1, 0), Horizontal = true, TargetObject = result},
                                             (b, c) => c switch
                                                       {
                                                           '.' => FinishObject (b),
                                                           '*' => ReadValue (D(b), b),
                                                           '(' => CreateObject (true, b),
                                                           '[' => CreateObject (false, b),
                                                           _   => b
                                                       });
            return result;
        }

        static ValueBuffer FinishObject(ValueBuffer b)
        {
            b.Back.Last = b.Back.Last.Join (b.Last);
           return b.Back;
        }

        static ValueBuffer ReadValue(object value, ValueBuffer b)
        {
            (b.IsObject ? b.addToObj : b.addToArray).Invoke (value);
            b.Last = b.Last.Join (Pair.NextPosition (b.First, b.Last, !b.Horizontal));
            return b;
        }

        static ValueBuffer CreateObject(bool horizontal, ValueBuffer b) => new ValueBuffer
                                                                           {
                                                                               IsObject     = b.IsObject
                                                                                                  ? b.getNextField (b.Index).DimensionCount == 0
                                                                                                  : b.Field.DimensionCount == (b.Depth + 1),
                                                                               Field        = b.IsObject ? b.getNextField (b.Index) : b.Field,
                                                                               Depth        = b.IsObject ? 0 : b.Depth + 1,
                                                                               TargetObject = b.NextObject (),
                                                                               Back         = b,
                                                                               First        = Pair.NextPosition (b.First, b.Last, !b.Horizontal),
                                                                               Horizontal   = horizontal,
                                                                           };
#endregion
    }

    class ValueBuffer
    {
        public MappedAttribute Field;
        public int Depth, Index;
        public object TargetObject;
        public Pair First, Last = new Pair(-1, -1);
        public ValueBuffer Back;
        public bool Horizontal, IsObject;

        public readonly Func<int, MappedAttribute> getNextField;
        private readonly Func<int, Type> getNextChildType;
        public readonly Action<object> addToObj, addToArray;

        public ValueBuffer(Func<int, MappedAttribute> getField = null, Func<int, Type> getChild = null)
        {
            getNextField     = getField ?? (i => Field.FrontType.CompactFields[i]);
            getNextChildType = getChild ?? (_ => Field.ArrayTypes[Depth]);
            addToObj         = child => getNextField (Index++).Field.SetValue (TargetObject, child);
            addToArray       = TargetObject.AddContent (getNextChildType(Index++));
        }

        public object NextObject()
        {
            var result = Activator.CreateInstance (getNextChildType (Index));
            (IsObject ? addToObj : addToArray).Invoke (result);
            return result;
        }
    }

    class ValueRangeAssembleContext
    {
        public readonly IList<IList<object>> Values = new List<IList<object>> (new List<object>[1]);
        public readonly StringBuilder Sb = new StringBuilder ();
        public Pair Pos = new Pair(1, 0);
    }

    class RequestedObject
    {
        List<string> sheets;
        public readonly MappedClassAttribute Type;
        public readonly string ParentName, OwnName;
        public readonly int[] Index;

        public IEnumerable<string> FullNames => sheets ??= Type.RequiredSheets.Select (OwnName.JoinSheetNames).ToList ();

        public RequestedObject(MappedClassAttribute type, string name, params int[] indices)
        {
            Type       = type;
            ParentName = name;
            Index      = indices;
            OwnName    = Index.Length == 0 ? name : $"{name} {string.Join (" ", Index)}";
        }
    }

    readonly struct Pair
    {
        public readonly int X, Y;
        public Pair(int x, int y) { X = x; Y = y; }
        public Pair Join(Pair other) => new Pair (Math.Max (X, other.X), Math.Max (Y, other.Y));
        public Pair Add(Pair other) => new Pair (X + other.X, Y + other.Y);
        public static Pair NextPosition(Pair first, Pair last, bool isY) =>  first.Join (new Pair (isY ? -1  : last.X + 1 , isY ? last.Y + 1: -1 ));
    }
}