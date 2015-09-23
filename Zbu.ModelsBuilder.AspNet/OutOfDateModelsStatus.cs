using System;
using System.IO;
using System.Web.Hosting;
using Umbraco.Core;
using Umbraco.Web.Cache;
using Zbu.ModelsBuilder.Configuration;

namespace Zbu.ModelsBuilder.AspNet
{
    public class OutOfDateModelsStatus : ApplicationEventHandler
    {
        protected override void ApplicationStarted(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        {
            if (Config.FlagOutOfDateModels == false) return;
            ContentTypeCacheRefresher.CacheUpdated += (sender, args) => Write();
            DataTypeCacheRefresher.CacheUpdated += (sender, args) => Write();
        }

        private static string GetFlagPath()
        {
            var appData = HostingEnvironment.MapPath("~/App_Data");
            if (appData == null) throw new Exception("Panic: appData is null.");
            var modelsDirectory = Path.Combine(appData, "Models");
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
            if (Config.FlagOutOfDateModels == false) return;
            var path = GetFlagPath();
            if (path == null || !File.Exists(path)) return;
            File.Delete(path);
        }

        public static bool IsEnabled
        {
            get { return Config.FlagOutOfDateModels; }
        }

        public static bool IsOutOfDate
        {
            get
            {
                if (Config.FlagOutOfDateModels == false) return false;
                var path = GetFlagPath();
                return path != null && File.Exists(path);
            }
        }
    }
}
