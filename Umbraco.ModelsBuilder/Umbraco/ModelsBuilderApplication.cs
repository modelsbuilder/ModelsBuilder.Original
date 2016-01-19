using System;
using Umbraco.Core;
using Umbraco.Core.Configuration;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.ModelsBuilder.Configuration;

namespace Umbraco.ModelsBuilder.Umbraco
{
    public class ModelsBuilderApplication : ApplicationEventHandler
    {
        protected override void ApplicationStarting(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
            var config = UmbracoConfig.For.ModelsBuilder();

            // if
            if (!config.EnableFactory // we don't want it
                || config.ModelsMode == ModelsMode.PureLive) // or PureLive takes care of it
                return; // don't do it

            // else install the standard factory
            var types = PluginManager.Current.ResolveTypes<PublishedContentModel>();
            var factory = new PublishedContentModelFactory(types);
            PublishedContentModelFactoryResolver.Current.SetFactory(factory);
        }
    }
}
