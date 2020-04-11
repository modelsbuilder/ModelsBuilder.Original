using System.IO;
using Our.ModelsBuilder.Options;
using Umbraco.Core;
using Umbraco.Core.Composing;
using Umbraco.Web.Cache;

namespace Our.ModelsBuilder.Umbraco
{
    public sealed class OutOfDateModelsStatus
    {
        private static ModelsBuilderOptions Options => Current.Factory.GetInstance<ModelsBuilderOptions>();

        internal static void Install()
        {
            // just be sure
            if (Options.FlagOutOfDateModels == false)
                return;

            ContentTypeCacheRefresher.CacheUpdated += (sender, args) => Write();
            DataTypeCacheRefresher.CacheUpdated += (sender, args) => Write();
        }

        private static string GetFlagPath()
        {
            var modelsDirectory = Options.ModelsDirectory;
            if (!Directory.Exists(modelsDirectory))
                Directory.CreateDirectory(modelsDirectory);
            return Path.Combine(modelsDirectory, "ood.flag");
        }

        private static void Write()
        {
            var path = GetFlagPath();
            if (path == null || File.Exists(path)) return;
            File.WriteAllText(path, "THIS FILE INDICATES THAT MODELS ARE OUT-OF-DATE\n\n");
        }

        public static void Clear()
        {
            if (Options.FlagOutOfDateModels == false) return;
            var path = GetFlagPath();
            if (path == null || !File.Exists(path)) return;
            File.Delete(path);
        }

        public static bool IsEnabled => Options.FlagOutOfDateModels;

        public static bool IsOutOfDate
        {
            get
            {
                if (Options.FlagOutOfDateModels == false) return false;
                var path = GetFlagPath();
                return path != null && File.Exists(path);
            }
        }
    }
}
