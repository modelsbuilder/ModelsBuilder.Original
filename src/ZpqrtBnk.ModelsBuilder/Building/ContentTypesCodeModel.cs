using System.Collections.Generic;
using Our.ModelsBuilder.Options.ContentTypes;

namespace Our.ModelsBuilder.Building
{
    /// <summary>
    /// Represents a model of the content types code to generate.
    /// </summary>
    public class ContentTypesCodeModel
    {
        // FIXME
        public List<ContentTypeModel> ContentTypes { get; set; }

        /// <summary>
        /// Gets or sets the fallback generation style.
        /// </summary>
        // TODO: could this be per content/property type?
        public FallbackStyle FallbackStyle { get; set; } = FallbackStyle.Modern;

        /// <summary>
        /// Gets or sets the property generation style.
        /// </summary>
        // TODO: could this be per content/property type?
        public PropertyStyle PropertyStyle { get; set; } = PropertyStyle.Methods;

        public string ElementBaseClassClrFullName { get; set; } = "Umbraco.Core.Models.PublishedContent.PublishedElementModel";
        public string ContentBaseClassClrFullName { get; set; } = "Umbraco.Core.Models.PublishedContent.PublishedContentModel";
        public string ElementBaseInterfaceClrFullName { get; set; } = "Umbraco.Core.Models.PublishedContent.IPublishedElement";
        public string ContentBaseInterfaceClrFullName { get; set; } = "Umbraco.Core.Models.PublishedContent.IPublishedContent";
    }
}