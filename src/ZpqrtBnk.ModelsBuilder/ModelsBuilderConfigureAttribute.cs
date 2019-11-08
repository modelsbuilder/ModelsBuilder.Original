using System;

namespace ZpqrtBnk.ModelsBuilder
{
    /// <summary>
    /// Configures Models Builder.
    /// </summary>
    /// <remarks>Overrides anything else that might come from settings.</remarks>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
    public class ModelsBuilderConfigureAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the models namespace.
        /// </summary>
        public string Namespace { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to generate property getters.
        /// </summary>
        public bool GeneratePropertyGetters { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to generate fallback-function extension methods.
        /// </summary>
        public bool GenerateFallbackFuncExtensionMethods { get; set; }

        /// <summary>
        /// Gets or sets the name of the infos class.
        /// </summary>
        public string ModelInfosClassName { get; set; }

        /// <summary>
        /// Gets or sets the namespace of the infos class.
        /// </summary>
        public string ModelInfosClassNamespace { get; set; }

        /// <summary>
        /// Gets or sets the prefix for content type models.
        /// </summary>
        public string TypeModelPrefix { get; set; }

        /// <summary>
        /// Gets or sets the prefix for content type models.
        /// </summary>
        public string TypeModelSuffix { get; set; }
    }
}
