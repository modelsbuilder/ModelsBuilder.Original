using System;

namespace Umbraco.ModelsBuilder.Building
{
    /// <summary>
    /// Represents a model property.
    /// </summary>
    internal class PropertyModel
    {
        /// <summary>
        /// Gets the alias of the property.
        /// </summary>
        public string Alias;

        /// <summary>
        /// Gets the name of the property.
        /// </summary>
        public string Name;

        /// <summary>
        /// Gets the description of the property.
        /// </summary>
        public string Description;

        /// <summary>
        /// Gets the clr name of the property.
        /// </summary>
        /// <remarks>This is just the local name eg "Price".</remarks>
        public string ClrName;

        /// <summary>
        /// Gets the CLR type of the property values.
        /// </summary>
        /// <remarks>As indicated by the <c>PublishedPropertyType</c>, ie by the <c>IPropertyValueConverter</c>
        /// if any, else <c>object</c>.</remarks>
        public Type ClrType;

        /// <summary>
        /// Gets a value indicating whether this property should be excluded from generation.
        /// </summary>
        public bool IsIgnored;
    }
}
