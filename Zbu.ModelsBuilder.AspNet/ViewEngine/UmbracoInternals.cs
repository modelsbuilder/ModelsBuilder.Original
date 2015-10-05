using System.Web.Mvc;

namespace Zbu.ModelsBuilder.AspNet.ViewEngine
{
    static class UmbracoInternals
    {
        // Umbraco.Web.Mvc.Constants.ViewLocation
        public const string ViewLocation = "~/Views";

        // Strings.WebConfigTemplate
        public const string WebConfigTemplate = @"..\..\umbraco.web.ui\views\web.config;System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089;Windows-1252";

        // extension method
        public static object GetDataTokenInViewContextHierarchy(ControllerContext controllerContext, string dataTokenName)
        {
            while (controllerContext != null)
            {
                if (controllerContext.RouteData.DataTokens.ContainsKey(dataTokenName))
                    return controllerContext.RouteData.DataTokens[dataTokenName];
                controllerContext = controllerContext.ParentActionViewContext;
            }
            return null;
        }
    }
}
