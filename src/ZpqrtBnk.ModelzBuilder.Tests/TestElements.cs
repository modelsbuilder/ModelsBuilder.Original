using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Models.PublishedContent;

namespace ZpqrtBnk.ModelzBuilder.Tests
{
    class TestElements
    {
        public class PublishedProperty : IPublishedProperty
        {
            public PublishedProperty(string alias, object sourceValue)
            {
                Alias = alias;
                SourceValue = sourceValue;
            }

            public object SourceValue { get; }

            public IPublishedPropertyType PropertyType => throw new NotImplementedException();

            bool IPublishedProperty.HasValue(string culture, string segment) => SourceValue != null;

            public object GetSourceValue(string culture = null, string segment = null)
            {
                throw new NotImplementedException();
            }

            public object GetValue(string culture = null, string segment = null) => SourceValue;

            public object GetXPathValue(string culture = null, string segment = null)
            {
                throw new NotImplementedException();
            }

            public string Alias { get; }
        }

        public class PublishedContent : IPublishedContent
        {
            private readonly PublishedProperty[] _properties;

            public PublishedContent(PublishedContentType contentType, PublishedProperty[] properties)
            {
                ContentType = contentType;
                _properties = properties;
            }

            public IEnumerable<IPublishedContent> Children => throw new NotImplementedException();

            public IEnumerable<IPublishedContent> ChildrenForAllCultures => throw new NotImplementedException();

            public IPublishedContentType ContentType { get; }

            public DateTime CreateDate => throw new NotImplementedException();

            public int CreatorId => throw new NotImplementedException();

            public string CreatorName => throw new NotImplementedException();

            public IPublishedProperty GetProperty(string alias)
            {
                return Properties.FirstOrDefault(x => x.Alias.InvariantEquals(alias));
            }

            public string GetUrl(string culture = null)
            {
                throw new NotImplementedException();
            }

            public PublishedCultureInfo GetCulture(string culture = null)
            {
                throw new NotImplementedException();
            }

            public bool IsPublished(string culture = null)
            {
                throw new NotImplementedException();
            }

            public int Id => throw new NotImplementedException();

            public Guid Key => throw new NotImplementedException();

            public bool IsDraft(string culture) => throw new NotImplementedException();

            public IReadOnlyDictionary<string, PublishedCultureInfo> Cultures { get; }
            public PublishedItemType ItemType => throw new NotImplementedException();

            public int Level => throw new NotImplementedException();

            public string Name => throw new NotImplementedException();
            public string UrlSegment { get; }

            public IPublishedContent Parent => throw new NotImplementedException();

            public string Path => throw new NotImplementedException();

            public IEnumerable<IPublishedProperty> Properties => _properties;

            public int SortOrder => throw new NotImplementedException();

            public int? TemplateId => throw new NotImplementedException();

            public DateTime UpdateDate => throw new NotImplementedException();

            public string Url => throw new NotImplementedException();

            public int WriterId => throw new NotImplementedException();

            public string WriterName => throw new NotImplementedException();

            public object this[string alias] => throw new NotImplementedException();
        }
    }
}
