using System;
using System.Web;
using System.Xml;
using Umbraco.Core.Logging;
using umbraco.interfaces;

namespace Zbu.ModelsBuilder.Umbraco.PackageActions
{
    // see also http://our.umbraco.org/forum/umbraco-7/developing-umbraco-7-packages/46885-How-can-I-run-code-on-package-uninstall
    // see also http://packageactioncontrib.codeplex.com
    // what happens when we save web.config?

    public class Configure : IPackageAction
    {
        private const string WebConfig = "~/web.config";
        private const string ActionAlias = "Zbu.ModelsBuilder.Umbraco.PackageActions.Configure";
        private const string AssemblyToAdd = "System.Runtime, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";

        public string Alias()
        {
            return ActionAlias;
        }

        public bool Execute(string packageName, XmlNode xmlData)
        {
            LogHelper.Debug<Configure>("Configure Zbu.ModelsBuilder package.");

            var webConfigPath = HttpContext.Current.Server.MapPath(WebConfig);
            var webConfig = new XmlDocument();
            var edit = false;
            webConfig.PreserveWhitespace = true;
            webConfig.Load(webConfigPath);

            var buildProviders = webConfig.SelectSingleNode("//configuration/system.web/compilation/buildProviders");
            if (buildProviders == null)
            {
                LogHelper.Warn<Configure>("Could not find buildProviders element in web.config.");
                return false;
            }
            var add = buildProviders.SelectSingleNode("add [@extension='.models']");
            if (add == null)
            {
                add = webConfig.CreateElement("add");
                var attr = webConfig.CreateAttribute("extension");
                attr.Value = ".models";
                add.Attributes.Append(attr);
                attr = webConfig.CreateAttribute("type");
                attr.Value = "Zbu.ModelsBuilder.AspNet.ModelsBuildProvider, Zbu.ModelsBuilder.AspNet";
                add.Attributes.Append(attr);
                buildProviders.AppendChild(add);
                LogHelper.Debug<Configure>("Registered .models BuildProvider.");
                edit = true;
            }

            var assemblies = webConfig.SelectSingleNode("//configuration/system.web/compilation/assemblies");
            if (assemblies == null)
            {
                LogHelper.Warn<Configure>("Could not find assemblies element in web.config.");
                return false;
            }
            add = assemblies.SelectSingleNode("add [@assembly='" + AssemblyToAdd + "']");
            if (add == null)
            {
                add = webConfig.CreateElement("add");
                var attr = webConfig.CreateAttribute("assembly");
                attr.Value = AssemblyToAdd;
                add.Attributes.Append(attr);
                assemblies.AppendChild(add);
                LogHelper.Debug<Configure>("Registered compilation assembly.");
                edit = true;
            }

            if (!edit) return true;

            var result = false;
            try
            {
                webConfig.Save(webConfigPath);
                result = true;
            }
            catch (Exception e)
            {
                LogHelper.WarnWithException<Configure>("Failed to save web.config", e);                
            }

            return result;
        }

        public XmlNode SampleXml()
        {
            const string xml = "<Action runat=\"install\" undo=\"true\" alias=\"" + ActionAlias + "\" />";
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            return doc.DocumentElement;
        }

        public bool Undo(string packageName, XmlNode xmlData)
        {
            LogHelper.Debug<Configure>("Remove Zbu.ModelsBuilder package.");

            var webConfigPath = HttpContext.Current.Server.MapPath(WebConfig);
            var webConfig = new XmlDocument();
            var edit = false;
            webConfig.PreserveWhitespace = true;
            webConfig.Load(webConfigPath);

            var buildProviders = webConfig.SelectSingleNode("//configuration/system.web/compilation/buildProviders");
            if (buildProviders != null)
            {
                var add = buildProviders.SelectSingleNode("add [@extension='.models']");
                if (add != null)
                {
                    buildProviders.RemoveChild(add);
                    LogHelper.Debug<Configure>("Removed .models BuildProvider.");
                    edit = true;
                }
            }

            var assemblies = webConfig.SelectSingleNode("//configuration/system.web/compilation/assemblies");
            if (assemblies != null)
            {
                var add = assemblies.SelectSingleNode("add [@assembly='System.Runtime, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a']");
                if (add != null)
                {
                    assemblies.RemoveChild(add);
                    LogHelper.Debug<Configure>("Removed compilation assembly.");
                    edit = true;
                }
            }

            if (!edit) return true;

            var result = false;
            try
            {
                webConfig.Save(webConfigPath);
                result = true;
            }
            catch (Exception e)
            {
                LogHelper.WarnWithException<Configure>("Failed to save web.config", e);
            }

            return result;
        }
    }
}
