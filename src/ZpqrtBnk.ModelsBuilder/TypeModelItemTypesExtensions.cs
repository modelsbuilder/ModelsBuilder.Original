using Umbraco.Core.Models.PublishedContent;
using ZpqrtBnk.ModelsBuilder.Building;

namespace ZpqrtBnk.ModelsBuilder
{
    public static class TypeModelItemTypesExtensions
    {
        public static PublishedItemType ToPublishedItemType(this ContentTypeModel.ItemTypes itemType)
        {
            switch (itemType)
            {
                case ContentTypeModel.ItemTypes.Content:
                    return PublishedItemType.Content;
                case ContentTypeModel.ItemTypes.Element:
                    return PublishedItemType.Element;
                case ContentTypeModel.ItemTypes.Media:
                    return PublishedItemType.Media;
                case ContentTypeModel.ItemTypes.Member:
                    return PublishedItemType.Member;
                default:
                    return PublishedItemType.Unknown;
            }
        }
    }
}
