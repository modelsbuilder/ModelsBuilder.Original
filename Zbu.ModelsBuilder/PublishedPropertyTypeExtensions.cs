using System.Collections.Generic;
using System.Linq;
using Umbraco.Core;
using Umbraco.Core.Models.PublishedContent;

namespace Zbu.ModelsBuilder
{
    public static class PublishedPropertyTypeExtensions
    {
        public static KeyValuePair<int, string>[] PreValues(this PublishedPropertyType propertyType)
        {
            return ApplicationContext.Current.Services.DataTypeService
                .GetPreValuesCollectionByDataTypeId(propertyType.DataTypeId)
                .PreValuesAsArray
                .Select(x => new KeyValuePair<int, string>(x.Id, x.Value))
                .ToArray();
        }
    }
}
