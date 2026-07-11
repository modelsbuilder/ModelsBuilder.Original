using System.ComponentModel;
using Microsoft.CodeAnalysis.CSharp;
using Our.ModelsBuilder.Building;

namespace Our.ModelsBuilder.Options;

/// <summary>
/// Represents the models builder options.
/// </summary>
public class ModelsBuilderOurOptions
{
    /// <summary>
    /// Defines default values for options.
    /// </summary>
    private static class DefaultValues
    {
        public const LanguageVersion LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp11;
        public const ClrNameSource ClrNameSource = Options.ClrNameSource.Alias;
        public const FallbackStyle FallbackStyle = Options.FallbackStyle.Modern;
        public const PropertyStyle PropertyStyle = Options.PropertyStyle.Methods;
    }

    /// <summary>
    /// Gets the models options.
    /// </summary>
    internal ModelsOptions ModelsOptions { get; } = new();

    /// <summary>
    /// Gets or sets the Roslyn parser language version.
    /// </summary>
    [DefaultValue(DefaultValues.LanguageVersion)]
    public LanguageVersion LanguageVersion { get; set; } = DefaultValues.LanguageVersion;

    /// <summary>
    /// Sets the Roslyn parser language version.
    /// </summary>
    /// <param name="languageVersion">The Roslyn parser language version.</param>
    /// <returns>These options.</returns>
    public ModelsBuilderOurOptions SetLanguageVersion(LanguageVersion languageVersion)
    {
        LanguageVersion = languageVersion;
        return this;
    }

    /// <summary>
    /// Gets or sets the CLR name source.
    /// </summary>
    [DefaultValue(DefaultValues.ClrNameSource)]
    public ClrNameSource ClrNameSource { get; set; } = DefaultValues.ClrNameSource;

    /// <summary>
    /// Sets the CLR name source.
    /// </summary>
    /// <param name="clrNameSource">The CLR name source.</param>
    /// <returns>These options.</returns>
    public ModelsBuilderOurOptions SetClrNameSource(ClrNameSource clrNameSource)
    {
        ClrNameSource = clrNameSource;
        return this;
    }

    /// <summary>
    /// Gets or sets the fallback style.
    /// </summary>
    [DefaultValue(DefaultValues.FallbackStyle)]
    public FallbackStyle FallbackStyle { get; set; } = DefaultValues.FallbackStyle;

    /// <summary>
    /// Sets the fallback style.
    /// </summary>
    /// <param name="fallbackStyle">The fallback style.</param>
    /// <returns>These options.</returns>
    public ModelsBuilderOurOptions SetFallbackStyle(FallbackStyle fallbackStyle)
    {
        FallbackStyle = fallbackStyle;
        return this;
    }

    /// <summary>
    /// Gets or sets the property style.
    /// </summary>
    [DefaultValue(DefaultValues.PropertyStyle)]
    public PropertyStyle PropertyStyle { get; set; } = DefaultValues.PropertyStyle;

    /// <summary>
    /// Sets the property style.
    /// </summary>
    /// <param name="propertyStyle">The property style.</param>
    /// <returns>These options.</returns>
    public ModelsBuilderOurOptions SetPropertyStyle(PropertyStyle propertyStyle)
    {
        PropertyStyle = propertyStyle;
        return this;
    }

    /// <summary>
    /// Ignores a content type.
    /// </summary>
    /// <param name="contentTypeAlias">The alias of the content type.</param>
    /// <remarks>
    /// <para>The <paramref name="contentTypeAlias"/> can end with a '*' (wildcard).</para>
    /// <para>When a content type is ignored, no model is generated for that content type,
    /// nor for any child content types, and none of its properties are generated in compositions.</para>
    /// </remarks>
    public ModelsBuilderOurOptions IgnoreContentType(string contentTypeAlias)
    {
        ModelsOptions.IgnoreContentType(contentTypeAlias);
        return this;
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
    public ModelsBuilderOurOptions IgnorePropertyType(ContentTypeIdentity contentTypeAliasOrClrName, string propertyTypeAlias)
    {
        ModelsOptions.IgnorePropertyType(contentTypeAliasOrClrName, propertyTypeAlias);
        return this;
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
    public ModelsBuilderOurOptions SetContentTypeClrName(string contentTypeAlias, string contentTypeClrName)
    {
        ModelsOptions.SetContentTypeClrName(contentTypeAlias, contentTypeClrName);
        return this;
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
    public ModelsBuilderOurOptions SetPropertyTypeClrName(ContentTypeIdentity contentTypeAliasOrClrName, string propertyTypeAlias, string propertyTypeClrName)
    {
        ModelsOptions.SetPropertyTypeClrName(contentTypeAliasOrClrName, propertyTypeAlias, propertyTypeClrName);
        return this;
    }
}
