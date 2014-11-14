using System.Linq;
using Microsoft.CodeAnalysis.CSharp;

namespace Zbu.ModelsBuilder
{
    static class FixReferences
    {
        static FixReferences()
        {
            // DO NOT DELETE
            // references Microsoft.CodeAnalysis.CSharp.Desktop assembly
            // so that it is included in dependent projects (otherwise it's not)
            // read http://stackoverflow.com/questions/1132243/msbuild-doesnt-copy-references-dlls-if-using-project-dependencies-in-solution
            var parser = CSharpCommandLineParser.Default;
            parser.Parse(Enumerable.Empty<string>(), string.Empty);
        }
    }
}
