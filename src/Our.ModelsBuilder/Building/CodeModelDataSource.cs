using Our.ModelsBuilder.Umbraco;

namespace Our.ModelsBuilder.Building;

/// <summary>
/// Implements the default <see cref="ICodeModelDataSource"/>.
/// </summary>
public class CodeModelDataSource : ICodeModelDataSource
{
    private readonly UmbracoServices _umbracoServices;

    /// <summary>
    /// Initializes a new instance of the <see cref="CodeModelDataSource"/> class.
    /// </summary>
    public CodeModelDataSource(UmbracoServices umbracoServices)
    {
        _umbracoServices = umbracoServices;
    }

    /// <inheritdoc />
    public CodeModelData GetCodeModelData()
    {
        //// TODO: switch to using TypeModel.ItemTypes
        //static ContentTypeKind MapKind(TypeModel.ItemTypes type)
        //{
        //    return type switch
        //    {
        //        TypeModel.ItemTypes.Content => ContentTypeKind.Content,
        //        TypeModel.ItemTypes.Element => ContentTypeKind.Element,
        //        TypeModel.ItemTypes.Media => ContentTypeKind.Media,
        //        TypeModel.ItemTypes.Member => ContentTypeKind.Member,
        //        _ => throw new NotSupportedException()
        //    };
        //}

        //static List<PropertyTypeModel> MapProperties(ContentTypeModel model, List<PropertyModel> properties)
        //    => properties.Select(x => new PropertyTypeModel
        //        {
        //            ContentType = model,
        //            Alias = x.Alias,
        //            Name = x.Name,
        //            Description = x.Description,
        //            Variations = x.Variations,
        //            ValueType = x.ModelClrType
        //        })
        //        .ToList();

        //static List<ContentTypeModel> MapContentTypes(IList<TypeModel> contentTypes)
        //    => contentTypes.Select(x =>
        //        {
        //            var model = new ContentTypeModel
        //            {
        //                Id = x.Id,
        //                Alias = x.Alias,
        //                Name = x.Name,
        //                Description = x.Description,
        //                Variations = x.Variations,
        //                Kind = MapKind(x.ItemType),
        //                ParentId = x.ParentId,
        //                ExpandedProperties = { }
        //            };
        //            model.Properties.AddRange(MapProperties(model, x.Properties));
        //            return model;
        //        })
        //        .ToList();

        //// FIXME property.Variations?
        //// FIXME missing stuff in property
        //// FIXME content type variations?
        //// FIXME content type expanded properties
        //// FIXME missing stuff in content type

        return new CodeModelData
        {
            ContentTypes = _umbracoServices.GetAllTypes() // MapContentTypes(_umbracoServices.GetContentTypes())
        };
    }
}