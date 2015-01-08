using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Zbu.ModelsBuilder.Configuration;

namespace Zbu.ModelsBuilder.Building
{
    public class Compiler
    {
        public readonly HashSet<Assembly> ReferencedAssemblies = new HashSet<Assembly>();
        private readonly LanguageVersion _languageVersion;

        public Compiler()
        {
            _languageVersion = Config.LanguageVersion;
        }

        public Compiler(LanguageVersion languageVersion)
        {
            _languageVersion = languageVersion;
        }

        public CSharpCompilation GetCompilation(string assemblyName, IDictionary<string, string> files, out SyntaxTree[] trees)
        {
            var options = new CSharpParseOptions(_languageVersion);
            trees = files.Select(x =>
            {
                var text = x.Value;
                var tree = CSharpSyntaxTree.ParseText(text, options: options);
                if (tree.GetDiagnostics().Any())
                    throw new Exception(string.Format("Syntax error in file \"{0}\".", x.Key));
                return tree;
            }).ToArray();

            // adding everything is going to cause issues with dynamic assemblies
            // so we would want to filter them anyway... but we don't need them really
            //var refs = AssemblyUtility.GetAllReferencedAssemblyLocations().Select(x => new MetadataFileReference(x));
            // though that one is not ok either since we want our own reference
            //var refs = Enumerable.Empty<MetadataReference>();
            // so use the bare minimum
            var asms = ReferencedAssemblies;
            var a1 = typeof(Builder).Assembly;
            asms.Add(a1);
            foreach (var a in GetDeepReferencedAssemblies(a1)) asms.Add(a);
            var refs = asms.Select(x => MetadataReference.CreateFromFile(x.Location));

            var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
            var compilation = CSharpCompilation.Create(
                assemblyName,
                /*syntaxTrees:*/ trees,
                /*references:*/ refs,
                compilationOptions);

            return compilation;
        }

        public void Compile(string binPath, string assemblyName, IDictionary<string, string> files)
        {
            // see http://www.c-sharpcorner.com/UploadFile/25c78a/using-microsoft-roslyn/

            // create the compilation
            SyntaxTree[] trees;
            var compilation = GetCompilation(assemblyName, files, out trees);

            // check diagnostics for errors (not warnings)
            foreach (var diag in compilation.GetDiagnostics().Where(x => x.Severity == DiagnosticSeverity.Error))
            {
                throw new Exception(string.Format("Models compilation {0}: {1}", diag.Severity, diag.GetMessage()));
            }

            // write the dll
            EmitResult result;
            var assemblyPath = Path.Combine(binPath, assemblyName + ".dll");
            using (var file = new FileStream(assemblyPath, FileMode.Create))
            {
                result = compilation.Emit(file);
            }
        }

        public Assembly Compile(string assemblyName, IDictionary<string, string> files)
        {
            // create the compilation
            SyntaxTree[] trees;
            var compilation = GetCompilation(assemblyName, files, out trees);

            // check diagnostics for errors (not warnings)
            foreach (var diag in compilation.GetDiagnostics().Where(x => x.Severity == DiagnosticSeverity.Error))
            {
                throw new Exception(string.Format("Models compilation {0}: {1}", diag.Severity, diag.GetMessage()));
            }

            // emit
            Assembly assembly;
            using (var stream = new MemoryStream())
            {
                var emitResult = compilation.Emit(stream);
                assembly = Assembly.Load(stream.GetBuffer());
            }

            return assembly;
        }

        public Assembly Compile(string assemblyName, string code)
        {
            // create the compilation
            SyntaxTree[] trees;
            var compilation = GetCompilation(assemblyName, new Dictionary<string, string>{{"code", code}}, out trees);

            // check diagnostics for errors (not warnings)
            foreach (var diag in compilation.GetDiagnostics().Where(x => x.Severity == DiagnosticSeverity.Error))
            {
                throw new Exception(string.Format("Models compilation {0}: {1}", diag.Severity, diag.GetMessage()));
            }

            // emit
            Assembly assembly;
            using (var stream = new MemoryStream())
            {
                var emitResult = compilation.Emit(stream);
                assembly = Assembly.Load(stream.GetBuffer());
            }

            return assembly;
        }

        private static IEnumerable<Assembly> GetDeepReferencedAssemblies(Assembly assembly)
        {
            var visiting = new Stack<Assembly>();
            var visited = new HashSet<Assembly>();

            visiting.Push(assembly);
            visited.Add(assembly);
            while (visiting.Count > 0)
            {
                var visAsm = visiting.Pop();
                foreach (var refAsm in visAsm.GetReferencedAssemblies()
                    .Select(TryLoad)
                    .Where(x => x != null && visited.Contains(x) == false))
                {
                    yield return refAsm;
                    visiting.Push(refAsm);
                    visited.Add(refAsm);
                }
            }
        }

        private static Assembly TryLoad(AssemblyName name)
        {
            try
            {
                return AppDomain.CurrentDomain.Load(name);
            }
            catch (Exception)
            {
                //Console.WriteLine(name);
                return null;
            }
        }
    }
}
