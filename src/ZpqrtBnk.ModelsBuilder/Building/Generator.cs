using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ZpqrtBnk.ModelsBuilder.Umbraco;

namespace ZpqrtBnk.ModelsBuilder.Building
{
    public class Generator
    {
        public static void GenerateModels(UmbracoServices umbracoServices, string modelsDirectory, string bin, string modelsNamespace)
        {
            if (!Directory.Exists(modelsDirectory))
                Directory.CreateDirectory(modelsDirectory);

            foreach (var file in Directory.GetFiles(modelsDirectory, "*.generated.cs"))
                File.Delete(file);

            var typeModels = umbracoServices.GetAllTypes();

            var ourFiles = Directory.GetFiles(modelsDirectory, "*.cs").ToDictionary(x => x, File.ReadAllText);
            var parseResult = new CodeParser().ParseWithReferencedAssemblies(ourFiles);
            var builder = new TextBuilder(typeModels, parseResult, modelsNamespace);

            foreach (var typeModel in builder.GetModelsToGenerate())
            {
                var sb = new StringBuilder();
                builder.Generate(sb, typeModel);
                var filename = Path.Combine(modelsDirectory, typeModel.ClrName + ".generated.cs");
                File.WriteAllText(filename, sb.ToString());
            }

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
                foreach (var file in Directory.GetFiles(modelsDirectory, "*.generated.cs"))
                    ourFiles[file] = File.ReadAllText(file);
                var compiler = new Compiler();
                compiler.Compile(builder.GetModelsNamespace(), ourFiles, bin);
            }

            OutOfDateModelsStatus.Clear();
        }

        public static Dictionary<string, string> GetModels(UmbracoServices umbracoServices, string modelsNamespace, IDictionary<string, string> files)
        {
            var typeModels = umbracoServices.GetAllTypes();

            var parseResult = new CodeParser().ParseWithReferencedAssemblies(files);
            var builder = new TextBuilder(typeModels, parseResult, modelsNamespace);

            var models = new Dictionary<string, string>();
            foreach (var typeModel in builder.GetModelsToGenerate())
            {
                var sb = new StringBuilder();
                builder.Generate(sb, typeModel);
                models[typeModel.ClrName] = sb.ToString();
            }
            return models;
        }
    }
}
