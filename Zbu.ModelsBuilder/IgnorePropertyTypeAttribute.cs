using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zbu.ModelsBuilder
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
    public class IgnorePropertyTypeAttribute : Attribute
    {
        public IgnorePropertyTypeAttribute(string alias)
        {}
    }
}
