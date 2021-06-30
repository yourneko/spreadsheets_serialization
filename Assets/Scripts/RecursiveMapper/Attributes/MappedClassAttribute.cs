using System;
using System.Collections.Generic;
using System.Reflection;

namespace RecursiveMapper
{
    [AttributeUsage (AttributeTargets.Class)]
    public class MappedClassAttribute : Attribute
    {
        internal readonly bool IsCompact;
        internal readonly string SheetName;

        internal IList<FieldInfo> Fields;

        public MappedClassAttribute(string sheetName)
        {
            IsCompact = false;
            SheetName = sheetName;
        }

        public MappedClassAttribute()
        {
            IsCompact = true;
        }
    }
}