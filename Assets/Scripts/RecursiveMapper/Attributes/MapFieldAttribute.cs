using System;
using System.Collections.Generic;
using System.Reflection;

namespace RecursiveMapper
{
    /// <summary>Contains metadata of fields.</summary>
    [AttributeUsage (AttributeTargets.Field)]
    public sealed class MapFieldAttribute : Attribute
    {
        public readonly IReadOnlyList<int> CollectionSize;

        internal bool Initialized { get; private set; }
        internal FieldInfo Field { get; private set; }
        internal int SortOrder { get; private set; }
        internal int Rank { get; private set; }
        internal IReadOnlyList<Type> ArrayTypes { get; private set; }
        internal IReadOnlyList<V2Int> TypeSizes { get; private set; }
        internal MapClassAttribute FrontType { get; private set; }
        internal IntRect Borders { get; private set; }

        /// <summary>Map this field to Google Spreadsheets.</summary>
        /// <param name="fixedCollectionSize">  </param>
        public MapFieldAttribute(params int[] fixedCollectionSize)
        {
            CollectionSize = fixedCollectionSize;
        }

        internal void CacheMeta(FieldInfo field)
        {
            Initialized = true;
            Field       = field;
            SortOrder = field.GetCustomAttribute<MapPlacementAttribute> ()?.SortOrder
                     ?? (Rank == 0 || (CollectionSize?.Count ?? 0) == Rank
                             ? 1000
                             : Int32.MaxValue + Rank - 2);
            var type = field.FieldType;
            var types = new List<Type> {type};
            while (type.MapAttribute () != null && (type = type.GetEnumeratedType()) != null)
                types.Add (type);
            ArrayTypes = types;
            Rank       = ArrayTypes.Count - 1;
            FrontType  = ArrayTypes[Rank].MapAttribute ();
        }

        internal IntRect GetSize(V2Int startPos)
        {
            var sizes   = new V2Int[Rank + 1];
            sizes[Rank] = FrontType?.Size ?? new V2Int (1, 1);
            for (int i = Rank; i > 0; i--)
                sizes[i - 1] = CollectionSize.Count == 0
                                   ? sizes[i].Max(new V2Int(999, 999).GetHalf(i - 1))
                                   : sizes[i].Scale (ExtensionMethods.GetScale (CollectionSize[i - 1], i - 1));
            TypeSizes = sizes;
            return (Borders = new IntRect (startPos.X, 0, sizes[0]));
        }

        internal V2Int GetV2InArray(V2Int origin, int rank, int i) => origin.Add(TypeSizes[rank].GetHalf (rank).Scale (new V2Int (i, i)));
    }
}
