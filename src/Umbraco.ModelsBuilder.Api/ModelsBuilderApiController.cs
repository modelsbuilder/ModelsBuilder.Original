using System.Net;
using System.Net.Http;
using Semver;
using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Core.Configuration;
using Umbraco.ModelsBuilder.Configuration;
using Umbraco.ModelsBuilder.Umbraco;
using Umbraco.Web.Mvc;
using Umbraco.Web.WebApi;

namespace Umbraco.ModelsBuilder.Api
{
    // read http://umbraco.com/follow-us/blog-archive/2014/1/17/heads-up,-breaking-change-coming-in-702-and-62.aspx
    // read http://our.umbraco.org/forum/developers/api-questions/43025-Web-API-authentication
    // UmbracoAuthorizedApiController :: /Umbraco/BackOffice/Zbu/ModelsBuilderApi/GetTypeModels
    // UmbracoApiController :: /Umbraco/Zbu/ModelsBuilderApi/GetTypeModels ??  UNLESS marked with isbackoffice
    //
    // BEWARE! the controller url is hard-coded in ModelsBuilderApi and needs to be in sync!

    [PluginController(ControllerArea)]
    [IsBackOffice]
    [HideFromTypeFinder] // make sure it is *not* automatically registered
    //[UmbracoApplicationAuthorize(Constants.Applications.Settings)] // see ApiBasicAuthFilter - that one would be for ASP.NET identity
    public class ModelsBuilderApiController : UmbracoApiController // UmbracoAuthorizedApiController - for ASP.NET identity
    {
        public const string ControllerArea = "ModelsBuilder";

        private readonly UmbracoServices _umbracoServices;

        public ModelsBuilderApiController(UmbracoServices umbracoServices)
        {
            _umbracoServices = umbracoServices;
        }

        private static Config Config => Current.Configs.ModelsBuilder();

        // invoked by the API
        [System.Web.Http.HttpPost] // use the http one, not mvc, with api controllers!
        [ApiBasicAuthFilter("settings")] // have to use our own, non-cookie-based, auth
        public HttpResponseMessage ValidateClientVersion(ValidateClientVersionData data)
        {
            if (!Config.ApiServer)
                return Request.CreateResponse(HttpStatusCode.Forbidden, "API server does not want to talk to you.");

            if (!ModelState.IsValid || data == null || !data.IsValid)
                return Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid data.");

            var checkResult = CheckVersion(data.ClientVersion, data.MinServerVersionSupportingClient);
            return (checkResult.Success
                ? Request.CreateResponse(HttpStatusCode.OK, "OK", Configuration.Formatters.JsonFormatter)
                : checkResult.Result);
        }

        // invoked by the API
        [System.Web.Http.HttpPost] // use the http one, not mvc, with api controllers!
        [ApiBasicAuthFilter("settings")] // have to use our own, non-cookie-based, auth
        public HttpResponseMessage GetModels(GetModelsData data)
        {
            if (!Config.ApiServer)
                return Request.CreateResponse(HttpStatusCode.Forbidden, "API server does not want to talk to you.");

            if (!ModelState.IsValid || data == null || !data.IsValid)
                return Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid data.");

            var checkResult = CheckVersion(data.ClientVersion, data.MinServerVersionSupportingClient);
            if (!checkResult.Success)
                return checkResult.Result;

            var models = ApiHelper.GetModels(_umbracoServices, data.Namespace, data.Files);

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
    }
}
