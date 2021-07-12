using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using UnityEngine;

namespace SpreadsheetsMapper
{
    public sealed class MapperService : IDisposable
    {
        internal const string FirstCell = "B2";
        internal const int MaxFreeSizeArrayElements = 100;

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
            if (!context.TryGetClass(typeof(T).MapAttribute(), sheet, out var result))
                throw new Exception("Can't parse the requested object. Data is missing in the provided spreadsheet");

            var valueRanges = await service.GetValueRanges(spreadsheet, context.Dictionary.Select(x => x.Key));
            foreach (var range in valueRanges)
                context.Dictionary.First(pair => range.MatchRange(pair.Key)).Value.Invoke(range.Values);
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
            MakeValueRange(obj, typeof(T).MapAttribute(), sheet, valueRangeList);
            return service.WriteRangesAsync(spreadsheet, valueRangeList);
        }

        /// <summary>Creates a new ready-to-use instance of the serializer.</summary>
        /// <param name="initializer"> An initializer for Google Sheets service. </param>
        /// <param name="valueSerializer"> Replaces the default serialization strategy. </param>
        public MapperService(BaseClientService.Initializer initializer, IValueSerializer valueSerializer = null)
        {
            // If you got an issue with missing System.IO.Compression lib, set GZipEnabled = FALSE in your initializer.
            service    = new SheetsService(initializer ?? throw new ArgumentException("SheetsService can't be null"));
            serializer = valueSerializer ?? new DefaultValueSerializer();
        }

        public void Dispose()
        {
            service.Dispose();
        }

        void MakeValueRanges(object obj, MapFieldAttribute field, string parentName, ICollection<ValueRange> results, int rank)
        {
            if (rank == field.Rank)
                MakeValueRange(obj, field.FrontType, parentName, results);
            else if (obj is ICollection c)
                foreach (var (e, i) in c.Cast<object>().Select((e, i) => (e, i)))
                    MakeValueRanges(e, field, $"{parentName} {i + 1}", results, rank + 1);
        }

        void MakeValueRange(object obj, MapClassAttribute type, string parentName, ICollection<ValueRange> results)
        {
            string name = $"{parentName} {type.SheetName}";
            foreach (var field in type.SheetsFields)
                MakeValueRanges(field.Field.GetValue(obj), field, name, results, 0);
            if (type.CompactFields.Count == 0) return;

            var data = new List<IList<object>>();
            WriteObject(data, type, obj, V2Int.Zero);
            results.Add(new ValueRange {Values = data, MajorDimension = "COLUMNS", Range = type.GetRange(name, FirstCell)});
        }

        void WriteObject(IList<IList<object>> values, MapClassAttribute type, object source, V2Int fromPoint)
        {
            if (type == null)
                WriteValue(values, serializer.Serialize(source), fromPoint);
            else
                foreach (var field in type.CompactFields)
                    WriteFieldContent(values, field, field.Field.GetValue(source), fromPoint.Add(field.PosInType), 0);
        }

        void WriteFieldContent(IList<IList<object>> values, MapFieldAttribute field, object source, V2Int fromPoint, int rank)
        {
            if (rank == field.Rank)
                WriteObject(values, field.FrontType, source, fromPoint);
            else if (source is ICollection c)
                foreach (var (e, i) in c.Cast<object>().Select((e, i) => (e, i)))
                    WriteFieldContent(values, field, e, fromPoint.Add(field.TypeOffsets[rank + 1].Scale(i)), rank + 1);
        } // todo: highlight elements of free array with color (chessboard-ish order)    with   ((index.X + index.Y) & 1) ? noColor : color

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

            public bool TryGetClass(MapClassAttribute type, string parentName, out object result)
            {
                var res = Activator.CreateInstance(type.Type);
                var name = $"{parentName} {type.SheetName}".Trim();
                result = res;
                if (type.SheetsFields.Select(f => TryMakeSheet(f, res, name)).ToArray().Any(x => !x) || !sheets.Contains(name))
                    return false;

                if (type.CompactFields.Count > 0)
                    Dictionary.Add(type.GetRange(name, MapperService.FirstCell), FillCompactFieldsAction(type, result));
                return true; // todo: do i make arrays there?
            }
              
