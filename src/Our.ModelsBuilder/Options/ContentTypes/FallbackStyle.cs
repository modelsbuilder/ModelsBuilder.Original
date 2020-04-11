namespace Our.ModelsBuilder.Options.ContentTypes
{
    /// <summary>
    /// Defines the fallback generation styles.
    /// </summary>
    public enum FallbackStyle
    {
        /// <summary>
        /// Unknown.
        /// </summary>
        Unknown, // default value

        /// <summary>
        /// Does not generate fallback code.
        /// </summary>
        Nothing,

        /// <summary>
        /// Generates the classic fallback code.
        /// </summary>
        Classic,

        /// <summary>
        /// Generates the modern fallback code.
        /// </summary>
        Modern
    }
}