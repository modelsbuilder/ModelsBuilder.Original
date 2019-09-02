using Umbraco.Core.Composing;
using ZpqrtBnk.ModelsBuilder.Umbraco;

namespace ZpqrtBnk.ModelsBuilder
{
    /// <summary>
    /// Provides extension methods for the <see cref="Composition"/> class.
    /// </summary>
    public static class CompositionExtensions
    {
        /// <summary>
        /// Gets the model types collection builder.
        /// </summary>
        public static ModelTypeCollectionBuilder ModelTypes(this Composition composition)
            => composition.WithCollectionBuilder<ModelTypeCollectionBuilder>();
    }
}
