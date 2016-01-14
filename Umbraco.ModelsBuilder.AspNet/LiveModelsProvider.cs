using System;
using System.Threading;
using System.Web;
using System.Web.Hosting;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Web.Cache;
using Umbraco.ModelsBuilder.AspNet;
using Umbraco.ModelsBuilder.Configuration;

// note
// we do not support pure LiveModels anymore - see notes in various places
// this should provide live AppData, AppCode or Dll models - but we do not want to
// restart the app anytime something changes, ?
//
// ideally we'd want something that does not generate models all the time, but
// only after a while, and then restarts, but that is prone to too much confusion,
// so we decide that ONLY live App_Data models are supported from now on.

// will install only if configuration says it needs to be installed
[assembly: PreApplicationStartMethod(typeof(LiveModelsProviderModule), "Install")]

namespace Umbraco.ModelsBuilder.AspNet
{
    public class LiveModelsProvider : ApplicationEventHandler
    {
        private static Mutex _mutex;
        private static int _req;

        internal static bool IsEnabled
        {
            get
            {
                if (!Config.EnableLiveModels)
                    return false;

                // not supported anymore
                //if (Config.EnableAppCodeModels)
                //    return true;

                if (Config.EnableAppDataModels || Config.EnableDllModels)
                    return true;

                // we do not manage pure live here
                return false;
            }
        }

        protected override void ApplicationStarted(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
            base.ApplicationStarted(umbracoApplication, applicationContext);

            if (!IsEnabled)
                return;

            // initialize mutex
            // ApplicationId will look like "/LM/W3SVC/1/Root/AppName"
            // name is system-wide and must be less than 260 chars
            var name = HostingEnvironment.ApplicationID + "/UmbracoLiveModelsProvider";
            _mutex = new Mutex(false, name);

            // anything changes, and we want to re-generate models.
            ContentTypeCacheRefresher.CacheUpdated += RequestModelsGeneration;
            DataTypeCacheRefresher.CacheUpdated += RequestModelsGeneration;

            // at the end of a request since we're restarting the pool
            // NOTE - this does NOT trigger - see module below
            //umbracoApplication.EndRequest += GenerateModelsIfRequested;
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
            LogHelper.Debug<LiveModelsProvider>("Requested to generate models.");
            Interlocked.Exchange(ref _req, 1);
        }

        public static void GenerateModelsIfRequested(object sender, EventArgs args)
        {
            //if (HttpContext.Current.Items[this] == null) return;
            if (Interlocked.Exchange(ref _req, 0) == 0) return;

            // cannot use a simple lock here because we don't want another AppDomain
            // to generate while we do... and there could be 2 AppDomains if the app restarts.

            try
            {
                LogHelper.Debug<LiveModelsProvider>("Generate models...");
                const int timeout = 2*60*1000; // 2 mins
                _mutex.WaitOne(timeout); // wait until it is safe, and acquire
                LogHelper.Info<LiveModelsProvider>("Generate models now.");
                GenerateModels();
                LogHelper.Info<LiveModelsProvider>("Generated.");
            }
            catch (TimeoutException)
            {
                LogHelper.Warn<LiveModelsProvider>("Timeout, models were NOT generated.");
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
            ModelsBuilderController.GenerateModels(appData, Config.EnableDllModels ? bin : null);

            // will recycle the app domain - but this request will end properly
            if (Config.EnableAppCodeModels)
                ModelsBuilderController.TouchModelsFile(appCode);
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
            if (!LiveModelsProvider.IsEnabled)
                return;

            HttpApplication.RegisterModule(typeof(LiveModelsProviderModule));
        }
    }
}
