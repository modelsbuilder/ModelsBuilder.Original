using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Zbu.ModelsBuilder
{
    public class CodeDiscovery
    {
        public DiscoveryResult Discover(IDictionary<string, string> files)
        {
            var options = new CSharpParseOptions();
            var trees = files.Select(x =>
            {
                var text = x.Value;
                var tree = CSharpSyntaxTree.ParseText(text, options: options);
                if (tree.GetDiagnostics().Any())
                    throw new Exception(string.Format("Syntax error in file \"{0}\".", x.Key));
                return tree;
            }).ToArray();

            var refs = AssemblyUtility.GetAllReferencedAssemblyLocations().Select(x => new MetadataFileReference(x));
            var compilation = CSharpCompilation.Create(
                "Zbu.ModelsBuilder.Generated",
                syntaxTrees: trees,
                references: refs);

            var disco = new DiscoveryResult();
            foreach (var tree in trees)
                Discover(disco, compilation, tree);

            return disco;
        }

        private void Discover(DiscoveryResult disco, CSharpCompilation compilation, SyntaxTree tree)
        {
            var model = compilation.GetSemanticModel(tree);

            //we quite probably have errors but that is normal
            //var diags = model.GetDiagnostics();

            var classDecls = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>();
            foreach (var classSymbol in classDecls.Select(x => model.GetDeclaredSymbol(x)))
            {
                DiscoverSymbols(disco, classSymbol);

                var baseClassSymbol = classSymbol.BaseType;
                if (baseClassSymbol != null)
                    disco.SetContentBaseClass(SymbolDisplay.ToDisplayString(classSymbol), SymbolDisplay.ToDisplayString(baseClassSymbol));

                var interfaceSymbols = classSymbol.Interfaces;
                disco.SetContentInterfaces(SymbolDisplay.ToDisplayString(classSymbol),
                    interfaceSymbols.Select(x => SymbolDisplay.ToDisplayString(x)));

                foreach (var propertySymbol in classSymbol.GetMembers().Where(x => x is IPropertySymbol))
                    DiscoverSymbols(disco, propertySymbol);
            }

            var interfaceDecls = tree.GetRoot().DescendantNodes().OfType<InterfaceDeclarationSyntax>();
            foreach (var interfaceSymbol in interfaceDecls.Select(x => model.GetDeclaredSymbol(x)))
            {
                DiscoverSymbols(disco, interfaceSymbol);

                var interfaceSymbols = interfaceSymbol.Interfaces;
                disco.SetContentInterfaces(SymbolDisplay.ToDisplayString(interfaceSymbol),
                    interfaceSymbols.Select(x => SymbolDisplay.ToDisplayString(x)));
            }

            foreach (var attrData in compilation.Assembly.GetAttributes())
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
                        disco.SetIgnoredContent(contentAliasToIgnore);
                        break;
                }
            }
        }

        private void DiscoverSymbols(DiscoveryResult disco, ISymbol symbol)
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
                        disco.SetIgnoredProperty(SymbolDisplay.ToDisplayString(symbol), propertyAliasToIgnore);
                        break;
                    case "Zbu.ModelsBuilder.RenamePropertyTypeAttribute":
                        var propertyAliasToRename = (string)attrData.ConstructorArguments[0].Value;
                        var propertyRenamed = (string)attrData.ConstructorArguments[1].Value;
                        disco.SetRenamedProperty(SymbolDisplay.ToDisplayString(symbol), propertyAliasToRename, propertyRenamed);
                        break;
                    case "Umbraco.Core.Models.PublishedContent.PublishedContentModelAttribute":
                        var contentAliasToRename = (string)attrData.ConstructorArguments[0].Value;
                        disco.SetRenamedContent(contentAliasToRename, SymbolDisplay.ToDisplayString(symbol));
                        break;
                }
            }
        }
    }
}
