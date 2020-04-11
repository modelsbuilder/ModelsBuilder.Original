using System;
using Our.ModelsBuilder.Options.ContentTypes;

namespace Our.ModelsBuilder.Options
{
    /// <summary>
    /// Builds the <see cref="ModelsBuilder.Options.CodeOptions"/>.
    /// </summary>
    public class CodeOptionsBuilder
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CodeOptionsBuilder"/> class.
        /// </summary>
        public CodeOptionsBuilder()
        {
            var contentTypesCodeOptions = new ContentTypesCodeOptions();
            CodeOptions = new CodeOptions(contentTypesCodeOptions);
            ContentTypes = new ContentTypesCodeOptionsBuilder(contentTypesCodeOptions);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CodeOptionsBuilder"/> class.
        /// </summary>
        protected CodeOptionsBuilder(CodeOptions codeOptions, ContentTypesCodeOptionsBuilder contentTypesCodeOptionsBuilder)
        {
            CodeOptions = codeOptions ?? throw new ArgumentNullException(nameof(codeOptions));
            ContentTypes = contentTypesCodeOptionsBuilder ?? throw new ArgumentNullException(nameof(contentTypesCodeOptionsBuilder));
        }

        /// <summary>
        /// Gets the options builder for content types.
        /// </summary>
        public virtual ContentTypesCodeOptionsBuilder ContentTypes { get; }

        /// <summary>
        /// Gets the options.
        /// </summary>
        public virtual CodeOptions CodeOptions { get; }

        /// <summary>
        /// Sets the models namespace.
        /// </summary>
        /// <param name="modelsNamespace">The models namespace.</param>
        public virtual void SetModelsNamespace(string modelsNamespace)
        {
            CodeOptions.ModelsNamespace = modelsNamespace;
        }
    }
}