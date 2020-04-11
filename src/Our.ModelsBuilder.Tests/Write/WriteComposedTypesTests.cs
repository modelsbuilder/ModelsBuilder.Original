using Microsoft.CodeAnalysis;
using NUnit.Framework;
using Our.ModelsBuilder.Building;
using Our.ModelsBuilder.Options;
using Our.ModelsBuilder.Options.ContentTypes;
using Our.ModelsBuilder.Tests.Testing;

namespace Our.ModelsBuilder.Tests.Write
{
    // TODO:
    // parents tree
    // mixin tree w/ duplicates
    //
    // then in another class:
    // ContentTypeStyle.Flatten, .Ignore, .Default, .PureMixin
    // PropertyTypeStyle.Flatten, .Ignore, .Default
    // PropertyTypeMemberStyle.Property, .Method, .ExtensionMethod, etc
    // + merge everything from builder that does not parse?
    //
    // then in another class, test the parser

    [TestFixture]
    public class WriteComposedTypesTests : TestsBase
    {
        private static Compilation GetCompilation(CodeModelData modelData, PropertyStyle propertyStyle, FallbackStyle fallbackStyle)
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

        // type1 <- type2
        // generates type1 as class
        //           type2 as class, inherits type1
        [Test]
        public void ParentAndChild_Property_Nothing()
        {
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
                ValueType = typeof(string)
            });

            var type2 = new ContentTypeModel
            {
                Id = 2,
                Alias = "type2",
                ParentId = 1,
                BaseContentType = type1,
                Kind = ContentTypeKind.Content,
            };
            modelSource.ContentTypes.Add(type2);
            type2.Properties.Add(new PropertyTypeModel
            {
                Alias = "prop2",
                ContentType = type2,
                ValueType = typeof(string)
            });

            var compilation = GetCompilation(modelSource, PropertyStyle.Property, FallbackStyle.Nothing);
            var semanticModel = compilation.GetSemanticModel("type1.generated");

            // Type1 and IType1 have only one Prop1 property
            AssertCode.HasType(semanticModel, "Umbraco.Web.PublishedModels.Type1");
            AssertCode.HasNotType(semanticModel, "Umbraco.Web.PublishedModels.IType1");
            AssertCode.HasProperty(semanticModel, "Umbraco.Web.PublishedModels.Type1.Prop1", true, true);
            AssertCode.HasNotMember(semanticModel, "Umbraco.Web.PublishedModels.Type1.Prop2");

            // Type1 has extensions with only one Prop1 static (not an extension) method
            AssertCode.HasType(semanticModel, "Umbraco.Web.PublishedModels.Type1Extensions");
            AssertCode.HasMethod(semanticModel, "Umbraco.Web.PublishedModels.Type1Extensions.Prop1", true, false, false, 1);

            // Type2 has only one Prop2 property
            AssertCode.HasType(semanticModel, "Umbraco.Web.PublishedModels.Type2", out var type2Symbol);
            AssertCode.HasNotType(semanticModel, "Umbraco.Web.PublishedModels.IType2");
            AssertCode.HasNotMember(semanticModel, "Umbraco.Web.PublishedModels.Type2.Prop1");
            AssertCode.HasProperty(semanticModel, "Umbraco.Web.PublishedModels.Type2.Prop2", true, true);

            // Type2 has extensions with only one Prop2 static (not an extension) method
            AssertCode.HasType(semanticModel, "Umbraco.Web.PublishedModels.Type2Extensions");
            AssertCode.HasMethod(semanticModel, "Umbraco.Web.PublishedModels.Type2Extensions.Prop2", true, false, false, 1);

