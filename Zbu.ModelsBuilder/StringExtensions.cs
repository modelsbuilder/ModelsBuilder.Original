using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zbu.ModelsBuilder
{
    public static class StringExtensions
    {
        public static bool InvariantEquals(this string s, string other)
        {
            return String.Equals(s, other, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
