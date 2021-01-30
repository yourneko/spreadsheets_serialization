using System;

namespace Mimimi.SpreadsheetsSerialization
{
    [Flags]
    public enum SpaceRequired
    {
        Undefined = 0,

        /// <summary> SingleValue occupies a single cell and has no inner structure. </summary>
        /// <remarks> The DEFAULT option. </remarks>
        SingleValue = 1,

        /// <summary> Range contains either SingleValues or other Ranges. </summary>
        Range = 2,

        /// <summary> Sheets may contain Ranges and SingleValues. </summary>
        Sheet = 4,

        /// <summary> The set of sheets connected by the content. </summary>
        SheetsGroup = 8,
    }
}
