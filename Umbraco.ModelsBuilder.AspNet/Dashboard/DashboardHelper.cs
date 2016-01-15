using System.Text;
using Umbraco.ModelsBuilder.Configuration;

namespace Umbraco.ModelsBuilder.AspNet.Dashboard
{
    public static class DashboardHelper
    {
        public static bool CanGenerate()
        {
            return Config.EnableAppDataModels || Config.EnableAppCodeModels || Config.EnableDllModels;
        }

        public static bool GenerateCausesRestart()
        {
            return Config.EnableAppCodeModels || Config.EnableDllModels;
        }

        public static bool AreModelsOutOfDate()
        {
            return OutOfDateModelsStatus.IsOutOfDate;
        }

        public static string Text()
        {
            if (!Config.Enable)
                return "ModelsBuilder is disabled<br />(the .Enable key is missing, or its value is not 'true').";

            var sb = new StringBuilder();

            sb.Append("ModelsBuilder is enabled, with the following configuration:");

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
            if ((Config.EnableDllModels || Config.EnableAppCodeModels || Config.EnableAppDataModels))
            {
                if (Config.EnableLiveModels)
                {
                    sb.Append(", in <strong>live</strong> mode, ie models are generated anytime content types change");
                    if (Config.EnableDllModels || Config.EnableAppCodeModels)
                        sb.Append("&mdash;and the application restarts");
                }
                else
                {
                    sb.Append(", but not <em>live</em>&mdash;use the button below to generate");
                }
            }
            if (Config.EnablePureLiveModels || Config.EnableDllModels || Config.EnableAppCodeModels || Config.EnableAppDataModels)
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

            sb.Append("<li>Tracking of <strong>out-of-date models</strong> is ");
            sb.Append(Config.FlagOutOfDateModels ? "enabled" : "not enabled");
            sb.Append(".</li>");

            sb.Append("</ul>");

            return sb.ToString();
        }
    }
}
