namespace RecursiveMapper
{
    class DimensionInfo
    {
        internal static readonly DimensionInfo Point = new DimensionInfo (ContentType.Value);
        internal static readonly DimensionInfo None = new DimensionInfo (ContentType.None);

        public readonly string Sheet;
        public readonly ContentType ContentType;

        public DimensionInfo(ContentType type, string sheet = "")
        {
            ContentType = type;
            Sheet       = sheet;
        }
    }
}