using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Common;
using System.IO;
using System.Linq;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.PropertyEditors;
using Umbraco.Core.PropertyEditors.ValueConverters;
using Umbraco.Core.Strings;
using Zbu.ModelsBuilder.Build;

namespace Zbu.ModelsBuilder.Umbraco
{
    public class Application //: IDisposable
    {
        #region Applicationmanagement

        //// ReSharper disable once ClassNeverInstantiated.Local
        //private class AppHandler : ApplicationEventHandler
        //{
        //    protected override void ApplicationStarting(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
        //    {
        //        base.ApplicationStarting(umbracoApplication, applicationContext);

        //        // fixme - if this is a Standalone Web application then the WebBootManager is the one to use
        //        // fixme - and it should do this by itself?!

        //        // remove core converters that are replaced by web converted
        //        PropertyValueConvertersResolver.Current.RemoveType<TinyMceValueConverter>();
        //        PropertyValueConvertersResolver.Current.RemoveType<TextStringValueConverter>();
        //        PropertyValueConvertersResolver.Current.RemoveType<SimpleEditorValueConverter>();
        //    }
        //}

        //private bool _installedConfigSystem;
        private static readonly object LockO = new object();
        private static Application _application;
        //private global::Umbraco.Web.Standalone.StandaloneApplication _umbracoApplication;

        private Application()
        {
            //_standalone = false;
        }

        //private Application(string connectionString, string databaseProvider, bool useLocalApplicationData)
        //{
        //    _connectionString = connectionString;
        //    _databaseProvider = databaseProvider;
        //    _standalone = true;
        //    _useLocalApplicationData = useLocalApplicationData;
        //}

        //private static string UmbracoVersion
        //{
        //    // this is what ApplicationContext.Configured wants in order to be happy
        //    get { return global::Umbraco.Core.Configuration.UmbracoVersion.Current.ToString(3); }
        //}

        //private readonly bool _standalone;
        //private readonly string _connectionString;
        //private readonly string _databaseProvider;
        //private readonly bool _useLocalApplicationData;

        // get app in ASP.NET context ie it already exists, not standalone, don't start anything
        public static Application GetApplication()
        {
            lock (LockO)
            {
                if (_application == null)
                {
                    _application = new Application();
                    // do NOT start it!
                }
                return _application;
            }
        }

        //// get app in non-ASP.NET context ie it does not exist, standalone, start
        //public static Application GetApplication(string connectionString, string databaseProvider, bool useLocalApplicationData = false)
        //{
        //    if (string.IsNullOrWhiteSpace(connectionString))
        //        throw new ArgumentException("Must not be null nor empty.", "connectionString");
        //    if (string.IsNullOrWhiteSpace(databaseProvider))
        //        throw new ArgumentException("Must not be null nor empty.", "databaseProvider");

        //    lock (LockO)
        //    {
        //        if (_application == null)
        //        {
        //            _application = new Application(connectionString, databaseProvider, useLocalApplicationData);
        //            _application.Start();
        //        }
        //        return _application;
        //    }
        //}

        //private void Start()
        //{
        //    if (!global::Umbraco.Web.Standalone.WriteableConfigSystem.Installed)
        //    {
        //        global::Umbraco.Web.Standalone.WriteableConfigSystem.Install();
        //        _installedConfigSystem = true;
        //    }

        //    var cstr = new ConnectionStringSettings("umbracoDbDSN", _connectionString, _databaseProvider);
        //    ConfigurationManager.ConnectionStrings.Add(cstr);
        //    ConfigurationManager.AppSettings.Add("umbracoConfigurationStatus", UmbracoVersion);

        //    // ensure we know about mysql
        //    // either it's already declared in DbProviderFactories config
        //    // or we'll try to register it programmatically
        //    if (_databaseProvider == "MySql.Data.MySqlClient")
        //    {
        //        ConfigureMySqlProvider();

        //        // fixme - this works everywhere but from within VisualStudio
        //        // fixme - and even with MySql.Data in the GAC?!
        //        try
        //        {
        //            var factory = DbProviderFactories.GetFactory("MySql.Data.MySqlClient");
        //        }
        //        catch (Exception e)
        //        {                    
        //            throw new Exception("Failed to configure MySql provider.", e);
        //        }
        //    }

        //    // we are standalone - we might be a Visual Studio custom tool or some sort of
        //    // application that cannot write to its own directory, and must use an AppData dir.
        //    var baseDirectory = Environment.CurrentDirectory;

        //    var useAppData = _useLocalApplicationData
        //        || ConfigurationManager.AppSettings["Zbu.ModelsBuilder.Umbraco.Application.UseLocalApplicationData"] == "true";

        //    if (!useAppData)
        //    {
        //        try
        //        {
        //            using (var fs = System.IO.File.Create(
        //                Path.Combine(baseDirectory, Path.GetRandomFileName()), 1, FileOptions.DeleteOnClose))
        //            {
        //            }
        //        }
        //        catch
        //        {
        //            useAppData = true;
        //        }
        //    }

