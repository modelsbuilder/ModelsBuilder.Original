using System;
using System.Configuration;
using System.Web.Compilation;
using Microsoft.CodeAnalysis.CSharp;

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

        private const string DefaultStaticMixinGetterPattern = "Get{0}";
        private const LanguageVersion DefaultLanguageVersion = LanguageVersion.CSharp5;
        public const string DefaultModelsNamespace = "Umbraco.Web.PublishedContentModels";

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
            
            // stop here, everything is false
            if (!Enable) return;

            // all of these have default: false
            EnableDllModels = Enable && ConfigurationManager.AppSettings[prefix + "EnableDllModels"].InvariantEquals("true");
            EnableAppCodeModels = Enable && ConfigurationManager.AppSettings[prefix + "EnableAppCodeModels"].InvariantEquals("true");
            EnableAppDataModels = Enable && ConfigurationManager.AppSettings[prefix + "EnableAppDataModels"].InvariantEquals("true");
            EnableLiveModels = Enable && ConfigurationManager.AppSettings[prefix + "EnableLiveModels"].InvariantEquals("true");
            //default is true
            FlagOutOfDateModels = Enable && ConfigurationManager.AppSettings[prefix + "FlagOutOfDateModels"].InvariantEquals("false") == false;

            //TODO: Re-enable this when we fix auth for VS
            EnableApi = false; //Enable && ConfigurationManager.AppSettings[prefix + "EnableApi"].InvariantEquals("true");

            // all of these have default: true
            EnableFactory = Enable && !ConfigurationManager.AppSettings[prefix + "EnableFactory"].InvariantEquals("false");
            StaticMixinGetters = Enable && !ConfigurationManager.AppSettings[prefix + "StaticMixinGetters"].InvariantEquals("false");

            // no default
            ModelsNamespace = ConfigurationManager.AppSettings[prefix + "ModelsNamespace"];

            // default: see DefaultStaticMixinGetterPattern const
            var value = ConfigurationManager.AppSettings[prefix + "StaticMixinGetterPattern"];
            if (!string.IsNullOrWhiteSpace(value))
                StaticMixinGetterPattern = value;

            // default: see DefaultLanguageVersion const
            value = ConfigurationManager.AppSettings[prefix + "LanguageVersion"];
            if (!string.IsNullOrWhiteSpace(value))
            {
                LanguageVersion lv;
                if (!Enum.TryParse(value, true, out lv))
                    throw new ConfigurationErrorsException($"Invalid language version \"{value}\".");
                LanguageVersion = lv;
            }

            var count =
                (EnableDllModels ? 1 : 0)
                + (EnableAppCodeModels ? 1 : 0)
                + (EnableAppDataModels ? 1 : 0);

            if (count > 1)
                throw new ConfigurationErrorsException("You can enable only one of Dll, AppCode or AppData models at a time.");

            // live alone = pure live
            // live + app_data = just generate files
            // live + dll = generate the dll (causes restart)
            // live + app_code = generate the files, and restart

            // not flagging if not generating, or live
            if (count == 0 || EnableLiveModels)
                FlagOutOfDateModels = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Config"/> class.
        /// </summary>
        public Config(
            bool enable = false, 
            bool enableDllModels = false,
            bool enableAppCodeModels = false,
            bool enableAppDataModels = false,
            bool enableLiveModels = false,
            bool enableApi = true,
            string modelsNamespace = null,
            bool enableFactory = true,
            LanguageVersion languageVersion = DefaultLanguageVersion,
            bool staticMixinGetters = true,
            string staticMixinGetterPattern = DefaultStaticMixinGetterPattern,
            bool flagOutOfDateModels = true)
        {
            Enable = enable;
            EnableDllModels = enableDllModels;
            EnableAppCodeModels = enableAppCodeModels;
            EnableAppDataModels = enableAppDataModels;
            EnableLiveModels = enableLiveModels;
            EnableApi = enableApi;
            ModelsNamespace = modelsNamespace;
            EnableFactory = enableFactory;
            LanguageVersion = languageVersion;
            StaticMixinGetters = staticMixinGetters;
            StaticMixinGetterPattern = staticMixinGetterPattern;
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
        /// Gets a value indicating whether "Dll models" are enabled.
        /// </summary>
        /// <remarks>
        ///     <para>Indicates whether a dll containing the models should be generated in ~/bin by compiling
        ///     the models created in App_Data.</para>
        ///     <para>When "Dll models" is enabled, the dashboard shows the "generate" button so that
        ///     models can be generated in App_Data/Models and then compiled in a dll.</para>
        ///     <para>Default value is <c>false</c> because once enabled, Umbraco will restart anytime models
        ///     are re-generated from the dashboard. This is probably what you want to do, but we're forcing
        ///     you to make a concious decision at the moment.</para>
        /// </remarks>
        public bool EnableDllModels { get; }

        /// <summary>
        /// Gets a value indicating whether "App_Code models" are enabled. 
        /// </summary>
        /// <remarks>
        ///     <para>Indicates whether a "build.models" file should be created in App_Code and associated
        ///     to a build provider so that models created in App_Data are automatically included in the site
        ///     build and made available to the view.</para>
        ///     <para>When "App_Code models" is enabled, the dashboard shows the "generate" button so that
        ///     models can be generated in App_Data/Models.</para>
        ///     <para>Default value is <c>false</c> because once enabled, Umbraco will restart anytime models
        ///     are re-generated from the dashboard. This is probably what you want to do, but we're forcing
        ///     you to make a concious decision at the moment.</para>
        /// </remarks>
        public bool EnableAppCodeModels { get; }

        /// <summary>
        /// Gets a value indicating whether "App_Data models" are enabled.
        /// </summary>
        /// <remarks>
        ///     <para>Default value is <c>false</c>.</para>
        ///     <para>When "App_Data models" is enabled, the dashboard shows the "generate" button so that
        ///     models can be generated in App_Data/Models. Nothing else happens so the site does not restart.</para>
        /// </remarks>
        public bool EnableAppDataModels { get; }

        /// <summary>
        /// Gets a value indicating whether "live models" are enabled.
        /// </summary>
        /// <remarks>
        ///     <para>When App_Data models are not enabled, indicates whether models
        ///     should be automatically generated (in-memory), compiled and loaded into an assembly
        ///     referenced by our custom Razor engine, so they are available to views and are updated
        ///     when content types change, without Umbraco restarting.</para>
        ///     <para>When App_Data models are enabled, indicates whether models
        ///     should be automatically generated anytime a content type changes, see EnablePureLiveModels
        ///     below.</para>
        ///     <para>Default value is <c>false</c>.</para>
        /// </remarks>
        public bool EnableLiveModels { get; }

        /// <summary>
        /// Gets a value indicating whether only "live models" are enabled.
        /// </summary>
        /// <remarks>
        ///     <para>When true neither Dll, App_Data nor App_Code models are enabled and we want our
        ///     custom Razor engine do handle models.</para>
        /// </remarks>
        public bool EnablePureLiveModels => EnableLiveModels && !EnableAppDataModels && !EnableDllModels && !EnableAppCodeModels;

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
        /// <remarks>No default value. That value could be overriden by other (attribute in user's code...).</remarks>
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
