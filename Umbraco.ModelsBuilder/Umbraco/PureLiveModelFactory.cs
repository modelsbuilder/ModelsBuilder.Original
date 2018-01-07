using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Web.Compilation;
using System.Web.Hosting;
using System.Web.WebPages.Razor;
using Umbraco.Core;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web.Cache;
using Umbraco.ModelsBuilder.Building;
using Umbraco.ModelsBuilder.Configuration;
using File = System.IO.File;

namespace Umbraco.ModelsBuilder.Umbraco
{
    internal class PureLiveModelFactory : IPublishedContentModelFactory, IRegisteredObject
    {
        private Assembly _modelsAssembly;
        private Dictionary<string, Func<IPublishedContent, IPublishedContent>> _constructors;
        private readonly ReaderWriterLockSlim _locker = new ReaderWriterLockSlim();
        private volatile bool _hasModels; // volatile 'cos reading outside lock
        private bool _pendingRebuild;
        private readonly ProfilingLogger _logger;
        private readonly FileSystemWatcher _watcher;
        private int _ver, _skipver;
        private readonly int _debugLevel;
        private BuildManager _theBuildManager;

        private static readonly Regex AssemblyVersionRegex = new Regex("AssemblyVersion\\(\"[0-9]+.[0-9]+.[0-9]+.[0-9]+\"\\)", RegexOptions.Compiled);
        private const string ProjVirt = "~/App_Data/Models/all.generated.cs";
        private static readonly string[] OurFiles = { "models.hash", "models.generated.cs", "all.generated.cs", "all.dll.path", "models.err" };

        public PureLiveModelFactory(ProfilingLogger logger)
        {
            _logger = logger;
            _ver = 1; // zero is for when we had no version
            _skipver = -1; // nothing to skip
            ContentTypeCacheRefresher.CacheUpdated += (sender, args) => ResetModels();
            DataTypeCacheRefresher.CacheUpdated += (sender, args) => ResetModels();
            RazorBuildProvider.CodeGenerationStarted += RazorBuildProvider_CodeGenerationStarted;

            if (!HostingEnvironment.IsHosted) return;

            var modelsDirectory = UmbracoConfig.For.ModelsBuilder().ModelsDirectory;
            if (!Directory.Exists(modelsDirectory))
                Directory.CreateDirectory(modelsDirectory);

            // BEWARE! if the watcher is not properly released then for some reason the
            // BuildManager will start confusing types - using a 'registered object' here
            // though we should probably plug into Umbraco's MainDom - which is internal
            HostingEnvironment.RegisterObject(this);
            _watcher = new FileSystemWatcher(modelsDirectory);
            _watcher.Changed += WatcherOnChanged;
            _watcher.EnableRaisingEvents = true;

            // get it here, this need to be fast
            _debugLevel = UmbracoConfig.For.ModelsBuilder().DebugLevel;
        }

        #region IPublishedContentModelFactory

        public IPublishedContent CreateModel(IPublishedContent content)
        {
            // get models, rebuilding them if needed
            var constructors = EnsureModels();
            if (constructors == null)
                return content;

            // be case-insensitive
            var contentTypeAlias = content.DocumentTypeAlias;

            // lookup model constructor (else null)
            constructors.TryGetValue(contentTypeAlias, out Func<IPublishedContent, IPublishedContent> constructor);

            // create model
            return constructor == null ? content : constructor(content);
        }

        #endregion

        #region Compilation

        // deadlock note
        //
        // when RazorBuildProvider_CodeGenerationStarted runs, the thread has Monitor.Enter-ed the BuildManager
        // singleton instance, through a call to CompilationLock.GetLock in BuildManager.GetVPathBuildResultInternal,
        // and now wants to lock _locker.
        // when EnsureModels runs, the thread locks _locker and then wants BuildManager to compile, which in turns
        // requires that the BuildManager can Monitor.Enter-ed itself.
        // so:
        //
        // T1 - needs to ensure models, locks _locker
        // T2 - needs to compile a view, locks BuildManager
        //      hits RazorBuildProvider_CodeGenerationStarted
        //      wants to lock _locker, wait
        // T1 - needs to compile models, using BuildManager
        //      wants to lock itself, wait
        // <deadlock>
        //
        // until ASP.NET kills the long-running request (thread abort)
        //
        // problem is, we *want* to suspend views compilation while the models assembly is being changed else we
        // end up with views compiled and cached with the old assembly, while models come from the new assembly,
        // which gives more YSOD. so we *have* to lock _locker in RazorBuildProvider_CodeGenerationStarted.
        //
        // one "easy" solution consists in locking the BuildManager *before* _locker in EnsureModels, thus ensuring
        // we always lock in the same order, and getting rid of deadlocks - but that requires having access to the
        // current BuildManager instance, which is BuildManager.TheBuildManager, which is an internal property.
        //
        // well, that's what we are doing in this class' TheBuildManager property, using reflection.

