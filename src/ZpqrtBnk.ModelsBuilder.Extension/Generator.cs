using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ZpqrtBnk.ModelsBuilder.Api;
using ZpqrtBnk.ModelsBuilder.Extension.VisualStudio;
using ZpqrtBnk.ModelsBuilder.Web.Api;

namespace ZpqrtBnk.ModelsBuilder.Extension
{
    public class Generator
    {
        private static void Progress(DTE dte, string message, int percent)
        {
            dte.StatusBar.Progress(percent < 100, message, percent, 100);
        }

        public static void Generate(AsyncPackage package, ProjectItem sourceItem)
        {
            try
            {
                if (sourceItem == null)
                    throw new ArgumentNullException(nameof(sourceItem));

                TryGenerate(sourceItem);
            }
            catch (Exception e)
            {
                var message = string.Format("UmbracoModelsBuilder failed to generate code: {0}: {1}",
                    e.GetType().Name, e.Message);

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

                Progress(sourceItem.DTE, "Failed.", 100);

                // Show a message box to prove we were here
                VsShellUtilities.ShowMessageBox(
                    package,
                    "See exception details in the General pane of the Output windows.",
                    "Failed to build models",
                    OLEMSGICON.OLEMSGICON_CRITICAL,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
        }

        private static string GetNameSpace(string path)
        {
            var parts = path.Split(new[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < parts.Length; i++)
            {
                parts[i] = ZpqrtBnk.ModelsBuilder.Building.Compiler.CreateValidIdentifier(parts[i]);
            }
            return string.Join(".", parts);
        }

        public static void TryGenerate(ProjectItem sourceItem)
        {
            Progress(sourceItem.DTE, "Build Models...", 0);
            VisualStudioHelper.ReportMessage("Starting v{0} {1}.", ApiVersion.Current.Version, DateTime.Now);

            // save project before modifying it
            Progress(sourceItem.DTE, "Save project...", 5);
            var project = sourceItem.ContainingProject;
            if (!project.Saved)
                project.Save();

            var sourceItemDirectory = Path.GetDirectoryName((string)sourceItem.Properties.Item("LocalPath").Value) ?? "";
            var projectDirectory = ((string)project.Properties.Item("LocalPath").Value).TrimEnd(Path.DirectorySeparatorChar);
            
            var relativePath = sourceItemDirectory.Substring(projectDirectory.Length).TrimStart(Path.DirectorySeparatorChar);
            var relativeNameSpace = GetNameSpace(relativePath);

            var projectNamespace = (string)project.Properties.Item("RootNamespace")?.Value;
            var defaultNameSpace = projectNamespace;

            if (!string.IsNullOrWhiteSpace(relativePath))
            {
                if (!string.IsNullOrWhiteSpace(defaultNameSpace))
                    defaultNameSpace += ".";
                defaultNameSpace += relativeNameSpace;
            }

            // validate options
            Progress(sourceItem.DTE, "Validate options...", 10);
            var options = VisualStudioHelper.GetOptions();
            options.Validate();

            // validate that we can talk to the server
            Progress(sourceItem.DTE, "Validate API server...", 15);
            var api = new ApiClient(options.UmbracoUrl, options.UmbracoUser, options.UmbracoPassword);
            api.ValidateClientVersion(); // so we get a meaningful error message first

            // load existing non-generated files
            Progress(sourceItem.DTE, "Load non-generated files...", 20);
            var ourFiles = Directory.GetFiles(sourceItemDirectory, "*.cs")
                .Where(x => !x.EndsWith(".generated.cs"))
                .ToDictionary(x => x, File.ReadAllText);

            // get new *.generated.cs files from server
            Progress(sourceItem.DTE, "Get models from server...", 25);
            var generatedFiles = api.GetModels(ourFiles, defaultNameSpace);

            // determine project type
            var isNetSdk = VisualStudioHelper.IsNetSdkProject(sourceItem.ContainingProject);

            // have to do things in different order
            // else for NetSdk weird things (can) happen in VS
            if (isNetSdk)
            {
                // delete existing *.generated.cs files from disk
                Progress(sourceItem.DTE, "Remove old generated files...", 50);
                foreach (var file in Directory.GetFiles(sourceItemDirectory, "*.generated.cs"))
                    File.Delete(file);

                // remove existing *.generated.cs files from project
                VisualStudioHelper.ClearExistingItems(sourceItem);

                // add new *.generated.cs files to project
                Progress(sourceItem.DTE, "Add new generated files...", 70);
                var files = new Dictionary<string, string>();
                foreach (var file in generatedFiles)
                {
                    var filename = Path.Combine(relativePath, file.Key + ".generated.cs");
                    files[filename] = file.Value;
                }
                VisualStudioHelper.AddGeneratedItems(sourceItem, projectDirectory, files.Keys);

                // with that pause here, it works
                // FIXME could we not pause, and wait for VS to detect the csproj changes?
                System.Threading.Thread.Sleep(1000);

                // save new *.generated.cs files to disk
                foreach (var file in files)
                {
                    File.WriteAllText(Path.Combine(projectDirectory, file.Key), file.Value);
                }
            }
            else
            {
                // remove existing *.generated.cs files from project
                Progress(sourceItem.DTE, "Remove old generated files...", 50);
                VisualStudioHelper.ClearExistingItems(sourceItem);

                // delete existing *.generated.cs files from disk
                foreach (var file in Directory.GetFiles(sourceItemDirectory, "*.generated.cs"))
                    File.Delete(file);

                // save new *.generated.cs files to disk
                Progress(sourceItem.DTE, "Add new generated files...", 70);
                var filenames = new List<string>();
                foreach (var file in generatedFiles)
                {
                    var filename = Path.Combine(relativePath, file.Key + ".generated.cs");
                    filenames.Add(filename);
                    File.WriteAllText(Path.Combine(projectDirectory, filename), file.Value);
                }

                // add new *.generated.cs files to project
                VisualStudioHelper.AddGeneratedItems(sourceItem, projectDirectory, filenames);
            }

            VisualStudioHelper.ReportMessage("Done.");
            Progress(sourceItem.DTE, "Done.", 100);
        }
    }
}
