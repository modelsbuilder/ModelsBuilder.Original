using System;
using System.Linq;
using System.Linq.Expressions;
using Umbraco.Web.Composing;
using Umbraco.Core.Models.PublishedContent;

namespace Umbraco.ModelsBuilder.Umbraco
{
    /// <summary>
    /// Utility class for published models.
    /// </summary>
    public static class PublishedModelUtility
    {
        /// <summary>
        /// Gets the published content type for the specified model <paramref name="alias" />.
        /// </summary>
        /// <param name="itemType">The published item type.</param>
        /// <param name="alias">The model alias.</param>
        /// <returns>
        /// The published content type.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">itemType</exception>
        public static IPublishedContentType GetModelContentType(PublishedItemType itemType, string alias)
        {
            var facade = Current.UmbracoContext.PublishedSnapshot; // fixme inject!
            switch (itemType)
            {
                case PublishedItemType.Content:
                    return facade.Content.GetContentType(alias);
                case PublishedItemType.Media:
                    return facade.Media.GetContentType(alias);
                case PublishedItemType.Member:
                    return facade.Members.GetContentType(alias);
                default:
                    throw new ArgumentOutOfRangeException(nameof(itemType));
            }
        }

        /// <summary>
        /// Gets the property type alias for the specified <paramref name="selector" />.
        /// </summary>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <param name="selector">The selector.</param>
        /// <returns>
        /// The property type alias.
        /// </returns>
        public static string GetModelPropertyTypeAlias<TModel>(Expression<Func<TModel, object>> selector)
            where TModel : IPublishedElement
        {
            return GetModelPropertyTypeAlias<TModel, object>(selector);
        }

        /// <summary>
        /// Gets the property type alias for the specified <paramref name="selector" />.
        /// </summary>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="selector">The selector.</param>
        /// <returns>
        /// The property type alias.
        /// </returns>
        public static string GetModelPropertyTypeAlias<TModel, TValue>(Expression<Func<TModel, TValue>> selector)
            where TModel : IPublishedElement
        {
            var expr = selector.Body as MemberExpression;

            if (expr == null)
                throw new ArgumentException("Not a property expression.", nameof(selector));

            var attr = expr.Member
                .GetCustomAttributes(typeof (ImplementPropertyTypeAttribute), false)
                .OfType<ImplementPropertyTypeAttribute>()
                .SingleOrDefault();

            if (string.IsNullOrWhiteSpace(attr?.Alias))
                throw new InvalidOperationException($"Could not figure out property alias for property \"{expr.Member.Name}\".");

            return attr.Alias;
        }

        /// <summary>
        /// Gets the published property type on the <paramref name="contentType" /> for the specified <paramref name="selector" />.
        /// </summary>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <param name="contentType">The published content type.</param>
        /// <param name="selector">The selector.</param>
        /// <returns>
        /// The published property type.
        /// </returns>
        public static IPublishedPropertyType GetModelPropertyType<TModel>(IPublishedContentType contentType, Expression<Func<TModel, object>> selector)
            where TModel : IPublishedElement
        {
            return GetModelPropertyType<TModel, object>(contentType, selector);
        }

        /// <summary>
        /// Gets the published property type on the <paramref name="contentType" /> for the specified <paramref name="selector" />.
        /// </summary>
        /// <typeparam name="TModel">The type of the model.</typeparam>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="contentType">The published content type.</param>
        /// <param name="selector">The selector.</param>
        /// <returns>
        /// The published property type.
        /// </returns>
        public static IPublishedPropertyType GetModelPropertyType<TModel, TValue>(IPublishedContentType contentType, Expression<Func<TModel, TValue>> selector)
            where TModel : IPublishedElement
        {
            return contentType.GetPropertyType(GetModelPropertyTypeAlias(selector));
        }
    }
}
