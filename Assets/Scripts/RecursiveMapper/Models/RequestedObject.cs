using System;
using System.Collections.Generic;
using System.Linq;

namespace RecursiveMapper
{
    class RequestedObject
    {
        List<string> sheets;
        public readonly MapClassAttribute Type;
        public readonly string ParentName, OwnName;
        public readonly int[] Index;

        public object MatchingSheets;
        public IReadOnlyList<Type> ArrayTypes;

        public IEnumerable<string> FullNames => sheets ??= Type.RequiredSheets.Select (OwnName.JoinSheetNames).ToList ();

        public RequestedObject(MapClassAttribute type, string name, params int[] indices)
        {
            Type       = type;
            ParentName = name;
            Index      = indices;
            OwnName    = Index.Length == 0 ? name : $"{name} {string.Join (" ", Index)}";
        }
    }
}