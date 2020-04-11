using Our.ModelsBuilder.Options;
using Umbraco.Web.Models.ContentEditing;

namespace Our.ModelsBuilder.Validation
{
    /// <summary>
    /// Validates the media type aliases when ModelsBuilder is enabled.
    /// </summary>
    public class MediaTypeModelValidator : ContentTypeModelValidatorBase<MediaTypeSave, PropertyTypeBasic>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MediaTypeModelValidator"/> class.
        /// </summary>
        public MediaTypeModelValidator(ModelsBuilderOptions options) 
            : base(options)
        { }
    }
}