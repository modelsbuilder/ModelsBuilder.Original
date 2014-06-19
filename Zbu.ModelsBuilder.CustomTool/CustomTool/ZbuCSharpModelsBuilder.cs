using System.Runtime.InteropServices;
using Microsoft.CSharp;

namespace Zbu.ModelsBuilder.CustomTool.CustomTool
{
    [Guid("98983F6D-BC77-46AC-BA5A-8D9E8763F0D2")]
    [ComVisible(true)]
    public class ZbuCSharpModelsBuilder : ZbuModelsBuilder
    {
        public ZbuCSharpModelsBuilder()
            //: base(new CSharpCodeProvider())
        {
        }

        public override int DefaultExtension(out string pbstrDefaultExtension)
        {
            pbstrDefaultExtension = ".generated.cs";
            return 0;
        }
    }
}