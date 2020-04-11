using Our.ModelsBuilder.Options;
using Umbraco.Web.Models.ContentEditing;

namespace Our.ModelsBuilder.Validation
{
    /// <summary>
    /// Validates the content type aliases when ModelsBuilder is enabled.
    /// </summary>
    public class ContentTypeModelValidator : ContentTypeModelValidatorBase<DocumentTypeSave, PropertyTypeBasic>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ContentTypeModelValidator"/> class.
        /// </summary>
        public ContentTypeModelValidator(ModelsBuilderOptions options) 
            : base(options)
        { }
    }
}
