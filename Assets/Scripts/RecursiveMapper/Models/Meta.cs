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
        public bool IsObject => Rank == indices.Length;
        public string FullName => IsObject && (Rank > 0)
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
            Rank        = reference.Rank;
            ContentType = reference.ContentType;

            indices = new int[reference.indices.Length];
            if (addingIndex > 0)
            {
                Rank          += 1;
                indices[Rank] =  addingIndex;
            }

            for (int i = 0; i < Rank; i++)
                indices[i] = reference.indices[i];
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