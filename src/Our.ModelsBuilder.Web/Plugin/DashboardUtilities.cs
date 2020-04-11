using System.Text;
using Our.ModelsBuilder.Api;
using Our.ModelsBuilder.Options;
using Our.ModelsBuilder.Umbraco;
using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Core.Models.PublishedContent;

namespace Our.ModelsBuilder.Web.Plugin
{
    internal class DashboardUtilities
    {
        private readonly ModelsBuilderOptions _options;

        public DashboardUtilities(ModelsBuilderOptions options)
        {
            _options = options;
        }

        public bool CanGenerate()
        {
            return _options.ModelsMode.SupportsExplicitGeneration();
        }

        public bool GenerateCausesRestart()
        {
            return _options.ModelsMode.IsAnyDll();
        }

        public bool AreModelsOutOfDate()
        {
            return OutOfDateModelsStatus.IsOutOfDate;
        }

        public string LastError()
        {
            return ModelsGenerationError.GetLastError();
        }

        public string Text()
        {
            if (!_options.Enable)
                return "Version: " + ApiVersion.Current.Version + "<br />&nbsp;<br />ModelsBuilder is disabled<br />(the .Enable appSetting is missing, or its value is not 'true').";

            var sb = new StringBuilder();

            sb.Append("Version: ");
            sb.Append(ApiVersion.Current.Version);
            sb.Append("<br />&nbsp;<br />");

            sb.Append("ModelsBuilder is enabled, with the following configuration:");

            sb.Append("<ul>");

            sb.Append("<li>The <strong>models factory</strong> is ");
            sb.Append(_options.EnableFactory || _options.ModelsMode == ModelsMode.PureLive
                ? "enabled"
                : "not enabled. Umbraco will <em>not</em> use models");
            if (_options.EnableFactory || _options.ModelsMode == ModelsMode.PureLive)
            {
                sb.Append(", of type <strong>");
                sb.Append(Current.Factory.GetInstance<IPublishedModelFactory>().GetType().FullName);
                sb.Append("</strong>");
            }
            sb.Append(".</li>");

            sb.Append(_options.ModelsMode != ModelsMode.Nothing
                ? $"<li><strong>{_options.ModelsMode} models</strong> are enabled.</li>"
                : "<li>No models mode is specified: models will <em>not</em> be generated.</li>");

            sb.Append($"<li>Models namespace is <strong>{_options.ModelsNamespace}</strong> but may be overriden by attribute.</li>");

            sb.Append("<li>Tracking of <strong>out-of-date models</strong> is ");
            sb.Append(_options.FlagOutOfDateModels ? "enabled" : "not enabled");
            sb.Append(".</li>");

            sb.Append("<li>The <strong>API</strong> is ");
            if (_options.EnableApi)
            {
                sb.Append("enabled");
                if (!_options.IsDebug) sb.Append(".<br />However, the API runs only with <em>debug</em> compilation mode");
            }
            else sb.Append("not enabled");
            sb.Append(". ");

            if (!_options.IsApiServer)
                sb.Append("External tools such as Visual Studio <em>cannot</em> use the API");
            else
                sb.Append("<span style=\"color:orange;font-weight:bold;\">The API endpoint is open on this server</span>");
            sb.Append(".</li>");

            sb.Append("<li><strong>BackOffice</strong> integrations are ");
            sb.Append(_options.EnableBackOffice ? "enabled (else, you would not see this dashboard)" : "disabled");
            sb.Append(".</li>");

            sb.Append("</ul>");

            return sb.ToString();
        }
    }
}
