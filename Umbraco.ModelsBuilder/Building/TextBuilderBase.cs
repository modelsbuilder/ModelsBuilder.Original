using System.Collections.Generic;
using System.Text;

namespace Umbraco.ModelsBuilder.Building
{
    public abstract class TextBuilderBase : Builder
    {
        internal TextBuilderBase()
        {
        }

        protected TextBuilderBase(IList<TypeModel> typeModels, ParseResult parseResult) : base(typeModels, parseResult)
        {
        }

        protected TextBuilderBase(IList<TypeModel> typeModels, ParseResult parseResult, string modelsNamespace) : base(typeModels, parseResult, modelsNamespace)
        {
        }

        /// <summary>
        /// Outputs a generated model to a string builder.
        /// </summary>
        /// <param name="sb">The string builder.</param>
        /// <param name="typeModel">The model to generate.</param>
        public abstract void Generate(StringBuilder sb, TypeModel typeModel);
    }
}