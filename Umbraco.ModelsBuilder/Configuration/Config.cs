using System;
using System.Configuration;
using Microsoft.CodeAnalysis.CSharp;
using Umbraco.Core;

namespace Umbraco.ModelsBuilder.Configuration
{
    /// <summary>
    /// Represents the models builder configuration.
    /// </summary>
    public class Config
    {
        private static Config _value;

        /// <summary>
        /// Gets the configuration - internal so that the UmbracoConfig extension
        /// can get the value to initialize its own value. Either a value has
        /// been provided via the Setup method, or a new instance is created, which
        /// will load settings from the config file.
        /// </summary>
        internal static Config Value => _value ?? new Config();

        /// <summary>
        /// Sets the configuration programmatically.
        /// </summary>
        /// <param name="config">The configuration.</param>
        /// <remarks>
        /// <para>Once the configuration has been accessed via the UmbracoConfig extension,
        /// it cannot be changed anymore, and using this method will achieve nothing.</para>
        /// <para>For tests, see UmbracoConfigExtensions.ResetConfig().</para>
        /// </remarks>
        public static void Setup(Config config)
        {
            _value = config;
        }

        internal const string DefaultStaticMixinGetterPattern = "Get{0}";
        internal const LanguageVersion DefaultLanguageVersion = LanguageVersion.CSharp5;
        internal const string DefaultModelsNamespace = "Umbraco.Web.PublishedContentModels";

