using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.WebPages;
using Umbraco.Core;
using Umbraco.Core.Configuration;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web.Mvc;
using Umbraco.ModelsBuilder.AspNet.ViewEngine;
using Umbraco.ModelsBuilder.Configuration;
using Umbraco.ModelsBuilder.Umbraco;
using Umbraco.Web;
using Umbraco.Web.UI.JavaScript;

namespace Umbraco.ModelsBuilder.AspNet
{
    public class ModelsBuilderApplication : ApplicationEventHandler
    {
        protected override void ApplicationStarting(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
            // install pure-live models if required

            if (UmbracoConfig.For.ModelsBuilder().ModelsMode == ModelsMode.PureLive)
                ApplicationStartingLiveModels();

            // always setup the dashboard

            RegisterServerVars();
        }

        private void ApplicationStartingLiveModels()
        {
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

        /// <summary>
        /// Add custom server variables for angular to use
        /// </summary>
        private void RegisterServerVars()
        {
            // register our url - for the backoffice api
            ServerVariablesParser.Parsing += (sender, serverVars) =>
            {
                if (!serverVars.ContainsKey("umbracoUrls"))
                    throw new Exception("Missing umbracoUrls.");
                var umbracoUrlsObject = serverVars["umbracoUrls"];
                if (umbracoUrlsObject == null)
                    throw new Exception("Null umbracoUrls");
                var umbracoUrls = umbracoUrlsObject as Dictionary<string, object>;
                if (umbracoUrls == null)
                    throw new Exception("Invalid umbracoUrls");

                if (HttpContext.Current == null) throw new InvalidOperationException("HttpContext is null");
                var urlHelper = new UrlHelper(new RequestContext(new HttpContextWrapper(HttpContext.Current), new RouteData()));

                umbracoUrls["modelsBuilderBaseUrl"] = urlHelper.GetUmbracoApiServiceBaseUrl<ModelsBuilderBackOfficeController>(controller => controller.BuildModels());
            };
        }
    }
}
