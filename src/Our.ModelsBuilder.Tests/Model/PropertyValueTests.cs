using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using NUnit.Framework;
using Our.ModelsBuilder.Tests.Testing;
using Umbraco.Core.Composing;
using Umbraco.Core.Models;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.PropertyEditors;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Models.PublishedContent;

namespace Our.ModelsBuilder.Tests.Model
{
    [TestFixture]
    public class PropertyValueTests
    {
        private IVariationContextAccessor _variationContextAccessor;

        [SetUp]
        public void SetUp()
        {
            Current.Reset();

            var factory = Mock.Of<IFactory>();

            var variationContext = new VariationContext("fr", "s");
            _variationContextAccessor = Mock.Of<IVariationContextAccessor>();
            Mock.Get(_variationContextAccessor).Setup(x => x.VariationContext).Returns(variationContext);

            var localizationService = Mock.Of<ILocalizationService>();
            var langs = new[]
            {
                new Language("fr") { Id = 1 }, 
                new Language("en") { Id = 2 }, 
                new Language("de") { Id = 3, FallbackLanguageId = 2 }, 
                new Language("dk") { Id = 4 }
            };
            Mock.Get(localizationService).Setup(x => x.GetLanguageByIsoCode(It.IsAny<string>()))
                .Returns<string>(isoCode => langs.FirstOrDefault(x => x.IsoCode == isoCode));
            Mock.Get(localizationService).Setup(x => x.GetLanguageById(It.IsAny<int>()))
                .Returns<int>(id => langs.FirstOrDefault(x => x.Id == id));
            var serviceContext = ServiceContext.CreatePartial(localizationService: localizationService);

            var fallback = new TestPublishedValueFallback(serviceContext, _variationContextAccessor);
            Mock.Get(factory).Setup(x => x.GetInstance(typeof(IPublishedValueFallback))).Returns(fallback);

            Current.Factory = factory;
        }

        private Thing CreateThing()
        {
            var valueConverters = new PropertyValueConverterCollection(Enumerable.Empty<IPropertyValueConverter>());
            var publishedModelFactory = Mock.Of<IPublishedModelFactory>();
            var publishedContentTypeFactory = Mock.Of<IPublishedContentTypeFactory>();

            var somePropertyType = new PublishedPropertyType("someValue", 1, false, ContentVariation.CultureAndSegment, valueConverters, publishedModelFactory, publishedContentTypeFactory);
            var otherPropertyType = new PublishedPropertyType("otherValue", 1, false, ContentVariation.Nothing, valueConverters, publishedModelFactory, publishedContentTypeFactory);

            var propertyTypes = new[] { somePropertyType, otherPropertyType };

            var contentType = new PublishedContentType(1, "thing", PublishedItemType.Content, Enumerable.Empty<string>(), propertyTypes, ContentVariation.CultureAndSegment);

            var contentItem = new TestObjects.PublishedContent(contentType)
                .WithProperty(new TestObjects.PublishedProperty(somePropertyType, _variationContextAccessor)
                    .WithValue("fr", "s", "val-fr")
                    .WithValue("en", "s", "val-en")
                    .WithValue("de", "alt", "segment"))
                .WithProperty(new TestObjects.PublishedProperty(otherPropertyType, _variationContextAccessor)
                    .WithValue("", "", "other"));

            return new Thing(contentItem);
        }

        [Test]
        public void ClassicFallback()
        {
            var thing = CreateThing();

            Assert.AreEqual("val-fr", ClassicThingExtensions.SomeValue(thing));
            Assert.AreEqual("val-fr", ClassicThingExtensions.SomeValue(thing, culture: "fr"));
            Assert.AreEqual("val-en", ClassicThingExtensions.SomeValue(thing, culture: "en"));

            Assert.AreEqual(null, ClassicThingExtensions.SomeValue(thing, culture: "de"));
            Assert.AreEqual("default", ClassicThingExtensions.SomeValue(thing, culture: "de", fallback: Fallback.ToDefaultValue, defaultValue: "default"));

            Assert.AreEqual("other", ClassicThingExtensions.SomeValue(thing, culture: "de", fallback: Fallback.ToDefaultValue, defaultValue: ClassicThingExtensions.OtherValue(thing)));

            Assert.AreEqual("val-en", ClassicThingExtensions.SomeValue(thing, culture: "de", fallback: Fallback.ToLanguage));
            Assert.AreEqual("default", ClassicThingExtensions.SomeValue(thing, culture: "dk", fallback: Fallback.To(Fallback.Language, Fallback.DefaultValue), defaultValue: "default"));

            Assert.AreEqual("segment", ClassicThingExtensions.SomeValue(thing, culture: "de", fallback: Fallback.To(FallbackToSegment)));
        }

