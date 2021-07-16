using System;
using System.Collections.Generic;
using Google.Apis.Sheets.v4.Data;

namespace SheetsIO
{
    readonly struct ReadObjectContext
    {
        public readonly IDictionary<string, Func<ValueRange, IValueSerializer, bool>> Dictionary;
        readonly HashSet<string> sheets;

        public ReadObjectContext(IEnumerable<string> sheets)
        {
            this.sheets = new HashSet<string>(sheets);
            Dictionary  = new Dictionary<string, Func<ValueRange, IValueSerializer, bool>>();
        }
            
        public bool ReadType(IOMetaAttribute type, string name, object obj)
        {
            if (!obj.ForEachChild(type.GetSheetPointers($"{name} {type.SheetName}".Trim()), ReadSheetObject)) return false;
            if (type.CompactFields.Count == 0) return true;
            if (!sheets.Contains(name)) return false;
            Dictionary.Add(name, ReadObjectCurry(type.GetPointers(V2Int.Zero), obj));
            return true;
        }

        bool ReadSheetObject(IOPointer p, object obj) => p.Rank == p.Field.Rank
                                                             ? ReadType(p.Field.FrontType, p.Name, obj)
                                                             : obj.ForEachChild(IOPointer.GetChildrenSheets(p), ReadSheetObject);

        static Func<ValueRange, IValueSerializer, bool> ReadObjectCurry(IEnumerable<IOPointer> pointers, object obj) => (range, serializer) =>
            obj.ForEachChild(pointers, new ReadRangeContext(range, serializer).ReadObject);
    }
}