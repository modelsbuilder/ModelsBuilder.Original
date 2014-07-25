using System.IO;
using System.Web.Hosting;
using System.Xml;
using Microsoft.Web.XmlTransform;
using Umbraco.Core.Logging;
using umbraco.interfaces;

namespace Zbu.ModelsBuilder.AspNet.PackageActions
{
    public class Configure2 : IPackageAction
    {
        private const string ActionAlias = "Zbu.ModelsBuilder.AspNet.PackageActions.Configure2";

        public string Alias()
        {
            return ActionAlias;
        }

        public bool Execute(string packageName, XmlNode xmlData)
        {
            LogHelper.Info<Configure2>("Install.");
            return InstallOrUninstall(true);
        }

        public bool Undo(string packageName, XmlNode xmlData)
        {
            LogHelper.Info<Configure2>("Uninstall.");
            return InstallOrUninstall(false);
        }

        public bool InstallOrUninstall(bool install)
        {
            var xdtPath = HostingEnvironment.MapPath("~/App_Plugins/Zbu.ModelsBuilder/package");

            var webConfigFullName = HostingEnvironment.MapPath("~/web.config");
            if (!Transform(webConfigFullName, xdtPath, install))
                return false;

            var dashboardConfigFullName = HostingEnvironment.MapPath("~/config/dashboard.config");
            if (!Transform(dashboardConfigFullName, xdtPath, install))
                return false;

            return true;
        }

        public XmlNode SampleXml()
        {
            const string xml = "<Action runat=\"install\" undo=\"true\" alias=\"" + ActionAlias + "\" />";
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            return doc.DocumentElement;
        }

        private static bool Transform(string fileFullName, string xdtPath, bool install)
        {
            var doc = new XmlTransformableDocument
            {
                PreserveWhitespace = true
            };
            doc.Load(fileFullName);

            var fileName = Path.GetFileName(fileFullName);
            var xdtName = fileName + "." + (install ? "install" : "uninstall") + ".xdt";
            var xdtFullName = Path.Combine(xdtPath, xdtName);
            if (!File.Exists(xdtFullName))
            {
                LogHelper.Info<Configure2>("Missing transform {0}.", () => xdtName);
                return true;
            }

            LogHelper.Info<Configure2>("Apply transform {0}.", () => xdtName);

            var transform = new XmlTransformation(xdtFullName, true, null);
            var result = transform.Apply(doc);

            if (!result)
                return false;

            doc.Save(fileFullName);
            return true;
        }
    }
}
