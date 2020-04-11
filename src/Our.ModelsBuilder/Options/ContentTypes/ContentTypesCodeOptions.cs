using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Core;

namespace Our.ModelsBuilder.Options.ContentTypes
{
    /// <summary>
    /// Represents content types code options.
    /// </summary>
    public class ContentTypesCodeOptions
    {
        /// <summary>
        /// Represents options internals.
        /// </summary>
        public class OptionsInternals
        {
            public HashSet<string> IgnoredContentTypeAliases { get; } = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
            public Dictionary<string, HashSet<string>> IgnoredPropertyTypeAliasesByAlias { get; } = new Dictionary<string, HashSet<string>>();
            public Dictionary<string, HashSet<string>> IgnoredPropertyTypeAliasesByName { get; } = new Dictionary<string, HashSet<string>>();

            public Dictionary<string, Dictionary<string, string>> PropertyTypeClrNamesByAlias { get; } = new Dictionary<string, Dictionary<string, string>>();
            public Dictionary<string, Dictionary<string, string>> PropertyTypeClrNamesByName { get; } = new Dictionary<string, Dictionary<string, string>>();

            public Dictionary<string, string> ContentTypeClrNames { get; } = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            public Dictionary<string, string> ContentTypeBaseClassClrName { get; } = new Dictionary<string, string>();
            public Dictionary<string, HashSet<string>> ContentInterfaces { get; } = new Dictionary<string, HashSet<string>>();

            public HashSet<string> OmitContentTypeConstructor { get; } = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Gets the internals.
        /// </summary>
        /// <remarks>
        /// <para>Options internals are accessed by the options builder, to set options, and but the options
        /// properties and methods, to report options. The options builder class provides a clean interface for
        /// setting options, and the options class provides a clean interface for querying options, and the
        /// internals sit in-between.</para>
        /// </remarks>
        public OptionsInternals Internals { get; } = new OptionsInternals();

        private bool _mapped;

        // FIXME move to internals
        public void MapContentTypeAliasesToClrNames(Dictionary<string, string> contentTypeAliasesToClrNames)
        {
            foreach (var (contentTypeAlias, ignoredByAlias) in Internals.IgnoredPropertyTypeAliasesByAlias)
            {
                var contentTypeClrName = contentTypeAliasesToClrNames[contentTypeAlias];
                if (!Internals.IgnoredPropertyTypeAliasesByName.TryGetValue(contentTypeClrName, out var ignoredByName))
                    ignoredByName = Internals.IgnoredPropertyTypeAliasesByName[contentTypeClrName] = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
                foreach (var ignored in ignoredByAlias)
                    ignoredByName.Add(ignored);
            }

            foreach (var (contentTypeAlias, clrNamesByAlias) in Internals.PropertyTypeClrNamesByAlias)
            {
                var contentTypeClrName = contentTypeAliasesToClrNames[contentTypeAlias];
                if (!Internals.PropertyTypeClrNamesByName.TryGetValue(contentTypeClrName, out var clrNamesByName))
                    clrNamesByName = Internals.PropertyTypeClrNamesByName[contentTypeClrName] = new Dictionary<string, string>();
                foreach (var (propertyTypeAlias, propertyTypeClrName) in clrNamesByAlias)
                    clrNamesByName[propertyTypeAlias] = propertyTypeClrName;
            }

            _mapped = true;
        }

        // TODO: more things should be protected for overwriting 

        protected Dictionary<string, string> ContentTypeClrNames => Internals.ContentTypeClrNames;

        protected Dictionary<string, Dictionary<string, string>> PropertyTypeClrNames
        {
            get
            {
                if (!_mapped)
                    throw new InvalidOperationException("Content type aliases haven't been mapped to Clr names.");
                return Internals.PropertyTypeClrNamesByName;
            }
        }

        protected Dictionary<string, HashSet<string>> IgnoredPropertyTypeAliases
        {
            get
            {
                if (!_mapped)
                    throw new InvalidOperationException("Content type aliases haven't been mapped to Clr names.");
                return Internals.IgnoredPropertyTypeAliasesByName;
            }
        }

