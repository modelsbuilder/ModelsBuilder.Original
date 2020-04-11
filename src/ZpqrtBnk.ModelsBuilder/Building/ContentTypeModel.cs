using System;
using System.Collections.Generic;
using Umbraco.Core.Models;

namespace Our.ModelsBuilder.Building
{
    /// <summary>
    /// Represents a model.
    /// </summary>
    public class ContentTypeModel
    {
        #region Things that come from Umbraco

        private ContentTypeKind _kind;

        /// <summary>
        /// Gets or sets the unique identifier of the content type.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the alias of the content type.
        /// </summary>
        public string Alias { get; set; }

        /// <summary>
        /// Gets or sets the name of the content type.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the description of the content type.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the content variation of the content type.
        /// </summary>
        public ContentVariation Variations { get; set; } = ContentVariation.Nothing;

        /// <summary>
        /// Gets or sets the kind of the content type.
        /// </summary>
        public ContentTypeKind Kind
        {
            get => _kind;
            set
            {
                _kind = value switch
                {
                    ContentTypeKind.Element => value,
                    ContentTypeKind.Content => value,
                    ContentTypeKind.Media => value,
                    ContentTypeKind.Member => value,
                    _ => throw new ArgumentException("value")
                };
            }
        }

        /// <summary>
        /// Gets or sets the unique identifier of the parent content type (-1 for none).
        /// </summary>
        /// <remarks>The parent can either be a base content type, or a content types container. If the content
        /// type does not have a base content type, then returns <c>-1</c>.</remarks>
        public int ParentId { get; set; }

        /// <summary>
        /// Gets the Umbraco-declared list of properties of the content type.
        /// </summary>
        /// <remarks>
        /// <para>These are only those property that are defined locally by this content type,
        /// and not properties inherited from base nor mixin content types.</para>
        /// <para>Corresponds to the properties that a model interface must declare.</para>
        /// </remarks>
        public List<PropertyTypeModel> Properties { get; }  = new List<PropertyTypeModel>();

        /// <summary>
        /// Gets the list of expanded properties of the content type. 
        /// </summary>
        /// <remarks>
        /// <para>This is an expanded version of <see cref="Properties"/> containing also all the
        /// properties from transitive (but not parent nor inherited) mixin content types.</para>
        /// <para>Corresponds to the properties that a model class must implement.</para>
        /// </remarks>
        public List<PropertyTypeModel> ExpandedProperties { get; } = new List<PropertyTypeModel>();

        #endregion

        #region Things that are managed by ModelsBuilder

        /// <summary>
        /// Gets or sets a value indicating whether this content type should be ignored.
        /// </summary>
        public bool IsIgnored { get; set; }

        /// <summary>
        /// Gets or sets the Clr name of the content type.
        /// </summary>
        /// <remarks>This is the local name eg "Product".</remarks>
        public string ClrName { get; set; }

        /// <summary>
        /// Gets or sets the base content type, if any, otherwise null.
        /// </summary>
        /// <remarks>
        /// <para>The base content type is the parent content type, if any.</para>
        /// <para>Content types inherit from their base content type.</para>
        /// </remarks>
        public ContentTypeModel BaseContentType { get; set; }

        /// <summary>
        /// Gets or sets the name of the content type base class.
        /// </summary>
        /// <remarks>This is the full name eg "Models.Product".</remarks>
        public string BaseClassClrFullName { get; set; }

        /// <summary>
        /// Gets the Umbraco-declared list of mixins that compose the content type.
        /// </summary>
        /// <remarks>These are the mixins as declared in Umbraco. Some may be transitive,
        /// and/or inherited from the parent content type.</remarks>
        public List<ContentTypeModel> MixinContentTypes { get; } = new List<ContentTypeModel>();

        /// <summary>
        /// Gets the list of local mixin content types that compose the content type.
        /// </summary>
        /// <remarks>
        /// <para>This is a sanitized version of <see cref="MixinContentTypes"/> containing
        /// no implicit (i.e. transitive or parent or inherited) mixin content types.</para>
        /// <para>Corresponds to the list of content types that the content type model needs
        /// to declare as interfaces it implements.</para>
        /// </remarks>
        public List<ContentTypeModel> LocalMixinContentTypes { get; }  = new List<ContentTypeModel>();
        // FIXME: LocalMixinStuff or simply update MixinContentType?

        /// <summary>
        /// Gets the list of expanded mixin content types that compose the content type.
        /// </summary>
        /// <remarks>
        /// <para>This is an expanded version of <see cref="LocalMixinContentTypes"/> containing
        /// also all the transitive (but not parent nor inherited) mixin content types.</para>
        /// <para>Corresponds to the list of content types whose local properties define the properties
        /// that the content type model needs to implement.</para>
        /// </remarks>
        public List<ContentTypeModel> ExpandedMixinContentTypes { get; }  = new List<ContentTypeModel>();

        /// <summary>
        /// Gets the list of mixin properties that should be ignored because this model implements them directly.
        /// </summary>
        public List<PropertyTypeModel> IgnoredMixinProperties { get; }  = new List<PropertyTypeModel>();

        /// <summary>
        /// Gets a value indicating whether to omit generating the base class.
        /// </summary>
        public bool OmitBaseClass { get; set; }

        /// <summary>
        /// Gets a value indicating whether this model has a custom Clr name that might not match the alias.
        /// </summary>
        public bool HasCustomClrName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this content type is used as a mixin by another content type.
        /// </summary>
        public bool IsMixin { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this content type is the base model of another content type.
        /// </summary>
        public bool IsParent { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to omit generating the content type constructor.
        /// </summary>
        public bool OmitConstructor { get; set; }

        /// <summary>
        /// Gets a value indicating whether the type is an element.
        /// </summary>
        public bool IsElement => Kind == ContentTypeKind.Element;

        /// <summary>
        /// Gets a value indicating whether the type is *not* an element.
        /// </summary>
        public bool IsNotElement => !IsElement;

        #endregion

        #region Utilities

        /// <summary>
        /// Enumerates the base models starting from the current model up.
        /// </summary>
        /// <param name="andSelf">Indicates whether the enumeration should start with the current model
        /// or from its base model.</param>
        /// <returns>The base models.</returns>
        public IEnumerable<ContentTypeModel> EnumerateBaseTypes(bool andSelf = false)
        {
            var typeModel = andSelf ? this : BaseContentType;
            while (typeModel != null)
            {
                yield return typeModel;
                typeModel = typeModel.BaseContentType;
            }
        }

        #endregion
    }
}
