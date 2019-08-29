using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Umbraco.Core;
using Umbraco.Core.Models.PublishedContent;

namespace ZpqrtBnk.ModelsBuilder.Building
{
    /// <summary>
    /// Implements code parsing.
    /// </summary>
    /// <remarks>Parses user's code and look for generator's instructions.</remarks>
    internal class CodeParser
    {
        /// <summary>
        /// Gets or sets a value indicating whether to write diagnostics to console, for debugging purposes.
        /// </summary>
        public bool WriteDiagnostics { get; set; }

        /// <summary>
        /// Parses a set of file.
        /// </summary>
        /// <param name="files">A set of (filename,content) representing content to parse.</param>
        /// <returns>The result of the code parsing.</returns>
        /// <remarks>The set of files is a dictionary of name, content.</remarks>
        public ParseResult Parse(IDictionary<string, string> files)
        {
            return Parse(files, Enumerable.Empty<PortableExecutableReference>());
        }

        /// <summary>
        /// Parses a set of file.
        /// </summary>
        /// <param name="files">A set of (filename,content) representing content to parse.</param>
        /// <param name="references">Assemblies to reference in compilations.</param>
        /// <returns>The result of the code parsing.</returns>
        /// <remarks>The set of files is a dictionary of name, content.</remarks>
        public ParseResult Parse(IDictionary<string, string> files, IEnumerable<PortableExecutableReference> references)
        {
            SyntaxTree[] trees;
            var compiler = new Compiler { References = references };
            var compilation = compiler.GetCompilation("ZpqrtBnk.ModelsBuilder.Generated", files, out trees);

            // debug
            if (WriteDiagnostics)
                foreach (var d in compilation.GetDiagnostics())
                    Console.WriteLine(d);

            var disco = new ParseResult();
            foreach (var tree in trees)
            {
                Parse(disco, compilation, tree);
            }

            return disco;
        }

        public ParseResult ParseWithReferencedAssemblies(IDictionary<string, string> files)
        {
            return Parse(files, ReferencedAssemblies.References);
        }

        private static void Parse(ParseResult disco, CSharpCompilation compilation, SyntaxTree tree)
        {
            var model = compilation.GetSemanticModel(tree);

            //we quite probably have errors but that is normal
            //var diags = model.GetDiagnostics();

            var classDecls = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>();
            var classSymbols = classDecls.Select(x => model.GetDeclaredSymbol(x)).ToList();
            var interfaceDecls = tree.GetRoot().DescendantNodes().OfType<InterfaceDeclarationSyntax>();

            // extensions
            foreach (var classSymbol in classSymbols.Where(x => x.IsStatic))
            {
                var methods = classSymbol.GetMembers().OfType<IMethodSymbol>().Where(x => x.IsExtensionMethod && !x.IsGenericMethod);

                foreach (var method in methods)
                {
                    var parameters = method.Parameters;
                    if (parameters.Length != 3) continue;

                    bool IsStringType(ITypeSymbol type)
                        => type.ToDisplayString() == "string";

                    if (!IsStringType(parameters[1].Type) || !IsStringType(parameters[2].Type))
                        continue;

                    // the type here is *not* the type full name - only a symbol
                    // at that point we may even be referencing a type that has not been generated
                    // and then it will come out as 'Foo' *but* it may come out as 'Namespace.Foo'.
                    // ParserResult will deal with it
                    // TODO this could have consequences with duplicate names in namespaces?
                    // but the only way would be to have an attribute to specify the alias = later
                    var typeName = parameters[0].Type.ToDisplayString();
                    var propertyName = method.Name;
                    disco.SetImplementedExtension(typeName, propertyName);
                }
            }

            // classes
            foreach (var classSymbol in classSymbols)
            {
                ParseClassSymbols(disco, classSymbol);

                var baseClassSymbol = classSymbol.BaseType;
                if (baseClassSymbol != null)
                    //disco.SetContentBaseClass(SymbolDisplay.ToDisplayString(classSymbol), SymbolDisplay.ToDisplayString(baseClassSymbol));
                    disco.SetContentBaseClass(classSymbol.Name, baseClassSymbol.Name);

                var interfaceSymbols = classSymbol.Interfaces;
                disco.SetContentInterfaces(classSymbol.Name, //SymbolDisplay.ToDisplayString(classSymbol),
                    interfaceSymbols.Select(x => x.Name)); //SymbolDisplay.ToDisplayString(x)));

                var hasCtor = classSymbol.Constructors
                    .Any(x =>
                    {
                        if (x.IsStatic) return false;
                        if (x.Parameters.Length != 1) return false;
                        var type1 = x.Parameters[0].Type;
                        var type2 = typeof (IPublishedContent);
                        return type1.ToDisplayString() == type2.FullName;
                    });

                if (hasCtor)
                    disco.SetHasCtor(classSymbol.Name);

                foreach (var propertySymbol in classSymbol.GetMembers().Where(x => x is IPropertySymbol))
                    ParsePropertySymbols(disco, classSymbol, propertySymbol);

                foreach (var staticMethodSymbol in classSymbol.GetMembers().Where(x => x is IMethodSymbol))
                    ParseMethodSymbol(disco, classSymbol, staticMethodSymbol);
            }

            // interfaces
            foreach (var interfaceSymbol in interfaceDecls.Select(x => model.GetDeclaredSymbol(x)))
            {
                ParseClassSymbols(disco, interfaceSymbol);

                var interfaceSymbols = interfaceSymbol.Interfaces;
                disco.SetContentInterfaces(interfaceSymbol.Name, //SymbolDisplay.ToDisplayString(interfaceSymbol),
                    interfaceSymbols.Select(x => x.Name)); // SymbolDisplay.ToDisplayString(x)));
            }

            ParseAssemblySymbols(disco, compilation.Assembly);
        }

