using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZpqrtBnk.ModelzBuilder
{
    /// <summary>
    /// Indicates that no model should be generated for a specified property type alias.
    /// </summary>
    /// <remarks>Supports trailing wildcard eg "foo*".</remarks>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class IgnorePropertyTypeAttribute : Attribute
    {
        public IgnorePropertyTypeAttribute(string alias)
        {}
    }
}
