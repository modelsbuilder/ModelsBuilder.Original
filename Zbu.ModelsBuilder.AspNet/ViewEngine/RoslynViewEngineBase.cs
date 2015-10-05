using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.Hosting;
using System.Web.Mvc;
using System.Web.Razor;
using System.Web.Razor.Parser.SyntaxTree;
using System.Web.WebPages;
using System.Web.WebPages.Razor;
using Microsoft.CSharp;
using Zbu.ModelsBuilder.Umbraco;

namespace Zbu.ModelsBuilder.AspNet.ViewEngine
{
    // read
    // https://github.com/chester89/RazorGenerator/blob/master/RazorGenerator.Mvc/PrecompiledMvcEngine.cs
    // http://stackoverflow.com/questions/17816579/razorgenerator-precompiledmvcengine-cannot-locate-partial-view-or-editor-templat
    // https://github.com/jbogard/aspnetwebstack/blob/master/src/System.Web.WebPages/VirtualPathFactoryManager.cs
    // http://stackoverflow.com/questions/13527354/razor-generator-how-to-use-view-compiled-in-a-library-as-the-partial-view-for-m
    // https://github.com/davidebbo/RoslynRazorViewEngine/blob/master/RoslynRazorViewEngine/RoslynRazorViewEngine.cs

    // and
    // https://github.com/stackexchange/stackexchange.precompilation
    // which is a more complex (robust?) version of what we have here,
    // obviously inspired from the very same sources, looking at their code

    public class RoslynViewEngineBase : RazorViewEngine, IVirtualPathFactory, IPureLiveModelsObserver
    {
        private readonly ReaderWriterLockSlim _locker = new ReaderWriterLockSlim();
        private const string CachePrefix = "RoslynRenderViewEngine_";

        #region ModelsBuilder

        protected override IView CreatePartialView(ControllerContext controllerContext, string partialPath)
        {
            Debug.WriteLine("CreatePartial " + partialPath);
            var type = GetTypeFromVirtualPath(partialPath);
            return new RoslynRazorView(partialPath, type, false, FileExtensions);
        }

        protected override IView CreateView(ControllerContext controllerContext, string viewPath, string masterPath)
        {
            Debug.WriteLine("CreateView " + viewPath);
            var type = GetTypeFromVirtualPath(viewPath);
            return new RoslynRazorView(viewPath, type, true, FileExtensions);
        }

        public object CreateInstance(string virtualPath)
        {
            Debug.WriteLine("CreateInstance " + virtualPath);
            var type = GetTypeFromVirtualPath(virtualPath);
            return Activator.CreateInstance(type); // could optimize with dynamic method...
        }

        public bool Exists(string virtualPath)
        {
            return FileExists(null, virtualPath);
        }

        private Type GetTypeFromVirtualPath(string virtualPath)
        {
            virtualPath = VirtualPathUtility.ToAbsolute(virtualPath);

            var cacheKey = CachePrefix + virtualPath;

            _locker.EnterReadLock();
            try
            {
                var type = (Type)HttpRuntime.Cache[cacheKey];
                if (type != null) return type;

                var utcStart = DateTime.UtcNow;
                type = GetTypeFromVirtualPathNoCache(virtualPath);

                // cache it, and make it dependent on the razor file
                var cacheDependency = HostingEnvironment.VirtualPathProvider.GetCacheDependency(virtualPath, new[] { virtualPath }, utcStart);
                HttpRuntime.Cache.Insert(cacheKey, type, cacheDependency);
                return type;
            }
            finally
            {
                _locker.ExitReadLock();
            }
        }

        private static Type GetTypeFromVirtualPathNoCache(string virtualPath)
        {
            // use the Razor engine to generate source code from the view
            var host = WebRazorHostFactory.CreateHostFromConfig(virtualPath);
            var code = GenerateCodeFromRazorTemplate(host, virtualPath);

            // use Roslyn to compile the code into an assembly
            var name = string.Format(CultureInfo.CurrentCulture, "{0}_{1}", host.DefaultNamespace, host.DefaultClassName);
            var assembly = RoslynRazorViewCompiler.Compile(name, code);

            return assembly.GetType(string.Format(CultureInfo.CurrentCulture, "{0}.{1}", host.DefaultNamespace, host.DefaultClassName));
        }

        private static string GenerateCodeFromRazorTemplate(WebPageRazorHost host, string virtualPath)
        {
            // create Razor engine and use it to generate a CodeCompileUnit
            var engine = new RazorTemplateEngine(host);
            GeneratorResults results;
            var file = HostingEnvironment.VirtualPathProvider.GetFile(virtualPath);
            using (var stream = file.Open())
            using (TextReader reader = new StreamReader(stream))
            {
                results = engine.GenerateCode(reader, null, null, host.PhysicalPath);
            }

            if (!results.Success)
            {
                throw CreateExceptionFromParserError(results.ParserErrors.Last(), virtualPath);
            }

            // use CodeDom to generate source code from the CodeCompileUnit
            var codeDomProvider = new CSharpCodeProvider();
            var srcFileWriter = new StringWriter();
            codeDomProvider.GenerateCodeFromCompileUnit(results.GeneratedCode, srcFileWriter, new CodeGeneratorOptions());

            return srcFileWriter.ToString();
        }

        private static HttpParseException CreateExceptionFromParserError(RazorError error, string virtualPath)
        {
            return new HttpParseException(error.Message + Environment.NewLine, null, virtualPath, null, error.Location.LineIndex + 1);
        }

        void IPureLiveModelsObserver.NotifyRebuild()
        {
            // clear all views cache

            _locker.EnterWriteLock();
            try
            {
                var cachedViewKeys = HttpRuntime.Cache
                    .Cast<DictionaryEntry>()
                    .Select(x => (string)x.Key)
                    .Where(key => key.StartsWith(CachePrefix));

                foreach (var key in cachedViewKeys)
                    HttpRuntime.Cache.Remove(key);
            }
            finally
            {
                _locker.ExitWriteLock();
            }
        }

        #endregion
    }
}
