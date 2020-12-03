namespace Mimimi.SpreadsheetsSerialization
{
    public enum SpaceRequired
    {
        /// <summary> SingleValue occupies a single cell and has no inner structure. </summary>
        /// <remarks> The DEFAULT option. </remarks>
        SingleValue = 0,

        /// <summary> Range contains either SingleValues or other Ranges. </summary>
        Range = 1,

        /// <summary> Sheets may contain Ranges and SingleValues. </summary>
        Sheet = 2,

        /// <summary> The set of sheets connected by the content. </summary>
        SheetsGroup = 3,
    }
}
