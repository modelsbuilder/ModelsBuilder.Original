using System.Collections.Generic;
using System.Text;

namespace ZpqrtBnk.ModelsBuilder.Building
{
    /// <summary>
    /// Builds models.
    /// </summary>
    public interface IBuilder
    {
        /// <summary>
        /// Writes content types metadata to a string builder.
        /// </summary>
        /// <param name="sb">The string builder.</param>
        /// <param name="typeModels">The models to generate.</param>
        /// <remarks>
        /// <para>The content of the StringBuilder represents the content of a single
        /// file, with 'using' statements etc.</para>
        /// </remarks>
        void WriteContentTypesMetadata(StringBuilder sb, IEnumerable<TypeModel> typeModels);

        /// <summary>
        /// Writes a generated content type model to a string builder.
        /// </summary>
        /// <param name="sb">The string builder.</param>
        /// <param name="typeModel">The model to generate.</param>
        /// <remarks>
        /// <para>The content of the StringBuilder represents the content of a single
        /// file, with 'using' statements etc. One cannot append several models to the
        /// same StringBuilder (see <see cref="WriteContentTypeModels"/>).</para>
        /// </remarks>
        void WriteContentTypeModel(StringBuilder sb, TypeModel typeModel);

        /// <summary>
        /// Writes generated content type models to a string builder.
        /// </summary>
        /// <param name="sb">The string builder.</param>
        /// <param name="typeModels">The models to generate.</param>
        /// <remarks>
        /// <para>The content of the StringBuilder represents the content of a single
        /// file, with 'using' statements etc.</para>
        /// </remarks>
        void WriteContentTypeModels(StringBuilder sb, IEnumerable<TypeModel> typeModels);

        /// <summary>
        /// Gets the list of models to generate.
        /// </summary>
        /// <returns>The models to generate, ie those that are not ignored.</returns>
        IEnumerable<TypeModel> GetContentTypeModels();

        /// <summary>
        /// Gets the namespace for models.
        /// </summary>
        string ModelsNamespace { get; }
    }
}
