using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Umbraco.ModelsBuilder.CustomTool.VisualStudio
{
    class VisualStudioHelper
    {
        // GetSourceItem was an endless source of confusion

        /*

        private static readonly string[] ExcludedProjectKinds =
        {
            EnvDTE.Constants.vsProjectKindSolutionItems.ToLowerInvariant(), // see [#49]
            "{E24C65DC-7377-472B-9ABA-BC803B73C61A}".ToLowerInvariant(), // see [#31]
        };

        private static string GetProjectName(EnvDTE.Project project)
        {
            try
            {
                return project.Name;
            }
            catch
            {
                return "(throws)";
            }
        }

        public static EnvDTE.ProjectItem GetSourceItem(string inputFilePath)
        {
            var dte = (EnvDTE.DTE)Package.GetGlobalService(typeof(EnvDTE.DTE));

            var pdwPriority = new VSDOCUMENTPRIORITY[1];
            uint itemId = 0;
            var vsProject = dte.Solution.Projects
                // process the project
                .Cast<EnvDTE.Project>()
                // exclude project types that are know to cause ToHierarchy to throw
                .Where(p =>
                {
                    var exclude = ExcludedProjectKinds.Contains(p.Kind.ToLowerInvariant());
                    if (!exclude) return true;

                    var msg = string.Format("Skipping project at \"{0}\" named \"{1}\" of kind \"{2}\" (excluded kind).",
                        p.FileName, GetProjectName(p), p.Kind);
                    ReportMessage(msg);
                    return false;
                })
                // exclude projet types that don't have a filename (ToHierarchy cannot work)
                .Where(p =>
                {
                    var exclude = string.IsNullOrWhiteSpace(p.FileName);
                    if (!exclude) return true;

                    var msg = string.Format("Skipping project at \"{0}\" named \"{1}\" of kind \"{2}\" (empty filename).",
                        p.FileName, GetProjectName(p), p.Kind);
                    ReportMessage(msg);
                    return false;
                })
                // try...catch ToHierarchy, in case it's a project type we should have excluded
                .Select(x =>
                {
                    try
                    {
                        return ToHierarchy(x);
                    }
                    catch (Exception e)
                    {
                        var errmsg = string.Format("Failed to process project at \"{0}\" named \"{1}\" of kind \"{2}\" (see inner exception).",
                            x.FileName, GetProjectName(x), x.Kind);

                        // what shall we do? throwing is not nice neither required, but it's the
                        // only way we can add project kinds to our exclude list... for the time
                        // being, be a pain to everybody and throw
                        throw new Exception(errmsg, e);
                        //ReportMessage(errmsg);
                        //return null;
                    }
                })
                // when ToHierachy has thrown, all we have is null
                .Where(x => x != null)
                // process the IVsProject
                .Cast<IVsProject>()
                .FirstOrDefault(x =>
                {
                    int iFound;
                    x.IsDocumentInProject(inputFilePath, out iFound, pdwPriority, out itemId);
                    return iFound != 0 && itemId != 0;
                });

            if (vsProject == null)
                throw new Exception("Panic: source file not found in any project.");

            Microsoft.VisualStudio.OLE.Interop.IServiceProvider oleSp;
            vsProject.GetItemContext(itemId, out oleSp);
            if (oleSp == null)
                throw new Exception("Panic: could not retrieve project item.");

            // convert handle to a ProjectItem
            var sp = new ServiceProvider(oleSp);
            var sourceItem = sp.GetService(typeof(EnvDTE.ProjectItem)) as EnvDTE.ProjectItem;
            if (sourceItem == null)
                throw new Exception("Panic: could not create source item.");


            return sourceItem;
        }
        */

        // https://github.com/dotnet/project-system/issues/1870
        // "in .NET Standard project I cannot create dependent upon file scenarios with AddFromFile"
        //
        // example: https://github.com/meision/Lever/commit/d59b5870ede8142a6073dde6bd0d5b47ea299c6f
        //
        // but https://developercommunity.visualstudio.com/content/problem/312523/envdteprojectkind-no-longer-differentiates-between.html
        // project.Kind does not indicate NetSdk anymore
        //
        // so https://github.com/Microsoft/VSProjectSystem/blob/master/doc/automation/detect_whether_a_project_is_a_CPS_project.md

        public static bool IsNetSdkProject(EnvDTE.ProjectItem sourceItem)
        {
            // see above, that does not work anymore
            //return sourceItem.ContainingProject.Kind == vsContextGuids.vsContextGuidNetSdkProject;

            var project = sourceItem.ContainingProject;
            var hierarchy = ToHierarchy(project);
            return hierarchy.IsCapabilityMatch("CPS");
        }

        public static XDocument GetProjectFile(EnvDTE.Project project)
        {
            string projectPath = System.IO.Path.Combine((string)project.Properties.Item("LocalPath").Value, (string)project.Properties.Item("FileName").Value);
            return XDocument.Load(projectPath);
        }

        public static void SaveProjectFile(EnvDTE.Project project, XDocument projectFile)
        {
            string projectPath = System.IO.Path.Combine((string)project.Properties.Item("LocalPath").Value, (string)project.Properties.Item("FileName").Value);
            projectFile.Save(projectPath);
        }

        public static void ClearExistingItems(EnvDTE.ProjectItem sourceItem)
        {
            if (IsNetSdkProject(sourceItem))
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

        public static void AddGeneratedItems(EnvDTE.ProjectItem sourceItem, string projectPath, IEnumerable<string> filenames)
        {
            if (IsNetSdkProject(sourceItem))
            {
                var sourceIdentity = (string)sourceItem.Properties.Item("Identity").Value;
                var dependentUpon = Path.GetFileName(sourceIdentity); // cannot be relative
                var projectFile = GetProjectFile(sourceItem.ContainingProject);

                var itemGroup = projectFile.XPathSelectElements($"//ItemGroup [@Label=\"DependentUpon:ModelsBuilder\"]").FirstOrDefault();
                if (itemGroup == null)
                {
                    itemGroup = XElement.Parse($"<ItemGroup Label=\"DependentUpon:ModelsBuilder\"></ItemGroup>");
                    projectFile.Root.Add(itemGroup);
                }

                foreach (var filename in filenames)
                {
                    var item = XElement.Parse($"<Compile Update=\"{filename}\"><DesignTime>True</DesignTime><AutoGen>True</AutoGen><DependentUpon>{dependentUpon}</DependentUpon></Compile>");
                    itemGroup.Add(item);
                }

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

        private static EnvDTE.DTE DTE => (EnvDTE.DTE)Package.GetGlobalService(typeof(EnvDTE.DTE));

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
