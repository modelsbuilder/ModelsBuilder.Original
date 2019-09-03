using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Umbraco.Core.Composing;
using ZpqrtBnk.ModelsBuilder.Configuration;

namespace ZpqrtBnk.ModelsBuilder.Building
{
    /// <summary>
    /// Provides a base class for <see cref="IBuilder"/> implementations.
    /// </summary>
    public abstract class BuilderBase : IBuilder
    {
        private readonly Dictionary<string, string> _modelsMap = new Dictionary<string, string>();
        private readonly string _modelsNamespace;
        private SemanticModel _ambiguousSymbolsModel;
        private int _ambiguousSymbolsPos;
        internal string ModelsNamespaceForTests;

        /// <summary>
        /// Initializes a new instance of the <see cref="BuilderBase"/> class with a list of models to generate
        /// and the result of code parsing.
        /// </summary>
        /// <param name="typeModels">The list of models to generate.</param>
        /// <param name="parseResult">The result of code parsing.</param>
        protected BuilderBase(IList<TypeModel> typeModels, ParseResult parseResult)
            : this(typeModels, parseResult, null)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="BuilderBase"/> class with a list of models to generate,
        /// the result of code parsing, and a models namespace.
        /// </summary>
        /// <param name="typeModels">The list of models to generate.</param>
        /// <param name="parseResult">The result of code parsing.</param>
        /// <param name="modelsNamespace">The models namespace.</param>
        protected BuilderBase(IList<TypeModel> typeModels, ParseResult parseResult, string modelsNamespace)
        {
            AllTypeModels = typeModels ?? throw new ArgumentNullException(nameof(typeModels));
            ParseResult = parseResult ?? throw new ArgumentNullException(nameof(parseResult));

            // can be null or empty, we'll manage
            _modelsNamespace = modelsNamespace;

            // but we want it to prepare
            Prepare();
        }

        /// <summary>
        /// Gets a map of model alias to model CLR class name.
        /// </summary>
        protected IReadOnlyDictionary<string, string> ModelsMap => _modelsMap;

        /// <summary>
        /// Gets the parse result.
        /// </summary>
        protected ParseResult ParseResult { get; }

        /// <summary>
        /// Gets Models Builder configuration.
        /// </summary>
        private static Config Config => Current.Configs.ModelsBuilder();

        /// <inheritdoc />
        public string ModelsNamespace
        {
            get
            {
                if (ModelsNamespaceForTests != null)
                    return ModelsNamespaceForTests;

                // code attribute overrides everything
                if (ParseResult.HasModelsNamespace)
                    return ParseResult.ModelsNamespace;

                // if builder was initialized with a namespace, use that one
                if (!string.IsNullOrWhiteSpace(_modelsNamespace))
                    return _modelsNamespace;

                // default
                return Config.ModelsNamespace;
            }
        }

        /// <summary>
        /// Gets the list of assemblies to add to the set of 'using' assemblies in each model file.
        /// </summary>
        public ISet<string> Using { get; } = new HashSet<string>
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

        /// <inheritdoc />
        public IEnumerable<TypeModel> GetContentTypeModels()
        {
            return AllTypeModels.Where(x => !x.IsContentIgnored);
        }

        /// <summary>
        /// Gets the list of all models.
        /// </summary>
        /// <remarks>Includes those that are ignored.</remarks>
        protected internal IList<TypeModel> AllTypeModels { get; }

