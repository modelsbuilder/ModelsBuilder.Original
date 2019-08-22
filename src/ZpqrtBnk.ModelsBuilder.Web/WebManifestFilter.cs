using System.Collections.Generic;
using System.Linq;
using Umbraco.Core.Manifest;
using ZpqrtBnk.ModelsBuilder.Configuration;

namespace ZpqrtBnk.ModelsBuilder.Web
{
    public class WebManifestFilter : IManifestFilter
    {
        private Config _config;

        public WebManifestFilter(Config config)
        {
            _config = config;
        }

        public void Filter(List<PackageManifest> manifests)
        {
            // remove ModelsBuilder built-in manifest
            // this disables models builder UI entirely (dashboards, buttons...)
            var modelsBuilder = manifests.FirstOrDefault(x => x.Source.EndsWith("\\App_Plugins\\ModelsBuilder\\package.manifest"));

            if (modelsBuilder != null)
                manifests.Remove(modelsBuilder);

            // we deploy files, but not the manifest, which we include here
            // but only if BackOffice is enabled

            if (!_config.EnableBackOffice)
                return;

            manifests.Add(new PackageManifest
            {
                Source = "(builtin)",
                Dashboards = new[] 
                {
                    new ManifestDashboard 
                    { 
                        Alias = "settingsModelsBuilder",
                        View = "~/App_Plugins/ModelsBuilder/modelsbuilder.html",
                        Sections = new[] { "settings" },
                        Weight = 40
                    }
                },
                Scripts = new[]
                {
                    "/App_Plugins/ModelsBuilder/modelsbuilder.controller.js",
                    "/App_Plugins/ModelsBuilder/modelsbuilder.resource.js"
                }
            });
        }
    }
}
