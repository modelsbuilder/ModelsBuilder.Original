using System.Collections.Generic;
using System.Text;

namespace ZpqrtBnk.ModelsBuilder.Building
{
    /// <summary>
    /// Provides a models building service.
    /// </summary>
    public interface IBuilder
    {
        /// <summary>
        /// Appends a generated model to a string builder.
        /// </summary>
        /// <param name="sb">The string builder.</param>
        /// <param name="typeModel">The model to generate.</param>
        void AppendModel(StringBuilder sb, TypeModel typeModel);

        /// <summary>
        /// Outputs generated models to a string builder.
        /// </summary>
        /// <param name="sb">The string builder.</param>
        /// <param name="typeModels">The models to generate.</param>
        void AppendModels(StringBuilder sb, IEnumerable<TypeModel> typeModels);

        // fixme
        IEnumerable<TypeModel> GetModels();

        string GetModelsNamespace();

        void AppendMeta(StringBuilder sb, IEnumerable<TypeModel> typeModels);
    }
}