        /// <summary>
        /// Prepares generation by processing the result of code parsing.
        /// </summary>
        /// <remarks>
        ///     Preparation includes figuring out from the existing code which models or properties should
        ///     be ignored or renamed, etc. -- anything that comes from the attributes in the existing code.
        /// </remarks>
        private void Prepare()
        {
            TypeModel.MapModelTypes(AllTypeModels, ModelsNamespace);

            var pureLive = Config.ModelsMode == ModelsMode.PureLive;

            // mark IsContentIgnored models that we discovered should be ignored
            // then propagate / ignore children of ignored contents
            // ignore content = don't generate a class for it, don't generate children
            foreach (var typeModel in AllTypeModels.Where(x => ParseResult.IsIgnored(x.Alias)))
                typeModel.IsContentIgnored = true;
            foreach (var typeModel in AllTypeModels.Where(x => !x.IsContentIgnored && x.EnumerateBaseTypes().Any(xx => xx.IsContentIgnored)))
                typeModel.IsContentIgnored = true;

            // handle model renames
            foreach (var typeModel in AllTypeModels.Where(x => ParseResult.IsContentRenamed(x.Alias)))
            {
                typeModel.ClrName = ParseResult.ContentClrName(typeModel.Alias);
                typeModel.IsRenamed = true;
                _modelsMap[typeModel.Alias] = typeModel.ClrName;
            }

            // handle implement
            foreach (var typeModel in AllTypeModels.Where(x => ParseResult.HasContentImplement(x.Alias)))
            {
                typeModel.HasImplement = true;
            }

            // mark OmitBase models that we discovered already have a base class
            foreach (var typeModel in AllTypeModels.Where(x => ParseResult.HasContentBase(ParseResult.ContentClrName(x.Alias) ?? x.ClrName)))
                typeModel.HasBase = true;

            foreach (var typeModel in AllTypeModels)
            {
                // mark IsRemoved properties that we discovered should be ignored
                // ie is marked as ignored on type, or on any parent type
                var tm = typeModel;
                foreach (var property in typeModel.Properties
                    .Where(property => tm.EnumerateBaseTypes(true).Any(x => ParseResult.IsPropertyIgnored(ParseResult.ContentClrName(x.Alias) ?? x.ClrName, property.Alias))))
                {
                    property.IsIgnored = true;
                }

                // handle property renames
                foreach (var property in typeModel.Properties)
                    property.ClrName = ParseResult.PropertyClrName(ParseResult.ContentClrName(typeModel.Alias) ?? typeModel.ClrName, property.Alias) ?? property.ClrName;
            }

            // for the first two of these two tests,
            //  always throw, even in purelive: cannot happen unless ppl start fidling with attributes to rename
            //  things, and then they should pay attention to the generation error log - there's no magic here
            // for the last one, don't throw in purelive, see comment

            // ensure we have no duplicates type names
            foreach (var xx in AllTypeModels.Where(x => !x.IsContentIgnored).GroupBy(x => x.ClrName).Where(x => x.Count() > 1))
                throw new InvalidOperationException($"Type name \"{xx.Key}\" is used"
                    + $" for types with alias {string.Join(", ", xx.Select(x => x.ItemType + ":\"" + x.Alias + "\""))}. Names have to be unique."
                    + " Consider using an attribute to assign different names to conflicting types.");

            // ensure we have no duplicates property names
            foreach (var typeModel in AllTypeModels.Where(x => !x.IsContentIgnored))
                foreach (var xx in typeModel.Properties.Where(x => !x.IsIgnored).GroupBy(x => x.ClrName).Where(x => x.Count() > 1))
                    throw new InvalidOperationException($"Property name \"{xx.Key}\" in type {typeModel.ItemType}:\"{typeModel.Alias}\""
                        + $" is used for properties with alias {string.Join(", ", xx.Select(x => "\"" + x.Alias + "\""))}. Names have to be unique."
                        + " Consider using an attribute to assign different names to conflicting properties.");

            // ensure content & property type don't have identical name (csharp hates it)
            foreach (var typeModel in AllTypeModels.Where(x => !x.IsContentIgnored))
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
            foreach (var typeModel in AllTypeModels)
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
            foreach (var typeModel in AllTypeModels)
            {
                foreach (var mixinProperty in typeModel.ImplementingInterfaces.SelectMany(x => x.Properties))
                {
                    if (ParseResult.IsPropertyIgnored(ParseResult.ContentClrName(typeModel.Alias) ?? typeModel.ClrName, mixinProperty.Alias))
                        typeModel.IgnoredMixinProperties.Add(mixinProperty);
                }
            }

            // ensure elements don't inherit from non-elements
            foreach (var typeModel in AllTypeModels.Where(x => !x.IsContentIgnored && x.IsElement))
            {
                if (typeModel.BaseType != null && !typeModel.BaseType.IsElement)
                    throw new Exception($"Cannot generate model for type '{typeModel.Alias}' because it is an element type, but its parent type '{typeModel.BaseType.Alias}' is not.");

                var errs = typeModel.MixinTypes.Where(x => !x.IsElement).ToList();
                if (errs.Count > 0)
                    throw new Exception($"Cannot generate model for type '{typeModel.Alias}' because it is an element type, but it is composed of {string.Join(", ", errs.Select(x => "'" + x.Alias + "'"))} which {(errs.Count == 1 ? "is" : "are")} not.");
            }

            // register using types
            foreach (var usingNamespace in ParseResult.UsingNamespaces)
            {
                Using.Add(usingNamespace); // 'using' is a set, will deduplicate
            }

            // handle ctor
            foreach (var typeModel in AllTypeModels.Where(x => ParseResult.HasCtor(x.ClrName)))
                typeModel.HasCtor = true;

            // handle extensions
            foreach (var typeModel in AllTypeModels)
            {
                var typeFullName = typeModel.ClrName;
                foreach (var propertyModel in typeModel.Properties)
                {
                    propertyModel.IsExtensionImplemented = ParseResult.IsExtensionImplemented(typeFullName, propertyModel.ClrName);
                }
            }
        }

        // internal for tests
        internal void PrepareAmbiguousSymbols()
        {
            var codeBuilder = new StringBuilder();
            foreach (var t in Using)
                codeBuilder.AppendFormat("using {0};\n", t);

            codeBuilder.AppendFormat("namespace {0}\n{{ }}\n", ModelsNamespace);

            var compiler = new Compiler();
            var compilation = compiler.GetCompilation("MyCompilation", new Dictionary<string, string> { { "code", codeBuilder.ToString() } }, out var trees);
            var tree = trees[0];
            _ambiguousSymbolsModel = compilation.GetSemanticModel(tree);

            var namespaceSyntax = tree.GetRoot().DescendantNodes().OfType<NamespaceDeclarationSyntax>().First();
            //var namespaceSymbol = model.GetDeclaredSymbol(namespaceSyntax);
            _ambiguousSymbolsPos = namespaceSyntax.OpenBraceToken.SpanStart;
        }

        /// <summary>
        /// Determines whether a symbol is ambiguous.
        /// </summary>
        /// <remarks>
        /// <para>Looking for a simple symbol eg 'Umbraco' or 'String' and expecting
        /// to match eg 'Umbraco' or 'System.String', returns true if either more than
        /// one symbol is found (explicitly ambiguous) or only one symbol is found,
        /// but is not fully matching (implicitly ambiguous).</para>
        /// </remarks>
        protected bool IsAmbiguousSymbol(string symbol, string match)
        {
            if (_ambiguousSymbolsModel == null)
                PrepareAmbiguousSymbols();
            if (_ambiguousSymbolsModel == null)
                throw new Exception("Could not prepare ambiguous symbols.");
            var symbols = _ambiguousSymbolsModel.LookupNamespacesAndTypes(_ambiguousSymbolsPos, null, symbol);

            if (symbols.Length > 1) return true;
            if (symbols.Length == 0) return false; // what else?

            // only 1 - ensure it matches
            var found = symbols[0].ToDisplayString();
            var pos = found.IndexOf('<'); // generic?
            if (pos > 0) found = found.Substring(0, pos); // strip
            return found != match; // and compare
        }

        /// <summary>
        /// Gets the base class name of a content type model.
        /// </summary>
        protected string GetBaseClassName(TypeModel type)
        {
            var baseClassName = ParseResult.GetModelBaseClassName(!type.IsElement, type.Alias);
            if (baseClassName != null) return baseClassName;

            // default
            return type.IsElement ? "PublishedElementModel" : "PublishedContentModel";
        }

        /// <inheritdoc />
        public abstract void WriteContentTypeModel(StringBuilder sb, TypeModel typeModel);

        /// <inheritdoc />
        public abstract void WriteContentTypeModels(StringBuilder sb, IEnumerable<TypeModel> typeModels);

        /// <inheritdoc />
        public abstract void WriteContentTypesMetadata(StringBuilder sb, IEnumerable<TypeModel> typeModels);
    }
}
