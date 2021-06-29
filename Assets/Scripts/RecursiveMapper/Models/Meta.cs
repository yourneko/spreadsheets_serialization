using System;
using System.Collections.Generic;
using System.Linq;

namespace RecursiveMapper
{
    class Meta
    {
        internal static readonly Meta Point = new Meta (string.Empty, typeof(string));

        public readonly string Sheet;
        public readonly int Rank;
        public readonly ContentType ContentType;

        private readonly IReadOnlyList<Type> types;
        private readonly int[] indices;

        public Type FrontType => types[indices.Length];
        public Type UnpackType => IsSingleObject ? null : types[Rank + 1];
        public bool IsSingleObject => Rank == indices.Length;
        public string FullName => IsSingleObject && (Rank > 0)
                                      ? $"{Sheet} {string.Join (" ", indices)}"
                                      : Sheet;

        public Meta(string dimensionName, params Type[] types)
        {
            this.types = types.ToList ();
            Sheet      = dimensionName;
            indices    = new int[this.types.Count - 1];
            Rank       = 0;

            var mapRegionAttribute = this.types.Last ().GetMappedAttribute ();
            ContentType = mapRegionAttribute is null
                              ? ContentType.Value
                              : mapRegionAttribute.IsCompact
                                  ? ContentType.Object
                                  : ContentType.Sheet;
        }

        public Meta(Meta reference, int addingIndex = 0)
        {
            types       = reference.types;
            Sheet       = reference.Sheet;
            ContentType = reference.ContentType;
            indices     = new int[reference.indices.Length];
            Rank        = reference.Rank + addingIndex.CompareTo (0);
            reference.indices.CopyTo (indices, 0);
            indices[reference.Rank] = addingIndex;
        }

        public Meta CreateChildMeta(Type[] types)
        {
            var fullName = FullName;
            return new Meta (fullName.Contains ("{0}")
                                 ? string.Format (fullName, types.Last().GetSheetName())
                                 : $"{fullName} {types.Last().GetSheetName()}",
                             types);
        }
    }
}