using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Compilation;
using System.Web.Hosting;

namespace Zbu.ModelsBuilder.AspNet
{
    public class ModelsBuildProvider : BuildProvider
    {
        // see http://msdn.microsoft.com/en-us/library/system.web.compilation.assemblybuilder.createcodefile%28v=vs.90%29.ASPX
        // there _is_ a way to create source code files, so we _could_ use the TextBuilder
        // but here we're assuming that going for CodeDom is faster (?)

        // no, wait -- anyway CodeDomBuilder will never work because umbraco is not
        // up and running at the time GenerateCode is invoked, so the code files have
        // to be generated beforehand and the builder should just insert them in the
        // right place...

        // can it generate them in app_code?
        // idea would be to drop a models.models file in App_Code that would trigger the build
        // this is for ppl who don't have visual studio, anyway... if you have VS you want to
        // build with VS and then copy the code over to the server, based upon models.cs

        // the actual files generation has to be triggered by the UI

        public override void GenerateCode(AssemblyBuilder assemblyBuilder)
        {
            // issue: I can't put my files into App_Code or they'll get... compiled... too soon?

            var appData = HostingEnvironment.MapPath("~/App_Data");
            if (appData == null)
                throw new Exception("Panic: appData is null.");
            var modelsDirectory = Path.Combine(appData, "Models");
            if (!Directory.Exists(modelsDirectory))
                return;

            foreach (var file in Directory.GetFiles(modelsDirectory, "*.cs"))
            {
                var text = File.ReadAllText(file);
                var textWriter = assemblyBuilder.CreateCodeFile(this);
                textWriter.Write(text);
                textWriter.Close();
            }
        }


        // that was the CodeDom attempt...
        /*
        private readonly CodeDomBuilder _builder = new CodeDomBuilder();

        public override void GenerateCode(AssemblyBuilder assemblyBuilder)
        {
            var filename = VirtualPath;

            // issue: will the application be started when we generated?
            // issue: I assume NOT because... bah ;->

            // fdo we HAVE to use codedom
            // or can we generate a cs file?

            if (global::Umbraco.Core.ApplicationContext.Current == null)
            {
                global::Umbraco.Core.Logging.LogHelper.Debug<ModelsBuildProvider>("NULL");
                return;
            }

            var modelTypes = Umbraco.Application.GetApplication().GetContentTypes();
            _builder.Prepare(modelTypes);

            // SHOULD be from the .models file? but THEN it should be a CS file?
            _builder.Namespace = "";
            _builder.Using.Add("");
            // references?

            var path = Path.GetDirectoryName(filename) ?? "";
            foreach (var file in Directory.GetFiles(path, "*.cs"))
                _builder.Parse(File.ReadAllText(file), modelTypes);

            foreach (var type in modelTypes)
            {
                var unit = GenerateUnit("wtf", type);
                assemblyBuilder.AddCodeCompileUnit(this, unit);

            }
        }

        CodeCompileUnit GenerateUnit(string filename, TypeModel modelType)
        {
            //List<ClassMapper> classes = ClassMapper.GetClasses(VirtualPathProvider.OpenFile(fileName));

            var unit = new CodeCompileUnit();
            //unit.ReferencedAssemblies.Add("ObjectMapperUtils");

            var ns = new CodeNamespace("my.namespace");
            unit.Namespaces.Add(ns);
            _builder.Generate(ns, modelType);

            return unit;
        }
        */
    }
}
