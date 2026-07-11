
using Microsoft.AspNetCore.Html;
using Umbraco.Cms.Infrastructure.ModelsBuilder;

namespace My.Namespace;

public partial class Yooki
{
    [ImplementPropertyType("propAlias")] 
    public HtmlString PropAliasImpl => default; //new HtmlString(this.Value<string>("html"));
}
