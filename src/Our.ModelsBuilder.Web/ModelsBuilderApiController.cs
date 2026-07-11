using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Our.ModelsBuilder.Api;
using Our.ModelsBuilder.Building;
using Our.ModelsBuilder.Options;
using Semver;
using Umbraco.Cms.Core;
using Umbraco.Cms.Web.Common.Controllers;
using Umbraco.Cms.Web.Common.Filters;

namespace Our.ModelsBuilder.Web;

[DisableBrowserCache]
public class ModelsBuilderApiController : UmbracoApiController
{
    private readonly ICodeFactory _codeFactory;
    private readonly ModelsBuilderOptions _options;

    public ModelsBuilderApiController(ICodeFactory codeFactory, ModelsBuilderOptions options)
    {
        _codeFactory = codeFactory;
        _options = options;
    }

    [HttpGet]
    [Authorize(AuthenticationSchemes = ApiAuthentication.AuthenticationScheme)]
    public IActionResult GetApiVersion()
    {
        if (!ModelState.IsValid)
            return new BadRequestResult();

        return new JsonResult(ApiVersion.Current);
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = ApiAuthentication.AuthenticationScheme)]
    public IActionResult ValidateClientVersion(ValidateClientVersionData? data)
    {
        if (!ModelState.IsValid || data is not { IsValid: true })
            return new BadRequestResult();

        var checkResult = CheckVersion(data.ClientVersion, data.MinServerVersionSupportingClient);
        return checkResult.Success
            ? new ContentResult { Content = "OK" }
            : checkResult.Result!;
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = ApiAuthentication.AuthenticationScheme)]
    public IActionResult GetModels(GetModelsData? data)
    {
        if (!ModelState.IsValid || data is not { IsValid: true })
            return new BadRequestResult();

        var checkResult = CheckVersion(data.ClientVersion, data.MinServerVersionSupportingClient);
        if (!checkResult.Success)
            return checkResult.Result!;

        var generator = new Generator(_codeFactory, _options);
        var models = generator.GetModels(data.Namespace, data.Files);

        return new JsonResult(models);
    }

    private static Attempt<IActionResult> CheckVersion(SemVersion? clientVersion, SemVersion? minServerVersionSupportingClient)
    {
        if (clientVersion == null)
            return Attempt<IActionResult>.Fail(new ContentResult { Content = $"API version conflict: client version (<null>) is not compatible with server version({ApiVersion.Current.Version})." });

        // minServerVersionSupportingClient can be null
        var isOk = ApiVersion.Current.IsCompatibleWith(clientVersion, minServerVersionSupportingClient);
        var response = isOk ? null : new ContentResult { Content = $"API version conflict: client version ({clientVersion}) is not compatible with server version({ApiVersion.Current.Version})." };

        return Attempt<IActionResult>.If(isOk, response);
    }
}