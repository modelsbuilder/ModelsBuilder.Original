using System.Linq;
using Umbraco.Core;
using Umbraco.Core.Composing;
using ZpqrtBnk.ModelzBuilder.Umbraco;
using Umbraco.Web.WebApi;
using Umbraco.Core.Manifest;
using System.Collections.Generic;
using Umbraco.Core.IO;

namespace ZpqrtBnk.ModelzBuilder.Web
{
    [Disable(typeof(ModelsBuilderComposer))]
    [RuntimeLevel(MinLevel = RuntimeLevel.Run)]
    public class WebComposer : IUserComposer
    {
        public void Compose(Composition composition)
        {
            // kill Umbraco.ModelsBuilder package manifest entirely, replace with ours
            composition.ManifestFilters().Append<WebManifestFilter>();

            // setup the API if enabled (and in debug mode)

            // the controller is hidden from type finder (to prevent it from being always and
            // automatically registered), which means that Umbraco.Web.Composing.CompositionExtensions
            // Controllers has *not* registered it into the container, and that it is not part of
            // UmbracoApiControllerTypeCollection (and won't get routed etc)

            // so...
            // add it to the collection + register it in the container

            if (composition.Configs.ModelsBuilder().ApiServer)
            {
                // add the controller to the list of known controllers
                composition.WithCollectionBuilder<UmbracoApiControllerTypeCollectionBuilder>()
                    .Add<ModelsBuilderApiController>();

                // register the controller into the container
                composition.Register(typeof(ModelsBuilderApiController), Lifetime.Request);
            }
        }
    }

    // fixme move
    public class WebManifestFilter : IManifestFilter
    {
        public void Filter(List<PackageManifest> manifests)
        {
            // remove ModelsBuilder built-in manifest
            // this disables models builder UI entirely (dashboards, buttons)
            var modelsBuilder = manifests.FirstOrDefault(x => x.Source.EndsWith("\\App_Plugins\\ModelsBuilder\\package.manifest"));

            if (modelsBuilder != null)
                manifests.Remove(modelsBuilder);

            // fixme files locations?! or shall we just put our own manifest there?

            manifests.Add(new PackageManifest
            {
                Source = "(builtin)",
                Dashboards = new[] { new ManifestDashboard 
                    { 
                        Alias = "settingsModelzBuilder", 
                        View = "~/App_Plugins/ModelsBuilder/modelsbuilder.html",
                        Sections = new[] { "settings"}, 
                        Weight = 40 
                    }},
                Scripts = new[] 
                    {
                        IOHelper.ResolveVirtualUrl("~/App_Plugins/ModelsBuilder/modelsbuilder.controller.js"),
                        IOHelper.ResolveVirtualUrl("~/App_Plugins/ModelsBuilder/modelsbuilder.resource.js")
                    }
            });
        }
    }
}
