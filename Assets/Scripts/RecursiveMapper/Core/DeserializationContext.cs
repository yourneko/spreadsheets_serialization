using System;
using System.Collections.Generic;
using Google.Apis.Sheets.v4.Data;
using UnityEngine;

namespace SpreadsheetsMapper
{
    class DeserializationContext
    {
        public readonly IDictionary<string, Action<ValueRange>> Dictionary = new Dictionary<string, Action<ValueRange>>();
        readonly HashSet<string> sheets;
        readonly IValueSerializer serializer;

        public DeserializationContext(IValueSerializer serializer, IEnumerable<string> sheets)
        {
            this.serializer = serializer;
            this.sheets     = new HashSet<string>(sheets);
        }
        
        public bool TryGetClass(MapClassAttribute type, string parentName, out object result)
        {
            result = Activator.CreateInstance(type.Type);
            var name = $"{parentName} {type.SheetName}";

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
                Dictionary.Add(name, FillCompactFieldsAction(type, result));
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

        Action<ValueRange> FillCompactFieldsAction(MapClassAttribute type, object target) => r => ParseClass(r.Values, target, type, V2Int.Zero);

        void ParseClass(IList<IList<object>> values, object target, MapClassAttribute type, V2Int from)
        {
            var pos = from;
            foreach (var field in type.CompactFields)
            {
                TryParse(values, target, field, 0, pos);
                pos = pos.Add(field.TypeSizes[0]);
            }
        }
        
        bool TryParse(IList<IList<object>> values, object target, MapFieldAttribute field, int rank, V2Int from)
        {
            if (rank == field.Rank)
            {
                if (field.FrontType is null)
                {
                    var b = TryGetValue(values, field.ArrayTypes[rank], from, out var o);
                    field.AddChild(target, rank, b ? o : null);
                    return b;
                }
                ParseClass(values, field.AddChild(target, rank), field.FrontType, from);
                return true;
            }
            if (field.CollectionSize.Count > 0)
            {
                for (int i = 0; i < field.CollectionSize[rank]; i++)
                    TryParse(values, field.AddChild(target, rank + 1), field, rank + 1, from.Add(field.TypeOffsets[rank + 1].Scale(i, i)));
                return true;
            }
            int index = 0;
            while (TryParse(values, field.AddChild(target, rank + 1), field, rank + 1, from.Add(field.TypeOffsets[rank + 1].Scale(index, index))))
                index += 1;
            return index > 0;
        }

        bool TryGetValue(IList<IList<object>> values, Type type, V2Int from, out object target)
        {
            try
            {
                target = serializer.Deserialize(type, values[from.X][from.Y]);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                target = null;
                return false;
            }
        }
    }
}