        /// <summary>
        /// Determines whether a content type is ignored.
        /// </summary>
        /// <param name="contentTypeAlias">The alias of the content type.</param>
        public bool IsContentTypeIgnored(string contentTypeAlias)
        {
            // explicit
            if (Internals.IgnoredContentTypeAliases.Contains(contentTypeAlias)) return true;

            // wildcard
            return Internals.IgnoredContentTypeAliases
                .Where(x => x.EndsWith("*"))
                .Select(x => x.Substring(0, x.Length - 1))
                .Any(x => contentTypeAlias.StartsWith(x, StringComparison.InvariantCultureIgnoreCase));
        }

        /// <summary>
        /// Determines whether a property type is ignored.
        /// </summary>
        /// <param name="contentTypeClrName">The Clr name of the content type.</param>
        /// <param name="propertyTypeAlias">The alias of the property type.</param>
        public bool IsPropertyIgnored(string contentTypeClrName, string propertyTypeAlias)
        {
            // direct
            if (IgnoredPropertyTypeAliases.TryGetValue(contentTypeClrName, out var ignores))
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
        public string GetContentTypeClrName(string contentTypeAlias)
        {
            return ContentTypeClrNames.TryGetValue(contentTypeAlias, out var name) ? name : null;
        }

        /// <summary>
        /// Gets the explicitly provided Clr name for a property type, or null if none has been specified.
        /// </summary>
        /// <param name="contentTypeClrName">The Clr name of the content type.</param>
        /// <param name="propertyTypeAlias">The alias of the property type.</param>
        public string GetPropertyTypeClrName(string contentTypeClrName, string propertyTypeAlias)
        {
            // direct
            if (PropertyTypeClrNames.TryGetValue(contentTypeClrName, out var names)
                && names.TryGetValue(propertyTypeAlias, out var name)) return name;

            // indirect
            // FIXME: explain implementing/renaming
            if (Internals.ContentTypeBaseClassClrName.TryGetValue(contentTypeClrName, out var baseName)
                && null != (name = GetPropertyTypeClrName(baseName, propertyTypeAlias))) return name;
            if (Internals.ContentInterfaces.TryGetValue(contentTypeClrName, out var interfaceNames)
                && null != (name = interfaceNames
                    .Select(interfaceName => GetPropertyTypeClrName(interfaceName, propertyTypeAlias))
                    .FirstOrDefault(x => x != null))) return name;

            return null;
        }

        /// <summary>
        /// Determines whether to omit the content type base class.
        /// </summary>
        /// <param name="contentTypeClrName">The clr name of the content type.</param>
        public bool OmitContentTypeBaseClass(string contentTypeClrName)
        {
            return Internals.ContentTypeBaseClassClrName.ContainsKey(contentTypeClrName);
        }

        /// <summary>
        /// Gets the content type base class name.
        /// </summary>
        /// <param name="contentTypeClrName">The Clr name of the content type.</param>
        /// <returns></returns>
        public string ContentTypeBaseClass(string contentTypeClrName)
            => Internals.ContentTypeBaseClassClrName.TryGetValue(contentTypeClrName, out var name) ? name : null;

        /// <summary>
        /// Determines whether to omit the content type constructor.
        /// </summary>
        /// <param name="contentTypeClrName">The content type Clr name.</param>
        /// <returns></returns>
        public bool OmitContentTypeConstructor(string contentTypeClrName)
        {
            return Internals.OmitContentTypeConstructor.Contains(contentTypeClrName);
        }

        public virtual string ElementBaseClassClrFullName { get; set; } = "Umbraco.Core.Models.PublishedContent.PublishedElementModel";
        public virtual string ContentBaseClassClrFullName { get; set; } = "Umbraco.Core.Models.PublishedContent.PublishedContentModel";
        public virtual string ElementBaseInterfaceClrFullName { get; set; } = "Umbraco.Core.Models.PublishedContent.IPublishedElement";
        public virtual string ContentBaseInterfaceClrFullName { get; set; } = "Umbraco.Core.Models.PublishedContent.IPublishedContent";

        /// <summary>
        /// Gets or sets the source of Clr names.
        /// </summary>
        public virtual ClrNameSource ClrNameSource { get; set; } = ClrNameSource.Alias; // for legacy reasons
    }
}