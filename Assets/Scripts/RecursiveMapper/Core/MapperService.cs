using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;

namespace RecursiveMapper
{
    public sealed class MapperService : IDisposable
    {
        const string FirstCell = "A2";

        readonly SheetsService service;
        readonly IValueSerializer serializer;

        /// <summary>Read the data from a spreadsheet and deserialize it to object of type T.</summary>
        /// <param name="spreadsheet">Spreadsheet ID.</param>
        /// <param name="sheet">Specify the name of the sheet, if the spreadsheet contains multiple sheets of a type.</param>
        /// <typeparam name="T">Type of object to read from the spreadsheet data.</typeparam>
        /// <returns>Object of type T.</returns>
        public async Task<T> ReadAsync<T>(string spreadsheet, string sheet = "")
        {
            var sheetsList = await service.GetSheetsListAsync (spreadsheet);
            var result = Activator.CreateInstance<T>();
            var remainingAssembleActions = new Dictionary<string, Action<IList<IList<object>>>> ();
            var typeMeta = typeof(T).MapAttribute();
            if (!ReadObject (result, sheet.JoinSheetNames(typeMeta.SheetName), typeMeta, new HashSet<string> (sheetsList), remainingAssembleActions))
                throw new Exception("Can't parse the requested object. Data is missing in the provided spreadsheet");

            var valueRanges = await service.GetValueRanges (spreadsheet, remainingAssembleActions.Select(x => x.Key));
            foreach (var pair in remainingAssembleActions)
                pair.Value.Invoke (valueRanges.FirstOrDefault (x => x.Range == pair.Key)?.Values);
            return result;
        }

        /// <summary>Write a serialized object of type T to the spreadsheet.</summary>
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

        /// <summary>Creates a new ready-to-use instance of the serializer.</summary>
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
            if (type.CompactFields.Count == 0) return;

            var data = new List<IList<object>> ();
            WriteValueRange (data, type, obj, SpreadsheetsUtility.ReadA1 (FirstCell));
            results.Add (new ValueRange{Values = data, MajorDimension = "COLUMNS", Range = type.GetReadRange(name, FirstCell)});
        }
        
        // todo: offset(int rank, int index) = rank == field.Rank ? field.FrontType.CompactFields.Take(index).Sum(Size.X) : field.GetOffset(rank, index);
        void WriteValueRange(IList<IList<object>> values, MapClassAttribute type, object source, V2Int fromPoint)
        {
            if (type == null)
                WriteSingleValue (values, serializer.Serialize (source), fromPoint);
            else
                foreach (var field in type.CompactFields)
                foreach ((var o, var p) in (field.Field.GetValue (source), fromPoint).UnwrapArray(field, 1, (v2, r, i) => field.GetOffset(r,i).Add(v2)))
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

        // There are 4 categories of fields, which are processed by different rules. Arrays may stay empty, but other sheets must be present.
        bool ReadObject(object obj, string name, MapClassAttribute type, HashSet<string> sheets, IDictionary<string, Action<IList<IList<object>>>> actions)
        {
            if (type.CompactFields.Any ())
            {
                if (!sheets.Contains(name)) return false;
                actions.Add (type.GetReadRange(name, FirstCell), ApplyValueCurried (obj, type));    // all compact fields together
            }
            foreach (var pair in type.SheetsFields.Where(f => f.Rank > 0 && f.Rank == f.CollectionSize.Count)    // arrays of sheets of fixed size
                                     .SelectMany(field => EnumerateFixedSizedArray((obj, name.JoinSheetNames(field.FrontType.SheetName)), field, 1)
                                                         .Select(x => (x.obj, x.name, new Dictionary<string, Action<IList<IList<object>>>>()))
                                                         .Where(x => ReadObject(x.obj, x.name, field.FrontType, sheets, x.Item3))
                                                         .SelectMany(x => x.Item3)))
                actions.Add(pair.Key, pair.Value);

            foreach (var field in type.SheetsFields.Where(f => f.Rank > 0 && f.CollectionSize.Count == 0))    // arrays of sheets of free size
                ReadFree(obj, name.JoinSheetNames(field.FrontType.SheetName), field, sheets, actions, 1);
            
            return type.SheetsFields.Where (f => f.Rank == 0)    // single sheets
                       .All (f => ReadObject (f.AddChild (obj, 0), name.JoinSheetNames(f.FrontType.SheetName), f.FrontType, sheets, actions));
        }

        bool ReadFree(object obj, string name, MapFieldAttribute f, HashSet<string> sheets, IDictionary<string, Action<IList<IList<object>>>> actions, int rank)
        {
            int index = 0;
            var dictionary = new Dictionary<string, Action<IList<IList<object>>>>();
            while (rank == f.Rank 
                       ? ReadObject(obj, $"{name} {++index}", f.FrontType, sheets, dictionary) 
                       : ReadFree(f.AddChild(obj, rank), $"{name} {++index}", f, sheets, dictionary, rank + 1))
            {
                foreach (var action in dictionary)
                    actions.Add(action.Key, action.Value);
                dictionary.Clear();
            }
            return index > 1;
        }
        
        IEnumerable<(object obj, string name)> EnumerateFixedSizedArray((object obj, string name) array, MapFieldAttribute f, int rank)
        {
            var indices = Enumerable.Range(1, f.CollectionSize[rank - 1]);
            return rank > f.CollectionSize.Count - 1 // only for collections of fixed size
                       ? indices.Select(i => (f.AddChild(array.obj, rank), $"{array.name} {i}"))
                       : indices.SelectMany(i => EnumerateFixedSizedArray((f.AddChild(array.obj, rank), $"{array.name} {i}"), f, rank + 1));
        }

        Action<IList<IList<object>>> ApplyValueCurried(object obj, MapClassAttribute type) => values =>
        {
            foreach (var f in type.CompactFields)
                Unwrap (f.AddChild (obj, 0), values.Take (f.Borders.Size.X), f, 0);
        };

        void ApplyValue(object target, IList<IList<object>> values, MapFieldAttribute field)
        {
            if (field.FrontType is null) // values should have 1 element there
                field.AddChild (target, field.Rank, serializer.Deserialize (field.ArrayTypes.Last (), (string)values[0][0]));
            else foreach (var f in field.FrontType.CompactFields)
                    Unwrap (f.AddChild (target, 1), values.Take (f.Borders.Size.X), f, 1);
        }

        void Unwrap(object target, IEnumerable<IList<object>> values, MapFieldAttribute field, int rank) //todo: can be removed. TopLeft corner pos is enough
        {
            if (rank == field.Rank)
                ApplyValue (target, values.ToList (), field);
            else foreach (var part in (rank & 1) > 0
                                          ? values.Select (x => (IEnumerable<IList<object>>)x.ToChunks (field.TypeSizes[rank].Y))
                                          : values.ToChunks (field.TypeSizes[rank].X))
                    Unwrap (field.AddChild (target, rank), part, field, rank + 1);
        }
    }
}
