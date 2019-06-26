using System.Linq;
using Umbraco.Core.Manifest;
using System.Collections.Generic;
using Umbraco.Core.IO;
using ZpqrtBnk.ModelzBuilder.Configuration;

namespace ZpqrtBnk.ModelzBuilder.Web
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
                        Alias = "settingsModelzBuilder",
                        View = "~/App_Plugins/ModelzBuilder/modelzbuilder.html",
                        Sections = new[] { "settings" },
                        Weight = 40
                    }
                },
                Scripts = new[]
                {
                    IOHelper.MapPath("~/App_Plugins/ModelzBuilder/modelzbuilder.controller.js"),
                    IOHelper.MapPath("~/App_Plugins/ModelzBuilder/modelzbuilder.resource.js")
                }
            });
        }
    }
}
