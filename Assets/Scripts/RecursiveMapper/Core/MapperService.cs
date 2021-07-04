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

            (int x1, int y1) = SpreadsheetsUtility.ReadA1 (FirstCell);
            var context = new ValueRangeAssembleContext ();
            (int x2, int y2)     = DoCollection (context, obj, type, 0);
            context.Values[0][0] = context.Sb.ToString ();
            results.Add (new ValueRange
                         {
                             Values         = context.Values,
                             MajorDimension = "COLUMNS",
                             Range          = $"'{name}'!{FirstCell}:{SpreadsheetsUtility.WriteA1 (x2 + x1, y2 + y1)}",
                         });
        }

        (int x, int y) DoCollection(ValueRangeAssembleContext context, object obj, MappedClassAttribute type, int repeats, bool vertical = true)
        {
            if (repeats == 0 && type is null)
                return WriteValue (context, obj);

            (int x1, int y1, int x2, int y2) rect = (context.Pos.x, context.Pos.y, -1, -1);
            context.Sb.Append (vertical ? '[' : '(');

            var collection = repeats == 0
                                 ? type.CompactFields.Select (e => DoCollection (context, e.Field.GetValue (obj), e.FrontType, e.DimensionCount))
                                 : obj is ICollection c
                                     ? c.Cast<object> ().Select (e => DoCollection (context, e, type, repeats - 1, repeats == 0 || !vertical))
                                     : throw new Exception ();
            foreach ((int x, int y) in collection)
            {
                rect        = (rect.x1, rect.y1, Math.Max (rect.x2, x), Math.Max (rect.y2, y));
                context.Pos = (vertical ? rect.x1 : Math.Max (rect.x2 + 1, rect.x1), vertical ? Math.Max (rect.y2 + 1, rect.y1) : rect.y1);
            }

            context.Sb.Append ('.');
            return (rect.x2, rect.y2);
        }

        (int x, int y) WriteValue(ValueRangeAssembleContext context, object value)
        {
            context.Sb.Append ('*');
            if (context.Values.Count == context.Pos.x)
                context.Values.Add (new List<object> ());
            for (int i = context.Values[context.Pos.x].Count; i < context.Pos.y; i++)
                context.Values[context.Pos.x].Add (null);
            context.Values[context.Pos.x].Add (serializer.Serialize (value));
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
            var context = new ReadValueRangeContext
                          {
                              b = new ValueBuffer (i => type.CompactFields[i], i => type.CompactFields[i].ArrayTypes[0])
                                  {
                                      IsObject = true, Rect = (1, 0, 0, 0), Horizontal = true, TargetObject = result,
                                  },
                              getValue = (f, x, y) => serializer.Deserialize(f.FrontType.Type, (string)sheet[x][y])
                          };
            foreach (var c in (string)sheet[0][0])
                context.DoChar (c);
            return result;
        }
#endregion
    }

    class ReadValueRangeContext    // todo - probably can put back in the original method
    {
        public Func<MappedAttribute, int, int, object> getValue;
        public ValueBuffer b;
        private int x = 1, y;

        public void DoChar(char c)
        {
            switch (c)
            {
                case '.':
                    b.Back.Rect = (b.Back.Rect.x1, b.Back.Rect.y1, Math.Max (b.Back.Rect.x2, b.Rect.x2), Math.Max (b.Back.Rect.y2, b.Rect.y2));
                    b           = b.Back;
                    break;
                case '*':
                    Add (getValue(b.Field, x, y));
                    b.Rect = (b.Rect.x1, b.Rect.y1, Math.Max (b.Rect.x2, x), Math.Max (b.Rect.y2, y));
                    break;
                case '(': case '[':
                    var result = new ValueBuffer
                                 {
                                     IsObject     = b.IsObject ? b.getNextField (b.Index).DimensionCount == 0 : b.Field.DimensionCount == (b.Depth + 1),
                                     Field        = b.IsObject ? b.getNextField (b.Index) : b.Field,
                                     Depth        = b.IsObject ? 0 : b.Depth + 1,
                                     TargetObject = Activator.CreateInstance (b.getNextChildType (b.Index)),
                                     Back         = b,
                                     Rect         = (x, y, -1, -1),
                                     Horizontal   = c == '(',
                                 };
                    Add ((b = result).TargetObject);
                    break;
                default: return;
            }

            x = b.Horizontal ? Math.Max (b.Rect.x1, b.Rect.x2 + 1) : b.Rect.x1;
            y = b.Horizontal ? b.Rect.y1 : Math.Max (b.Rect.y1, b.Rect.y2 + 1);
        }

        void Add(object child)
        {
            if (b.IsObject)
                b.Field.FrontType.CompactFields[b.Index].Field.SetValue (b.TargetObject, child);
            else
                b.TargetObject.AddContent (b.Field.ArrayTypes[b.Depth]);
            b.Index += 1;
        }
    }

    class ValueBuffer
    {
        public MappedAttribute Field;
        public int Depth, Index;
        public object TargetObject;
        public (int x1, int y1, int x2, int y2) Rect;
        public ValueBuffer Back;
        public bool Horizontal, IsObject;
        public readonly Func<int, MappedAttribute> getNextField;
        public readonly Func<int, Type> getNextChildType;

        public ValueBuffer(Func<int, MappedAttribute> getField = null, Func<int, Type> getChild = null)
        {
            getNextField     = getField ?? (i => Field.FrontType.CompactFields[i]);
            getNextChildType = getChild ?? (_ => Field.ArrayTypes[Depth]);
        }
    }

    class ValueRangeAssembleContext
    {
        public readonly IList<IList<object>> Values = new List<IList<object>> (new List<object>[1]);
        public readonly StringBuilder Sb = new StringBuilder ();
        public (int x, int y) Pos = (1, 0);
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
}