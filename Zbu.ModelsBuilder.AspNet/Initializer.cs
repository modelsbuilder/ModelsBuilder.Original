using System.Web;
using System.Web.Compilation;

[assembly: PreApplicationStartMethod(typeof(Zbu.ModelsBuilder.AspNet.Initializer), "Initialize")]

namespace Zbu.ModelsBuilder.AspNet
{
    public static class Initializer
    {
        public static void Initialize()
        {
            BuildProvider.RegisterBuildProvider(".models", typeof(ModelsBuildProvider));
            BuildManager.AddReferencedAssembly(typeof(IgnoreContentTypeAttribute).Assembly);
        }
    }
}
