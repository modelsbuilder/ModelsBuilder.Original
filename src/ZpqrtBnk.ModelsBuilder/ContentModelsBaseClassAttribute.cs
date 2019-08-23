using System;

namespace ZpqrtBnk.ModelsBuilder
{
    /// <summary>
    /// Indicates the default base class for element models.
    /// </summary>
    /// <remarks>Otherwise it is PublishedElementModel.</remarks>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
    public sealed class ContentModelsBaseClassAttribute : Attribute
    {
        public ContentModelsBaseClassAttribute(Type type)
        { }

        public ContentModelsBaseClassAttribute(string aliasPattern, Type type)
        { }
    }
}