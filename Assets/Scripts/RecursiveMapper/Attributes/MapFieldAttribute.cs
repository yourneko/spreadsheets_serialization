using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RecursiveMapper
{
    /// <summary>
    /// The target field can be mapped to the Google Spreadsheets.
    /// </summary>
    [AttributeUsage (AttributeTargets.Field)]
    public class MapFieldAttribute : Attribute
    {
        public readonly IReadOnlyList<int> CollectionSize;

        internal bool Initialized { get; private set; }
        internal int Rank { get; private set; }
        internal int SortOrder { get; private set; }
        internal FieldInfo Field { get; private set; }
        internal IReadOnlyList<Type> ArrayTypes { get; private set; }
        internal MapClassAttribute FrontType { get; private set; }
        internal IntRect Borders { get; private set; }

        /// <summary>
        /// The target field can be mapped to the Google Spreadsheets.
        /// </summary>
        /// <param name="fixedCollectionSize">  </param>
        public MapFieldAttribute(params int[] fixedCollectionSize)
        {
            CollectionSize = fixedCollectionSize;
        }

        internal void CacheMeta(FieldInfo field)
        {
            Initialized = true;
            Field       = field;
            var placement = field.GetCustomAttribute<MapPlacementAttribute> ();
            SortOrder    = placement?.SortOrder ?? Int32.MaxValue;

            var type = field.FieldType;
            var types = new List<Type> {type};
            while (type.MapAttribute () != null &&
                   (type = type.GetEnumeratedType()) != null)
                types.Add (type);
            ArrayTypes = types;
            Rank       = ArrayTypes.Count - 1;
            FrontType  = ArrayTypes[Rank].MapAttribute ();
        }

        internal object AddChild(object parent, int rank, object child = null)
        {
            var childToAdd = child ?? Activator.CreateInstance (ArrayTypes[rank]);
            if (rank == 0)
                Field.SetValue (parent, childToAdd);
            else
                ArrayTypes[rank].AddContent ().Invoke (parent, childToAdd);
            return childToAdd;
        }

        internal IntRect GetSize(V2Int startPos)
        {
            var v2 = FrontType?.Size ?? new V2Int (1, 1);
            var size = CollectionSize != null && CollectionSize.Count > 0
                           ? Rank <= 2 || CollectionSize.Count == Rank && CollectionSize.All (n => n > 0)
                                 ? CollectionSize.Select ((cc, i) => GetScale (cc, i & 1)).Aggregate (v2, (value, mult) => value.Scale (mult))
                                 : throw new Exception ($"Invalid collection parameters set for field {Field.Name} (type of {Field.FieldType.Name}).")
                           : Rank switch
                             {
                                 0 => v2,
                                 1 => new V2Int (v2.X, 999),
                                 2 => new V2Int (999, 999),
                                 _ => throw new Exception ("Collection with undefined size is not allowed to have Rank > 2")
                             };
            return (Borders = new IntRect (startPos.X, 0, size));
        }

        V2Int GetScale(int count, int rankIsEven) => new V2Int ((int)Math.Pow (count, rankIsEven), (int)Math.Pow (count, 1 - rankIsEven));
    }
}