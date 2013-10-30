using System.Web;
using System.Web.Compilation;

[assembly: PreApplicationStartMethod(typeof(Zbu.ModelsBuilder.AspNet.Initializer), "Initialize")]

namespace Zbu.ModelsBuilder.AspNet
{
    public static class Initializer
    {
        public static void Initialize()
        {
            // registers the models build provider
            BuildProvider.RegisterBuildProvider(".models", typeof(ModelsBuildProvider));

            // ensure that Zbu.ModelsBuilder is referenced
            BuildManager.AddReferencedAssembly(typeof(IgnoreContentTypeAttribute).Assembly);
        }
    }
}
