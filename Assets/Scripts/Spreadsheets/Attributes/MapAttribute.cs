using System;

namespace Mimimi.SpreadsheetsSerialization
{
    [AttributeUsage (AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public class MapAttribute : Attribute
    {
        private readonly int group;
        private readonly int element;

        public MapAttribute(int _index, int _group = 0)
        {
            element = _index;
            group = _group;
        }

        public int ElementIndex => element;
        public int GroupIndex => group;
    }
}