        private static void ParseClassSymbols(ParseResult disco, ISymbol symbol)
        {
            foreach (var attrData in symbol.GetAttributes())
            {
                var attrClassSymbol = attrData.AttributeClass;

                // handle errors
                if (attrClassSymbol is IErrorTypeSymbol) continue;
                if (attrData.AttributeConstructor == null) continue;

                var attrClassName = SymbolDisplay.ToDisplayString(attrClassSymbol);
                switch (attrClassName)
                {
                    case "ZpqrtBnk.ModelsBuilder.IgnorePropertyTypeAttribute":
                        var propertyAliasToIgnore = (string)attrData.ConstructorArguments[0].Value;
                        disco.SetIgnoredProperty(symbol.Name /*SymbolDisplay.ToDisplayString(symbol)*/, propertyAliasToIgnore);
                        break;
                    case "ZpqrtBnk.ModelsBuilder.RenamePropertyTypeAttribute":
                        var propertyAliasToRename = (string)attrData.ConstructorArguments[0].Value;
                        var propertyRenamed = (string)attrData.ConstructorArguments[1].Value;
                        disco.SetRenamedProperty(symbol.Name /*SymbolDisplay.ToDisplayString(symbol)*/, propertyAliasToRename, propertyRenamed);
                        break;
                    // that one causes all sorts of issues with references to Umbraco.Core in Roslyn
                    //case "Umbraco.Core.Models.PublishedContent.PublishedContentModelAttribute":
                    //    var contentAliasToRename = (string)attrData.ConstructorArguments[0].Value;
                    //    disco.SetRenamedContent(contentAliasToRename, symbol.Name /*SymbolDisplay.ToDisplayString(symbol)*/);
                    //    break;
                    case "ZpqrtBnk.ModelsBuilder.ImplementContentTypeAttribute":
                        var contentAliasToRename = (string)attrData.ConstructorArguments[0].Value;
                        disco.SetRenamedContent(contentAliasToRename, symbol.Name, true /*SymbolDisplay.ToDisplayString(symbol)*/);
                        break;
                }
            }
        }

        private static void ParsePropertySymbols(ParseResult disco, ISymbol classSymbol, ISymbol symbol)
        {
            foreach (var attrData in symbol.GetAttributes())
            {
                var attrClassSymbol = attrData.AttributeClass;

                // handle errors
                if (attrClassSymbol is IErrorTypeSymbol) continue;
                if (attrData.AttributeConstructor == null) continue;

                var attrClassName = SymbolDisplay.ToDisplayString(attrClassSymbol);
                // ReSharper disable once SwitchStatementMissingSomeCases
                switch (attrClassName)
                {
                    case "ZpqrtBnk.ModelsBuilder.ImplementPropertyTypeAttribute":
                        var propertyAliasToIgnore = (string)attrData.ConstructorArguments[0].Value;
                        disco.SetIgnoredProperty(classSymbol.Name /*SymbolDisplay.ToDisplayString(classSymbol)*/, propertyAliasToIgnore);
                        break;
                }
            }
        }

