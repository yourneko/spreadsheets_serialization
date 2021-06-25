namespace RecursiveMapper
{
    enum ContentType
    {
        /// <summary>
        /// Represents an invalid content type.
        /// </summary>
        None = 0,
        /// <summary>
        /// Represents a single cell content.
        /// </summary>
        Value = 1,
        /// <summary>
        /// Represents a MapClass instance smaller than a Sheet. May contain Value, Object, HorizontalArray and VerticalArray entities.
        /// </summary>
        Object = 2,
        /// <summary>
        /// Sheet may contain content of any type.
        /// </summary>
        Sheet = 3,
    }
}