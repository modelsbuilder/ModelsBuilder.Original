using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core;
using Umbraco.Core.Models.PublishedContent;

namespace Zbu.ModelsBuilder.Umbraco
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
