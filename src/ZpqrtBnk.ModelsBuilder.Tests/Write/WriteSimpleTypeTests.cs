using Microsoft.CodeAnalysis;
using NUnit.Framework;
using Our.ModelsBuilder.Building;
using Our.ModelsBuilder.Options;
using Our.ModelsBuilder.Options.ContentTypes;
using Our.ModelsBuilder.Tests.Testing;
using Umbraco.Core.Models;

namespace Our.ModelsBuilder.Tests.Write
{
    [TestFixture]
    public class WriteSimpleTypeTests : TestsBase
    {
        private Compilation GetCompilation(CodeModelData modelData, PropertyStyle propertyStyle, FallbackStyle fallbackStyle)
        {
            var codeOptionsBuilder = new CodeOptionsBuilder();
            var modelBuilder = new CodeModelBuilder(new ModelsBuilderOptions(), codeOptionsBuilder.CodeOptions);
            var model = modelBuilder.Build(modelData);

            // FIXME stop this! use OPTIONS!
            model.ContentTypes.PropertyStyle = propertyStyle;
            model.ContentTypes.FallbackStyle = fallbackStyle;

            AssertCode.Compiles(model, out var compilation);
            return compilation;
        }

        private SemanticModel GetSemanticModel(PropertyStyle propertyStyle, FallbackStyle fallbackStyle)
        {
            // Umbraco returns nice, pascal-cased names

            var modelSource = new CodeModelData();

            var type1 = new ContentTypeModel
            {
                Id = 1,
                Alias = "type1",
                ParentId = 0,
                BaseContentType = null,
                Kind = ContentTypeKind.Content,
            };
            modelSource.ContentTypes.Add(type1);
            type1.Properties.Add(new PropertyTypeModel
            {
                Alias = "prop1",
                ContentType = type1,
                ValueType = typeof(string),
                Variations = ContentVariation.Nothing
            });
            type1.Properties.Add(new PropertyTypeModel
            {
                Alias = "prop2",
                ContentType = type1,
                ValueType = typeof(string),
                Variations = ContentVariation.Culture
            });
            type1.Properties.Add(new PropertyTypeModel
            {
                Alias = "prop3",
                ContentType = type1,
                ValueType = typeof(string),
                Variations = ContentVariation.Segment
            });
            type1.Properties.Add(new PropertyTypeModel
            {
                Alias = "prop4",
                ContentType = type1,
                ValueType = typeof(string),
                Variations = ContentVariation.CultureAndSegment
            });

            var compilation = GetCompilation(modelSource, propertyStyle, fallbackStyle);
            return compilation.GetSemanticModel("type1.generated");
        }

        [Test]
        public void Property_Nothing()
        {
            var semanticModel = GetSemanticModel(PropertyStyle.Property, FallbackStyle.Nothing);

            // only one "Prop1" property
            var symbols = semanticModel.LookupTypeSymbolMembers("Umbraco.Web.PublishedModels", "Type1", "Prop1");
            Assert.AreEqual(1, symbols.Length);
            AssertCode.IsProperty(symbols[0], true, true);

            // only one "Prop1" static (not an extension) method
            symbols = semanticModel.LookupTypeSymbolMembers("Umbraco.Web.PublishedModels", "Type1Extensions", "Prop1");
            Assert.AreEqual(1, symbols.Length);
            AssertCode.IsMethod(symbols[0], true, false, false, 1);

            // only one "Prop4" static (not an extension) method
            symbols = semanticModel.LookupTypeSymbolMembers("Umbraco.Web.PublishedModels", "Type1Extensions", "Prop4");
            Assert.AreEqual(1, symbols.Length);
            AssertCode.IsMethod(symbols[0], true, false, false, 3);

            Assert.IsNull(semanticModel.LookupTypeSymbol("Umbraco.Web.PublishedModels", "IType1"));
        }

        [Test]
        public void Property_Classic()
        {
            var semanticModel = GetSemanticModel(PropertyStyle.Property, FallbackStyle.Classic);

            // only one "Prop1" property
            var symbols = semanticModel.LookupTypeSymbolMembers("Umbraco.Web.PublishedModels", "Type1", "Prop1");
            Assert.AreEqual(1, symbols.Length);
            AssertCode.IsProperty(symbols[0], true, true);

            // only one "Prop1" static (not an extension) method
            symbols = semanticModel.LookupTypeSymbolMembers("Umbraco.Web.PublishedModels", "Type1Extensions", "Prop1");
            Assert.AreEqual(1, symbols.Length);
            AssertCode.IsMethod(symbols[0], true, false, false, 1);

            // only one "Prop4" static (not an extension) method
            symbols = semanticModel.LookupTypeSymbolMembers("Umbraco.Web.PublishedModels", "Type1Extensions", "Prop4");
            Assert.AreEqual(1, symbols.Length);
            AssertCode.IsMethod(symbols[0], true, false, false, 3);

            Assert.IsNull(semanticModel.LookupTypeSymbol("Umbraco.Web.PublishedModels", "IType1"));
        }

