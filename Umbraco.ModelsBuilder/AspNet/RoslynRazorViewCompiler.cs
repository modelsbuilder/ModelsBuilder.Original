using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Umbraco.Core;
using Umbraco.Core.Configuration;
using Umbraco.ModelsBuilder.Configuration;

namespace Umbraco.ModelsBuilder.AspNet
{
    // that class is static because of AppDomain.CurrentDomain.AssemblyResolve
    // (see in the static ctor)

    //NOTE: This is not a thread safe class
        
    internal static class RoslynRazorViewCompiler
    {
        private static int _modelsGeneration;

        // read - about dynamic assemblies
        // http://stackoverflow.com/questions/26822811/how-do-you-add-references-to-types-compiled-in-a-memory-stream-using-roslyn
        // http://stackoverflow.com/questions/28503569/roslyn-create-metadatareference-from-in-memory-assembly
        // https://github.com/dotnet/roslyn/issues/2246

        private static PortableExecutableReference _modelsReference;
        private static Assembly _modelsAssembly;
        private static string _modelsVersionString;
        private static readonly Lazy<PortableExecutableReference[]> References;

        static RoslynRazorViewCompiler()
        {
            // plug assembly resolution, so the domain can load the dynamic assembly
            // need to have a version string that changes, so that new generations are detected

            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) => 
                args.Name == _modelsVersionString ? _modelsAssembly : null;

            References = new Lazy<PortableExecutableReference[]>(() => AssemblyUtility.AllReferencedAssemblyLocations
                .Select(x => MetadataReference.CreateFromFile(x))
                .ToArray());
        }

        private static CSharpCompilation GetCompilation(string assemblyName, IDictionary<string, string> files, out SyntaxTree[] trees)
        {
            var options = new CSharpParseOptions(UmbracoConfig.For.ModelsBuilder().LanguageVersion);
            trees = files.Select(x =>
            {
                var text = x.Value;
                var tree = CSharpSyntaxTree.ParseText(text, /*options:*/ options);
                var diagnostic = tree.GetDiagnostics().FirstOrDefault(y => y.Severity == DiagnosticSeverity.Error);
                if (diagnostic != null)
                    ThrowExceptionFromDiagnostic(x.Key, x.Value, diagnostic);
                return tree;
            }).ToArray();

            var refs = _modelsReference == null ? References.Value : References.Value.And(_modelsReference);

            var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
            var compilation = CSharpCompilation.Create(
                assemblyName,
                /*syntaxTrees:*/ trees,
                /*references:*/ refs,
                compilationOptions);

            return compilation;
        }

        public static Assembly Compile(string assemblyName, string path, string code)
        {
            // create the compilation
            SyntaxTree[] trees;
            var compilation = GetCompilation(assemblyName, new Dictionary<string, string> { { path, code } }, out trees);

            // check diagnostics for errors (not warnings)
            foreach (var diagnostic in compilation.GetDiagnostics().Where(x => x.Severity == DiagnosticSeverity.Error))
                ThrowExceptionFromDiagnostic(path, code, diagnostic);

            // emit
            Assembly assembly;
            using (var stream = new MemoryStream())
            {
                var result = compilation.Emit(stream);
                if (!result.Success)
                {
                    var diagnostic = result.Diagnostics.First(x => x.Severity == DiagnosticSeverity.Error);
                    ThrowExceptionFromDiagnostic(path, code, diagnostic);
                }
                assembly = Assembly.Load(stream.GetBuffer());
            }

            return assembly;
        }

        private static void ThrowExceptionFromDiagnostic(string path, string code, Diagnostic diagnostic)
        {
            var message = diagnostic.GetMessage();
            var position = diagnostic.Location.GetLineSpan().StartLinePosition.Line + 1;
            throw new HttpParseException(message, null, path, code, position);
        }

        // invoked by the PureLiveModelsFactory when it detects it needs to rebuild models
        // the factory should have notified all "pure live models" view engines that it is
        // rebuilding models, and engines should have suspended compilation, so it is safe
        // to have zero lock in this method
        public static Assembly CompileAndRegisterModels(string code)
        {
            // zero everything and increment generation
            _modelsReference = null;
            _modelsAssembly = null;
            _modelsVersionString = null;
            _modelsGeneration++;

            // we don't really need the _gen in the name since it's in the version already
            // but that helps when debugging because the name itself indicates the _gen
            var assemblyName = "__dynamic__" + _modelsGeneration + "__";
            var assemblyVersion = "0.0.0." + _modelsGeneration;

            code = code.Replace("//ASSATTR", @"[assembly:System.Reflection.AssemblyTitle(""" + assemblyName + @""")]
[assembly:System.Reflection.AssemblyVersion(""" + assemblyVersion + @""")]");

            // create the compilation
            SyntaxTree[] trees;
            var compilation = GetCompilation(assemblyName, new Dictionary<string, string> { { "code", code } }, out trees);

            // check diagnostics for errors (not warnings)
            foreach (var diag in compilation.GetDiagnostics().Where(x => x.Severity == DiagnosticSeverity.Error))
                throw new Exception(string.Format("Compilation {0}: {1}", diag.Severity, diag.GetMessage()));

            // emit
            var stream = new MemoryStream();
            compilation.Emit(stream); // should we check the result?
            var bytes = stream.GetBuffer();

            _modelsAssembly = Assembly.Load(bytes);
            _modelsVersionString = assemblyName + ", Version=" + assemblyVersion + ", Culture=neutral, PublicKeyToken=null";
            _modelsReference = MetadataReference.CreateFromImage(bytes);

            return _modelsAssembly;
        }
    }
}
