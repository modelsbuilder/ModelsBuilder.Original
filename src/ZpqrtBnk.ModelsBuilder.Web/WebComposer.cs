using System.Web.Http;
using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Web.Editors;
using ZpqrtBnk.ModelsBuilder.Umbraco;
using ZpqrtBnk.ModelsBuilder.Validation;
using ZpqrtBnk.ModelsBuilder.Web.Api;

namespace ZpqrtBnk.ModelsBuilder.Web
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

            // replaces the model validators
            // Core (in WebInitialComposer) registers with:
            //
            // composition.WithCollectionBuilder<EditorValidatorCollectionBuilder>()
            //  .Add(() => composition.TypeLoader.GetTypes<IEditorValidator>());
            //
            // so ours are already in there, but better be safe: clear the collection,
            // and then add exactly those that we want.

            composition.WithCollectionBuilder<EditorValidatorCollectionBuilder>()
                .Clear();

            if (composition.Configs.ModelsBuilder().EnableBackOffice)
            {
                composition.WithCollectionBuilder<EditorValidatorCollectionBuilder>()
                    .Add<ContentTypeModelValidator>()
                    .Add<MediaTypeModelValidator>()
                    .Add<MemberTypeModelValidator>();
            }

            // setup the API if enabled (and in debug mode)

            // the controller is hidden from type finder (to prevent it from being always and
            // automatically registered), which means that Umbraco.Web.Composing.CompositionExtensions
            // Controllers has *not* registered it into the container, and that it is not part of
            // UmbracoApiControllerTypeCollection (and won't get routed etc)

            // so...
            // register it in the container
            // do NOT add it to the collection - we will route it in the component, our way
            // fixme - explain why?

            if (composition.Configs.ModelsBuilder().IsApiServer)
            {
                // add the controller to the list of known controllers
                //composition.WithCollectionBuilder<UmbracoApiControllerTypeCollectionBuilder>()
                //    .Add<ApiController>();

                // register the controller into the container
                composition.Register(typeof(ModelsBuilderApiController), Lifetime.Request);
            }
        }
    }
}
