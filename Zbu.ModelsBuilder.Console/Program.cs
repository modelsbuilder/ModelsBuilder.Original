using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zbu.ModelsBuilder.Umbraco;

namespace Zbu.ModelsBuilder.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            GenerateModels();
        }

        private static void GenerateModels()
        {
            const string modelsDirectory = "Models";
            if (!Directory.Exists(modelsDirectory))
                Directory.CreateDirectory(modelsDirectory);

            foreach (var file in Directory.GetFiles(modelsDirectory, "*.generated.cs"))
                File.Delete(file);

            var cstr = ConfigurationManager.ConnectionStrings["umbracoDbDSN"];

            IList<TypeModel> modelTypes;
            using (var umbraco = Application.GetApplication(cstr.ConnectionString, cstr.ProviderName))
            {
                modelTypes = umbraco.GetContentAndMediaTypes();
            }

            var ns = ConfigurationManager.AppSettings["Zbu.ModelsBuilder.ModelsNamespace"];
            if (string.IsNullOrWhiteSpace(ns)) ns = "Umbraco.Web.PublishedContentModels";

            var builder = new TextBuilder();
            builder.Namespace = ns;
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
