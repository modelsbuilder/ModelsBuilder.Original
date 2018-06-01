using LightInject;
using Umbraco.Core;
using Umbraco.Core.Components;
using Umbraco.Core.Configuration;
using Umbraco.ModelsBuilder.Configuration;
using Umbraco.ModelsBuilder.Umbraco;

namespace Umbraco.ModelsBuilder.Api
{
    [RequireComponent(typeof(ModelsBuilderComponent))]
    [RuntimeLevel(MinLevel = RuntimeLevel.Run)]
    public class ModelsBuilderApiComponent : UmbracoComponentBase, IUmbracoUserComponent
    {
        public override void Compose(Composition composition)
        {
            var config = UmbracoConfig.For.ModelsBuilder();

            // setup the API if enabled (and in debug mode)
            if (config.ApiServer)
                composition.Container.Register(typeof(ModelsBuilderApiController), new PerRequestLifeTime());
        }
    }
}
