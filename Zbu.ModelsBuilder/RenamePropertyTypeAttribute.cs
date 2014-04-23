using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zbu.ModelsBuilder
{
    /// <summary>
    /// Indicates a model name for a specified property alias.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
    public sealed class RenamePropertyTypeAttribute : Attribute
    {
        public RenamePropertyTypeAttribute(string alias, string name)
        {}

        public RenamePropertyTypeAttribute(string alias)
        {}
    }
}
