using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SheetsIO
{
    /// <summary>Contains metadata of fields.</summary>
    [AttributeUsage (AttributeTargets.Field)]
    public sealed class IOFieldAttribute : Attribute
    { 
        /// <summary>Empty optional fields are valid. (Always TRUE for collections)</summary>
        public bool IsOptional;

        internal readonly IReadOnlyList<int> CollectionSize;
        internal V2Int PosInType;
        
        internal bool Initialized { get; private set; }
        internal IOMetaAttribute FrontType { get; private set; }
        internal FieldInfo Field { get; private set; }
        internal int Rank { get; private set; }
        internal IReadOnlyList<Type> ArrayTypes { get; private set; }
        internal IReadOnlyList<V2Int> TypeSizes { get; private set; }
        internal IReadOnlyList<V2Int> TypeOffsets { get; private set; }

        /// <summary>Map this field to Google Spreadsheets.</summary>
        /// <param name="fixedCollectionSize">Fixed number of elements for each rank of array.</param>
        public IOFieldAttribute(params int[] fixedCollectionSize)
        {
            CollectionSize = fixedCollectionSize;
        }

        internal void CacheMeta(FieldInfo field)
        {
            Initialized = true;
            Field       = field;
            ArrayTypes  = field.FieldType.GetArrayTypes(Math.Max(CollectionSize.Count, 2)).ToArray();
            Rank        = ArrayTypes.Count - 1;
            FrontType   = ArrayTypes[Rank].GetIOAttribute ();
            
            var sizes   = new V2Int[ArrayTypes.Count];
            sizes[Rank] = FrontType?.Size ?? new V2Int (1, 1);
            for (int i = Rank; i > 0; i--)
                sizes[i - 1] = CollectionSize.Count == 0
                                   ? new V2Int((i & 1) > 0 ? sizes[i].X : 999, 999)
                                   : sizes[i].Scale((int) Math.Pow(CollectionSize[i - 1], 1 - (i & 1)), (int) Math.Pow(CollectionSize[i - 1], i & 1));
            TypeSizes   = sizes;
            TypeOffsets = TypeSizes.Select((v2, i) => new V2Int(v2.X * (1 - (i & 1)), v2.Y * (i & 1))).ToArray();
            if (Rank > 0) 
                ValidateArrayField();
        }

        void ValidateArrayField()
        {
            IsOptional = true;
            if (!string.IsNullOrEmpty(FrontType?.SheetName)) ValidateSheetsArrayField();
            if ((FrontType?.OptionalInstance ?? false) && CollectionSize.Count > 0)
                throw new Exception($"Set array lengths for a field {Field.Name}, or add some non-optional fields to class {FrontType.Type.Name}");
        }

        void ValidateSheetsArrayField()
        {
            if (ArrayTypes[Rank - 1].IsArray && CollectionSize.Count == 0)
                throw new Exception($"Set array lengths for a field {Field.Name}, or use a collection that supports IList.Add method.");
            if (Field.GetCustomAttribute<IOPlacementAttribute>() != null)
                throw new Exception($"Remove MapPlacement attribute from a field {Field.Name}. Instances of type {FrontType.Type.Name} are placed on separate sheets.");
        }
    }
}
