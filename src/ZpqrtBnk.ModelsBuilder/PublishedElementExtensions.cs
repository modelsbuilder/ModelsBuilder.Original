using System;
using System.Linq.Expressions;
using System.Reflection;
using Umbraco.Core.Models.PublishedContent;
using ZpqrtBnk.ModelsBuilder;

// same namespace as original Umbraco.Web PublishedElementExtensions
// ReSharper disable once CheckNamespace
namespace Umbraco.Web
{
    /// <summary>
    /// Provides extension methods to models.
    /// </summary>
    public static class PublishedElementExtensions
    {
        /// <summary>
        /// Gets the value of a property.
        /// </summary>
        public static TValue Value<TModel, TValue>(this TModel model, Expression<Func<TModel, TValue>> property, string culture = null, string segment = null, Fallback fallback = default, TValue defaultValue = default)
            where TModel : IPublishedElement
        {
            var alias = GetAlias(model, property);
            return model.Value<TValue>(alias, culture, segment, fallback, defaultValue);
        }

        // fixme that one should be public so ppl can use it
        private static string GetAlias<TModel, TValue>(TModel model, Expression<Func<TModel, TValue>> property)
        {
            if (property.NodeType != ExpressionType.Lambda)
                throw new ArgumentException("Not a proper lambda expression (lambda).", nameof(property));

            var lambda = (LambdaExpression) property;
            var lambdaBody = lambda.Body;

            if (lambdaBody.NodeType != ExpressionType.MemberAccess)
                throw new ArgumentException("Not a proper lambda expression (body).", nameof(property));

            var memberExpression = (MemberExpression) lambdaBody;
            if (memberExpression.Expression.NodeType != ExpressionType.Parameter)
                throw new ArgumentException("Not a proper lambda expression (member).", nameof(property));

            var member = memberExpression.Member;

            var attribute = member.GetCustomAttribute<ImplementPropertyTypeAttribute>();
            if (attribute == null)
                throw new InvalidOperationException("Property is not marked with ImplementPropertyType attribute.");
            
            return attribute.Alias;
        }

        /// <summary>
        /// Gets the value of a property.
        /// </summary>
        public static TValue Value<TModel, TValue>(this TModel model, string alias, string culture = null, string segment = null, Func<TModel, TValue> fallback = default)
            where TModel : IPublishedElement
        {
            var property = model.GetProperty(alias);

            // if we have a property, and it has a value, return that value
            if (property != null && property.HasValue(culture, segment))
                return property.Value<TValue>(culture, segment);

            // else use the fallback method if any
            if (fallback != null)
                return fallback(model);

            // else... if we have a property, at least let the converter return its own
            // vision of 'no value' (could be an empty enumerable) - otherwise, default
            return property == null ? default : property.Value<TValue>(culture, segment);
        }
    }
}
