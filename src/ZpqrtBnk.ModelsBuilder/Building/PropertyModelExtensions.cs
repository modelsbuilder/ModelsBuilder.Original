using Umbraco.Core.Models;

namespace ZpqrtBnk.ModelsBuilder.Building
{
    public static class PropertyModelExtensions
    {
        public static bool VariesByCulture(this PropertyModel property)
            => (property.Variations & ContentVariation.Culture) > 0;

        public static bool VariesBySegment(this PropertyModel property)
            => (property.Variations & ContentVariation.Segment) > 0;
    }
}
