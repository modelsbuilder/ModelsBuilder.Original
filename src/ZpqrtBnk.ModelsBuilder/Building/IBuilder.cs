using ZpqrtBnk.ModelsBuilder.Configuration;

namespace ZpqrtBnk.ModelsBuilder.Building
{
    public interface IBuilder
    {
        CodeContext Build(Config config, ParseResult parseResult, string modelsNamespace, CodeModels models);
    }
}
