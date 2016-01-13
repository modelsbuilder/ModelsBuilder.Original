using NUnit.Framework;
using Umbraco.Core.Models;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web;

namespace Umbraco.ModelsBuilder.Tests
{
    [TestFixture]
    public class PublishedContentTests
    {
        [Test]
        public void Test()
        {
            UmbracoInternals.InitializeConverters();
            UmbracoInternals.FreezeResolution();

            var type = UmbracoInternals.CreatePublishedContentType(1, "typ", new[]
            {
                UmbracoInternals.CreatePublishedPropertyType("prop", 1, "?")
            });

            var content = new TestElements.PublishedContent(type, new []
            {
                new TestElements.PublishedProperty("prop", "val")
            });

            var value = content.GetPropertyValue("prop");
            Assert.IsInstanceOf<string>(value);
            Assert.AreEqual("val", (string) value);

            var model = new ContentModel1(content);
            Assert.AreEqual("val", model.Prop);

            IPublishedContent um = model;
            var wrapped = um as PublishedContentExtended;
            while (wrapped != null /*&& ((IPublishedContentExtended) wrapped).HasAddedProperties == false*/)
                wrapped = (um = wrapped.Unwrap()) as PublishedContentExtended;

            Assert.AreSame(content, um);

            var nest = new ContentModel1(um);
            Assert.AreEqual("val", nest.Prop);
        }

        public class ContentModel1 : PublishedContentModel
        {
            public ContentModel1(IPublishedContent content)
                : base(content)
            { }

            public string Prop { get { return this.GetPropertyValue<string>("prop"); } }
        }
    }
}
