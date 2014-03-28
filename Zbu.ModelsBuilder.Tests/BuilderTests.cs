using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Umbraco.Core;
using Umbraco.Core.PropertyEditors;
using Umbraco.Core.PropertyEditors.ValueConverters;
using Zbu.ModelsBuilder;

namespace Zbu.ModelsBuilder.Tests
{
    [TestFixture]
    public class BuilderTests
    {
        //[SetUp]
        [TestFixtureSetUp]
        public void Setup()
        {
            //var app = Umbraco.Web.Standalone.StandaloneApplication.GetApplication(Environment.CurrentDirectory)
            //    .WithoutApplicationEventHandler<Umbraco.Web.Search.ExamineEvents>()
            //    .WithApplicationEventHandler<AppHandler>();
            ////if (app.Started == false)
            //app.Start();
        }

        [Test]
        public void Test()
        {
            IList<TypeModel> types;
            using (var umbraco = Umbraco.Application.GetApplication(TestOptions.ConnectionString, TestOptions.DatabaseProvider))
            {
                types = umbraco.GetContentTypes();
            }

            var builder = new TextBuilder();
            var sb = new StringBuilder();
            builder.Prepare(types);
            foreach (var type in types)
                builder.Generate(sb, type);
            Console.WriteLine(sb.ToString());
        }

        [Test]
        public void Test2()
        {
            const string code = @"
using Umbraco.Web;
using Zbu.ModelsBuilder;
using Umbraco.Core.Models.PublishedContent;

[assembly: IgnoreContentType(""ccc"")]

namespace Zbu.ModelsBuilder.Tests.Models
{
    // don't create a model for ddd
    // invalid here though roslyn just ignores it
    //[assembly: IgnoreContentType(""ddd"")]

    // create a mixin for MixinTest but with a different class name
    [PublishedContentModel(""MixinTest"")]
    public partial class MixinTestRenamed
    { }

    // create a model for bbb but with a different class name
    [PublishedContentModel(""bbb"")]
    public partial class SpecialBbb
    { }

    // create a model for ...
    [IgnorePropertyType(""nomDeLEleve"")] // but don't include that property
    public partial class LoskDalmosk
    { }

    // create a model for page...
    public partial class Page
    {
        // but don't include that property because I'm doing it
        // must do it because the legacy converter can't tell the type of the property
        [IgnorePropertyType(""alternativeText"")]
        //public AlternateText AlternativeText { get { return this.GetPropertyValue<AlternateText>(""alternativeText""); } }
        public string AlternativeText { get { return this.GetPropertyValue<string>(""alternativeText""); } } // fixme
    }
}
";

            IList<TypeModel> types;
            using (var umbraco = Umbraco.Application.GetApplication(TestOptions.ConnectionString, TestOptions.DatabaseProvider))
            {
                types = umbraco.GetContentTypes();
            }

            var builder = new TextBuilder();
            var sb = new StringBuilder();
            builder.Prepare(types);
            builder.Parse(code, types);
            foreach (var type in types)
                builder.Generate(sb, type);
            Console.WriteLine(sb.ToString());
        }

        [TestCase("int", typeof (int))]
        [TestCase("IEnumerable<int>", typeof(IEnumerable<int>))]
        [TestCase("Zbu.ModelsBuilder.Tests.BuilderTestsClass1", typeof(BuilderTestsClass1))]
        [TestCase("Zbu.ModelsBuilder.Tests.BuilderTests.Class1", typeof(Class1))]
        public void WriteClrType(string expected, Type input)
        {
            var builder = new TextBuilder();
            var sb = new StringBuilder();
            builder.WriteClrType(sb, input);
            Assert.AreEqual(expected, sb.ToString());
        }

        [TestCase("int", typeof(int))]
        [TestCase("IEnumerable<int>", typeof(IEnumerable<int>))]
        [TestCase("BuilderTestsClass1", typeof(BuilderTestsClass1))]
        [TestCase("BuilderTests.Class1", typeof(Class1))]
        public void WriteClrTypeUsing(string expected, Type input)
        {
            var builder = new TextBuilder();
            builder.Using.Add("Zbu.ModelsBuilder.Tests");
            var sb = new StringBuilder();
            builder.WriteClrType(sb, input);
            Assert.AreEqual(expected, sb.ToString());
        }

        public class Class1 { }
    }

    class BuilderTestsClass1 {}
}
