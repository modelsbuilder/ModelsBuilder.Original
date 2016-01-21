using System.Text;
using Umbraco.Core.Configuration;
using Umbraco.ModelsBuilder.Configuration;

namespace Umbraco.ModelsBuilder.AspNet.Dashboard
{
    internal static class DashboardHelper
    {
        public static bool CanGenerate()
        {
            var config = UmbracoConfig.For.ModelsBuilder();
            return config.ModelsMode.SupportsExplicitGeneration();
        }

        public static bool GenerateCausesRestart()
        {
            var config = UmbracoConfig.For.ModelsBuilder();
            return
                config.ModelsMode == ModelsMode.AppCode
                || config.ModelsMode == ModelsMode.LiveAppCode
                || config.ModelsMode == ModelsMode.Dll
                || config.ModelsMode == ModelsMode.LiveDll;
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
            sb.Append(config.EnableFactory || config.ModelsMode == ModelsMode.PureLive
                ? "enabled"
                : "not enabled. Umbraco will <em>not</em> use models");
            sb.Append(".</li>");

            sb.Append("<li>The <strong>API</strong> is ");
            sb.Append(config.EnableApi
                ? "enabled"
                : "not enabled. External tools such as Visual Studio <em>cannot</em> use the API");
            sb.Append(".</li>");

            sb.Append(config.ModelsMode != ModelsMode.Nothing
                ? $"<li><strong>{config.ModelsMode} models</strong> are enabled.</li>"
                : "<li>No models mode is specified: models will <em>not</em> be generated.</li>");

            sb.Append($"<li>Models namespace is {config.ModelsNamespace}.</li>");

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
