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
using Umbraco.Core.IO;
using Umbraco.Web.Mvc;
using Umbraco.Core;
using Umbraco.Web.WebApi;
using Zbu.ModelsBuilder.Umbraco;

namespace Zbu.ModelsBuilder.AspNet
{
    [PluginController(ZbuArea)]
    public class ModelsBuilderApiController : UmbracoAuthorizedApiController
    {
        public const string ZbuArea = "Zbu";

        public class BuildResult
        {
            public bool Success;
            public string Message;
        }

        // http://umbraco.local/Umbraco/Zbu/ModelsBuilderApi/BuildModels
        [System.Web.Http.HttpGet] // use the http one, not mvc, with api controllers!
        public HttpResponseMessage BuildModels()
        {
            try
            {
                // fixme - ask Shannon, there has to be a better way to do this
                if (UmbracoUser.Applications.All(x => x.alias != "developer"))
                    throw new Exception("Panic: user has no access to the required application.");

                var appData = HostingEnvironment.MapPath("~/App_Data");
                if (appData == null)
                    throw new Exception("Panic: appData is null.");

                var appCode = HostingEnvironment.MapPath("~/App_Code");
                if (appCode == null)
                    throw new Exception("Panic: appCode is null.");

                GenerateModels(appData);
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

        private static void GenerateModels(string appData)
        {
            var modelsDirectory = Path.Combine(appData, "Models");
            if (!Directory.Exists(modelsDirectory))
                Directory.CreateDirectory(modelsDirectory);

            foreach (var file in Directory.GetFiles(modelsDirectory, "*.generated.cs"))
                File.Delete(file);

            var umbraco = Application.GetApplication();
            var modelTypes = umbraco.GetContentAndMediaTypes();

            var builder = new TextBuilder();
            builder.Namespace = "Umbraco.Web.PublishedContentModels"; // note - could be a config option
            builder.Prepare(modelTypes);

            foreach (var file in Directory.GetFiles(modelsDirectory, "*.cs"))
                builder.Parse(File.ReadAllText(file), modelTypes);

            foreach (var modelType in modelTypes)
            {
                var sb = new StringBuilder();
                builder.Generate(sb, modelType);
                var filename = Path.Combine(modelsDirectory, modelType.Name + ".generated.cs");
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

        public static string GetBuildModelsUrl(HttpContext context)
        {
            // fixme - ask Shannon, there has to be a better way to do this
            // ok, I just want the route to my controller, statically, so I can have it in the dashboard
            // which is not part of the MVC world... been through MSDN & StackOverflow & such for too long
            // without finding the solution - so it's hard-coded here in all its dirtyness.
            return "/Umbraco/Zbu/ModelsBuilderApi/BuildModels";
        }
    }
}
