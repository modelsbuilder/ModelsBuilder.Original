using Microsoft.AspNetCore.Authentication;

namespace Our.ModelsBuilder.Web;

public class ApiAuthenticationOptions : AuthenticationSchemeOptions
{
    public string? SecretKey { get; set; }
}