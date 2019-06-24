using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using ZpqrtBnk.ModelzBuilder.CustomTool.VisualStudio;

namespace ZpqrtBnk.ModelzBuilder.CustomTool
{
    public class VisualStudioOptions
    {
        public const string OptionsCategory = "Umbraco";
        public const string OptionsPageName = "ModelsBuilder Options";

        public static readonly VisualStudioOptions Instance = new VisualStudioOptions();

        // note: could not find a way to order the properties, and Visual Studio sorts them alphabetically, so adjusting
        //       display names here so that we have the order that we want (url, user, password). would be nice to figure
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

        public void Clear()
        {
            UmbracoUrl = UmbracoUser = UmbracoPassword = null;
        }

        public void Reload()
        {
            Clear();
            Load();
        }

        public void Load()
        {
            // no solution = no options
            if (!VisualStudioHelper.HasSolution)
            {
                UmbracoUrl = "(no solution)";
                return;
            }

            // no solution = no options
            var filename = OptionsFileName;
            if (!File.Exists(filename))
                return;

            var text = File.ReadAllText(filename);
            var xml = new XmlDocument();
            xml.LoadXml(text);

            var config = xml.SelectSingleNode("/configuration/umbraco/modelsBuilder");
            if (config == null || config.Attributes == null) return;

            // we're not version-dependent at the moment
            //var attr = config.Attributes["version"];
            //if (attr == null) return;
            //var version = attr.Value;

            var attr = config.Attributes["umbracoUrl"];
            if (attr != null)
                UmbracoUrl = attr.Value;

            attr = config.Attributes["umbracoUser"];
            if (attr != null)
                UmbracoUser = attr.Value;

            attr = config.Attributes["umbracoPassword"];
            if (attr != null)
                UmbracoPassword = attr.Value;
        }

        public void Save()
        {
            // no solution = no options
            if (!VisualStudioHelper.HasSolution)
                return;

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

            // write version
            writer.WriteAttributeString("version", version);

            // write values
            writer.WriteAttributeString("umbracoUrl", UmbracoUrl);
            writer.WriteAttributeString("umbracoUser", UmbracoUser);
            writer.WriteAttributeString("umbracoPassword", UmbracoPassword);

            // close
            writer.WriteEndElement(); // modelsBuilder
            writer.WriteEndElement(); // umbraco
            writer.WriteEndElement(); // configuration
            writer.Flush();
            writer.Close();

            File.WriteAllText(filename, sb.ToString());
        }

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
                        message.Append(", ");
                    message.Append(empty[i]);
                }
                message.Append(" cannot be empty.");
            }

            try
            {
                _ = new Uri(UmbracoUrl);
            }
            catch
            {
                if (message == null)
                    message = new StringBuilder("Invalid configuration. Site Url \"");
                else
                    message.Append(" Site Url \"");
                message.Append(UmbracoUrl);
                message.Append("\" is not a valid Uri.");
            }

            if (message != null)
                throw new Exception(message.ToString());
        }
    }
}