        /// <summary>
        /// Initializes a new instance of the <see cref="Config"/> class.
        /// </summary>
        private Config()
        {
            const string prefix = "Umbraco.ModelsBuilder.";

            // giant kill switch, default: false
            // must be explicitely set to true for anything else to happen
            Enable = ConfigurationManager.AppSettings[prefix + "Enable"] == "true";

            // ensure defaults are initialized for tests
            StaticMixinGetterPattern = DefaultStaticMixinGetterPattern;
            LanguageVersion = DefaultLanguageVersion;
            ModelsNamespace = DefaultModelsNamespace;

            // stop here, everything is false
            if (!Enable) return;

            // mode
            var modelsMode = ConfigurationManager.AppSettings[prefix + "ModelsMode"];
            if (!string.IsNullOrWhiteSpace(modelsMode))
            {
                switch (modelsMode)
                {
                    case nameof(ModelsMode.PureLive):
                        ModelsMode = ModelsMode.PureLive;
                        break;
                    case nameof(ModelsMode.Dll):
                        ModelsMode = ModelsMode.Dll;
                        break;
                    case nameof(ModelsMode.LiveDll):
                        ModelsMode = ModelsMode.LiveDll;
                        break;
                    case nameof(ModelsMode.AppData):
                        ModelsMode = ModelsMode.AppData;
                        break;
                    case nameof(ModelsMode.LiveAppData):
                        ModelsMode = ModelsMode.LiveAppData;
                        break;
                    default:
                        throw new ConfigurationErrorsException($"ModelsMode \"{modelsMode}\" is not a valid mode."
                            + " Note that modes are case-sensitive.");
                }
            }

            // default: false
            EnableApi = ConfigurationManager.AppSettings[prefix + "EnableApi"].InvariantEquals("true");

            // default: true
            EnableFactory = !ConfigurationManager.AppSettings[prefix + "EnableFactory"].InvariantEquals("false");
            StaticMixinGetters = !ConfigurationManager.AppSettings[prefix + "StaticMixinGetters"].InvariantEquals("false");
            FlagOutOfDateModels = !ConfigurationManager.AppSettings[prefix + "FlagOutOfDateModels"].InvariantEquals("false");

            // default: initialized above with DefaultModelsNamespace const
            var value = ConfigurationManager.AppSettings[prefix + "ModelsNamespace"];
            if (!string.IsNullOrWhiteSpace(value))
                ModelsNamespace = value;

            // default: initialized above with DefaultStaticMixinGetterPattern const
            value = ConfigurationManager.AppSettings[prefix + "StaticMixinGetterPattern"];
            if (!string.IsNullOrWhiteSpace(value))
                StaticMixinGetterPattern = value;

            // default: initialized above with DefaultLanguageVersion const
            value = ConfigurationManager.AppSettings[prefix + "LanguageVersion"];
            if (!string.IsNullOrWhiteSpace(value))
            {
                LanguageVersion lv;
                if (!Enum.TryParse(value, true, out lv))
                    throw new ConfigurationErrorsException($"Invalid language version \"{value}\".");
                LanguageVersion = lv;
            }

            // not flagging if not generating, or live (incl. pure)
            if (ModelsMode == ModelsMode.Nothing || ModelsMode.IsLive())
                FlagOutOfDateModels = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Config"/> class.
        /// </summary>
        public Config(
            bool enable = false,
            ModelsMode modelsMode = ModelsMode.Nothing,
            bool enableApi = true,
            string modelsNamespace = null,
            bool enableFactory = true,
            LanguageVersion languageVersion = DefaultLanguageVersion,
            bool staticMixinGetters = true,
            string staticMixinGetterPattern = null,
            bool flagOutOfDateModels = true)
        {
            Enable = enable;
            ModelsMode = modelsMode;

            EnableApi = enableApi;
            ModelsNamespace = string.IsNullOrWhiteSpace(modelsNamespace) ? DefaultModelsNamespace : modelsNamespace;
            EnableFactory = enableFactory;
            LanguageVersion = languageVersion;
            StaticMixinGetters = staticMixinGetters;
            StaticMixinGetterPattern = string.IsNullOrWhiteSpace(staticMixinGetterPattern) ? DefaultStaticMixinGetterPattern : staticMixinGetterPattern;
            FlagOutOfDateModels = flagOutOfDateModels;
        }

        /// <summary>
        /// Gets a value indicating whether the whole models experience is enabled.
        /// </summary>
        /// <remarks>
        ///     <para>If this is false then absolutely nothing happens.</para>
        ///     <para>Default value is <c>false</c> which means that unless we have this setting, nothing happens.</para>
        /// </remarks>
        public bool Enable { get; }

        /// <summary>
        /// Gets the models mode.
        /// </summary>
        public ModelsMode ModelsMode { get; }

        /// <summary>
        /// Gets a value indicating whether to enable the API.
        /// </summary>
        /// <remarks>
        ///     <para>Default value is <c>true</c>.</para>
        ///     <para>The API is used by the Visual Studio extension and the console tool to talk to Umbraco
        ///     and retrieve the content types. It needs to be enabled so the extension & tool can work.</para>
        /// </remarks>
        public bool EnableApi { get; }

        /// <summary>
        /// Gets the models namespace.
        /// </summary>
        /// <remarks>That value could be overriden by other (attribute in user's code...). Return default if no value was supplied.</remarks>
        public string ModelsNamespace { get; }

        /// <summary>
        /// Gets a value indicating whether we should enable the models factory.
        /// </summary>
        /// <remarks>Default value is <c>true</c> because no factory is enabled by default in Umbraco.</remarks>
        public bool EnableFactory { get; }

        /// <summary>
        /// Gets the Roslyn parser language version.
        /// </summary>
        /// <remarks>Default value is <c>CSharp5</c>.</remarks>
        public LanguageVersion LanguageVersion { get; }

        /// <summary>
        /// Gets a value indicating whether to generate static mixin getters.
        /// </summary>
        /// <remarks>Default value is <c>false</c> for backward compat reaons.</remarks>
        public bool StaticMixinGetters { get; }

        /// <summary>
        /// Gets the string pattern for mixin properties static getter name.
        /// </summary>
        /// <remarks>Default value is "GetXxx". Standard string format.</remarks>
        public string StaticMixinGetterPattern { get; }

        /// <summary>
        /// Gets a value indicating whether we should flag out-of-date models.
        /// </summary>
        /// <remarks>Models become out-of-date when data types or content types are updated. When this
        /// setting is activated the ~/App_Data/Models/ood.txt file is then created. When models are
        /// generated through the dashboard, the files is cleared. Default value is <c>false</c>.</remarks>
        public bool FlagOutOfDateModels { get; }
    }
}
