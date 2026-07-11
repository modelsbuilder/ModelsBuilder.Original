using Examine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;
using Our.ModelsBuilder.Tests.Testing;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Services.Navigation;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.ModelsBuilder;
using Umbraco.Extensions;

namespace Our.ModelsBuilder.Tests.Model
{
    [TestFixture]
    public partial class PropertyValueTests
    {
        private IVariationContextAccessor _variationContextAccessor;
        private IPublishedValueFallback _publishedValueFallback;

        // FIXME feedback has, to a point, been merged into Core
        // so we probably want to entirely refactor how we implement it?

        [SetUp]
        public void SetUp()
        {
            var serviceProvider = Substitute.For<IServiceProvider>();
            StaticServiceProvider.Instance = serviceProvider;

            _variationContextAccessor = Substitute.For<IVariationContextAccessor>();
            serviceProvider.GetService(typeof(IVariationContextAccessor)).Returns(_variationContextAccessor);

            var variationContext = new VariationContext("fr-FR", "s");
            _variationContextAccessor.VariationContext.Returns(variationContext);

            var langs = new[]
            {
                new Language("fr-FR", "French") { Id = 1 },
                new Language("en-EN", "English") { Id = 2 },
                new Language("de-DE", "German") { Id = 3, FallbackIsoCode = "en-EN" },
                new Language("dk-DK", "Danish") { Id = 4 }
            };

            var languageService = Substitute.For<ILanguageService>();
            serviceProvider.GetService(typeof(ILanguageService)).Returns(languageService);
            languageService
                .GetAsync(Arg.Any<string>())
                .Returns(call => langs.FirstOrDefault(x => x.IsoCode == call.Arg<string>()));

            var domainCache = Substitute.For<IDomainCache>();
            serviceProvider.GetService(typeof(IDomainCache)).Returns(domainCache);

            var siteDomainMapper = Substitute.For<ISiteDomainMapper>();
            serviceProvider.GetService(typeof(ISiteDomainMapper)).Returns(siteDomainMapper);

            var publishedContentCache = Substitute.For<IPublishedContentCache>();
            serviceProvider.GetService(typeof(IPublishedContentCache)).Returns(publishedContentCache);

            var documentNavigationQueryService = Substitute.For<IDocumentNavigationQueryService>();
            serviceProvider.GetService(typeof(IDocumentNavigationQueryService)).Returns(documentNavigationQueryService);

            var mediaNavigationQueryService = Substitute.For<IMediaNavigationQueryService>();
            serviceProvider.GetService(typeof(IMediaNavigationQueryService)).Returns(mediaNavigationQueryService);

            var publishedModelFactory = Substitute.For<IPublishedModelFactory>();
            serviceProvider.GetService(typeof(IPublishedModelFactory)).Returns(publishedModelFactory);

            var publishedUrlProvider = Substitute.For<IPublishedUrlProvider>();
            serviceProvider.GetService(typeof(IPublishedUrlProvider)).Returns(publishedUrlProvider);

            var userService = Substitute.For<IUserService>();
            serviceProvider.GetService(typeof(IUserService)).Returns(userService);

            var umbracoContextAccessor = Substitute.For<IUmbracoContextAccessor>();
            serviceProvider.GetService(typeof(IUmbracoContextAccessor)).Returns(umbracoContextAccessor);

            var examineManager = Substitute.For<IExamineManager>();
            serviceProvider.GetService(typeof(IExamineManager)).Returns(examineManager);

            var fileService = Substitute.For<IFileService>();
            serviceProvider.GetService(typeof(IFileService)).Returns(fileService);

            var webRoutingSettingsOptions = Substitute.For<IOptions<WebRoutingSettings>>();
            serviceProvider.GetService(typeof(IOptions<WebRoutingSettings>)).Returns(webRoutingSettingsOptions);

            var contentTypeService = Substitute.For<IContentTypeService>();
            serviceProvider.GetService(typeof(IContentTypeService)).Returns(contentTypeService);

            var mediaTypeService = Substitute.For<IMediaTypeService>();
            serviceProvider.GetService(typeof(IMediaTypeService)).Returns(mediaTypeService);

            var memberTypeService = Substitute.For<IMemberTypeService>();
            serviceProvider.GetService(typeof(IMemberTypeService)).Returns(memberTypeService);

            var propertyRenderingContextAccessor = Substitute.For<IPropertyRenderingContextAccessor>();
            serviceProvider.GetService(typeof(IPropertyRenderingContextAccessor)).Returns(propertyRenderingContextAccessor);

            var publishedValueFallback = _publishedValueFallback = new TestPublishedValueFallback();
            serviceProvider.GetService(typeof(IPublishedValueFallback)).Returns(publishedValueFallback);
        }

        private Thing CreateThing()
        {
            var valueConverters = new PropertyValueConverterCollection(Enumerable.Empty<IPropertyValueConverter>);
            var publishedModelFactory = Substitute.For<IPublishedModelFactory>();
            var publishedContentTypeFactory = Substitute.For<IPublishedContentTypeFactory>();

            var somePropertyType = new PublishedPropertyType("someValue", 1, false, ContentVariation.CultureAndSegment, valueConverters, publishedModelFactory, publishedContentTypeFactory);
            var otherPropertyType = new PublishedPropertyType("otherValue", 1, false, ContentVariation.Nothing, valueConverters, publishedModelFactory, publishedContentTypeFactory);

            var propertyTypes = new[] { somePropertyType, otherPropertyType };

            var contentType = new PublishedContentType(Guid.NewGuid(), 1, "thing", PublishedItemType.Content, Enumerable.Empty<string>(), propertyTypes, ContentVariation.CultureAndSegment);

            var contentItem = new TestObjects.PublishedContent(contentType)
                .WithProperty(new TestObjects.PublishedProperty(somePropertyType, _variationContextAccessor)
                    .WithValue("fr-FR", "s", "val-fr")
                    .WithValue("en-EN", "s", "val-en")
                    .WithValue("de-DE", "alt", "segment"))
                .WithProperty(new TestObjects.PublishedProperty(otherPropertyType, _variationContextAccessor)
                    .WithValue("", "", "other"));

            return new Thing(contentItem, _publishedValueFallback);
        }

