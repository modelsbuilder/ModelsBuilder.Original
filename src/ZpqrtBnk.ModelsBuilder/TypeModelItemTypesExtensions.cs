using Umbraco.Core.Models.PublishedContent;
using ZpqrtBnk.ModelsBuilder.Building;

namespace ZpqrtBnk.ModelsBuilder
{
    public static class TypeModelItemTypesExtensions
    {
        public static PublishedItemType ToPublishedItemType(this TypeModel.ItemTypes itemType)
        {
            switch (itemType)
            {
                case TypeModel.ItemTypes.Content:
                    return PublishedItemType.Content;
                case TypeModel.ItemTypes.Element:
                    return PublishedItemType.Element;
                case TypeModel.ItemTypes.Media:
                    return PublishedItemType.Media;
                case TypeModel.ItemTypes.Member:
                    return PublishedItemType.Member;
                default:
                    return PublishedItemType.Unknown;
            }
        }
    }
}
