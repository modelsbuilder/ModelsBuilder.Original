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
using Umbraco.ModelsBuilder.CustomTool.VisualStudio;

namespace Umbraco.ModelsBuilder.CustomTool
{
    // see http://msdn.microsoft.com/en-us/library/bb166195.aspx

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [CLSCompliant(false), ComVisible(true)] // that's for the grid
    // [Guid("1D9ECCF3-5D2F-4112-9B25-264596873DC9")] // that's for the custom page
    public class VisualStudioOptions : DialogPage
    {
        public const string OptionsCategory = "Umbraco";
        public const string OptionsPageName = "ModelsBuilder Options";

        //[Category(OptionsCategory)]
        //[DisplayName("Connection string")]
        //[Description("The database connection string.")]
        //public string ConnectionString { get; set; }

        //[Category(OptionsCategory)]
        //[DisplayName("Database provider")]
        //[Description("The database provider.")]
        //public string DatabaseProvider { get; set; }

        //[Category(OptionsCategory)]
        //[DisplayName("Bin directory")]
        //[Description("The directory containing the project's binaries. By default it is the project's OutputPath. Can be relative to project's root.")]
        //public string BinaryDirectory { get; set; }

        // note: could not find a way to order the properties, and Visual Studio sorts them alphabetically, so adjusting
        //       display names here so that we have the order that we want (url, user, password). Still need to figure
        //       out how to do it properly, though...

        [Category(OptionsCategory)]
        [DisplayName("Site Url")]
        [Description("The base url of the Umbraco website, eg \"http://example.com\".")]
        public string UmbracoUrl { get; set; }

        [Category(OptionsCategory)]
        [DisplayName("User Name")]
        [Description("The name of a user to connect to Umbraco (must be dev).")]
        public string UmbracoUser { get; set; }

        [Category(OptionsCategory)]
        [DisplayName("User Password")]
        [Description("The password of the user.")]
        public string UmbracoPassword { get; set; }

        // by default "storage" is the registry
        // we want to write to our own settings file,
        // <solution>.sln.UmbracoModelsBuilder.user
        // and yes - we should prob. use true config sections,
        // not parse XML...

        private string OptionsFileName
        {
            get
            {
                var solution = VisualStudioHelper.GetSolution();
                if (solution.EndsWith(".sln"))
                    solution = solution.Substring(0, solution.Length - ".sln".Length);
                var filename = solution + ".UmbracoModelsBuilder.user";
                return filename;
            }
        }

        public override void LoadSettingsFromStorage()
        {
            //base.LoadSettingsFromStorage();

            var filename = OptionsFileName;
            if (!File.Exists(filename)) return;

            var text = File.ReadAllText(filename);
            var xml = new XmlDocument();
            xml.LoadXml(text);

            var config = xml.SelectSingleNode("/configuration/umbraco/modelsBuilder");
            if (config == null || config.Attributes == null) return;

            var attr = config.Attributes["version"];
            if (attr == null) return;
            var version = attr.Value;

            // we're not version-dependent at the moment
            //attr = config.Attributes["connectionString"];
            //if (attr != null)
            //    ConnectionString = attr.Value;
            //attr = config.Attributes["databaseProvider"];
            //if (attr != null)
            //    DatabaseProvider = attr.Value;
            //attr = config.Attributes["binaryDirectory"];
            //if (attr != null)
            //    BinaryDirectory = attr.Value;
            attr = config.Attributes["umbracoUrl"];
            if (attr != null)
                UmbracoUrl = attr.Value;
            attr = config.Attributes["umbracoUser"];
            if (attr != null)
                UmbracoUser = attr.Value;
            attr = config.Attributes["umbracoPassword"];
            if (attr != null)
                UmbracoPassword = attr.Value;
        }

        public override void SaveSettingsToStorage()
        {
            //base.SaveSettingsToStorage();

            var filename = OptionsFileName;

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
            writer.WriteStartElement("umbraco");
            writer.WriteStartElement("modelsBuilder");
            writer.WriteAttributeString("version", version);
            //writer.WriteAttributeString("connectionString", ConnectionString);
            //writer.WriteAttributeString("databaseProvider", DatabaseProvider);
            //writer.WriteAttributeString("binaryDirectory", BinaryDirectory);
            writer.WriteAttributeString("umbracoUrl", UmbracoUrl);
            writer.WriteAttributeString("umbracoUser", UmbracoUser);
            writer.WriteAttributeString("umbracoPassword", UmbracoPassword);
            writer.WriteEndElement(); // modelsBuilder
            writer.WriteEndElement(); // umbraco
            writer.WriteEndElement(); // configuration
            writer.Flush();
            writer.Close();

            File.WriteAllText(filename, sb.ToString());
        }

        // what about the FromXml / ToXml methods?!
        // that would be for import/export?!
    }
}
