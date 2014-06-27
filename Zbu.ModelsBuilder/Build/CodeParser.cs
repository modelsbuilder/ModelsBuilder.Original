using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Zbu.ModelsBuilder.Build
{
    /// <summary>
    /// Implements code parsing.
    /// </summary>
    /// <remarks>Parses user's code and look for generator's instructions.</remarks>
    public class CodeParser
    {
        /// <summary>
        /// Parses a set of file.
        /// </summary>
        /// <param name="files">A set of (filename,content) representing content to parse.</param>
        /// <returns>The result of the code parsing.</returns>
        /// <remarks>The set of files is a dictionary of name, content.</remarks>
        public ParseResult Parse(IDictionary<string, string> files)
        {
            SyntaxTree[] trees;
            var compiler = new Compiler();
            var compilation = compiler.GetCompilation("Zbu.ModelsBuilder.Generated", files, out trees);

            var disco = new ParseResult();
            foreach (var tree in trees)
                Parse(disco, compilation, tree);

            return disco;
        }

        private static void Parse(ParseResult disco, CSharpCompilation compilation, SyntaxTree tree)
        {
            var model = compilation.GetSemanticModel(tree);

            //we quite probably have errors but that is normal
            //var diags = model.GetDiagnostics();

            var classDecls = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>();
            foreach (var classSymbol in classDecls.Select(x => model.GetDeclaredSymbol(x)))
            {
                ParseClassSymbols(disco, classSymbol);

                var baseClassSymbol = classSymbol.BaseType;
                if (baseClassSymbol != null)
                    //disco.SetContentBaseClass(SymbolDisplay.ToDisplayString(classSymbol), SymbolDisplay.ToDisplayString(baseClassSymbol));
                    disco.SetContentBaseClass(classSymbol.Name, baseClassSymbol.Name);

                var interfaceSymbols = classSymbol.Interfaces;
                disco.SetContentInterfaces(classSymbol.Name, //SymbolDisplay.ToDisplayString(classSymbol),
                    interfaceSymbols.Select(x => x.Name)); //SymbolDisplay.ToDisplayString(x)));

                foreach (var propertySymbol in classSymbol.GetMembers().Where(x => x is IPropertySymbol))
                    ParsePropertySymbols(disco, classSymbol, propertySymbol);
            }

            var interfaceDecls = tree.GetRoot().DescendantNodes().OfType<InterfaceDeclarationSyntax>();
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
                    case "Zbu.ModelsBuilder.IgnorePropertyTypeAttribute":
                        var propertyAliasToIgnore = (string)attrData.ConstructorArguments[0].Value;
                        disco.SetIgnoredProperty(symbol.Name /*SymbolDisplay.ToDisplayString(symbol)*/, propertyAliasToIgnore);
                        break;
                    case "Zbu.ModelsBuilder.RenamePropertyTypeAttribute":
                        var propertyAliasToRename = (string)attrData.ConstructorArguments[0].Value;
                        var propertyRenamed = (string)attrData.ConstructorArguments[1].Value;
                        disco.SetRenamedProperty(symbol.Name /*SymbolDisplay.ToDisplayString(symbol)*/, propertyAliasToRename, propertyRenamed);
                        break;
                    // that one causes all sorts of issues with references to Umbraco.Core in Roslyn
                    //case "Umbraco.Core.Models.PublishedContent.PublishedContentModelAttribute":
                    //    var contentAliasToRename = (string)attrData.ConstructorArguments[0].Value;
                    //    disco.SetRenamedContent(contentAliasToRename, symbol.Name /*SymbolDisplay.ToDisplayString(symbol)*/);
                    //    break;
                    case "Zbu.ModelsBuilder.ImplementContentTypeAttribute":
                        var contentAliasToRename = (string)attrData.ConstructorArguments[0].Value;
                        disco.SetRenamedContent(contentAliasToRename, symbol.Name /*SymbolDisplay.ToDisplayString(symbol)*/);
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
                switch (attrClassName)
                {
                    case "Zbu.ModelsBuilder.ImplementPropertyTypeAttribute":
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
                    case "Zbu.ModelsBuilder.IgnoreContentTypeAttribute":
                        var contentAliasToIgnore = (string)attrData.ConstructorArguments[0].Value;
                        // see notes in IgnoreContentTypeAttribute
                        //var ignoreContent = (bool)attrData.ConstructorArguments[1].Value;
                        //var ignoreMixin = (bool)attrData.ConstructorArguments[1].Value;
                        //var ignoreMixinProperties = (bool)attrData.ConstructorArguments[1].Value;
                        disco.SetIgnoredContent(contentAliasToIgnore /*, ignoreContent, ignoreMixin, ignoreMixinProperties*/);
                        break;

                    case "Zbu.ModelsBuilder.RenameContentTypeAttribute":
                        var contentAliasToRename = (string) attrData.ConstructorArguments[0].Value;
                        var contentRenamed = (string)attrData.ConstructorArguments[1].Value;
                        disco.SetRenamedContent(contentAliasToRename, contentRenamed);
                        break;

                    case "Zbu.ModelsBuilder.ModelsBaseClassAttribute":
                        var modelsBaseClass = (INamedTypeSymbol) attrData.ConstructorArguments[0].Value;
                        if (modelsBaseClass is IErrorTypeSymbol)
                            throw new Exception(string.Format("Invalid base class type \"{0}\".", modelsBaseClass.Name));
                        disco.SetModelsBaseClassName(SymbolDisplay.ToDisplayString(modelsBaseClass));
                        break;

                    case "Zbu.ModelsBuilder.ModelsNamespaceAttribute":
                        var modelsNamespace= (string) attrData.ConstructorArguments[0].Value;
                        disco.SetModelsNamespace(modelsNamespace);
                        break;

                    case "Zbu.ModelsBuilder.ModelsUsingAttribute":
                        var usingNamespace = (string)attrData.ConstructorArguments[0].Value;
                        disco.SetUsingNamespace(usingNamespace);
                        break;
                }
            }
        }
    }
}
