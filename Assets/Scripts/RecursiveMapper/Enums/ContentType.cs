namespace RecursiveMapper
{
    enum ContentType
    {
        /// <summary>
        /// Represents a single cell content.
        /// </summary>
        Value,
        /// <summary>
        /// Represents a MapClass instance smaller than a Sheet. May contain Value, Object, HorizontalArray and VerticalArray entities.
        /// </summary>
        Object,
        /// <summary>
        /// Sheet may contain content of any type.
        /// </summary>
        Sheet,
    }
}