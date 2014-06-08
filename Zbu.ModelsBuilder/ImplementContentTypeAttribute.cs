using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zbu.ModelsBuilder
{
    /// <summary>
    /// Indicates that a (partial) class defines the model type for a specific alias.
    /// </summary>
    /// <remarks>Though a model will be generated - so that is the way to register a rename.</remarks>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class ImplementContentTypeAttribute : Attribute
    {
        public ImplementContentTypeAttribute(string alias)
        { }
    }
}
