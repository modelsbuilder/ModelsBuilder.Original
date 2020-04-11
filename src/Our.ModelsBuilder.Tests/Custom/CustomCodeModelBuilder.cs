using Our.ModelsBuilder.Building;
using Our.ModelsBuilder.Options;

namespace Our.ModelsBuilder.Tests.Custom
{
    public class CustomCodeModelBuilder : CodeModelBuilder
    {
        public CustomCodeModelBuilder(ModelsBuilderOptions options, CodeOptions codeOptions, ContentTypesCodeModelBuilder contentTypesCodeModelBuilder) 
            : base(options, codeOptions, contentTypesCodeModelBuilder)
        { }
    }
}