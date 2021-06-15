using System;

namespace RecursiveMapper
{
    [AttributeUsage (AttributeTargets.Class)]
    public class MappedClassAttribute : Attribute
    {
        public readonly string StartFrom;
        public readonly bool IsCompact;
        public readonly string SheetName;

        public MappedClassAttribute(string sheetName, string startFrom = "B2")
        {
            StartFrom = startFrom;
            IsCompact = true;
            SheetName = sheetName;
        }

        public MappedClassAttribute() { }
    }
}