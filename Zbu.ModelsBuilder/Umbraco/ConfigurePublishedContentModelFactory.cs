using System;
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

            // NOTE
            // this is done once when the app starts, which means that "pure" live models
            // are not being detected (not compiled yet) and cannot be updated once they
            // have been re-generated. We cannot compile then right now, because we don't
            // have access to the DB etc yet. We cannot register them later on to the
            // factory (provided that we modify the Umbraco factory to support it) because
            // then... the factory will switch to one version of type "Foo" to another version
            // of the same time right in the middle of a request... bad. And the factory is
            // bound to the cache, ie it's global -- not one per request, otherwise we
            // lose the perfs benefit.
            //
            // conclusion... RIP pure live models

            var types = PluginManager.Current.ResolveTypes<PublishedContentModel>();
            var factory = new PublishedContentModelFactory(types);
            PublishedContentModelFactoryResolver.Current.SetFactory(factory);
        }
    }
}