            Func<IList<IList<object>>, bool> FillCompactFieldsAction(MapClassAttribute type, object target) => r =>
                type.CompactFields.Select(f => TrySetObject(r, target, f, 0, 0, f.PosInType)).ToArray().Any(x => x);

            bool TryMakeSheet(MapFieldAttribute field, object parent, string name) // missing arrays
            {
                bool success = field.Rank == 0
                                   ? TryGetClass(field.FrontType, name, out var child)
                                   : field.CollectionSize.Count > 0
                                       ? MakeFixedSizeArray(field, name, 0, out child)
                                       : MakeFreeSizeArray(field, name, 0, out child);
                field.Field.SetValue(parent, child);
                return success;
            }

            bool MakeFixedSizeArray(MapFieldAttribute field, string name, int rank, out object result) =>
                Loop(field, name, result = Activator.CreateInstance(field.ArrayTypes[rank]), rank, field.CollectionSize[rank], true);
            bool MakeFreeSizeArray(MapFieldAttribute field, string name, int rank, out object result) =>
                Loop(field, name, result = Activator.CreateInstance(field.ArrayTypes[rank]), rank, MapperService.MaxFreeSizeArrayElements, false);

            bool Loop(MapFieldAttribute field, string name, object parent, int rank, int maxIndex, bool ignoreFails)
            {
                int index = 0; // first index must be 1
                while (index < maxIndex && ((rank < field.Rank
                                                 ? MakeFreeSizeArray(field, $"{name} {index + 1}", rank + 1, out var child)
                                                 : TryGetClass(field.FrontType, $"{name} {index + 1}", out child)) || ignoreFails))
                    field.AddChild(parent, rank + 1, index++, child);
                return index > 0;
            }
#endregion
#region Finalizing the requested object with ValueRange values
            bool TrySetObject(IList<IList<object>> values, object parent, MapFieldAttribute f, int rank, int index, V2Int from) =>
                rank == f.Rank && f.FrontType is null
                    ? TrySetValue(values, parent, f, rank, index, from)
                    : TryGoInside(values, f, rank, from, f.AddChild(parent, rank, index, f.ArrayTypes[rank].IsArray
                                                                                             ? CreateArrayInstance(values, from, f, rank + 1)
                                                                                             : Activator.CreateInstance(f.ArrayTypes[rank])));

            bool TryGoInside(IList<IList<object>> values, MapFieldAttribute f, int rank, V2Int from, object target) =>
                f.Rank == rank
                    ? f.FrontType.CompactFields.Select((ff, i) => TrySetObject(values, target, ff, 0, i, from.Add(ff.PosInType))).ToArray().Any(x => x)
                    : f.CollectionSize.Count > 0
                        ? Loop(values, f, rank, from, target, f.CollectionSize[rank], true)
                        : Loop(values, f, rank, from, target, MapperService.MaxFreeSizeArrayElements, false);

            bool TrySetValue(IList<IList<object>> values, object target, MapFieldAttribute f, int rank, int index, V2Int from)
            {
                try
                {
                    var value = serializer.Deserialize(f.ArrayTypes[rank], values[from.X][from.Y]);
                    f.AddChild(target, rank, index, value);
                    return true;
                }
                catch (Exception e)
                {
                    Debug.LogError($"failed to read value of type {f.ArrayTypes[rank]} at {from}{Environment.NewLine}{e}");
                    // todo: mark .asmdef with No Dependency On Unity 
                    return false;
                }
            }

            bool Loop(IList<IList<object>> values, MapFieldAttribute f, int rank, V2Int from, object target, int maxIndex, bool ignoreFails)
            {
                int index = 0;
                while (index < maxIndex && (TrySetObject(values, target, f, rank + 1, index, from.Add(f.TypeOffsets[rank + 1].Scale(index))) || ignoreFails))
                    index += 1;
                return index > 0;
            }

            static object CreateArrayInstance(IList<IList<object>> values, V2Int from, MapFieldAttribute f, int childRank)
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
