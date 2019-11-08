using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Core.Strings;
using ZpqrtBnk.ModelsBuilder.Configuration;

namespace ZpqrtBnk.ModelsBuilder.Building
{
    /// <summary>
    /// Provides a base class for <see cref="IBuilder"/> implementations.
    /// </summary>
    public class Builder : IBuilder
    {
        internal string ModelsNamespaceForTests;

        private string GetModelsNamespace(Config config, ParseResult parseResult, string modelsNamespace)
        {
            // test namespace overrides everything
            if (ModelsNamespaceForTests != null)
                return ModelsNamespaceForTests;

            // code attribute overrides everything
            if (parseResult.HasModelsNamespace)
                return parseResult.ModelsNamespace;

            // if builder was initialized with a namespace, use that one
            if (!string.IsNullOrWhiteSpace(modelsNamespace))
                return modelsNamespace;

            // default
            return config.ModelsNamespace;
        }

        private static ISet<string> GetUsing() => new HashSet<string>
        {
            // initialize with default values
            "System",
            "System.Collections.Generic",
            "System.Linq.Expressions",
            "System.Web",
            "Umbraco.Core.Models",
            "Umbraco.Core.Models.PublishedContent",
            "Umbraco.Web",
            "ZpqrtBnk.ModelsBuilder",
            "ZpqrtBnk.ModelsBuilder.Umbraco",
        };

        public CodeContext Build(Config config, ParseResult parseResult, string modelsNamespace, CodeModels models)
        {
            var context = new CodeContext
            {
                // general properties
                ModelInfosClassName = parseResult.ModelInfoClassName,
                ModelInfosClassNamespace = parseResult.ModelInfoClassNamespace,

                // type models
                GeneratePropertyGetters = parseResult.GeneratePropertyGetters,
                GenerateFallbackFuncExtensionMethods = parseResult.GenerateFallbackFuncExtensionMethods,
                ModelsNamespace = GetModelsNamespace(config, parseResult, modelsNamespace),
                Using = GetUsing()
            };

            // apply the parse result to type models and context
            BuildTypeModels(models.TypeModels, context, parseResult, config);

            // filter types,
            // remove ignored types, remove ignored properties
            models.TypeModels.RemoveAll(x => x.IsContentIgnored);
            foreach (var model in models.TypeModels)
                model.Properties.RemoveAll(x => x.IsIgnored);

            return context;
        }

