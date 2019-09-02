using System.Net;
using System.Net.Http;
using Semver;
using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Web.Mvc;
using Umbraco.Web.WebApi;
using ZpqrtBnk.ModelsBuilder.Api;
using ZpqrtBnk.ModelsBuilder.Building;
using ZpqrtBnk.ModelsBuilder.Configuration;
using ZpqrtBnk.ModelsBuilder.Umbraco;
// use the http one, not mvc, with api controllers!
using HttpGetAttribute = System.Web.Http.HttpGetAttribute;
using HttpPostAttribute = System.Web.Http.HttpPostAttribute;

namespace ZpqrtBnk.ModelsBuilder.Web.Api
{
    // read http://umbraco.com/follow-us/blog-archive/2014/1/17/heads-up,-breaking-change-coming-in-702-and-62.aspx
    // read http://our.umbraco.org/forum/developers/api-questions/43025-Web-API-authentication
    // UmbracoAuthorizedApiController :: /Umbraco/BackOffice/Zbu/ModelsBuilderApi/GetTypeModels
    // UmbracoApiController :: /Umbraco/Zbu/ModelsBuilderApi/GetTypeModels ??  UNLESS marked with isbackoffice
    //   :: /umbraco/api/keepalive/ping
    //
    // routed by WebComponent as /umbraco/ModelsBuilderApi/{action}


    [DisableBrowserCache]
    [HideFromTypeFinder] // make sure it is *not* automatically registered
    //[UmbracoApplicationAuthorize(Constants.Applications.Settings)] // see ApiBasicAuthFilter - that one would be for ASP.NET identity
    public class ModelsBuilderApiController : UmbracoApiController // UmbracoAuthorizedApiController - for ASP.NET identity
    {
        private readonly UmbracoServices _umbracoServices;
        private readonly IBuilderFactory _builderFactory;

        public ModelsBuilderApiController(UmbracoServices umbracoServices, IBuilderFactory builderFactory)
        {
            _umbracoServices = umbracoServices;
            _builderFactory = builderFactory;
        }

        private static Config Config => Current.Configs.ModelsBuilder();

        [HttpGet]
        [ApiBasicAuthFilter("settings")] // have to use our own, non-cookie-based, auth
        public HttpResponseMessage GetApiVersion()
        {
            if (!Config.IsApiServer)
                return Request.CreateResponse(HttpStatusCode.Forbidden, "API server does not want to talk to you.");

            if (!ModelState.IsValid)
                return Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid data.");

            return Request.CreateResponse(HttpStatusCode.OK, ApiVersion.Current, Configuration.Formatters.JsonFormatter);
        }

        // invoked by the API
        [HttpPost]
        [ApiBasicAuthFilter("settings")] // have to use our own, non-cookie-based, auth
        public HttpResponseMessage ValidateClientVersion(ValidateClientVersionData data)
        {
            if (!Config.IsApiServer)
                return Request.CreateResponse(HttpStatusCode.Forbidden, "API server does not want to talk to you.");

            if (!ModelState.IsValid || data == null || !data.IsValid)
                return Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid data.");

            var checkResult = CheckVersion(data.ClientVersion, data.MinServerVersionSupportingClient);
            return (checkResult.Success
                ? Request.CreateResponse(HttpStatusCode.OK, "OK", Configuration.Formatters.JsonFormatter)
                : checkResult.Result);
        }

        // invoked by the API
        [HttpPost]
        [ApiBasicAuthFilter("settings")] // have to use our own, non-cookie-based, auth
        public HttpResponseMessage GetModels(GetModelsData data)
        {
            if (!Config.IsApiServer)
                return Request.CreateResponse(HttpStatusCode.Forbidden, "API server does not want to talk to you.");

            if (!ModelState.IsValid || data == null || !data.IsValid)
                return Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid data.");

            var checkResult = CheckVersion(data.ClientVersion, data.MinServerVersionSupportingClient);
            if (!checkResult.Success)
                return checkResult.Result;

            var models = Generator.GetModels(_umbracoServices, _builderFactory, data.Namespace, data.Files);

            return Request.CreateResponse(HttpStatusCode.OK, models, Configuration.Formatters.JsonFormatter);
        }

        private Attempt<HttpResponseMessage> CheckVersion(SemVersion clientVersion, SemVersion minServerVersionSupportingClient)
        {
            if (clientVersion == null)
                return Attempt<HttpResponseMessage>.Fail(Request.CreateResponse(HttpStatusCode.Forbidden,
                    $"API version conflict: client version (<null>) is not compatible with server version({ApiVersion.Current.Version})."));

            // minServerVersionSupportingClient can be null
            var isOk = ApiVersion.Current.IsCompatibleWith(clientVersion, minServerVersionSupportingClient);
            var response = isOk ? null : Request.CreateResponse(HttpStatusCode.Forbidden,
                $"API version conflict: client version ({clientVersion}) is not compatible with server version({ApiVersion.Current.Version}).");

            return Attempt<HttpResponseMessage>.If(isOk, response);
        }

        public static string UrlBase { get; private set; } = string.Empty;

        public static void Route(string umbracoPath)
        {
            UrlBase = ApiRoute.Route(umbracoPath, typeof(ModelsBuilderApiController));
        }
    }
}
