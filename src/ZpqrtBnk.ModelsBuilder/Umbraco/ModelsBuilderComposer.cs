using Our.ModelsBuilder.Building;
using Our.ModelsBuilder.Options;
using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web.PublishedCache.NuCache;

namespace Our.ModelsBuilder.Umbraco
{
    [ComposeBefore(typeof(NuCacheComposer))]
    [RuntimeLevel(MinLevel = RuntimeLevel.Run)]
    public sealed class ModelsBuilderComposer : ComponentComposer<ModelsBuilderComponent>, ICoreComposer
    {
        public override void Compose(Composition composition)
        {
            base.Compose(composition);

            // compose umbraco & the code factory
            composition.Register<UmbracoServices>(Lifetime.Singleton);
            composition.Register<ICodeFactory, CodeFactory>(Lifetime.Singleton);

            // compose configuration of options
            composition.Configs.Add(() => new OptionsConfiguration());
            composition.ConfigureOptions(OptionsWebConfigReader.ConfigureOptions);
            composition.Register(factory => factory.GetInstance<OptionsConfiguration>().ModelsBuilderOptions, Lifetime.Singleton);

            // always discover model types in code
            // could be used with pure live, to provide some models,
            // and then pure live would not generate them
            composition.WithCollectionBuilder<ModelTypeCollectionBuilder>()
                .Add(composition.TypeLoader.GetTypes<PublishedElementModel>())
                .Add(composition.TypeLoader.GetTypes<PublishedContentModel>());

            // create the appropriate factory, depending on options
            composition.RegisterUnique<IPublishedModelFactory>(factory =>
            {
                var options = factory.GetInstance<Options.ModelsBuilderOptions>();

                if (options.ModelsMode == ModelsMode.PureLive)
                    return factory.CreateInstance<PureLiveModelFactory>();
                
                if (options.EnableFactory)
                    return new PublishedModelFactory(factory.GetInstance<ModelTypeCollection>());

                return new NoopPublishedModelFactory();
            });
        }
    }
}