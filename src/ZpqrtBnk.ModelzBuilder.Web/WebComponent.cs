using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Umbraco.Core.Composing;
using Umbraco.Core.Configuration;
using Umbraco.Web;
using Umbraco.Web.JavaScript;
using ZpqrtBnk.ModelzBuilder.Configuration;
using ZpqrtBnk.ModelzBuilder.Web.Api;
using ZpqrtBnk.ModelzBuilder.Web.Umbraco;

namespace ZpqrtBnk.ModelzBuilder.Web
{
    public class WebComponent : IComponent
    {
        private IGlobalSettings _globalSettings;
        private Config _config;

        public WebComponent(IGlobalSettings globalSettings, Config config)
        {
            _globalSettings = globalSettings;
            _config = config;
        }

        public void Initialize()
        {
            InstallServerVars();

            if (_config.ApiServer)
            {
                ModelzBuilderApiController.Route(_globalSettings.GetUmbracoMvcArea());
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

                umbracoUrls["modelsBuilderBaseUrl"] = urlHelper.GetUmbracoApiServiceBaseUrl<ModelzBuilderController>(controller => controller.BuildModels());
                umbracoPlugins["modelsBuilder"] = GetModelsBuilderSettings();
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