        [Test]
        public void ModernFallback()
        {
            var thing = CreateThing();

            Assert.AreEqual("val-fr", ModernThingExtensions.SomeValue(thing));
            Assert.AreEqual("val-fr", ModernThingExtensions.SomeValue(thing, culture: "fr"));
            Assert.AreEqual("val-en", ModernThingExtensions.SomeValue(thing, culture: "en"));

            Assert.AreEqual(null, ModernThingExtensions.SomeValue(thing, culture: "de"));
            Assert.AreEqual("default", ModernThingExtensions.SomeValue(thing, culture: "de", fallback: x => "default"));
            Assert.AreEqual("default", ModernThingExtensions.SomeValue(thing, culture: "de", fallback: x => x.Default("default")));
            Assert.AreEqual("other", ModernThingExtensions.SomeValue(thing, culture: "de", fallback: x => ModernThingExtensions.OtherValue(thing)));

            Assert.AreEqual("val-en", ModernThingExtensions.SomeValue(thing, culture: "de", fallback: x => x.Languages()));
            Assert.AreEqual("default", ModernThingExtensions.SomeValue(thing, culture: "dk", fallback: x => x.Languages().Default("default")));

            Assert.AreEqual("segment", ModernThingExtensions.SomeValue(thing, culture: "de", fallback: x => Segments(x)));
        }

        [Test]
        public void PropertyGetters()
        {
            var thing = CreateThing();

            Assert.AreEqual("val-fr", thing.SomeValue);
            Assert.AreEqual("val-fr", thing.Value(x => x.SomeValue));
        }

        public const int FallbackToSegment = 666;

        // this thing is practically impossible to override ;(
        public class TestPublishedValueFallback : IPublishedValueFallback
        {
            private readonly ILocalizationService _localizationService;
            private readonly IVariationContextAccessor _variationContextAccessor;

            /// <summary>
            /// Initializes a new instance of the <see cref="PublishedValueFallback"/> class.
            /// </summary>
            public TestPublishedValueFallback(ServiceContext serviceContext, IVariationContextAccessor variationContextAccessor)
            {
                _localizationService = serviceContext.LocalizationService;
                _variationContextAccessor = variationContextAccessor;
            }

            /// <inheritdoc />
            public bool TryGetValue(IPublishedProperty property, string culture, string segment, Fallback fallback, object defaultValue, out object value)
            {
                return TryGetValue<object>(property, culture, segment, fallback, defaultValue, out value);
            }

            /// <inheritdoc />
            public bool TryGetValue<T>(IPublishedProperty property, string culture, string segment, Fallback fallback, T defaultValue, out T value)
            {
                _variationContextAccessor.ContextualizeVariation(property.PropertyType.Variations, ref culture, ref segment);

                foreach (var f in fallback)
                {
                    switch (f)
                    {
                        case Fallback.None:
                            continue;
                        case Fallback.DefaultValue:
                            value = defaultValue;
                            return true;
                        case Fallback.Language:
                            if (TryGetValueWithLanguageFallback(property, culture, segment, out value))
                                return true;
                            break;
                        default:
                            throw NotSupportedFallbackMethod(f, "property");
                    }
                }

                value = default;
                return false;
            }

            /// <inheritdoc />
            public bool TryGetValue(IPublishedElement content, string alias, string culture, string segment, Fallback fallback, object defaultValue, out object value)
            {
                return TryGetValue<object>(content, alias, culture, segment, fallback, defaultValue, out value);
            }

            /// <inheritdoc />
            public bool TryGetValue<T>(IPublishedElement content, string alias, string culture, string segment, Fallback fallback, T defaultValue, out T value)
            {
                var propertyType = content.ContentType.GetPropertyType(alias);
                if (propertyType == null)
                {
                    value = default;
                    return false;
                }

                _variationContextAccessor.ContextualizeVariation(propertyType.Variations, ref culture, ref segment);

                foreach (var f in fallback)
                {
                    switch (f)
                    {
                        case Fallback.None:
                            continue;
                        case Fallback.DefaultValue:
                            value = defaultValue;
                            return true;
                        case Fallback.Language:
                            if (TryGetValueWithLanguageFallback(content, alias, culture, segment, out value))
                                return true;
                            break;
                        case FallbackToSegment: // hack our own!
                            if (content.HasValue(alias, culture, "alt"))
                            {
                                value = content.Value<T>(alias, "alt", segment);
                                return true;
                            }
                            break;
                        default:
                            throw NotSupportedFallbackMethod(f, "element");
                    }
                }

                value = default;
                return false;
            }

