using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;
using Our.ModelsBuilder.Building;
using Our.ModelsBuilder.Options;
using Umbraco.Cms.Core.Configuration;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Hosting;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Cms.Infrastructure.ModelsBuilder;
using Umbraco.Cms.Infrastructure.ModelsBuilder.Building;
using Umbraco.Cms.Infrastructure.Serialization;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Extensions;
using File = System.IO.File;
using Microsoft.Extensions.Hosting;
using Umbraco.Cms.Core.Extensions;
using Microsoft.AspNetCore.Html;

namespace Our.ModelsBuilder.Tests;


[TestFixture]
public class Tests
{
    [Test]
    public void Test()
    {
        var configuration = new ConfigurationRoot(new List<IConfigurationProvider>());

        var services = new ServiceCollection();

        // add our ModelsBuilder
        services.AddOurModelsBuilder(configuration,

            configureCoreOptions: settings =>
            {
                settings.ModelsMode = "Nothing";
                //settings.ModelsDirectory = "~/models";
                settings.AcceptUnsafeModelsDirectory = false;
                settings.FlagOutOfDateModels = false;
                settings.ModelsNamespace = "My.Namespace";
                settings.DebugLevel = 0;
            },

            configureOurOptions: options => options
                .SetLanguageVersion(LanguageVersion.Latest) // yolo
                .SetClrNameSource(ClrNameSource.Alias)
                .SetFallbackStyle(FallbackStyle.Nothing)
                .SetPropertyStyle(PropertyStyle.Property)
                .IgnoreContentType("contentTypeAlias")
                .SetContentTypeClrName("thingAlias", "Yooki")
            );

        // create a self-collected temp dir for the test
        using var tempDir = new TempDir();

        // copy Core configuration
        var shortStringHelperConfig = new DefaultShortStringHelperConfig()
            .WithConfig(CleanStringType.ConvertCase, new DefaultShortStringHelperConfig.Config
            {
                PreFilter = null,
                IsTerm = (c, leading) => char.IsLetterOrDigit(c) || c == '_', // letter, digit or underscore
                StringType = CleanStringType.Ascii,
                BreakTermsOnUpper = true,
            });
        var shortStringHelper = new DefaultShortStringHelper(shortStringHelperConfig);

        // configure host environment
        // all we need is ContentRootPath, MapPathContentRoot is an Umbraco extension on top of it
        var hostEnvironment = Substitute.For<IHostEnvironment>();
        hostEnvironment.ContentRootPath.Returns(tempDir.Path);

        // create content types, data types, etc
        var contentType1 = new ContentType(shortStringHelper, -1)
        {
            Id = 1,
            Name = "thingName",
            Alias = "thingAlias",
            Description = "thingDescription",
            IsElement = false,
            // FIXME gone?
            //IsContainer = false,
            Variations = ContentVariation.Nothing,
        };

        contentType1.AddPropertyType(new PropertyType(shortStringHelper,
            global::Umbraco.Cms.Core.Constants.PropertyEditors.Aliases.TextBox, 
            ValueStorageType.Nvarchar, 
            "propAlias")
        {
            Id = 1,
            Name = "propName",
            Description = "propDescription",
            DataTypeId = 1,
        });

        contentType1.AddPropertyType(new PropertyType(shortStringHelper,
            global::Umbraco.Cms.Core.Constants.PropertyEditors.Aliases.TextBox,
            ValueStorageType.Nvarchar,
            "otherAlias")
        {
            Id = 2,
            Name = "otherName",
            Description = "otherDescription",
            DataTypeId = 1,
        });

        var contentTypeService = Substitute.For<IContentTypeService>();
        contentTypeService
            .GetAll()
            .Returns([
                contentType1
            ]);

        var configurationEditor = Substitute.For<IConfigurationEditor>();
        configurationEditor
            .DefaultConfiguration
            .Returns(new Dictionary<string, object>());
        // FIXME gone?
        //configurationEditor
        //    .IsConfiguration(Arg.Any<object>())
        //    .Returns(true);

        var editor = Substitute.For<IDataEditor>();
        editor.GetConfigurationEditor().Returns(configurationEditor);
        editor.Alias.Returns(global::Umbraco.Cms.Core.Constants.PropertyEditors.Aliases.TextBox);

        var encoderFactory = new DefaultJsonSerializerEncoderFactory();
        var serializer = new SystemTextConfigurationEditorJsonSerializer(encoderFactory);
        var dataType =  new DataType(editor, serializer)
        {
            Id = 1,
        };

        var dataTypeById = new Dictionary<int, IDataType>
        {
            [dataType.Id] = dataType
        };
        var dataTypeByEditorAlias = new Dictionary<string, IDataType>();
        dataTypeByEditorAlias[dataType.EditorAlias] = dataType;

        var dataTypeService = Substitute.For<IDataTypeService>();
        dataTypeService
            .GetByEditorAliasAsync(Arg.Any<string>())
            .Returns(call => dataTypeByEditorAlias.TryGetValue(call.Arg<string>(), out var x) ? new[] { x } : null);
        dataTypeService
            .GetAll()
            .Returns(dataTypeById.Values);
        dataTypeService
            .GetAllAsync()
            .Returns(dataTypeById.Values);

        // wire services
        services.AddSingleton(Substitute.For<IMediaTypeService>());
        services.AddSingleton(Substitute.For<IMemberTypeService>());
        services.AddOptions<ModelsBuilderSettings>();
        services.AddSingleton<OutOfDateModelsStatus>();
        services.AddSingleton(hostEnvironment);
        services.AddSingleton(contentTypeService);
        services.AddSingleton(dataTypeService);
        services.AddSingleton<IShortStringHelper>(shortStringHelper);

        // that would require a TypeLoader etc. so we do it differently
        //services.AddSingleton(factory => factory.CreateDefaultPublishedModelFactory());
        services.AddSingleton<IPublishedModelFactory>(factory =>
        {
            // will do for now, later we'll test with the real thing
            var publishedValueFallback = new NoopPublishedValueFallback();
            var types = new Type[] // all types PublishedElementModel/PublishedContentModel
            {
                // say we have none for now
            };
            return new PublishedModelFactory(types, publishedValueFallback);
        });

        services.AddSingleton<IPublishedContentTypeFactory, PublishedContentTypeFactory>();
        services.AddSingleton(new PropertyValueConverterCollection(() =>
        [
            new TextStringValueConverter(default, default)
        ]));

        // add a custom file
        var generatorDirectory = hostEnvironment.MapPathContentRoot("~/models");
        Directory.CreateDirectory(generatorDirectory);
        File.WriteAllText(Path.Combine(generatorDirectory, "Yooki.cs"), @"
using Microsoft.AspNetCore.Html;
using Umbraco.Cms.Infrastructure.ModelsBuilder;

namespace My.Namespace;

public partial class Yooki
{
    [ImplementPropertyType(""propAlias"")] 
    public HtmlString PropAliasImpl => default; //new HtmlString(this.Value<string>(""html""));
}
");

        // build services
        var serviceProvider = services.BuildServiceProvider();

        // generate models
        var generator = serviceProvider.GetRequiredService<IModelsGenerator>();
        generator.GenerateModels();

        // assert that files have been created
        Directory.CreateDirectory("../../../Models"); // temp. copy them to test project?
        var sources = new Dictionary<string, string>();
        foreach (var file in Directory.GetFiles(hostEnvironment.MapPathContentRoot("models")))
        {
            var text = File.ReadAllText(file);
            sources[Path.GetFileNameWithoutExtension(file)] = text;
            File.WriteAllText(Path.Combine("../../../Models", Path.GetFileName(file)), text);
        }

        var excludes = new HashSet<string>
        {
            "Our.ModelsBuilder.Tests.dll",
            "System.Drawing.Common.dll" // meh - should not be there anymore?
        };

        // all *.dll files in the ~/bin directory
        var files = Directory
            .EnumerateFiles(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "*.dll")
            .Where(x => !excludes.Contains(Path.GetFileName(x)));

        // the path to .NET assemblies
        var netAssemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        var aspAssemblyPath = Path.GetDirectoryName(typeof(HtmlString).Assembly.Location)!;

        // C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.9
        Console.WriteLine("DEBUG: netAssemblyPath=" + netAssemblyPath);
        // C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.9\System.Linq.Expressions.dll
        Console.WriteLine("DEBUG: Expression in " + typeof(System.Linq.Expressions.Expression).Assembly.Location);
        // C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.9\System.Private.CoreLib.dll
        Console.WriteLine("DEBUG: GeneratedCodeAttribute in " + typeof(System.CodeDom.Compiler.GeneratedCodeAttribute).Assembly.Location);
        // C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App\10.0.9
        Console.WriteLine("DEBUG: aspAssemblyPath=" + aspAssemblyPath);
        // C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App\10.0.9\Microsoft.AspNetCore.Html.Abstractions.dll
        Console.WriteLine("DEBUG: HtmlString in " + typeof(HtmlString).Assembly.Location);

        // references all *.dll files in the ~/bin directory
        var references = files.Select(x => MetadataReference.CreateFromFile(x));

        // FIXME this 'selection of assemblies' does not look very robust?

        // plus a selection of .NET assemblies
        references = references
            .Concat(
            [
                //MetadataReference.CreateFromFile(Path.Combine(netAssemblyPath, "mscorlib.dll")),
                MetadataReference.CreateFromFile(Path.Combine(netAssemblyPath, "System.dll")),
                MetadataReference.CreateFromFile(Path.Combine(netAssemblyPath, "System.Linq.dll")),
                MetadataReference.CreateFromFile(Path.Combine(netAssemblyPath, "System.Linq.Expressions.dll")),
                MetadataReference.CreateFromFile(Path.Combine(netAssemblyPath, "System.Core.dll")),
                MetadataReference.CreateFromFile(Path.Combine(netAssemblyPath, "System.Runtime.dll")), // design time
                MetadataReference.CreateFromFile(Path.Combine(netAssemblyPath, "System.Private.CoreLib.dll")), // runtime
            ]);

        // plus a selection of ASP.NET assemblies
        references = references
            .Concat(
            [
                MetadataReference.CreateFromFile(Path.Combine(aspAssemblyPath, "Microsoft.AspNetCore.Html.Abstractions.dll")),
            ]);

        var compiler = new Compiler(references);
        var assembly = compiler.Compile("assemblyName", sources);
    }
}

internal class TempDir : IDisposable
{
    public TempDir()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"tmp-{Guid.NewGuid()}");
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public void Dispose()
    {
        Directory.Delete(Path, true);
    }
}