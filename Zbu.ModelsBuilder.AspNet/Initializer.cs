using System.Web.Compilation;
using Zbu.ModelsBuilder.Building;
using Zbu.ModelsBuilder.Configuration;

// NOTE
// see note in the Initialize method... this just cannot work so we configure things in web.config
// it's more explicit anyway... we keep the code here though so we're not tempted to try it again later.

// so... don't do it
//[assembly: PreApplicationStartMethod(typeof(Zbu.ModelsBuilder.AspNet.Initializer), "Initialize")]

namespace Zbu.ModelsBuilder.AspNet
{
    public static class Initializer
    {
        public static void Initialize()
        {
            // registers the models build provider
            if (Config.EnableAppCodeModels)
                BuildProvider.RegisterBuildProvider(".models", typeof(ModelsBuildProvider));

            // ensure that Zbu.ModelsBuilder is referenced
            BuildManager.AddReferencedAssembly(typeof(Builder).Assembly);
        }
    }
}
