using System;

namespace RecursiveMapper
{
    [AttributeUsage (AttributeTargets.Class)]
    public class MappedClassAttribute : Attribute
    {
        internal readonly bool IsCompact;
        internal readonly string SheetName;

        public MappedClassAttribute(string sheetName, bool isCompact)
        {
            IsCompact = isCompact;
            SheetName = sheetName;
        }

        public MappedClassAttribute() { }
    }
}