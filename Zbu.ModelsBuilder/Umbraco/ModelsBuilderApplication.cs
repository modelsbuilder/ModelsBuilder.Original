using System;
using Umbraco.Core;
using Umbraco.Core.Models.PublishedContent;
using Zbu.ModelsBuilder.Configuration;

namespace Zbu.ModelsBuilder.Umbraco
{
    public class ModelsBuilderApplication : ApplicationEventHandler
    {
        protected override void ApplicationStarting(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
            // install standard factory if
            // - we want it
            // - we don't also want pure live models

            if (!Config.EnablePublishedContentModelsFactory || Config.EnablePureLiveModels)
                return;

            var types = PluginManager.Current.ResolveTypes<PublishedContentModel>();
            var factory = new PublishedContentModelFactory(types);
            PublishedContentModelFactoryResolver.Current.SetFactory(factory);
        }
    }
}
