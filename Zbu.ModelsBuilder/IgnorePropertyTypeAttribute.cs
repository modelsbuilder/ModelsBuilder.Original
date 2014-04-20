using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zbu.ModelsBuilder
{
    /// <summary>
    /// Indicates that no model should be generated for a specified property type alias.
    /// </summary>
    /// <remarks>Supports trailing wildcard eg "foo*".</remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
    public sealed class IgnorePropertyTypeAttribute : Attribute
    {
        public IgnorePropertyTypeAttribute(string alias)
        {}
    }

    // usage
    /*
    [IgnorePropertyType("foo")] // simply ignore property with alias 'foo'
    [IgnorePropertyType("bar*")] // ignore properties with alias starting with 'bar'
    [RenamePropertyType("duh", "DuhRenamed")] // generate property with a a different name
    public partial class MyModel
    {
        [IgnorePropertyType("nil")] // simply ignore property with alias 'nil'
        public string NilRenamed
        {
            // and provide our own implementation
            get { return this.GetPropertyValue<string>("nil"); }
        }
    }
    */
}
