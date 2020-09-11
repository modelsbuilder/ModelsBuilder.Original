using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Our.ModelsBuilder.Building
{
    /// <summary>
    /// Represents a model of the code to generate.
    /// </summary>
    public class CodeModel
    {
        private readonly LanguageVersion _languageVersion;
        private SemanticModel _ambiguousSymbolsModel;
        private int _ambiguousSymbolsPos;

        /// <summary>
        /// Initializes a new instance of the <see cref="CodeModel"/> class.
        /// </summary>
        public CodeModel(CodeModelData data, LanguageVersion languageVersion = LanguageVersion.Default)
        {
            _languageVersion = languageVersion;
            ContentTypes = new ContentTypesCodeModel { ContentTypes = data.ContentTypes };
        }

        /// <summary>
        /// Gets or sets the name of the code generator.
        /// </summary>
        public string GeneratorName { get; set; } = "Our.ModelsBuilder";

        /// <summary>
        /// Gets or sets the name of the model infos class.
        /// </summary>
        public string ModelInfosClassName { get; set; } = "ModelInfos";

        // TODO: implement this when we implement per-model namespace
        // (requires that we also manage 'using' statements etc)
        // (at the moment, using ModelsNamespace for everything)
        ///// <summary>
        ///// Gets or sets the namespace of the model infos class.
        ///// </summary>
        //public string ModelInfosClassNamespace { get; set; } = "Umbraco.Web.PublishedModels";
        public string ModelInfosClassNamespace => ModelsNamespace;

        /// <summary>
        /// Gets or sets the content types code model.
        /// </summary>
        public ContentTypesCodeModel ContentTypes { get; set; } // FIXME rename ContentTypesModel (ContentTypesOptions etc)


        // FIXME everything below belong to the content types code model?


        /// <summary>
        /// Gets or sets the models namespace.
        /// </summary>
        // TODO: per-model namespace
        public string ModelsNamespace { get; set; }
        
        /// <summary>
        /// Gets or sets the models assembly name.
        /// </summary>
        // TODO: per-model assembly name
        public string CustomAssemblyName { get; set; }
        
        /// <summary>
        /// Gets models assembly name or fallback to the models namespace
        /// </summary>
        public string AssemblyName
        {
            get
            {
                if (CustomAssemblyName != null)
                {
                    return CustomAssemblyName;
                }

                return ModelsNamespace;
            }
        }

        public ISet<string> Using { get; set; } = new HashSet<string>();

        #region Ambiguous Symbols

        // internal for tests
        internal void PrepareAmbiguousSymbols()
        {
            var codeBuilder = new StringBuilder();
            foreach (var t in Using)
                codeBuilder.AppendFormat("using {0};\n", t);

            codeBuilder.AppendFormat("namespace {0}\n{{ }}\n", ModelsNamespace);

            var compiler = new Compiler(_languageVersion);
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
        public virtual bool IsAmbiguousSymbol(string symbol, string match)
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

        #endregion
    }
}