using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Zbu.ModelsBuilder.CustomTool.VisualStudio;

namespace Zbu.ModelsBuilder.CustomTool
{
    // see http://msdn.microsoft.com/en-us/library/bb166195.aspx

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [CLSCompliant(false), ComVisible(true)] // that's for the grid
    // [Guid("1D9ECCF3-5D2F-4112-9B25-264596873DC9")] // that's for the custom page
    public class VisualStudioOptions : DialogPage
    {
        public const string OptionsCategory = "Zbu";
        public const string OptionsPageName = "ModelsBuilder Options";

        [Category(OptionsCategory)]
        [DisplayName("Connection string")]
        [Description("The database connection string.")]
        public string ConnectionString { get; set; }

        [Category(OptionsCategory)]
        [DisplayName("Database provider")]
        [Description("The database provider.")]
        public string DatabaseProvider { get; set; }

        // by default "storage" is the registry
        // we want to write to our own settings file,
        // <solution>.sln.zbu.user
        // <zbu>
        //   <modelsBuilder version="1.4.0.0" connectionString="..." databaseProvider="..." />
        // </zbu>

        public override void LoadSettingsFromStorage()
        {
            //base.LoadSettingsFromStorage();

            var sln = VisualStudioHelper.GetSolution();
            var cfg = sln + ".zbu.user";
            if (File.Exists(cfg))
            {
                var txt = File.ReadAllText(cfg);
                var pos = txt.IndexOf("|||");
                ConnectionString = txt.Substring(0, pos);
                DatabaseProvider = txt.Substring(pos + 3);
            }
        }

        public override void SaveSettingsToStorage()
        {
            //base.SaveSettingsToStorage();

            // fixme - just testing, we need a better format
            // fixme - must also serialize OUR own version for upgrade purpose
            var sln = VisualStudioHelper.GetSolution();
            var cfg = sln + ".zbu.user";
            if (File.Exists(cfg))
                File.Delete(cfg);
            File.WriteAllText(cfg, ConnectionString + "|||" + DatabaseProvider);
        }

        // FIXME but what about the FromXml / ToXml methods?!
        // that would be for import/export?!
    }
}
