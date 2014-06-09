using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;
using System.Web.Http;
using System.Web.Http.Hosting;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;
using umbraco.BusinessLogic;
using Umbraco.Core.IO;
using Umbraco.Web.Mvc;
using Umbraco.Core;
using Umbraco.Web.WebApi;
using Application = Zbu.ModelsBuilder.Umbraco.Application;
using User = Umbraco.Core.Models.Membership.User;

namespace Zbu.ModelsBuilder.AspNet
{
    // read http://umbraco.com/follow-us/blog-archive/2014/1/17/heads-up,-breaking-change-coming-in-702-and-62.aspx
    // read http://our.umbraco.org/forum/developers/api-questions/43025-Web-API-authentication
    // UmbracoAuthorizedApiController :: /Umbraco/BackOffice/Zbu/ModelsBuilderApi/GetTypeModels
    // UmbracoApiController :: /Umbraco/Zbu/ModelsBuilderApi/GetTypeModels ??  UNLESS marked with isbackoffice
    [PluginController(ZbuArea)]
    [IsBackOffice] // because we want back-office users
    public class ModelsBuilderApiController : UmbracoApiController //UmbracoAuthorizedApiController
    {
        public const string ZbuArea = "Zbu";

        private static readonly Version ServerVersion = typeof(TypeModel).Assembly.GetName().Version;
        private static readonly Version MinClientVersion = ServerVersion;
        private static readonly Version MaxClientVersion = ServerVersion;

        private static void AcceptClientVersion(Version clientVersion)
        {
            if (clientVersion < MinClientVersion || clientVersion > MaxClientVersion)
                throw new Exception(string.Format("Client version ({0}) is not compatible with server version({1}).",
                    clientVersion, ServerVersion));
        }

        public class BuildResult
        {
            public bool Success;
            public string Message;
        }

        public class GetModelsData
        {
            public Version ClientVersion { get; set; }
            public string Namespace { get; set; }
            public IDictionary<string, string> Files { get; set; }
        }

        [System.Web.Http.HttpPost] // use the http one, not mvc, with api controllers!
        [ModelsBuilderAuthFilter("developer")] // have to use our own, non-cookie-based, auth
        public HttpResponseMessage ValidateClientVersion(Version clientVersion)
        {
            AcceptClientVersion(clientVersion); // or throw
            return Request.CreateResponse(HttpStatusCode.OK, "OK", Configuration.Formatters.JsonFormatter);
        }

