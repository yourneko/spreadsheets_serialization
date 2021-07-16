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

        /// <summary>Read the data from a spreadsheet and deserialize it to object of type T.</summary>
        /// <param name="spreadsheet">Spreadsheet ID.</param>
        /// <param name="sheet">Specify the name of the sheet, if the spreadsheet contains multiple sheets of a type.</param>
        /// <typeparam name="T">Type of object to read from the spreadsheet data.</typeparam>
        /// <returns>Object of type T.</returns>
        public async Task<T> ReadAsync<T>(string spreadsheet, string sheet = "")
        {
            var spreadsheets = await service.GetSpreadsheetAsync(spreadsheet);
            var context = new ReadingRequest(serializer, spreadsheets.GetSheetsList());
            var type = typeof(T).GetIOAttribute();
            var result = Activator.CreateInstance<T>();
            if (!context.ReadType(type, $"{sheet} {type.SheetName}".Trim(), result))
                throw new Exception("Can't parse the requested object. Some required sheets are missing in the provided spreadsheet");

            var valueRanges = await service.GetValueRanges(spreadsheet, context.Dictionary.Select(x => x.Key));
            if (!valueRanges.All(range => context.Dictionary.First(pair => StringComparer.Ordinal.Equals(range.Range.GetSheetName(), pair.Key.GetSheetName())
                                                                        && StringComparer.Ordinal.Equals(range.Range.GetFirstCell(), pair.Key.GetFirstCell()))
                                                 .Value.Invoke(range)))
                throw new Exception("Failed to assemble the object.");
            return result;
        }

        /// <summary>Write a serialized object of type T to the spreadsheet.</summary>
        /// <param name="obj">Object to serialize.</param>
        /// <param name="spreadsheet">Spreadsheet ID.</param>
        /// <param name="sheet">Specify the name of the sheet, if the spreadsheet contains multiple sheets of a type.</param>
        /// <typeparam name="T">Type of serialized object.</typeparam>
        public Task<bool> WriteAsync<T>(T obj, string spreadsheet, string sheet = "") => 
            service.WriteRangesAsync(spreadsheet, new WriteObjectContext(typeof(T).GetIOAttribute(), sheet, obj, serializer).ValueRanges);

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

        readonly struct WriteObjectContext
        {
            public readonly IList<ValueRange> ValueRanges;
            readonly IValueSerializer s;

            public WriteObjectContext(IOMetaAttribute type, string name, object obj, IValueSerializer serializer)
            {
                ValueRanges = new List<ValueRange>();
                s = serializer;
                WriteType(type, name, obj);
            }
            
            void WriteType(IOMetaAttribute type, string name, object obj)
            {
                obj.ForEachChild(type.GetSheetPointers($"{name} {type.SheetName}".Trim()), WriteSheetObject);
                if (type.CompactFields.Count == 0) return;

                var sheet = new WriteSheetContext(s);
                obj.ForEachChild(type.GetPointers(V2Int.Zero), sheet.WriteObject);
                ValueRanges.Add(new ValueRange {Values = sheet.Values, MajorDimension = "COLUMNS", Range = type.GetA1Range(name, FirstCell)});
            }

            void WriteSheetObject(IOPointer pointer, object obj)
            {
                if (pointer.Rank == pointer.Field.Rank)
                    WriteType(pointer.Field.FrontType, pointer.Name, obj);
                else
                    obj.ForEachChild(IOPointer.GetChildrenSheets(pointer), WriteSheetObject);
            }
        }

        readonly struct WriteSheetContext
        {
            public readonly IList<IList<object>> Values;
            readonly IValueSerializer serializer;

            public WriteSheetContext(IValueSerializer s)
            {
                Values     = new List<IList<object>>();
                serializer = s;
            }
            
            public void WriteObject(IOPointer pointer, object obj)
            {
                if (pointer.IsValue)
                    WriteValue(obj, pointer.Pos);
                else 
                    obj.ForEachChild(IOPointer.GetChildren(pointer), WriteObject);
            } // todo: highlight elements of free array with color (chessboard-ish order)    with   ((index.X + index.Y) & 1) ? color1 : color2

            void WriteValue(object value, V2Int pos)
            {
                for (int i = Values.Count; i <= pos.X; i++)
                    Values.Add(new List<object>());
                for (int i = Values[pos.X].Count; i < pos.Y; i++)
                    Values[pos.X].Add(null);
                Values[pos.X].Add(serializer.Serialize(value));
            }
        }

        class ReadingRequest
        {
            public readonly IDictionary<string, Func<ValueRange, bool>> Dictionary = new Dictionary<string, Func<ValueRange, bool>>();
            readonly HashSet<string> sheets;
            readonly IValueSerializer serializer;

            public ReadingRequest(IValueSerializer serializer, IEnumerable<string> sheets)
            {
                this.serializer = serializer;
                this.sheets     = new HashSet<string>(sheets);
            }
            
            public bool ReadType(IOMetaAttribute type, string name, object obj)
            {
                if (!type.GetSheetPointers($"{name} {type.SheetName}".Trim()).All(p => ReadSheetObject(p, p.CreateObject(obj)) || p.Optional)) return false;
                if (type.CompactFields.Count == 0) return true;
                if (!sheets.Contains(name)) return false;
                Dictionary.Add(name, ReadObjectCurry(type.GetPointers(V2Int.Zero), obj));
                return true;
            }

            bool ReadSheetObject(IOPointer p, object obj) => p.Rank == p.Field.Rank
                                                                 ? ReadType(p.Field.FrontType, p.Name, obj)
                                                                 : IOPointer.GetChildrenSheets(p).All(ch => ReadSheetObject(ch, ch.CreateObject(obj)));

            Func<ValueRange, bool> ReadObjectCurry(IEnumerable<IOPointer> pointers, object obj) => range => pointers.All(p => ReadObject(p, obj, range.Values));

            bool ReadObject(IOPointer p, object obj, IList<IList<object>> values) =>
                (p.IsValue
                    ? TryReadObject(p, values, obj)
                    : IOPointer.GetChildren(p).All(ch => ReadObject(ch, ch.CreateObject(obj), values))) // todo: cut enumeration if free size array
             || p.Optional;

            bool TryReadObject(IOPointer p, IList<IList<object>> data, object parent) =>
                data.TryGetElement(p.Pos.X, out var column) && column.TryGetElement(p.Pos.Y, out var cell) && 
                !(p.AddChild(parent, serializer.Deserialize(p.Field.ArrayTypes[p.Rank], cell)) is null);
        }
    }
}
