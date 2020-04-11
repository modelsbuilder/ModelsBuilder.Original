using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Our.ModelsBuilder.Options;
using Our.ModelsBuilder.Umbraco;

namespace Our.ModelsBuilder.Building
{
    public class Generator
    {
        private readonly ICodeFactory _codeFactory;
        private readonly ModelsBuilderOptions _options;

        public Generator(ICodeFactory codeFactory, ModelsBuilderOptions options)
        {
            _codeFactory = codeFactory;
            _options = options;
        }

        public void GenerateModels(string modelsDirectory, string modelsNamespace, string bin)
        {
            if (!Directory.Exists(modelsDirectory))
                Directory.CreateDirectory(modelsDirectory);

            // delete all existing generated files
            foreach (var file in Directory.GetFiles(modelsDirectory, "*.generated.cs"))
                File.Delete(file);

            // get our (non-generated) files
            var files = Directory.GetFiles(modelsDirectory, "*.cs").ToDictionary(x => x, File.ReadAllText);

            var codeModel = CreateModels(modelsNamespace, files, (name, code) =>
            {
                var filename = Path.Combine(modelsDirectory, name + ".generated.cs");
                File.WriteAllText(filename, code);
            });

            // the idea was to calculate the current hash and to add it as an extra file to the compilation,
            // in order to be able to detect whether a DLL is consistent with an environment - however the
            // environment *might not* contain the local partial files, and thus it could be impossible to
            // calculate the hash. So... maybe that's not a good idea after all?
            /*
            var currentHash = HashHelper.Hash(ourFiles, typeModels);
            ourFiles["models.hash.cs"] = $@"using Our.ModelsBuilder;
[assembly:ModelsBuilderAssembly(SourceHash = ""{currentHash}"")]
";
            */

            if (bin != null)
            {
                // build
                foreach (var file in Directory.GetFiles(modelsDirectory, "*.generated.cs"))
                    files[file] = File.ReadAllText(file);
                var compiler = new Compiler(_options.LanguageVersion);
                // FIXME what is the name of the DLL as soon as we accept several namespaces = an option?
                compiler.Compile(codeModel.ModelsNamespace, files, bin);
            }

            OutOfDateModelsStatus.Clear();
        }

        public Dictionary<string, string> GetModels(string modelsNamespace, IDictionary<string, string> files)
        {
            var generated = new Dictionary<string, string>();
            CreateModels(modelsNamespace, files, (name, code) => generated[name] = code);
            return generated;
        }

        private CodeModel CreateModels(string modelsNamespace, IDictionary<string, string> sources, Action<string, string> acceptModel)
        {
            // get model data from Umbraco, and create a code model (via all the steps)
            var modelData = _codeFactory.CreateCodeModelDataSource().GetCodeModelData();
            var codeModel = CreateCodeModel(_codeFactory, sources, modelData, _options, modelsNamespace);

            // create a code writer
            var codeWriter = _codeFactory.CreateCodeWriter(codeModel);

            // write each model file
            foreach (var contentTypeModel in codeModel.ContentTypes.ContentTypes)
            {
                codeWriter.Reset();
                codeWriter.WriteModelFile(contentTypeModel);

                // detect name collision
                if (contentTypeModel.ClrName == codeModel.ModelInfosClassName)
                    throw new InvalidOperationException($"Collision, cannot use {codeModel.ModelInfosClassName} for both a content type and the infos class.");

                acceptModel(contentTypeModel.ClrName, codeWriter.Code);
            }

            // write the info files
            codeWriter.Reset();
            codeWriter.WriteModelInfosFile();
            acceptModel(codeModel.ModelInfosClassName, codeWriter.Code);

            return codeModel;
        }

        public static CodeModel CreateCodeModel(ICodeFactory codeFactory, IDictionary<string, string> sources, CodeModelData modelData, ModelsBuilderOptions options, string modelsNamespace = null)
        {
            // create an option builder
            var optionsBuilder = codeFactory.CreateCodeOptionsBuilder();

            // create a parser, and parse the (non-generated) files, updating the options builder
            var parser = codeFactory.CreateCodeParser();
            parser.Parse(sources, optionsBuilder, ReferencedAssemblies.References);

            // apply namespace - may come from e.g. the Visual Studio extension - FIXME no?
            if (!string.IsNullOrWhiteSpace(modelsNamespace))
                optionsBuilder.SetModelsNamespace(modelsNamespace);

            // create a code model builder, and build the code model
            var codeModelBuilder = codeFactory.CreateCodeModelBuilder(options, optionsBuilder.CodeOptions);
            return codeModelBuilder.Build(modelData);
        }
    }
}
