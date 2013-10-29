using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Zbu.ModelsBuilder.CustomTool.VisualStudio
{
    class VisualStudioHelper
    {
        public static EnvDTE.ProjectItem GetSourceItem(string inputFilePath)
        {
            var dte = (EnvDTE.DTE)Package.GetGlobalService(typeof(EnvDTE.DTE));
            var dteProjects = (Array)dte.ActiveSolutionProjects;
            if (dteProjects.Length <= 0)
                throw new Exception("Panic: no projets.");

            var dteProject = (EnvDTE.Project)dteProjects.GetValue(0);

            var pdwPriority = new VSDOCUMENTPRIORITY[1];

            // obtain a reference to the current project as an IVsProject type
            var vsProject = ToHierarchy(dteProject) as IVsProject;
            if (vsProject == null)
                throw new Exception("Panic: vsProject is null.");

            // locates, and returns a handle to source file, as a ProjectItem
            int iFound;
            uint itemId;
            vsProject.IsDocumentInProject(inputFilePath, out iFound, pdwPriority, out itemId);
            if (iFound == 0 || itemId == 0)
                throw new Exception("Panic: source file not found in project.");

            Microsoft.VisualStudio.OLE.Interop.IServiceProvider oleSp = null;
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

        private static IVsHierarchy ToHierarchy(EnvDTE.Project project)
        {
            if (project == null)
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
            const string format = "\nZbuModelsBuilder: {0}";
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

        public static Options GetOptions()
        {
            var dte = (EnvDTE.DTE)Package.GetGlobalService(typeof(EnvDTE.DTE));
            var properties = dte.get_Properties(VisualStudioOptions.OptionsCategory, VisualStudioOptions.OptionsPageName);
            return new Options(properties);
        }

        public class Options
        {
            private readonly EnvDTE.Properties _properties;

            public Options(EnvDTE.Properties properties)
            {
                _properties = properties;
            }

            public string ConnectionString { get { return (string)_properties.Item("ConnectionString").Value; } }
            public string DatabaseProvider { get { return (string)_properties.Item("DatabaseProvider").Value; } }

            public void Validate()
            {
                var valid = true;
                valid &= !string.IsNullOrWhiteSpace(ConnectionString);
                valid &= !string.IsNullOrWhiteSpace(DatabaseProvider);

                if (!valid)
                    throw new Exception("Invalid configuration.");
            }
        }
    }
}
