using System.Text;
using Umbraco.Core.Configuration;
using Umbraco.ModelsBuilder.Configuration;

namespace Umbraco.ModelsBuilder.AspNet.Dashboard
{
    public static class DashboardHelper
    {
        public static bool CanGenerate()
        {
            var config = UmbracoConfig.For.ModelsBuilder();
            return config.EnableAppDataModels || config.EnableAppCodeModels || config.EnableDllModels;
        }

        public static bool GenerateCausesRestart()
        {
            var config = UmbracoConfig.For.ModelsBuilder();
            return config.EnableAppCodeModels || config.EnableDllModels;
        }

        public static bool AreModelsOutOfDate()
        {
            return OutOfDateModelsStatus.IsOutOfDate;
        }

        public static string Text()
        {
            var config = UmbracoConfig.For.ModelsBuilder();
            if (!config.Enable)
                return "ModelsBuilder is disabled<br />(the .Enable key is missing, or its value is not 'true').";

            var sb = new StringBuilder();

            sb.Append("ModelsBuilder is enabled, with the following configuration:");

            sb.Append("<ul>");

            sb.Append("<li>The <strong>models factory</strong> is ");
            sb.Append(config.EnableFactory || config.EnablePureLiveModels
                ? "enabled" 
                : "not enabled. Umbraco will <em>not</em> use models");
            sb.Append(".</li>");

            sb.Append("<li>The <strong>API</strong> is ");
            sb.Append(config.EnableApi
                ? "enabled"
                : "not enabled. External tools such as Visual Studio <em>cannot</em> use the API");
            sb.Append(".</li>");

            if (config.EnablePureLiveModels)
                sb.Append("<li><strong>Pure Live models</strong> are enabled");

            if (config.EnableDllModels)
                sb.Append("<li><strong>Dll models</strong> are enabled");
            if (config.EnableAppCodeModels)
                sb.Append("<li><strong>AppCode models</strong> are enabled");
            if (config.EnableAppDataModels)
                sb.Append("<li><strong>AppData models</strong> are enabled");
            if ((config.EnableDllModels || config.EnableAppCodeModels || config.EnableAppDataModels))
            {
                if (config.EnableLiveModels)
                {
                    sb.Append(", in <strong>live</strong> mode, ie models are generated anytime content types change");
                    if (config.EnableDllModels || config.EnableAppCodeModels)
                        sb.Append("&mdash;and the application restarts");
                }
                else
                {
                    sb.Append(", but not <em>live</em>&mdash;use the button below to generate");
                }
            }
            if (config.EnablePureLiveModels || config.EnableDllModels || config.EnableAppCodeModels || config.EnableAppDataModels)
                sb.Append(".</li>");

            sb.Append("<li>Models namespace is ");
            sb.Append(string.IsNullOrWhiteSpace(config.ModelsNamespace)
                ? "not configured (will use default)"
                : $"\"{config.ModelsNamespace}\"");
            sb.Append(".</li>");

            sb.Append("<li>Static mixin getters are ");
            sb.Append(config.StaticMixinGetters ? "enabled" : "disabled");
            if (config.StaticMixinGetters)
            {
                sb.Append(". The pattern for getters is ");
                sb.Append(string.IsNullOrWhiteSpace(config.StaticMixinGetterPattern)
                    ? "not configured (will use default)"
                    : $"\"{config.StaticMixinGetterPattern}\"");
            }
            sb.Append(".</li>");

            sb.Append("<li>Tracking of <strong>out-of-date models</strong> is ");
            sb.Append(config.FlagOutOfDateModels ? "enabled" : "not enabled");
            sb.Append(".</li>");

            sb.Append("</ul>");

            return sb.ToString();
        }
    }
}
