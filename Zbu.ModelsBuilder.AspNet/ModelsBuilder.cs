using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Hosting;

namespace Zbu.ModelsBuilder.AspNet
{
    public class ModelsBuilder
    {
        // fixme - how shall we handle namespaces?
        // fixme - how shall we handle using?

        public void GenerateSourceFiles()
        {
            var appData = HostingEnvironment.MapPath("~/App_Data");
            if (appData == null)
                throw new Exception("Panic: appData is null.");
            var modelsDirectory = Path.Combine(appData, "Models");
            if (!Directory.Exists(modelsDirectory))
                Directory.CreateDirectory(modelsDirectory);

            foreach (var file in Directory.GetFiles(modelsDirectory, "*.generated.cs"))
                File.Delete(file);

            var umbraco = Umbraco.Application.GetApplication();
            var modelTypes = umbraco.GetContentTypes();

            var builder = new TextBuilder();
            builder.Namespace = "wszDefaultNamespace"; // FIXME
            builder.Prepare(modelTypes);

            foreach (var file in Directory.GetFiles(modelsDirectory, "*.cs"))
                builder.Parse(File.ReadAllText(file), modelTypes);

            foreach (var modelType in modelTypes)
            {
                var sb = new StringBuilder();
                builder.Generate(sb, modelType);
                var filename = Path.Combine(modelsDirectory, modelType.Name + ".generated.cs");
                File.WriteAllText(filename, sb.ToString());
            }
        }
    }
}
