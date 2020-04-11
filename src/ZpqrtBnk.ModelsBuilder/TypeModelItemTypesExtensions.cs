using Our.ModelsBuilder.Building;
using Umbraco.Core.Models.PublishedContent;

namespace Our.ModelsBuilder
{
    public static class TypeModelItemTypesExtensions
    {
        public static PublishedItemType ToPublishedItemType(this ContentTypeKind contentTypeKind)
        {
            switch (contentTypeKind)
            {
                case ContentTypeKind.Content:
                    return PublishedItemType.Content;
                case ContentTypeKind.Element:
                    return PublishedItemType.Element;
                case ContentTypeKind.Media:
                    return PublishedItemType.Media;
                case ContentTypeKind.Member:
                    return PublishedItemType.Member;
                default:
                    return PublishedItemType.Unknown;
            }
        }
    }
}
