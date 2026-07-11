namespace Our.ModelsBuilder.Options;

internal static class ModelsOptionsExtensions
{
    /// <summary>
    /// Determines whether a content type is ignored.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <param name="contentTypeAlias">The alias of the content type.</param>
    public static bool IsContentTypeIgnored(this ModelsOptions options, string contentTypeAlias)
    {
        // explicit
        if (options.IgnoredContentTypeAliases.Contains(contentTypeAlias)) return true;

        // wildcard
        return options.IgnoredContentTypeAliases
            .Where(x => x.EndsWith("*"))
            .Select(x => x.Substring(0, x.Length - 1))
            .Any(x => contentTypeAlias.StartsWith(x, StringComparison.InvariantCultureIgnoreCase));
    }

    /// <summary>
    /// Determines whether a property type is ignored.
    /// </summary>
    /// <param name="contentTypeClrName">The Clr name of the content type.</param>
    /// <param name="propertyTypeAlias">The alias of the property type.</param>
    public static bool IsPropertyIgnored(this ModelsOptions options, string contentTypeClrName, string propertyTypeAlias)
    {
        // direct
        if (options.IgnoredPropertyTypeAliases.TryGetValue(contentTypeClrName, out var ignores))
        {
            // explicit
            if (ignores.Contains(propertyTypeAlias)) return true;

            // wildcard
            if (ignores
                .Where(x => x.EndsWith("*"))
                .Select(x => x.Substring(0, x.Length - 1))
                .Any(x => propertyTypeAlias.StartsWith(x, StringComparison.InvariantCultureIgnoreCase)))
                return true;
        }

        // indirect
        // FIXME: this should not happen here but in Apply - and maybe not at all
        /*
        // can be ignored: on a parent, recursively, or on an interface, recursively

        if (_contentTypeBaseClassClrName.TryGetValue(contentTypeClrName, out var baseClassClrname)
            && IsPropertyIgnored(baseClassClrname, propertyTypeAlias)) return true;

        if (_contentInterfaces.TryGetValue(contentTypeClrName, out var interfaceNames)
            && interfaceNames.Any(interfaceName => IsPropertyIgnored(interfaceName, propertyTypeAlias))) return true;
        */
        // not ignored
        return false;
    }

    /// <summary>
    /// Gets the explicitly provided Clr name for a content type, or null if none has been specified.
    /// </summary>
    /// <param name="contentTypeAlias">The alias of the content type.</param>
    public static string GetContentTypeClrName(this ModelsOptions options, string contentTypeAlias)
    {
        return options.ContentTypeClrNames.TryGetValue(contentTypeAlias, out var name) ? name : null;
    }

    /// <summary>
    /// Gets the explicitly provided Clr name for a property type, or null if none has been specified.
    /// </summary>
    /// <param name="contentTypeClrName">The Clr name of the content type.</param>
    /// <param name="propertyTypeAlias">The alias of the property type.</param>
    public static string GetPropertyTypeClrName(this ModelsOptions options, string contentTypeClrName, string propertyTypeAlias)
    {
        // direct
        if (options.PropertyTypeClrNames.TryGetValue(contentTypeClrName, out var names)
            && names.TryGetValue(propertyTypeAlias, out var name)) return name;

        // indirect
        // FIXME: explain implementing/renaming
        if (options.ContentTypeBaseClassClrName.TryGetValue(contentTypeClrName, out var baseName)
            && null != (name = options.GetPropertyTypeClrName(baseName, propertyTypeAlias))) return name;
        if (options.ContentInterfaces.TryGetValue(contentTypeClrName, out var interfaceNames)
                    && null != (name = interfaceNames
                        .Select(interfaceName => options.GetPropertyTypeClrName(interfaceName, propertyTypeAlias))
                        .FirstOrDefault(x => x != null))) return name;

        return null;
    }

    /// <summary>
    /// Determines whether to omit the content type base class.
    /// </summary>
    /// <param name="contentTypeClrName">The clr name of the content type.</param>
    public static bool OmitContentTypeBaseClass(this ModelsOptions options, string contentTypeClrName)
    {
        return options.ContentTypeBaseClassClrName.ContainsKey(contentTypeClrName);
    }

    /// <summary>
    /// Gets the content type base class name.
    /// </summary>
    /// <param name="contentTypeClrName">The Clr name of the content type.</param>
    /// <returns></returns>
    public static string ContentTypeBaseClass(this ModelsOptions options, string contentTypeClrName)
        => options.ContentTypeBaseClassClrName.TryGetValue(contentTypeClrName, out var name) ? name : null;

    /// <summary>
    /// Determines whether to omit the content type constructor.
    /// </summary>
    /// <param name="contentTypeClrName">The content type Clr name.</param>
    /// <returns></returns>
    public static bool OmitContentTypeConstructor(this ModelsOptions options, string contentTypeClrName)
    {
        return options.OmitContentTypeConstructor.Contains(contentTypeClrName);
    }

}