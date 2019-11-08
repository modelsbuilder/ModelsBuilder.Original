using System.Collections.Generic;

namespace ZpqrtBnk.ModelsBuilder.Building
{
    public class BuilderFactory : IBuilderFactory
    {
        public IBuilder CreateBuilder()
        {
            return new Builder();
        }
    }
}