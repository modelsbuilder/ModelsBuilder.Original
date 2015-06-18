using System;
using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using VSOLE = Microsoft.VisualStudio.OLE.Interop;

namespace Zbu.ModelsBuilder.CustomTool.CustomTool
{
    // note: see https://github.com/RazorGenerator/RazorGenerator

    [ComVisible(true)]
    public abstract class BaseCodeGeneratorWithSite : BaseCodeGenerator, VSOLE.IObjectWithSite
    {
        private object _site;
        private ServiceProvider _serviceProvider;

        #region IObjectWithSite Members


        void VSOLE.IObjectWithSite.GetSite(ref Guid riid, out IntPtr ppvSite)
        {
            if (_site == null)
                throw new COMException("object is not sited", VSConstants.E_FAIL);

            var pUnknownPointer = Marshal.GetIUnknownForObject(_site);
            IntPtr intPointer; // = IntPtr.Zero;
            Marshal.QueryInterface(pUnknownPointer, ref riid, out intPointer);

            if (intPointer == IntPtr.Zero)
                throw new COMException("site does not support requested interface", VSConstants.E_NOINTERFACE);

            ppvSite = intPointer;
        }

        void VSOLE.IObjectWithSite.SetSite(object pUnkSite)
        {
            _site = pUnkSite;
        }

        #endregion

        private ServiceProvider SiteServiceProvider
        {
            get {
                return _serviceProvider ?? (_serviceProvider = new ServiceProvider(_site as VSOLE.IServiceProvider));
            }
        }

        protected object GetService(Type serviceType)
        {
            return SiteServiceProvider.GetService(serviceType);
        }

        protected ProjectItem GetProjectItem()
        {
            var p = GetService(typeof (ProjectItem));
            //Debug.Assert(p != null, "Unable to get Project Item.");
            return (ProjectItem) p;
        }

        protected Project GetProject()
        {
            return GetProjectItem().ContainingProject;
        }
    }
}
