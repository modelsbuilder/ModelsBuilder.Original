using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace ZpqrtBnk.ModelzBuilder.CustomTool
{
    // see http://msdn.microsoft.com/en-us/library/bb166195.aspx

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [CLSCompliant(false), ComVisible(true)] // that's for the grid
    // [Guid("1D9ECCF3-5D2F-4112-9B25-264596873DC9")] // that's for the custom page
    public class OptionsDialog : DialogPage
    {
        public override object AutomationObject => VisualStudioOptions.Instance;

        // reload settings from file when activated
        // reload settings when loading a solution
        // clear settings when unloading a solution
        // see https://stackoverflow.com/questions/44108369/how-to-be-notified-when-a-solution-has-been-loaded-for-a-vspackage

        protected override void OnActivate(CancelEventArgs e)
        {
            base.OnActivate(e);

            // reload whenever activated
            VisualStudioOptions.Instance.Reload();
        }

        public override void LoadSettingsFromStorage()
        {
            VisualStudioOptions.Instance.Load();
        }

        public override void SaveSettingsToStorage()
        {
            VisualStudioOptions.Instance.Save();
        }
    }
}
