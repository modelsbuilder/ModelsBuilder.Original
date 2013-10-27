using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell.Interop;

namespace Zbu.ModelsBuilder.CustomTool.CustomTool
{
    [ComVisible(true)]
    public abstract class ZbuModelsBuilder : IVsSingleFileGenerator
    {
        private readonly CodeDomProvider _codeDomProvider;
        private readonly TypeAttributes? _classAccessibility;

        protected ZbuModelsBuilder(CodeDomProvider codeDomProvider, TypeAttributes? classAccessibility = null)
        {
            this._codeDomProvider = codeDomProvider;
            this._classAccessibility = classAccessibility;
        }

        #region IVsSingleFileGenerator Members

        public abstract int DefaultExtension(out string pbstrDefaultExtension);

        public virtual int Generate(string wszInputFilePath,
                                    string bstrInputFileContents,
                                    string wszDefaultNamespace,
                                    IntPtr[] rgbOutputFileContents,
                                    out uint pcbOutput,
                                    IVsGeneratorProgress pGenerateProgress)
        {
            try
            {
                IList<TypeModel> types;
                using (var umbraco = Umbraco.Application.GetApplication())
                {
                    types = umbraco.GetContentTypes();
                }
                var builder = new Builder();
                builder.Prepare(types);
                var sb = new StringBuilder();
                builder.Generate(sb, types);
                var code = sb.ToString();

                var data = Encoding.Default.GetBytes(code);

                var ptr = Marshal.AllocCoTaskMem(data.Length);
                Marshal.Copy(data, 0, ptr, data.Length);

                pcbOutput = (uint)data.Length;
                rgbOutputFileContents[0] = ptr;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Unable to generate code");
                throw;
            }

            return 0;
        }

        #endregion
    }
}