        [Test]
        public void Property_Modern()
        {
            var semanticModel = GetSemanticModel(PropertyStyle.Property, FallbackStyle.Modern);

            // only one "Prop1" property
            var symbols = semanticModel.LookupTypeSymbolMembers("Umbraco.Web.PublishedModels", "Type1", "Prop1");
            Assert.AreEqual(1, symbols.Length);
            AssertCode.IsProperty(symbols[0], true, true);

            // only one "Prop1" static (not an extension) method
            symbols = semanticModel.LookupTypeSymbolMembers("Umbraco.Web.PublishedModels", "Type1Extensions", "Prop1");
            Assert.AreEqual(1, symbols.Length);
            AssertCode.IsMethod(symbols[0], true, false, false, 1);

            // only one "Prop4" static (not an extension) method
            symbols = semanticModel.LookupTypeSymbolMembers("Umbraco.Web.PublishedModels", "Type1Extensions", "Prop4");
            Assert.AreEqual(1, symbols.Length);
            AssertCode.IsMethod(symbols[0], true, false, false, 3);

            Assert.IsNull(semanticModel.LookupTypeSymbol("Umbraco.Web.PublishedModels", "IType1"));
        }

        [Test]
        public void PropertyAndExtensionMethods_Nothing()
        {
            var semanticModel = GetSemanticModel(PropertyStyle.PropertyAndExtensionMethods, FallbackStyle.Nothing);

            // only one "Prop1" property
            var symbols = semanticModel.LookupTypeSymbolMembers("Umbraco.Web.PublishedModels", "Type1", "Prop1");
            Assert.AreEqual(1, symbols.Length);
            AssertCode.IsProperty(symbols[0], true, true);

            // only one "Prop1" extension method
            symbols = semanticModel.LookupTypeSymbolMembers("Umbraco.Web.PublishedModels", "Type1Extensions", "Prop1");
            Assert.AreEqual(1, symbols.Length);
            AssertCode.IsMethod(symbols[0], true, true, false, 1);

            // only one "Prop4" static (not an extension) method
            symbols = semanticModel.LookupTypeSymbolMembers("Umbraco.Web.PublishedModels", "Type1Extensions", "Prop4");
            Assert.AreEqual(1, symbols.Length);
            AssertCode.IsMethod(symbols[0], true, true, false, 3);

            Assert.IsNull(semanticModel.LookupTypeSymbol("Umbraco.Web.PublishedModels", "IType1"));
        }

        [Test]
        public void PropertyAndExtensionMethods_Classic()
        {
            var semanticModel = GetSemanticModel(PropertyStyle.PropertyAndExtensionMethods, FallbackStyle.Classic);

            // only one "Prop1" property
            var symbols = semanticModel.LookupTypeSymbolMembers("Umbraco.Web.PublishedModels", "Type1", "Prop1");
            Assert.AreEqual(1, symbols.Length);
            AssertCode.IsProperty(symbols[0], true, true);

            // only one "Prop1" extension method
            symbols = semanticModel.LookupTypeSymbolMembers("Umbraco.Web.PublishedModels", "Type1Extensions", "Prop1");
            Assert.AreEqual(1, symbols.Length);
            AssertCode.IsMethod(symbols[0], true, true, false, 3);

            // only one "Prop4" static (not an extension) method
            symbols = semanticModel.LookupTypeSymbolMembers("Umbraco.Web.PublishedModels", "Type1Extensions", "Prop4");
            Assert.AreEqual(1, symbols.Length);
            AssertCode.IsMethod(symbols[0], true, true, false, 5);

            Assert.IsNull(semanticModel.LookupTypeSymbol("Umbraco.Web.PublishedModels", "IType1"));
        }

