using System;
using System.IO;
using System.Web.Hosting;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Zbu.ModelsBuilder.AspNet
{
    public class GenerateModelsDashboard : UserControl
    {
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if (Request.QueryString["generate"] == "GenerateModelsDashBoardPleaseGenerate")
            {
                var appData = HostingEnvironment.MapPath("~/App_Data");
                if (appData == null)
                    throw new Exception("Panic: appData is null.");

                var appCode = HostingEnvironment.MapPath("~/App_Code");
                if (appCode == null)
                    throw new Exception("Panic: appCode is null.");

                var modelsBuilder = new ModelsBuilder();
                modelsBuilder.GenerateSourceFiles();

                var modelsFile = Path.Combine(appCode, "build.models");

                // touch the file & make sure it exists, will recycle the domain
                File.WriteAllText(modelsFile, DateTime.Now.ToString());
            }
        }
    }
}