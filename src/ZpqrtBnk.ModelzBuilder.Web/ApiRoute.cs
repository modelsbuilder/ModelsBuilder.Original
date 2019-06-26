using System;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;

namespace ZpqrtBnk.ModelzBuilder.Web
{
    public static class ApiRoute
    {
        public static string Route<TController>(string umbracoPath)
            => Route(umbracoPath, typeof(TController));

        public static string Route(string umbracoPath, Type typeofController)
        {
            // see Umbraco.Web.Runtime.WebFinalComponent

            var name = typeofController.Name;
            name = name.Substring(0, name.Length - "Controller".Length);

            var urlBase = umbracoPath + "/" + name + "/";
            var url = urlBase + "{action}/{id}";
            var route = RouteTable.Routes.MapHttpRoute(
                "zpqrtbnk-modelzbuilder-" + name,
                url,
                new { controller = name, id = UrlParameter.Optional },
                new[] { typeofController.Namespace });

            if (route.DataTokens == null)
                route.DataTokens = new RouteValueDictionary();

            route.DataTokens.Add(global::Umbraco.Core.Constants.Web.UmbracoDataToken, "api");

            return urlBase;
        }
    }
}
