using System.Web.Mvc;
using System.Web.WebPages;
using Umbraco.Core;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web.Mvc;
using Zbu.ModelsBuilder.AspNet.ViewEngine;
using Zbu.ModelsBuilder.Umbraco;

namespace Zbu.ModelsBuilder.AspNet
{
    public class ModelsBuilderApplication : ApplicationEventHandler
    {
        protected override void ApplicationStarting(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
            // install pure-live models if required

            if (!Configuration.Config.EnablePureLiveModels)
                return;

            var renderViewEngine = new RoslynRenderViewEngine();
            var pluginViewEngine = new RoslynPluginViewEngine();

            // not! there are engines we don't want to remove
            //ViewEngines.Engines.Clear();

            // better substitute our engines
            ViewEngines.Engines.Substitute<RenderViewEngine>(renderViewEngine);
            ViewEngines.Engines.Substitute<PluginViewEngine>(pluginViewEngine);

            // FIXME WHAT IS THIS?!
            VirtualPathFactoryManager.RegisterVirtualPathFactory(renderViewEngine);

            var factory = new PureLiveModelFactory(renderViewEngine, pluginViewEngine);
            PublishedContentModelFactoryResolver.Current.SetFactory(factory);

            // no need for @using in views
            // note:
            //  we are NOT using the in-code attribute here, config is required
            //  because that would require parsing the code... and what if it changes?
            //  we can AddGlobalImport not sure we can remove one anyways
            var modelsNamespace = Configuration.Config.ModelsNamespace;
            if (string.IsNullOrWhiteSpace(modelsNamespace))
                modelsNamespace = Configuration.Config.DefaultModelsNamespace;
            System.Web.WebPages.Razor.WebPageRazorHost.AddGlobalImport(modelsNamespace);
        }
    }
}
