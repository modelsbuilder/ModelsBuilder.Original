using System;
using System.Collections.Generic;
using Our.ModelsBuilder.Building;

namespace Our.ModelsBuilder.Options.ContentTypes
{
    /// <summary>
    /// Builds the <see cref="ContentTypesCodeOptions"/>.
    /// </summary>
    public class ContentTypesCodeOptionsBuilder
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ContentTypesCodeOptionsBuilder"/> class.
        /// </summary>
        public ContentTypesCodeOptionsBuilder(ContentTypesCodeOptions options)
        {
            Options = options;
        }

        /// <summary>
        /// Gets the options.
        /// </summary>
        public ContentTypesCodeOptions Options { get; }

        /// <summary>
        /// Ignores a content type.
        /// </summary>
        /// <param name="contentTypeAlias">The alias of the content type.</param>
        /// <remarks>
        /// <para>The <paramref name="contentTypeAlias"/> can end with a '*' (wildcard).</para>
        /// <para>When a content type is ignored, no model is generated for that content type,
        /// nor for any child content types, and none of its properties are generated in compositions.</para>
        /// </remarks>
        public void IgnoreContentType(string contentTypeAlias)
        {
            Options.Internals.IgnoredContentTypeAliases.Add(contentTypeAlias);
        }

        // TODO: OmitContentType = do *not* generate it, but consider it exists
        // meaning, when it's used as a parent, inherit (assume it exists)
        // and when it's used as a mixin, declare the interface and inherit the properties
        // = what we would use if the content type was fully implemented by custom code

        /// <summary>
        /// Ignores a property type.
        /// </summary>
        /// <param name="contentTypeAliasOrClrName">The alias or Clr name of the content type.</param>
        /// <param name="propertyTypeAlias">The alias of the property type.</param>
        /// <remarks>
        /// <para>The <paramref name="propertyTypeAlias"/> can end with a '*' (wildcard).</para>
        /// </remarks>
        public void IgnorePropertyType(ContentTypeIdentity contentTypeAliasOrClrName, string propertyTypeAlias)
        {
            var ignoredPropertyTypeAliases = contentTypeAliasOrClrName.IsAlias
                ? Options.Internals.IgnoredPropertyTypeAliasesByAlias
                : Options.Internals.IgnoredPropertyTypeAliasesByName;

            if (!ignoredPropertyTypeAliases.TryGetValue(contentTypeAliasOrClrName.Value, out var ignoredAliases))
                ignoredAliases = ignoredPropertyTypeAliases[contentTypeAliasOrClrName.Value] = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
            ignoredAliases.Add(propertyTypeAlias);
        }

        /// <summary>
        /// Sets the Clr name of a content type.
        /// </summary>
        /// <param name="contentTypeAlias">The alias of the content type.</param>
        /// <param name="contentTypeClrName">The Clr name of the content type.</param>
        /// <remarks>
        /// <para>The <paramref name="contentTypeClrName"/> is a simple name (no namespace) e.g. "Product".</para>
        /// <para>Setting the name explicitly overrides the Clr name generation that would otherwise take place.</para>
        /// <para>If the content type is used as a mixin, the corresponding interface has a leading "I" e.g. "IProduct".</para>
        /// </remarks>
        public void SetContentTypeClrName(string contentTypeAlias, string contentTypeClrName)
        {
            Options.Internals.ContentTypeClrNames[contentTypeAlias] = contentTypeClrName;
        }

        /// <summary>
        /// Sets the Clr name of a property type.
        /// </summary>
        /// <param name="contentTypeAliasOrClrName">The alias or Clr name of the content type.</param>
        /// <param name="propertyTypeAlias">The alias of the property type.</param>
        /// <param name="propertyTypeClrName">The Clr name of the property.</param>
        /// <remarks>
        /// <para>The <paramref name="propertyTypeClrName"/> is a simple name e.g. "Price".</para>
        /// </remarks>
        public void SetPropertyTypeClrName(ContentTypeIdentity contentTypeAliasOrClrName, string propertyTypeAlias, string propertyTypeClrName)
        {
            // this can be from code
            // or from a [ImplementPropertyType("alias")] on a property in a class
            // or from a [
            // though - soon as we have multiple implementations (property, etc) = the attribute cannot work = kill
            // FIXME but we still need to find out that it is implemented!
            // FIXME: can a *property* be defined on an interface as [ImplementPropertyType]?
            // we *are* parsing interfaces... just as if they were classes => wtf?!

            var propertyTypeClrNames = contentTypeAliasOrClrName.IsAlias
                ? Options.Internals.PropertyTypeClrNamesByAlias
                : Options.Internals.PropertyTypeClrNamesByName;

            if (!propertyTypeClrNames.TryGetValue(contentTypeAliasOrClrName.Value, out var clrNames))
                clrNames = propertyTypeClrNames[contentTypeAliasOrClrName.Value] = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            clrNames[propertyTypeAlias] = propertyTypeClrName;
        }

        public void ContentTypeModelClassHasProperty(string contentTypeClrName, string propertyClrName)
        {
            // FIXME: detect that a property is implemented, in the class
            // and then, no need to generate an implementation for it
        }

        public void ContentTypeModelInterfaceHasProperty(string contentTypeInterfaceClrName, string propertyClrName)
        {
            // FIXME: detect that a property is declared, in the interface
            // and then, no need to generate a declaration for it

            // but! we could detect
            // - the property ie Prop => Value("alias", culture)
            // - the method ie Prop(string culture = null) => Value("alias", culture);
        }

        /// <summary>
        /// Specifies that the content type model already has a base class,
        /// and none should be generated.
        /// </summary>
        /// <param name="contentTypeClrName">The content type Clr name.</param>
        /// <param name="baseClassClrName">The base class Clr name.</param>
        public void ContentTypeModelHasBaseClass(string contentTypeClrName, string baseClassClrName)
        {
            Options.Internals.ContentTypeBaseClassClrName[contentTypeClrName] = baseClassClrName;
        }

        /// <summary>
        /// Specifies that the content type model already declares an interface,
        /// which should not be declared nor implemented.
        /// </summary>
        /// <param name="contentTypeClrName">The content type Clr name.</param>
        /// <param name="interfaceClrName">The Clr name of the interface.</param>
        public void ContentTypeModelHasInterface(string contentTypeClrName, string interfaceClrName)
        {
            // FIXME: usage? + we use it for the class & the interface = ?
            if (!Options.Internals.ContentInterfaces.TryGetValue(contentTypeClrName, out var contentInterfaces))
                contentInterfaces = Options.Internals.ContentInterfaces[contentTypeClrName] = new HashSet<string>();
            contentInterfaces.Add(interfaceClrName);
        }

        /// <summary>
        /// Specifies that the content type model already has a class constructor,
        /// and none should be generated.
        /// </summary>
        /// <param name="contentTypeClrName">The content type Clr name.</param>
        public void ContentTypeModelHasConstructor(string contentTypeClrName)
        {
            Options.Internals.OmitContentTypeConstructor.Add(contentTypeClrName);
        }
    }
}