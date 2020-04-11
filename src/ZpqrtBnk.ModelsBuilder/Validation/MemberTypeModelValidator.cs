using Our.ModelsBuilder.Options;
using Umbraco.Web.Models.ContentEditing;

namespace Our.ModelsBuilder.Validation
{
    /// <summary>
    /// Validates the member type aliases when ModelsBuilder is enabled.
    /// </summary>
    public class MemberTypeModelValidator : ContentTypeModelValidatorBase<MemberTypeSave, MemberPropertyTypeBasic>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MemberTypeModelValidator"/> class.
        /// </summary>
        public MemberTypeModelValidator(ModelsBuilderOptions options) 
            : base(options)
        { }
    }
}