using System;
using System.Configuration;

namespace Zbu.ModelsBuilder.Configuration
{
    public static class Config
    {
        static Config()
        {
            // for the time being config is stored in web.config appSettings
            // and is static ie requires the app to be restarted for changes to be detected

            const string prefix = "Zbu.ModelsBuilder.";
            EnableAppCodeModels = ConfigurationManager.AppSettings[prefix + "CreateAppCodeModelsFile"] == "true";
            EnableAppDataModels = ConfigurationManager.AppSettings[prefix + "EnableAppDataModels"] == "true";
            EnableLiveModels = ConfigurationManager.AppSettings[prefix + "EnableLiveModels"] == "true";
            EnableApi = ConfigurationManager.AppSettings[prefix + "EnableApi"] != "false";
            ModelsNamespace = ConfigurationManager.AppSettings[prefix + "ModelsNamespace"];
            EnablePublishedContentModelsFactory = ConfigurationManager.AppSettings[prefix + "EnablePublishedContentModelsFactory"] != "false";

            var count =
                (EnableAppCodeModels ? 1 : 0)
                + (EnableAppDataModels ? 1 : 0)
                + (EnableLiveModels ? 1 : 0);

            if (count > 1)
                throw new Exception("Configuration error: you can enable only one of AppCode, AppData or Live models at a time.");
        }

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
        public static bool EnableAppCodeModels { get; private set; }

        /// <summary>
        /// Gets a value indicating whether "App_Data models" are enabled.
        /// </summary>
        /// <remarks>
        ///     <para>Default value is <c>false</c>.</para>
        ///     <para>When "App_Data models" is enabled, the dashboard shows the "generate" button so that
        ///     models can be generated in App_Data/Models. Whether they will be just sitting there or loaded
        ///     and compiled depends on EnableAppCoreModels.</para>
        /// </remarks>
        public static bool EnableAppDataModels { get; private set; }

        /// <summary>
        /// Gets a value indicating whether "live models" are enabled.
        /// </summary>
        /// <remarks>
        ///     <para>Indicates whether the models created in App_Data are automatically compiled
        ///     and loaded into an assembly referenced by our custom Razor engine, so they are
        ///     available to views and are updated when content types change, without Umbraco
        ///     restarting.</para>
        ///     <para>Default value is <c>false</c>.</para>
        ///     <para>Enabling "live" models also enables "App_Data models".</para>
        /// </remarks>
        public static bool EnableLiveModels { get; private set; }

        /// <summary>
        /// Gets a value indicating whether to enable the API.
        /// </summary>
        /// <remarks>
        ///     <para>Default value is <c>true</c>.</para>
        ///     <para>The API is used by the Visual Studio extension and the console tool to talk to Umbraco
        ///     and retrieve the content types. It needs to be enabled so the extension & tool can work.</para>
        /// </remarks>
        public static bool EnableApi { get; private set; }

        /// <summary>
        /// Gets the models namespace.
        /// </summary>
        /// <remarks>No default value. That value could be overriden by other (attribute in user's code...).</remarks>
        public static string ModelsNamespace { get; private set; }

        /// <summary>
        /// Gets a value indicating whether we should enable the models factory.
        /// </summary>
        /// <remarks>Default value is <c>true</c> because no factory is enabled by default in Umbraco.</remarks>
        public static bool EnablePublishedContentModelsFactory { get; private set; }

    }
}