        [Test]
        public void PropertyAndExtensionMethods_Modern()
        {
            var semanticModel = GetSemanticModel(PropertyStyle.PropertyAndExtensionMethods, FallbackStyle.Modern);

            // only one "Prop1" property
            var symbols = semanticModel.LookupTypeSymbolMembers("Umbraco.Web.PublishedModels", "Type1", "Prop1");
            Assert.AreEqual(1, symbols.Length);
            AssertCode.IsProperty(symbols[0], true, true);

            // only one "Prop1" extension method
            symbols = semanticModel.LookupTypeSymbolMembers("Umbraco.Web.PublishedModels", "Type1Extensions", "Prop1");
            Assert.AreEqual(1, symbols.Length);
            AssertCode.IsMethod(symbols[0], true, true, false, 2);

            // only one "Prop4" static (not an extension) method
            symbols = semanticModel.LookupTypeSymbolMembers("Umbraco.Web.PublishedModels", "Type1Extensions", "Prop4");
            Assert.AreEqual(1, symbols.Length);
            AssertCode.IsMethod(symbols[0], true, true, false, 4);

            Assert.IsNull(semanticModel.LookupTypeSymbol("Umbraco.Web.PublishedModels", "IType1"));
        }

        [Test]
        public void Methods_Nothing()
        {
            var semanticModel = GetSemanticModel(PropertyStyle.Methods, FallbackStyle.Nothing);

            // only one "Prop1" method
            var symbols = semanticModel.LookupTypeSymbolMembers("Umbraco.Web.PublishedModels", "Type1", "Prop1");
            Assert.AreEqual(1, symbols.Length);
            AssertCode.IsMethod(symbols[0], false, false, true, 0);

            // only one "Prop4" method
            symbols = semanticModel.LookupTypeSymbolMembers("Umbraco.Web.PublishedModels", "Type1", "Prop4");
            Assert.AreEqual(1, symbols.Length);
            AssertCode.IsMethod(symbols[0], false, false, true, 2);

            // only one "Prop1" static (not extension) method
            symbols = semanticModel.LookupTypeSymbolMembers("Umbraco.Web.PublishedModels", "Type1Extensions", "Prop1");
            Assert.AreEqual(1, symbols.Length);
            AssertCode.IsMethod(symbols[0], true, false, false, 1);

            // only one "Prop4" static (not extension) method
            symbols = semanticModel.LookupTypeSymbolMembers("Umbraco.Web.PublishedModels", "Type1Extensions", "Prop4");
            Assert.AreEqual(1, symbols.Length);
            AssertCode.IsMethod(symbols[0], true, false, false, 3);

            Assert.IsNull(semanticModel.LookupTypeSymbol("Umbraco.Web.PublishedModels", "IType1"));
        }

        [Test]
        public void Methods_Classic()
        {
            var semanticModel = GetSemanticModel(PropertyStyle.Methods, FallbackStyle.Classic);

            // only one "Prop1" method
            var symbols = semanticModel.LookupTypeSymbolMembers("Umbraco.Web.PublishedModels", "Type1", "Prop1");
            Assert.AreEqual(1, symbols.Length);
            AssertCode.IsMethod(symbols[0], false, false, true, 2);

            // only one "Prop4" method
            symbols = semanticModel.LookupTypeSymbolMembers("Umbraco.Web.PublishedModels", "Type1", "Prop4");
            Assert.AreEqual(1, symbols.Length);
            AssertCode.IsMethod(symbols[0], false, false, true, 4);

            // only one "Prop1" static (not extension) method
            symbols = semanticModel.LookupTypeSymbolMembers("Umbraco.Web.PublishedModels", "Type1Extensions", "Prop1");
            Assert.AreEqual(1, symbols.Length);
            AssertCode.IsMethod(symbols[0], true, false, false, 3);

            // only one "Prop4" static (not extension) method
            symbols = semanticModel.LookupTypeSymbolMembers("Umbraco.Web.PublishedModels", "Type1Extensions", "Prop4");
            Assert.AreEqual(1, symbols.Length);
            AssertCode.IsMethod(symbols[0], true, false, false, 5);

            Assert.IsNull(semanticModel.LookupTypeSymbol("Umbraco.Web.PublishedModels", "IType1"));
        }

        [Test]
        public void Methods_Modern()
        {
            var semanticModel = GetSemanticModel(PropertyStyle.Methods, FallbackStyle.Modern);

            // only one "Prop1" method
            var symbols = semanticModel.LookupTypeSymbolMembers("Umbraco.Web.PublishedModels", "Type1", "Prop1");
            Assert.AreEqual(1, symbols.Length);
            AssertCode.IsMethod(symbols[0], false, false, true, 1);

            // only one "Prop4" method
            symbols = semanticModel.LookupTypeSymbolMembers("Umbraco.Web.PublishedModels", "Type1", "Prop4");
            Assert.AreEqual(1, symbols.Length);
            AssertCode.IsMethod(symbols[0], false, false, true, 3);

            // only one "Prop1" static (not extension) method
            symbols = semanticModel.LookupTypeSymbolMembers("Umbraco.Web.PublishedModels", "Type1Extensions", "Prop1");
            Assert.AreEqual(1, symbols.Length);
            AssertCode.IsMethod(symbols[0], true, false, false, 2);

            // only one "Prop4" static (not extension) method
            symbols = semanticModel.LookupTypeSymbolMembers("Umbraco.Web.PublishedModels", "Type1Extensions", "Prop4");
            Assert.AreEqual(1, symbols.Length);
            AssertCode.IsMethod(symbols[0], true, false, false, 4);

            Assert.IsNull(semanticModel.LookupTypeSymbol("Umbraco.Web.PublishedModels", "IType1"));
        }

