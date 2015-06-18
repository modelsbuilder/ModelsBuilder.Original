using System.Runtime.InteropServices;

namespace Zbu.ModelsBuilder.CustomTool.CustomTool
{
    [Guid("98983F6D-BC77-46AC-BA5A-8D9E8763F0D2")]
    [ComVisible(true)]
    public class ZbuCSharpModelsBuilder : ZbuModelsBuilder
    {
        //public ZbuCSharpModelsBuilder()
        //    : base(new CSharpCodeProvider())
        //{ }

        protected override string GetDefaultExtension()
        {
            return ".generated.cs";
        }
    }
}