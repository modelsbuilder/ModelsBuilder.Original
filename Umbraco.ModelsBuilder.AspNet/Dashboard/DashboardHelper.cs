using System.Text;
using Umbraco.ModelsBuilder.Configuration;

namespace Umbraco.ModelsBuilder.AspNet.Dashboard
{
    public static class DashboardHelper
    {
        public static bool IsUmbraco6()
        {
            var ver = global::Umbraco.Core.Configuration.UmbracoVersion.Current;
            return ver.Major == 6;
        }

        public static System.Web.UI.Control Umbraco6Control()
        {
            var css = new ClientDependency.Core.Controls.CssInclude
            {
                FilePath = "propertypane/style.css",
                PathNameAlias = "UmbracoClient"
            };
            return css;
        }

        public static bool CanGenerate()
        {
            return Config.EnableAppDataModels || Config.EnableAppCodeModels || Config.EnableDllModels;
        }

        public static bool GenerateRestarts()
        {
            return Config.EnableAppCodeModels || Config.EnableDllModels;
        }

        public static string GenerateLabel()
        {
            return OutOfDateModelsStatus.IsOutOfDate
                ? "Models are <strong>out-of-date</strong>, click button to generate models."
                : "Click button to generate models.";
        }

        public static string BuildUrl()
        {
            return ModelsBuilderController.ActionUrl(nameof(ModelsBuilderController.BuildModels));
        }

        private static string Enabled(bool value)
        {
            return value ? "enabled" : "disabled";
        }

        public static string Report()
        {
            if (!Config.Enable)
                return "ModelsBuilder is disabled<br />(the .Enable key is missing, or its value is not 'true').";

            var sb = new StringBuilder();

            sb.Append("ModelsBuilder is enabled.");

            sb.Append("<ul>");

            sb.Append("<li>The <strong>models factory</strong> is ");
            sb.Append(Config.EnableFactory || Config.EnablePureLiveModels
                ? "enabled" 
                : "not enabled. Umbraco will <em>not</em> use models");
            sb.Append(".</li>");

            sb.Append("<li>The <strong>API</strong> is ");
            sb.Append(Config.EnableApi
                ? "enabled"
                : "not enabled. External tools such as Visual Studio <em>cannot</em> use the API");
            sb.Append(".</li>");

            if (Config.EnablePureLiveModels)
                sb.Append("<li><strong>Pure Live models</strong> are enabled");
            if (Config.EnableDllModels)
                sb.Append("<li><strong>Dll models</strong> are enabled");
            if (Config.EnableAppCodeModels)
                sb.Append("<li><strong>AppCode models</strong> are enabled");
            if (Config.EnableAppDataModels)
                sb.Append("<li><strong>AppData models</strong> are enabled");
            if (Config.EnableLiveModels && !Config.EnablePureLiveModels)
            {
                sb.Append(", in <strong>live</strong> mode, ie models are generated anytime content types change");
                if (Config.EnableDllModels || Config.EnableAppCodeModels)
                    sb.Append(", and the application restarts");
            }
            sb.Append(".</li>");

            sb.Append("<li>Models namespace is ");
            sb.Append(string.IsNullOrWhiteSpace(Config.ModelsNamespace)
                ? "not configured (will use default)"
                : $"\"{Config.ModelsNamespace}\"");
            sb.Append(".</li>");

            sb.Append("<li>Static mixin getters are ");
            sb.Append(Config.StaticMixinGetters ? "enabled" : "disabled");
            if (Config.StaticMixinGetters)
            {
                sb.Append(". The pattern for getters is ");
                sb.Append(string.IsNullOrWhiteSpace(Config.StaticMixinGetterPattern)
                    ? "not configured (will use default)"
                    : $"\"{Config.StaticMixinGetterPattern}\"");
            }
            sb.Append(".</li>");

            sb.Append("<li>Tracking <strong>out-of-date models</strong> is ");
            sb.Append(Config.FlagOutOfDateModels ? "enabled" : "not enabled");
            sb.Append(".</li>");

            sb.Append("</ul>");

            return sb.ToString();
        }
    }
}
