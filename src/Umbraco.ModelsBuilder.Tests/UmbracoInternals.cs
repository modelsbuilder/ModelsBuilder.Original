using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LightInject;
using Moq;
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

        //public static void InitializeConverters()
        //{
        //    var serviceContainerMock = new Mock<IServiceContainer>();
        //    var serviceContainer = serviceContainerMock.Object;

        //    var logger = Mock.Of<ILogger>();
        //    var r = Ctor<PropertyValueConvertersResolver>(serviceContainer, logger, (IEnumerable<Type>)new Type[0]);
        //    PropertyValueConvertersResolver.Current = r;
        //}

        //public static void FreezeResolution()
        //{
        //    var t = typeof(ApplicationContext)
        //        .Assembly
        //        .GetType("Umbraco.Core.ObjectResolution.Resolution");
        //    var m = t.GetMethod("Freeze");
        //    m.Invoke(null, new object[0]);
        //}

        public static PublishedPropertyType CreatePublishedPropertyType(string alias, int definition, string editor)
        {
            return Ctor<PublishedPropertyType>(alias, definition, editor, false);
        }

        public static PublishedContentType CreatePublishedContentType(int id, string alias, IEnumerable<PublishedPropertyType> propertyTypes)
        {
            return Ctor<PublishedContentType>(id, alias, propertyTypes);
        }
    }
}
