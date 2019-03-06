using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Design.Serialization;
using Microsoft.VisualStudio.Shell.Interop;
using Umbraco.ModelsBuilder.CustomTool.CustomTool;

namespace Umbraco.ModelsBuilder.CustomTool
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    ///
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the 
    /// IVsPackage interface and uses the registration attributes defined in the framework to 
    /// register itself and its components with the shell.
    /// </summary>

    // tells CreatePkgDef.exe utility that this class is a package
    [PackageRegistration(UseManagedResourcesOnly = true)]

    // load when a solution exists - not
    //[ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExistsAndFullyLoaded_string)]

    // tells VS that we have an options dialog
    [ProvideOptionPage(typeof(OptionsDialog), VisualStudioOptions.OptionsCategory, VisualStudioOptions.OptionsPageName, 0, 0, true)]
    
    // register infos needed to show this package in the Help/About dialog of VS
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [Guid(GuidList.PkgString)]
    [DefaultRegistryRoot("Software\\Microsoft\\VisualStudio\\11.0")]

    // register the generator
    [ProvideObject(typeof(UmbracoCSharpModelsBuilder))]
    [ProvideGenerator(typeof(UmbracoCSharpModelsBuilder), "UmbracoModelsBuilder", "Umbraco ModelsBuilder Custom Tool for C#", "{FAE04EC1-301F-11D3-BF4B-00C04F79EFBC}", true)] // csharp

    public sealed class UmbracoModelsBuilder : Package, IVsSolutionEvents
    {
        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require 
        /// any Visual Studio service because at this point the package object is created but 
        /// not sited yet inside Visual Studio environment. The place to do all the other 
        /// initialization is the Initialize method.
        /// </summary>
        public UmbracoModelsBuilder()
        {
            Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString()));
        }

        //OptionsDialog Options
        //    => GetDialogPage(typeof(OptionsDialog)) as OptionsDialog;

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            Debug.WriteLine (string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this.ToString()));
            base.Initialize();
        }

        #region IVsSolutionEvents

        public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
            VisualStudioOptions.Instance.Reload();
            return VSConstants.S_OK;
        }

        public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeCloseSolution(object pUnkReserved)
        {
            // don't leave settings around
            VisualStudioOptions.Instance.Clear();
            return VSConstants.S_OK;
        }

        public int OnAfterCloseSolution(object pUnkReserved)
        {
            return VSConstants.S_OK;
        }

        #endregion
    }
}
