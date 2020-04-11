using System.Collections.Generic;
using System.Linq;
using Moq;
using Umbraco.Core.Models;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.PropertyEditors;

namespace Our.ModelsBuilder.Tests
{
    class UmbracoInternals
    {
        public static PublishedPropertyType CreatePublishedPropertyType(string alias, int definition, string editor, ContentVariation variations = ContentVariation.Nothing)
        {
            var valueConverters = new PropertyValueConverterCollection(Enumerable.Empty<IPropertyValueConverter>());
            var publishedModelFactory = Mock.Of<IPublishedModelFactory>();
            var publishedContentTypeFactory = Mock.Of<IPublishedContentTypeFactory>();
            return new PublishedPropertyType(alias, definition, false, variations, valueConverters, publishedModelFactory, publishedContentTypeFactory);
        }

        public static PublishedContentType CreatePublishedContentType(int id, string alias, IEnumerable<PublishedPropertyType> propertyTypes)
        {
            return new PublishedContentType(id, alias, PublishedItemType.Content, Enumerable.Empty<string>(), propertyTypes, ContentVariation.Nothing);
        }
    }
}
