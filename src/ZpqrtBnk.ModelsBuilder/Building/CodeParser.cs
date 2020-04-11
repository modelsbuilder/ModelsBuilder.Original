using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Our.ModelsBuilder.Options;
using Our.ModelsBuilder.Options.ContentTypes;
using Umbraco.Core.Models.PublishedContent;

namespace Our.ModelsBuilder.Building
{
    /// <summary>
    /// Implements the default <see cref="ICodeParser"/>.
    /// </summary>
    public class CodeParser : ICodeParser
    {
        private readonly LanguageVersion _languageVersion;

        /// <summary>
        /// Initializes a new instance of the <see cref="CodeParser"/> class.
        /// </summary>
        public CodeParser(LanguageVersion languageVersion = LanguageVersion.Default)
        {
            _languageVersion = languageVersion;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to write diagnostics to console (for tests).
        /// </summary>
        public bool WriteDiagnostics { get; set; }

        /// <inheritdoc />
        public void Parse(IDictionary<string, string> files, CodeOptionsBuilder optionsBuilder, IEnumerable<PortableExecutableReference> references = null)
        {
            var compiler = new Compiler(_languageVersion) { References = references ?? Enumerable.Empty<PortableExecutableReference>() };
            var compilation = compiler.GetCompilation("Our.ModelsBuilder.Generated", files, out var trees);

            // debug
            if (WriteDiagnostics)
                foreach (var d in compilation.GetDiagnostics())
                    Console.WriteLine(d);

            foreach (var tree in trees)
                Parse(optionsBuilder.ContentTypes, compilation, tree);
        }

        private static void Parse(ContentTypesCodeOptionsBuilder transform, CSharpCompilation compilation, SyntaxTree tree)
        {
            var model = compilation.GetSemanticModel(tree);

            // we quite probably have errors but that is normal, as we may for instance refer
            // to types that have not been generated yet, etc.
            //var diagnostics = model.GetDiagnostics();

            // classes
            var classDeclarations = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>();
            var classSymbols = classDeclarations.Select(x => model.GetDeclaredSymbol(x));
            foreach (var classSymbol in classSymbols)
            {
                if (classSymbol.IsStatic)
                {
                    // extension class
                    var methodSymbols = classSymbol.GetMembers().OfType<IMethodSymbol>().Where(x => x.IsStatic && !x.IsGenericMethod);
                    foreach (var methodSymbol in methodSymbols)
                        ParseExtensions(transform, methodSymbol);
                }
                else
                {
                    // class
                    ParseClassDeclaration(transform, classSymbol);
                }
            }

            // interfaces
            var interfaceDeclarations = tree.GetRoot().DescendantNodes().OfType<InterfaceDeclarationSyntax>();
            var interfaceSymbols = interfaceDeclarations.Select(x => model.GetDeclaredSymbol(x));
            foreach (var interfaceSymbol in interfaceSymbols)
            {
                ParseInterfaceDeclaration(transform, interfaceSymbol);
            }
        }

        private static bool IsObject(INamedTypeSymbol symbol)
        {
            return symbol.ContainingNamespace.ToString() == "System" && 
                   symbol.Name == "Object";
        }

        // parse the partial class declaration, detecting
        // - if the partial class defines a base class
        // - the interfaces declared by the partial class
        // - whether the partial class provides the constructor
        // - FIXME properties implemented by the partial class
        private static void ParseClassDeclaration(ContentTypesCodeOptionsBuilder transform, INamedTypeSymbol classSymbol)
        {
            // is a partial defining a base class?
            var baseClassSymbol = classSymbol.BaseType;
            if (!IsObject(baseClassSymbol)) // never null, but can be 'object'
            {
                var className = baseClassSymbol.Name;
                //var assemblyName = baseClassSymbol is IErrorTypeSymbol
                //    ? ""
                //    : baseClassSymbol.ContainingNamespace.ToString();
                transform.ContentTypeModelHasBaseClass(classSymbol.Name, className);
            }

            // discover the interfaces
            var interfaceSymbols = classSymbol.Interfaces;
            foreach (var symbol in interfaceSymbols)
                transform.ContentTypeModelHasInterface(classSymbol.Name, symbol.Name);

            // is a partial implementing the constructor?
            var hasConstructor = classSymbol.Constructors
                .Any(x =>
                {
                    if (x.IsStatic) return false;
                    if (x.Parameters.Length != 1) return false;
                    var type1 = x.Parameters[0].Type;
                    var type2 = typeof(IPublishedContent);
                    return type1.ToDisplayString() == type2.FullName;
                });

            if (hasConstructor)
                transform.ContentTypeModelHasConstructor(classSymbol.Name);

            // is the partial implementing some properties?
            var propertySymbols = classSymbol.GetMembers().OfType<IPropertySymbol>();
            foreach (var propertySymbol in propertySymbols)
                ParsePropertySymbol(transform, classSymbol, propertySymbol);
        }

        // parse the partial interface declaration, detecting
        // - the interfaces declared by the partial interface
        // - FIXME so no property?
        private static void ParseInterfaceDeclaration(ContentTypesCodeOptionsBuilder transform, INamedTypeSymbol interfaceSymbol)
        {
            var interfaceSymbols = interfaceSymbol.Interfaces;
            foreach (var symbol in interfaceSymbols)
                transform.ContentTypeModelHasInterface(interfaceSymbol.Name, symbol.Name);
        }

        private static void ParsePropertySymbol(ContentTypesCodeOptionsBuilder transform, INamedTypeSymbol classSymbol, IPropertySymbol symbol)
        {
            foreach (var attrData in symbol.GetAttributes())
            {
                var attrClassSymbol = attrData.AttributeClass;
                var attrClassName = SymbolDisplay.ToDisplayString(attrClassSymbol);

                // handle errors
                if (attrClassSymbol is IErrorTypeSymbol) continue;
                if (attrData.AttributeConstructor == null) continue;

                // ReSharper disable once SwitchStatementMissingSomeCases
                switch (attrClassName)
                {
                    case "Our.ModelsBuilder.ImplementPropertyTypeAttribute":
                        if (attrData.ConstructorArguments.Length != 1)
                            throw new InvalidOperationException("Invalid number of arguments for ImplementPropertyTypeAttribute.");
                        var propertyAliasToIgnore = (string)attrData.ConstructorArguments[0].Value;
                        transform.IgnorePropertyType(ContentTypeIdentity.ClrName(classSymbol.Name), propertyAliasToIgnore);
                        break;
                }
            }
        }

        // FIXME should be "extensions" don't need to be true extension methods
        private static void ParseExtensions(ContentTypesCodeOptionsBuilder transform, IMethodSymbol symbol)
        {
            foreach (var attrData in symbol.GetAttributes())
            {
                var attrClassSymbol = attrData.AttributeClass;
                var attrClassName = SymbolDisplay.ToDisplayString(attrClassSymbol);

                // handle errors
                if (attrClassSymbol is IErrorTypeSymbol) continue;
                if (attrData.AttributeConstructor == null) continue;

                // there isn't much we can guess from an extension method,
                // the attribute has to be explicit about everything

                // ReSharper disable once SwitchStatementMissingSomeCases
                switch (attrClassName)
                {
                    case "Our.ModelsBuilder.ImplementPropertyTypeAttribute":
                        if (attrData.ConstructorArguments.Length != 2)
                            throw new InvalidOperationException("Invalid number of arguments for ImplementPropertyTypeAttribute.");
                        var contentTypeAlias = (string) attrData.ConstructorArguments[0].Value;
                        var propertyTypeAlias = (string) attrData.ConstructorArguments[1].Value;
                        transform.IgnorePropertyType(ContentTypeIdentity.Alias(contentTypeAlias), propertyTypeAlias);
                        break;
                }
            }
        }
    }
}
