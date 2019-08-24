using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Umbraco.Core;
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
            var allFiles = Directory.GetFiles(sourceItemDirectory, "*.cs");
            var ourFiles = allFiles
                .Where(x => !x.EndsWith(".generated.cs"))
                .ToDictionary(x => x, File.ReadAllText);

            // get new *.generated.cs files from server
            Progress(sourceItem.DTE, "Get models from server...", 25);
            var generatedFiles = api.GetModels(ourFiles, defaultNameSpace);

            // determine project type
            var isNetSdk = VisualStudioHelper.IsNetSdkProject(sourceItem.ContainingProject);

            // prepare file lists
            var oldGeneratedFiles = allFiles.Where(x => x.EndsWith(".generated.cs")).ToList();
            var newGeneratedFiles = new List<string>(); // full path of new files
            var keepGeneratedFiles = new List<string>(); // relative path of files to keep

            foreach (var filename in generatedFiles.Keys)
            {
                var relative = Path.Combine(relativePath, filename + ".generated.cs");
                var full = Path.Combine(projectDirectory, relative);

                if (oldGeneratedFiles.Contains(full))
                    keepGeneratedFiles.Add(relative);

                newGeneratedFiles.Add(full);
            }

            // full path of files to delete
            var removeGeneratedFiles = oldGeneratedFiles.Except(newGeneratedFiles).ToList();

            // have to do things in different order
            // else for NetSdk weird things (can) happen in VS
            if (isNetSdk)
            {
                // delete existing *.generated.cs files from disk
                Progress(sourceItem.DTE, "Delete old generated files...", 50);
                foreach (var file in removeGeneratedFiles)
                    File.Delete(file);

                // remove existing *.generated.cs files from project
                Progress(sourceItem.DTE, "Remove old generated files from project...", 55);
                VisualStudioHelper.ClearGeneratedItems(sourceItem, keepGeneratedFiles);

                // add new *.generated.cs files to project
                Progress(sourceItem.DTE, "Add new generated files to project...", 70);
                var relFilenames = new List<string>(); // relative file names to add
                var filesToWrite = new Dictionary<string, string>(); // files to write to disk
                foreach (var (filename, text) in generatedFiles)
                {
                    var relFilename = Path.Combine(relativePath, filename + ".generated.cs");
                    var fulFilename = Path.Combine(projectDirectory, relFilename);
                    if (!oldGeneratedFiles.Contains(fulFilename))
                        relFilenames.Add(relFilename);
                    filesToWrite[fulFilename] = text;
                }
                VisualStudioHelper.AddGeneratedItems(sourceItem, projectDirectory, relFilenames);

                // VS must reload the project *before* we create the files, else there's a conflict
                // with that pause here, it works
                // FIXME could we not pause, and wait for VS to detect the csproj changes?
                System.Threading.Thread.Sleep(1000);

                // save new *.generated.cs files to disk
                Progress(sourceItem.DTE, "Write new generated files...", 80);
                foreach (var (path, text) in filesToWrite)
                {
                    File.WriteAllText(path, text);
                }
            }
            else
            {
                // remove existing *.generated.cs files from project
                // (but not those that are simply overwritten)
                Progress(sourceItem.DTE, "Remove old generated files from project...", 50);
                VisualStudioHelper.ClearGeneratedItems(sourceItem, keepGeneratedFiles);

                // delete existing *.generated.cs files from disk
                // (but not those that are simply overwritten)
                Progress(sourceItem.DTE, "Delete old generated files...", 55);
                foreach (var file in removeGeneratedFiles)
                    File.Delete(file);

                // save new *.generated.cs files to disk
                Progress(sourceItem.DTE, "Write new generated files...", 70);
                var relFilenames = new List<string>(); // relative file names to add
                foreach (var (filename, text) in generatedFiles)
                {
                    var relFilename = Path.Combine(relativePath, filename + ".generated.cs");
                    var fulFilename = Path.Combine(projectDirectory, relFilename);
                    if (!oldGeneratedFiles.Contains(fulFilename))
                        relFilenames.Add(relFilename);
                    File.WriteAllText(fulFilename, text);
                }

                // add new *.generated.cs files to project
                // (those that are really new, not overwritten)
                Progress(sourceItem.DTE, "Add new generated files to project...", 75);
                VisualStudioHelper.AddGeneratedItems(sourceItem, projectDirectory, relFilenames);
            }

            VisualStudioHelper.ReportMessage("Done.");
            Progress(sourceItem.DTE, "Done.", 100);
        }
    }
}
