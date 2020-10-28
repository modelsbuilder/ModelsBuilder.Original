using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;

namespace Our.ModelsBuilder.Building
{
    public class CodeCompilationsArgs : EventArgs
    {
        public LanguageVersion OptionsLanguageVersion { get; }

        public CodeCompilationsArgs(LanguageVersion optionsLanguageVersion, Dictionary<string, string> dictionary)
        {
            OptionsLanguageVersion = optionsLanguageVersion;
        }
    }
}