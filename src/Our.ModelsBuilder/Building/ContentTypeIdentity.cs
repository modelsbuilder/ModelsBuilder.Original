namespace Our.ModelsBuilder.Building
{
    /// <summary>
    /// Represents a content type identity.
    /// </summary>
    public struct ContentTypeIdentity
    {
        private ContentTypeIdentity(string value, bool isAlias)
        {
            Value = value;
            IsAlias = isAlias;
        }

        /// <summary>
        /// Gets the identity string, which is either the content type alias, or its Clr name.
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// Gets a value indicating whether the identity is the content type alias.
        /// </summary>
        public bool IsAlias { get; }

        /// <summary>
        /// Gets a value indicating whether the identity is the content type Clr name.
        /// </summary>
        public bool IsClrName => !IsAlias;

        /// <summary>
        /// Identifies a content type by its alias.
        /// </summary>
        public static ContentTypeIdentity Alias(string contentTypeAlias) => new ContentTypeIdentity(contentTypeAlias, true);

        /// <summary>
        /// Identifies a content type by its Clr name.
        /// </summary>
        public static ContentTypeIdentity ClrName(string contentTypeClrName) => new ContentTypeIdentity(contentTypeClrName, false);
    }
}