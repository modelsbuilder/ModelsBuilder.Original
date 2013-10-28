using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
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
                existingItem.Remove();
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
    }
}
