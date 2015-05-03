using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Web.Compilation;
using System.Web.Hosting;
using Umbraco.Web.Mvc;
using Umbraco.Core;
using Umbraco.Web.WebApi;
using Zbu.ModelsBuilder.Building;
using Zbu.ModelsBuilder.Configuration;
using Application = Zbu.ModelsBuilder.Umbraco.Application;

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
        private const string ControllerUrl = "/Umbraco/BackOffice/Zbu/ModelsBuilderApi";

        #region Models

        public class BuildResult
        {
            public bool Success;
            public string Message;
        }

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
        [ModelsBuilderAuthFilter("developer")] // have to use our own, non-cookie-based, auth
        public HttpResponseMessage ValidateClientVersion(ValidateClientVersionData data)
        {
            if (!Config.EnableApi)
                return Request.CreateResponse(HttpStatusCode.Forbidden, "API is not enabled.");

            var checkResult = CheckVersion(data.ClientVersion, data.MinServerVersionSupportingClient);
            return (checkResult.Success
                ? Request.CreateResponse(HttpStatusCode.OK, "OK", Configuration.Formatters.JsonFormatter)
                : checkResult.Result);
        }

        public const string ValidateClientVersionUrl = ControllerUrl + "/ValidateClientVersion";

        // invoked by the dashboard
        // requires that the user is logged into the backoffice and has access to the developer section
        [System.Web.Http.HttpGet] // use the http one, not mvc, with api controllers!
        [global::Umbraco.Web.WebApi.UmbracoAuthorize] // can use Umbraco's
        public HttpResponseMessage BuildModels()
        {
            try
            {
                // the UmbracoAuthorize attribute validates the current user
                // the UmbracoAuthorizedApiController would in addition check for .Disabled and .NoConsole
                // but to do it it relies on internal methods so we have to do it here explicitely

                // was doing it using legacy User class, now using new API class

                //var user = umbraco.BusinessLogic.User.GetCurrent();
                var user = UmbracoContext.Security.CurrentUser;
                //if (user.Disabled || user.NoConsole)
                if (!user.IsApproved && user.IsLockedOut)
                    return new HttpResponseMessage(HttpStatusCode.Unauthorized);
                //if (user.Applications.All(x => x.alias != "developer"))
                if (!user.AllowedSections.Contains("developer"))
                    return new HttpResponseMessage(HttpStatusCode.Unauthorized);

                if (!Config.EnableAppDataModels && !Config.EnableAppCodeModels && !Config.EnableDllModels)
                {
                    var result2 = new BuildResult { Success = false, Message = "Models generation is not enabled." };
                    return Request.CreateResponse(HttpStatusCode.OK, result2, Configuration.Formatters.JsonFormatter);
                }

                var appData = HostingEnvironment.MapPath("~/App_Data");
                if (appData == null)
                    throw new Exception("Panic: appData is null.");

                var appCode = HostingEnvironment.MapPath("~/App_Code");
                if (appCode == null)
                    throw new Exception("Panic: appCode is null.");

                var bin = HostingEnvironment.MapPath("~/bin");
                if (bin== null)
                    throw new Exception("Panic: bin is null.");

                // EnableDllModels will recycle the app domain - but this request will end properly
                GenerateModels(appData, Config.EnableDllModels ? bin : null);

                // will recycle the app domain - but this request will end properly
                if (Config.EnableAppCodeModels)
                    TouchModelsFile(appCode);

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

        public const string BuildModelsUrl = ControllerUrl + "/BuildModels";

        // invoked by the API
        [System.Web.Http.HttpPost] // use the http one, not mvc, with api controllers!
        [ModelsBuilderAuthFilter("developer")] // have to use our own, non-cookie-based, auth
        public HttpResponseMessage GetModels(GetModelsData data)
        {
            if (!Config.EnableApi)
                return Request.CreateResponse(HttpStatusCode.Forbidden, "API is not enabled.");

            var checkResult = CheckVersion(data.ClientVersion, data.MinServerVersionSupportingClient);
            if (!checkResult.Success)
                return checkResult.Result;

            var umbraco = Application.GetApplication();
            var typeModels = umbraco.GetAllTypes();

            // using BuildManager references
            var referencedAssemblies = BuildManager.GetReferencedAssemblies().Cast<Assembly>().ToArray();

            var parseResult = new CodeParser().Parse(data.Files, referencedAssemblies);
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

        public const string GetModelsUrl = ControllerUrl + "/GetModels";

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

        public static void GenerateModels(string appData, string bin)
        {
            var modelsDirectory = Path.Combine(appData, "Models");
            if (!Directory.Exists(modelsDirectory))
                Directory.CreateDirectory(modelsDirectory);

            foreach (var file in Directory.GetFiles(modelsDirectory, "*.generated.cs"))
                File.Delete(file);

            var umbraco = Application.GetApplication();
            var typeModels = umbraco.GetAllTypes();

            // using BuildManager references
            var referencedAssemblies = BuildManager.GetReferencedAssemblies().Cast<Assembly>().ToArray();

            var ourFiles = Directory.GetFiles(modelsDirectory, "*.cs").ToDictionary(x => x, File.ReadAllText);
            var parseResult = new CodeParser().Parse(ourFiles, referencedAssemblies);
            var builder = new TextBuilder(typeModels, parseResult, Config.ModelsNamespace);

            foreach (var typeModel in builder.GetModelsToGenerate())
            {
                var sb = new StringBuilder();
                builder.Generate(sb, typeModel);
                var filename = Path.Combine(modelsDirectory, typeModel.ClrName + ".generated.cs");
                File.WriteAllText(filename, sb.ToString());
            }

            if (bin != null)
            {
                foreach (var file in Directory.GetFiles(modelsDirectory, "*.generated.cs"))
                    ourFiles[file] = File.ReadAllText(file);
                var compiler = new Compiler();                
                foreach (var asm in referencedAssemblies)
                    compiler.ReferencedAssemblies.Add(asm);
                compiler.Compile(bin, builder.GetModelsNamespace(), ourFiles);
            }
        }

        public static void TouchModelsFile(string appCode)
        {
            var modelsFile = Path.Combine(appCode, "build.models");

            // touch the file & make sure it exists, will recycle the domain
            var text = string.Format("ZpqrtBnk Umbraco ModelsBuilder\r\n"
                + "Actual models code in ~/App_Data/Models\r\n"
                + "Removing this file disables all generated models\r\n"
                + "{0:yyyy-MM-ddTHH:mm:ssZ}", DateTime.UtcNow);
            File.WriteAllText(modelsFile, text);
        }

        private Attempt<HttpResponseMessage> CheckVersion(Version clientVersion, Version minServerVersionSupportingClient)
        {
            if (clientVersion == null)
                return Attempt<HttpResponseMessage>.Fail(Request.CreateResponse(HttpStatusCode.Forbidden, string.Format(
                    "API version conflict: client version (<null>) is not compatible with server version({0}).",
                    Compatibility.Version)));

            // see also: CompatibilityTests
            var isOk = minServerVersionSupportingClient == null
                ? Compatibility.IsCompatible(clientVersion)
                : Compatibility.IsCompatible(clientVersion, minServerVersionSupportingClient);
            var response = isOk ? null : Request.CreateResponse(HttpStatusCode.Forbidden, string.Format(
                "API version conflict: client version ({0}) is not compatible with server version({1}).",
                clientVersion, Compatibility.Version));
            return Attempt<HttpResponseMessage>.SucceedIf(isOk, response);
        }
    }
}
