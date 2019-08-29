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
        public bool GeneratePropertyGetters { get; set; } = true;
    }
}
