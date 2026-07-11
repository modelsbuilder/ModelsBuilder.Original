using Our.ModelsBuilder.Options;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace Our.ModelsBuilder.Building;

/// <summary>
/// Represents a model of the content types code to generate.
/// </summary>
public class ContentTypesCodeModel
{
    private static class Defaults
    {
        public static readonly string ElementBaseClassClrFullName = typeof(PublishedElementModel).FullName!;
        public static readonly string ContentBaseClassClrFullName = typeof(PublishedContentModel).FullName!;
        public static readonly string ElementBaseInterfaceClrFullName = typeof(IPublishedElement).FullName!;
        public static readonly string ContentBaseInterfaceClrFullName = typeof(IPublishedContent).FullName!;
    }

    static ContentTypesCodeModel()
    {
        // FIXME what's the point?!
        var n = typeof(IPublishedContent).FullName;
    }

    // FIXME
    public List<ContentTypeModel> ContentTypes { get; set; }


    public string ElementBaseClassClrFullName { get; set; } = Defaults.ElementBaseClassClrFullName;
    public string ContentBaseClassClrFullName { get; set; } = Defaults.ContentBaseClassClrFullName;
    public string ElementBaseInterfaceClrFullName { get; set; } = Defaults.ElementBaseInterfaceClrFullName;
    public string ContentBaseInterfaceClrFullName { get; set; } = Defaults.ContentBaseInterfaceClrFullName;
}