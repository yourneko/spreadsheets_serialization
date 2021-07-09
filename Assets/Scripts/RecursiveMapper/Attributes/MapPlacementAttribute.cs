using System;

namespace SpreadsheetsMapper
{
    /// <summary>Adjusts the placement of target field content.</summary>
    /// <remarks>This attribute shouldn't be placed on fields of whole-sheet-sized type.</remarks>
    [AttributeUsage (AttributeTargets.Field)]
    public sealed class MapPlacementAttribute : Attribute
    {
        public int SortOrder;
    }
}
