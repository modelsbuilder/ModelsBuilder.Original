using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web.Compilation;
using System.Web.Hosting;
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
        private static Assembly _modelsAssembly;

        public override void GenerateCode(AssemblyBuilder assemblyBuilder)
        {
            // if live models are enabled, compile & add assembly
            if (Config.EnableLiveModels)
                AddModelsAssemblyReference(assemblyBuilder);

            base.GenerateCode(assemblyBuilder);
        }

        private static void AddModelsAssemblyReference(AssemblyBuilder assemblyBuilder)
        {
            lock (LockO)
            {
                if (_modelsAssembly == null && !_triedToGetModelsAssemblyAlready)
                    _modelsAssembly = GetModelsAssembly();
                _triedToGetModelsAssemblyAlready = true;
            }
            if (_modelsAssembly != null)
                assemblyBuilder.AddAssemblyReference(_modelsAssembly);
        }

        private static Assembly GetModelsAssembly()
        {
            // ensure we have a proper App_Data directory
            var appData = HostingEnvironment.MapPath("~/App_Data");
            if (appData == null || !Directory.Exists(appData)) return null;

            //// ensure we have a models directory and it's not empty
            //var modelsDirectory = Path.Combine(appData, "Models");
            //if (!Directory.Exists(modelsDirectory)) return null;
            //var files = Directory.GetFiles(modelsDirectory, "*.cs");
            //if (files.Length == 0) return null;

            //// concatenate all code files into one
            //var code = new StringBuilder();
            //foreach (var file in files)
            //{
            //    code.AppendFormat("// FILE: {0}\n\n", file);
            //    var text = File.ReadAllText(file);
            //    code.Append(text);
            //    code.Append("\n\n");
            //}

            var umbraco = Application.GetApplication();
            var typeModels = umbraco.GetAllTypes();

            var parseResult = new CodeParser().Parse(new Dictionary<string, string>());
            var builder = new TextBuilder(typeModels, parseResult, Config.ModelsNamespace);

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
