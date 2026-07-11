using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Infrastructure.ModelsBuilder;
using Umbraco.Cms.Infrastructure.ModelsBuilder.Building;
using Umbraco.Extensions;

namespace Our.ModelsBuilder.Building;

public class OurModelsGenerator : IModelsGenerator
{
    private readonly IHostEnvironment _hostingEnvironment;
    private readonly OutOfDateModelsStatus _outOfDateModels;
    private readonly ModelsBuilderSettings _settings;

    private readonly ICodeModelDataSource _codeModelDataSource;
    private readonly ICodeModelBuilder _codeModelBuilder;
    private readonly ICodeVisitor _codeVisitor;
    private readonly ICodeWriter _codeWriter;

    public OurModelsGenerator(
        IHostEnvironment hostingEnvironment, 
        OutOfDateModelsStatus outOfDateModels, 
        IOptions<ModelsBuilderSettings> settings, 
        ICodeModelDataSource codeModelDataSource, 
        ICodeModelBuilder codeModelBuilder, 
        ICodeVisitor codeVisitor, 
        ICodeWriter codeWriter)
    {
        _hostingEnvironment = hostingEnvironment;
        _outOfDateModels = outOfDateModels;
        _settings = settings.Value;
        _codeModelDataSource = codeModelDataSource;
        _codeModelBuilder = codeModelBuilder;
        _codeVisitor = codeVisitor;
        _codeWriter = codeWriter;
    }

    public void GenerateModels()
    {
        var modelsDirectory = _settings.ModelsDirectoryAbsolute(_hostingEnvironment);
        if (!Directory.Exists(modelsDirectory))
            Directory.CreateDirectory(modelsDirectory);

        var sources = new Dictionary<string, string>();
        foreach (var file in Directory.GetFiles(modelsDirectory, "*.cs"))
            if (!file.EndsWith(".generated.cs")) sources[file] = File.ReadAllText(file);
        _codeVisitor.Visit(sources);

        var codeModelData = _codeModelDataSource.GetCodeModelData();
        var codeModel = _codeModelBuilder.Build(codeModelData);

        var generated = new Dictionary<string, string>();

        _codeWriter.WriteModelInfosFile(codeModel);
        var filename = Path.Combine(modelsDirectory, "ModelInfos.generated.cs");
        generated[filename] = _codeWriter.Code;

        foreach (var model in codeModel.ContentTypes.ContentTypes)
        {
            _codeWriter.Reset();
            _codeWriter.WriteModelFile(codeModel, model);
            filename = Path.Combine(modelsDirectory, model.ClrName + ".generated.cs");
            generated[filename] = _codeWriter.Code;
        }

        // only if we've come so far, don't trash what's existing in case of a failure

        foreach (var file in Directory.GetFiles(modelsDirectory, "*.generated.cs"))
            File.Delete(file);

        foreach (var (generatedFilename, generatedCode) in generated)
            File.WriteAllText(generatedFilename, generatedCode);

        _outOfDateModels.Clear();
    }
}
