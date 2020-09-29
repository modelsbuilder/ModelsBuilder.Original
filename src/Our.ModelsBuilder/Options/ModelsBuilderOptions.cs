using System.Configuration;
using System.Web.Configuration;
using Microsoft.CodeAnalysis.CSharp;

namespace Our.ModelsBuilder.Options
{
    public class ModelsBuilderOptions
    {
        public class Defaults
        {
            public const bool Enable = false;
            public const bool EnableApi = false;
            public const bool EnableBackOffice = true;
            public const bool EnableFactory = true;
            public const ModelsMode ModelsMode = Options.ModelsMode.Nothing;
            public const bool AcceptUnsafeModelsDirectory = false;
            public const bool FlagOutOfDateModels = true;
            public const int DebugLevel = 0;
            public const LanguageVersion LanguageVersion = Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp7_3;
            public const string ModelsNamespace = "Umbraco.Web.PublishedModels";
            public const string ModelsDirectory = "~/App_Data/Models";
        }

        /// <summary>
        /// Gets a value indicating whether the whole models experience is enabled.
        /// </summary>
        /// <remarks>
        ///     <para>If this is false then absolutely nothing happens.</para>
        ///     <para>Default value is <c>false</c> which means that unless we have this setting, nothing happens.</para>
        /// </remarks>
        public bool Enable { get; set; } = Defaults.Enable;

        /// <summary>
        /// Gets a value indicating whether to enable the API.
        /// </summary>
        /// <remarks>
        ///     <para>Default value is <c>true</c>.</para>
        ///     <para>The API is used by the Visual Studio extension and the console tool to talk to Umbraco
        ///     and retrieve the content types. It needs to be enabled so the extension & tool can work.</para>
        /// </remarks>
        public bool EnableApi { get; set; } = Defaults.EnableApi;

        /// <summary>
        /// Gets a value indicating whether back-office integration is enabled.
        /// </summary>
        public bool EnableBackOffice { get; set; } = Defaults.EnableBackOffice;

        /// <summary>
        /// Gets a value indicating whether we should enable the models factory.
        /// </summary>
        /// <remarks>Default value is <c>true</c> because no factory is enabled by default in Umbraco.</remarks>
        public bool EnableFactory { get; set; } = Defaults.EnableFactory;

        /// <summary>
        /// Gets the models mode.
        /// </summary>
        public ModelsMode ModelsMode { get; set; } = Defaults.ModelsMode;

        /// <summary>
        /// Gets the models namespace.
        /// </summary>
        /// <remarks>That value could be overriden by other (attribute in user's code...). Return default if no value was supplied.</remarks>
        public string ModelsNamespace { get; set; } = Defaults.ModelsNamespace;
        public string AssemblyName { get; set; } = Defaults.ModelsNamespace;
        /// <summary>
        /// Gets the Roslyn parser language version.
        /// </summary>
        public LanguageVersion LanguageVersion { get; set; } = Defaults.LanguageVersion;

        /// <summary>
        /// Gets a value indicating whether we should flag out-of-date models.
        /// </summary>
        /// <remarks>Models become out-of-date when data types or content types are updated. When this
        /// setting is activated the ~/App_Data/Models/ood.txt file is then created. When models are
        /// generated through the dashboard, the files is cleared. Default value is <c>false</c>.</remarks>
        public bool FlagOutOfDateModels { get; set; } = Defaults.FlagOutOfDateModels;

        /// <summary>
        /// Gets the models directory.
        /// </summary>
        /// <remarks>Default is ~/App_Data/Models but that can be changed.</remarks>
        public string ModelsDirectory { get; set; } = Defaults.ModelsDirectory;

        /// <summary>
        /// Gets a value indicating whether to accept an unsafe value for ModelsDirectory.
        /// </summary>
        /// <remarks>An unsafe value is an absolute path, or a relative path pointing outside
        /// of the website root.</remarks>
        public bool AcceptUnsafeModelsDirectory { get; set; } = Defaults.AcceptUnsafeModelsDirectory;

        /// <summary>
        /// Gets a value indicating the debug log level.
        /// </summary>
        /// <remarks>0 means minimal (safe on live site), anything else means more and more details (maybe not safe).</remarks>
        public int DebugLevel { get; set; } = Defaults.DebugLevel;

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
                var section = (CompilationSection) ConfigurationManager.GetSection("system.web/compilation");
                return section != null && section.Debug;
            }
        }
    }
}
