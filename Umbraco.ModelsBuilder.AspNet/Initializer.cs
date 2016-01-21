using System.Web.Compilation;
using Umbraco.Core.Configuration;
using Umbraco.ModelsBuilder.Building;
using Umbraco.ModelsBuilder.Configuration;

// NOTE
// see note in the Initialize method... this just cannot work so we configure things in web.config
// it's more explicit anyway... we keep the code here though so we're not tempted to try it again later.

// so... don't do it
//[assembly: PreApplicationStartMethod(typeof(Umbraco.ModelsBuilder.AspNet.Initializer), "Initialize")]

namespace Umbraco.ModelsBuilder.AspNet
{
    public static class Initializer
    {
        public static void Initialize()
        {
            // registers the models build provider
            if (UmbracoConfig.For.ModelsBuilder().ModelsMode.IsAnyAppCode())
                BuildProvider.RegisterBuildProvider(".models", typeof(ModelsBuildProvider));

            // ensure that Umbraco.ModelsBuilder is referenced
            BuildManager.AddReferencedAssembly(typeof(Builder).Assembly);
        }
    }
}
