using System;
using System.Collections.Generic;
using Umbraco.Core.Models;
using Umbraco.Core.Models.PublishedContent;

namespace Our.ModelsBuilder.Building
{
    /// <summary>
    /// Represents a model property.
    /// </summary>
    public class PropertyTypeModel
    {
        #region Things that come from Umbraco

        /// <summary>
        /// Gets or sets the content type owning the property.
        /// </summary>
        public ContentTypeModel ContentType { get; set; }

        /// <summary>
        /// Gets or sets the alias of the property.
        /// </summary>
        public string Alias { get; set; }

        /// <summary>
        /// Gets or sets the name of the property.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the description of the property.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the content variation of the property.
        /// </summary>
        public ContentVariation Variations { get; set; } = ContentVariation.Nothing;

        /// <summary>
        /// Gets or sets the model Clr type of the property values (may be a true type, or a <see cref="ModelType"/>).
        /// </summary>
        /// <remarks>
        /// <para>As indicated by the <c>IPublishedPropertyType</c>, ie by the <c>IPropertyValueConverter</c>
        /// if any, else <c>object</c>. May include some <see cref="ModelType"/> that will need to be mapped.</para>
        /// <para>The <see cref="ValueTypeClrFullName"/> property contains the mapped name of the Clr type of values.</para>
        /// </remarks>
        public Type ValueType { get; set; }

        #endregion

        #region Things managed by ModelsBuilder

        /// <summary>
        /// Gets or sets a value indicating whether this property should be excluded from generation.
        /// </summary>
        public bool IsIgnored { get; set; }

        /// <summary>
        /// Gets or sets the Clr name of the property.
        /// </summary>
        /// <remarks>This is the local name eg "Price".</remarks>
        public string ClrName { get; set; }

        /// <summary>
        /// Gets the Clr type name of the property values.
        /// </summary>
        /// <remarks>This is the full name eg "System.DateTime".</remarks>
        public string ValueTypeClrFullName;

        /// <summary>
        /// Gets or sets the list of generation errors for the property.
        /// </summary>
        /// <remarks>This should be null, unless something prevents the property from being
        /// generated, and then the value should explain what. This can be used to generate
        /// commented out code eg in PureLive.</remarks>
        public List<string> Errors { get; set; }

        /// <summary>
        /// Adds an error.
        /// </summary>
        public void AddError(string error)
        {
            if (Errors == null) Errors = new List<string>();
            Errors.Add(error);
        }

        #endregion
    }
}
