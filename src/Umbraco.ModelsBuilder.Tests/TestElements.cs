using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Models.PublishedContent;

namespace Umbraco.ModelsBuilder.Tests
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

            public bool HasValue => SourceValue != null;

            public object Value => SourceValue;

            public object XPathValue
            {
                get { throw new NotImplementedException(); }
            }

            bool IPublishedProperty.HasValue(int? languageId, string segment)
            {
                throw new NotImplementedException();
            }

            public object GetSourceValue(int? languageId = null, string segment = null)
            {
                throw new NotImplementedException();
            }

            public object GetValue(int? languageId = null, string segment = null)
            {
                throw new NotImplementedException();
            }

            public object GetXPathValue(int? languageId = null, string segment = null)
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

            public IEnumerable<IPublishedContent> Children
            {
                get { throw new NotImplementedException(); }
            }

            public PublishedContentType ContentType { get; }

            public DateTime CreateDate
            {
                get { throw new NotImplementedException(); }
            }

            public int CreatorId
            {
                get { throw new NotImplementedException(); }
            }

            public string CreatorName
            {
                get { throw new NotImplementedException(); }
            }

            public string DocumentTypeAlias
            {
                get { throw new NotImplementedException(); }
            }

            public int DocumentTypeId
            {
                get { throw new NotImplementedException(); }
            }

            public IPublishedProperty GetProperty(string alias, bool recurse)
            {
                return GetProperty(alias);
            }

            public IPublishedProperty GetProperty(string alias)
            {
                return Properties.FirstOrDefault(x => x.Alias.InvariantEquals(alias));
            }

            public int Id
            {
                get { throw new NotImplementedException(); }
            }

            public Guid Key
            {
                get { throw new NotImplementedException(); }
            }

            public bool IsDraft
            {
                get { throw new NotImplementedException(); }
            }

            public PublishedItemType ItemType
            {
                get { throw new NotImplementedException(); }
            }

            public int Level
            {
                get { throw new NotImplementedException(); }
            }

            public string Name
            {
                get { throw new NotImplementedException(); }
            }

            public IPublishedContent Parent
            {
                get { throw new NotImplementedException(); }
            }

            public string Path
            {
                get { throw new NotImplementedException(); }
            }

            public IEnumerable<IPublishedProperty> Properties => _properties;

            public int SortOrder
            {
                get { throw new NotImplementedException(); }
            }

            public int TemplateId
            {
                get { throw new NotImplementedException(); }
            }

            public DateTime UpdateDate
            {
                get { throw new NotImplementedException(); }
            }

            public string Url
            {
                get { throw new NotImplementedException(); }
            }

            public string UrlName
            {
                get { throw new NotImplementedException(); }
            }

            public Guid Version
            {
                get { throw new NotImplementedException(); }
            }

            public int WriterId
            {
                get { throw new NotImplementedException(); }
            }

            public string WriterName
            {
                get { throw new NotImplementedException(); }
            }

            public object this[string alias]
            {
                get { throw new NotImplementedException(); }
            }
        }
    }
}
