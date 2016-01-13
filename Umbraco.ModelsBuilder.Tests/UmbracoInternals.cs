using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.PropertyEditors;

namespace Umbraco.ModelsBuilder.Tests
{
    class UmbracoInternals
    {
        private static T Ctor<T>(params object[] args)
        {
            var types = args.Select(x => x.GetType()).ToArray();
            var ctor = typeof(T).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, types, null);
            return (T)ctor.Invoke(args);
        }

        public static void InitializeConverters()
        {
            var serviceProvider = new MockServiceProvider();
            var logger = new MockLogger();
            var r = Ctor<PropertyValueConvertersResolver>(serviceProvider, logger, (IEnumerable<Type>)new Type[0]);
            PropertyValueConvertersResolver.Current = r;
        }

        public static void FreezeResolution()
        {
            var t = typeof(ApplicationContext)
                .Assembly
                .GetType("Umbraco.Core.ObjectResolution.Resolution");
            var m = t.GetMethod("Freeze");
            m.Invoke(null, new object[0]);
        }

        public static PublishedPropertyType CreatePublishedPropertyType(string alias, int definition, string editor)
        {
            return Ctor<PublishedPropertyType>(alias, definition, editor);
        }

        public static PublishedContentType CreatePublishedContentType(int id, string alias, IEnumerable<PublishedPropertyType> propertyTypes)
        {
            return Ctor<PublishedContentType>(id, alias, propertyTypes);
        }

        #region Mock

        // we should use a mock framework

        class MockServiceProvider : IServiceProvider
        {
            public object GetService(Type serviceType)
            {
                throw new NotImplementedException();
            }
        }

        class MockLogger : ILogger
        {
            public void Error(Type callingType, string message, Exception exception)
            {
                throw new NotImplementedException();
            }

            public void Warn(Type callingType, string message, params Func<object>[] formatItems)
            {
                throw new NotImplementedException();
            }

            public void WarnWithException(Type callingType, string message, Exception e, params Func<object>[] formatItems)
            {
                throw new NotImplementedException();
            }

            public void Info(Type callingType, Func<string> generateMessage)
            {
                throw new NotImplementedException();
            }

            public void Info(Type type, string generateMessageFormat, params Func<object>[] formatItems)
            {
                throw new NotImplementedException();
            }

            public void Debug(Type callingType, Func<string> generateMessage)
            {
                throw new NotImplementedException();
            }

            public void Debug(Type type, string generateMessageFormat, params Func<object>[] formatItems)
            {
                throw new NotImplementedException();
            }
        }

        #endregion
    }
}
