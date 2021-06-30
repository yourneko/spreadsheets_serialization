using System;
using System.Collections.Generic;

namespace RecursiveMapper
{
    [AttributeUsage (AttributeTargets.Field)]
    public class MappedAttribute : Attribute
    {
        internal readonly int Position;
        internal readonly int DimensionCount;

        internal IReadOnlyList<Type> ArrayTypes;

        public MappedAttribute(int position, int dimensions = 0)
        {
            DimensionCount = dimensions;
            Position       = position;
        }
    }
}