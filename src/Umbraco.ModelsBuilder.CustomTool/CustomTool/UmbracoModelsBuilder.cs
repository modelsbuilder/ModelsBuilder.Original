using System;
using System.Collections.Generic;
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
        private IVsGeneratorProgress _progress;

        #region IVsSingleFileGenerator Members

        protected override int Generate(string wszInputFilePath,
                                    string bstrInputFileContents,
                                    string wszDefaultNamespace,
                                    IntPtr[] rgbOutputFileContents,
                                    out uint pcbOutput,
                                    IVsGeneratorProgress pGenerateProgress)
        {
            var rc = GenerateInternal(wszInputFilePath,
                //bstrInputFileContents,
                wszDefaultNamespace,
                rgbOutputFileContents,
                out pcbOutput,
                pGenerateProgress,
                out var errMsg);

            if (errMsg != null)
                VisualStudioHelper.ReportError(pGenerateProgress, errMsg);

            return rc;
        }

        private void Progress(IVsGeneratorProgress progress, uint percent)
        {
            progress.Progress(percent, 100);
        }

        private int GenerateInternal(string wszInputFilePath,
                                //string bstrInputFileContents,
                                string wszDefaultNamespace,
                                IntPtr[] rgbOutputFileContents,
                                out uint pcbOutput,
                                IVsGeneratorProgress pGenerateProgress,
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

                Progress(pGenerateProgress, 0);
                VisualStudioHelper.ReportMessage("Starting v{0} {1}.", ApiVersion.Current.Version, DateTime.Now);

                // GetSourceItem was an endless source of confusion - this should be better
                //var vsitem = VisualStudioHelper.GetSourceItem(wszInputFilePath);
                var sourceItem = GetProjectItem();

                // save project before modifying it
                var project = sourceItem.ContainingProject;
                if (!project.Saved)
                    project.Save();
                Progress(pGenerateProgress, 5);

                var sourceItemDirectory = Path.GetDirectoryName(wszInputFilePath) ?? "";

                Progress(pGenerateProgress, 10);
                var options = VisualStudioHelper.GetOptions();
                options.Validate();

                Progress(pGenerateProgress, 15);
                var api = new ApiClient(options.UmbracoUrl, options.UmbracoUser, options.UmbracoPassword);
                api.ValidateClientVersion(); // so we get a meaningful error message first

                // exclude .generated.cs files but don't delete them now, should anything go wrong
                Progress(pGenerateProgress, 20);
                var ourFiles = Directory.GetFiles(sourceItemDirectory, "*.cs")
                    .Where(x => !x.EndsWith(".generated.cs"))
                    .ToDictionary(x => x, File.ReadAllText);

                Progress(pGenerateProgress, 25);
                var generatedFiles = api.GetModels(ourFiles, wszDefaultNamespace);

                /*
                VisualStudioHelper.ReportMessage("Found {0} content types in Umbraco.", modelTypes.Count);
                */

                Progress(pGenerateProgress, 50);

                VisualStudioHelper.ClearExistingItems(sourceItem);
                Progress(pGenerateProgress, 70);

                var projectDirectory = ((string)sourceItem.ContainingProject.Properties.Item("LocalPath").Value).TrimEnd(Path.DirectorySeparatorChar);
                var relativePath = sourceItemDirectory.Substring(projectDirectory.Length).TrimStart(Path.DirectorySeparatorChar);

                foreach (var file in Directory.GetFiles(sourceItemDirectory, "*.generated.cs"))
                    File.Delete(file);
                var filenames = new List<string>();
                foreach (var file in generatedFiles)
                {
                    var filename = Path.Combine(relativePath, file.Key + ".generated.cs");
                    filenames.Add(filename);
                    File.WriteAllText(Path.Combine(projectDirectory, filename), file.Value);
                }
                Progress(pGenerateProgress, 80);

                VisualStudioHelper.AddGeneratedItems(sourceItem, projectDirectory, filenames);
                Progress(pGenerateProgress, 90);

                // we *do* need to generate something
                // else Visual Studio reports an error
                var code = new StringBuilder();
                TextHeaderWriter.WriteHeader(code);
                code.Append("// Umbraco ModelsBuilder\n");
                code.AppendFormat("// {0:yyyy-MM-ddTHH:mm:ssZ}\n\n", DateTime.UtcNow);
                var data = Encoding.Default.GetBytes(code.ToString());
                var ptr = Marshal.AllocCoTaskMem(data.Length);
                Marshal.Copy(data, 0, ptr, data.Length);
                pcbOutput = (uint)data.Length;
                rgbOutputFileContents[0] = ptr;

                // fixme
                // on NetSdk project, we have modified the csproj - but not reloaded it
                // and returning something here, will modify the project in Visual Studio
                // which will then try to reload the modified csproj = conflict
                // how can we force-reload the project before we return?
                project.DTE.ExecuteCommand("Project.ReloadProject"); // but, must unload first = slow?!
                //
                // if we don't return something we have an error
                // if we *do* return something, it conflicts with the changes we have made to the csproj
                // if we make it a command (vs a generator) we are totally in charge
                // but, how can we make sure that the command only shows on 1 file? foo.models

                Progress(pGenerateProgress, 95);

                VisualStudioHelper.ReportMessage("Done.");
                Progress(pGenerateProgress, 100);
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