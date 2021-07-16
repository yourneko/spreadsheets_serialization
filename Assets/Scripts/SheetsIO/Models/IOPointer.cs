using System;
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
        public bool Optional => Field.IsOptional 
                             || Rank > 0 && Field.CollectionSize.Count > 0;
        public Type TargetType => Field.ArrayTypes[Rank];
        public int MaxElements => IsArray ? Rank < Field.CollectionSize.Count ? Field.CollectionSize[Rank] : SheetsIO.MaxArrayElements : 0;

        public IOPointer(IOFieldAttribute field, int rank, int index, V2Int pos, string name)
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
                : Enumerable.Range(0, p.MaxElements).Select(i => new IOPointer(p.Field, p.Rank + 1, i, V2Int.Zero, $"{p.Name} {i + 1}"));
            
        public static IEnumerable<IOPointer> GetChildren(IOPointer p) => 
            p.Rank == p.Field.Rank
                ? p.Field.FrontType.GetPointers(p.Pos)
                : Enumerable.Range(0, p.MaxElements).Select(i => new IOPointer(p.Field, p.Rank + 1, i, p.Pos.Add(p.Field.TypeOffsets[p.Rank + 1].Scale(i)), ""));

        public override string ToString() => string.IsNullOrEmpty(Name)
                       ? $"{TargetType.FullName} [#{Index}]. Pos = ({Pos.X},{Pos.Y})"
                       : $"{TargetType.FullName} [#{Index}]. Name = {Name}"; 
    }
}
