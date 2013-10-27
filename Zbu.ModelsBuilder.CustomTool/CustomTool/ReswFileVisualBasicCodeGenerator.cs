using System.Runtime.InteropServices;
using Microsoft.VisualBasic;

namespace ChristianHelle.DeveloperTools.CodeGenerators.Resw.VSPackage.CustomTool
{
    [Guid("92DFB543-7138-419B-99D9-90CC77607671")]
    [ComVisible(true)]
    public class ReswFileVisualBasicCodeGenerator : ReswFileCodeGenerator
    {
        public ReswFileVisualBasicCodeGenerator()
            : base(new VBCodeProvider())
        {
        }

        public override int DefaultExtension(out string pbstrDefaultExtension)
        {
            pbstrDefaultExtension = ".vb";
            return 0;
        }
    }
}