using System;

namespace RecursiveMapper
{
    [AttributeUsage (AttributeTargets.Field)]
    public class MappedAttribute : Attribute
    {
        public int Position { get; set; }
        public int DimensionCount { get; set; }

        public MappedAttribute() { }
    }
}