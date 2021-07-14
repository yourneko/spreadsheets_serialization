using System;
using System.Collections;
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
        const int MaxFreeSizeArrayElements = 100;

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
            var context = new SheetsReadContext(serializer, spreadsheets.GetSheetsList());
            var type = typeof(T).GetIOAttribute();
            if (!context.TryGetClass(type, $"{sheet} {type.SheetName}".Trim(), out var result))
                throw new Exception("Can't parse the requested object. Some required sheets are missing in the provided spreadsheet");

            var valueRanges = await service.GetValueRanges(spreadsheet, context.Dictionary.Select(x => x.Key));
            if (!valueRanges.All(range => context.Dictionary.First(pair => range.IsSameRange(pair.Key)).Value.Invoke(range.Values)))
                throw new Exception("Failed to assemble the object.");
            return (T) result;
        }

        /// <summary>Write a serialized object of type T to the spreadsheet.</summary>
        /// <param name="obj">Object to serialize.</param>
        /// <param name="spreadsheet">Spreadsheet ID.</param>
        /// <param name="sheet">Specify the name of the sheet, if the spreadsheet contains multiple sheets of a type.</param>
        /// <typeparam name="T">Type of serialized object.</typeparam>
        public Task<bool> WriteAsync<T>(T obj, string spreadsheet, string sheet = "")
        {
            var valueRangeList = new List<ValueRange>();
            var type = typeof(T).GetIOAttribute();
            MakeValueRange(obj, type, $"{sheet} {type.SheetName}".Trim(), valueRangeList);
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
        
        void MakeValueRanges(object obj, IOFieldAttribute field, string parentName, ICollection<ValueRange> results, int rank)
        {
            if (rank == field.Rank)
                MakeValueRange(obj, field.FrontType, $"{parentName} {field.FrontType.SheetName}", results);
            else if (obj is ICollection c)
                foreach (var (e, i) in c.Cast<object>().Select((e, i) => (e, i)))
                    MakeValueRanges(e, field, $"{parentName} {i + 1}", results, rank + 1);
        }

        void MakeValueRange(object obj, IOMetaAttribute type, string name, ICollection<ValueRange> results)
        {
            foreach (var field in type.SheetsFields)
                MakeValueRanges(field.Field.GetValue(obj), field, name, results, 0);
            if (type.CompactFields.Count == 0) return;
            var data = new List<IList<object>>();
            WriteObject(data, type, obj, new V2Int(0, 0));
            results.Add(new ValueRange {Values = data, MajorDimension = "COLUMNS", Range = type.GetA1Range(name, FirstCell)});
        }

        void WriteObject(IList<IList<object>> values, IOMetaAttribute type, object source, V2Int fromPoint)
        {
            if (type == null)
                WriteValue(values, serializer.Serialize(source), fromPoint);
            else
                foreach (var field in type.CompactFields)
                    WriteFieldContent(values, field, field.Field.GetValue(source), fromPoint.Add(field.PosInType), 0);
        }

        void WriteFieldContent(IList<IList<object>> values, IOFieldAttribute field, object source, V2Int fromPoint, int rank)
        {
            if (rank == field.Rank)
                WriteObject(values, field.FrontType, source, fromPoint);
            else if (source is ICollection c)
                foreach (var (e, i) in c.Cast<object>().Select((e, i) => (e, i)))
                    WriteFieldContent(values, field, e, fromPoint.Add(field.TypeOffsets[rank + 1].Scale(i)), rank + 1);
        } // todo: highlight elements of free array with color (chessboard-ish order)    with   ((index.X + index.Y) & 1) ? color1 : color2

        void WriteValue(IList<IList<object>> values, object value, V2Int pos)
        {
            for (int i = values.Count; i <= pos.X; i++)
                values.Add(new List<object>());
            for (int i = values[pos.X].Count; i < pos.Y; i++)
                values[pos.X].Add(null);
            values[pos.X].Add(serializer.Serialize(value));
        }

        class SheetsReadContext 
        {
            public readonly IDictionary<string, Func<IList<IList<object>>, bool>> Dictionary = new Dictionary<string, Func<IList<IList<object>>, bool>>();
            readonly HashSet<string> sheets;
            readonly IValueSerializer serializer;

            public SheetsReadContext(IValueSerializer serializer, IEnumerable<string> sheets)
            {
                this.serializer = serializer;
                this.sheets     = new HashSet<string>(sheets);
            }

#region Listing required sheets, meanwhile partially assembling the object
            public bool TryGetClass(IOMetaAttribute type, string name, out object result)
            {
                var res = Activator.CreateInstance(type.Type);
                result = res;
                if (type.SheetsFields.Select((f, i) => (f, i)).Any(pair => !TryMakeSheet(pair.f, res, name, 0, pair.i))) 
                    return false;
                if (type.CompactFields.Count == 0) 
                    return true;
                if (!sheets.Contains(name)) 
                    return false;
                Dictionary.Add(type.GetA1Range(name, FirstCell), CreateObjectFromValueRangeCurried(type, result));
                return true;
            }

            bool TryMakeSheet(IOFieldAttribute field, object parent, string name, int rank, int index) 
            {
                var newName = $"{name} {(rank == 0 ? field.FrontType.SheetName : (index + 1).ToString())}".Trim();
                bool success = field.Rank == rank
                                   ? TryGetClass(field.FrontType, newName, out var child)
                                   : field.CollectionSize.Count > 0
                                       ? MakeFixedSizeArray(field, newName, rank, out child) 
                                       : MakeFreeSizeArray(field, newName, rank, out child);
                field.AddChild(parent, rank, index, child);
                return success;
            } 
            
            bool MakeFixedSizeArray(IOFieldAttribute field, string name, int rank, out object result) =>
                Loop(field, name, result = CreateArray(field, rank), rank, field.CollectionSize[rank], true);
            bool MakeFreeSizeArray(IOFieldAttribute field, string name, int rank, out object result) =>
                Loop(field, name, result = CreateArray(field, rank), rank, MaxFreeSizeArrayElements, false);
            static object CreateArray(IOFieldAttribute f, int rank) => f.ArrayTypes[rank].IsArray
                                                                                      ? Array.CreateInstance(f.ArrayTypes[rank + 1], f.CollectionSize[rank])
                                                                                      : Activator.CreateInstance(f.ArrayTypes[rank]);

            bool Loop(IOFieldAttribute field, string name, object parent, int rank, int maxIndex, bool ignoreFails)
            {
                int index = 0; // first index must be 1
                while (index < maxIndex && (TryMakeSheet(field, parent, name, rank + 1, index) || ignoreFails))
                    index += 1;
                return index > 0;
            }
              
            Func<IList<IList<object>>, bool> CreateObjectFromValueRangeCurried(IOMetaAttribute type, object target) => r =>
                type.CompactFields.Select((field, index) => (field, index))
                    .All(pair => TryCreateObject(r, target, pair.field, 0, pair.index, pair.field.PosInType));
#endregion
#region Finalizing the requested object with ValueRanges
            bool TryCreateObject(IList<IList<object>> values, object parent, IOFieldAttribute f, int rank, int index, V2Int from) =>
                rank == f.Rank && f.FrontType is null
                    ? TrySetValue(values, parent, f, rank, index, from)
                    : TryGoInside(values, f, rank, from, f.AddChild(parent, rank, index, f.ArrayTypes[rank].IsArray
                                                                                             ? CreateArray(values, from, f, rank + 1)
                                                                                             : Activator.CreateInstance(f.ArrayTypes[rank])));

            bool TryGoInside(IList<IList<object>> values, IOFieldAttribute f, int rank, V2Int from, object target) =>
                f.Rank == rank
                    ? f.FrontType.CompactFields.Select((field, index) => (field, index))
                       .All(pair => TryCreateObject(values, target, pair.field, 0, pair.index, from.Add(pair.field.PosInType)) || pair.field.IsOptional)
                    : f.CollectionSize.Count > 0
                        ? SetArrayValues(values, f, rank, from, target, f.CollectionSize[rank], true)
                        : SetArrayValues(values, f, rank, from, target, MaxFreeSizeArrayElements, false);

            bool TrySetValue(IList<IList<object>> values, object target, IOFieldAttribute f, int rank, int index, V2Int from) =>
                values.TryGetElement(from.X, out var column)
             && column.TryGetElement(from.Y, out var cell)
             && !(f.AddChild(target, rank, index, serializer.Deserialize(f.ArrayTypes[rank], cell)) is null);

            bool SetArrayValues(IList<IList<object>> values, IOFieldAttribute f, int rank, V2Int from, object target, int maxIndex, bool ignoreFails)
            {
                int index = 0;
                while (index < maxIndex && (TryCreateObject(values, target, f, rank + 1, index, from.Add(f.TypeOffsets[rank + 1].Scale(index))) || ignoreFails))
                    index += 1;
                if (!ignoreFails && !f.ArrayTypes[rank].IsArray && target is IList list) 
                    list.RemoveAt(list.Count - 1); 
                return index > 0;
            }

            static object CreateArray(IList<IList<object>> values, V2Int from, IOFieldAttribute f, int childRank)
            {
                var v = f.TypeSizes[childRank];
                return Array.CreateInstance(f.ArrayTypes[childRank], f.CollectionSize.Count > 0
                                                                         ? f.CollectionSize[childRank - 1]
                                                                         : (childRank & 1) > 0
                                                                             ? (values.Count - from.X + (v.X - 1)) / v.X
                                                                             : (values.Skip(from.X).Take(v.X).Max(v2 => v2.Count) - from.Y + (v.Y - 1)) / v.Y);
            }
#endregion
        }
    }
}