        [Test]
        public void ClassicFallback()
        {
            var thing = CreateThing();

            Assert.AreEqual("val-fr", ClassicThingExtensions.SomeValue(thing));
            Assert.AreEqual("val-fr", ClassicThingExtensions.SomeValue(thing, culture: "fr-FR"));
            Assert.AreEqual("val-en", ClassicThingExtensions.SomeValue(thing, culture: "en-EN"));

            Assert.AreEqual(null, ClassicThingExtensions.SomeValue(thing, culture: "de-DE"));
            Assert.AreEqual("default", ClassicThingExtensions.SomeValue(thing, culture: "de-DE", fallback: Fallback.ToDefaultValue, defaultValue: "default"));

            Assert.AreEqual("other", ClassicThingExtensions.SomeValue(thing, culture: "de-DE", fallback: Fallback.ToDefaultValue, defaultValue: ClassicThingExtensions.OtherValue(thing)));

            Assert.AreEqual("val-en", ClassicThingExtensions.SomeValue(thing, culture: "de-DE", fallback: Fallback.ToLanguage));
            Assert.AreEqual("default", ClassicThingExtensions.SomeValue(thing, culture: "dk-DK", fallback: Fallback.To(Fallback.Language, Fallback.DefaultValue), defaultValue: "default"));

            Assert.AreEqual("segment", ClassicThingExtensions.SomeValue(thing, culture: "de-DE", fallback: Fallback.To(TestPublishedValueFallback.FallbackToSegment)));
        }

        [Test]
        public void ModernFallback()
        {
            var thing = CreateThing();

            Assert.AreEqual("val-fr", ModernThingExtensions.SomeValue(thing));
            Assert.AreEqual("val-fr", ModernThingExtensions.SomeValue(thing, culture: "fr-FR"));
            Assert.AreEqual("val-en", ModernThingExtensions.SomeValue(thing, culture: "en-EN"));

            Assert.AreEqual(null, ModernThingExtensions.SomeValue(thing, culture: "de-DE"));
            Assert.AreEqual("default", ModernThingExtensions.SomeValue(thing, culture: "de-DE", fallback: x => "default"));
            Assert.AreEqual("default", ModernThingExtensions.SomeValue(thing, culture: "de-DE", fallback: x => x.Default("default")));
            Assert.AreEqual("other", ModernThingExtensions.SomeValue(thing, culture: "de-DE", fallback: x => ModernThingExtensions.OtherValue(thing)));

            Assert.AreEqual("val-en", ModernThingExtensions.SomeValue(thing, culture: "de-DE", fallback: x => x.Languages()));
            Assert.AreEqual("default", ModernThingExtensions.SomeValue(thing, culture: "dk-DK", fallback: x => x.Languages().Default("default")));

            Assert.AreEqual("segment", ModernThingExtensions.SomeValue(thing, culture: "de-DE", fallback: x => Segments(x)));
        }

        [Test]
        public void PropertyGetters()
        {
            var thing = CreateThing();

            Assert.AreEqual("val-fr", thing.SomeValue);
            Assert.AreEqual("val-fr", thing.Value(x => x.SomeValue));
        }

        // in real life this would be an extension method
        public FallbackValue<TModel, TValue> Segments<TModel, TValue>(FallbackInfos<TModel, TValue> fallbackInfos)
            where TModel : IPublishedElement
        {
            return fallbackInfos.CreateFallbackValue().To(TestPublishedValueFallback.FallbackToSegment);
        }

        // in real life this would be an extension method
        public FallbackValue<TModel, TValue> Segments<TModel, TValue>(FallbackValue<TModel, TValue> fallbackValue)
            where TModel : IPublishedElement
        {
            return fallbackValue.To(TestPublishedValueFallback.FallbackToSegment);
        }

        // generated extensions for Classic style -> using HQ's .Value() extensions (fallback object)
        public static class ClassicThingExtensions
        {
            public static string SomeValue(Thing model, string? culture = null, string? segment = null, Fallback fallback = default, string defaultValue = default)
            {
                return model.Value("someValue", culture, segment, fallback, defaultValue);
            }

            public static string OtherValue(Thing model, string? culture = null, string? segment = null, Fallback fallback = default, string defaultValue = default)
            {
                return model.Value("otherValue", culture, segment, fallback, defaultValue);
            }
        }

        // generated extensions for Modern style -> using Our's .Value() extensions (fallback func)
        public static class ModernThingExtensions
        {
            public static string SomeValue(Thing model, string? culture = null, string? segment = null, Func<FallbackInfos<Thing, string>, string> fallback = null)
            {
                return model.Value("someValue", culture, segment, fallback);
            }

            public static string OtherValue(Thing model, string? culture = null, string? segment = null, Func<FallbackInfos<Thing, string>, string> fallback = null)
            {
                return model.Value("otherValue", culture, segment, fallback);
            }
        }

        public class Thing(IPublishedContent content, IPublishedValueFallback publishedValueFallback) 
            : PublishedContentModel(content, publishedValueFallback)
        {

            // use our own attribute - we should know nothing about the embedded MB
            [ImplementPropertyType("someValue")]
            public string SomeValue => ClassicThingExtensions.SomeValue(this);
        }
    }
}
