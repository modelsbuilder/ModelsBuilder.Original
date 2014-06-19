using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web.Compilation;
using System.Web.Hosting;
using Umbraco.Web.Cache;
using Zbu.ModelsBuilder.Build;
using Zbu.ModelsBuilder.Configuration;
using Zbu.ModelsBuilder.Umbraco;

namespace Zbu.ModelsBuilder.AspNet
{
    /*
        <remove extension=".cshtml"/>
        <add extension=".cshtml" type="Zbu.ModelsBuilder.AspNet.RazorBuildProvider, Zbu.ModelsBuilder.AspNet"/>
    */

    // NOTE
    // This build provider is NOT installed in web.config as shown above
    // It is installed at runtime by the Initializer class, if required (depends on config)

    [BuildProviderAppliesTo(BuildProviderAppliesTo.Web | BuildProviderAppliesTo.Code)]
    public class RazorBuildProvider : System.Web.WebPages.Razor.RazorBuildProvider
    {
        private static readonly object LockO = new object();
        private static bool _triedToGetModelsAssemblyAlready;
        private static bool _initialized;
        private static Assembly _modelsAssembly;

        public override void GenerateCode(AssemblyBuilder assemblyBuilder)
        {
            // if live models are enabled, compile & add assembly
            if (Config.EnableLiveModels)
                AddModelsAssemblyReference(assemblyBuilder);

            base.GenerateCode(assemblyBuilder);
        }

        private static void Initialize()
        {
            // anything changes, and we want to re-generate models.
            ContentTypeCacheRefresher.CacheUpdated += (sender, args) => ResetModelsAssembly();
            DataTypeCacheRefresher.CacheUpdated += (sender, args) => ResetModelsAssembly();

            _initialized = true;
        }

        private static void ResetModelsAssembly()
        {
            lock (LockO)
            {
                _modelsAssembly = null;
                _triedToGetModelsAssemblyAlready = false;
            }
        }

        private static void AddModelsAssemblyReference(AssemblyBuilder assemblyBuilder)
        {
            lock (LockO)
            {
                if (!_initialized)
                    Initialize();

                if (_modelsAssembly == null && !_triedToGetModelsAssemblyAlready)
                {
                    _modelsAssembly = GetModelsAssembly();
                    _triedToGetModelsAssemblyAlready = true;
                }
            }
            if (_modelsAssembly != null)
                assemblyBuilder.AddAssemblyReference(_modelsAssembly);
        }

        private static Assembly GetModelsAssembly()
        {
            // ensure we have a proper App_Data directory
            var appData = HostingEnvironment.MapPath("~/App_Data");
            if (appData == null || !Directory.Exists(appData)) return null;

            var umbraco = Application.GetApplication();
            var typeModels = umbraco.GetAllTypes();

            var builder = new TextBuilder(typeModels, ParseResult.Empty, Config.ModelsNamespace);

            var code = new StringBuilder();
            builder.Generate(code, builder.GetModelsToGenerate());

            // NOTE
            // would be interesting to figure out whether we can compile that code
            // using Roslyn...

            // write the code to a temp file
            // cannot be in Path.GetTempPath() because GetCompiledAssembly wants code in the website
            //var temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".cs");
            //var virttemp = "~/App_Data/Models/tmp." + Guid.NewGuid() + ".cs";
            var virttemp = "~/App_Data/tmp." + Guid.NewGuid() + ".cs";
            var temp = HostingEnvironment.MapPath(virttemp);
            if (temp == null)
                throw new Exception("Failed to map temp file.");
            File.WriteAllText(temp, code.ToString());

            try
            {
                // get the compiled assembly
                return BuildManager.GetCompiledAssembly(virttemp);
            }
            finally
            {
                // make sure whatever happens we properly delete the temp file
                File.Delete(temp);
            }
        }
    }
}
