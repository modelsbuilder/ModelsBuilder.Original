using System;

namespace ZpqrtBnk.ModelsBuilder
{
    /// <summary>
    /// Indicates the default base class for element models.
    /// </summary>
    /// <remarks>Otherwise it is PublishedElementModel.</remarks>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
    public sealed class ElementModelsBaseClassAttribute : Attribute
    {
        public ElementModelsBaseClassAttribute(Type type)
        { }

        public ElementModelsBaseClassAttribute(string aliasPattern, Type type)
        { }
    }
}
