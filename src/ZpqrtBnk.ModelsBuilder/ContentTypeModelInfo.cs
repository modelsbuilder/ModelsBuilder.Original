using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZpqrtBnk.ModelsBuilder
{
    public class ContentTypeModelInfo
    {
        public ContentTypeModelInfo(string alias, string clrName, Type clrType, params PropertyTypeModelInfo[] properties)
        {
            Alias = alias;
            ClrName = clrName;
            ClrType = clrType;
            Properties = properties;
        }

        public string Alias { get; }
        public string ClrName { get; }
        public Type ClrType { get; }

        public IReadOnlyCollection<PropertyTypeModelInfo> Properties { get; }

        public PropertyTypeModelInfo Property(string alias) => Properties.FirstOrDefault(x => x.Alias == alias);
    }
}
