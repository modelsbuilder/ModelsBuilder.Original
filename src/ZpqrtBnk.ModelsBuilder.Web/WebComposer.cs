using System;
using System.Reflection;
using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Web.Editors;
using ZpqrtBnk.ModelsBuilder.Umbraco;
using ZpqrtBnk.ModelsBuilder.Validation;
using ZpqrtBnk.ModelsBuilder.Web.Api;

namespace ZpqrtBnk.ModelsBuilder.Web
{
    [Disable]
    public class NoopComposer : IComposer {
        public void Compose(Composition composition)
        { }
    }

    public class DisableUmbracoModelsBuilderAttribute : DisableAttribute
    {
        public DisableUmbracoModelsBuilderAttribute()
        {
            var field = typeof(DisableAttribute).GetField("<DisabledType>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null) throw new Exception("panic: cannot get DisableAttribute.DisableType backing field.");

            var type = Type.GetType("Umbraco.ModelsBuilder.Umbraco.ModelsBuilderComposer,Umbraco.ModelsBuilder", false);
            field.SetValue(this, type ?? typeof(NoopComposer));
        }
    }

    // do not reference Umbraco.ModelsBuilder - the type will be replaced at runtime
    // (see DisableUmbracoModelsBuilderAttribute class above)
    //[Disable(typeof(global::Umbraco.ModelsBuilder.Umbraco.ModelsBuilderComposer))]
    [DisableUmbracoModelsBuilder]

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
