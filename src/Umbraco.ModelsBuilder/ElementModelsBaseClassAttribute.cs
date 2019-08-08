using System;

namespace Umbraco.ModelsBuilder
{
    /// <summary>
    /// Indicates the default base class for element models.
    /// </summary>
    /// <remarks>Otherwise it is PublishedElementModel. Would make sense to inherit from PublishedElementModel.</remarks>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
    public sealed class ElementModelsBaseClassAttribute : Attribute
    {
        public ElementModelsBaseClassAttribute(Type type)
        {}
    }
}

