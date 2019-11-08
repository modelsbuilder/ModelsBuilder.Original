using System;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Web.Hosting;
using Umbraco.Web.Editors;
using Umbraco.Web.WebApi.Filters;
using ZpqrtBnk.ModelsBuilder.Building;
using ZpqrtBnk.ModelsBuilder.Configuration;
using ZpqrtBnk.ModelsBuilder.Umbraco;
using ZpqrtBnk.ModelsBuilder.Web.Plugin;
// use the http one, not mvc, with api controllers!
using HttpGetAttribute = System.Web.Http.HttpGetAttribute;
using HttpPostAttribute = System.Web.Http.HttpPostAttribute;

namespace ZpqrtBnk.ModelsBuilder.Web.Umbraco
{
    // routed as /umbraco/BackOffice/Api/ModelsBuilder/{action}/{id}

    /// <summary>
    /// Represents the ModelsBuilder back office controller.
    /// </summary>
    /// <remarks>
    /// We've created a different controller for the backoffice/angular specifically this is to ensure that the
    /// correct CSRF security is adhered to for angular and it also ensures that this controller is not subseptipal to
    /// global WebApi formatters being changed since this is always forced to only return Angular JSON Specific formats.
    /// </remarks>
    [UmbracoApplicationAuthorize(global::Umbraco.Core.Constants.Applications.Settings)]
    public class ModelsBuilderController : UmbracoAuthorizedJsonController
    {
        private readonly UmbracoServices _umbracoServices;
        private readonly IBuilderFactory _builderFactory;
        private readonly ICodeWriterFactory _writerFactory;
        private readonly Config _config;

        public ModelsBuilderController(UmbracoServices umbracoServices, IBuilderFactory builderFactory, ICodeWriterFactory writerFactory, Config config)
        {
            _umbracoServices = umbracoServices;
            _builderFactory = builderFactory;
            _writerFactory = writerFactory;
            _config = config;
        }

        // invoked by the dashboard
        // requires that the user is logged into the backoffice and has access to the settings section
        // beware! the name of the method appears in modelsbuilder.controller.js
        [HttpPost]
        public HttpResponseMessage BuildModels()
        {
            try
            {
                if (!_config.ModelsMode.SupportsExplicitGeneration())
                {
                    var result2 = new BuildResult { Success = false, Message = "Models generation is not enabled." };
                    return Request.CreateResponse(HttpStatusCode.OK, result2, Configuration.Formatters.JsonFormatter);
                }

                var modelsDirectory = _config.ModelsDirectory;

                var bin = HostingEnvironment.MapPath("~/bin");
                if (bin == null)
                    throw new Exception("Panic: bin is null.");

                // EnableDllModels will recycle the app domain - but this request will end properly
                GenerateModels(modelsDirectory, _config.ModelsMode.IsAnyDll() ? bin : null);

                ModelsGenerationError.Clear();
            }
            catch (Exception e)
            {
                ModelsGenerationError.Report("Failed to build models.", e);
            }

            return Request.CreateResponse(HttpStatusCode.OK, GetDashboardResult(), Configuration.Formatters.JsonFormatter);
        }

        // invoked by the back-office
        // requires that the user is logged into the backoffice and has access to the settings section
        [HttpGet]
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
        // requires that the user is logged into the backoffice and has access to the settings section
        // beware! the name of the method appears in modelsbuilder.controller.js
        [HttpGet]
        public HttpResponseMessage GetDashboard()
        {
            return Request.CreateResponse(HttpStatusCode.OK, GetDashboardResult(), Configuration.Formatters.JsonFormatter);
        }

        private Dashboard GetDashboardResult()
        {
            return new Dashboard
            {
                Enable = _config.Enable,
                Text = DashboardHelper.Text(),
                CanGenerate = DashboardHelper.CanGenerate(),
                GenerateCausesRestart = DashboardHelper.GenerateCausesRestart(),
                OutOfDateModels = DashboardHelper.AreModelsOutOfDate(),
                LastError = DashboardHelper.LastError(),
            };
        }

        private void GenerateModels(string modelsDirectory, string bin)
        {
            var generator = new Generator(_umbracoServices, _builderFactory, _writerFactory, _config);
            generator.GenerateModels(modelsDirectory, bin, _config.ModelsNamespace);
        }

        [DataContract]
        internal class BuildResult
        {
            [DataMember(Name = "success")]
            public bool Success;
            [DataMember(Name = "message")]
            public string Message;
        }

        [DataContract]
        internal class Dashboard
        {
            [DataMember(Name = "enable")]
            public bool Enable;
            [DataMember(Name = "text")]
            public string Text;
            [DataMember(Name = "canGenerate")]
            public bool CanGenerate;
            [DataMember(Name = "generateCausesRestart")]
            public bool GenerateCausesRestart;
            [DataMember(Name = "outOfDateModels")]
            public bool OutOfDateModels;
            [DataMember(Name = "lastError")]
            public string LastError;
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