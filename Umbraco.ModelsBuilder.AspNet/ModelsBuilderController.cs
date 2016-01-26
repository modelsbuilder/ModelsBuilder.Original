using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Web;
using System.Web.Compilation;
using System.Web.Mvc;
using System.Web.Routing;
using Umbraco.Web.Mvc;
using Umbraco.Core;
using Umbraco.Core.Configuration;
using Umbraco.ModelsBuilder.AspNet.Dashboard;
using Umbraco.Web.WebApi;
using Umbraco.ModelsBuilder.Building;
using Umbraco.ModelsBuilder.Configuration;
using Umbraco.Web;
using Umbraco.Web.WebApi.Filters;
using Application = Umbraco.ModelsBuilder.Umbraco.Application;
using Constants = Umbraco.Core.Constants;

namespace Umbraco.ModelsBuilder.AspNet
{
    // read http://umbraco.com/follow-us/blog-archive/2014/1/17/heads-up,-breaking-change-coming-in-702-and-62.aspx
    // read http://our.umbraco.org/forum/developers/api-questions/43025-Web-API-authentication
    // UmbracoAuthorizedApiController :: /Umbraco/BackOffice/Zbu/ModelsBuilderApi/GetTypeModels
    // UmbracoApiController :: /Umbraco/Zbu/ModelsBuilderApi/GetTypeModels ??  UNLESS marked with isbackoffice

    [PluginController(ControllerArea)]
    [IsBackOffice]
    [UmbracoApplicationAuthorize(Constants.Applications.Developer)]
    public class ModelsBuilderController : UmbracoAuthorizedApiController
    {
        public const string ControllerArea = "ModelsBuilder";

        /// <summary>
        /// Returns the base url for this controller
        /// </summary>
        public static string ControllerUrl { get; private set; }

        private static readonly Lazy<string> ControllerUrlLazy = new Lazy<string>(() =>
        {
            //Return the URL based on the booted umbraco application
            if (HttpContext.Current != null)
            {
                var urlHelper = new UrlHelper(new RequestContext(new HttpContextWrapper(HttpContext.Current), new RouteData()));
                return urlHelper.GetUmbracoApiServiceBaseUrl<ModelsBuilderController>(controller => controller.GetModels(null)).EnsureEndsWith('/');
            }

            //NOTE: This could very well be incorrect depending on current route values, virtual folders, etc...
            // but without an HttpContext and without a booted Umbraco install we can't know.
            return "/Umbraco/BackOffice/" + ControllerArea + "/" + nameof(ModelsBuilderController).TrimEnd("Controller") + "/";
        });

        /// <summary>
        /// Returns the url for the action specified
        /// </summary>
        /// <param name="actionName"></param>
        /// <returns></returns>
        public static string ActionUrl(string actionName)
        {
            return ControllerUrl + actionName;
        }

        /// <summary>
        /// Static constructor
        /// </summary>
        static ModelsBuilderController()
        {
            ControllerUrl = ControllerUrlLazy.Value;
        }

        #region Models

        [DataContract]
        public class ValidateClientVersionData
        {
            // issues 32, 34... problems when serializing versions
            //
            // make sure System.Version objects are transfered as strings
            // depending on the JSON serializer version, it looks like versions are causing issues
            // see
            // http://stackoverflow.com/questions/13170386/why-system-version-in-json-string-does-not-deserialize-correctly
            //
            // if the class is marked with [DataContract] then only properties marked with [DataMember]
            // are serialized and the rest is ignored, see
            // http://www.asp.net/web-api/overview/formats-and-model-binding/json-and-xml-serialization

            [DataMember]
            public string ClientVersionString
            {
                get { return VersionToString(ClientVersion); }
                set { ClientVersion = ParseVersion(value, false, "client"); }
            }

            [DataMember]
            public string MinServerVersionSupportingClientString
            {
                get { return VersionToString(MinServerVersionSupportingClient); }
                set { MinServerVersionSupportingClient = ParseVersion(value, true, "minServer"); }
            }

            // not serialized
            public Version ClientVersion { get; set; }
            public Version MinServerVersionSupportingClient { get; set; }

            private static string VersionToString(Version version)
            {
                return version == null ? "0.0.0.0" : version.ToString();
            }

            private static Version ParseVersion(string value, bool canBeNull, string name)
            {
                if (string.IsNullOrWhiteSpace(value) && canBeNull)
                    return null;

                Version version;
                if (Version.TryParse(value, out version))
                    return version;

                throw new ArgumentException(string.Format("Failed to parse \"{0}\" as {1} version.", value, name));
            }
        }

        [DataContract]
        public class GetModelsData : ValidateClientVersionData
        {
            [DataMember]
            public string Namespace { get; set; }

            [DataMember]
            public IDictionary<string, string> Files { get; set; }
        }

        #endregion

        #region Actions

        // invoked by the API
        [System.Web.Http.HttpPost] // use the http one, not mvc, with api controllers!
        public HttpResponseMessage ValidateClientVersion(ValidateClientVersionData data)
        {
            if (!UmbracoConfig.For.ModelsBuilder().EnableApi)
                return Request.CreateResponse(HttpStatusCode.Forbidden, "API is not enabled.");

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

        // invoked by the API
        // DISABLED - works but useless, because if we return type models that
        // reference some Clr types that exist only on the server and not in the
        // remove app, then what can we do with them? Better do everything on
        // the server.
        //
        //[System.Web.Http.HttpGet] // use the http one, not mvc, with api controllers!
        //[ModelsBuilderAuthFilter("developer")] // have to use our own, non-cookie-based, auth
        //public HttpResponseMessage GetTypeModels()
        //{
        //    var umbraco = Application.GetApplication();
        //    var modelTypes = umbraco.GetContentAndMediaTypes();

        //    return Request.CreateResponse(HttpStatusCode.OK, modelTypes, Configuration.Formatters.JsonFormatter);
        //}
        //
        //public const string GetTypeModelsUrl = ControllerUrl + "/GetTypeModels";

        #endregion

        private Attempt<HttpResponseMessage> CheckVersion(Version clientVersion, Version minServerVersionSupportingClient)
        {
            if (clientVersion == null)
                return Attempt<HttpResponseMessage>.Fail(Request.CreateResponse(HttpStatusCode.Forbidden, string.Format(
                    "API version conflict: client version (<null>) is not compatible with server version({0}).",
                    ApiVersion.Current.Version)));

            // minServerVersionSupportingClient can be null
            var isOk = ApiVersion.Current.IsCompatibleWith(clientVersion, minServerVersionSupportingClient);
            var response = isOk ? null : Request.CreateResponse(HttpStatusCode.Forbidden, string.Format(
                "API version conflict: client version ({0}) is not compatible with server version({1}).",
                clientVersion, ApiVersion.Current.Version));
            return Attempt<HttpResponseMessage>.SucceedIf(isOk, response);
        }
    }
}
