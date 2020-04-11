using Umbraco.Core.Composing;


// ReSharper disable once CheckNamespace, reason: extensions
namespace Umbraco.Core.Models.PublishedContent
{
    public class FallbackInfos<TModel, TValue>
        where TModel : IPublishedElement
    {
        public FallbackInfos(TModel model, string propertyAlias, string culture, string segment)
        {
            Model = model;
            PropertyAlias = propertyAlias;
            Culture = culture;
            Segment = segment;
        }

        public TModel Model { get; }

        public string PropertyAlias { get; }

        public string Culture { get; }

        public string Segment { get; }

        // service locators are evil, we should inject...
        // but: FallbackInfo instances are created in an extension method, so?
        public IPublishedValueFallback PublishedValueFallback => Current.PublishedValueFallback;

        // TODO: think about having a FallbackValue factory to let people enhance it?
        public FallbackValue<TModel, TValue> CreateFallbackValue() => new FallbackValue<TModel, TValue>(this);

        public FallbackValue<TModel, TValue> To(params int[] values) => CreateFallbackValue().To(values);

        public FallbackValue<TModel, TValue> Languages() => CreateFallbackValue().Languages();

        public FallbackValue<TModel, TValue> Ancestors() => CreateFallbackValue().Ancestors();

        public FallbackValue<TModel, TValue> Default(TValue defaultValue) => CreateFallbackValue().Default(defaultValue);
    }
}
