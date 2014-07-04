using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Web.Compilation;
using System.Web.Hosting;
using Umbraco.Web.Cache;
using Zbu.ModelsBuilder.Building;
using Zbu.ModelsBuilder.Configuration;

namespace Zbu.ModelsBuilder.Umbraco
{
    public class ModelsAssemblyProvider
    {
        private static readonly object LockO = new object();
        private static bool _triedToGetModelsAssemblyAlready;
        private static bool _initialized;
        private static Assembly _modelsAssembly;

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

        public static Assembly ModelsAssembly
        {
            get
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

                    return _modelsAssembly;
                }
            }
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

            // use the Roslyn compiler!
            //
            // NOT! this is a bad idea because the BuildManager wants the assembly
            // on disk not in memory... BuildManager.GetCompiledAssembly does generate
            // the assembly, but also ensures it is saved in a temp. location that
            // the BuildManager can reference at compile time. so... can't work.
            //
            //var compiler = new Compiler();
            //foreach (var asm in BuildManager.GetReferencedAssemblies().Cast<Assembly>())
            //    compiler.ReferencedAssemblies.Add(asm);
            //return compiler.Compile(builder.GetModelsNamespace(), code.ToString());

            // use the BuildManager compiler...
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
