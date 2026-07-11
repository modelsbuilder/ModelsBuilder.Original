using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Our.ModelsBuilder.Building;
using Our.ModelsBuilder.Options;
using Our.ModelsBuilder.Options.ContentTypes;
using Our.ModelsBuilder.Tests.Testing;

namespace Our.ModelsBuilder.Tests.Write
{
    [TestFixture]
    public class WriteClrTypeTests : TestsBase
    {
        [TestCase("int", typeof(int))]
        [TestCase("IEnumerable<int>", typeof(IEnumerable<int>))]
        [TestCase("Our.ModelsBuilder.Tests.BuilderTestsClass1", typeof(BuilderTestsClass1))]
        [TestCase("Our.ModelsBuilder.Tests.Write.WriteClrTypeTests.Class1", typeof(Class1))]
        public void WriteClrType(string expected, Type input)
        {
            var codeModelBuilder = new CodeModelBuilder(new ModelsBuilderOptions(), new CodeOptions(new ContentTypesCodeOptions()));
            var codeModel = codeModelBuilder.Build(new CodeModelData());
            codeModel.ModelsNamespace = "ModelsNamespace";

            var writer = new CodeWriter(codeModel);

            writer.WriteClrType(input);
            Assert.AreEqual(expected, writer.Code);
        }

        [TestCase("int", typeof(int))]
        [TestCase("IEnumerable<int>", typeof(IEnumerable<int>))]
        [TestCase("BuilderTestsClass1", typeof(BuilderTestsClass1))]
        [TestCase("WriteClrTypeTests.Class1", typeof(Class1))]
        public void WriteClrTypeUsing(string expected, Type input)
        {
            var codeModelBuilder = new CodeModelBuilder(new ModelsBuilderOptions(), new CodeOptions(new ContentTypesCodeOptions()));
            var codeModel = codeModelBuilder.Build(new CodeModelData());
            codeModel.ModelsNamespace = "ModelsNamespace";
            codeModel.Using.Add("Our.ModelsBuilder.Tests"); // BuilderTestsClass1
            codeModel.Using.Add("Our.ModelsBuilder.Tests.Write"); // WriteClrTypeTests.Class1

            var writer = new CodeWriter(codeModel);

            writer.WriteClrType(input);
            Assert.AreEqual(expected, writer.Code);
        }

        [TestCase(true, true, "Borked", typeof(global::System.Text.ASCIIEncoding), "System.Text.ASCIIEncoding")]
        [TestCase(true, false, "Borked", typeof(global::System.Text.ASCIIEncoding), "ASCIIEncoding")]
        [TestCase(false, true, "Borked", typeof(global::System.Text.ASCIIEncoding), "System.Text.ASCIIEncoding")]
        [TestCase(false, false, "Borked", typeof(global::System.Text.ASCIIEncoding), "System.Text.ASCIIEncoding")]

        [TestCase(true, true, "Our.ModelsBuilder.Tests", typeof(global::System.Text.ASCIIEncoding), "global::System.Text.ASCIIEncoding")]
        [TestCase(true, false, "Our.ModelsBuilder.Tests", typeof(global::System.Text.ASCIIEncoding), "global::System.Text.ASCIIEncoding")]
        [TestCase(false, true, "Our.ModelsBuilder.Tests", typeof(global::System.Text.ASCIIEncoding), "global::System.Text.ASCIIEncoding")]
        [TestCase(false, false, "Our.ModelsBuilder.Tests", typeof(global::System.Text.ASCIIEncoding), "global::System.Text.ASCIIEncoding")]

        [TestCase(true, true, "Borked", typeof(StringBuilder), "StringBuilder")]
        [TestCase(true, false, "Borked", typeof(StringBuilder), "StringBuilder")]
        [TestCase(false, true, "Borked", typeof(StringBuilder), "System.Text.StringBuilder")] // magic? using = not ambiguous
        [TestCase(false, false, "Borked", typeof(StringBuilder), "System.Text.StringBuilder")]

        [TestCase(true, true, "Our.ModelsBuilder.Tests", typeof(StringBuilder), "StringBuilder")]
        [TestCase(true, false, "Our.ModelsBuilder.Tests", typeof(StringBuilder), "StringBuilder")]
        [TestCase(false, true, "Our.ModelsBuilder.Tests", typeof(StringBuilder), "global::System.Text.StringBuilder")] // magic? in ns = ambiguous
        [TestCase(false, false, "Our.ModelsBuilder.Tests", typeof(StringBuilder), "global::System.Text.StringBuilder")]
        public void WriteClrType_Ambiguous_Ns(bool usingSystem, bool usingZb, string ns, Type type, string expected)
        {
            var codeModel = new CodeModel(new CodeModelData()) { ModelsNamespace = ns };
            if (usingSystem) codeModel.Using.Add("System.Text");
            if (usingZb) codeModel.Using.Add("Our.ModelsBuilder.Tests");

            var writer = new CodeWriter(codeModel);

            writer.WriteClrType(type);

            Assert.AreEqual(expected, writer.Code);
        }

        [Test]
        public void WriteClrType_AmbiguousWithNested()
        {
            var codeModel = new CodeModel(new CodeModelData()) { ModelsNamespace = "SomeRandomNamespace" };
            codeModel.Using.Add("System.Text");
            codeModel.Using.Add("Our.ModelsBuilder.Tests");

            var writer = new CodeWriter(codeModel);

            writer.WriteClrType(typeof(ASCIIEncoding.Nested));

            // full type name is needed but not global::
            Assert.AreEqual("Our.ModelsBuilder.Tests.ASCIIEncoding.Nested", writer.Code);
        }

        public class Class1 { }
    }
}