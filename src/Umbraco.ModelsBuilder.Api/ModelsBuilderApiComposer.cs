using Umbraco.Core;
using Umbraco.Core.Components;
using Umbraco.Core.Composing;
using Umbraco.ModelsBuilder.Umbraco;

namespace Umbraco.ModelsBuilder.Api
{
    [ComposeAfter(typeof(ModelsBuilderComposer))]
    [RuntimeLevel(MinLevel = RuntimeLevel.Run)]
    public class ModelsBuilderApiComposer : IUserComposer
    {
        public void Compose(Composition composition)
        {
            // setup the API if enabled (and in debug mode)
            // we *must* register the controller 'cos it is hidden from type finder
            // (to prevent it from being always and automatically registered)
            if (composition.Configs.ModelsBuilder().ApiServer)
                composition.Register(typeof(ModelsBuilderApiController), Lifetime.Request);
        }
    }
}
