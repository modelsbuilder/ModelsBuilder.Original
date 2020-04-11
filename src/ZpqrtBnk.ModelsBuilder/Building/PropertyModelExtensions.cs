using Umbraco.Core.Models;

namespace Our.ModelsBuilder.Building
{
    public static class PropertyModelExtensions
    {
        public static bool VariesByCulture(this PropertyTypeModel property)
            => (property.Variations & ContentVariation.Culture) > 0;

        public static bool VariesBySegment(this PropertyTypeModel property)
            => (property.Variations & ContentVariation.Segment) > 0;
    }
}
