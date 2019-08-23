using System;

namespace ZpqrtBnk.ModelsBuilder
{
    /// <summary>
    /// Indicates the default base class for content models.
    /// </summary>
    /// <remarks>Otherwise it is PublishedContentModel.</remarks>
    [Obsolete("Use ElementModelBaseClassAttribute or ContentModelBaseClassAttribute")]
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
    public sealed class ModelsBaseClassAttribute : Attribute
    {
        public ModelsBaseClassAttribute(Type type)
        {}
    }
}

