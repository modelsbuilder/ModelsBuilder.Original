using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Our.ModelsBuilder.Building;
using Our.ModelsBuilder.Options;
using Our.ModelsBuilder.Umbraco;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Infrastructure.ModelsBuilder.Building;

namespace Our.ModelsBuilder;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOurModelsBuilder(this IServiceCollection services,
        IConfiguration configuration,
        Action<ModelsBuilderSettings>? configureCoreOptions = null,
        Action<ModelsBuilderOurOptions>? configureOurOptions = null)
    {
        // configure Core's options - which are named 'settings', go figure
        // they work like plain options, and Core will add them as
        // builder.AddUmbracoOptions<ModelsBuilderSettings>()
        // which uses the UmbracoOption attribute for proper binding
        // see src/Umbraco.Core/DependencyInjection/UmbracoBuilder.Configuration.cs
        if (configureCoreOptions != null) services.Configure(configureCoreOptions);

        // register and configure our options
        // 'our options' are a Our thing that we use to complement settings
        // we *could* consider 'extending' Core's but... later
        services.AddOptions<ModelsBuilderOurOptions>()
            .Bind(configuration.GetSection("Our:ModelsBuilder")) //, o => o.BindNonPublicProperties = true)
            .ValidateDataAnnotations();
        if (configureOurOptions != null) services.Configure(configureOurOptions);

        // register our generator
        services.AddSingleton<IModelsGenerator, OurModelsGenerator>();

        // register our stuff - singletons?
        services.AddSingleton<ICodeModelDataSource, CodeModelDataSource>();
        services.AddSingleton<ICodeVisitor, CodeVisitor>();
        services.AddSingleton<ICodeModelBuilder, CodeModelBuilder>();
        services.AddSingleton<ICodeWriter, CodeWriter>();

        // register our helper
        services.AddSingleton<UmbracoServices>();

        return services;
    }
}