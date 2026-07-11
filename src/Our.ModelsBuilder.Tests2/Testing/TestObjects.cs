using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Core;
using Umbraco.Core.Models.PublishedContent;

namespace Our.ModelsBuilder.Tests.Testing
{
    public class TestObjects
    {
        public class PublishedProperty : IPublishedProperty
        {
            private readonly IVariationContextAccessor _variationContextAccessor;

            public PublishedProperty(string alias)
            {
                Alias = alias;
            }

            public PublishedProperty(IPublishedPropertyType propertyType, IVariationContextAccessor variationContextAccessor)
            {
                Alias = propertyType.Alias;
                PropertyType = propertyType;
                _variationContextAccessor = variationContextAccessor;
            }

            public Dictionary<(string, string), object> Values { get; } = new Dictionary<(string, string), object>();

            public PublishedProperty WithValue(string culture, string segment, object value)
            {
                Values[(culture, segment)] = value;
                return this;
            }

            public IPublishedPropertyType PropertyType { get; }

            bool IPublishedProperty.HasValue(string culture, string segment)
            {
                _variationContextAccessor.ContextualizeVariation(PropertyType.Variations, ref culture, ref segment);
                return Values.ContainsKey((culture, segment));
            }

            public object GetSourceValue(string culture = null, string segment = null)
                => new NotImplementedException();

            public object GetValue(string culture = null, string segment = null)
            {
                _variationContextAccessor.ContextualizeVariation(PropertyType.Variations, ref culture, ref segment);
                return Values.TryGetValue((culture, segment), out var value) ? value : null;
            }

            public object GetXPathValue(string culture = null, string segment = null)
                => new NotImplementedException();

            public string Alias { get; }
        }

        public class PublishedContent : IPublishedContent
        {
            private readonly List<IPublishedProperty> _properties = new List<IPublishedProperty>();

            public PublishedContent(IPublishedContentType contentType)
            {
                ContentType = contentType;
            }

            public PublishedContent WithProperty(IPublishedProperty property)
            {
                _properties.Add(property);
                return this;
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
