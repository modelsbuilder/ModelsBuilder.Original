using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.VisualStudio.Shell.Interop;
using Umbraco.ModelsBuilder.Api;
using Umbraco.ModelsBuilder.Building;
using Umbraco.ModelsBuilder.CustomTool.VisualStudio;

namespace Umbraco.ModelsBuilder.CustomTool.CustomTool
{
    [ComVisible(true)]
    public abstract class UmbracoModelsBuilder : BaseCodeGeneratorWithSite
    {
        #region IVsSingleFileGenerator Members

        protected override int Generate(string wszInputFilePath,
                                    string bstrInputFileContents,
                                    string wszDefaultNamespace,
                                    IntPtr[] rgbOutputFileContents,
                                    out uint pcbOutput,
                                    IVsGeneratorProgress pGenerateProgress)
        {
            return GenerateWithPump(wszInputFilePath,
                //bstrInputFileContents,
                wszDefaultNamespace,
                rgbOutputFileContents,
                out pcbOutput,
                pGenerateProgress);
        }

        // wraps GenerateRaw in a message pump so that Visual Studio
        // will display the nice "waiting" modal window...
        private int GenerateWithPump(string wszInputFilePath,
                                     //string bstrInputFileContents,
                                     string wszDefaultNamespace,
                                     IntPtr[] rgbOutputFileContents,
                                     out uint pcbOutput,
                                     IVsGeneratorProgress pGenerateProgress)
        {
            uint pcbOutput2 = 0;
            var rc = 0;
            string errMsg = null;

            VisualStudioHelper.PumpAction("Generating models...", "Please wait while Umbraco.ModelsBuilder generates models.", () =>
            {
                rc = GenerateRaw(wszInputFilePath,
                //bstrInputFileContents,
                wszDefaultNamespace,
                rgbOutputFileContents,
                out pcbOutput2,
                //pGenerateProgress,
                out errMsg);
            });

            // get value back
            pcbOutput = pcbOutput2;

            // handle error here - cannot do it in PumpAction - ComObject exception
            if (errMsg != null)
                VisualStudioHelper.ReportError(pGenerateProgress, errMsg);

            return rc;
        }

        private int GenerateRaw(string wszInputFilePath,
                                //string bstrInputFileContents,
                                string wszDefaultNamespace,
                                IntPtr[] rgbOutputFileContents,
                                out uint pcbOutput,
                                //IVsGeneratorProgress pGenerateProgress,
                                out string errMsg)
        {
            errMsg = null;

            try
            {
                // though that only happens if you explicitely set it to whitespaces
                // otherwise VisualStudio will use the default one... so it will work
                // if the namespace is left empty in VS.
                if (string.IsNullOrWhiteSpace(wszDefaultNamespace))
                    throw new Exception("No namespace.");

                VisualStudioHelper.ReportMessage("Starting v{0} {1}.", ApiVersion.Current.Version, DateTime.Now);

                var path = Path.GetDirectoryName(wszInputFilePath) ?? "";

                var options = VisualStudioHelper.GetOptions();
                options.Validate();

                var api = new ApiClient(options.UmbracoUrl, options.UmbracoUser, options.UmbracoPassword);
                api.ValidateClientVersion(); // so we get a meaningful error message first

                // exclude .generated.cs files but don't delete them now, should anything go wrong
                var ourFiles = Directory.GetFiles(path, "*.cs")
                    .Where(x => !x.EndsWith(".generated.cs"))
                    .ToDictionary(x => x, File.ReadAllText);
                var genFiles = api.GetModels(ourFiles, wszDefaultNamespace);

                /*
                VisualStudioHelper.ReportMessage("Found {0} content types in Umbraco.", modelTypes.Count);
                */

                // GetSourceItem was an endless source of confusion - this should be better
                //var vsitem = VisualStudioHelper.GetSourceItem(wszInputFilePath);
                var vsitem = GetProjectItem();
                VisualStudioHelper.ClearExistingItems(vsitem);

                foreach (var file in Directory.GetFiles(path, "*.generated.cs"))
                    File.Delete(file);

                foreach (var file in genFiles)
                {
                    var filename = Path.Combine(path, file.Key + ".generated.cs");
                    File.WriteAllText(filename, file.Value);
                    VisualStudioHelper.AddGeneratedItem(vsitem, filename);
                }

                // we need to generate something
                var code = new StringBuilder();
                TextHeaderWriter.WriteHeader(code);
                code.Append("// Umbraco ModelsBuilder\n");
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
                var message = string.Format("UmbracoModelsBuilder failed to generate code: {0}: {1}",
                    e.GetType().Name, e.Message);
                errMsg = message;
                //cannot do this within a running Task - ComObject exception
                //VisualStudioHelper.ReportError(pGenerateProgress, message);
                VisualStudioHelper.ReportMessage(message);
                VisualStudioHelper.ReportMessage(e.StackTrace);

                var inner = e.InnerException;
                while (inner != null)
                {
                    message = string.Format("Inner: {0}: {1}", inner.GetType().Name, inner.Message);
                    VisualStudioHelper.ReportMessage(message);
                    VisualStudioHelper.ReportMessage(inner.StackTrace);
                    inner = inner.InnerException;
                }

                var aggr = e as AggregateException;
                if (aggr != null)
                    foreach (var aggrInner in aggr.Flatten().InnerExceptions)
                    {
                        message = string.Format("AggregateInner: {0}: {1}", aggrInner.GetType().Name, aggrInner.Message);
                        VisualStudioHelper.ReportMessage(message);
                        VisualStudioHelper.ReportMessage(aggrInner.StackTrace);
                    }

                throw;
            }

            return 0;
        }

        #endregion
    }
}