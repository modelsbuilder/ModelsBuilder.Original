using System.Collections.Generic;
using Our.ModelsBuilder.Options;
using Umbraco.Core.Manifest;

namespace Our.ModelsBuilder.Web
{
    public class WebManifestFilter : IManifestFilter
    {
        private readonly ModelsBuilderOptions _options;

        public WebManifestFilter(ModelsBuilderOptions options)
        {
            _options = options;
        }

        public void Filter(List<PackageManifest> manifests)
        {
            // we deploy files, but not the manifest, which we include here
            // but only if BackOffice is enabled

            if (!_options.EnableBackOffice)
                return;

            manifests.Add(new PackageManifest
            {
                Source = "(builtin)",
                Dashboards = new[] 
                {
                    new ManifestDashboard 
                    { 
                        Alias = "settingsModelsBuilder",
                        View = "~/App_Plugins/Our.ModelsBuilder/modelsbuilder.html",
                        Sections = new[] { "settings" },
                        Weight = 40
                    }
                },
                Scripts = new[]
                {
                    "/App_Plugins/Our.ModelsBuilder/modelsbuilder.controller.js",
                    "/App_Plugins/Our.ModelsBuilder/modelsbuilder.resource.js"
                }
            });
        }
    }
}
