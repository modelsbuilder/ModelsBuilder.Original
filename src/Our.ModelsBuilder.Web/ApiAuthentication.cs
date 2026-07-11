using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Our.ModelsBuilder.Web;

// see: https://dev.to/d_inventor/how-to-secure-your-umbraco-api-controller-with-an-api-key-3ia2
// we only implement the authentication part and assume that anyone who authenticates is authorized

internal class ApiAuthentication : AuthenticationHandler<ApiAuthenticationOptions>
{
    public const string AuthenticationScheme = "Our.ModelsBuilder.ApiKey";
    public const string HeaderName = "ourModelsBuilderApiKey";

    public ApiAuthentication(
        IOptionsMonitor<ApiAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock)
        : base(options, logger, encoder, clock)
    { }

    // All our code will be synchronous, so let's leave all async stuff separate.
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        => Task.FromResult(HandleAuthenticate());

    private AuthenticateResult HandleAuthenticate()
    {
        // We have to make sure that an api key is defined.
        // If the api key is not defined, the endpoint is potentially vulnerable, so this handler should stop early to prevent accidental exposure
        var apiKey = Options.SecretKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Logger.LogError("The configured api key is empty or null. APIs with api key are closed off for security");
            return AuthenticateResult.NoResult();
        }

        // If the incoming request does not have the api key header, then it can't be authenticated with this handler, so the handler stops early
        if (!Request.Headers.ContainsKey(HeaderName))
        {
            return AuthenticateResult.NoResult();
        }

        // If the incoming request does contain the api key header, then it's certain that they try to authenticate with this handler.
        // If the incoming api key does not match configuration, the authentication should actively fail.
        if (!Options.SecretKey.Equals(Request.Headers[HeaderName]))
        {
            return AuthenticateResult.Fail("Invalid Key provided");
        }

        // If all the previous checks pass, then we know that the requester is legit.
        // Notice that these claims can pretty much be anything you want. The Claim of type 'ClaimTypes.Name' is required to have, but the rest is completely up to you.
        // In this example, I use the remote IP address as part of the identity. This will come in use later in the authorization step
        var remoteIpAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, remoteIpAddress),
            new Claim("origin", remoteIpAddress)
        };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return AuthenticateResult.Success(ticket);
    }
}