using System;
using System.Collections.Generic;
using System.Linq;

namespace Our.ModelsBuilder
{
    public class ContentTypeModelInfo
    {
        public ContentTypeModelInfo(string alias, string clrName, Type clrType, params PropertyTypeModelInfo[] properties)
        {
            Alias = alias;
            ClrName = clrName;
            ClrType = clrType;
            PropertyTypeInfos = properties;
        }

        public string Alias { get; }
        public string ClrName { get; }
        public Type ClrType { get; }

        public IReadOnlyCollection<PropertyTypeModelInfo> PropertyTypeInfos { get; }

        public PropertyTypeModelInfo PropertyTypeInfo(string alias) => PropertyTypeInfos.FirstOrDefault(x => x.Alias == alias);
    }
}
