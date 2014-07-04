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

    // NOTE
    // this is not used anymore because, to some extent, it cannot work
    // see note in ConfigurePublishedContentModelFactory.cs

    [BuildProviderAppliesTo(BuildProviderAppliesTo.Web | BuildProviderAppliesTo.Code)]
    public class RazorBuildProvider : System.Web.WebPages.Razor.RazorBuildProvider
    {
        public override void GenerateCode(AssemblyBuilder assemblyBuilder)
        {
            // if pure live models are enabled, compile & add assembly
            if (Config.EnablePureLiveModels)
            {
                var modelsAssembly = ModelsAssemblyProvider.ModelsAssembly;
                if (modelsAssembly != null)
                    assemblyBuilder.AddAssemblyReference(modelsAssembly);
            }

            base.GenerateCode(assemblyBuilder);
        }
    }
}
