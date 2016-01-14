using System.Web.Mvc;
using System.Web.WebPages;
using Umbraco.Core;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web.Mvc;
using Umbraco.ModelsBuilder.AspNet.ViewEngine;
using Umbraco.ModelsBuilder.Umbraco;

namespace Umbraco.ModelsBuilder.AspNet
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

            // fixme - VirtualPathFactory?
            // is what handles layouts for views - if we don't have it, then the layouts
            // don't seem to see our models nor to be compiled by Roslyn - but then, is
            // it taking care both of Umbraco templates, and regular MVC? should we filter
            // the virtual paths in the view engine?
            VirtualPathFactoryManager.RegisterVirtualPathFactory(renderViewEngine);

            var factory = new PureLiveModelFactory(renderViewEngine, pluginViewEngine);
            PublishedContentModelFactoryResolver.Current.SetFactory(factory);

            // the following would add @using statement in every view so user's don't
            // have to do it - however, then noone understands where the @using statement
            // comes from, and it cannot be avoided / removed --- DISABLED
            //
            /*
            // no need for @using in views
            // note:
            //  we are NOT using the in-code attribute here, config is required
            //  because that would require parsing the code... and what if it changes?
            //  we can AddGlobalImport not sure we can remove one anyways
            var modelsNamespace = Configuration.Config.ModelsNamespace;
            if (string.IsNullOrWhiteSpace(modelsNamespace))
                modelsNamespace = Configuration.Config.DefaultModelsNamespace;
            System.Web.WebPages.Razor.WebPageRazorHost.AddGlobalImport(modelsNamespace);
            */
        }
    }
}
