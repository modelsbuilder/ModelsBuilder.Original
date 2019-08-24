using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;
using Umbraco.Core;
using ZpqrtBnk.ModelsBuilder.Api;
using ZpqrtBnk.ModelsBuilder.Extension.VisualStudio;
using ZpqrtBnk.ModelsBuilder.Web.Api;
using Task = System.Threading.Tasks.Task;

namespace ZpqrtBnk.ModelsBuilder.Extension
{
    public class Generator
    {
        private readonly AsyncPackage _package;
        private readonly ProjectItem _sourceItem;
        private readonly DTE _dte;

        public Generator(AsyncPackage package, ProjectItem sourceItem)
        {
            _package = package;
            _sourceItem = sourceItem;
            _dte = _sourceItem.DTE;
        }

        public delegate void ProgressedHandler(string message, int percent);

        public event ProgressedHandler Progressed;

        private async Task ProgressAsync(string message, int percent)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            Progress(message, percent);
            await TaskScheduler.Default;
        }

        private void Progress(string message, int percent)
        {
            _dte.StatusBar.Progress(percent < 100, message, percent, 100);
            Progressed?.Invoke(message, percent);
        }

        public async Task GenerateAsync()
        {
            try
            {
                if (_sourceItem == null)
                    throw new ArgumentNullException(nameof(_sourceItem));

                await TryGenerateAsync(_sourceItem);
            }
            catch (Exception e)
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                HandleException(e);
            }
        }

        private void HandleException(Exception e)
        {
            var message = $"UmbracoModelsBuilder failed to generate code: {e.GetType().Name}: {e.Message}";

            //cannot do this within a running Task - ComObject exception
            //VisualStudioHelper.ReportError(pGenerateProgress, message);
            VisualStudioHelper.ReportMessage(message);
            VisualStudioHelper.ReportMessage(e.StackTrace);

            var inner = e.InnerException;
            while (inner != null)
            {
                message = $"Inner: {inner.GetType().Name}: {inner.Message}";
                VisualStudioHelper.ReportMessage(message);
                VisualStudioHelper.ReportMessage(inner.StackTrace);
                inner = inner.InnerException;
            }

            if (e is AggregateException aggr)
                foreach (var aggrInner in aggr.Flatten().InnerExceptions)
                {
                    message = $"AggregateInner: {aggrInner.GetType().Name}: {aggrInner.Message}";
                    VisualStudioHelper.ReportMessage(message);
                    VisualStudioHelper.ReportMessage(aggrInner.StackTrace);
                }

            Progress("Failed.", 100);

            // Show a message box to prove we were here
            VsShellUtilities.ShowMessageBox(
                _package,
                "See exception details in the General pane of the Output windows.",
                "Failed to build models",
                OLEMSGICON.OLEMSGICON_CRITICAL,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }

        private static string GetDefaultNameSpace(string projectNamespace, string relativePath)
        {
            var parts = relativePath.Split(new[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < parts.Length; i++)
            {
                parts[i] = Building.Compiler.CreateValidIdentifier(parts[i]);
            }
            var relativeNameSpace = string.Join(".", parts);

            var defaultNameSpace = projectNamespace;

            if (!string.IsNullOrWhiteSpace(relativePath))
            {
                if (!string.IsNullOrWhiteSpace(defaultNameSpace))
                    defaultNameSpace += ".";
                defaultNameSpace += relativeNameSpace;
            }

            return defaultNameSpace;
        }

        public async Task TryGenerateAsync(ProjectItem sourceItem)
        {
            await ProgressAsync("Build Models...", 0);
            VisualStudioHelper.ReportMessage("Starting v{0} {1}.", ApiVersion.Current.Version, DateTime.Now);

            // save project before modifying it
            await ProgressAsync("Save project...", 5);
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var project = sourceItem.ContainingProject;
            if (!project.Saved)
                project.Save();

            // determine project type
            var isNetSdk = VisualStudioHelper.IsNetSdkProject(sourceItem.ContainingProject);

            // get directories
            var sourceItemDirectory = Path.GetDirectoryName((string)sourceItem.Properties.Item("LocalPath").Value) ?? "";
            var projectDirectory = ((string)project.Properties.Item("LocalPath").Value).TrimEnd(Path.DirectorySeparatorChar);
            
            // get namespace
            var relativePath = sourceItemDirectory.Substring(projectDirectory.Length).TrimStart(Path.DirectorySeparatorChar);
            var projectNamespace = (string)project.Properties.Item("RootNamespace")?.Value;
            var defaultNameSpace = GetDefaultNameSpace(projectNamespace, relativePath);
            
            // validate options
            await ProgressAsync("Validate options...", 10);
            var options = VisualStudioHelper.GetOptions();
            options.Validate();

            // back to background
            await TaskScheduler.Default;

            // validate that we can talk to the server
            await ProgressAsync("Validate API server...", 15);
            var api = new ApiClient(options.UmbracoUrl, options.UmbracoUser, options.UmbracoPassword);
            api.ValidateClientVersion(); // so we get a meaningful error message first

            // load existing non-generated files
            await ProgressAsync("Load non-generated files...", 20);
            var allFiles = Directory.GetFiles(sourceItemDirectory, "*.cs");
            var ourFiles = allFiles
                .Where(x => !x.EndsWith(".generated.cs"))
                .ToDictionary(x => x, File.ReadAllText);

            // get new *.generated.cs files from server
            await ProgressAsync("Get models from server...", 25);
            var generatedFiles = api.GetModels(ourFiles, defaultNameSpace);

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
                await ProgressAsync("Delete old generated files...", 50);
                foreach (var file in removeGeneratedFiles)
                    File.Delete(file);

                // remove existing *.generated.cs files from project
                await ProgressAsync("Remove old generated files from project...", 55);
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                VisualStudioHelper.ClearGeneratedItems(sourceItem, keepGeneratedFiles);
                await TaskScheduler.Default;

                // add new *.generated.cs files to project
                await ProgressAsync("Add new generated files to project...", 70);
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
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                VisualStudioHelper.AddGeneratedItems(sourceItem, projectDirectory, relFilenames);
                await TaskScheduler.Default;

                // VS must reload the project *before* we create the files, else there's a conflict
                // with that pause here, it works
                // FIXME could we not pause, and wait for VS to detect the csproj changes?
                System.Threading.Thread.Sleep(1000);

                // save new *.generated.cs files to disk
                await ProgressAsync("Write new generated files...", 80);
                foreach (var (path, text) in filesToWrite)
                {
                    File.WriteAllText(path, text);
                }
            }
            else
            {
                // remove existing *.generated.cs files from project
                // (but not those that are simply overwritten)
                await ProgressAsync("Remove old generated files from project...", 50);
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                VisualStudioHelper.ClearGeneratedItems(sourceItem, keepGeneratedFiles);
                await TaskScheduler.Default;

                // delete existing *.generated.cs files from disk
                // (but not those that are simply overwritten)
                await ProgressAsync("Delete old generated files...", 55);
                foreach (var file in removeGeneratedFiles)
                    File.Delete(file);

                // save new *.generated.cs files to disk
                await ProgressAsync("Write new generated files...", 70);
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
                await ProgressAsync("Add new generated files to project...", 75);
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                VisualStudioHelper.AddGeneratedItems(sourceItem, projectDirectory, relFilenames);
                await TaskScheduler.Default;
            }

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            VisualStudioHelper.ReportMessage("Done.");
            await TaskScheduler.Default;

            await ProgressAsync("Done.", 100);
        }
    }
}
