using System.Collections.Generic;
using System.Linq;

namespace RecursiveMapper
{
    class RequestedObject
    {
        List<string> sheets;
        public readonly MappedClassAttribute Type;
        public readonly string ParentName, OwnName;
        public readonly int[] Index;

        public IEnumerable<string> FullNames => sheets ??= Type.RequiredSheets.Select (OwnName.JoinSheetNames).ToList ();

        public RequestedObject(MappedClassAttribute type, string name, params int[] indices)
        {
            Type       = type;
            ParentName = name;
            Index      = indices;
            OwnName    = Index.Length == 0 ? name : $"{name} {string.Join (" ", Index)}";
        }
    }
}