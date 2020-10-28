using System;
using Our.ModelsBuilder.Options;

namespace Our.ModelsBuilder.Building
{
    public class CodeGenerationArgs : EventArgs
    {
        public ICodeFactory CodeFactory;
        public ModelsBuilderOptions Options;

        public CodeGenerationArgs(ICodeFactory codeFactory, ModelsBuilderOptions options)
        {
            CodeFactory = codeFactory;
            Options = options;
        }
    }
}