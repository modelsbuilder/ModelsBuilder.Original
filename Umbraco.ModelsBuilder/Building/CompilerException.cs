using System;

namespace Umbraco.ModelsBuilder.Building
{
    public class CompilerException : Exception
    {
        public CompilerException(string message, string path, string sourceCode, int line)
            : base(message)
        {
            Path = path;
            SourceCode = sourceCode;
            Line = line;
        }

        public string Path { get; }

        public string SourceCode { get; }

        public int Line { get; }
    }
}
