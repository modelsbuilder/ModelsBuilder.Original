using System;

namespace ZpqrtBnk.ModelsBuilder
{
    /// <summary>
    /// Indicates a model name for a specified content alias.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
    public sealed class RenameContentTypeAttribute : Attribute
    {
        public RenameContentTypeAttribute(string alias, string name)
        {}
    }
}
