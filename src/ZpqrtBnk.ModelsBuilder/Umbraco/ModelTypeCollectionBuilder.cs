using Umbraco.Core.Composing;
using Umbraco.Core.Models.PublishedContent;

namespace ZpqrtBnk.ModelsBuilder.Umbraco
{
    /// <summary>
    /// Represents the collection builder for the <see cref="ModelTypeCollection"/>.
    /// </summary>
    public class ModelTypeCollectionBuilder : TypeCollectionBuilderBase<ModelTypeCollectionBuilder, ModelTypeCollection, IPublishedElement>
    {
        /// <inheritdoc />
        protected override ModelTypeCollectionBuilder This => this;
    }
}