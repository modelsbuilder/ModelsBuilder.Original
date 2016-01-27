using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Umbraco.Core;
using Umbraco.Core.Configuration;
using Umbraco.ModelsBuilder.Building;
using Umbraco.ModelsBuilder.Configuration;
using Umbraco.Web;
using Umbraco.Web.Mvc;
using Umbraco.Web.WebApi;
using Umbraco.Web.WebApi.Filters;
using Application = Umbraco.ModelsBuilder.Umbraco.Application;
using Constants = Umbraco.Core.Constants;

namespace Umbraco.ModelsBuilder.AspNet.Api
{
    // read http://umbraco.com/follow-us/blog-archive/2014/1/17/heads-up,-breaking-change-coming-in-702-and-62.aspx
    // read http://our.umbraco.org/forum/developers/api-questions/43025-Web-API-authentication
    // UmbracoAuthorizedApiController :: /Umbraco/BackOffice/Zbu/ModelsBuilderApi/GetTypeModels
    // UmbracoApiController :: /Umbraco/Zbu/ModelsBuilderApi/GetTypeModels ??  UNLESS marked with isbackoffice
    //
    // BEWARE! the controller url is hard-coded in ModelsBuilderApi and needs to be in sync!

    [PluginController(ControllerArea)]
    [IsBackOffice]
    [UmbracoApplicationAuthorize(Constants.Applications.Developer)]
    public class ModelsBuilderApiController : UmbracoAuthorizedApiController
    {
        public const string ControllerArea = "ModelsBuilder";

        // invoked by the API
        [System.Web.Http.HttpPost] // use the http one, not mvc, with api controllers!
        public HttpResponseMessage ValidateClientVersion(ValidateClientVersionData data)
        {
            if (!UmbracoConfig.For.ModelsBuilder().EnableApi)
                return Request.CreateResponse(HttpStatusCode.Forbidden, "API is not enabled.");

            if (!ModelState.IsValid || data == null || !data.IsValid)
                return Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid data.");

            var checkResult = CheckVersion(data.ClientVersion, data.MinServerVersionSupportingClient);
            return (checkResult.Success
                ? Request.CreateResponse(HttpStatusCode.OK, "OK", Configuration.Formatters.JsonFormatter)
                : checkResult.Result);
        }

        // invoked by the API
        [System.Web.Http.HttpPost] // use the http one, not mvc, with api controllers!
        public HttpResponseMessage GetModels(GetModelsData data)
        {
            if (!UmbracoConfig.For.ModelsBuilder().EnableApi)
                return Request.CreateResponse(HttpStatusCode.Forbidden, "API is not enabled.");

            if (!ModelState.IsValid || data == null || !data.IsValid)
                return Request.CreateResponse(HttpStatusCode.BadRequest, "Invalid data.");

            var checkResult = CheckVersion(data.ClientVersion, data.MinServerVersionSupportingClient);
            if (!checkResult.Success)
                return checkResult.Result;

            var umbraco = Application.GetApplication();
            var typeModels = umbraco.GetAllTypes();

            var parseResult = new CodeParser().ParseWithReferencedAssemblies(data.Files);
            var builder = new TextBuilder(typeModels, parseResult, data.Namespace);

            var models = new Dictionary<string, string>();
            foreach (var typeModel in builder.GetModelsToGenerate())
            {
                var sb = new StringBuilder();
                builder.Generate(sb, typeModel);
                models[typeModel.ClrName] = sb.ToString();
            }

            return Request.CreateResponse(HttpStatusCode.OK, models, Configuration.Formatters.JsonFormatter);
        }

        private Attempt<HttpResponseMessage> CheckVersion(Version clientVersion, Version minServerVersionSupportingClient)
        {
            if (clientVersion == null)
                return Attempt<HttpResponseMessage>.Fail(Request.CreateResponse(HttpStatusCode.Forbidden,
                    $"API version conflict: client version (<null>) is not compatible with server version({ApiVersion.Current.Version})."));

            // minServerVersionSupportingClient can be null
            var isOk = ApiVersion.Current.IsCompatibleWith(clientVersion, minServerVersionSupportingClient);
            var response = isOk ? null : Request.CreateResponse(HttpStatusCode.Forbidden,
                $"API version conflict: client version ({clientVersion}) is not compatible with server version({ApiVersion.Current.Version}).");

            return Attempt<HttpResponseMessage>.SucceedIf(isOk, response);
        }
    }
}
