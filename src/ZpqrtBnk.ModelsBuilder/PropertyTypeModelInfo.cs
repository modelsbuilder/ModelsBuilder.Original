using System;

namespace ZpqrtBnk.ModelsBuilder
{
    public class PropertyTypeModelInfo
    {
        public PropertyTypeModelInfo(string alias, string clrName, Type clrType)
        {
            Alias = alias;
            ClrName = clrName;
            ClrType = clrType;
        }

        public string Alias { get; }
        public string ClrName { get; }
        public Type ClrType { get; }
    }
}