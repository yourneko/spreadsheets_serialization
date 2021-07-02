using System;
using System.Collections.Generic;
using System.Linq;

namespace RecursiveMapper
{
    class RequestedObject
    {
        public readonly MappedClassAttribute Type;
        public readonly string ParentName;
        public readonly int[] Index;
        public readonly IReadOnlyList<string> RequestedSheets;

        public IEnumerable<string> FullNames => Type.RequiredSheets.Select (ParentName.JoinSheetNames);

        public RequestedObject(MappedClassAttribute type, string name, params int[] indices)
        {
            Type       = type;
            ParentName = name;
            Index      = indices;

            string dimensionsStr = Index.Length == 0
                                       ? name
                                       : $"{name} {string.Join (" ", Index)}";
            RequestedSheets = type.RequiredSheets.Select (dimensionsStr.JoinSheetNames).ToArray();
        }
    }
}