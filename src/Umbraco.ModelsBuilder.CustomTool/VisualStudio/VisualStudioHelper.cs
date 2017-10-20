using System;
using System.Collections.Generic;
using System.Text;
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

        public static void ClearExistingItems(EnvDTE.ProjectItem sourceItem)
        {
            foreach (EnvDTE.ProjectItem existingItem in sourceItem.ProjectItems)
                existingItem.Remove(); // or, there's .Delete() ?
        }

        public static void AddGeneratedItem(EnvDTE.ProjectItem sourceItem, string filename)
        {
            var newItem = sourceItem.ProjectItems.AddFromFile(filename);
            // build actions
            // 0 - none
            // 1 - compile
            // 2 - content
            // 3 - embedded resources
            // ?
            newItem.Properties.Item("BuildAction").Value = 1;
        }

        /*
        private static IVsHierarchy ToHierarchy(EnvDTE.Project project)
        {
            if (project == null || string.IsNullOrWhiteSpace(project.FileName))
                throw new ArgumentNullException("project");

            string projectGuid = null;

            // DTE does not expose the project GUID that exists at in the msbuild project file.
            // Cannot use MSBuild object model because it uses a static instance of the Engine,
            // and using the Project will cause it to be unloaded from the engine when the
            // GC collects the variable that we declare.
            using (var projectReader = XmlReader.Create(project.FileName))
            {
                projectReader.MoveToContent();

                if (projectReader.NameTable == null)
                    throw new Exception("Panic: projectReader.NameTable is null.");

                object nodeName = projectReader.NameTable.Add("ProjectGuid");
                while (projectReader.Read())
                {
                    if (!Equals(projectReader.LocalName, nodeName)) continue;

                    projectGuid = projectReader.ReadElementContentAsString();
                    break;
                }
            }
            if (projectGuid == null)
                throw new Exception("Panic: projectGuid is null.");

            IServiceProvider serviceProvider = new ServiceProvider(project.DTE as Microsoft.VisualStudio.OLE.Interop.IServiceProvider);
            return VsShellUtilities.GetHierarchy(serviceProvider, new Guid(projectGuid));
        }
        */

        // see http://msdn.microsoft.com/fr-fr/library/microsoft.visualstudio.shell.interop.ivsgeneratorprogress.generatorerror%28v=vs.90%29.aspx
        // level is ignored, line should be -1 if not specified

        public static void ReportError(IVsGeneratorProgress progress, string message, uint line = 0xFFFFFFFF, uint column = 0xFFFFFFFF)
        {
            if (progress != null)
                progress.GeneratorError(0, 0, message, line, column);
        }

        public static void ReportWarning(IVsGeneratorProgress progress, string message, uint line = 0xFFFFFFFF, uint column = 0xFFFFFFFF)
        {
            if (progress != null)
                progress.GeneratorError(1, 0, message, line, column);
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
            Guid guidGeneral = VSConstants.OutputWindowPaneGuid.GeneralPane_guid;
            IVsOutputWindowPane pane;
            if (output.GetPane(guidGeneral, out pane) != VSConstants.S_OK || pane == null)
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

        private static IVsStatusbar StatusBar
        {
            get
            {
                var dte = (EnvDTE.DTE)Package.GetGlobalService(typeof(EnvDTE.DTE));
                IServiceProvider serviceProvider = new ServiceProvider(dte as Microsoft.VisualStudio.OLE.Interop.IServiceProvider);
                return serviceProvider.GetService(typeof(SVsStatusbar)) as IVsStatusbar;
            }
        }

        public static void SetStatus(string msg)
        {
            // http://geekswithblogs.net/onlyutkarsh/archive/2013/08/11/using-visual-studio-status-bar-in-your-extensions.aspx
            var bar = StatusBar;
            int frozen;
            bar.IsFrozen(out frozen);
            if (frozen != 0) return;
            bar.SetText("Dim da da...");
            object icon = (short) Constants.SBAI_Synch;
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
            var dte = (EnvDTE.DTE)Package.GetGlobalService(typeof(EnvDTE.DTE));
            return dte.Solution.FullName;
        }

        public static Options GetOptions()
        {
            var dte = (EnvDTE.DTE)Package.GetGlobalService(typeof(EnvDTE.DTE));
            var properties = dte.Properties[VisualStudioOptions.OptionsCategory, VisualStudioOptions.OptionsPageName];
            return new Options(properties);
        }

        public class Options
        {
            private readonly EnvDTE.Properties _properties;

            public Options(EnvDTE.Properties properties)
            {
                _properties = properties;
            }

            //public string ConnectionString { get { return (string)_properties.Item("ConnectionString").Value; } }
            //public string DatabaseProvider { get { return (string)_properties.Item("DatabaseProvider").Value; } }
            //public string BinaryDirectory { get { return (string)_properties.Item("BinaryDirectory").Value; } }
            public string UmbracoUrl { get { return (string)_properties.Item("UmbracoUrl").Value; } }
            public string UmbracoUser { get { return (string)_properties.Item("UmbracoUser").Value; } }
            public string UmbracoPassword { get { return (string)_properties.Item("UmbracoPassword").Value; } }

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
                            message.Append(i < empty.Count - 1 ? ", " : "nor ");
                        message.Append(empty[i]);
                    }
                    message.Append(" cannot be empty.");
                }

                try
                {
                    var uri = new Uri(UmbracoUrl);
                }
                catch
                {
                    if (message == null) message = new StringBuilder("Invalid configuration. Site Url \"");
                    message.Append(UmbracoUrl);
                    message.Append("\" is not a valid Uri.");
                }

                if (message != null)
                    throw new Exception(message.ToString());
            }
        }

        public static void PumpAction(string title, string text, Action action)
        {
            // see http://stackoverflow.com/questions/13457948/how-to-display-waiting-popup-from-visual-studio-extension

            var pump = new CommonMessagePump
            {
                AllowCancel = false,
                EnableRealProgress = false,
                WaitTitle = title,
                WaitText = text
            };

            //var task = PumpActionStaTask(action);
            var task = System.Threading.Tasks.Task.Run(action);

            // ignore exit code - we can't cancel, anything - have to wait to the task anyway...
            pump.ModalWaitForHandles(((IAsyncResult)task).AsyncWaitHandle);

            task.Wait();

            // this is debugging code...
            // details go to the output window anyway

            //try
            //{
            //    task.Wait();
            //}
            //catch (Exception e)
            //{
            //    // COM exception while GenerateRaw tries to log an error to VisualStudio
            //    // wtf is that?!

            //    MessageBox.Show(e.Message, "Error");

            //    var aggr = e as AggregateException;
            //    if (aggr != null)
            //        foreach (var aggrInner in aggr.Flatten().InnerExceptions)
            //        {
            //            var message = string.Format("AggregateInner: {0}: {1}\r\n{2}", aggrInner.GetType().Name, aggrInner.Message,
            //                aggrInner.StackTrace);
            //            MessageBox.Show(message, "Error");
            //        }

            //    throw;
            //}
        }

        // no used
        //private static System.Threading.Tasks.Task PumpActionStaTask(Action action)
        //{
        //    var tcs = new TaskCompletionSource<bool>();
        //    var thread = new Thread(() =>
        //    {
        //        try
        //        {
        //            action();
        //            tcs.SetResult(true); // anything, really
        //        }
        //        catch (Exception e)
        //        {
        //            tcs.SetException(e);
        //        }
        //    });
        //    thread.SetApartmentState(ApartmentState.STA);
        //    thread.Start();
        //    return tcs.Task;
        //}
    }
}