        [Test]
        public void ExtensionMethods_Nothing()
        {
            var semanticModel = GetSemanticModel(PropertyStyle.ExtensionMethods, FallbackStyle.Nothing);

            // nothing
            var symbols = semanticModel.LookupTypeSymbolMembers("Umbraco.Web.PublishedModels", "Type1", "Prop1");
            Assert.AreEqual(0, symbols.Length);

            symbols = semanticModel.LookupTypeSymbolMembers("Umbraco.Web.PublishedModels", "Type1", "Prop4");
            Assert.AreEqual(0, symbols.Length);

            // only one "Prop1" extension method
            symbols = semanticModel.LookupTypeSymbolMembers("Umbraco.Web.PublishedModels", "Type1Extensions", "Prop1");
            Assert.AreEqual(1, symbols.Length);
            AssertCode.IsMethod(symbols[0], true, true, false, 1);

            // only one "Prop4" extension method
            symbols = semanticModel.LookupTypeSymbolMembers("Umbraco.Web.PublishedModels", "Type1Extensions", "Prop4");
            Assert.AreEqual(1, symbols.Length);
            AssertCode.IsMethod(symbols[0], true, true, false, 3);

            Assert.IsNull(semanticModel.LookupTypeSymbol("Umbraco.Web.PublishedModels", "IType1"));
        }

        [Test]
        public void ExtensionMethods_Classic()
        {
            var semanticModel = GetSemanticModel(PropertyStyle.ExtensionMethods, FallbackStyle.Classic);

            // nothing
            var symbols = semanticModel.LookupTypeSymbolMembers("Umbraco.Web.PublishedModels", "Type1", "Prop1");
            Assert.AreEqual(0, symbols.Length);

            symbols = semanticModel.LookupTypeSymbolMembers("Umbraco.Web.PublishedModels", "Type1", "Prop4");
            Assert.AreEqual(0, symbols.Length);

            // only one "Prop1" extension method
            symbols = semanticModel.LookupTypeSymbolMembers("Umbraco.Web.PublishedModels", "Type1Extensions", "Prop1");
            Assert.AreEqual(1, symbols.Length);
            AssertCode.IsMethod(symbols[0], true, true, false, 3);

            // only one "Prop4" extension method
            symbols = semanticModel.LookupTypeSymbolMembers("Umbraco.Web.PublishedModels", "Type1Extensions", "Prop4");
            Assert.AreEqual(1, symbols.Length);
            AssertCode.IsMethod(symbols[0], true, true, false, 5);

            Assert.IsNull(semanticModel.LookupTypeSymbol("Umbraco.Web.PublishedModels", "IType1"));
        }

        [Test]
        public void ExtensionMethods_Modern()
        {
            var semanticModel = GetSemanticModel(PropertyStyle.ExtensionMethods, FallbackStyle.Modern);

            // nothing
            var symbols = semanticModel.LookupTypeSymbolMembers("Umbraco.Web.PublishedModels", "Type1", "Prop1");
            Assert.AreEqual(0, symbols.Length);

            symbols = semanticModel.LookupTypeSymbolMembers("Umbraco.Web.PublishedModels", "Type1", "Prop4");
            Assert.AreEqual(0, symbols.Length);

            // only one "Prop1" extension method
            symbols = semanticModel.LookupTypeSymbolMembers("Umbraco.Web.PublishedModels", "Type1Extensions", "Prop1");
            Assert.AreEqual(1, symbols.Length);
            AssertCode.IsMethod(symbols[0], true, true, false, 2);

            // only one "Prop4" extension method
            symbols = semanticModel.LookupTypeSymbolMembers("Umbraco.Web.PublishedModels", "Type1Extensions", "Prop4");
            Assert.AreEqual(1, symbols.Length);
            AssertCode.IsMethod(symbols[0], true, true, false, 4);

            Assert.IsNull(semanticModel.LookupTypeSymbol("Umbraco.Web.PublishedModels", "IType1"));
        }
    }
}