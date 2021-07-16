using System;
using System.Collections.Generic;
using System.Linq;
using Google.Apis.Sheets.v4.Data;

namespace SheetsIO
{
    readonly struct ReadContext
    {
        readonly IDictionary<string, (IEnumerable<IOPointer> pointers, object obj)> dictionary;
        readonly HashSet<string> sheets;
        readonly IValueSerializer serializer;
        public IEnumerable<string> Ranges => dictionary.Select(x => x.Key);
        
        public ReadContext(IEnumerable<string> sheets, IValueSerializer serializer)
        {
            this.sheets     = new HashSet<string>(sheets);
            this.serializer = serializer;
            dictionary      = new Dictionary<string, (IEnumerable<IOPointer> pointers, object obj)>();
        }
        
        public bool ReadType(IOMetaAttribute type, string name, object obj)
        {
            if (!obj.CreateChildren(type.GetSheetPointers(name), ReadSheetObject)) return false;
            if (type.Regions.Count == 0) return true;
            if (!sheets.Contains(name)) return false;
            dictionary.Add(type.GetA1Range(name, SheetsIO.FirstCell), (type.GetPointers(V2Int.Zero), obj));
            return true;
        }

        bool ReadSheetObject(IOPointer p, object obj) => p.Rank == p.Field.Rank
                                                             ? ReadType(p.Field.Meta, p.Name, obj)
                                                             : obj.CreateChildren(IOPointer.GetChildrenSheets(p), ReadSheetObject);

        public bool TryApplyRange(ValueRange range)
        {
            var (pointers, obj) = dictionary.First(pair => StringComparer.Ordinal.Equals(range.Range.GetSheetName(), pair.Key.GetSheetName())).Value;
            return obj.CreateChildren(pointers, new ReadRangeContext(range, serializer).ReadObject);
        }
    }
}
