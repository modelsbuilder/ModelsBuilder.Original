using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace Zbu.ModelsBuilder.Build
{
    internal class Compiler
    {
        public readonly IList<string> Using = new List<string>();

        public void Compile(string binPath, string assemblyName, IDictionary<string, string> files)
        {
            // see http://www.c-sharpcorner.com/UploadFile/25c78a/using-microsoft-roslyn/

            // create the compilation
            SyntaxTree[] trees;
            var compilation = CodeParser.GetCompilation(assemblyName, files, out trees);

            // check diagnostics?
            foreach (var diag in compilation.GetDiagnostics())
            {
                throw new Exception(string.Format("Compilation error: {0}", diag.GetMessage()));
            }

            // write the dll
            EmitResult result;
            var assemblyPath = Path.Combine(binPath, assemblyName + ".dll");
            using (var file = new FileStream(assemblyPath, FileMode.Create))
            {
                result = compilation.Emit(file);
            }
        }
    }
}