        //    if (useAppData)
        //    {
        //        baseDirectory = GetLocalApplicationDataRootDirectory();
        //    }

        //    var app = global::Umbraco.Web.Standalone.StandaloneApplication.GetApplication(baseDirectory)
        //        .WithoutApplicationEventHandler<global::Umbraco.Web.Search.ExamineEvents>()
        //        .WithApplicationEventHandler<AppHandler>();

        //    try
        //    {
        //        app.Start(); // will throw if already started
        //    }
        //    catch
        //    {
        //        if (_installedConfigSystem)
        //            global::Umbraco.Web.Standalone.WriteableConfigSystem.Uninstall();
        //        _installedConfigSystem = false;                
        //        throw;
        //    }

        //    _umbracoApplication = app;
        //}

        //public static string GetLocalApplicationDataRootDirectory()
        //{
        //    var appdata = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        //    var rootDir = Path.Combine(appdata, "Zbu.ModelsBuilder");
        //    if (!Directory.Exists(rootDir))
        //        Directory.CreateDirectory(rootDir);
        //    return rootDir;
        //}

        //private void Terminate()
        //{
        //    if (_umbracoApplication != null)
        //    {
        //        _umbracoApplication.Terminate();

        //        if (_installedConfigSystem)
        //        {
        //            global::Umbraco.Web.Standalone.WriteableConfigSystem.Uninstall();
        //            _installedConfigSystem = false;
        //        }
        //    }

        //    lock (LockO)
        //    {
        //        _application = null;
        //    }
        //}

        //private void ConfigureMySqlProvider()
        //{
        //    var section = ConfigurationManager.GetSection("system.data");
        //    var dataset = section as System.Data.DataSet;
        //    if (dataset == null)
        //        throw new Exception("Failed to access system.data configuration section.");
        //    System.Data.DataRowCollection dbProviderFactories = null;
        //    foreach (System.Data.DataTable t in dataset.Tables)
        //        if (t.TableName == "DbProviderFactories")
        //        {
        //            dbProviderFactories = t.Rows;
        //            break;
        //        }
        //    if (dbProviderFactories == null)
        //        throw new Exception("Failed to access system.data/DbProviderFactories.");
        //    var exists = false;
        //    foreach (System.Data.DataRow r in dbProviderFactories)
        //        if (r["InvariantName"].ToString() == "MySql.Data.MySqlClient")
        //            exists = true;
        //    if (!exists)
        //    {
        //        // <add name="MySQL Data Provider" 
        //        //      invariant="MySql.Data.MySqlClient" 
        //        //      description=".Net Framework Data Provider for MySQL" 
        //        //      type="MySql.Data.MySqlClient.MySqlClientFactory, MySql.Data, Version=6.6.5.0, Culture=neutral, PublicKeyToken=c5687fc88969c44d" />
        //        dbProviderFactories.Add("MySQL Data Provider",
        //            ".Net Framework Data Provider for MySQL",
        //            "MySql.Data.MySqlClient",
        //            "MySql.Data.MySqlClient.MySqlClientFactory, MySql.Data, Version=6.6.5.0, Culture=neutral, PublicKeyToken=c5687fc88969c44d");
        //            //"MySql.Data.MySqlClient.MySqlClientFactory, MySql.Data");

        //        dataset.AcceptChanges();
        //    }
        //}

        #endregion

        #region Services

        public IList<TypeModel> GetAllTypes()
        {
            //if (_standalone && _umbracoApplication == null)
            //    throw new InvalidOperationException("Application is not ready.");

            var types = new List<TypeModel>();
            
            var contentTypeService = ApplicationContext.Current.Services.ContentTypeService;
            types.AddRange(GetTypes(PublishedItemType.Content, contentTypeService.GetAllContentTypes().Cast<IContentTypeBase>().ToArray()));
            types.AddRange(GetTypes(PublishedItemType.Media, contentTypeService.GetAllMediaTypes().Cast<IContentTypeBase>().ToArray()));

            var memberTypeService = ApplicationContext.Current.Services.MemberTypeService;
            types.AddRange(GetTypes(PublishedItemType.Member, memberTypeService.GetAll().Cast<IContentTypeBase>().ToArray()));
            
            return types;
        }

        public IList<TypeModel> GetContentTypes()
        {
            //if (_standalone && _umbracoApplication == null)
            //    throw new InvalidOperationException("Application is not ready.");

            var contentTypeService = ApplicationContext.Current.Services.ContentTypeService;
            var contentTypes = contentTypeService.GetAllContentTypes().Cast<IContentTypeBase>().ToArray();
            return GetTypes(PublishedItemType.Content, contentTypes);
        }

        public IList<TypeModel> GetMediaTypes()
        {
            //if (_standalone && _umbracoApplication == null)
            //    throw new InvalidOperationException("Application is not ready.");

            var contentTypeService = ApplicationContext.Current.Services.ContentTypeService;
            var contentTypes = contentTypeService.GetAllMediaTypes().Cast<IContentTypeBase>().ToArray();
            return GetTypes(PublishedItemType.Media, contentTypes);
        }

