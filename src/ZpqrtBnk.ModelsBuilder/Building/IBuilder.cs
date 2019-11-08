using ZpqrtBnk.ModelsBuilder.Configuration;

namespace ZpqrtBnk.ModelsBuilder.Building
{
    public interface IBuilder
    {
        void Build(CodeModel model, Config config, ParseResult parseResult, string modelsNamespace);
    }
}
