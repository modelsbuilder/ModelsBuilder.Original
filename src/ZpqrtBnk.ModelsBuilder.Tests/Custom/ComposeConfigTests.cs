using Our.ModelsBuilder.Options;
using Umbraco.Core.Composing;
namespace Our.ModelsBuilder.Tests.Custom
{
    // ReSharper disable once UnusedMember.Global, reason: composer
    public class CustomConfigComposer : IOptionsComposer 
    {
        public void Compose(Composition composition)
        {
            composition.ConfigureCodeOptions(optionsBuilder => optionsBuilder.ContentTypes.IgnoreContentType("blah"));
        }
    }
}
