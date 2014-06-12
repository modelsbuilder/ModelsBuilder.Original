using System;
using System.IO;
using System.Text;
using System.Web.Compilation;
using System.Web.Hosting;
using Zbu.ModelsBuilder.Configuration;

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
        public override void GenerateCode(AssemblyBuilder assemblyBuilder)
        {
            // if live models are enabled, compile & add assembly
            if (Config.EnableLiveModels)
                AddModelsAssemblyReference(assemblyBuilder);

            base.GenerateCode(assemblyBuilder);
        }

        private static void AddModelsAssemblyReference(AssemblyBuilder assemblyBuilder)
        {
            // ensure we have a proper App_Data directory
            var appData = HostingEnvironment.MapPath("~/App_Data");
            if (appData == null || !Directory.Exists(appData)) return;

            // FIXME - but obviously we'd want to cache the assembly
            // FIXME - and have an event trigger when anything changes

            // ensure we have a models directory and it's not empty
            var modelsDirectory = Path.Combine(appData, "Models");
            if (!Directory.Exists(modelsDirectory)) return;
            var files = Directory.GetFiles(modelsDirectory, "*.cs");
            if (files.Length == 0) return;

            // concatenate all code files into one
            var code = new StringBuilder();
            foreach (var file in files)
            {
                code.AppendFormat("// FILE: {0}\n\n", file);
                var text = File.ReadAllText(file);
                code.Append(text);
                code.Append("\n\n");
            }

            // write the code to a temp file
            var temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".cs");
            File.WriteAllText(temp, code.ToString());

            try
            {
                // get the compiled assembly and add as a reference
                var assembly = BuildManager.GetCompiledAssembly(temp);
                assemblyBuilder.AddAssemblyReference(assembly);
            }
            finally
            {
                // make sure whatever happens we properly delete the temp file
                File.Delete(temp);
            }
        }
    }
}
