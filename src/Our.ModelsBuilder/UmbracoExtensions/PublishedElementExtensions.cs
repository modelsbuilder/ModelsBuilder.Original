using System;
using System.Linq.Expressions;
using System.Reflection;
using Our.ModelsBuilder;
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

        // note: the method below used to be provided by Core, but then when they embedded MB, they ran into
        // collision, and their "fix" consisted in renaming the method "ValueFor" - so we can provide it here.
        // see: https://github.com/umbraco/Umbraco-CMS/issues/7469

        /// <summary>
        /// Gets the value of a property.
        /// </summary>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <typeparam name="TValue">The type of the property.</typeparam>
        /// <param name="model">The model.</param>
        /// <param name="property">An expression selecting the property.</param>
        /// <param name="culture">An optional culture.</param>
        /// <param name="segment">An optional segment.</param>
        /// <param name="fallback">An optional fallback.</param>
        /// /// <param name="defaultValue">An optional default value.</param>
        /// <returns>The value of the property.</returns>
        public static TValue Value<TModel, TValue>(this TModel model, Expression<Func<TModel, TValue>> property, string culture = null, string segment = null, Fallback fallback = default, TValue defaultValue = default)
            where TModel : IPublishedElement
        {
            var alias = GetAlias(model, property);
            return model.Value(alias, culture, segment, fallback, defaultValue);
        }

        /// <summary>
        /// Gets the alias of a property.
        /// </summary>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <typeparam name="TValue">The type of the property.</typeparam>
        /// <param name="model">The model.</param>
        /// <param name="property">An expression selecting the property.</param>
        /// <returns>The alias of the property.</returns>
        private static string GetAlias<TModel, TValue>(TModel model, Expression<Func<TModel, TValue>> property)
        {
            if (property.NodeType != ExpressionType.Lambda)
                throw new ArgumentException("Not a proper lambda expression (lambda).", nameof(property));

            var lambda = (LambdaExpression) property;
            var lambdaBody = lambda.Body;

            if (lambdaBody.NodeType != ExpressionType.MemberAccess)
                throw new ArgumentException("Not a proper lambda expression (body).", nameof(property));

            var memberExpression = (MemberExpression)lambdaBody;
            if (memberExpression.Expression.NodeType != ExpressionType.Parameter)
                throw new ArgumentException("Not a proper lambda expression (member).", nameof(property));

            var member = memberExpression.Member;

            var attribute = member.GetCustomAttribute<ImplementPropertyTypeAttribute>();
            if (attribute == null)
                throw new InvalidOperationException("Property is not marked with ImplementPropertyType attribute.");

            return attribute.PropertyTypeAlias;
        }
    }
}
