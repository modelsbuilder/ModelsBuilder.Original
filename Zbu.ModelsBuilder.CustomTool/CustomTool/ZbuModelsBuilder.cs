using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell.Interop;
using Zbu.ModelsBuilder.CustomTool.VisualStudio;

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
                // though that only happens if you explicitely set it to whitespaces
                // otherwise VisualStudio will use the default one... so it will work
                // if the namespace is left empty in VS.
                if (string.IsNullOrWhiteSpace(wszDefaultNamespace))
                    throw new Exception("No namespace.");

                VisualStudioHelper.ReportMessage("Starting.");

                var path = Path.GetDirectoryName(wszInputFilePath) ?? "";
                
                var options = VisualStudioHelper.GetOptions();
                options.Validate();

                var vsitem = VisualStudioHelper.GetSourceItem(wszInputFilePath);
                VisualStudioHelper.ClearExistingItems(vsitem);

                foreach (var file in Directory.GetFiles(path, "*.generated.cs"))
                    File.Delete(file);

                IList<TypeModel> modelTypes;
                using (var umbraco = Umbraco.Application.GetApplication(options.ConnectionString, options.DatabaseProvider))
                {
                    modelTypes = umbraco.GetContentAndMediaTypes();
                }
                
                VisualStudioHelper.ReportMessage("Found {0} content types in Umbraco.", modelTypes.Count);

                var builder = new TextBuilder();
                builder.Namespace = wszDefaultNamespace;
                builder.Prepare(modelTypes);
                foreach (var file in Directory.GetFiles(path, "*.cs"))
                    builder.Parse(File.ReadAllText(file), modelTypes);

                VisualStudioHelper.ReportMessage("Need to generate {0} files.", modelTypes.Count);

                var inputFilename = Path.GetFileNameWithoutExtension(wszInputFilePath);
                if (modelTypes.Any(x => x.Name.InvariantEquals(inputFilename)))
                    throw new Exception("Name collision, there is a model named " + inputFilename);

                foreach (var modelType in modelTypes)
                {
                    var sb = new StringBuilder();
                    builder.Generate(sb, modelType);
                    var filename = Path.Combine(path, modelType.Name + ".generated.cs");
                    File.WriteAllText(filename, sb.ToString());
                    VisualStudioHelper.AddGeneratedItem(vsitem, filename);
                }

                VisualStudioHelper.ReportMessage("Generated {0} files.", modelTypes.Count);

                var code = new StringBuilder();
                builder.WriteHeader(code);
                code.Append("// ZpqrtBnk Umbraco ModelsBuilder");
                code.AppendFormat("// {0:yyyy-MM-ddTHH:mm:ssZ}", DateTime.UtcNow);

                var data = Encoding.Default.GetBytes(code.ToString());
                var ptr = Marshal.AllocCoTaskMem(data.Length);
                Marshal.Copy(data, 0, ptr, data.Length);
                pcbOutput = (uint)data.Length;
                rgbOutputFileContents[0] = ptr;

                VisualStudioHelper.ReportMessage("Done.");
            }
            catch (Exception e)
            {
                var message = string.Format("ZbuModelsBuilder failed to generate code: {0}: {1}",
                    e.GetType().Name, e.Message);
                VisualStudioHelper.ReportError(pGenerateProgress, message);
                VisualStudioHelper.ReportMessage(message);
                VisualStudioHelper.ReportMessage(e.StackTrace);
                //MessageBox.Show(e.Message, "Unable to generate code");
                throw;
            }

            return 0;
        }

        #endregion
    }
}