        private static void ParseAssemblySymbols(ParseResult disco, ISymbol symbol)
        {
            foreach (var attrData in symbol.GetAttributes())
            {
                var attrClassSymbol = attrData.AttributeClass;

                // handle errors
                if (attrClassSymbol is IErrorTypeSymbol) continue;
                if (attrData.AttributeConstructor == null) continue;

                var attrClassName = SymbolDisplay.ToDisplayString(attrClassSymbol);
                switch (attrClassName)
                {
                    case "ZpqrtBnk.ModelsBuilder.IgnoreContentTypeAttribute":
                        var contentAliasToIgnore = (string)attrData.ConstructorArguments[0].Value;
                        // see notes in IgnoreContentTypeAttribute
                        //var ignoreContent = (bool)attrData.ConstructorArguments[1].Value;
                        //var ignoreMixin = (bool)attrData.ConstructorArguments[1].Value;
                        //var ignoreMixinProperties = (bool)attrData.ConstructorArguments[1].Value;
                        disco.SetIgnoredContent(contentAliasToIgnore /*, ignoreContent, ignoreMixin, ignoreMixinProperties*/);
                        break;

                    case "ZpqrtBnk.ModelsBuilder.RenameContentTypeAttribute":
                        var contentAliasToRename = (string) attrData.ConstructorArguments[0].Value;
                        var contentRenamed = (string)attrData.ConstructorArguments[1].Value;
                        disco.SetRenamedContent(contentAliasToRename, contentRenamed, false);
                        break;

                    case "ZpqrtBnk.ModelsBuilder.ModelsBaseClassAttribute":
                        throw new NotSupportedException("The ModelsBaseClassAttribute is not supported anymore.");

                    case "ZpqrtBnk.ModelsBuilder.ContentModelsBaseClassAttribute":
                        var contentArgsCount = attrData.ConstructorArguments.Length;
                        var contentAliasPattern = contentArgsCount == 1 ? "*" : (string) attrData.ConstructorArguments[0].Value;
                        var contentModelsBaseClass = (INamedTypeSymbol) attrData.ConstructorArguments[contentArgsCount - 1].Value;
                        if (contentModelsBaseClass is IErrorTypeSymbol)
                            throw new Exception($"Invalid content base class type \"{contentModelsBaseClass.Name}\".");
                        disco.SetModelsBaseClassName(true, contentAliasPattern, SymbolDisplay.ToDisplayString(contentModelsBaseClass));
                        break;

                    case "ZpqrtBnk.ModelsBuilder.ElementModelsBaseClassAttribute":
                        var elementArgsCount = attrData.ConstructorArguments.Length;
                        var elementAliasPattern = elementArgsCount == 1 ? "*" : (string) attrData.ConstructorArguments[0].Value;
                        var elementModelsBaseClass = (INamedTypeSymbol) attrData.ConstructorArguments[elementArgsCount - 1].Value;
                        if (elementModelsBaseClass is IErrorTypeSymbol)
                            throw new Exception($"Invalid element base class type \"{elementModelsBaseClass.Name}\".");
                        disco.SetModelsBaseClassName(false, elementAliasPattern, SymbolDisplay.ToDisplayString(elementModelsBaseClass));
                        break;

                    // fixme obsolete but we keep it around for now
                    case "ZpqrtBnk.ModelsBuilder.ModelsNamespaceAttribute":
                        var modelsNamespace= (string) attrData.ConstructorArguments[0].Value;
                        disco.SetModelsNamespace(modelsNamespace);
                        break;

                    case "ZpqrtBnk.ModelsBuilder.ModelsUsingAttribute":
                        var usingNamespace = (string)attrData.ConstructorArguments[0].Value;
                        disco.SetUsingNamespace(usingNamespace);
                        break;

                    case "ZpqrtBnk.ModelsBuilder.ModelsBuilderConfigureAttribute":
                        foreach (var (argName, argValue) in attrData.NamedArguments)
                        {
                            switch (argName)
                            {
                                case nameof(ModelsBuilderConfigureAttribute.Namespace):
                                    disco.SetModelsNamespace((string) argValue.Value);
                                    break;
                                case nameof(ModelsBuilderConfigureAttribute.GeneratePropertyGetters):
                                    disco.SetGeneratePropertyGetters((bool) argValue.Value);
                                    break;
                            }
                        }
                        break;
                }
            }
        }

        private static void ParseMethodSymbol(ParseResult disco, ISymbol classSymbol, ISymbol symbol)
        {
            var methodSymbol = symbol as IMethodSymbol;

            if (methodSymbol == null
                || !methodSymbol.IsStatic
                || methodSymbol.IsGenericMethod
                || methodSymbol.ReturnsVoid
                || methodSymbol.IsExtensionMethod
                || methodSymbol.Parameters.Length != 1)
                return;

            var returnType = methodSymbol.ReturnType;
            var paramSymbol = methodSymbol.Parameters[0];
            var paramType = paramSymbol.Type;

            // cannot do this because maybe the param type is ISomething and we don't have
            // that type yet - will be generated - so cannot put any condition on it really
            //const string iPublishedContent = "Umbraco.Core.Models.IPublishedContent";
            //var implements = paramType.AllInterfaces.Any(x => x.ToDisplayString() == iPublishedContent);
            //if (!implements)
            //    return;

            //disco.SetStaticMixinMethod(classSymbol.Name, methodSymbol.Name, returnType.Name, paramType.Name);
        }
    }
}