        // invoked by the dashboard
        // requires that the user is logged into the backoffice
        // and has access to the developer section
        //
        [System.Web.Http.HttpGet] // use the http one, not mvc, with api controllers!
        [global::Umbraco.Web.WebApi.UmbracoAuthorize] // can use Umbraco's
        public HttpResponseMessage BuildModels()
        {
            try
            {
                // the UmbracoAuthorize attribute validates the current user
                // the UmbracoAuthorizedApiController would in addition check for .Disabled and .NoConsole
                // but to do it it relies on internal methods so we have to do it here explicitely
                var user = umbraco.BusinessLogic.User.GetCurrent();
                if (user.Disabled || user.NoConsole )
                    return new HttpResponseMessage(HttpStatusCode.Unauthorized);
                if (user.Applications.All(x => x.alias != "developer"))
                    return new HttpResponseMessage(HttpStatusCode.Unauthorized);

                var appData = HostingEnvironment.MapPath("~/App_Data");
                if (appData == null)
                    throw new Exception("Panic: appData is null.");

                var appCode = HostingEnvironment.MapPath("~/App_Code");
                if (appCode == null)
                    throw new Exception("Panic: appCode is null.");

                GenerateModels(appData);

                var buildModels = ConfigurationManager.AppSettings["Zbu.ModelsBuilder.AspNet.BuildModels"] == "true";
                if (buildModels)
                    TouchModelsFile(appCode); // will recycle the app domain - but this request will end properly

                var result = new BuildResult {Success = true};
                return Request.CreateResponse(HttpStatusCode.OK, result, Configuration.Formatters.JsonFormatter);

            }
            catch (Exception e)
            {
                var message = string.Format("{0}: {1}\r\n{2}", e.GetType().FullName, e.Message, e.StackTrace);
                var result = new BuildResult { Success = false, Message = message };
                return Request.CreateResponse(HttpStatusCode.OK, result, Configuration.Formatters.JsonFormatter);
            }
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

        // invoked by the API
        // wich should log the user in using basic auth
        // and then the user must have access to the developer section
        //
        [System.Web.Http.HttpPost] // use the http one, not mvc, with api controllers!
        [ModelsBuilderAuthFilter("developer")] // have to use our own, non-cookie-based, auth
        public HttpResponseMessage GetModels(GetModelsData data)
        {
            AcceptClientVersion(data.ClientVersion); // or throw

            var umbraco = Application.GetApplication();
            var typeModels = umbraco.GetContentAndMediaTypes();

            var builder = new TextBuilder(typeModels);
            builder.Namespace = data.Namespace;
            var disco = new CodeDiscovery().Discover(data.Files);
            builder.Prepare(disco);

            var models = new Dictionary<string, string>();
            foreach (var typeModel in builder.GetModelsToGenerate())
            {
                var sb = new StringBuilder();
                builder.Generate(sb, typeModel);
                models[typeModel.Name] = sb.ToString();
            }

            return Request.CreateResponse(HttpStatusCode.OK, models, Configuration.Formatters.JsonFormatter);
        }

        private static void GenerateModels(string appData)
        {
            var modelsDirectory = Path.Combine(appData, "Models");
            if (!Directory.Exists(modelsDirectory))
                Directory.CreateDirectory(modelsDirectory);

            foreach (var file in Directory.GetFiles(modelsDirectory, "*.generated.cs"))
                File.Delete(file);

            var umbraco = Application.GetApplication();
            var typeModels = umbraco.GetContentAndMediaTypes();

            var ns = ConfigurationManager.AppSettings["Zbu.ModelsBuilder.ModelsNamespace"];
            if (string.IsNullOrWhiteSpace(ns)) ns = "Umbraco.Web.PublishedContentModels";

            var builder = new TextBuilder(typeModels);
            builder.Namespace = ns;
            var ourFiles = Directory.GetFiles(modelsDirectory, "*.cs").ToDictionary(x => x, File.ReadAllText);
            var disco = new CodeDiscovery().Discover(ourFiles);
            builder.Prepare(disco);

            foreach (var typeModel in builder.GetModelsToGenerate())
            {
                var sb = new StringBuilder();
                builder.Generate(sb, typeModel);
                var filename = Path.Combine(modelsDirectory, typeModel.Name + ".generated.cs");
                File.WriteAllText(filename, sb.ToString());
            }
        }

        private static void TouchModelsFile(string appCode)
        {
            var modelsFile = Path.Combine(appCode, "build.models");

            // touch the file & make sure it exists, will recycle the domain
            var text = string.Format("ZpqrtBnk Umbraco ModelsBuilder\r\n"
                + "Actual models code in ~/App_Data/Models\r\n"
                + "Removing this file disables all generated models\r\n"
                + "{0:yyyy-MM-ddTHH:mm:ssZ}", DateTime.UtcNow);
            File.WriteAllText(modelsFile, text);
        }

        // FIXME - what would be the proper way to get these urls without hard-coding them?
        // ok, I just want the route to my controller, statically, so I can have it in the dashboard
        // which is not part of the MVC world... been through MSDN & StackOverflow & such for too long
        // without finding the solution - so it's hard-coded here in all its dirtyness.
        public const string ValidateClientVersionUrl = "/Umbraco/BackOffice/Zbu/ModelsBuilderApi/ValidateClientVersion";
        public const string BuildModelsUrl = "/Umbraco/BackOffice/Zbu/ModelsBuilderApi/BuildModels";
        //public const string GetTypeModelsUrl = "/Umbraco/BackOffice/Zbu/ModelsBuilderApi/GetTypeModels";
        public const string GetModelsUrl = "/Umbraco/BackOffice/Zbu/ModelsBuilderApi/GetModels";
    }
}
