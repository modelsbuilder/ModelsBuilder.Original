using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ZpqrtBnk.ModelsBuilder.Configuration;
using ZpqrtBnk.ModelsBuilder.Umbraco;

namespace ZpqrtBnk.ModelsBuilder.Building
{
    public class Generator
    {
        private readonly UmbracoServices _umbracoServices;
        private readonly IBuilderFactory _builderFactory;
        private readonly ICodeWriterFactory _writerFactory;
        private readonly Config _config;

        public Generator(UmbracoServices umbracoServices, IBuilderFactory builderFactory, ICodeWriterFactory writerFactory, Config config)
        {
            _umbracoServices = umbracoServices;
            _builderFactory = builderFactory;
            _writerFactory = writerFactory;
            _config = config;
        }

        public void GenerateModels(string modelsDirectory, string modelsNamespace, string bin)
        {
            if (!Directory.Exists(modelsDirectory))
                Directory.CreateDirectory(modelsDirectory);

            // delete all existing generated files
            foreach (var file in Directory.GetFiles(modelsDirectory, "*.generated.cs"))
                File.Delete(file);

            // get models from Umbraco
            var model = new CodeModel
            {
                TypeModels = _umbracoServices.GetAllTypes()
            };

            // get our (non-generated) files and parse them
            var ourFiles = Directory.GetFiles(modelsDirectory, "*.cs").ToDictionary(x => x, File.ReadAllText);
            var parseResult = new CodeParser().ParseWithReferencedAssemblies(ourFiles);
            
            // create a builder, build a context, create a writer
            var builder = _builderFactory.CreateBuilder();
            builder.Build(model, _config, parseResult, modelsNamespace);
            var writer = _writerFactory.CreateWriter(model);

            // write each model file
            foreach (var typeModel in model.TypeModels)
            {
                writer.Reset();
                writer.WriteModelFile(typeModel);
                var filename = Path.Combine(modelsDirectory, typeModel.ClrName + ".generated.cs");
                File.WriteAllText(filename, writer.Code);
            }

            // write the infos file
            writer.Reset();
            writer.WriteModelInfosFile(model);
            var metaFilename = Path.Combine(modelsDirectory, model.ModelInfosClassName + ".generated.cs");
            File.WriteAllText(metaFilename, writer.Code);

            // the idea was to calculate the current hash and to add it as an extra file to the compilation,
            // in order to be able to detect whether a DLL is consistent with an environment - however the
            // environment *might not* contain the local partial files, and thus it could be impossible to
            // calculate the hash. So... maybe that's not a good idea after all?
            /*
            var currentHash = HashHelper.Hash(ourFiles, typeModels);
            ourFiles["models.hash.cs"] = $@"using ZpqrtBnk.ModelsBuilder;
[assembly:ModelsBuilderAssembly(SourceHash = ""{currentHash}"")]
";
            */

            if (bin != null)
            {
                // build
                foreach (var file in Directory.GetFiles(modelsDirectory, "*.generated.cs"))
                    ourFiles[file] = File.ReadAllText(file);
                var compiler = new Compiler();
                compiler.Compile(model.ModelsNamespace, ourFiles, bin);
            }

            OutOfDateModelsStatus.Clear();
        }

        public Dictionary<string, string> GetModels(string modelsNamespace, IDictionary<string, string> files)
        {
            // get models from Umbraco
            var model = new CodeModel
            {
                TypeModels = _umbracoServices.GetAllTypes()
            };

            // parse the (non-generated) files
            var parseResult = new CodeParser().ParseWithReferencedAssemblies(files);

            // create a builder, build a context, create a writer
            var builder = _builderFactory.CreateBuilder();
            builder.Build(model, _config, parseResult, modelsNamespace);
            var writer = _writerFactory.CreateWriter(model);
            var generated = new Dictionary<string, string>();

            // write each model file
            foreach (var typeModel in model.TypeModels)
            {
                writer.Reset();
                writer.WriteModelFile(typeModel);
                generated[typeModel.ClrName] = writer.Code;
            }

            if (generated.ContainsKey(parseResult.ModelInfoClassName))
                throw new InvalidOperationException($"Collision, cannot use {parseResult.ModelInfoClassName} for both a content type and the infos class.");

            // write the info files
            writer.Reset();
            writer.WriteModelInfosFile(model);
            generated[parseResult.ModelInfoClassName] = writer.Code;

            return generated;
        }
    }
}
