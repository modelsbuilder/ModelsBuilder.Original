using System;

namespace ZpqrtBnk.ModelsBuilder
{
    /// <summary>
    /// Indicates namespaces that should be used in generated models (in using clauses).
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
    public sealed class ModelsUsingAttribute : Attribute
    {
        public ModelsUsingAttribute(string usingNamespace)
        {}
    }
}

