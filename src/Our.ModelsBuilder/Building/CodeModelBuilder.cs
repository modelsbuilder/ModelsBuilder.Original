using System.Collections.Generic;
using Our.ModelsBuilder.Options;

namespace Our.ModelsBuilder.Building
{
    /// <summary>
    /// Implements the default <see cref="ICodeModelBuilder"/>.
    /// </summary>
    public class CodeModelBuilder : ICodeModelBuilder
    {
        // FIXME but we still need ... to 'build' options vs to 'query' options
        // ICodeFactory / CodeFactory
        //    provides the key services to generate models
        //  CodeModelSourceProvider
        //    provides a CodeModelSource
        //  CodeModelSource
        //    represents a source of models, i.e. raw from Umbraco
        //  CodeOptionsBuilder
        //    is passed to CodeParser
        //    builds a CodeOptions
        //  ICodeParser / CodeParser
        //    parses existing code and updates the CodeOptionsBuilder
        //  CodeOptions
        //    provides options for building a code model
        //  ICodeModelBuilder / CodeModelBuilder
        //    + ContentTypesCodeModelBuilder
        //    builds a code model, from a CodeModelSource, CodeOptions and a Configuration
        //  CodeModel
        //    provides a complete description of the code to be written
        //  ICodeWriter / CodeWriter
        //    + ContentTypesCodeWriter
        //    writes code defined in a code model

        /// <summary>
        /// Initializes a new instance of the <see cref="CodeModelBuilder"/> class.
        /// </summary>
        public CodeModelBuilder(ModelsBuilderOptions options, CodeOptions codeOptions)
            : this(options, codeOptions, new ContentTypesCodeModelBuilder(options, codeOptions))
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CodeModelBuilder"/> class.
        /// </summary>
        protected CodeModelBuilder(ModelsBuilderOptions options, CodeOptions codeOptions, ContentTypesCodeModelBuilder contentTypes)
        {
            Options = options;
            CodeOptions = codeOptions;
            ContentTypes = contentTypes;
        }

        /// <summary>
        /// Gets the content types code model builder.
        /// </summary>
        protected ContentTypesCodeModelBuilder ContentTypes { get; }

        protected ModelsBuilderOptions Options { get; }

        protected CodeOptions CodeOptions { get; }

        /// <inheritdoc />
        public virtual CodeModel Build(CodeModelData data)
        {
            var model = new CodeModel(data, Options.LanguageVersion)
            {
                ModelsNamespace = GetModelsNamespace(), 
                Using = GetUsing()
            };

            // TODO: refactor using, have transform.Use("namespace")

            ContentTypes.Build(model);

            return model;
        }

        protected virtual string GetDefaultModelsNamespace() => "Umbraco.Web.PublishedModels";

        protected virtual string GetModelsNamespace()
        {
            // use namespace from code options... or from options
            var modelsNamespace = CodeOptions.HasModelsNamespace
                ? CodeOptions.ModelsNamespace
                : Options.ModelsNamespace;

            // otherwise, use const
            if (string.IsNullOrWhiteSpace(modelsNamespace))
                modelsNamespace = GetDefaultModelsNamespace();

            return modelsNamespace;
        }

        /// <summary>
        /// Gets the namespace to use in 'using' section.
        /// </summary>
        protected virtual ISet<string> GetUsing() => new HashSet<string>
        {
            // initialize with default values
            "System",
            "System.Collections.Generic",
            "System.Linq.Expressions",
            "System.Web",
            "Umbraco.Core.Models",
            "Umbraco.Core.Models.PublishedContent",
            "Umbraco.Web",
            "Our.ModelsBuilder",
            "Our.ModelsBuilder.Umbraco",
        };

    }
}