        public IList<TypeModel> GetMemberTypes()
        {
            //if (_standalone && _umbracoApplication == null)
            //    throw new InvalidOperationException("Application is not ready.");

            var memberTypeService = ApplicationContext.Current.Services.MemberTypeService;
            var memberTypes = memberTypeService.GetAll().Cast<IContentTypeBase>().ToArray();
            return GetTypes(PublishedItemType.Member, memberTypes);
        }

        private static IList<TypeModel> GetTypes(PublishedItemType itemType, IContentTypeBase[] contentTypes)
        {
            var typeModels = new List<TypeModel>();

            // get the types and the properties
            foreach (var contentType in contentTypes)
            {
                var typeModel = new TypeModel
                {
                    Id = contentType.Id,
                    Alias = contentType.Alias,
                    ClrName = contentType.Alias.ToCleanString(CleanStringType.ConvertCase | CleanStringType.PascalCase),
                    BaseTypeId = contentType.ParentId,

                    Name = contentType.Name,
                    Description = contentType.Description
                };

                switch (itemType)
                {
                    case PublishedItemType.Content:
                        typeModel.ItemType = TypeModel.ItemTypes.Content;
                        break;
                    case PublishedItemType.Media:
                        typeModel.ItemType = TypeModel.ItemTypes.Media;
                        break;
                    case PublishedItemType.Member:
                        typeModel.ItemType = TypeModel.ItemTypes.Member;
                        break;
                    default:
                        throw new InvalidOperationException(string.Format("Unsupported PublishedItemType \"{0}\".", itemType));
                }

                typeModels.Add(typeModel);

                var publishedContentType = PublishedContentType.Get(itemType, contentType.Alias);

                foreach (var propertyType in contentType.PropertyTypes)
                {
                    var propertyModel = new PropertyModel
                    {
                        Alias = propertyType.Alias,
                        ClrName = propertyType.Alias.ToCleanString(CleanStringType.ConvertCase | CleanStringType.PascalCase),

                        Name = propertyType.Name,
                        Description = propertyType.Description
                    };

                    var publishedPropertyType = publishedContentType.GetPropertyType(propertyType.Alias);
                    propertyModel.ClrType = publishedPropertyType.ClrType;

                    typeModel.Properties.Add(propertyModel);
                }
            }

            // wire the base types
            foreach (var typeModel in typeModels.Where(x => x.BaseTypeId > 0))
            {
                typeModel.BaseType = typeModels.SingleOrDefault(x => x.Id == typeModel.BaseTypeId);
                if (typeModel.BaseType == null) throw new Exception("Panic: parent type does not exist.");
                typeModel.IsParent = true;
            }

            // discover mixins
            foreach (var contentType in contentTypes)
            {
                var typeModel = typeModels.SingleOrDefault(x => x.Id == contentType.Id);
                if (typeModel == null) throw new Exception("Panic: no type model matching content type.");

                IEnumerable<IContentTypeComposition> compositionTypes;
                var contentTypeAsMedia = contentType as IMediaType;
                var contentTypeAsContent = contentType as IContentType;
                var contentTypeAsMember = contentType as IMemberType;
                if (contentTypeAsMedia != null) compositionTypes = contentTypeAsMedia.ContentTypeComposition;
                else if (contentTypeAsContent != null) compositionTypes = contentTypeAsContent.ContentTypeComposition;
                else if (contentTypeAsMember != null) compositionTypes = contentTypeAsMember.ContentTypeComposition;
                else throw new Exception(string.Format("Panic: unsupported type \"{0}\".", contentType.GetType().FullName));

                foreach (var compositionType in compositionTypes)
                {
                    var compositionModel = typeModels.SingleOrDefault(x => x.Id == compositionType.Id);
                    if (compositionModel == null) throw new Exception("Panic: composition type does not exist.");

                    if (compositionType.Id == contentType.ParentId) continue;

                    // add to mixins
                    typeModel.MixinTypes.Add(compositionModel);

                    // mark as mixin - as well as parents
                    compositionModel.IsMixin = true;
                    while ((compositionModel = compositionModel.BaseType) != null)
                        compositionModel.IsMixin = true;
                }
            }

            return typeModels;
        }

        #endregion

        #region IDisposable

        //private bool _disposed;

        //public void Dispose()
        //{
        //    Dispose(true);
        //    GC.SuppressFinalize(this);
        //}

        //private void Dispose(bool disposing)
        //{
        //    if (!_disposed)
        //    {
        //        if (disposing)
        //        {
        //            // managed
        //            Terminate();
        //        }
        //        // unmanaged
        //        _disposed = true;
        //    }
        //    // base.Dispose()
        //}

        ////~Application()
        ////{
        ////    Dispose(false);
        ////}

        #endregion
    }
}
