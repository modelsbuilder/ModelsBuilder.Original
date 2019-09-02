using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZpqrtBnk.ModelsBuilder.Building
{
    public interface IBuilderFactory
    {
        IBuilder CreateBuilder(IList<TypeModel> typeModels, ParseResult parseResult, string modelsNamespace = null);
    }
}
