using System.Collections.Generic;
using Google.Apis.Sheets.v4.Data;

namespace SheetsIO
{
    readonly struct ReadRangeContext
    {
        readonly IList<IList<object>> values;
        readonly IValueSerializer serializer;

        public ReadRangeContext(ValueRange range, IValueSerializer s)
        {
            values     = range.Values;
            serializer = s;
        }
            
        public bool ReadObject(IOPointer p, object obj) => (p.IsValue
                                                                ? TryReadValue(p, obj)
                                                                : obj.ForEachChild(IOPointer.GetChildren(p), ReadObject)) || p.Optional;

        bool TryReadValue(IOPointer p, object parent) =>
            values.TryGetElement(p.Pos.X, out var column) && column.TryGetElement(p.Pos.Y, out var cell) && 
            !(p.AddChild(parent, serializer.Deserialize(p.Field.ArrayTypes[p.Rank], cell)) is null);
    }
}