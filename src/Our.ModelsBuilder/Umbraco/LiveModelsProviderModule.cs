using System.Web;
using Our.ModelsBuilder.Umbraco;

// will install only if configuration says it needs to be installed
[assembly: PreApplicationStartMethod(typeof(LiveModelsProviderModule), "Install")]

namespace Our.ModelsBuilder.Umbraco
{
    // have to do this because it's the only way to subscribe to EndRequest,
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
            // always - don't read config in PreApplicationStartMethod
            HttpApplication.RegisterModule(typeof(LiveModelsProviderModule));
        }
    }
}
