using System.Runtime.InteropServices;

namespace Umbraco.ModelsBuilder.CustomTool.CustomTool
{
    [Guid("98983F6D-BC77-46AC-BA5A-8D9E8763F0D2")]
    [ComVisible(true)]
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