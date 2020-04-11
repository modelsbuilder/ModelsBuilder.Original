using System;
using Our.ModelsBuilder.Options;
using Umbraco.Core.Composing;

// ReSharper disable once CheckNamespace, reason: extension method
namespace Our.ModelsBuilder
{
    /// <summary>
    /// Provides extension methods for the <see cref="Composition"/> class.
    /// </summary>
    public static class OptionsCompositionExtensions
    {
        /// <summary>
        /// Configures ModelsBuilder options.
        /// </summary>
        public static Composition ConfigureOptions(this Composition composition, Action<ModelsBuilderOptions> configure)
        {
            composition.Configs.GetConfig<OptionsConfiguration>().AddConfigure(configure);
            return composition;
        }

        /// <summary>
        /// Configures ModelsBuilder code options.
        /// </summary>
        public static Composition ConfigureCodeOptions(this Composition composition, Action<CodeOptionsBuilder> configure)
        {
            composition.Configs.GetConfig<OptionsConfiguration>().AddConfigure(configure);
            return composition;
        }
    }
}
