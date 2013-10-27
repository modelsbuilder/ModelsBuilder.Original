using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.PropertyEditors;
using Umbraco.Core.PropertyEditors.ValueConverters;
using Umbraco.Core.Strings;

namespace Zbu.ModelsBuilder.Umbraco
{
    public class Application : IDisposable
    {
// ReSharper disable once ClassNeverInstantiated.Local
        private class AppHandler : ApplicationEventHandler
        {
            protected override void ApplicationStarting(UmbracoApplicationBase umbracoApplication, ApplicationContext applicationContext)
            {
                base.ApplicationStarting(umbracoApplication, applicationContext);

                // remove core converters that are replaced by web converted
                PropertyValueConvertersResolver.Current.RemoveType<TinyMceValueConverter>();
                PropertyValueConvertersResolver.Current.RemoveType<TextStringValueConverter>();
                PropertyValueConvertersResolver.Current.RemoveType<SimpleEditorValueConverter>();
            }
        }

        private bool _installedConfigSystem;
        private static readonly object LockO = new object();
        private static Application _application;
        private global::Umbraco.Web.Standalone.StandaloneApplication _umbracoApplication;

        private Application()
        { }

        public static Application GetApplication()
        {
            lock (LockO)
            {
                if (_application == null)
                {
                    _application = new Application();
                    _application.Start();
                }
                return _application;
            }
        }

        private void Start()
        {
            if (!ConfigSystem.Installed)
            {
                ConfigSystem.Install();
                _installedConfigSystem = true;
            }

            // FIXME should not hard-code
            const string connectionString = @"server=localhost\sqlexpress;database=dev_umbraco6;user id=sa;password=sayg";
            const string providerName = "System.Data.SqlClient";
            const string version = "6.2.0";
            ConfigurationManager.ConnectionStrings.Add(
                new ConnectionStringSettings("umbracoDbDSN", connectionString, providerName));
            ConfigurationManager.AppSettings.Add("umbracoConfigurationStatus", version);

            var app = global::Umbraco.Web.Standalone.StandaloneApplication.GetApplication(Environment.CurrentDirectory)
                .WithoutApplicationEventHandler<global::Umbraco.Web.Search.ExamineEvents>()
                .WithApplicationEventHandler<AppHandler>();

            try
            {
                app.Start(); // will throw if already started
            }
            catch
            {
                if (_installedConfigSystem)
                    ConfigSystem.Uninstall();
                _installedConfigSystem = false;                
                throw;
            }

            _umbracoApplication = app;
        }

        private void Terminate()
        {
            if (_umbracoApplication != null)
            {
                if (_installedConfigSystem)
                {
                    ConfigSystem.Uninstall();
                    _installedConfigSystem = false;
                }

                _umbracoApplication.Terminate();
            }

            lock (LockO)
            {
                _application = null;
            }
        }

        public IList<TypeModel> GetContentTypes()
        {
            if (_umbracoApplication == null)
                throw new InvalidOperationException("Application is not ready.");

            var contentTypeService = ApplicationContext.Current.Services.ContentTypeService;
            var contentTypes = contentTypeService.GetAllContentTypes().ToArray();

            var typeModels = new List<TypeModel>();

            // get the types and the properties
            foreach (var contentType in contentTypes)
            {
                var typeModel = new TypeModel
                {
                    Id = contentType.Id,
                    Alias = contentType.Alias,
                    Name = contentType.Alias.ToCleanString(CleanStringType.PascalCase),
                    BaseTypeId = contentType.ParentId
                };

                typeModels.Add(typeModel);

                var publishedContentType = PublishedContentType.Get(PublishedItemType.Content, contentType.Alias);

                foreach (var propertyType in contentType.PropertyTypes)
                {
                    var propertyModel = new PropertyModel
                    {
                        Alias = propertyType.Alias,
                        Name = propertyType.Alias.ToCleanString(CleanStringType.PascalCase)
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
                if (typeModel.BaseType == null) throw new Exception();
            }

            // discover mixins
            foreach (var contentType in contentTypes)
            {
                var typeModel = typeModels.SingleOrDefault(x => x.Id == contentType.Id);
                if (typeModel == null) throw new Exception();

                foreach (var compositionType in contentType.ContentTypeComposition)
                {
                    var compositionModel = typeModels.SingleOrDefault(x => x.Id == compositionType.Id);
                    if (compositionModel == null) throw new Exception();

                    if (compositionType.Id != contentType.ParentId)
                    {
                        // add to mixins
                        typeModel.MixinTypes.Add(compositionModel);

                        // mark as mixin - as well as parents
                        compositionModel.IsMixin = true;
                        while ((compositionModel = compositionModel.BaseType) != null)
                            compositionModel.IsMixin = true;
                    }
                }
            }

            return typeModels;
        }

        public IList<TypeModel> GetMediaTypes()
        {
            throw new NotImplementedException();
        }

        private bool _disposed;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // managed
                    Terminate();
                }
                // unmanaged
                _disposed = true;
            }
            // base.Dispose()
        }

        //~Application()
        //{
        //    Dispose(false);
        //}
    }
}
