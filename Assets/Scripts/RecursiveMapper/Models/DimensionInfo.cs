namespace RecursiveMapper
{
    class DimensionInfo
    {
        internal static readonly DimensionInfo Point = new DimensionInfo (ContentType.Value,  string.Empty);

        public readonly string Sheet;
        public readonly ContentType ContentType;
        public readonly int[] Indices;

        public DimensionInfo(ContentType type, string sheet, params int[] indices)
        {
            ContentType = type;
            Sheet       = sheet;
            Indices     = indices;
        }

        public bool IsCompact => (int)ContentType < 5;

        public DimensionInfo Copy()
        {
            return new DimensionInfo (ContentType, Sheet, Indices);
        }
    }
}