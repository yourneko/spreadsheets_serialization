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

            var valueRanges = await service.GetValueRanges (spreadsheet, context.Dictionary.Select(x => x.Key));
            foreach (var range in valueRanges)
            {
                Debug.Log(range.Range);
                context.Dictionary.First(pair => range.MatchRange(pair.Key)).Value.Invoke(range);
            }
            return (T)result;
        }

        /// <summary>Write a serialized object of type T to the spreadsheet.</summary>
        /// <param name="obj">Object to serialize.</param>
        /// <param name="spreadsheet">Spreadsheet ID.</param>
        /// <param name="sheet">Specify the name of the sheet, if the spreadsheet contains multiple sheets of a type.</param>
        /// <typeparam name="T">Type of serialized object.</typeparam>
        public Task<bool> WriteAsync<T>(T obj, string spreadsheet, string sheet = "")
        {
            var valueRangeList = new List<ValueRange> ();
            MakeValueRange (obj, typeof(T).MapAttribute (), sheet, valueRangeList);
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

        // todo: is it replaceable by TrySetObject/TryParse like methods?
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
                MakeValueRanges (field.Field.GetValue(obj), field, name, results, 0);
            if (type.CompactFields.Count == 0) return;

            var data = new List<IList<object>> ();
            WriteObject (data, type, obj, V2Int.Zero);
            results.Add (new ValueRange{Values = data, MajorDimension = "COLUMNS", Range = type.GetRange(name, FirstCell)});
        }

        void WriteObject(IList<IList<object>> values, MapClassAttribute type, object source, V2Int fromPoint)
        {
            if (type == null)
                WriteValue(values, serializer.Serialize(source), fromPoint);
            else foreach (var field in type.CompactFields)
                    WriteFieldContent(values, field, field.Field.GetValue(source), fromPoint.Add(type.GetFieldPos(field)), 0);
        }

        void WriteFieldContent(IList<IList<object>> values, MapFieldAttribute field, object source, V2Int fromPoint, int rank)
        {
            if (rank == field.Rank)
                WriteObject(values, field.FrontType, source, fromPoint);
            else if (source is ICollection c)
                foreach (var (e, i) in c.Cast<object>().Select((e, i) => (e, i)))
                    WriteFieldContent(values, field, e, fromPoint.Add(field.TypeOffsets[rank + 1].Scale(i)), rank + 1);
        }

        void WriteValue(IList<IList<object>> values, object value, V2Int pos)
        {
            for (int i = values.Count; i <= pos.X; i++)
                values.Add (new List<object> ());
            for (int i = values[pos.X].Count; i < pos.Y; i++)
                values[pos.X].Add (null);
            values[pos.X].Add (serializer.Serialize (value));
        }
    }
}
