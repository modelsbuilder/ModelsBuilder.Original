using System;
using System.Threading;
using System.Web;
using System.Web.Hosting;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Web.Cache;
using Zbu.ModelsBuilder.AspNet;
using Zbu.ModelsBuilder.Configuration;

[assembly: PreApplicationStartMethod(typeof(LiveModelsProviderModule), "Install")]

namespace Zbu.ModelsBuilder.AspNet
{
    public class LiveModelsProvider : ApplicationEventHandler
    {
        private static Mutex _mutex;
        private static int _req;

        protected override void ApplicationStarted(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
            base.ApplicationStarted(umbracoApplication, applicationContext);

            // if no live, or pure live, do nothing
            if (!Config.EnableLiveModels || (!Config.EnableAppCodeModels && !Config.EnableDllModels))
                return;

            // initialize mutex
            // ApplicationId will look like "/LM/W3SVC/1/Root/AppName"
            // name is system-wide and must be less than 260 chars
            var name = HostingEnvironment.ApplicationID + "/ZbuLiveModelsProvider";
            _mutex = new Mutex(false, name);

            // anything changes, and we want to re-generate models.
            ContentTypeCacheRefresher.CacheUpdated += RequestModelsGeneration;
            DataTypeCacheRefresher.CacheUpdated += RequestModelsGeneration;

            // at the end of a request since we're restarting the pool
            // NOTE - this does NOT trigger - see module below
            umbracoApplication.EndRequest += GenerateModelsIfRequested;
        }

        // NOTE
        // Using HttpContext Items fails because CacheUpdated triggers within
        // some asynchronous backend task where we seem to have no HttpContext.

        // So we use a static (non request-bound) var to register that models
        // need to be generated. Could be by another request. Anyway. We could
        // have collisions but... you know the risk.

        private static void RequestModelsGeneration(object sender, EventArgs args)
        {
            //HttpContext.Current.Items[this] = true;
            LogHelper.Debug<LiveModelsProvider>("Request to generate models.");
            Interlocked.Exchange(ref _req, 1);
        }

        public static void GenerateModelsIfRequested(object sender, EventArgs args)
        {
            //if (HttpContext.Current.Items[this] == null) return;
            if (Interlocked.Exchange(ref _req, 0) == 0) return;

            // cannot use a simple lock here because we don't want another AppDomain
            // to generate while we do... and there could be 2 AppDomains if the app restarts.
            // or... am I being paranoid?

            try
            {
                LogHelper.Debug<LiveModelsProvider>("Requested to generate models.");
                _mutex.WaitOne(); // wait until it is safe, and acquire
                LogHelper.Info<LiveModelsProvider>("Generate models.");
                GenerateModels();
            }
            catch (Exception e)
            {
                LogHelper.Error<LiveModelsProvider>("Failed to generate models.", e);
            }
            finally
            {
                _mutex.ReleaseMutex(); // release
            }
        }

        private static void GenerateModels()
        {
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
            ModelsBuilderApiController.GenerateModels(appData, Config.EnableDllModels ? bin : null);

            // will recycle the app domain - but this request will end properly
            if (Config.EnableAppCodeModels)
                ModelsBuilderApiController.TouchModelsFile(appCode);
        }
    }

    // have to do this because it's the only way to subscribe to EndRequest
    // module is installed by assembly attribute at the top of this file
    public class LiveModelsProviderModule : IHttpModule
    {
        public void Init(HttpApplication app)
        {
            app.EndRequest += LiveModelsProvider.GenerateModelsIfRequested;
        }

        public void Dispose()
        {
            // nothing
        }

        public static void Install()
        {
            // if no live, or pure live, do nothing
            if (!Config.EnableLiveModels || (!Config.EnableAppCodeModels && !Config.EnableDllModels))
                return;

            HttpApplication.RegisterModule(typeof(LiveModelsProviderModule));
        }
    }
}
