using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LightInject;
using Moq;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.PropertyEditors;
using Umbraco.Web.Composing;

namespace Umbraco.ModelsBuilder.Tests
{
    class UmbracoInternals
    {
        public static PublishedPropertyType CreatePublishedPropertyType(string alias, int definition, string editor)
        {
            var valueConverters = new PropertyValueConverterCollection(Enumerable.Empty<IPropertyValueConverter>());
            var publishedModelFactory = Mock.Of<IPublishedModelFactory>();
            var publishedContentTypeFactory = Mock.Of<IPublishedContentTypeFactory>();
            return new PublishedPropertyType(alias, definition, false, ContentVariation.InvariantNeutral, valueConverters, publishedModelFactory, publishedContentTypeFactory);
            //return Ctor<PublishedPropertyType>(alias, definition, editor, false);
        }

        public static PublishedContentType CreatePublishedContentType(int id, string alias, IEnumerable<PublishedPropertyType> propertyTypes)
        {
            return new PublishedContentType(id, alias, PublishedItemType.Content, Enumerable.Empty<string>(), propertyTypes, ContentVariation.InvariantNeutral);
            //return Ctor<PublishedContentType>(id, alias, propertyTypes);
        }
    }
}
