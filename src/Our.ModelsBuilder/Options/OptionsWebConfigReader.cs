using System;
using System.Configuration;
using System.IO;
using System.Web.Hosting;
using Microsoft.CodeAnalysis.CSharp;
using Umbraco.Core;

namespace Our.ModelsBuilder.Options
{
    public static class OptionsWebConfigReader
    {
        // the master prefix for all appSetting entries
        private const string AppSettingsPrefix = "Our.ModelsBuilder.";

        public static void ConfigureOptions(ModelsBuilderOptions options)
        {
            // giant kill switch, default: false
            // must be explicitly set to true for anything else to happen
            options.Enable = GetSetting("Enable", options.Enable);

            // switches
            options.EnableApi = GetSetting("EnableApi", options.EnableApi);
            options.EnableBackOffice = GetSetting("EnableBackOffice", options.EnableBackOffice);
            options.EnableFactory = GetSetting("EnableFactory", options.EnableFactory);

            // mode
            options.ModelsMode = GetSetting("ModelsMode", options.ModelsMode);

            // more switches
            options.AcceptUnsafeModelsDirectory = GetSetting("AcceptUnsafeModelsDirectory", options.AcceptUnsafeModelsDirectory);
            options.FlagOutOfDateModels = GetSetting("FlagOutOfDateModels", options.FlagOutOfDateModels);

            // strings
            options.ModelsNamespace = GetSetting("ModelsNamespace", options.ModelsNamespace);

            // others
            options.DebugLevel = GetSetting("DebugLevel", options.DebugLevel);
            options.LanguageVersion = GetSetting("LanguageVersion", options.LanguageVersion);

            // directory
            var directory = GetSetting("ModelsDirectory", "");
            if (string.IsNullOrWhiteSpace(directory))
            {
            
                options.ModelsDirectory = HostingEnvironment.IsHosted
                    ? HostingEnvironment.MapPath(options.ModelsDirectory)
                    : options.ModelsDirectory.TrimStart("~/");
            }
            else
            {
                var root = HostingEnvironment.IsHosted
                    ? HostingEnvironment.MapPath("~/")
                    : Directory.GetCurrentDirectory();
                if (root == null)
                    throw new ConfigurationErrorsException("Could not determine root directory.");

                // GetModelsDirectory will ensure that the path is safe
                options.ModelsDirectory = GetModelsDirectory(root, directory, options.AcceptUnsafeModelsDirectory);
            }

            // not flagging if not generating, or live (incl. pure)
            if (options.ModelsMode == ModelsMode.Nothing || options.ModelsMode.IsLive())
                options.FlagOutOfDateModels = false;
        }

        // reads a bool setting
        private static bool GetSetting(string name, bool defaultValue)
        {
            name = AppSettingsPrefix + name;
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
            name = AppSettingsPrefix + name;
            var setting = ConfigurationManager.AppSettings[name];
            return string.IsNullOrWhiteSpace(setting) ? defaultValue : setting;
        }

        // reads an int setting
        private static int GetSetting(string name, int defaultValue)
        {
            name = AppSettingsPrefix + name;
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
            name = AppSettingsPrefix + name;
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
            name = AppSettingsPrefix + name;
            var setting = ConfigurationManager.AppSettings[name];
            if (string.IsNullOrWhiteSpace(setting))
                return defaultValue;

            return setting switch
            {
                nameof(ModelsMode.Nothing) => ModelsMode.Nothing,
                nameof(ModelsMode.PureLive) => ModelsMode.PureLive,
                nameof(ModelsMode.Dll) => ModelsMode.Dll,
                nameof(ModelsMode.LiveDll) => ModelsMode.LiveDll,
                nameof(ModelsMode.AppData) => ModelsMode.AppData,
                nameof(ModelsMode.LiveAppData) => ModelsMode.LiveAppData,
                _ => throw new ConfigurationErrorsException($"ModelsMode \"{setting}\" is not a valid mode." + " Note that modes are case-sensitive. Possible values are: " + string.Join(", ", Enum.GetNames(typeof(ModelsMode))))
            };
        }

        // reads the name source setting
        private static ClrNameSource GetSetting(string name, ClrNameSource defaultValue)
        {
            name = AppSettingsPrefix + name;
            var setting = ConfigurationManager.AppSettings[name];
            if (string.IsNullOrWhiteSpace(setting))
                return defaultValue;

            return setting switch
            {
                nameof(ClrNameSource.Nothing) => ClrNameSource.Nothing,
                nameof(ClrNameSource.Alias) => ClrNameSource.Alias,
                nameof(ClrNameSource.RawAlias) => ClrNameSource.RawAlias,
                nameof(ClrNameSource.Name) => ClrNameSource.Name,
                _ => throw new ConfigurationErrorsException($"ClrNameSource \"{setting}\" is not a valid source." + " Note that sources are case-sensitive. Possible values are: " + string.Join(", ", Enum.GetNames(typeof(ClrNameSource))))
            };
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
                var isOutside =   config.StartsWith("~/..");
                if (isOutside && !acceptUnsafe)
                    throw new ConfigurationErrorsException($"Invalid models directory \"{config}\".");

                var path = isOutside
                    ? Path.GetFullPath(Path.Combine(HostingEnvironment.MapPath("~/") + config.TrimStart("~/")))
                    : HostingEnvironment.MapPath(config);
                var dir = HostingEnvironment.IsHosted
                    ? path
                    : Path.Combine(root, config.TrimStart("~/"));

                if (dir == null) throw new Exception("panic");

                // sanitize - GetFullPath will take care of any relative
                // segments in path, eg '../../foo.tmp' - it may throw a SecurityException
                // if the combined path reaches illegal parts of the filesystem
                dir = Path.GetFullPath(dir);
                
                return dir;
            }

            if (acceptUnsafe)
                return Path.GetFullPath(config);

            throw new ConfigurationErrorsException($"Invalid models directory \"{config}\".");
        }
    }
}
