using Our.ModelsBuilder.Umbraco;
using Umbraco.Core.Composing;

namespace Our.ModelsBuilder.Options
{
    [ComposeAfter(typeof(ModelsBuilderComposer))]
    public interface IOptionsComposer : IComposer
    { }
}