        private void RazorBuildProvider_CodeGenerationStarted(object sender, EventArgs e)
        {
            try
            {
                _locker.EnterReadLock();

                // just be safe - can happen if the first view is not an Umbraco view,
                // or if something went wrong and we don't have an assembly at all
                if (_modelsAssembly == null) return;

                if (_debugLevel > 0)
                    _logger.Logger.Debug<PureLiveModelFactory>("RazorBuildProvider.CodeGenerationStarted");
                var provider = sender as RazorBuildProvider;
                if (provider == null) return;

                // add the assembly, and add a dependency to a text file that will change on each
                // compilation as in some environments (could not figure which/why) the BuildManager
                // would not re-compile the views when the models assembly is rebuilt.
                provider.AssemblyBuilder.AddAssemblyReference(_modelsAssembly);
                provider.AddVirtualPathDependency(ProjVirt);
            }
            finally
            {
                if (_locker.IsReadLockHeld)
                    _locker.ExitReadLock();
            }
        }

        // tells the factory that it should build a new generation of models
        private void ResetModels()
        {
            _logger.Logger.Debug<PureLiveModelFactory>("Resetting models.");

            try
            {
                _locker.EnterWriteLock();

                _hasModels = false;
                _pendingRebuild = true;

                var modelsDirectory = UmbracoConfig.For.ModelsBuilder().ModelsDirectory;
                if (!Directory.Exists(modelsDirectory))
                    Directory.CreateDirectory(modelsDirectory);

                // clear stuff
                var modelsHashFile = Path.Combine(modelsDirectory, "models.hash");
                //var modelsSrcFile = Path.Combine(modelsDirectory, "models.generated.cs");
                //var projFile = Path.Combine(modelsDirectory, "all.generated.cs");
                var dllPathFile = Path.Combine(modelsDirectory, "all.dll.path");

                if (File.Exists(dllPathFile)) File.Delete(dllPathFile);
                if (File.Exists(modelsHashFile)) File.Delete(modelsHashFile);
            }
            finally
            {
                if (_locker.IsWriteLockHeld)
                    _locker.ExitWriteLock();
            }
        }

        // gets "the" build manager
        private BuildManager TheBuildManager
        {
            get
            {
                if (_theBuildManager != null) return _theBuildManager;
                var prop = typeof (BuildManager).GetProperty("TheBuildManager", BindingFlags.NonPublic | BindingFlags.Static);
                if (prop == null)
                    throw new InvalidOperationException("Could not get BuildManager.TheBuildManager property.");
                _theBuildManager = (BuildManager) prop.GetValue(null);
                return _theBuildManager;
            }
        }

        // ensure that the factory is running with the lastest generation of models
        internal Dictionary<string, Func<IPublishedContent, IPublishedContent>> EnsureModels()
        {
            if (_debugLevel > 0)
                _logger.Logger.Debug<PureLiveModelFactory>("Ensuring models.");

            // don't use an upgradeable lock here because only 1 thread at a time could enter it
            try
            {
                _locker.EnterReadLock();
                if (_hasModels)
                    return _constructors;
            }
            finally
            {
                if (_locker.IsReadLockHeld)
                    _locker.ExitReadLock();
            }

            var buildManagerLocked = false;
            try
            {
                // always take the BuildManager lock *before* taking the _locker lock
                // to avoid possible deadlock situations (see notes above)
                Monitor.Enter(TheBuildManager, ref buildManagerLocked);

                _locker.EnterUpgradeableReadLock();

                if (_hasModels) return _constructors;

                _locker.EnterWriteLock();

                // we don't have models,
                // either they haven't been loaded from the cache yet
                // or they have been reseted and are pending a rebuild

                using (_logger.DebugDuration<PureLiveModelFactory>("Get models.", "Got models."))
                {
                    try
                    {
                        var assembly = GetModelsAssembly(_pendingRebuild);

                        // the one below can be used to simulate an issue with BuildManager, ie it will register
                        // the models with the factory but NOT with the BuildManager, which will not recompile views.
                        // this is for U4-8043 which is an obvious issue but I cannot replicate
                        //_modelsAssembly = _modelsAssembly ?? assembly;

                        // the one below is the normal one
                        _modelsAssembly = assembly;

                        var types = assembly.ExportedTypes.Where(x => x.Inherits<PublishedContentModel>());
                        _constructors = RegisterModels(types);
                        ModelsGenerationError.Clear();
                    }
                    catch (Exception e)
                    {
                        try
                        {
                            _logger.Logger.Error<PureLiveModelFactory>("Failed to build models.", e);
                            _logger.Logger.Warn<PureLiveModelFactory>("Running without models."); // be explicit
                            ModelsGenerationError.Report("Failed to build PureLive models.", e);
                        }
                        finally
                        {
                            _modelsAssembly = null;
                            _constructors = null;
                        }
                    }

                    // don't even try again
                    _hasModels = true;
                }

                return _constructors;
            }
            finally
            {
                if (_locker.IsWriteLockHeld)
                    _locker.ExitWriteLock();
                if (_locker.IsUpgradeableReadLockHeld)
                    _locker.ExitUpgradeableReadLock();
                if (buildManagerLocked)
                    Monitor.Exit(TheBuildManager);
            }
        }

