using System;
using Umbraco.Core.Models.PublishedContent;

// ReSharper disable once CheckNamespace, reason: extension methods
namespace Umbraco.Web
{
    /// <summary>
    /// Provides extension methods to models.
    /// </summary>
    public static class OurModelsBuilderPublishedElementExtensions // ensure name does not conflicts with Core's class
    {
        /// <summary>
        /// Gets the value of a property.
        /// </summary>
        /// <typeparam name="TModel">The type of the content model.</typeparam>
        /// <typeparam name="TValue">The type of the property value.</typeparam>
        /// <param name="model">The content model.</param>
        /// <param name="alias">The alias of the property.</param>
        /// <param name="culture">An optional culture.</param>
        /// <param name="segment">An optional segment.</param>
        /// <param name="fallback">A fallback method.</param>
        /// <returns>A value for the property.</returns>
        public static TValue Value<TModel, TValue>(this TModel model, string alias, string culture = null, string segment = null, Func<FallbackInfos<TModel, TValue>, TValue> fallback = null)
            where TModel : IPublishedElement
        {
            var property = model.GetProperty(alias);

            // if we have a property, and it has a value, return that value
            if (property != null && property.HasValue(culture, segment))
                return property.Value<TValue>(culture, segment);

            // else use the fallback method, if any
            if (fallback != default)
                return fallback(new FallbackInfos<TModel, TValue>(model, alias, culture, segment));

            // else... if we have a property, at least let the converter return its own
            // vision of 'no value' (could be an empty enumerable) - otherwise, default
            return property == null ? default : property.Value<TValue>(culture, segment);
        }
    }
}
