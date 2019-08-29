using System.Text;
using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Core.Models.PublishedContent;
using ZpqrtBnk.ModelsBuilder.Api;
using ZpqrtBnk.ModelsBuilder.Configuration;
using ZpqrtBnk.ModelsBuilder.Umbraco;

namespace ZpqrtBnk.ModelsBuilder.Web.Plugin
{
    internal static class DashboardHelper
    {
        private static Config Config => Current.Configs.ModelsBuilder();

        public static bool CanGenerate()
        {
            return Config.ModelsMode.SupportsExplicitGeneration();
        }

        public static bool GenerateCausesRestart()
        {
            return Config.ModelsMode.IsAnyDll();
        }

        public static bool AreModelsOutOfDate()
        {
            return OutOfDateModelsStatus.IsOutOfDate;
        }

        public static string LastError()
        {
            return ModelsGenerationError.GetLastError();
        }

        public static string Text()
        {
            var config = Config;

            if (!config.Enable)
                return "Version: " + ApiVersion.Current.Version + "<br />&nbsp;<br />ModelsBuilder is disabled<br />(the .Enable appSetting is missing, or its value is not 'true').";

            var sb = new StringBuilder();

            sb.Append("Version: ");
            sb.Append(ApiVersion.Current.Version);
            sb.Append("<br />&nbsp;<br />");

            sb.Append("ModelsBuilder is enabled, with the following configuration:");

            sb.Append("<ul>");

            sb.Append("<li>The <strong>models factory</strong> is ");
            sb.Append(config.EnableFactory || config.ModelsMode == ModelsMode.PureLive
                ? "enabled"
                : "not enabled. Umbraco will <em>not</em> use models");
            if (config.EnableFactory || config.ModelsMode == ModelsMode.PureLive)
            {
                sb.Append(", of type <strong>");
                sb.Append(Current.Factory.GetInstance<IPublishedModelFactory>().GetType().FullName);
                sb.Append("</strong>");
            }
            sb.Append(".</li>");

            sb.Append(config.ModelsMode != ModelsMode.Nothing
                ? $"<li><strong>{config.ModelsMode} models</strong> are enabled.</li>"
                : "<li>No models mode is specified: models will <em>not</em> be generated.</li>");

            sb.Append($"<li>Models namespace is <strong>{config.ModelsNamespace}</strong> but may be overriden by attribute.</li>");

            sb.Append("<li>Tracking of <strong>out-of-date models</strong> is ");
            sb.Append(config.FlagOutOfDateModels ? "enabled" : "not enabled");
            sb.Append(".</li>");

            sb.Append("<li>The <strong>API</strong> is ");
            if (config.EnableApi)
            {
                sb.Append("enabled");
                if (!config.IsDebug) sb.Append(".<br />However, the API runs only with <em>debug</em> compilation mode");
            }
            else sb.Append("not enabled");
            sb.Append(". ");

            if (!config.IsApiServer)
                sb.Append("External tools such as Visual Studio <em>cannot</em> use the API");
            else
                sb.Append("<span style=\"color:orange;font-weight:bold;\">The API endpoint is open on this server</span>");
            sb.Append(".</li>");

            sb.Append("<li><strong>BackOffice</strong> integrations are ");
            sb.Append(config.EnableBackOffice ? "enabled (else, you would not see this dashboard)" : "disabled");
            sb.Append(".</li>");

            sb.Append("</ul>");

            return sb.ToString();
        }
    }
}
