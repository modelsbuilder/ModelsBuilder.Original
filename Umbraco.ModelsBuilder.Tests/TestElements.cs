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
            private readonly string _alias;
            private readonly object _dataValue;

            public PublishedProperty(string alias, object dataValue)
            {
                _alias = alias;
                _dataValue = dataValue;
            }

            public object DataValue
            {
                get { return _dataValue; }
            }

            public bool HasValue
            {
                get { return _dataValue != null; }
            }

            public string PropertyTypeAlias
            {
                get { return _alias; }
            }

            public object Value
            {
                get { return _dataValue; }
            }

            public object XPathValue
            {
                get { throw new NotImplementedException(); }
            }
        }

        public class PublishedContent : IPublishedContent
        {
            private readonly PublishedContentType _contentType;
            private readonly PublishedProperty[] _properties;

            public PublishedContent(PublishedContentType contentType, PublishedProperty[] properties)
            {
                _contentType = contentType;
                _properties = properties;
            }

            public IEnumerable<IPublishedContent> Children
            {
                get { throw new NotImplementedException(); }
            }

            public IEnumerable<IPublishedContent> ContentSet
            {
                get { throw new NotImplementedException(); }
            }

            public PublishedContentType ContentType
            {
                get { return _contentType; }
            }

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

            public int GetIndex()
            {
                throw new NotImplementedException();
            }

            public IPublishedProperty GetProperty(string alias, bool recurse)
            {
                return GetProperty(alias);
            }

            public IPublishedProperty GetProperty(string alias)
            {
                return Properties.FirstOrDefault(x => x.PropertyTypeAlias.InvariantEquals(alias));
            }

            public int Id
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

            public ICollection<IPublishedProperty> Properties
            {
                get { return _properties; }
            }

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
