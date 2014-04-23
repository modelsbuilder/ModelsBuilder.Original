using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

            // adding everything is going to cause issues with dynamic assemblies
            // so we would want to filter them anyway... but we don't need them really
            //var refs = AssemblyUtility.GetAllReferencedAssemblyLocations().Select(x => new MetadataFileReference(x));
            // though that one is not ok either since we want our own reference
            //var refs = Enumerable.Empty<MetadataReference>();
            // so use the bare minimum
            var asms = new HashSet<Assembly>();
            var a1 = typeof (IgnoreContentTypeAttribute).Assembly;
            asms.Add(a1);
            foreach (var a in GetDeepReferencedAssemblies(a1)) asms.Add(a);
            var refs = asms.Select(x => new MetadataFileReference(x.Location));

            var compilation = CSharpCompilation.Create(
                "Zbu.ModelsBuilder.Generated",
                /*syntaxTrees:*/ trees,
                /*references:*/ refs);

            var disco = new DiscoveryResult();
            foreach (var tree in trees)
                Discover(disco, compilation, tree);

            return disco;
        }

        private static IEnumerable<Assembly> GetDeepReferencedAssemblies(Assembly assembly)
        {
            var visiting = new Stack<Assembly>();
            var visited = new HashSet<Assembly>();

            visiting.Push(assembly);
            visited.Add(assembly);
            while (visiting.Count > 0)
            {
                var visAsm = visiting.Pop();
                foreach (var refAsm in visAsm.GetReferencedAssemblies()
                    .Select(TryLoad)
                    .Where(x => x != null && visited.Contains(x) == false))
                {
                    yield return refAsm;
                    visiting.Push(refAsm);
                    visited.Add(refAsm);
                }
            }
        }

        private static Assembly TryLoad(AssemblyName name)
        {
            try
            {
                return AppDomain.CurrentDomain.Load(name);
            }
            catch (Exception)
            {
                Console.WriteLine(name);
                return null;
            }
        }

        private static void Discover(DiscoveryResult disco, CSharpCompilation compilation, SyntaxTree tree)
        {
            var model = compilation.GetSemanticModel(tree);

            //we quite probably have errors but that is normal
            //var diags = model.GetDiagnostics();

            var classDecls = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>();
            foreach (var classSymbol in classDecls.Select(x => model.GetDeclaredSymbol(x)))
            {
                DiscoverClassSymbols(disco, classSymbol);

                var baseClassSymbol = classSymbol.BaseType;
                if (baseClassSymbol != null)
                    //disco.SetContentBaseClass(SymbolDisplay.ToDisplayString(classSymbol), SymbolDisplay.ToDisplayString(baseClassSymbol));
                    disco.SetContentBaseClass(classSymbol.Name, baseClassSymbol.Name);

                var interfaceSymbols = classSymbol.Interfaces;
                disco.SetContentInterfaces(classSymbol.Name, //SymbolDisplay.ToDisplayString(classSymbol),
                    interfaceSymbols.Select(x => x.Name)); //SymbolDisplay.ToDisplayString(x)));

                foreach (var propertySymbol in classSymbol.GetMembers().Where(x => x is IPropertySymbol))
                    DiscoverPropertySymbols(disco, classSymbol, propertySymbol);
            }

            var interfaceDecls = tree.GetRoot().DescendantNodes().OfType<InterfaceDeclarationSyntax>();
            foreach (var interfaceSymbol in interfaceDecls.Select(x => model.GetDeclaredSymbol(x)))
            {
                DiscoverClassSymbols(disco, interfaceSymbol);

                var interfaceSymbols = interfaceSymbol.Interfaces;
                disco.SetContentInterfaces(interfaceSymbol.Name, //SymbolDisplay.ToDisplayString(interfaceSymbol),
                    interfaceSymbols.Select(x => x.Name)); // SymbolDisplay.ToDisplayString(x)));
            }

            DiscoverAssemblySymbols(disco, compilation.Assembly);
        }

        private static void DiscoverClassSymbols(DiscoveryResult disco, ISymbol symbol)
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
                        if (attrData.ConstructorArguments.Length != 2)
                            throw new Exception("Invalid RenamePropertyTypeAttribute usage.");
                        var propertyAliasToRename = (string)attrData.ConstructorArguments[0].Value;
                        var propertyRenamed = (string)attrData.ConstructorArguments[1].Value;
                        disco.SetRenamedProperty(symbol.Name /*SymbolDisplay.ToDisplayString(symbol)*/, propertyAliasToRename, propertyRenamed);
                        break;
                    // that one causes all sorts of issues with references to Umbraco.Core in Roslyn
                    //case "Umbraco.Core.Models.PublishedContent.PublishedContentModelAttribute":
                    //    var contentAliasToRename = (string)attrData.ConstructorArguments[0].Value;
                    //    disco.SetRenamedContent(contentAliasToRename, symbol.Name /*SymbolDisplay.ToDisplayString(symbol)*/);
                    //    break;
                    case "Zbu.ModelsBuilder.RenameContentTypeAttribute":
                        if (attrData.ConstructorArguments.Length != 1)
                            throw new Exception("Invalid RenameContentTypeAttribute usage.");
                        var contentAliasToRename = (string)attrData.ConstructorArguments[0].Value;
                        disco.SetRenamedContent(contentAliasToRename, symbol.Name /*SymbolDisplay.ToDisplayString(symbol)*/);
                        break;
                }
            }
        }

        private static void DiscoverPropertySymbols(DiscoveryResult disco, ISymbol classSymbol, ISymbol symbol)
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
                    case "Zbu.ModelsBuilder.RenamePropertyTypeAttribute":
                        if (attrData.ConstructorArguments.Length != 1)
                            throw new Exception("Invalid RenamePropertyTypeAttribute usage.");
                        var propertyAliasToRename = (string)attrData.ConstructorArguments[0].Value;
                        disco.SetRenamedProperty(classSymbol.Name /*SymbolDisplay.ToDisplayString(classSymbol)*/, propertyAliasToRename, symbol.Name);
                        break;
                }
            }
        }

        private static void DiscoverAssemblySymbols(DiscoveryResult disco, ISymbol symbol)
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
                        if (attrData.ConstructorArguments.Length != 2)
                            throw new Exception("Invalid RenamePropertyTypeAttribute usage.");
                        var contentAliasToRename = (string) attrData.ConstructorArguments[0].Value;
                        var contentRenamed = (string)attrData.ConstructorArguments[1].Value;
                        disco.SetRenamedContent(contentAliasToRename, contentRenamed);
                        break;
                }
            }
        }
    }
}
