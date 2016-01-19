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
            // install standard factory if
            // - we want it
            // - we don't also want pure live models

            var config = UmbracoConfig.For.ModelsBuilder();
            if (!config.EnableFactory || config.EnablePureLiveModels)
                return;

            var types = PluginManager.Current.ResolveTypes<PublishedContentModel>();
            var factory = new PublishedContentModelFactory(types);
            PublishedContentModelFactoryResolver.Current.SetFactory(factory);
        }
    }
}
