using Umbraco.Core;
using Umbraco.Core.Models.PublishedContent;
using Zbu.ModelsBuilder.Configuration;

namespace Zbu.ModelsBuilder.Umbraco
{
    public class ConfigurePublishedContentModelFactory : ApplicationEventHandler
    {
        protected override void ApplicationStarting(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
            if (!Config.EnablePublishedContentModelsFactory)
                return;

            var types = PluginManager.Current.ResolveTypes<PublishedContentModel>();
            var factory = new PublishedContentModelFactory(types);
            PublishedContentModelFactoryResolver.Current.SetFactory(factory);
        }
    }
}
