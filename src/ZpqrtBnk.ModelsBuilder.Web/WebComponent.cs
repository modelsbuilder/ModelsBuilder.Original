using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Umbraco.Core.Composing;
using Umbraco.Core.Configuration;
using Umbraco.Web;
using Umbraco.Web.JavaScript;
using ZpqrtBnk.ModelsBuilder.Configuration;
using ZpqrtBnk.ModelsBuilder.Web.Api;
using ZpqrtBnk.ModelsBuilder.Web.Umbraco;

namespace ZpqrtBnk.ModelsBuilder.Web
{
    public class WebComponent : IComponent
    {
        private readonly IGlobalSettings _globalSettings;
        private readonly Config _config;

        public WebComponent(IGlobalSettings globalSettings, Config config)
        {
            _globalSettings = globalSettings;
            _config = config;
        }

        public void Initialize()
        {
            InstallServerVars();

            if (_config.IsApiServer)
            {
                ModelsBuilderApiController.Route(_globalSettings.GetUmbracoMvcArea());
            }
        }

        public void Terminate()
        { }

        private void InstallServerVars()
        {
            // register our url - for the backoffice api
            ServerVariablesParser.Parsing += (sender, serverVars) =>
            {
                if (!serverVars.ContainsKey("umbracoUrls"))
                    throw new Exception("Missing umbracoUrls.");
                var umbracoUrlsObject = serverVars["umbracoUrls"];
                if (umbracoUrlsObject == null)
                    throw new Exception("Null umbracoUrls");
                if (!(umbracoUrlsObject is Dictionary<string, object> umbracoUrls))
                    throw new Exception("Invalid umbracoUrls");

                if (!serverVars.ContainsKey("umbracoPlugins"))
                    throw new Exception("Missing umbracoPlugins.");
                if (!(serverVars["umbracoPlugins"] is Dictionary<string, object> umbracoPlugins))
                    throw new Exception("Invalid umbracoPlugins");

                if (HttpContext.Current == null) throw new InvalidOperationException("HttpContext is null");
                var urlHelper = new UrlHelper(new RequestContext(new HttpContextWrapper(HttpContext.Current), new RouteData()));

                umbracoUrls["modelsBuilderBaseUrl"] = urlHelper.GetUmbracoApiServiceBaseUrl<ModelsBuilderController>(controller => controller.BuildModels());
                umbracoPlugins["modelsBuilder"] = GetModelsBuilderSettings();

                // see modelsbuilder.resource.js
                // see Core's contenttypehelper.service.js service
                // also register the plugin as 'modelsBuilder' so the Core UI can see it,
                // and enhance 'Save' buttons with 'Save and Generate Models'
                umbracoPlugins["modelsBuilder"] = umbracoPlugins["modelsBuilder"];
            };
        }

        private Dictionary<string, object> GetModelsBuilderSettings()
        {
            var settings = new Dictionary<string, object>
            {
                {"enabled", _config.Enable}
            };

            return settings;
        }
    }
}
