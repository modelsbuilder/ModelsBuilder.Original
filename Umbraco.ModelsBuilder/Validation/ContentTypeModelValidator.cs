using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Umbraco.Core;
using Umbraco.Core.Configuration;
using Umbraco.Core.Models;
using Umbraco.ModelsBuilder.Configuration;
using Umbraco.Web.Editors;
using Umbraco.Web.Models.ContentEditing;

namespace Umbraco.ModelsBuilder.Validation
{
    /// <summary>
    /// Used to validate the aliases for the content type when MB is enabled to ensure that
    /// no illegal aliases are used
    /// </summary>
    internal class ContentTypeModelValidator : ContentTypeModelValidatorBase<DocumentTypeSave, PropertyTypeBasic>
    {
    }

    /// <summary>
    /// Used to validate the aliases for the content type when MB is enabled to ensure that
    /// no illegal aliases are used
    /// </summary>
    internal class MediaTypeModelValidator : ContentTypeModelValidatorBase<MediaTypeSave, PropertyTypeBasic>
    {
    }

    /// <summary>
    /// Used to validate the aliases for the content type when MB is enabled to ensure that
    /// no illegal aliases are used
    /// </summary>
    internal class MemberTypeModelValidator : ContentTypeModelValidatorBase<MemberTypeSave, MemberPropertyTypeBasic>
    {
    }

    internal abstract class ContentTypeModelValidatorBase<TModel, TProperty> : EditorValidator<TModel>
        where TModel: ContentTypeSave<TProperty>
        where TProperty: PropertyTypeBasic
    {
        protected override IEnumerable<ValidationResult> PerformValidate(TModel model)
        {
            //don't do anything if we're not enabled
            if (UmbracoConfig.For.ModelsBuilder().Enable)
            {
                var properties = model.Groups.SelectMany(x => x.Properties)
                    .Where(x => x.Inherited == false)
                    .ToArray();

                foreach (var prop in properties)
                {
                    //we need to return the field name with an index so it's wired up correctly
                    var propertyGroup = model.Groups.Single(x => x.Properties.Contains(prop));
                    var groupIndex = model.Groups.IndexOf(propertyGroup);
                    var propertyIndex = propertyGroup.Properties.IndexOf(prop);

                    var validationResult = ValidateProperty(prop, groupIndex, propertyIndex);
                    if (validationResult != null)
                    {
                        yield return validationResult;
                    }
                }
            }
        }

        private ValidationResult ValidateProperty(PropertyTypeBasic property, int groupIndex, int propertyIndex)
        {
            //don't let them match any properties or methods in IPublishedContent
            //TODO: There are probably more!
            var reservedProperties = typeof(IPublishedContent).GetProperties().Select(x => x.Name).ToArray();
            var reservedMethods = typeof(IPublishedContent).GetMethods().Select(x => x.Name).ToArray();

            var alias = property.Alias;

            if (reservedProperties.InvariantContains(alias) || reservedMethods.InvariantContains(alias))
            {
                return new ValidationResult(
                    string.Format("The alias {0} is a reserved term and cannot be used", alias), new[]
                    {
                        string.Format("Groups[{0}].Properties[{1}].Alias", groupIndex, propertyIndex)
                    });
            }

            return null;
        }
    }
}
