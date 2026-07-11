using Microsoft.AspNetCore.Authentication;

namespace Our.ModelsBuilder.Web;

internal static class AuthenticationBuilderExtensions
{
    public static AuthenticationBuilder AddApiAuthenticationScheme(this AuthenticationBuilder builder, Action<ApiAuthenticationOptions>? configureOptions)
        => builder.AddApiAuthenticationScheme(ApiAuthentication.AuthenticationScheme, configureOptions);

    private static AuthenticationBuilder AddApiAuthenticationScheme(this AuthenticationBuilder builder, string scheme, Action<ApiAuthenticationOptions>? configureOptions)
        => builder.AddScheme<ApiAuthenticationOptions, ApiAuthentication>(scheme, configureOptions);
}