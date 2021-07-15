using System.Collections.Generic;
using System.Linq;

namespace SheetsIO
{
    readonly struct IOPointer
    {
        public readonly IOFieldAttribute Field;
        public readonly int Rank, Index;
        public readonly V2Int Pos;
        public readonly string Name;

        public bool IsValue => Rank == Field.Rank && Field.FrontType is null;
        public bool IsArray => Field.ArrayTypes[Rank].IsArray;
        public bool IsFreeSize => Field.Rank > 0 && Field.CollectionSize.Count == 0;

        internal IOPointer(IOFieldAttribute field, int rank, int index, V2Int pos, string name)
        {
            Field = field;
            Rank  = rank;
            Index = index;
            Pos   = pos;
            Name  = name;
        }

        public static IEnumerable<IOPointer> GetChildrenSheets(IOPointer p) => 
            p.Rank == p.Field.Rank
                ? p.Field.FrontType.GetSheetPointers(p.Name)
                : p.EnumerateIndices().Select(i => new IOPointer(p.Field, p.Rank + 1, i, V2Int.Zero, $"{p.Name} {i + 1}"));
            
        public static IEnumerable<IOPointer> GetChildren(IOPointer p) => 
            p.Rank == p.Field.Rank
                ? p.Field.FrontType.GetPointers(p.Pos)
                : p.EnumerateIndices().Select(i => new IOPointer(p.Field, p.Rank + 1, i, p.Pos.Add(p.Field.TypeOffsets[p.Rank + 1].Scale(i)), ""));
    }
}
