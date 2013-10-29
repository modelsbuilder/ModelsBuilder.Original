using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;

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
        [DisplayName("Test")]
        [Description("Test description")]
// ReSharper disable once ConvertToAutoProperty
        public string UmbracoVersion
        {
            get { return _umbracoVersion; }
            set { _umbracoVersion = value; }
        }

        private string _umbracoVersion;


        //[Category(OptionsCategory)]
        //[DisplayName("Umbraco version")]
        //[Description("The Umbraco version eg 6.2.0.")]
        //public string UmbracoVersion { get; set; }

        //[Category(OptionsCategory)]
        //[DisplayName("Connection string")]
        //[Description("The database connection string.")]
        //public string ConnectionString { get; set; }

        //[Category(OptionsCategory)]
        //[DisplayName("Database provider")]
        //[Description("The database provider.")]
        //public string DatabaseProvider { get; set; }

        // by default "storage" is the registry
        // we want to write to our own settings file,
        // <solution>.sln.zbu.user
        // <zbu>
        //   <modelsBuilder umbracoVersion="6.2.0" connectionString="..." databaseProvider="..." />
        // </zbu>

        //public override void LoadSettingsFromStorage()
        //{
        //    // set hard-coded value for now
        //    UmbracoVersion = "6.2.0.testing";

        //    //base.LoadSettingsFromStorage();
        //}

        //public override void SaveSettingsToStorage()
        //{
        //    // ignore for now
        //    //base.SaveSettingsToStorage();
        //}

        // FIXME but what about the FromXml / ToXml methods?!
        // that would be for import/export?!
    }
}
