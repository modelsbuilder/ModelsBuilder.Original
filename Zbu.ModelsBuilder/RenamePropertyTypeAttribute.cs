using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zbu.ModelsBuilder
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class RenamePropertyTypeAttribute : Attribute
    {
        public RenamePropertyTypeAttribute(string alias, string name)
        {}
    }
}
