using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Infrastructure.ModelsBuilder;

// ReSharper disable once CheckNamespace, reason: extensions
namespace Umbraco.Extensions;

public static class PublishedItemTypeExtensions
{
    public static IPublishedContentType GetModelContentType(this PublishedItemType itemType, string alias)
    {
        var publishedContentTypeCache = StaticServiceProvider.Instance.GetRequiredService<IPublishedContentTypeCache>();
        return PublishedModelUtility.GetModelContentType(publishedContentTypeCache, itemType, alias);
    }
}