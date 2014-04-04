using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace Zbu.ModelsBuilder
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
    public sealed class IgnoreContentTypeAttribute : Attribute
    {
        public IgnoreContentTypeAttribute(string alias)
        {}
    }
}