            /// <inheritdoc />
            public bool TryGetValue(IPublishedContent content, string alias, string culture, string segment, Fallback fallback, object defaultValue, out object value, out IPublishedProperty noValueProperty)
            {
                return TryGetValue<object>(content, alias, culture, segment, fallback, defaultValue, out value, out noValueProperty);
            }

            /// <inheritdoc />
            public virtual bool TryGetValue<T>(IPublishedContent content, string alias, string culture, string segment, Fallback fallback, T defaultValue, out T value, out IPublishedProperty noValueProperty)
            {
                noValueProperty = default;

                var propertyType = content.ContentType.GetPropertyType(alias);
                if (propertyType != null)
                {
                    _variationContextAccessor.ContextualizeVariation(propertyType.Variations, ref culture, ref segment);
                    noValueProperty = content.GetProperty(alias);
                }

                // note: we don't support "recurse & language" which would walk up the tree,
                // looking at languages at each level - should someone need it... they'll have
                // to implement it.

                foreach (var f in fallback)
                {
                    switch (f)
                    {
                        case Fallback.None:
                            continue;
                        case Fallback.DefaultValue:
                            value = defaultValue;
                            return true;
                        case Fallback.Language:
                            if (propertyType == null)
                                continue;
                            if (TryGetValueWithLanguageFallback(content, alias, culture, segment, out value))
                                return true;
                            break;
                        case Fallback.Ancestors:
                            if (TryGetValueWithAncestorsFallback(content, alias, culture, segment, out value, ref noValueProperty))
                                return true;
                            break;
                        case FallbackToSegment: // hack our own!
                            if (content.HasValue(alias, culture, "alt"))
                            {
                                value = content.Value<T>(alias, culture, "alt");
                                return true;
                            }
                            break;
                        default:
                            throw NotSupportedFallbackMethod(f, "content");
                    }
                }

                value = default;
                return false;
            }

            private NotSupportedException NotSupportedFallbackMethod(int fallback, string level)
            {
                return new NotSupportedException($"Fallback {GetType().Name} does not support fallback code '{fallback}' at {level} level.");
            }

            // tries to get a value, recursing the tree
            // because we recurse, content may not even have the a property with the specified alias (but only some ancestor)
            // in case no value was found, noValueProperty contains the first property that was found (which does not have a value)
            private bool TryGetValueWithAncestorsFallback<T>(IPublishedContent content, string alias, string culture, string segment, out T value, ref IPublishedProperty noValueProperty)
            {
                IPublishedProperty property; // if we are here, content's property has no value
                do
                {
                    content = content.Parent;

                    var propertyType = content?.ContentType.GetPropertyType(alias);

                    if (propertyType != null)
                    {
                        culture = null;
                        segment = null;
                        _variationContextAccessor.ContextualizeVariation(propertyType.Variations, ref culture, ref segment);
                    }

                    property = content?.GetProperty(alias);
                    if (property != null && noValueProperty == null)
                    {
                        noValueProperty = property;
                    }
                }
                while (content != null && (property == null || property.HasValue(culture, segment) == false));

                // if we found a content with the property having a value, return that property value
                if (property != null && property.HasValue(culture, segment))
                {
                    value = property.Value<T>(culture, segment);
                    return true;
                }

                value = default;
                return false;
            }

            // tries to get a value, falling back onto other languages
            private bool TryGetValueWithLanguageFallback<T>(IPublishedProperty property, string culture, string segment, out T value)
            {
                value = default;

                if (string.IsNullOrWhiteSpace(culture)) return false;

                var visited = new HashSet<int>();

                var language = _localizationService.GetLanguageByIsoCode(culture);
                if (language == null) return false;

                while (true)
                {
                    if (language.FallbackLanguageId == null) return false;

                    var language2Id = language.FallbackLanguageId.Value;
                    if (visited.Contains(language2Id)) return false;
                    visited.Add(language2Id);

                    var language2 = _localizationService.GetLanguageById(language2Id);
                    if (language2 == null) return false;
                    var culture2 = language2.IsoCode;

                    if (property.HasValue(culture2, segment))
                    {
                        value = property.Value<T>(culture2, segment);
                        return true;
                    }

                    language = language2;
                }
            }

