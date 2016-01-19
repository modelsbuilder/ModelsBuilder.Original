using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Web.Compilation;
using System.Web.Hosting;
using Umbraco.Core;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web.Cache;
using Umbraco.ModelsBuilder.AspNet;
using Umbraco.ModelsBuilder.Building;
using Umbraco.ModelsBuilder.Configuration;

namespace Umbraco.ModelsBuilder.Umbraco
{
    public class PureLiveModelFactory : IPublishedContentModelFactory
    {
        private Dictionary<string, Func<IPublishedContent, IPublishedContent>> _constructors;
        private readonly ReaderWriterLockSlim _locker = new ReaderWriterLockSlim();
        private readonly IPureLiveModelsEngine[] _engines;
        private bool _hasModels;

        public PureLiveModelFactory(params IPureLiveModelsEngine[] engines)
        {
            _engines = engines;
            ContentTypeCacheRefresher.CacheUpdated += (sender, args) => ResetModels();
            DataTypeCacheRefresher.CacheUpdated += (sender, args) => ResetModels();
        }

        #region IPublishedContentModelFactory

        public IPublishedContent CreateModel(IPublishedContent content)
        {
            // get models, rebuilding them if needed
            var constructors = EnsureModels();
            if (constructors == null)
                return null;

            // be case-insensitive
            var contentTypeAlias = content.DocumentTypeAlias;

            // lookup model constructor (else null)
            Func<IPublishedContent, IPublishedContent> constructor;
            constructors.TryGetValue(contentTypeAlias, out constructor);

            // create model
            return constructor == null ? content : constructor(content);
        }

        #endregion

        #region Compilation

        // tells the factory that it should build a new generation of models
        private void ResetModels()
        {
            LogHelper.Debug<PureLiveModelFactory>("Resetting models.");
            _locker.EnterWriteLock();
            try
            {
                _hasModels = false;
            }
            finally
            {
                _locker.ExitWriteLock();
            }
        }

        // ensure that the factory is running with the lastest generation of models
        private Dictionary<string, Func<IPublishedContent, IPublishedContent>> EnsureModels()
        {
            LogHelper.Debug<PureLiveModelFactory>("Ensuring models.");
            _locker.EnterReadLock();
            try
            {
                if (_hasModels) return _constructors;
            }
            finally
            {
                _locker.ExitReadLock();
            }

            _locker.EnterWriteLock();
            try
            {
                if (_hasModels) return _constructors;

                LogHelper.Debug<PureLiveModelFactory>("Rebuilding models.");

                // this will lock the engines - must take care that whatever happens,
                // we unlock them - even if generation failed for some reason
                foreach (var engine in _engines)
                    engine.NotifyRebuilding();

                try
                {
                    var code = GenerateModelsCode();
                    var assembly = RoslynRazorViewCompiler.CompileAndRegisterModels(code);
                    var types = assembly.ExportedTypes.Where(x => x.Inherits<PublishedContentModel>());

                    _constructors = RegisterModels(types);
                    _hasModels = true;
                }
                finally
                {
                    foreach (var engine in _engines)
                        engine.NotifyRebuilt();
                }

                LogHelper.Debug<PureLiveModelFactory>("Done rebuilding.");
                return _constructors;
            }
            finally
            {                
                _locker.ExitWriteLock();
            }
        }

        private static Dictionary<string, Func<IPublishedContent, IPublishedContent>> RegisterModels(IEnumerable<Type> types)
        {
            var ctorArgTypes = new[] { typeof(IPublishedContent) };
            var constructors = new Dictionary<string, Func<IPublishedContent, IPublishedContent>>(StringComparer.InvariantCultureIgnoreCase);

            foreach (var type in types)
            {
                var constructor = type.GetConstructor(ctorArgTypes);
                if (constructor == null)
                    throw new InvalidOperationException(string.Format("Type {0} is missing a public constructor with one argument of type IPublishedContent.", type.FullName));
                var attribute = type.GetCustomAttribute<PublishedContentModelAttribute>(false);
                var typeName = attribute == null ? type.Name : attribute.ContentTypeAlias;

                if (constructors.ContainsKey(typeName))
                    throw new InvalidOperationException(string.Format("More that one type want to be a model for content type {0}.", typeName));

                var exprArg = Expression.Parameter(typeof(IPublishedContent), "content");
                var exprNew = Expression.New(constructor, exprArg);
                var expr = Expression.Lambda<Func<IPublishedContent, IPublishedContent>>(exprNew, exprArg);
                var func = expr.Compile();
                constructors[typeName] = func;
            }

            return constructors.Count > 0 ? constructors : null;
        }

        private static string GenerateModelsCode()
        {
            var appData = HostingEnvironment.MapPath("~/App_Data");
            if (appData == null)
                throw new Exception("Panic: appData is null.");

            var modelsDirectory = Path.Combine(appData, "Models");
            if (!Directory.Exists(modelsDirectory))
                Directory.CreateDirectory(modelsDirectory);

            foreach (var file in Directory.GetFiles(modelsDirectory, "*.generated.cs"))
                System.IO.File.Delete(file);

            var ourFiles = Directory.Exists(modelsDirectory)
                ? Directory.GetFiles(modelsDirectory, "*.cs").ToDictionary(x => x, System.IO.File.ReadAllText)
                : new Dictionary<string, string>();

            var umbraco = Application.GetApplication();
            var typeModels = umbraco.GetAllTypes();

            // using BuildManager references
            var referencedAssemblies = BuildManager.GetReferencedAssemblies().Cast<Assembly>().ToArray();

            var parseResult = new CodeParser().Parse(ourFiles, referencedAssemblies);
            var builder = new TextBuilder(typeModels, parseResult, UmbracoConfig.For.ModelsBuilder().ModelsNamespace);

            var codeBuilder = new StringBuilder();
            builder.Generate(codeBuilder, builder.GetModelsToGenerate());
            var code = codeBuilder.ToString();

            // save code for debug purposes
            System.IO.File.WriteAllText(Path.Combine(modelsDirectory, "models.generated.cs"), code);

            return code;
        }

        #endregion
    }
}
