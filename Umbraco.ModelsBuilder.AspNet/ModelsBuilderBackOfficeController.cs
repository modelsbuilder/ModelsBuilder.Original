using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Web.Compilation;
using System.Web.Hosting;
using Umbraco.Core.Configuration;
using Umbraco.ModelsBuilder.AspNet.Dashboard;
using Umbraco.ModelsBuilder.Building;
using Umbraco.ModelsBuilder.Configuration;
using Umbraco.ModelsBuilder.Umbraco;
using Umbraco.Web.Editors;

namespace Umbraco.ModelsBuilder.AspNet
{
    /// <summary>
    /// API controller for use in the Umbraco back office with Angular resources
    /// </summary>
    /// <remarks>
    /// We've created a different controller for the backoffice/angular specifically this is to ensure that the
    /// correct CSRF security is adhered to for angular and it also ensures that this controller is not subseptipal to 
    /// global WebApi formatters being changed since this is always forced to only return Angular JSON Specific formats.
    /// </remarks>
    public class ModelsBuilderBackOfficeController : UmbracoAuthorizedJsonController
    {
        // invoked by the dashboard
        // requires that the user is logged into the backoffice and has access to the developer section
        // beware! the name of the method appears in modelsbuilder.controller.js
        [System.Web.Http.HttpPost] // use the http one, not mvc, with api controllers!
        public HttpResponseMessage BuildModels()
        {
            try
            {
                if (!UmbracoConfig.For.ModelsBuilder().ModelsMode.SupportsExplicitGeneration())
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
                if (bin == null)
                    throw new Exception("Panic: bin is null.");

                // EnableDllModels will recycle the app domain - but this request will end properly
                GenerateModels(appData, UmbracoConfig.For.ModelsBuilder().ModelsMode.IsAnyDll() ? bin : null);

                // will recycle the app domain - but this request will end properly
                if (UmbracoConfig.For.ModelsBuilder().ModelsMode.IsAnyAppCode())
                    TouchModelsFile(appCode);

                var result = new BuildResult { Success = true };
                return Request.CreateResponse(HttpStatusCode.OK, result, Configuration.Formatters.JsonFormatter);

            }
            catch (Exception e)
            {
                var message = string.Format("{0}: {1}\r\n{2}", e.GetType().FullName, e.Message, e.StackTrace);
                var result = new BuildResult { Success = false, Message = message };
                return Request.CreateResponse(HttpStatusCode.OK, result, Configuration.Formatters.JsonFormatter);
            }
        }

        // invoked by the back-office
        // requires that the user is logged into the backoffice and has access to the developer section
        [System.Web.Http.HttpGet] // use the http one, not mvc, with api controllers!
        public HttpResponseMessage GetModelsOutOfDateStatus()
        {
            var status = OutOfDateModelsStatus.IsEnabled
                ? (OutOfDateModelsStatus.IsOutOfDate
                    ? new OutOfDateStatus { Status = OutOfDateType.OutOfDate }
                    : new OutOfDateStatus { Status = OutOfDateType.Current })
                : new OutOfDateStatus { Status = OutOfDateType.Unknown };

            return Request.CreateResponse(HttpStatusCode.OK, status, Configuration.Formatters.JsonFormatter);
        }

        // invoked by the back-office
        // requires that the user is logged into the backoffice and has access to the developer section
        // beware! the name of the method appears in modelsbuilder.controller.js
        [System.Web.Http.HttpGet] // use the http one, not mvc, with api controllers!        
        public HttpResponseMessage GetDashboard()
        {
            var dashboard = new
            {
                enable = UmbracoConfig.For.ModelsBuilder().Enable,
                text = DashboardHelper.Text(),
                canGenerate = DashboardHelper.CanGenerate(),
                generateCausesRestart = DashboardHelper.GenerateCausesRestart(),
                outOfDateModels = DashboardHelper.AreModelsOutOfDate(),
            };
            return Request.CreateResponse(HttpStatusCode.OK, dashboard, Configuration.Formatters.JsonFormatter);
        }

        internal static void TouchModelsFile(string appCode)
        {
            var modelsFile = Path.Combine(appCode, "build.models");

            // touch the file & make sure it exists, will recycle the domain
            var text = string.Format("Umbraco ModelsBuilder\r\n"
                                     + "Actual models code in ~/App_Data/Models\r\n"
                                     + "Removing this file disables all generated models\r\n"
                                     + "{0:yyyy-MM-ddTHH:mm:ssZ}", DateTime.UtcNow);
            File.WriteAllText(modelsFile, text);
        }

        internal static void GenerateModels(string appData, string bin)
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
            var builder = new TextBuilder(typeModels, parseResult, UmbracoConfig.For.ModelsBuilder().ModelsNamespace);

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

            OutOfDateModelsStatus.Clear();
        }

        [DataContract]
        internal class BuildResult
        {
            [DataMember(Name = "success")]
            public bool Success;
            [DataMember(Name = "message")]
            public string Message;
        }

        internal enum OutOfDateType
        {
            OutOfDate,
            Current,
            Unknown = 100
        }

        [DataContract]
        internal class OutOfDateStatus
        {
            [DataMember(Name = "status")]
            public OutOfDateType Status { get; set; }
        }
    }
}