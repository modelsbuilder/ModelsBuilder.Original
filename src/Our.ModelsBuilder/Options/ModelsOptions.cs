using Our.ModelsBuilder.Building;

namespace Our.ModelsBuilder.Options;

/// <summary>
/// Represents the models options.
/// </summary>
internal class ModelsOptions
{
    public HashSet<string> IgnoredContentTypeAliases { get; } = new(StringComparer.InvariantCultureIgnoreCase);
    public Dictionary<string, HashSet<string>> IgnoredPropertyTypeAliasesByAlias { get; } = new();
    public Dictionary<string, HashSet<string>> IgnoredPropertyTypeAliasesByName { get; } = new();

    public Dictionary<string, Dictionary<string, string>> PropertyTypeClrNamesByAlias { get; } = new();
    public Dictionary<string, Dictionary<string, string>> PropertyTypeClrNamesByName { get; } = new();

    public Dictionary<string, string> ContentTypeClrNames { get; } = new(StringComparer.InvariantCultureIgnoreCase);
    public Dictionary<string, string> ContentTypeBaseClassClrName { get; } = new();
    public Dictionary<string, HashSet<string>> ContentInterfaces { get; } = new();

    public HashSet<string> OmitContentTypeConstructor { get; } = new(StringComparer.InvariantCultureIgnoreCase);

    /// <summary>
    /// Ignores a content type.
    /// </summary>
    /// <param name="contentTypeAlias">The alias of the content type.</param>
    /// <remarks>
    /// <para>The <paramref name="contentTypeAlias"/> can end with a '*' (wildcard).</para>
    /// <para>When a content type is ignored, no model is generated for that content type,
    /// nor for any child content types, and none of its properties are generated in compositions.</para>
    /// </remarks>
    public ModelsOptions IgnoreContentType(string contentTypeAlias)
    {
        IgnoredContentTypeAliases.Add(contentTypeAlias);
        return this;
    }

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
            ? IgnoredPropertyTypeAliasesByAlias
            : IgnoredPropertyTypeAliasesByName;

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
        ContentTypeClrNames[contentTypeAlias] = contentTypeClrName;
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
            ? PropertyTypeClrNamesByAlias
            : PropertyTypeClrNamesByName;

        if (!propertyTypeClrNames.TryGetValue(contentTypeAliasOrClrName.Value, out var clrNames))
            clrNames = propertyTypeClrNames[contentTypeAliasOrClrName.Value] = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        clrNames[propertyTypeAlias] = propertyTypeClrName;
    }

    public void SetModelClassHasProperty(string contentTypeClrName, string propertyClrName)
    {
        // FIXME: detect that a property is implemented, in the class
        // and then, no need to generate an implementation for it
    }

    public void SetModelInterfaceHasProperty(string contentTypeInterfaceClrName, string propertyClrName)
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
    public void SetModelClassHasBaseClass(string contentTypeClrName, string baseClassClrName)
    {
        ContentTypeBaseClassClrName[contentTypeClrName] = baseClassClrName;
    }

    /// <summary>
    /// Specifies that the content type model already declares an interface,
    /// which should not be declared nor implemented.
    /// </summary>
    /// <param name="contentTypeClrName">The content type Clr name.</param>
    /// <param name="interfaceClrName">The Clr name of the interface.</param>
    public void SetModelClassHasInterface(string contentTypeClrName, string interfaceClrName)
    {
        // FIXME: usage? + we use it for the class & the interface = ?
        if (!ContentInterfaces.TryGetValue(contentTypeClrName, out var contentInterfaces))
            contentInterfaces = ContentInterfaces[contentTypeClrName] = new HashSet<string>();
        contentInterfaces.Add(interfaceClrName);
    }

    /// <summary>
    /// Specifies that the content type model already has a class constructor,
    /// and none should be generated.
    /// </summary>
    /// <param name="contentTypeClrName">The content type Clr name.</param>
    public void SetModelClassHasConstructor(string contentTypeClrName)
    {
        OmitContentTypeConstructor.Add(contentTypeClrName);
    }

    // ---

    // FIXME all this and maybe more above should move to _internals
    // code should have internal extension methods for all this, accessing internal _internals

    private bool _mapped;

    public void MapContentTypeAliasesToClrNames(Dictionary<string, string> contentTypeAliasesToClrNames)
    {
        foreach (var (contentTypeAlias, ignoredByAlias) in IgnoredPropertyTypeAliasesByAlias)
        {
            var contentTypeClrName = contentTypeAliasesToClrNames[contentTypeAlias];
            if (!IgnoredPropertyTypeAliasesByName.TryGetValue(contentTypeClrName, out var ignoredByName))
                ignoredByName = IgnoredPropertyTypeAliasesByName[contentTypeClrName] = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
            foreach (var ignored in ignoredByAlias)
                ignoredByName.Add(ignored);
        }

        foreach (var (contentTypeAlias, clrNamesByAlias) in PropertyTypeClrNamesByAlias)
        {
            var contentTypeClrName = contentTypeAliasesToClrNames[contentTypeAlias];
            if (!PropertyTypeClrNamesByName.TryGetValue(contentTypeClrName, out var clrNamesByName))
                clrNamesByName = PropertyTypeClrNamesByName[contentTypeClrName] = new Dictionary<string, string>();
            foreach (var (propertyTypeAlias, propertyTypeClrName) in clrNamesByAlias)
                clrNamesByName[propertyTypeAlias] = propertyTypeClrName;
        }

        _mapped = true;
    }

    // TODO: more things should be protected for overwriting 

    public Dictionary<string, Dictionary<string, string>> PropertyTypeClrNames
    {
        get
        {
            if (!_mapped)
                throw new InvalidOperationException("Content type aliases haven't been mapped to Clr names.");
            return PropertyTypeClrNamesByName;
        }
    }

    public Dictionary<string, HashSet<string>> IgnoredPropertyTypeAliases
    {
        get
        {
            if (!_mapped)
                throw new InvalidOperationException("Content type aliases haven't been mapped to Clr names.");
            return IgnoredPropertyTypeAliasesByName;
        }
    }


    public virtual string ElementBaseClassClrFullName { get; set; } = "Umbraco.Cms.Core.Models.PublishedContent.PublishedElementModel";
    public virtual string ContentBaseClassClrFullName { get; set; } = "Umbraco.Cms.Core.Models.PublishedContent.PublishedContentModel";
    public virtual string ElementBaseInterfaceClrFullName { get; set; } = "Umbraco.Cms.Core.Models.PublishedContent.IPublishedElement";
    public virtual string ContentBaseInterfaceClrFullName { get; set; } = "Umbraco.Cms.Core.Models.PublishedContent.IPublishedContent";

    ///// <summary>
    ///// Gets or sets the source of Clr names.
    ///// </summary>
    //public virtual ClrNameSource ClrNameSource { get; set; } = ClrNameSource.Alias; // for legacy reasons
}