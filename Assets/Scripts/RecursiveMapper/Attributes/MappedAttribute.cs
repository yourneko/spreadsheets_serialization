using System;

namespace RecursiveMapper
{
    [AttributeUsage (AttributeTargets.Field)]
    public class MappedAttribute : Attribute
    {
        internal readonly int Position;
        internal readonly int DimensionCount;

        public MappedAttribute(int position, int dimensions = 0)
        {
            DimensionCount = dimensions;
            Position       = position;
        }
    }
}