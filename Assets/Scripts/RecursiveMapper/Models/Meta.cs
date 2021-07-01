using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RecursiveMapper
{
    class Meta
    {
        internal static Meta Point => new Meta (string.Empty, new[]{typeof(string)});

        public readonly string Sheet;
        public readonly int Rank;
        public readonly ContentType ContentType;

        private readonly IReadOnlyList<Type> types;
        private readonly int[] indices;

        public Type FrontType => types[indices.Length];
        public bool IsSingleObject => Rank == indices.Length;
        public string FullName => Sheet + indices.Where (i => i > 0).Select (i => $" {i}");

        public Meta(string dimensionName, IReadOnlyList<Type> arrayTypes)
        {
            types   = arrayTypes.ToList ();
            Sheet   = dimensionName;
            indices = new int[types.Count - 1];
            Rank    = 0;

            var attribute = types.Last ().GetMappedAttribute ();
            ContentType = attribute is null
                              ? ContentType.Value
                              : attribute.IsCompact
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

        public Meta CreateChildMeta(FieldInfo field)
        {
            var tt = field.GetArrayTypes ();
            return new Meta (FullName.JoinSheetNames (tt.Last ().GetSheetName ()), tt);
        }
    }
}