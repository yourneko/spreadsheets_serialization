using System;
using System.Collections.Generic;
using System.Linq;
using Google.Apis.Sheets.v4.Data;
using UnityEngine;

namespace SpreadsheetsMapper
{
    class SheetsReadContext
    {
        public readonly IDictionary<string, Func<ValueRange, bool>> Dictionary = new Dictionary<string, Func<ValueRange, bool>>();
        readonly HashSet<string> sheets;
        readonly IValueSerializer serializer;

        public SheetsReadContext(IValueSerializer serializer, IEnumerable<string> sheets)
        {
            this.serializer = serializer;
            this.sheets     = new HashSet<string>(sheets);
            Debug.Log($"SheetsReadContext created. Spreadsheet contains {this.sheets.Count} sheets.");
        }
        
        public bool TryGetClass(MapClassAttribute type, string parentName, out object result) // todo: is it replaceable by TrySetObject/TryParse like methods?
        {
            result = Activator.CreateInstance(type.Type);
            var name = $"{parentName} {type.SheetName}".Trim();

            foreach (var field in type.SheetsFields)
                field.Field.SetValue(result, field.Rank > 0
                                                 ? field.CollectionSize.Count > 0
                                                       ? MakeFixedSizeArray(field, name, 0)
                                                       : MakeFreeSizeArray(field, name, 0, out var child) ? child : null
                                                 : TryGetClass(field.FrontType, name, out var obj) ? obj : null);
            if (type.CompactFields.Count <= 0) 
                return true;

            bool success = sheets.Contains(name);
            if (success)
                Dictionary.Add(type.GetRange(name, MapperService.FirstCell), FillCompactFieldsAction(type, result));
            return success;
        }

        object MakeFixedSizeArray(MapFieldAttribute field, string name, int rank)
        {
            var result = Activator.CreateInstance(field.ArrayTypes[rank]);
            for (int i = 0; i < field.CollectionSize[rank]; i++)
                field.AddChild(result, rank + 1, rank < field.Rank
                                                     ? MakeFixedSizeArray(field, $"{name} {i + 1}", rank + 1)
                                                     : TryGetClass(field.FrontType, $"{name} {i + 1}", out var obj) ? obj : null);
            return result;
        }

        bool MakeFreeSizeArray(MapFieldAttribute field, string name, int rank, out object result)
        {
            result = Activator.CreateInstance(field.ArrayTypes[rank]);
            int index = 0;
            while (rank < field.Rank
                       ? MakeFreeSizeArray(field, $"{name} {++index}", rank + 1, out var child)
                       : TryGetClass(field.FrontType, $"{name} {++index}", out child))
                field.AddChild(result, rank + 1, child);
            return index > 1 || rank == 0;
        }

        Func<ValueRange, bool> FillCompactFieldsAction(MapClassAttribute type, object target) => r => 
            type.CompactFields.Select((f, i) => TrySetObject(r.Values, target, f, 0, i, V2Int.Zero)).ToArray().Any(x => x);
        
        bool TrySetObject(IList<IList<object>> values, object parent, MapFieldAttribute f, int rank, int index, V2Int from)
        {
            return rank != f.Rank
                       ? TryParse(values, f.AddChild(parent, rank), f, rank, from)
                       : f.FrontType != null
                           ? TryParse(values, f.AddChild(parent, rank), f.FrontType.CompactFields[index], 0, from)
                           : TryGetValue(values, parent, f, rank, from);
        }
        
        bool TryParse(IList<IList<object>> values, object target, MapFieldAttribute f, int rank, V2Int from) 
        {
            if (f.Rank == rank)
            {
                var pos = from;
                return f.FrontType.CompactFields
                        .Select((ff, i) => TrySetObject(values, target, ff, rank, i, pos = pos.Add(ff.TypeOffsets[0].Scale(i)))).ToArray().Any(x => x);
            }

            if (f.CollectionSize.Count > 0)
            {
                for (int i = 0; i < f.CollectionSize[rank]; i++)
                    TrySetObject(values, target, f, rank + 1, i, from.Add(f.TypeOffsets[rank + 1].Scale(i)));
                return true;
            }
            
            int index = 0;
            while (TrySetObject(values, target, f, rank + 1, index, from.Add(f.TypeOffsets[rank + 1].Scale(index))))
                index += 1;
            return index > 0;
        }
        
        bool TryGetValue(IList<IList<object>> values, object target, MapFieldAttribute f, int rank, V2Int from)
        {
            try
            {
                f.AddChild(target, rank, serializer.Deserialize(f.ArrayTypes[rank], values[from.X][from.Y]));
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                f.AddChild(target, rank, default);
                return false;
            }
        }
    }
}
