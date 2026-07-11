using Microsoft.Extensions.Options;
using Our.ModelsBuilder.Options;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Strings;

namespace Our.ModelsBuilder.Building;

/// <summary>
/// Implements the default <see cref="ICodeModelBuilder"/>.
/// </summary>
public class CodeModelBuilder : ICodeModelBuilder
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CodeModelBuilder"/> class.
    /// </summary>
    public CodeModelBuilder(IOptions<ModelsBuilderSettings> settings, IOptions<ModelsBuilderOurOptions> options, IShortStringHelper shortStringHelper)
        : this(settings.Value, options.Value, new ContentTypesCodeModelBuilder(settings.Value, options.Value, shortStringHelper))
    { }

    /// <summary>
    /// Initializes a new instance of the <see cref="CodeModelBuilder"/> class.
    /// </summary>
    protected CodeModelBuilder(ModelsBuilderSettings settings, ModelsBuilderOurOptions options, ContentTypesCodeModelBuilder contentTypes)
    {
        Settings = settings;
        Options = options;
        ContentTypes = contentTypes;
    }

    /// <summary>
    /// Gets the content types code model builder.
    /// </summary>
    protected ContentTypesCodeModelBuilder ContentTypes { get; }

    /// <summary>
    /// Gets the models builder settings.
    /// </summary>
    protected ModelsBuilderSettings Settings { get; }

    /// <summary>
    /// Gets the model build options.
    /// </summary>
    protected ModelsBuilderOurOptions Options { get; }

    /// <inheritdoc />
    public virtual CodeModel Build(CodeModelData data)
    {
        var model = new CodeModel(data, Options.LanguageVersion)
        {
            ModelsNamespace = Settings.ModelsNamespace, 
            FallbackStyle = Options.FallbackStyle,
            PropertyStyle = Options.PropertyStyle,
            //CustomAssemblyName = GetAssemblyName(),
            Using = Using.Defaults // FIXME why?
        };

        // TODO: refactor using, have transform.Use("namespace")

        ContentTypes.Build(model);

        return model;
    }
}