using System;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;

namespace Our.ModelsBuilder.Web
{
    public static class ApiRoute
    {
        public static string Route<TController>(string umbracoPath)
            => Route(umbracoPath, typeof(TController));

        public static string Route(string umbracoPath, Type typeofController)
        {
            // see Umbraco.Web.Runtime.WebFinalComponent

            var controllerName = typeofController.Name;
            controllerName = controllerName.Substring(0, controllerName.Length - "Controller".Length);

            var url = umbracoPath + "/BackOffice/Api/" + controllerName;
            var route = RouteTable.Routes.MapHttpRoute(
                "our-modelsbuilder-" + controllerName.ToLowerInvariant(),
                url + "/{action}/{id}",
                new { controller = controllerName, id = UrlParameter.Optional });

            if (route.DataTokens == null)
                route.DataTokens = new RouteValueDictionary();

            route.DataTokens.Add(global::Umbraco.Core.Constants.Web.UmbracoDataToken, "api");

            route.DataTokens.Add("Namespaces", new[] { typeofController.Namespace }); // look in this namespace to create the controller
            route.DataTokens.Add("UseNamespaceFallback", false); // Don't look anywhere else except this namespace!

            return url;
        }
    }
}
