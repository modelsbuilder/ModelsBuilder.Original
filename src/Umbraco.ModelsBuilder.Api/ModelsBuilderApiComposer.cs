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
            // we *must* register the controller 'cos it is hidden from type finder
            // (to prevent it from being always and automatically registered)
            if (composition.Configs.ModelsBuilder().ApiServer)
            {
                //composition.WithCollectionBuilder<UmbracoApiControllerTypeCollectionBuilder>().Append<ModelsBuilderApiController>();

                // fixme
                // so far, the collection is not a real collection - which is sad
                var umbracoApiControllerTypes = new UmbracoApiControllerTypeCollection(
                    composition.TypeLoader.GetTypes<UmbracoApiController>().And(typeof(ModelsBuilderApiController)));
                composition.RegisterUnique(umbracoApiControllerTypes);

                composition.Register(typeof(ModelsBuilderApiController), Lifetime.Request);
            }
        }
    }
}
