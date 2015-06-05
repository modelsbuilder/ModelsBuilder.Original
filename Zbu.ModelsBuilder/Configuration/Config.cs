using System;
using System.Configuration;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;

namespace Zbu.ModelsBuilder.Configuration
{
    public static class Config
    {
        static Config()
        {
            // for the time being config is stored in web.config appSettings
            // and is static ie requires the app to be restarted for changes to be detected

            const string prefix = "Zbu.ModelsBuilder.";
            EnableDllModels = ConfigurationManager.AppSettings[prefix + "EnableDllModels"] == "true";
            EnableAppCodeModels = ConfigurationManager.AppSettings[prefix + "EnableAppCodeModels"] == "true";
            EnableAppDataModels = ConfigurationManager.AppSettings[prefix + "EnableAppDataModels"] == "true";
            EnableLiveModels = ConfigurationManager.AppSettings[prefix + "EnableLiveModels"] == "true";
            EnableApi = ConfigurationManager.AppSettings[prefix + "EnableApi"] != "false";
            ModelsNamespace = ConfigurationManager.AppSettings[prefix + "ModelsNamespace"];
            EnablePublishedContentModelsFactory = ConfigurationManager.AppSettings[prefix + "EnablePublishedContentModelsFactory"] != "false";

            StaticMixinGetters = ConfigurationManager.AppSettings[prefix + "StaticMixinGetters"] == "true";
            StaticMixinGetterPattern = ConfigurationManager.AppSettings[prefix + "StaticMixinGetterPattern"];
            if (string.IsNullOrWhiteSpace(StaticMixinGetterPattern))
                StaticMixinGetterPattern = "Get{0}";

            LanguageVersion = LanguageVersion.CSharp5;
            var lvSetting = ConfigurationManager.AppSettings[prefix + "LanguageVersion"];
            if (!string.IsNullOrWhiteSpace(lvSetting))
            {
                LanguageVersion lv;
                if (!Enum.TryParse(lvSetting, true, out lv))
                    throw new ConfigurationErrorsException(string.Format("Invalid language version \"{0}\".", lvSetting));
                LanguageVersion = lv;
            }

            var count =
                (EnableDllModels ? 1 : 0)
                + (EnableAppCodeModels ? 1 : 0)
                + (EnableAppDataModels ? 1 : 0);

            if (count > 1)
                throw new Exception("Configuration error: you can enable only one of Dll, AppCode or AppData models at a time.");
        }

        // note: making setters internal below for testing purposes

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
        public static bool EnableDllModels { get; internal set; }

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
        public static bool EnableAppCodeModels { get; internal set; }

        /// <summary>
        /// Gets a value indicating whether "App_Data models" are enabled.
        /// </summary>
        /// <remarks>
        ///     <para>Default value is <c>false</c>.</para>
        ///     <para>When "App_Data models" is enabled, the dashboard shows the "generate" button so that
        ///     models can be generated in App_Data/Models. Nothing else happens so the site does not restart.</para>
        /// </remarks>
        public static bool EnableAppDataModels { get; internal set; }

        /// <summary>
        /// Gets a value indicating whether "live models" are enabled.
        /// </summary>
        /// <remarks>
        ///     <para>When neither Dll, App_Data nor App_Code models are enabled, indicates whether models
        ///     should be automatically generated (in-memory), compiled and loaded into an assembly
        ///     referenced by our custom Razor engine, so they are available to views and are updated
        ///     when content types change, without Umbraco restarting.</para>
        ///     <para>When either Dll, App_Data or App_Code models are enabled, indicates whether models
        ///     should be automatically generated anytime a content type changes, see EnablePureLiveModels
        ///     below.</para>
        ///     <para>Default value is <c>false</c>.</para>
        /// </remarks>
        public static bool EnableLiveModels { get; internal set; }

        /// <summary>
        /// Gets a value indicating whether only "live models" are enabled.
        /// </summary>
        /// <remarks>
        ///     <para>When true neither Dll, App_Data nor App_Code models are enabled and we want our
        ///     custom Razor engine do handle models - NOTE: pure live models are disabled because
        ///     they sort of don't work.</para>
        /// </remarks>
        public static bool EnablePureLiveModels
        {
            get { return false; }
            //get { return EnableLiveModels && !(EnableAppCodeModels || EnableDllModels || EnableAppDataModels); }
        }

        /// <summary>
        /// Gets a value indicating whether to enable the API.
        /// </summary>
        /// <remarks>
        ///     <para>Default value is <c>true</c>.</para>
        ///     <para>The API is used by the Visual Studio extension and the console tool to talk to Umbraco
        ///     and retrieve the content types. It needs to be enabled so the extension & tool can work.</para>
        /// </remarks>
        public static bool EnableApi { get; internal set; }

        /// <summary>
        /// Gets the models namespace.
        /// </summary>
        /// <remarks>No default value. That value could be overriden by other (attribute in user's code...).</remarks>
        public static string ModelsNamespace { get; internal set; }

        /// <summary>
        /// Gets a value indicating whether we should enable the models factory.
        /// </summary>
        /// <remarks>Default value is <c>true</c> because no factory is enabled by default in Umbraco.</remarks>
        public static bool EnablePublishedContentModelsFactory { get; internal set; }

        /// <summary>
        /// Gets the Roslyn parser language version.
        /// </summary>
        /// <remarks>Default value is <c>CSharp5</c>.</remarks>
        public static LanguageVersion LanguageVersion { get; internal set; }

        /// <summary>
        /// Gets a value indicating whether to generate static mixin getters.
        /// </summary>
        /// <remarks>Default value is <c>false</c> for backward compat reaons.</remarks>
        public static bool StaticMixinGetters { get; internal set; }

        /// <summary>
        /// Gets the string pattern for mixin properties static getter name.
        /// </summary>
        /// <remarks>Default value is "GetXxx". Standard string format.</remarks>
        public static string StaticMixinGetterPattern { get; internal set; }
    }
}
