using System;
using System.Configuration;
using System.IO;
using System.Web.Configuration;
using System.Web.Hosting;
using Microsoft.CodeAnalysis.CSharp;
using Umbraco.Core;

namespace ZpqrtBnk.ModelsBuilder.Configuration
{
    /// <summary>
    /// Represents the models builder configuration.
    /// </summary>
    public class Config
    {
        // the master prefix for all appSetting entries
        private const string prefix = "ZpqrtBnk.ModelsBuilder.";

        // default values for options
        internal const bool DefaultEnable = false;
        internal const bool DefaultEnableApi = false;
        internal const bool DefaultEnableBackOffice = true;
        internal const bool DefaultEnableFactory = true;

        internal const ModelsMode DefaultModelsMode = ModelsMode.Nothing;

        internal const bool DefaultAcceptUnsafeModelsDirectory = false;
        internal const bool DefaultFlagOutOfDateModels = true;

        internal const int DefaultDebugLevel = 0;

        internal const LanguageVersion DefaultLanguageVersion = LanguageVersion.CSharp7_3;
        internal const string DefaultModelsNamespace = "Umbraco.Web.PublishedModels";
        internal const string DefaultModelsDirectory = "~/App_Data/Models";

        /// <summary>
        /// Initializes a new instance of the <see cref="Config"/> class.
        /// </summary>
        public Config()
        {
            // giant kill switch, default: false
            // must be explicitely set to true for anything else to happen
            Enable = GetSetting("Enable", DefaultEnable);

            // switches
            EnableApi = GetSetting("EnableApi", DefaultEnableApi);
            EnableBackOffice = GetSetting("EnableBackOffice", DefaultEnableBackOffice);
            EnableFactory = GetSetting("EnableFactory", DefaultEnableFactory);

            // mode
            ModelsMode = GetSetting("ModelsMode", DefaultModelsMode);

            // more switches
            AcceptUnsafeModelsDirectory = GetSetting("AcceptUnsafeModelsDirectory", DefaultAcceptUnsafeModelsDirectory);
            FlagOutOfDateModels = GetSetting("FlagOutOfDateModels", DefaultFlagOutOfDateModels);

            // strings
            ModelsNamespace = GetSetting("ModelsNamespace", DefaultModelsNamespace);

            // others
            DebugLevel = GetSetting("DebugLevel", DefaultDebugLevel);
            LanguageVersion = GetSetting("LanguageVersion", DefaultLanguageVersion);

            // directory
            var directory = GetSetting("ModelsDirectory", "");
            if (string.IsNullOrWhiteSpace(directory))
            {
                ModelsDirectory = HostingEnvironment.IsHosted
                    ? HostingEnvironment.MapPath(DefaultModelsDirectory)
                    : DefaultModelsDirectory.TrimStart("~/");
            }
            else
            {
                var root = HostingEnvironment.IsHosted
                    ? HostingEnvironment.MapPath("~/")
                    : Directory.GetCurrentDirectory();
                if (root == null)
                    throw new ConfigurationErrorsException("Could not determine root directory.");

                // GetModelsDirectory will ensure that the path is safe
                ModelsDirectory = GetModelsDirectory(root, directory, AcceptUnsafeModelsDirectory);
            }

            // not flagging if not generating, or live (incl. pure)
            if (ModelsMode == ModelsMode.Nothing || ModelsMode.IsLive())
                FlagOutOfDateModels = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Config"/> class.
        /// </summary>
        public Config(
            bool enable = DefaultEnable,
            ModelsMode modelsMode = DefaultModelsMode,
            bool enableBackOffice = DefaultEnableBackOffice,
            bool enableApi = DefaultEnableApi,
            string modelsNamespace = DefaultModelsNamespace,
            bool enableFactory = DefaultEnableFactory,
            LanguageVersion languageVersion = DefaultLanguageVersion,
            bool flagOutOfDateModels = DefaultFlagOutOfDateModels,
            string modelsDirectory = DefaultModelsDirectory,
            bool acceptUnsafeModelsDirectory = DefaultAcceptUnsafeModelsDirectory,
            int debugLevel = DefaultDebugLevel)
        {
            Enable = enable;
            ModelsMode = modelsMode;

            EnableBackOffice = enableBackOffice;
            EnableApi = enableApi;
            ModelsNamespace = modelsNamespace;
            EnableFactory = enableFactory;
            LanguageVersion = languageVersion;
            FlagOutOfDateModels = flagOutOfDateModels;
            ModelsDirectory = modelsDirectory;
            AcceptUnsafeModelsDirectory = acceptUnsafeModelsDirectory;
            DebugLevel = debugLevel;
        }

        // reads a bool setting
        private static bool GetSetting(string name, bool defaultValue)
        {
            name = prefix + name;
            var setting = ConfigurationManager.AppSettings[name];
            if (string.IsNullOrWhiteSpace(setting)) 
                return defaultValue;

            if (!bool.TryParse(setting, out var value))
                throw new ConfigurationErrorsException($"Invalid value for setting '{name}': cannot parse '{setting}' as a boolean.");

            return value;
        }

        // reads a string setting
        private static string GetSetting(string name, string defaultValue)
        {
            name = prefix + name;
            var setting = ConfigurationManager.AppSettings[name];
            return string.IsNullOrWhiteSpace(setting) ? defaultValue : setting;
        }

        // reads an int setting
        private static int GetSetting(string name, int defaultValue)
        {
            name = prefix + name;
            var setting = ConfigurationManager.AppSettings[name];
            if (string.IsNullOrWhiteSpace(setting))
                return defaultValue;

            if (!int.TryParse(setting, out var value))
                throw new ConfigurationErrorsException($"Invalid value for setting '{name}': cannot parse '{setting}' as an integer.");

            return value;
        }

        // reads a LanguageVersion setting
        private static LanguageVersion GetSetting(string name, LanguageVersion defaultValue)
        {
            name = prefix + name;
            var setting = ConfigurationManager.AppSettings[name];
            if (string.IsNullOrWhiteSpace(setting))
                return defaultValue;

            if (!Enum.TryParse<LanguageVersion>(setting, true, out var value))
                throw new ConfigurationErrorsException($"Invalid value for setting '{name}': cannot parse '{setting}' as a language version.");

            return value;
        }

        // reads the mode setting
        private static ModelsMode GetSetting(string name, ModelsMode defaultValue)
        {
            name = prefix + name;
            var setting = ConfigurationManager.AppSettings[name];
            if (string.IsNullOrWhiteSpace(setting))
                return defaultValue;

            switch (setting)
            {
                case nameof(ModelsMode.Nothing):
                    return ModelsMode.Nothing;
                case nameof(ModelsMode.PureLive):
                    return ModelsMode.PureLive;
                case nameof(ModelsMode.Dll):
                    return ModelsMode.Dll;
                case nameof(ModelsMode.LiveDll):
                    return ModelsMode.LiveDll;
                case nameof(ModelsMode.AppData):
                    return ModelsMode.AppData;
                case nameof(ModelsMode.LiveAppData):
                    return ModelsMode.LiveAppData;
                default:
                    throw new ConfigurationErrorsException($"ModelsMode \"{setting}\" is not a valid mode."
                        + " Note that modes are case-sensitive. Possible values are: " + string.Join(", ", Enum.GetNames(typeof(ModelsMode))));
            }
        }

        // reads the name source setting
        private static ClrNameSource GetSetting(string name, ClrNameSource defaultValue)
        {
            name = prefix + name;
            var setting = ConfigurationManager.AppSettings[name];
            if (string.IsNullOrWhiteSpace(setting))
                return defaultValue;

            switch (setting)
            {
                case nameof(ClrNameSource.Nothing):
                    return ClrNameSource.Nothing;
                case nameof(ClrNameSource.Alias):
                    return ClrNameSource.Alias;
                case nameof(ClrNameSource.RawAlias):
                    return ClrNameSource.RawAlias;
                case nameof(ClrNameSource.Name):
                    return ClrNameSource.Name;
                default:
                    throw new ConfigurationErrorsException($"ClrNameSource \"{setting}\" is not a valid source."
                        + " Note that sources are case-sensitive. Possible values are: " + string.Join(", ", Enum.GetNames(typeof(ClrNameSource))));
            }
        }

        // internal for tests
        internal static string GetModelsDirectory(string root, string config, bool acceptUnsafe)
        {
            // making sure it is safe, ie under the website root,
            // unless AcceptUnsafeModelsDirectory and then everything is OK.

            if (!Path.IsPathRooted(root))
                throw new ConfigurationErrorsException($"Root is not rooted \"{root}\".");

            if (config.StartsWith("~/"))
            {
                var dir = HostingEnvironment.IsHosted
                    ? HostingEnvironment.MapPath(config)
                    : Path.Combine(root, config.TrimStart("~/"));

                // sanitize - GetFullPath will take care of any relative
                // segments in path, eg '../../foo.tmp' - it may throw a SecurityException
                // if the combined path reaches illegal parts of the filesystem
                dir = Path.GetFullPath(dir);
                root = Path.GetFullPath(root);

                if (!dir.StartsWith(root) && !acceptUnsafe)
                    throw new ConfigurationErrorsException($"Invalid models directory \"{config}\".");

                return dir;
            }

            if (acceptUnsafe)
                return Path.GetFullPath(config);

            throw new ConfigurationErrorsException($"Invalid models directory \"{config}\".");
        }

        #region Config options

        /// <summary>
        /// Gets a value indicating whether the whole models experience is enabled.
        /// </summary>
        /// <remarks>
        ///     <para>If this is false then absolutely nothing happens.</para>
        ///     <para>Default value is <c>false</c> which means that unless we have this setting, nothing happens.</para>
        /// </remarks>
        public bool Enable { get; }

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
        /// Gets a value indicating whether backoffice integration is enabled.
        /// </summary>
        public bool EnableBackOffice { get; }

        /// <summary>
        /// Gets a value indicating whether we should enable the models factory.
        /// </summary>
        /// <remarks>Default value is <c>true</c> because no factory is enabled by default in Umbraco.</remarks>
        public bool EnableFactory { get; }


        /// <summary>
        /// Gets the models mode.
        /// </summary>
        public ModelsMode ModelsMode { get; }

        /// <summary>
        /// Gets the models namespace.
        /// </summary>
        /// <remarks>That value could be overriden by other (attribute in user's code...). Return default if no value was supplied.</remarks>
        public string ModelsNamespace { get; }

        /// <summary>
        /// Gets the Roslyn parser language version.
        /// </summary>
        /// <remarks>Default value is <c>CSharp6</c>.</remarks>
        public LanguageVersion LanguageVersion { get; }

        /// <summary>
        /// Gets a value indicating whether we should flag out-of-date models.
        /// </summary>
        /// <remarks>Models become out-of-date when data types or content types are updated. When this
        /// setting is activated the ~/App_Data/Models/ood.txt file is then created. When models are
        /// generated through the dashboard, the files is cleared. Default value is <c>false</c>.</remarks>
        public bool FlagOutOfDateModels { get; }

        /// <summary>
        /// Gets the models directory.
        /// </summary>
        /// <remarks>Default is ~/App_Data/Models but that can be changed.</remarks>
        public string ModelsDirectory { get; }

        /// <summary>
        /// Gets a value indicating whether to accept an unsafe value for ModelsDirectory.
        /// </summary>
        /// <remarks>An unsafe value is an absolute path, or a relative path pointing outside
        /// of the website root.</remarks>
        public bool AcceptUnsafeModelsDirectory { get; }

        /// <summary>
        /// Gets a value indicating the debug log level.
        /// </summary>
        /// <remarks>0 means minimal (safe on live site), anything else means more and more details (maybe not safe).</remarks>
        public int DebugLevel { get; }

        #endregion

        #region Utilities

        /// <summary>
        /// Gets a value indicating whether to serve the API.
        /// </summary>
        public bool IsApiServer => EnableApi && IsDebug;

        /// <summary>
        /// Gets a value indicating whether system.web/compilation/@debug is true.
        /// </summary>
        public bool IsDebug
        {
            get
            {
                var section = (CompilationSection)ConfigurationManager.GetSection("system.web/compilation");
                return section != null && section.Debug;
            }
        }
        
        #endregion
    }
}
