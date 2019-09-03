using System.Collections.Generic;

namespace ZpqrtBnk.ModelsBuilder.Building
{
    public class TextBuilderFactory : IBuilderFactory
    {
        public IBuilder CreateBuilder(IList<TypeModel> typeModels, ParseResult parseResult, string modelsNamespace = null)
        {
            return new Builder(typeModels, parseResult, modelsNamespace);
        }
    }
}