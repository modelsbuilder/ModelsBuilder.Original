using System.Linq;
namespace Zbu.ModelsBuilder
{
    static class FixReferences
    {
        static FixReferences()
        {
            // DO NOT DELETE

            // references assembly, so that it is included in dependent projects (otherwise it's not)
            // read http://stackoverflow.com/questions/1132243/msbuild-doesnt-copy-references-dlls-if-using-project-dependencies-in-solution

            // removed: that assembly is not a dependency anymore
            //// references Microsoft.CodeAnalysis.CSharp.Desktop assembly
            //var parser = Microsoft.CodeAnalysis.CSharp.CSharpCommandLineParser.Default;
            //parser.Parse(Enumerable.Empty<string>(), string.Empty, string.Empty);
        }
    }
}
