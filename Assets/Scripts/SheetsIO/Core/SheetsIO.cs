using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;

namespace SheetsIO
{
    public sealed class SheetsIO : IDisposable
    {
        const string FirstCell = "B2";
        internal const int MaxFreeSizeArrayElements = 100;
        internal const int A1LettersCount = 26;
        
        readonly SheetsService service;
        readonly IValueSerializer serializer;

#region GOOD
        /// <summary>Read the data from a spreadsheet and deserialize it to object of type T.</summary>
        /// <param name="spreadsheet">Spreadsheet ID.</param>
        /// <param name="sheet">Specify the name of the sheet, if the spreadsheet contains multiple sheets of a type.</param>
        /// <typeparam name="T">Type of object to read from the spreadsheet data.</typeparam>
        /// <returns>Object of type T.</returns>
        public async Task<T> ReadAsync<T>(string spreadsheet, string sheet = "")
        {
            var spreadsheets = await service.GetSpreadsheetAsync(spreadsheet);
            var context = new ReadContext(serializer, spreadsheets.GetSheetsList());
            var type = typeof(T).GetIOAttribute();
            var result = Activator.CreateInstance<T>();
            if (!context.ReadType(type, $"{sheet} {type.SheetName}".Trim(), result))
                throw new Exception("Can't parse the requested object. Some required sheets are missing in the provided spreadsheet");

            var valueRanges = await service.GetValueRanges(spreadsheet, context.Dictionary.Select(x => x.Key));
            if (!valueRanges.All(range => context.Dictionary.First(pair => range.IsSameRange(pair.Key)).Value.Invoke(range)))
                throw new Exception("Failed to assemble the object.");
            return result;
        }

        /// <summary>Write a serialized object of type T to the spreadsheet.</summary>
        /// <param name="obj">Object to serialize.</param>
        /// <param name="spreadsheet">Spreadsheet ID.</param>
        /// <param name="sheet">Specify the name of the sheet, if the spreadsheet contains multiple sheets of a type.</param>
        /// <typeparam name="T">Type of serialized object.</typeparam>
        public Task<bool> WriteAsync<T>(T obj, string spreadsheet, string sheet = "")
        {
            var valueRangeList = new List<ValueRange>();
            WriteType(typeof(T).GetIOAttribute(), sheet, obj, valueRangeList);
            return service.WriteRangesAsync(spreadsheet, valueRangeList);
        }

        /// <summary>Creates a new ready-to-use instance of the serializer.</summary>
        /// <param name="initializer"> An initializer for Google Sheets service. </param>
        /// <param name="valueSerializer"> Replaces the default serialization strategy. </param>
        public SheetsIO(BaseClientService.Initializer initializer, IValueSerializer valueSerializer = null)
        {
            // If you got an issue with missing System.IO.Compression lib, set GZipEnabled = FALSE in your initializer.
            service    = new SheetsService(initializer ?? throw new ArgumentException("SheetsService can't be null"));
            serializer = valueSerializer ?? new DefaultValueSerializer();
        }

        public void Dispose() => service.Dispose();

        void WriteType(IOMetaAttribute type, string name, object obj, ICollection<ValueRange> c)
        {
            foreach (var pointer in type.GetSheetPointers($"{name} {type.SheetName}".Trim()))
                WriteSheetObject(pointer, obj.GetChild(pointer), c);
            if (type.CompactFields.Count == 0) return;
            
            var data = new List<IList<object>>();
            foreach (var pointer in type.GetPointers(V2Int.Zero))
                WriteObject(pointer, obj.GetChild(pointer), data);
            c.Add(new ValueRange {Values = data, MajorDimension = "COLUMNS", Range = type.GetA1Range(name, FirstCell)});
        }
        
        void WriteSheetObject(IOPointer p, object obj, ICollection<ValueRange> c)
        {
            if (p.Rank == p.Field.Rank)
                WriteType(p.Field.FrontType, p.Name, obj, c);
            else foreach (var ch in IOPointer.GetChildrenSheets(p))
                WriteSheetObject(ch, obj.GetChild(ch), c);
        }

        void WriteObject(IOPointer p, object obj, IList<IList<object>> values)
        {
            if (p.IsValue)
                WriteValue(values, obj, p.Pos);
            else foreach (var ch in IOPointer.GetChildren(p))
                WriteObject(ch, obj.GetChild(ch), values);
        } // todo: highlight elements of free array with color (chessboard-ish order)    with   ((index.X + index.Y) & 1) ? color1 : color2

        void WriteValue(IList<IList<object>> values, object value, V2Int pos)
        {
            for (int i = values.Count; i <= pos.X; i++)
                values.Add(new List<object>());
            for (int i = values[pos.X].Count; i < pos.Y; i++)
                values[pos.X].Add(null);
            values[pos.X].Add(serializer.Serialize(value));
        }

        class ReadContext
        {
            public readonly IDictionary<string, Func<ValueRange, bool>> Dictionary = new Dictionary<string, Func<ValueRange, bool>>();
            readonly HashSet<string> sheets;
            readonly IValueSerializer serializer;

            public ReadContext(IValueSerializer serializer, IEnumerable<string> sheets)
            {
                this.serializer = serializer;
                this.sheets     = new HashSet<string>(sheets);
            }
            
            public bool ReadType(IOMetaAttribute type, string name, object obj)
            {
                if (!type.GetSheetPointers($"{name} {type.SheetName}".Trim()).All(p => ReadSheetObject(p, p.CreateObject(obj)))) return false;
                if (type.CompactFields.Count == 0) return true;
                if (!sheets.Contains(name)) return false;
                Dictionary.Add(name, ReadObjectCurry(type.GetPointers(V2Int.Zero), obj));
                return true;
            }

            bool ReadSheetObject(IOPointer p, object obj)
                => p.Rank == p.Field.Rank
                       ? ReadType(p.Field.FrontType, p.Name, obj)
                       : IOPointer.GetChildrenSheets(p).All(ch => ReadSheetObject(ch, ch.CreateObject(obj)));

            Func<ValueRange, bool> ReadObjectCurry(IEnumerable<IOPointer> pointers, object obj) => range => pointers.All(p => ReadObject(p, obj, range.Values));
            
            bool ReadObject(IOPointer p, object obj, IList<IList<object>> values) =>
                p.IsValue
                    ? TryReadObject(p, values, obj)
                    : IOPointer.GetChildren(p).All(ch => ReadObject(ch, ch.CreateObject(obj), values));

            bool TryReadObject(IOPointer p, IList<IList<object>> data, object parent) =>
                data.TryGetElement(p.Pos.X, out var column)
             && column.TryGetElement(p.Pos.Y, out var cell)
             && !(p.Field.AddChild(parent, p.Rank, p.Index, serializer.Deserialize(p.Field.ArrayTypes[p.Rank], cell)) is null);
        }
#endregion
    }
}
