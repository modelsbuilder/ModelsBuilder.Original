using System;

namespace ZpqrtBnk.ModelsBuilder
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
