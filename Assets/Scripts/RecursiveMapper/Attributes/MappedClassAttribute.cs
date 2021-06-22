using System;

namespace RecursiveMapper
{
    [AttributeUsage (AttributeTargets.Class)]
    public class MappedClassAttribute : Attribute
    {
        internal readonly bool IsCompact;
        internal readonly string SheetName;

        public MappedClassAttribute(string sheetName)  // todo - set size
        {
            IsCompact = true;
            SheetName = sheetName;
        }

        public MappedClassAttribute() { }
    }
}