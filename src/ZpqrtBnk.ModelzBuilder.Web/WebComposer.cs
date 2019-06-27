using Umbraco.Core;
using Umbraco.Core.Composing;
using ZpqrtBnk.ModelzBuilder.Umbraco;
using System.Web.Http;

namespace ZpqrtBnk.ModelzBuilder.Web
{
    [Disable(typeof(global::Umbraco.ModelsBuilder.Umbraco.ModelsBuilderComposer))]
    [ComposeAfter(typeof(ModelsBuilderComposer))]
    [RuntimeLevel(MinLevel = RuntimeLevel.Run)]
    public class WebComposer : ComponentComposer<WebComponent>, IUserComposer
    {
        public override void Compose(Composition composition)
        {
            base.Compose(composition);

            // kill Umbraco.ModelsBuilder package manifest entirely, replaced with ours
            // always, as soon as we are installed, regardless of what is enabled or not
            composition.ManifestFilters().Append<WebManifestFilter>();

            // setup the API if enabled (and in debug mode)

            // the controller is hidden from type finder (to prevent it from being always and
            // automatically registered), which means that Umbraco.Web.Composing.CompositionExtensions
            // Controllers has *not* registered it into the container, and that it is not part of
            // UmbracoApiControllerTypeCollection (and won't get routed etc)

            // so...
            // register it in the container
            // do NOT add it to the collection - we will route it in the component, our way

            if (composition.Configs.ModelsBuilder().IsApiServer)
            {
                // add the controller to the list of known controllers
                //composition.WithCollectionBuilder<UmbracoApiControllerTypeCollectionBuilder>()
                //    .Add<ApiController>();

                // register the controller into the container
                composition.Register(typeof(ApiController), Lifetime.Request);
            }
        }
    }
}
