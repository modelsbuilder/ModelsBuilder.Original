using System.Collections.Generic;
using Umbraco.Web;

// ReSharper disable once CheckNamespace, reason: extensions
namespace Umbraco.Core.Models.PublishedContent
{
    public class FallbackValue<TModel, TValue>
        where TModel : IPublishedElement
    {
        private readonly FallbackInfos<TModel, TValue> _fallbackInfos;
        private readonly List<int> _fallbacks = new List<int>();
        private TValue _defaultValue;

        public FallbackValue(FallbackInfos<TModel, TValue> fallbackInfos)
        {
            _fallbackInfos = fallbackInfos;
        }

        public static implicit operator TValue(FallbackValue<TModel, TValue> fallbackValue)
        {
            var fallbackInfos = fallbackValue._fallbackInfos;
            var property = fallbackInfos.Model.GetProperty(fallbackInfos.PropertyAlias);

            // no need to test for a value on property, if we are running this fallback
            // code, we know that it is because there isn't a value - can directly try
            // the fallback provider - which can be invoked in two different ways,
            // depending on whether we are working on an IPublishedContent or an
            // IPublishedElement (which is the reason why there are two Value<>() overloads,
            // one for IPublishedContent and one for IPublishedElement)

            var fallback = fallbackInfos.PublishedValueFallback;
            var success = fallbackInfos.Model is IPublishedContent publishedContent
                ? fallback.TryGetValue(publishedContent, fallbackInfos.PropertyAlias, fallbackInfos.Culture, fallbackInfos.Segment, Fallback.To(fallbackValue._fallbacks.ToArray()), fallbackValue._defaultValue, out var value, out property)
                : fallback.TryGetValue(fallbackInfos.Model, fallbackInfos.PropertyAlias, fallbackInfos.Culture, fallbackInfos.Segment, Fallback.To(fallbackValue._fallbacks.ToArray()), fallbackValue._defaultValue, out value);

            if (success)
                return value;

            // we *have* to return something
            // see PublishedElementExtensions.Value<TModel, TValue> method, there is no "try" here
            // so, repeat what that method would do if no fallback method was provided

            // if we have a property, at least let the converter return its own
            // vision of 'no value' (could be an empty enumerable) - otherwise, default
            return property == null ? default : property.Value<TValue>(fallbackInfos.Culture, fallbackInfos.Segment);
        }

        public FallbackValue<TModel, TValue> To(params int[] values)
        {
            _fallbacks.AddRange(values);
            return this;
        }

        public FallbackValue<TModel, TValue> Ancestors()
        {
            _fallbacks.Add(Fallback.Ancestors);
            return this;
        }

        public FallbackValue<TModel, TValue> Languages()
        {
            _fallbacks.Add(Fallback.Language);
            return this;
        }

        public FallbackValue<TModel, TValue> Default(TValue defaultValue)
        {
            _defaultValue = defaultValue;
            _fallbacks.Add(Fallback.DefaultValue);
            return this;
        }
    }
}