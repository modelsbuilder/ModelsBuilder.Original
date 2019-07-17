using Microsoft.VisualStudio.Shell;
using System.Runtime.InteropServices;
using Umbraco.ModelsBuilder.CustomTool.VisualStudio;

namespace Umbraco.ModelsBuilder.CustomTool.CustomTool
{
    [Guid("98983F6D-BC77-46AC-BA5A-8D9E8763F0D2")]
    [ComVisible(true)]

    // https://stackoverflow.com/questions/44154869/single-file-generator-not-working-for-net-standard-projects-in-visual-studio-20
    // https://github.com/aspnet/Tooling/issues/394
    // https://github.com/dotnet/project-system/issues/3535
    [CodeGeneratorRegistration(typeof(UmbracoCSharpModelsBuilder), "UmbracoModelsBuilder", VSLangProj80.vsContextGuids.vsContextGuidVCSProject, GeneratesDesignTimeSource = true)]
    [CodeGeneratorRegistration(typeof(UmbracoCSharpModelsBuilder), "UmbracoModelsBuilder", vsContextGuids.vsContextGuidNetSdkProject, GeneratesDesignTimeSource = true)]
    //[CodeGeneratorRegistration(typeof(UmbracoCSharpModelsBuilder), "UmbracoModelsBuilder", "{694DD9B6-B865-4C5B-AD85-86356E9C88DC}", GeneratesDesignTimeSource = true)]
    
    public class UmbracoCSharpModelsBuilder : UmbracoModelsBuilder
    {
        //public UmbracoCSharpModelsBuilder()
        //    : base(new CSharpCodeProvider())
        //{ }

        protected override string GetDefaultExtension()
        {
            return ".generated.cs";
        }
    }
}