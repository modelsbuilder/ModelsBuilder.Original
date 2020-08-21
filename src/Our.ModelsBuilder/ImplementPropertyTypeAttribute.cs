using System;

namespace Our.ModelsBuilder
{
    /// <summary>
    /// Indicates that a property implements a given property alias.
    /// </summary>
    /// <remarks>And therefore it should not be generated.</remarks>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class ImplementPropertyTypeAttribute : Attribute
    {
        public ImplementPropertyTypeAttribute(string propertyTypeAlias)
        {
            PropertyTypeAlias = propertyTypeAlias;
        }

        public ImplementPropertyTypeAttribute(string contentTypeAlias, string propertyTypeAlias)
            : this(propertyTypeAlias)
        {
            ContentTypeAlias = contentTypeAlias;
        }

        public string ContentTypeAlias { get; }

        public string PropertyTypeAlias { get; }
    }
}
