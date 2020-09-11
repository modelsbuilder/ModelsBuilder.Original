using System;
using Our.ModelsBuilder.Options.ContentTypes;

namespace Our.ModelsBuilder.Options
{
    /// <summary>
    /// Represents code options.
    /// </summary>
    public class CodeOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CodeOptions"/> class.
        /// </summary>
        /// <param name="contentTypes"></param>
        public CodeOptions(ContentTypesCodeOptions contentTypes)
        {
            ContentTypes = contentTypes ?? throw new ArgumentNullException(nameof(contentTypes));
        }

        /// <summary>
        /// Gets the options for content types.
        /// </summary>
        public ContentTypesCodeOptions ContentTypes { get; }

        /// <summary>
        /// Gets a value indicating whether a models namespace has been specified.
        /// </summary>
        public bool HasModelsNamespace => !string.IsNullOrWhiteSpace(ModelsNamespace);

        /// <summary>
        /// Gets the models namespace.
        /// </summary>
        public string ModelsNamespace { get; internal set; }
        /// <summary>
        /// Gets a value indicating whether a models namespace has been specified.
        /// </summary>
        public bool HasCustomAssemblyName => !string.IsNullOrWhiteSpace(CustomAssemblyName);

        /// <summary>
        /// Gets the models namespace.
        /// </summary>
        public string CustomAssemblyName { get; internal set; }
    }
}