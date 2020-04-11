namespace Our.ModelsBuilder.Options.ContentTypes
{
    /// <summary>
    /// Defines the property generation styles.
    /// </summary>
    public enum PropertyStyle
    {
        /// <summary>
        /// Unknown.
        /// </summary>
        Unknown, // default value

        /// <summary>
        /// Generate class properties.
        /// </summary>
        Property,

        /// <summary>
        /// Generate class properties and extension methods.
        /// </summary>
        PropertyAndExtensionMethods,

        /// <summary>
        /// Generate extension methods.
        /// </summary>
        ExtensionMethods,

        /// <summary>
        /// Generate class methods.
        /// </summary>
        Methods
    }
}