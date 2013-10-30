using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
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
        // and yes - we should prob. use true config sections,
        // not parse XML...

        public override void LoadSettingsFromStorage()
        {
            //base.LoadSettingsFromStorage();

            var solution = VisualStudioHelper.GetSolution();
            var filename = solution + ".zbu.user";
            if (!File.Exists(filename)) return;

            var text = File.ReadAllText(filename);
            var xml = new XmlDocument();
            xml.LoadXml(text);

            var config = xml.SelectSingleNode("/configuration/zbu/modelsBuilder");
            if (config == null || config.Attributes == null) return;

            var attr = config.Attributes["version"];
            if (attr == null) return;
            var version = attr.Value;

            // we're not version-dependent at the moment
            attr = config.Attributes["connectionString"];
            if (attr != null)
                ConnectionString = attr.Value;
            attr = config.Attributes["databaseProvider"];
            if (attr != null)
                DatabaseProvider = attr.Value;
        }

        public override void SaveSettingsToStorage()
        {
            //base.SaveSettingsToStorage();

            var solution = VisualStudioHelper.GetSolution();
            var filename = solution + ".zbu.user";

            if (File.Exists(filename))
                File.Delete(filename);

            var version = Assembly.GetExecutingAssembly().GetName().Version.ToString();

            var sb = new StringBuilder();
            var settings = new XmlWriterSettings
            {
                Encoding = Encoding.UTF8,
                OmitXmlDeclaration = true, // 'cos it's utf-16 and a pain to change
                Indent = true,
                NewLineChars = "\r\n"
            };
            var writer = XmlWriter.Create(sb, settings);

            sb.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>");

            writer.WriteStartElement("configuration");
            writer.WriteStartElement("zbu");
            writer.WriteStartElement("modelsBuilder");
            writer.WriteAttributeString("version", version);
            writer.WriteAttributeString("connectionString", ConnectionString);
            writer.WriteAttributeString("databaseProvider", DatabaseProvider);
            writer.WriteEndElement(); // modelsBuilder
            writer.WriteEndElement(); // zbu
            writer.WriteEndElement(); // configuration
            writer.Flush();
            writer.Close();

            File.WriteAllText(filename, sb.ToString());
        }

        // what about the FromXml / ToXml methods?!
        // that would be for import/export?!
    }
}
