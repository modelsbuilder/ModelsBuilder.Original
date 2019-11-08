using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ZpqrtBnk.ModelsBuilder.Building
{
    public class CodeContext
    {
        private SemanticModel _ambiguousSymbolsModel;
        private int _ambiguousSymbolsPos;

        public string GeneratorName { get; set; } = "ZpqrtBnk.ModelsBuilder";

        public string ModelInfosClassName { get; set; } = "ModelInfos";

        public string ModelInfosClassNamespace { get; set; } = "Umbraco.Web.PublishedModels"; // FIXME

        public bool GeneratePropertyGetters { get; set; }

        public bool GenerateFallbackFuncExtensionMethods { get; set; }

        // TODO: configure per model? also folders?
        public string ModelsNamespace { get; set; } // FIXME more complex that just a const?

        public ISet<string> Using { get; set; } = new HashSet<string>();

        // FIXME make it a method MapModel() and explain
        public Dictionary<string, string> ModelsMap { get; } = new Dictionary<string, string>();

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
        public bool IsAmbiguousSymbol(string symbol, string match)
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
    }
}