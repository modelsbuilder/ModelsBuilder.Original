using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.XPath;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace ZpqrtBnk.ModelsBuilder.Extension.VisualStudio
{
    internal class VisualStudioHelper
    {
        // https://github.com/dotnet/project-system/issues/1870
        // "in .NET Standard project I cannot create dependent upon file scenarios with AddFromFile"
        //
        // example: https://github.com/meision/Lever/commit/d59b5870ede8142a6073dde6bd0d5b47ea299c6f
        //
        // but https://developercommunity.visualstudio.com/content/problem/312523/envdteprojectkind-no-longer-differentiates-between.html
        // project.Kind does not indicate NetSdk anymore
        //
        // so https://github.com/Microsoft/VSProjectSystem/blob/master/doc/automation/detect_whether_a_project_is_a_CPS_project.md

        public static bool IsNetSdkProject(EnvDTE.Project project)
        {
            // see above, that does not work anymore
            //return sourceItem.ContainingProject.Kind == vsContextGuids.vsContextGuidNetSdkProject;

            var hierarchy = ToHierarchy(project);
            return hierarchy.IsCapabilityMatch("CPS");
        }

        public static XDocument GetProjectFile(EnvDTE.Project project)
        {
            return XDocument.Load(GetProjectFilePath(project));
        }

        public static void SaveProjectFile(EnvDTE.Project project, XDocument projectFile)
        {
            var projectPath = Path.Combine(GetProjectDirectory(project), GetProjectFileName(project));
            projectFile.Save(GetProjectFilePath(project));
        }

        /*
        public static void ClearExistingItems(EnvDTE.ProjectItem sourceItem)
        {
            if (IsNetSdkProject(sourceItem.ContainingProject))
            {
                var sourceIdentity = (string)sourceItem.Properties.Item("Identity").Value;
                var sourceDirectory = Path.GetDirectoryName(sourceIdentity) + Path.DirectorySeparatorChar;
                var dependentUpon = Path.GetFileName(sourceIdentity); // cannot be relative
                var projectFile = GetProjectFile(sourceItem.ContainingProject);

                var items = projectFile.XPathSelectElements($"//ItemGroup/Compile [@Update [starts-with(.,\"{sourceDirectory}\")] and DependentUpon=\"{dependentUpon}\"]");
                foreach (var item in items.ToList()) // ToList is important! else only the first one is actually removed!
                    item.Remove();

                SaveProjectFile(sourceItem.ContainingProject, projectFile);
            }
            else
            {
                foreach (EnvDTE.ProjectItem existingItem in sourceItem.ProjectItems)
                    existingItem.Remove(); // or, there's .Delete() ?
            }
        }
        */

        /// <summary>
        /// Gets the full path of the directory containing the project file (the csproj).
        /// </summary>
        private static string GetProjectDirectory(Project project)
            => (string)project.Properties.Item("LocalPath").Value;

        /// <summary>
        /// Gets the project file name (just the csproj name).
        /// </summary>
        private static string GetProjectFileName(Project project)
            => (string)project.Properties.Item("FileName").Value;

        /// <summary>
        /// Gets the full path of the project file (the csproj).
        /// </summary>
        /// <returns></returns>
        private static string GetProjectFilePath(Project project)
            => Path.Combine(GetProjectDirectory(project), GetProjectFileName(project));

        /// <summary>
        /// Gets the full path of the project item (including file name).
        /// </summary>
        private static string GetItemFullPath(ProjectItem item)
            => (string)item.Properties.Item("FullPath").Value;
        
        public static void ClearGeneratedItems(EnvDTE.ProjectItem sourceItem, List<string> preserve)
        {
            var projectDirectory = GetProjectDirectory(sourceItem.ContainingProject);

            if (IsNetSdkProject(sourceItem.ContainingProject))
            {
                var sourcePath = GetItemFullPath(sourceItem);
                var sourceDirectory = Path.GetDirectoryName(sourcePath).Substring(projectDirectory.Length) + Path.DirectorySeparatorChar;
                var dependentUpon = Path.GetFileName(sourcePath); // cannot be relative
                var projectFile = GetProjectFile(sourceItem.ContainingProject);

                var edited = false;
                var items = projectFile.XPathSelectElements($"//ItemGroup/Compile [@Update [starts-with(.,\"{sourceDirectory}\")] and DependentUpon=\"{dependentUpon}\"]");
                foreach (var item in items.ToList()) // ToList is important! else only the first one is actually removed!
                {
                    var relativeFilename = item.Attribute("Update").Value;
                    if (preserve.Contains(relativeFilename)) continue;
                    item.Remove();
                    edited = true;
                }

                if (edited)
                    SaveProjectFile(sourceItem.ContainingProject, projectFile);
            }
            else
            {
                foreach (ProjectItem existingItem in sourceItem.ProjectItems)
                {
                    var relativePath = GetItemFullPath(existingItem).Substring(projectDirectory.Length);
                    if (preserve.Contains(relativePath)) continue;
                    existingItem.Remove(); // or, there's .Delete() ?
                }
            }
        }

        public static void AddGeneratedItems(EnvDTE.ProjectItem sourceItem, string projectPath, IEnumerable<string> filenames)
        {
            if (IsNetSdkProject(sourceItem.ContainingProject))
            {
                var sourcePath = GetItemFullPath(sourceItem);
                var dependentUpon = Path.GetFileName(sourcePath); // cannot be relative
                var projectFile = GetProjectFile(sourceItem.ContainingProject);

                var itemGroup = projectFile.XPathSelectElements($"//ItemGroup [@Label=\"DependentUpon:ModelsBuilder\"]").FirstOrDefault();
                if (itemGroup == null)
                {
                    itemGroup = XElement.Parse($"<ItemGroup Label=\"DependentUpon:ModelsBuilder\"></ItemGroup>");
                    projectFile.Root.Add(itemGroup);
                }

                var edited = false;
                foreach (var filename in filenames)
                {
                    var item = XElement.Parse($"<Compile Update=\"{filename}\"><DesignTime>True</DesignTime><AutoGen>True</AutoGen><DependentUpon>{dependentUpon}</DependentUpon></Compile>");
                    itemGroup.Add(item);
                    edited = true;
                }

                if (edited)
                    SaveProjectFile(sourceItem.ContainingProject, projectFile);
            }
            else
            {
                foreach (var filename in filenames)
                {
                    var newItem = sourceItem.ProjectItems.AddFromFile(Path.Combine(projectPath, filename));
                    // build actions
                    // 0 - none
                    // 1 - compile
                    // 2 - content
                    // 3 - embedded resources
                    // ?
                    newItem.Properties.Item("BuildAction").Value = 1;
                }
            }
        }

        public static IVsSolution GetSolution(EnvDTE.Project project)
        {
            var provider = project.DTE as Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
            var solution = provider.QueryService<SVsSolution>() as IVsSolution;
            return solution;
        }

        private static IVsHierarchy ToHierarchy(EnvDTE.Project project)
        {
            if (project == null || string.IsNullOrWhiteSpace(project.FileName))
                throw new ArgumentNullException("project");

            var solution = GetSolution(project);
            if (solution.GetProjectOfUniqueName(project.UniqueName, out var hierarchy) == 0)
                return hierarchy;

            throw new Exception("Panic: failed to get hierarchy.");
        }

        /*
        // see http://msdn.microsoft.com/fr-fr/library/microsoft.visualstudio.shell.interop.ivsgeneratorprogress.generatorerror%28v=vs.90%29.aspx
        // level is ignored, line should be -1 if not specified

        public static void ReportError(IVsGeneratorProgress progress, string message, uint line = 0xFFFFFFFF, uint column = 0xFFFFFFFF)
        {
            progress?.GeneratorError(0, 0, message, line, column);
        }

        public static void ReportWarning(IVsGeneratorProgress progress, string message, uint line = 0xFFFFFFFF, uint column = 0xFFFFFFFF)
        {
            progress?.GeneratorError(1, 0, message, line, column);
        }

        // see http://stackoverflow.com/questions/16443331/visual-studio-vspackage-single-file-generator-log-message-to-error-list
        // there's no equivalent to GenerateError above for just reporting stuff...
        // see http://msdn.microsoft.com/en-us/library/bb187346%28v=VS.80%29.aspx
        public static void ReportMessage(string message)
        {
            // fixme - should we cache that somewhere? that class should not be static in fact!
            // fixme - NOT writing to the output window at the moment...

            var output = Package.GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;
            if (output == null)
                return;

            // note
            // in vs2010 the general pane is not there by default
            var guidGeneral = VSConstants.OutputWindowPaneGuid.GeneralPane_guid;
            if (output.GetPane(guidGeneral, out var pane) != VSConstants.S_OK || pane == null)
            {
                output.CreatePane(guidGeneral, "General", 1, 0); // should we create our own?
                if (output.GetPane(guidGeneral, out pane) != VSConstants.S_OK || pane == null)
                    return;
            }
            pane.Activate();

            //// can't write to the output window - just a pane
            //// see References.Services HelperMethodsClass SDK Sample
            //var generalPaneGuid = VSConstants.GUID_OutWindowGeneralPane; // eg GitExtensions create its own pane...
            //IVsOutputWindowPane pane;

            //// fetch the pane wrapped in error handling:
            //// ideally, throw an exception or trace/output...
            //if (ErrorHandler.Failed(output.GetPane(ref generalPaneGuid, out pane)) || pane == null)
            //    return;

            // prepare the message for output:
            const string format = "\nUmbracoModelsBuilder: {0}";
            var text = string.Format(format, message);

            // wrap attempts to write in an error handler:
            if (ErrorHandler.Failed(pane.OutputString(text)))
            {
                // throw an exception/etc. if it fails?
            }
        }

        public static void ReportMessage(string format, params object[] args)
        {
            ReportMessage(string.Format(format, args));
        }
        */

        private static EnvDTE.DTE DTE => (EnvDTE.DTE)Package.GetGlobalService(typeof(EnvDTE.DTE));

        /*
        private static IVsStatusbar StatusBar
        {
            get
            {
                var dte = DTE;
                IServiceProvider serviceProvider = new ServiceProvider(dte as Microsoft.VisualStudio.OLE.Interop.IServiceProvider);
                return serviceProvider.GetService(typeof(SVsStatusbar)) as IVsStatusbar;
            }
        }

        public static void SetStatus(string msg)
        {
            // http://geekswithblogs.net/onlyutkarsh/archive/2013/08/11/using-visual-studio-status-bar-in-your-extensions.aspx
            var bar = StatusBar;
            bar.IsFrozen(out var frozen);
            if (frozen != 0) return;
            bar.SetText("Dim da da...");
            object icon = (short) Microsoft.VisualStudio.Shell.Interop.Constants.SBAI_Synch;
            bar.Animation(1, ref icon);

            // could we have also a "long running task" popup?
        }

        public static void ReleaseStatus()
        {
            var bar = StatusBar;
            bar.FreezeOutput(0);
            bar.Clear();
        }
        */

        public static string GetSolution()
        {
            var dte = DTE;
            return dte.Solution.FullName;
        }

        public static VisualStudioOptions GetOptions()
        {
            // reload - in case file has been modified
            var options = VisualStudioOptions.Instance;
            options.Reload();
            return options;

            //var dte = DTE;
            //var properties = dte.Properties[OptionsDialog.OptionsCategory, OptionsDialog.OptionsPageName];
            //return new Options(properties);
        }

        public static bool HasSolution
        {
            get
            {
                var dte = DTE;
                return dte.Solution.IsOpen;
            }
        }

        public class Options
        {
            private readonly EnvDTE.Properties _properties;

            public Options(EnvDTE.Properties properties)
            {
                _properties = properties;
            }

            public string UmbracoUrl => (string)_properties.Item("UmbracoUrl").Value;
            public string UmbracoUser => (string)_properties.Item("UmbracoUser").Value;
            public string UmbracoPassword => (string)_properties.Item("UmbracoPassword").Value;

            public void Validate()
            {
                StringBuilder message = null;

                var empty = new List<string>();
                if (string.IsNullOrWhiteSpace(UmbracoUrl))
                    empty.Add("Site Url");
                if (string.IsNullOrWhiteSpace(UmbracoUser))
                    empty.Add("User Name");
                if (string.IsNullOrWhiteSpace(UmbracoPassword))
                    empty.Add("User Password");

                if (empty.Count > 0)
                {
                    message = new StringBuilder("Invalid configuration. ");
                    for (var i = 0; i < empty.Count; i++)
                    {
                        if (i > 0)
                            message.Append(", ");
                        message.Append(empty[i]);
                    }
                    message.Append(" cannot be empty.");
                }

                try
                {
                    _ = new Uri(UmbracoUrl);
                }
                catch
                {
                    if (message == null)
                        message = new StringBuilder("Invalid configuration. Site Url \"");
                    else
                        message.Append(" Site Url \"");
                    message.Append(UmbracoUrl);
                    message.Append("\" is not a valid Uri.");
                }

                if (message != null)
                    throw new Exception(message.ToString());
            }
        }

        public static ProjectItem GetProjectItem(DTE2 dte)
        {
            Window2 window = dte.ActiveWindow as Window2;

            if (window == null)
                return null;

            if (window.Type == vsWindowType.vsWindowTypeDocument)
            {
                Document doc = dte.ActiveDocument;

                if (doc != null && !string.IsNullOrEmpty(doc.FullName))
                {
                    return dte.Solution.FindProjectItem(doc.FullName);
                }
            }

            return GetSelectedItems(dte).FirstOrDefault();
        }

        public static IEnumerable<ProjectItem> GetSelectedItems(DTE2 dte)
        {
            var items = (Array)dte.ToolWindows.SolutionExplorer.SelectedItems;

            foreach (UIHierarchyItem selItem in items)
            {
                ProjectItem item = selItem.Object as ProjectItem;

                if (item != null)
                    yield return item;
            }
        }
    }
}