        private Assembly GetModelsAssembly(bool forceRebuild)
        {
            var modelsDirectory = UmbracoConfig.For.ModelsBuilder().ModelsDirectory;
            if (!Directory.Exists(modelsDirectory))
                Directory.CreateDirectory(modelsDirectory);

            // must filter out *.generated.cs because we haven't deleted them yet!
            var ourFiles = Directory.Exists(modelsDirectory)
                ? Directory.GetFiles(modelsDirectory, "*.cs")
                    .Where(x => !x.EndsWith(".generated.cs"))
                    .ToDictionary(x => x, File.ReadAllText)
                : new Dictionary<string, string>();

            var umbraco = Application.GetApplication();
            var typeModels = umbraco.GetAllTypes();
            var currentHash = HashHelper.Hash(ourFiles, typeModels);
            var modelsHashFile = Path.Combine(modelsDirectory, "models.hash");
            var modelsSrcFile = Path.Combine(modelsDirectory, "models.generated.cs");
            var projFile = Path.Combine(modelsDirectory, "all.generated.cs");
            var dllPathFile = Path.Combine(modelsDirectory, "all.dll.path");

            // caching the generated models speeds up booting
            // currentHash hashes both the types & the user's partials

            if (!forceRebuild)
            {
                _logger.Logger.Debug<PureLiveModelFactory>("Looking for cached models.");
                if (File.Exists(modelsHashFile) && File.Exists(projFile))
                {
                    var cachedHash = File.ReadAllText(modelsHashFile);
                    if (currentHash != cachedHash)
                    {
                        _logger.Logger.Debug<PureLiveModelFactory>("Found obsolete cached models.");
                        forceRebuild = true;
                    }

                    // else cachedHash matches currentHash, we can try to load an existing dll
                }
                else
                {
                    _logger.Logger.Debug<PureLiveModelFactory>("Could not find cached models.");
                    forceRebuild = true;
                }
            }

            Assembly assembly;
            if (!forceRebuild)
            {
                // try to load the dll directly (avoid rebuilding)
                //
                // ensure that the .dll file does not have a corresponding .dll.delete file
                // as that would mean the the .dll file is going to be deleted and should not
                // be re-used - that should not happen in theory, but better be safe
                //
                // ensure that the .dll file is in the curreng codegen directory - when IIS
                // or Express does a full restart, it can switch to an entirely new codegen
                // directory, and then we end up referencing a dll which is *not* in that
                // directory, and BuildManager fails to instanciate views ("the view found
                // at ... was not created").
                //
                if (File.Exists(dllPathFile))
                {
                    var dllPath = File.ReadAllText(dllPathFile);
                    var codegen = HttpRuntime.CodegenDir;

                    _logger.Logger.Debug<PureLiveModelFactory>($"Cached models dll at {dllPath}.");

                    if (File.Exists(dllPath) && !File.Exists(dllPath + ".delete") && dllPath.StartsWith(codegen))
                    {
                        assembly = Assembly.LoadFile(dllPath);
                        var attr = assembly.GetCustomAttribute<ModelsBuilderAssemblyAttribute>();
                        if (attr != null && attr.PureLive && attr.SourceHash == currentHash)
                        {
                            // if we were to resume at that revision, then _ver would keep increasing
                            // and that is probably a bad idea - so, we'll always rebuild starting at
                            // ver 1, but we remember we want to skip that one - so we never end up
                            // with the "same but different" version of the assembly in memory
                            _skipver = assembly.GetName().Version.Revision;

                            _logger.Logger.Debug<PureLiveModelFactory>("Loading cached models (dll).");
                            return assembly;
                        }

                        _logger.Logger.Debug<PureLiveModelFactory>("Cached models dll cannot be loaded (invalid assembly).");
                    }
                    else if (!File.Exists(dllPath))
                        _logger.Logger.Debug<PureLiveModelFactory>("Cached models dll does not exist.");
                    else if (File.Exists(dllPath + ".delete"))
                        _logger.Logger.Debug<PureLiveModelFactory>("Cached models dll is marked for deletion.");
                    else if (!dllPath.StartsWith(codegen))
                        _logger.Logger.Debug<PureLiveModelFactory>("Cached models dll is in a different codegen directory.");
                    else
                        _logger.Logger.Debug<PureLiveModelFactory>("Cached models dll cannot be loaded (why?).");
                }

                // must reset the version in the file else it would keep growing
                // loading cached modules only happens when the app restarts
                var text = File.ReadAllText(projFile);
                var match = AssemblyVersionRegex.Match(text);
                if (match.Success)
                {
                    text = text.Replace(match.Value, "AssemblyVersion(\"0.0.0." + _ver + "\")");
                    File.WriteAllText(projFile, text);
                }

                // generate a marker file that will be a dependency
                // see note in RazorBuildProvider_CodeGenerationStarted
                // NO: using all.generated.cs as a dependency
                //File.WriteAllText(Path.Combine(modelsDirectory, "models.dep"), "VER:" + _ver);

                _ver++;
                try
                {
                    assembly = BuildManager.GetCompiledAssembly(ProjVirt);
                    File.WriteAllText(dllPathFile, assembly.Location);
                }
                catch
                {
                    _logger.Logger.Debug<PureLiveModelFactory>("Failed to compile.");

                    // the dll file reference still points to the previous dll, which is obsolete
                    // now and will be deleted by ASP.NET eventually, so better clear that reference.
                    // also touch the proj file to force views to recompile - don't delete as it's
                    // useful to have the source around for debuggin.
                    try
                    {
                        if (File.Exists(dllPathFile)) File.Delete(dllPathFile);
                        if (File.Exists(modelsHashFile)) File.Delete(modelsHashFile);
                        if (File.Exists(projFile)) File.SetLastWriteTime(projFile, DateTime.Now);
                    }
                    catch { /* enough */ }
                    throw;
                }

                _logger.Logger.Debug<PureLiveModelFactory>("Loading cached models (source).");
                return assembly;
            }

            // need to rebuild
            _logger.Logger.Debug<PureLiveModelFactory>("Rebuilding models.");

            // generate code, save
            var code = GenerateModelsCode(ourFiles, typeModels);
            // add extra attributes,
            //  PureLiveAssembly helps identifying Assemblies that contain PureLive models
            //  AssemblyVersion is so that we have a different version for each rebuild
            var ver = _ver == _skipver ? ++_ver : _ver;
            _ver++;
            code = code.Replace("//ASSATTR", $@"[assembly: PureLiveAssembly]
[assembly:ModelsBuilderAssembly(PureLive = true, SourceHash = ""{currentHash}"")]
[assembly:System.Reflection.AssemblyVersion(""0.0.0.{ver}"")]");
            File.WriteAllText(modelsSrcFile, code);

            // generate proj, save
            ourFiles["models.generated.cs"] = code;
            var proj = GenerateModelsProj(ourFiles);
            File.WriteAllText(projFile, proj);

            // compile and register
            try
            {
                assembly = BuildManager.GetCompiledAssembly(ProjVirt);
                File.WriteAllText(dllPathFile, assembly.Location);
                File.WriteAllText(modelsHashFile, currentHash);
            }
            catch
            {
                _logger.Logger.Debug<PureLiveModelFactory>("Failed to compile.");

                // the dll file reference still points to the previous dll, which is obsolete
                // now and will be deleted by ASP.NET eventually, so better clear that reference.
                // also touch the proj file to force views to recompile - don't delete as it's
                // useful to have the source around for debuggin.
                try
                {
                    if (File.Exists(dllPathFile)) File.Delete(dllPathFile);
                    if (File.Exists(modelsHashFile)) File.Delete(modelsHashFile);
                    if (File.Exists(projFile)) File.SetLastWriteTime(projFile, DateTime.Now);
                }
                catch { /* enough */ }
                throw;
            }

            _logger.Logger.Debug<PureLiveModelFactory>("Done rebuilding.");
            return assembly;
        }

        private static Dictionary<string, Func<IPublishedContent, IPublishedContent>> RegisterModels(IEnumerable<Type> types)
        {
            var ctorArgTypes = new[] { typeof(IPublishedContent) };
            var constructors = new Dictionary<string, Func<IPublishedContent, IPublishedContent>>(StringComparer.InvariantCultureIgnoreCase);

            foreach (var type in types)
            {
                var constructor = type.GetConstructor(ctorArgTypes);
                if (constructor == null)
                    throw new InvalidOperationException($"Type {type.FullName} is missing a public constructor with one argument of type IPublishedContent.");
                var attribute = type.GetCustomAttribute<PublishedContentModelAttribute>(false);
                var typeName = attribute == null ? type.Name : attribute.ContentTypeAlias;

                if (constructors.ContainsKey(typeName))
                    throw new InvalidOperationException($"More that one type want to be a model for content type {typeName}.");

                var exprArg = Expression.Parameter(typeof(IPublishedContent), "content");
                var exprNew = Expression.New(constructor, exprArg);
                var expr = Expression.Lambda<Func<IPublishedContent, IPublishedContent>>(exprNew, exprArg);
                var func = expr.Compile();
                constructors[typeName] = func;
            }

            return constructors.Count > 0 ? constructors : null;
        }

        private static string GenerateModelsCode(IDictionary<string, string> ourFiles, IList<TypeModel> typeModels)
        {
            var modelsDirectory = UmbracoConfig.For.ModelsBuilder().ModelsDirectory;
            if (!Directory.Exists(modelsDirectory))
                Directory.CreateDirectory(modelsDirectory);

            foreach (var file in Directory.GetFiles(modelsDirectory, "*.generated.cs"))
                File.Delete(file);

            var parseResult = new CodeParser().ParseWithReferencedAssemblies(ourFiles);
            var builder = new TextBuilder(typeModels, parseResult, UmbracoConfig.For.ModelsBuilder().ModelsNamespace);

            var codeBuilder = new StringBuilder();
            builder.Generate(codeBuilder, builder.GetModelsToGenerate());
            var code = codeBuilder.ToString();

            return code;
        }

        private static readonly Regex UsingRegex = new Regex("^using(.*);", RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly Regex AattrRegex = new Regex("^\\[assembly:(.*)\\]", RegexOptions.Compiled | RegexOptions.Multiline);

        private static string GenerateModelsProj(IDictionary<string, string> files)
        {
            // ideally we would generate a CSPROJ file but then we'd need a BuildProvider for csproj
            // trying to keep things simple for the time being, just write everything to one big file

            // group all 'using' at the top of the file (else fails)
            var usings = new List<string>();
            foreach (var k in files.Keys.ToList())
                files[k] = UsingRegex.Replace(files[k], m =>
                {
                    usings.Add(m.Groups[1].Value);
                    return string.Empty;
                });

            // group all '[assembly:...]' at the top of the file (else fails)
            var aattrs = new List<string>();
            foreach (var k in files.Keys.ToList())
                files[k] = AattrRegex.Replace(files[k], m =>
                {
                    aattrs.Add(m.Groups[1].Value);
                    return string.Empty;
                });

            var text = new StringBuilder();
            foreach (var u in usings.Distinct())
            {
                text.Append("using ");
                text.Append(u);
                text.Append(";\r\n");
            }
            foreach (var a in aattrs)
            {
                text.Append("[assembly:");
                text.Append(a);
                text.Append("]\r\n");
            }
            text.Append("\r\n\r\n");
            foreach (var f in files)
            {
                text.Append("// FILE: ");
                text.Append(f.Key);
                text.Append("\r\n\r\n");
                text.Append(f.Value);
                text.Append("\r\n\r\n\r\n");
            }
            text.Append("// EOF\r\n");

            return text.ToString();
        }

        #endregion

        #region Watching

        private void WatcherOnChanged(object sender, FileSystemEventArgs args)
        {
            var changed = args.Name;

            // don't reset when our files change because we are building!
            //
            // comment it out, and always ignore our files, because it seems that some
            // race conditions can occur on slow Cloud filesystems and then we keep
            // rebuilding

            //if (_building && OurFiles.Contains(changed))
            //{
            //    //_logger.Logger.Info<PureLiveModelFactory>("Ignoring files self-changes.");
            //    return;
            //}

            // always ignore our own file changes
            if (OurFiles.Contains(changed))
                return;

            _logger.Logger.Info<PureLiveModelFactory>("Detected files changes.");

            ResetModels();
        }

        public void Stop(bool immediate)
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Dispose();
            HostingEnvironment.UnregisterObject(this);
        }

        #endregion
    }
}