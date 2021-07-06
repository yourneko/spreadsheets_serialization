using System;

namespace RecursiveMapper
{
    /// <summary>
    /// Adjusts the placement of target field content.
    /// </summary>
    /// <remarks>This attribute shouldn't be placed on fields of whole-sheet-sized type.</remarks>
    [AttributeUsage (AttributeTargets.Field)]
    public class MapPlacementAttribute : Attribute
    {
        public readonly int SortOrder;

        public MapPlacementAttribute(int sortOrder)
        {
            SortOrder = sortOrder;
        }
    }
}