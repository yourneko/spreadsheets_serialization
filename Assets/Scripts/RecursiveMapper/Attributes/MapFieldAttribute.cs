using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace SpreadsheetsMapper
{
    /// <summary>Contains metadata of fields.</summary>
    [AttributeUsage (AttributeTargets.Field)]
    public sealed class MapFieldAttribute : Attribute
    {
        public readonly IReadOnlyList<int> CollectionSize;

        internal bool Initialized { get; private set; }
        internal MapClassAttribute FrontType { get; private set; }
        internal FieldInfo Field { get; private set; }
        internal int Rank { get; private set; }
        internal IReadOnlyList<Type> ArrayTypes { get; private set; }
        internal IReadOnlyList<V2Int> TypeSizes { get; private set; }
        internal IReadOnlyList<V2Int> TypeOffsets { get; private set; }

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
            ArrayTypes  = field.FieldType.GetArrayTypes(Math.Max(CollectionSize.Count, 2)).ToArray();
            Rank        = ArrayTypes.Count - 1;
            FrontType   = ArrayTypes[Rank].MapAttribute ();
            
            var sizes   = new V2Int[ArrayTypes.Count];
            sizes[Rank] = FrontType?.Size ?? new V2Int (1, 1);
            for (int i = Rank; i > 0; i--)
                sizes[i - 1] = CollectionSize.Count == 0
                                   ? sizes[i].Max(new V2Int(999 * ((i + 1) & 1), 999))
                                   : sizes[i].Scale((int) Math.Pow(CollectionSize[i - 1], 1 - (i & 1)), (int) Math.Pow(CollectionSize[i - 1], i & 1));
            TypeSizes   = sizes;
            TypeOffsets = TypeSizes.Select((v2, i) => new V2Int(v2.X * (1 - (i & 1)), v2.Y * (i & 1))).ToArray();
            MonoBehaviour.print($"Field {Field.Name} of type {Field.FieldType}: rank {Rank}, sizes {string.Join(" ", TypeSizes)}");
        }
    }
}
