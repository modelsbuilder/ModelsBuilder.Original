using NUnit.Framework;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web;

namespace ZpqrtBnk.ModelsBuilder.Tests
{
    [TestFixture]
    public class PublishedContentTests
    {
        [Test]
        public void Test()
        {
            //UmbracoInternals.InitializeConverters();
            //UmbracoInternals.FreezeResolution();

            var type = UmbracoInternals.CreatePublishedContentType(1, "typ", new[]
            {
                UmbracoInternals.CreatePublishedPropertyType("prop", 1, "?")
            });

            var content = new TestElements.PublishedContent(type, new []
            {
                new TestElements.PublishedProperty("prop", "val")
            });

            var value = content.Value("prop");
            Assert.IsInstanceOf<string>(value);
            Assert.AreEqual("val", (string) value);

            var model = new ContentModel1(content);
            Assert.AreEqual("val", model.Prop);

            IPublishedContent um = model;
            var wrapped = um as PublishedContentWrapped;
            while (wrapped != null /*&& ((IPublishedContentExtended) wrapped).HasAddedProperties == false*/)
                wrapped = (um = wrapped.Unwrap()) as PublishedContentWrapped;

            Assert.AreSame(content, um);

            var nest = new ContentModel1(um);
            Assert.AreEqual("val", nest.Prop);
        }

        [Test]
        public void Test2()
        {
            var type = UmbracoInternals.CreatePublishedContentType(1, "typ", new[]
            {
                UmbracoInternals.CreatePublishedPropertyType("prop", 1, "?")
            });

            var content = new TestElements.PublishedContent(type, new[]
            {
                new TestElements.PublishedProperty("prop", "val")
            });

            string value;

            // test: content is IPublishedContent

            // default version Value<T>() that ships with Core
            value = content.Value("prop", fallback: Fallback.ToDefaultValue, defaultValue: "oops");

            // default Value<T>() that ships with Core
            value = content.Value<string>("prop");

            // default Value() that ships with Core
            value = (string) content.Value("prop");


            var model = new ContentModel1(content);

            // test: model is strongly typed

            // default version Value() that ships with Core
            value = model.Value("prop", fallback: Fallback.ToDefaultValue, defaultValue: "oops");

            // fallback-function version of Value() that MB provides
            value = model.Value("prop", fallback: x => x.Value<string>("prop"));

            // nothing is ambiguous because of generics
            value = (string) model.Value("prop"); // non-generic Value()
            value = model.Value<string>("prop"); // generic Value<T>()
        }

        public class ContentModel1 : PublishedContentModel
        {
            public ContentModel1(IPublishedContent content)
                : base(content)
            { }

            public string Prop => this.Value<string>("prop");
        }
    }
}