            // tries to get a value, falling back onto other languages
            private bool TryGetValueWithLanguageFallback<T>(IPublishedElement content, string alias, string culture, string segment, out T value)
            {
                value = default;

                if (string.IsNullOrWhiteSpace(culture)) return false;

                var visited = new HashSet<int>();

                var language = _localizationService.GetLanguageByIsoCode(culture);
                if (language == null) return false;

                while (true)
                {
                    if (language.FallbackLanguageId == null) return false;

                    var language2Id = language.FallbackLanguageId.Value;
                    if (visited.Contains(language2Id)) return false;
                    visited.Add(language2Id);

                    var language2 = _localizationService.GetLanguageById(language2Id);
                    if (language2 == null) return false;
                    var culture2 = language2.IsoCode;

                    if (content.HasValue(alias, culture2, segment))
                    {
                        value = content.Value<T>(alias, culture2, segment);
                        return true;
                    }

                    language = language2;
                }
            }

            // tries to get a value, falling back onto other languages
            private bool TryGetValueWithLanguageFallback<T>(IPublishedContent content, string alias, string culture, string segment, out T value)
            {
                value = default;

                if (string.IsNullOrWhiteSpace(culture)) return false;

                var visited = new HashSet<int>();

                // TODO: _localizationService.GetXxx() is expensive, it deep clones objects
                // we want _localizationService.GetReadOnlyXxx() returning IReadOnlyLanguage which cannot be saved back = no need to clone

                var language = _localizationService.GetLanguageByIsoCode(culture);
                if (language == null) return false;

                while (true)
                {
                    if (language.FallbackLanguageId == null) return false;

                    var language2Id = language.FallbackLanguageId.Value;
                    if (visited.Contains(language2Id)) return false;
                    visited.Add(language2Id);

                    var language2 = _localizationService.GetLanguageById(language2Id);
                    if (language2 == null) return false;
                    var culture2 = language2.IsoCode;

                    if (content.HasValue(alias, culture2, segment))
                    {
                        value = content.Value<T>(alias, culture2, segment);
                        return true;
                    }

                    language = language2;
                }
            }
        }

        // in real life this would be an extension method
        public FallbackValue<TModel, TValue> Segments<TModel, TValue>(FallbackInfos<TModel, TValue> fallbackInfos)
            where TModel : IPublishedElement
        {
            return fallbackInfos.CreateFallbackValue().To(FallbackToSegment);
        }

        // in real life this would be an extension method
        public FallbackValue<TModel, TValue> Segments<TModel, TValue>(FallbackValue<TModel, TValue> fallbackValue)
            where TModel : IPublishedElement
        {
            return fallbackValue.To(FallbackToSegment);
        }

        // generated extensions for Classic style
        public static class ClassicThingExtensions
        {
            public static string SomeValue(Thing model, string culture = null, string segment = null, Fallback fallback = default, string defaultValue = default)
            {
                return model.Value("someValue", culture, segment, fallback, defaultValue);
            }

            public static string OtherValue(Thing model, string culture = null, string segment = null, Fallback fallback = default, string defaultValue = default)
            {
                return model.Value("otherValue", culture, segment, fallback, defaultValue);
            }
        }

        // generated extensions for Modern style
        public static class ModernThingExtensions
        {
            public static string SomeValue(Thing model, string culture = null, string segment = null, Func<FallbackInfos<Thing, string>, string> fallback = null)
            {
                return model.Value("someValue", culture, segment, fallback);
            }

            public static string OtherValue(Thing model, string culture = null, string segment = null, Func<FallbackInfos<Thing, string>, string> fallback = null)
            {
                return model.Value("otherValue", culture, segment, fallback);
            }
        }

        public class Thing : PublishedContentModel
        {
            public Thing(IPublishedContent content)
                : base(content)
            { }
            
            // FIXME: this means:
            // we should *not* define our own (no need for it)
            // if we want to support pre-embedded, we need to detect it?

            // beware! this *has* to be the embedded attribute
            [global::Umbraco.ModelsBuilder.Embedded.ImplementPropertyType("someValue")]
            public string SomeValue => ClassicThingExtensions.SomeValue(this);
        }
    }
}