            // Type2 inherits Type1, no interface
            Assert.AreEqual("Type1", type2Symbol.BaseType.Name);
            Assert.AreEqual(0, type2Symbol.Interfaces.Length);
        }

        // type1 o- type2
        // generates type1 as interface + class
        //           type2 as class, declares & implements type1
        [Test]
        public void MixinAndComposed_Property_Nothing()
        {
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
                ValueType = typeof(string)
            });

            var type2 = new ContentTypeModel
            {
                Id = 2,
                Alias = "type2",
                ParentId = 0, // FIXME: -1 or 0 for none??
                BaseContentType = null,
                Kind = ContentTypeKind.Content,
            };
            modelSource.ContentTypes.Add(type2);
            type2.MixinContentTypes.Add(type1);
            type2.Properties.Add(new PropertyTypeModel
            {
                Alias = "prop2",
                ContentType = type2,
                ValueType = typeof(string)
            });

            var compilation = GetCompilation(modelSource, PropertyStyle.Property, FallbackStyle.Nothing);
            var semanticModel = compilation.GetSemanticModel("type1.generated");

            // Type1 and IType1 have only one Prop1 property
            AssertCode.HasType(semanticModel, "Umbraco.Web.PublishedModels.Type1");
            AssertCode.HasType(semanticModel, "Umbraco.Web.PublishedModels.IType1", out var type1Interface);
            AssertCode.HasProperty(semanticModel, "Umbraco.Web.PublishedModels.Type1.Prop1", true, true);
            AssertCode.HasProperty(semanticModel, "Umbraco.Web.PublishedModels.IType1.Prop1", true, false);
            AssertCode.HasNotMember(semanticModel, "Umbraco.Web.PublishedModels.Type1.Prop2");
            AssertCode.HasNotMember(semanticModel, "Umbraco.Web.PublishedModels.IType1.Prop2");

            // Type1 has extensions with only one Prop1 static (not an extension) method
            AssertCode.HasType(semanticModel, "Umbraco.Web.PublishedModels.Type1Extensions");
            AssertCode.HasMethod(semanticModel, "Umbraco.Web.PublishedModels.Type1Extensions.Prop1", true, false, false, 1);

            // Type2 has both Prop1 and Prop2 properties
            AssertCode.HasType(semanticModel, "Umbraco.Web.PublishedModels.Type2", out var type2Symbol);
            AssertCode.HasNotType(semanticModel, "Umbraco.Web.PublishedModels.IType2");
            AssertCode.HasProperty(semanticModel, "Umbraco.Web.PublishedModels.Type2.Prop1", true, true);
            AssertCode.HasProperty(semanticModel, "Umbraco.Web.PublishedModels.Type2.Prop2", true, true);

            // Type2 has extensions with only one Prop2 static (not an extension) method
            AssertCode.HasType(semanticModel, "Umbraco.Web.PublishedModels.Type2Extensions");
            AssertCode.HasMethod(semanticModel, "Umbraco.Web.PublishedModels.Type2Extensions.Prop2", true, false, false, 1);

            // Type2 has no base type, but implements IType1
            Assert.AreEqual("PublishedContentModel", type2Symbol.BaseType.Name);
            Assert.Contains(type1Interface, type2Symbol.Interfaces);
        }

        // type1 <- type2 o- type3
        // generates type1 as interface + class
        //           type2 as interface + class
        //           type3 as class, declares type2 + implements type1, type2
        [Test]
        public void ParentMixinAndComposed_Property_Nothing()
        {
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
                ValueType = typeof(string)
            });

            var type2 = new ContentTypeModel
            {
                Id = 2,
                Alias = "type2",
                ParentId = 1,
                BaseContentType = type1,
                Kind = ContentTypeKind.Content,
            };
            modelSource.ContentTypes.Add(type2);
            type2.Properties.Add(new PropertyTypeModel
            {
                Alias = "prop2",
                ContentType = type2,
                ValueType = typeof(string)
            });

            var type3 = new ContentTypeModel
            {
                Id = 3,
                Alias = "type3",
                ParentId = 0,
                BaseContentType = null,
                Kind = ContentTypeKind.Content,
            };
            modelSource.ContentTypes.Add(type3);
            type3.MixinContentTypes.Add(type2);
            type3.Properties.Add(new PropertyTypeModel
            {
                Alias = "prop3",
                ContentType = type3,
                ValueType = typeof(string)
            });

            var compilation = GetCompilation(modelSource, PropertyStyle.Property, FallbackStyle.Nothing);
            var semanticModel = compilation.GetSemanticModel("type1.generated");

            // types and interfaces
            AssertCode.HasType(semanticModel, "Umbraco.Web.PublishedModels.Type1");
            AssertCode.HasType(semanticModel, "Umbraco.Web.PublishedModels.IType1", out var type1Interface);
            AssertCode.HasType(semanticModel, "Umbraco.Web.PublishedModels.Type2", out var type2Symbol);
            AssertCode.HasType(semanticModel, "Umbraco.Web.PublishedModels.IType2", out var type2Interface);
            AssertCode.HasType(semanticModel, "Umbraco.Web.PublishedModels.Type3", out var type3Symbol);
            AssertCode.HasNotType(semanticModel, "Umbraco.Web.PublishedModels.IType3");

            Assert.AreEqual(1, type2Symbol.Interfaces.Length);
            Assert.Contains(type2Interface, type2Symbol.Interfaces);

            Assert.AreEqual(1, type2Interface.Interfaces.Length);
            Assert.Contains(type1Interface, type2Interface.Interfaces);

            Assert.AreEqual(1, type3Symbol.Interfaces.Length);
            Assert.Contains(type2Interface, type3Symbol.Interfaces);

            // properties
            AssertCode.HasProperty(semanticModel, "Umbraco.Web.PublishedModels.Type1.Prop1", true, true);
            AssertCode.HasProperty(semanticModel, "Umbraco.Web.PublishedModels.Type2.Prop2", true, true);
            AssertCode.HasProperty(semanticModel, "Umbraco.Web.PublishedModels.Type3.Prop1", true, true);
            AssertCode.HasProperty(semanticModel, "Umbraco.Web.PublishedModels.Type3.Prop2", true, true);
            AssertCode.HasProperty(semanticModel, "Umbraco.Web.PublishedModels.Type3.Prop3", true, true);

            // extensions
            AssertCode.HasMethod(semanticModel, "Umbraco.Web.PublishedModels.Type1Extensions.Prop1", true, false, false, 1);
            AssertCode.HasMethod(semanticModel, "Umbraco.Web.PublishedModels.Type2Extensions.Prop2", true, false, false, 1);
            AssertCode.HasMethod(semanticModel, "Umbraco.Web.PublishedModels.Type3Extensions.Prop3", true, false, false, 1);
        }

        // type1 o- type2 <- type3
        // generates type1 as interface + class
        //           type2 as class, declares & implements type1
        //           type3 as class, inherits type2 
        [Test]
        public void MixinAndParentComposed_Property_Nothing()
        {
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
                ValueType = typeof(string)
            });

            var type2 = new ContentTypeModel
            {
                Id = 2,
                Alias = "type2",
                ParentId = 0,
                BaseContentType = null,
                Kind = ContentTypeKind.Content,
            };
            modelSource.ContentTypes.Add(type2);
            type2.MixinContentTypes.Add(type1);
            type2.Properties.Add(new PropertyTypeModel
            {
                Alias = "prop2",
                ContentType = type2,
                ValueType = typeof(string)
            });

            var type3 = new ContentTypeModel
            {
                Id = 3,
                Alias = "type3",
                ParentId = 2,
                BaseContentType = type2,
                Kind = ContentTypeKind.Content,
            };
            modelSource.ContentTypes.Add(type3);
            type3.Properties.Add(new PropertyTypeModel
            {
                Alias = "prop3",
                ContentType = type3,
                ValueType = typeof(string)
            });

            var compilation = GetCompilation(modelSource, PropertyStyle.Property, FallbackStyle.Nothing);
            var semanticModel = compilation.GetSemanticModel("type1.generated");

            // type1 o- type2 <- type3
            // generates type1 as interface + class
            //           type2 as class, declares & implements type1
            //           type3 as class, inherits type2 

            // types and interfaces
            AssertCode.HasType(semanticModel, "Umbraco.Web.PublishedModels.Type1");
            AssertCode.HasType(semanticModel, "Umbraco.Web.PublishedModels.IType1", out var type1Interface);
            AssertCode.HasType(semanticModel, "Umbraco.Web.PublishedModels.Type2", out var type2Symbol);
            AssertCode.HasNotType(semanticModel, "Umbraco.Web.PublishedModels.IType2");
            AssertCode.HasType(semanticModel, "Umbraco.Web.PublishedModels.Type3", out var type3Symbol);
            AssertCode.HasNotType(semanticModel, "Umbraco.Web.PublishedModels.IType3");

            Assert.AreEqual(1, type2Symbol.Interfaces.Length);
            Assert.Contains(type1Interface, type2Symbol.Interfaces);

            Assert.AreEqual(0, type3Symbol.Interfaces.Length);
            Assert.AreEqual(type2Symbol, type3Symbol.BaseType);

            // properties
            AssertCode.HasProperty(semanticModel, "Umbraco.Web.PublishedModels.Type1.Prop1", true, true);
            AssertCode.HasProperty(semanticModel, "Umbraco.Web.PublishedModels.Type2.Prop1", true, true);
            AssertCode.HasProperty(semanticModel, "Umbraco.Web.PublishedModels.Type2.Prop2", true, true);
            AssertCode.HasProperty(semanticModel, "Umbraco.Web.PublishedModels.Type3.Prop3", true, true);

            // extensions
            AssertCode.HasMethod(semanticModel, "Umbraco.Web.PublishedModels.Type1Extensions.Prop1", true, false, false, 1);
            AssertCode.HasMethod(semanticModel, "Umbraco.Web.PublishedModels.Type2Extensions.Prop2", true, false, false, 1);
            AssertCode.HasMethod(semanticModel, "Umbraco.Web.PublishedModels.Type3Extensions.Prop3", true, false, false, 1);
        }
    }
}
