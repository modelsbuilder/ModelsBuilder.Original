using System.Linq;
using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.ModelsBuilder.Umbraco;
using Umbraco.Web.WebApi;

namespace Umbraco.ModelsBuilder.Api
{
    [ComposeAfter(typeof(ModelsBuilderComposer))]
    [RuntimeLevel(MinLevel = RuntimeLevel.Run)]
    public class ModelsBuilderApiComposer : IUserComposer
    {
        public void Compose(Composition composition)
        {
            // setup the API if enabled (and in debug mode)

            // the controller is hidden from type finder (to prevent it from being always and
            // automatically registered), which means that Umbraco.Web.Composing.CompositionExtensions
            // Controllers has *not* registered it into the container, and that it is not part of
            // UmbracoApiControllerTypeCollection (and won't get routed etc)

            // so...
            // add it to the collection + register it in the container

            if (composition.Configs.ModelsBuilder().ApiServer)
            {
                // add the controller to the list of known controllers
                composition.WithCollectionBuilder<UmbracoApiControllerTypeCollectionBuilder>()
                    .Add<ModelsBuilderApiController>();

                // register the controller into the container
                composition.Register(typeof(ModelsBuilderApiController), Lifetime.Request);
            }
        }
    }
}