        public virtual void BuildTypeModels(IList<TypeModel> typeModels, CodeContext context, ParseResult parseResult, Config config)
        {
            var pureLive = config.ModelsMode == ModelsMode.PureLive;
            var uniqueTypes = new HashSet<string>();

            // assign ClrName
            foreach (var typeModel in typeModels)
            {
                typeModel.ClrName = GetClrName(typeModel, parseResult, config);

                // of course this should never happen, but when it happens, better detect it
                // else we end up with weird nullrefs everywhere
                if (uniqueTypes.Contains(typeModel.ClrName))
                    throw new Exception($"Panic: duplicate type ClrName \"{typeModel.ClrName}\".");
                uniqueTypes.Add(typeModel.ClrName);

                foreach (var propertyModel in typeModel.Properties)
                {
                    propertyModel.ClrName = GetClrName(propertyModel, config);
                }
            }

            // then, use these names, map model type
            TypeModel.MapModelTypes(typeModels, context.ModelsNamespace);

            // mark IsContentIgnored models that we discovered should be ignored
            // then propagate / ignore children of ignored contents
            // ignore content = don't generate a class for it, don't generate children
            foreach (var typeModel in typeModels.Where(x => parseResult.IsIgnored(x.Alias)))
                typeModel.IsContentIgnored = true;
            foreach (var typeModel in typeModels.Where(x => !x.IsContentIgnored && x.EnumerateBaseTypes().Any(xx => xx.IsContentIgnored)))
                typeModel.IsContentIgnored = true;

            // handle model renames
            foreach (var typeModel in typeModels.Where(x => parseResult.IsContentRenamed(x.Alias)))
            {
                typeModel.ClrName = parseResult.ContentClrName(typeModel.Alias);
                typeModel.IsRenamed = true;
                context.ModelsMap[typeModel.Alias] = typeModel.ClrName;
            }

            // handle implement
            foreach (var typeModel in typeModels.Where(x => parseResult.HasContentImplement(x.Alias)))
            {
                typeModel.HasImplement = true;
            }

            // mark OmitBase models that we discovered already have a base class
            foreach (var typeModel in typeModels.Where(x => parseResult.HasContentBase(parseResult.ContentClrName(x.Alias) ?? x.ClrName)))
                typeModel.HasBase = true;

            foreach (var typeModel in typeModels)
            {
                // mark IsRemoved properties that we discovered should be ignored
                // ie is marked as ignored on type, or on any parent type
                var tm = typeModel;
                foreach (var property in typeModel.Properties
                    .Where(property => tm.EnumerateBaseTypes(true).Any(x => parseResult.IsPropertyIgnored(parseResult.ContentClrName(x.Alias) ?? x.ClrName, property.Alias))))
                {
                    property.IsIgnored = true;
                }

                // handle property renames
                foreach (var property in typeModel.Properties)
                    property.ClrName = parseResult.PropertyClrName(parseResult.ContentClrName(typeModel.Alias) ?? typeModel.ClrName, property.Alias) ?? property.ClrName;
            }

            // for the first two of these two tests,
            //  always throw, even in purelive: cannot happen unless ppl start fidling with attributes to rename
            //  things, and then they should pay attention to the generation error log - there's no magic here
            // for the last one, don't throw in purelive, see comment

            // ensure we have no duplicates type names
            foreach (var xx in typeModels.Where(x => !x.IsContentIgnored).GroupBy(x => x.ClrName).Where(x => x.Count() > 1))
                throw new InvalidOperationException($"Type name \"{xx.Key}\" is used"
                    + $" for types with alias {string.Join(", ", xx.Select(x => x.ItemType + ":\"" + x.Alias + "\""))}. Names have to be unique."
                    + " Consider using an attribute to assign different names to conflicting types.");

            // ensure we have no duplicates property names
            foreach (var typeModel in typeModels.Where(x => !x.IsContentIgnored))
                foreach (var xx in typeModel.Properties.Where(x => !x.IsIgnored).GroupBy(x => x.ClrName).Where(x => x.Count() > 1))
                    throw new InvalidOperationException($"Property name \"{xx.Key}\" in type {typeModel.ItemType}:\"{typeModel.Alias}\""
                        + $" is used for properties with alias {string.Join(", ", xx.Select(x => "\"" + x.Alias + "\""))}. Names have to be unique."
                        + " Consider using an attribute to assign different names to conflicting properties.");

            // ensure content & property type don't have identical name (csharp hates it)
            foreach (var typeModel in typeModels.Where(x => !x.IsContentIgnored))
            {
                foreach (var xx in typeModel.Properties.Where(x => !x.IsIgnored && x.ClrName == typeModel.ClrName))
                {
                    if (!pureLive)
                        throw new InvalidOperationException($"The model class for content type with alias \"{typeModel.Alias}\" is named \"{xx.ClrName}\"."
                            + $" CSharp does not support using the same name for the property with alias \"{xx.Alias}\"."
                            + " Consider using an attribute to assign a different name to the property.");

                    // for purelive, will we generate a commented out properties with an error message,
                    // instead of throwing, because then it kills the sites and ppl don't understand why
                    xx.AddError($"The class {typeModel.ClrName} cannot implement this property, because"
                        + $" CSharp does not support naming the property with alias \"{xx.Alias}\" with the same name as content type with alias \"{typeModel.Alias}\"."
                        + " Consider using an attribute to assign a different name to the property.");

                    // will not be implemented on interface nor class
                    // note: we will still create the static getter, and implement the property on other classes...
                }
            }

            // ensure we have no collision between base types
            // NO: we may want to define a base class in a partial, on a model that has a parent
            // we are NOT checking that the defined base type does maintain the inheritance chain
            //foreach (var xx in _typeModels.Where(x => !x.IsContentIgnored).Where(x => x.BaseType != null && x.HasBase))
            //    throw new InvalidOperationException(string.Format("Type alias \"{0}\" has more than one parent class.",
            //        xx.Alias));

            // discover interfaces that need to be declared / implemented
            foreach (var typeModel in typeModels)
            {
                // collect all the (non-removed) types implemented at parent level
                // ie the parent content types and the mixins content types, recursively
                var parentImplems = new List<TypeModel>();
                if (typeModel.BaseType != null && !typeModel.BaseType.IsContentIgnored)
                    TypeModel.CollectImplems(parentImplems, typeModel.BaseType);

                // interfaces we must declare we implement (initially empty)
                // ie this type's mixins, except those that have been removed,
                // and except those that are already declared at the parent level
                // in other words, DeclaringInterfaces is "local mixins"
                var declaring = typeModel.MixinTypes
                    .Where(x => !x.IsContentIgnored)
                    .Except(parentImplems);
                typeModel.DeclaringInterfaces.AddRange(declaring);

                // interfaces we must actually implement (initially empty)
                // if we declare we implement a mixin interface, we must actually implement
                // its properties, all recursively (ie if the mixin interface implements...)
                // so, starting with local mixins, we collect all the (non-removed) types above them
                var mixinImplems = new List<TypeModel>();
                foreach (var i in typeModel.DeclaringInterfaces)
                    TypeModel.CollectImplems(mixinImplems, i);
                // and then we remove from that list anything that is already declared at the parent level
                typeModel.ImplementingInterfaces.AddRange(mixinImplems.Except(parentImplems));
            }

            // detect mixin properties that have local implementations
            foreach (var typeModel in typeModels)
            {
                foreach (var mixinProperty in typeModel.ImplementingInterfaces.SelectMany(x => x.Properties))
                {
                    if (parseResult.IsPropertyIgnored(parseResult.ContentClrName(typeModel.Alias) ?? typeModel.ClrName, mixinProperty.Alias))
                        typeModel.IgnoredMixinProperties.Add(mixinProperty);
                }
            }

            // ensure elements don't inherit from non-elements
            foreach (var typeModel in typeModels.Where(x => !x.IsContentIgnored && x.IsElement))
            {
                if (typeModel.BaseType != null && !typeModel.BaseType.IsElement)
                    throw new Exception($"Cannot generate model for type '{typeModel.Alias}' because it is an element type, but its parent type '{typeModel.BaseType.Alias}' is not.");

                var errs = typeModel.MixinTypes.Where(x => !x.IsElement).ToList();
                if (errs.Count > 0)
                    throw new Exception($"Cannot generate model for type '{typeModel.Alias}' because it is an element type, but it is composed of {string.Join(", ", errs.Select(x => "'" + x.Alias + "'"))} which {(errs.Count == 1 ? "is" : "are")} not.");
            }

            // register using types
            foreach (var usingNamespace in parseResult.UsingNamespaces)
            {
                context.Using.Add(usingNamespace); // 'using' is a set, will deduplicate
            }

            // handle ctor
            foreach (var typeModel in typeModels.Where(x => parseResult.HasCtor(x.ClrName)))
                typeModel.HasCtor = true;

            // handle extensions
            foreach (var typeModel in typeModels)
            {
                var typeFullName = typeModel.ClrName;
                foreach (var propertyModel in typeModel.Properties)
                {
                    propertyModel.IsExtensionImplemented = parseResult.IsExtensionImplemented(typeFullName, propertyModel.ClrName);
                }
            }

            // get base class names
            foreach (var typeModel in typeModels)
            {
                typeModel.BaseClassName = parseResult.GetModelBaseClassName(!typeModel.IsElement, typeModel.Alias)
                                            ?? (typeModel.IsElement ? "PublishedElementModel" : "PublishedContentModel");
            }
        }

        /// <summary>
        /// Gets the CLR name for a type model.
        /// </summary>
        public virtual string GetClrName(TypeModel typeModel, ParseResult parseResult, Config config)
            => parseResult.TypeModelPrefix + GetClrName(typeModel.Name, typeModel.Alias, config) + parseResult.TypeModelSuffix;

        /// <summary>
        /// Gets the CLR name for a property model.
        /// </summary>
        public virtual string GetClrName(PropertyModel propertyModel, Config config)
            => GetClrName(propertyModel.Name, propertyModel.Alias, config);

        /// <summary>
        /// Gets the CLR name for a name, alias pair.
        /// </summary>
        public virtual string GetClrName(string name, string alias, Config config)
        {
            // ideally we should just be able to re-use Umbraco's alias,
            // just upper-casing the first letter, however in v7 for backward
            // compatibility reasons aliases derive from names via ToSafeAlias which is
            //   PreFilter = ApplyUrlReplaceCharacters,
            //   IsTerm = (c, leading) => leading
            //     ? char.IsLetter(c) // only letters
            //     : (char.IsLetterOrDigit(c) || c == '_'), // letter, digit or underscore
            //   StringType = CleanStringType.Ascii | CleanStringType.UmbracoCase,
            //   BreakTermsOnUpper = false
            //
            // but that is not ideal with acronyms and casing
            // however we CANNOT change Umbraco
            // so, adding a way to "do it right" deriving from name, here

            switch (config.ClrNameSource)
            {
                case ClrNameSource.RawAlias:
                    // use Umbraco's alias
                    return alias;

                case ClrNameSource.Alias:
                    // ModelsBuilder's legacy - but not ideal
                    return alias.ToCleanString(CleanStringType.ConvertCase | CleanStringType.PascalCase);

                case ClrNameSource.Name:
                    // derive from name
                    var source = name.TrimStart('_'); // because CleanStringType.ConvertCase accepts them
                    return source.ToCleanString(CleanStringType.ConvertCase | CleanStringType.PascalCase | CleanStringType.Ascii);

                default:
                    throw new Exception("Invalid ClrNameSource.");
            }
        }
    }
}
