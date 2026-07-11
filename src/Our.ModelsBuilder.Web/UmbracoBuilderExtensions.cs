using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.DependencyInjection;

namespace Our.ModelsBuilder.Web;

public static class UmbracoBuilderExtensions
{
    public static IUmbracoBuilder AddOurModelsBuilderApi(IUmbracoBuilder builder, Action<ApiAuthenticationOptions>? configureAuthenticationOptions = null)
    {
        // is marked [HideFromTypeFinder] so it is *not* automatically discovered
        // and registered and routed and authorized the Umbraco way, so we need to
        // add it explicitly here

        builder.Services
            .AddTransient<ModelsBuilderApiController>();

        // and we need to add its own secret-key based authentication scheme

        builder.Services
            .AddAuthentication(ApiAuthentication.AuthenticationScheme)
            .AddApiAuthenticationScheme(configureAuthenticationOptions);

        return builder;
    }
}