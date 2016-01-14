using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace Zbu.ModelsBuilder.CustomTool.CustomTool
{
    // note: see https://github.com/RazorGenerator/RazorGenerator

    [ComVisible(true)]
    public abstract class BaseCodeGenerator : IVsSingleFileGenerator
    {
        #region IVsSingleFileGenerator Members

        int IVsSingleFileGenerator.DefaultExtension(out string pbstrDefaultExtension)
        {
            return DefaultExtension(out pbstrDefaultExtension);
        }

        int IVsSingleFileGenerator.Generate(string wszInputFilePath, string bstrInputFileContents, string wszDefaultNamespace, IntPtr[] rgbOutputFileContents, out uint pcbOutput, IVsGeneratorProgress pGenerateProgress)
        {
            if (bstrInputFileContents == null)
                throw new ArgumentNullException("bstrInputFileContents");
            return Generate(wszInputFilePath, bstrInputFileContents, wszDefaultNamespace, rgbOutputFileContents, out pcbOutput, pGenerateProgress);
        }

        // use the following methods instead of doing it in explicit methods above,
        // else we have a "CA1033: Interface methods should be callable by child types"

        protected int DefaultExtension(out string pbstrDefaultExtension)
        {
            try
            {
                pbstrDefaultExtension = GetDefaultExtension();
                return VSConstants.S_OK;
            }
            catch (Exception)
            {
                pbstrDefaultExtension = string.Empty;
                return VSConstants.E_FAIL;
            }
        }

        #endregion

        protected abstract string GetDefaultExtension();

        protected abstract int Generate(string wszInputFilePath, string bstrInputFileContents, string wszDefaultNamespace, IntPtr[] rgbOutputFileContents, out uint pcbOutput, IVsGeneratorProgress pGenerateProgress);
    }
}
