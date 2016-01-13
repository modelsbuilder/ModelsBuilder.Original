using Umbraco.Web;
using Umbraco.ModelsBuilder;
using Umbraco.Core.Models.PublishedContent;

[assembly: IgnoreContentType("ccc")]

namespace Umbraco.ModelsBuilder.Tests.Models
{
    // don't create a model for ddd
    // invalid here though roslyn just ignores it
    //[assembly: IgnoreContentType("ddd")]

    // create a mixin for MixinTest but with a different class name
    // don't do it on the interfaces - will be automatic
    [ImplementContentType("MixinTest")]
    public partial class MixinTestRenamed
    { }

    // create a model for bbb but with a different class name
    [ImplementContentType("bbb")]
    public partial class SpecialBbb
    { }

    // create a model for ...
    [IgnorePropertyType("nomDeLEleve")] // but don't include that property
    public partial class LoskDalmosk
    { }

    // create a model for page...
    public partial class Page
    {
        // but don't include that property because I'm doing it
        // must do it because the legacy converter can't tell the type of the property
        [ImplementPropertyType("alternativeText")]
        //public AlternateText AlternativeText { get { return this.GetPropertyValue<AlternateText>("alternativeText"); } }
        public string AlternativeText { get { return this.GetPropertyValue<string>("alternativeText"); } } // fixme
    }
}