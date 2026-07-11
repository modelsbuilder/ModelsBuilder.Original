// ReSharper disable once CheckNamespace, reason: extensions
namespace Umbraco.Cms.Core.Models.PublishedContent;

public class FallbackInfos<TModel, TValue>
    where TModel : IPublishedElement
{
    public FallbackInfos(TModel model, string propertyAlias, string culture, string segment)
    {
        Model = model;
        PropertyAlias = propertyAlias;
        Culture = culture;
        Segment = segment;
    }

    public TModel Model { get; }

    public string PropertyAlias { get; }

    public string Culture { get; }

    public string Segment { get; }

    // TODO: think about having a FallbackValue factory to let people enhance it?
    public FallbackValue<TModel, TValue> CreateFallbackValue() => new(this);

    public FallbackValue<TModel, TValue> To(params int[] values) => CreateFallbackValue().To(values);

    public FallbackValue<TModel, TValue> Languages() => CreateFallbackValue().Languages();

    /// <summary>
    /// Fall back to ancestors.
    /// </summary>
    /// <returns></returns>
    public FallbackValue<TModel, TValue> Ancestors() => CreateFallbackValue().Ancestors();

    /// <summary>
    /// Fall back to a default value.
    /// </summary>
    /// <param name="defaultValue"></param>
    /// <returns></returns>
    public FallbackValue<TModel, TValue> Default(TValue defaultValue) => CreateFallbackValue().Default(defaultValue);
}