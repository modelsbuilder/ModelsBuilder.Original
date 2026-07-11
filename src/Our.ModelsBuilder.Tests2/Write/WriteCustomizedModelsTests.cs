using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using NUnit.Framework;
using Our.ModelsBuilder.Building;
using Our.ModelsBuilder.Options;
using Our.ModelsBuilder.Options.ContentTypes;
using Our.ModelsBuilder.Tests.Custom;
using Our.ModelsBuilder.Tests.Testing;
using Umbraco.Core.Models;

namespace Our.ModelsBuilder.Tests.Write
{
    [TestFixture]
    public class WriteCustomizedModelsTests : TestsBase
    {
        [Test]
        public void CustomNamespace()
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
                ValueType = typeof(string),
            });

            var codeOptionsBuilder = new CodeOptionsBuilder();
            codeOptionsBuilder.SetModelsNamespace("Some.Models");
            var modelBuilder = new CodeModelBuilder(new ModelsBuilderOptions(), codeOptionsBuilder.CodeOptions);
            var model = modelBuilder.Build(modelSource);

            // FIXME this should work with options (and everywhere in this file)
            model.ContentTypes.PropertyStyle = PropertyStyle.Property;
            model.ContentTypes.FallbackStyle = FallbackStyle.Nothing;
            AssertCode.Compiles(model, out var compilation);

            var semanticModel = compilation.GetSemanticModel("type1.generated");

            AssertCode.HasNotNamespace(semanticModel, "Umbraco.Web.PublishedModels");
            AssertCode.HasType(semanticModel, "Some.Models.Type1");
        }

        [Test]
        public void RenameContentType()
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
                ValueType = typeof(string),
            });

            var codeOptionsBuilder = new CodeOptionsBuilder();
            codeOptionsBuilder.ContentTypes.SetContentTypeClrName("type1", "Type2");
            var modelBuilder = new CodeModelBuilder(new ModelsBuilderOptions(), codeOptionsBuilder.CodeOptions);
            var model = modelBuilder.Build(modelSource);

            model.ContentTypes.PropertyStyle = PropertyStyle.Property;
            model.ContentTypes.FallbackStyle = FallbackStyle.Nothing;
            AssertCode.Compiles(model, out var compilation);

            var semanticModel = compilation.GetSemanticModel("type1.generated");

            AssertCode.HasNotType(semanticModel, "Umbraco.Web.PublishedModels.Type1");
            AssertCode.HasType(semanticModel, "Umbraco.Web.PublishedModels.Type2");
        }

        [Test]
        public void OmitPartialConstructor()
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
                ValueType = typeof(string),
            });

            var codeOptionsBuilder = new CodeOptionsBuilder();
            codeOptionsBuilder.ContentTypes.ContentTypeModelHasConstructor("Type1"); // FIXME: must use the ClrName not the alias!
            var modelBuilder = new CodeModelBuilder(new ModelsBuilderOptions(), codeOptionsBuilder.CodeOptions);
            var model = modelBuilder.Build(modelSource);

            model.ContentTypes.PropertyStyle = PropertyStyle.Property;
            model.ContentTypes.FallbackStyle = FallbackStyle.Nothing;

            // must supply a constructor, else it cannot compile
            // and therefore, must inherit from PublishedContentModel *here*
            var sources = new Dictionary<string, string>
            {
                ["type1.custom"] = @"
using Umbraco.Core.Models.PublishedContent;
namespace Umbraco.Web.PublishedModels
{
    public partial class Type1 : PublishedContentModel
    {
        public Type1(IPublishedContent content) : base(content) { }
    }
}"
            };

            AssertCode.Compiles(model, out var compilation, sources);

            var semanticModel = compilation.GetSemanticModel("type1.generated");

            AssertCode.HasType(semanticModel, "Umbraco.Web.PublishedModels.Type1", out var symbol);

            var constructors = symbol.GetMembers()
                .OfType<IMethodSymbol>() // methods
                .Where(x => x.MethodKind == MethodKind.Constructor) // which are ctors
                .Where(x => x.Parameters.Length > 0) // not the default ctor (always exists)
                .ToList();

            Assert.AreEqual(1, constructors.Count);
            var constructor = constructors[0];

            Assert.AreEqual(1, constructor.Locations.Length);
            Assert.AreEqual("type1.custom", constructor.Locations[0].SourceTree.FilePath);
        }

        [Test]
        public void IgnoreParentContentType()
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

            var codeOptionsBuilder = new CodeOptionsBuilder();
            codeOptionsBuilder.ContentTypes.IgnoreContentType("type1");
            var modelBuilder = new CodeModelBuilder(new ModelsBuilderOptions(), codeOptionsBuilder.CodeOptions);
            var model = modelBuilder.Build(modelSource);

            model.ContentTypes.PropertyStyle = PropertyStyle.Property;
            model.ContentTypes.FallbackStyle = FallbackStyle.Nothing;
            AssertCode.Compiles(model, out var compilation);

            Assert.AreEqual(1, compilation.SyntaxTrees.Count());
            Assert.AreEqual("infos.generated", compilation.SyntaxTrees.First().FilePath);
        }

        [Test]
        public void IgnoreMixinContentType()
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


            var codeOptionsBuilder = new CodeOptionsBuilder();
            codeOptionsBuilder.ContentTypes.IgnoreContentType("type1");
            var modelBuilder = new CodeModelBuilder(new ModelsBuilderOptions(), codeOptionsBuilder.CodeOptions);
            var model = modelBuilder.Build(modelSource);

            model.ContentTypes.PropertyStyle = PropertyStyle.Property;
            model.ContentTypes.FallbackStyle = FallbackStyle.Nothing;
            AssertCode.Compiles(model, out var compilation);
            var semanticModel = compilation.GetSemanticModel("type2.generated");

            // Type1 does not exist
            AssertCode.HasNotType(semanticModel, "Umbraco.Web.PublishedModels.Type1");
            AssertCode.HasNotType(semanticModel, "Umbraco.Web.PublishedModels.IType1");
            AssertCode.HasNotType(semanticModel, "Umbraco.Web.PublishedModels.Type1Extensions");

            // Type2 has only the Prop2 properties
            AssertCode.HasType(semanticModel, "Umbraco.Web.PublishedModels.Type2", out var type2Symbol);
            AssertCode.HasNotType(semanticModel, "Umbraco.Web.PublishedModels.IType2");
            AssertCode.HasNotMember(semanticModel, "Umbraco.Web.PublishedModels.Type2.Prop1");
            AssertCode.HasProperty(semanticModel, "Umbraco.Web.PublishedModels.Type2.Prop2", true, true);

            // Type2 has extensions with only one Prop2 static (not an extension) method
            AssertCode.HasType(semanticModel, "Umbraco.Web.PublishedModels.Type2Extensions");
            AssertCode.HasMethod(semanticModel, "Umbraco.Web.PublishedModels.Type2Extensions.Prop2", true, false, false, 1);

            // Type2 has no base type, no interface
            Assert.AreEqual("PublishedContentModel", type2Symbol.BaseType.Name);
            Assert.AreEqual(0, type2Symbol.Interfaces.Length);
        }

        [Test]
        public void IgnoreContentTypeWithWildcard()
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

            var type2 = new ContentTypeModel
            {
                Id = 2,
                Alias = "ttype2",
                ParentId = 0,
                BaseContentType = null,
                Kind = ContentTypeKind.Content,
            };
            modelSource.ContentTypes.Add(type2);

            var type3 = new ContentTypeModel
            {
                Id = 3,
                Alias = "type3",
                ParentId = 0,
                BaseContentType = null,
                Kind = ContentTypeKind.Content,
            };
            modelSource.ContentTypes.Add(type3);

            var type4 = new ContentTypeModel
            {
                Id = 4,
                Alias = "ttype4",
                ParentId = 0,
                BaseContentType = null,
                Kind = ContentTypeKind.Content,
            };
            modelSource.ContentTypes.Add(type4);

            var codeOptionsBuilder = new CodeOptionsBuilder();
            codeOptionsBuilder.ContentTypes.IgnoreContentType("ttype*");
            var modelBuilder = new CodeModelBuilder(new ModelsBuilderOptions(), codeOptionsBuilder.CodeOptions);
            var model = modelBuilder.Build(modelSource);

            model.ContentTypes.PropertyStyle = PropertyStyle.Property;
            model.ContentTypes.FallbackStyle = FallbackStyle.Nothing;
            AssertCode.Compiles(model, out var compilation);

            Assert.AreEqual(3, compilation.SyntaxTrees.Count());
            Assert.IsTrue(compilation.SyntaxTrees.Any(x => x.FilePath == "infos.generated"));
            Assert.IsTrue(compilation.SyntaxTrees.Any(x => x.FilePath == "type1.generated"));
            Assert.IsTrue(compilation.SyntaxTrees.Any(x => x.FilePath == "type3.generated"));
        }

        [Test]
        public void RenamePropertyType()
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
                ValueType = typeof(string),
            });
            type1.Properties.Add(new PropertyTypeModel
            {
                Alias = "prop2",
                ContentType = type1,
                ValueType = typeof(string),
            });

            var codeOptionsBuilder = new CodeOptionsBuilder();
            codeOptionsBuilder.ContentTypes.SetPropertyTypeClrName(ContentTypeIdentity.Alias("type1"), "prop1", "PropX");
            var modelBuilder = new CodeModelBuilder(new ModelsBuilderOptions(), codeOptionsBuilder.CodeOptions);
            var model = modelBuilder.Build(modelSource);

            model.ContentTypes.PropertyStyle = PropertyStyle.Property;
            model.ContentTypes.FallbackStyle = FallbackStyle.Nothing;
            AssertCode.Compiles(model, out var compilation);

            var semanticModel = compilation.GetSemanticModel("type1.generated");

            AssertCode.HasNotMember(semanticModel, "Umbraco.Web.PublishedModels.Type1.Prop1");
            AssertCode.HasProperty(semanticModel, "Umbraco.Web.PublishedModels.Type1.PropX", true, true);
            AssertCode.HasProperty(semanticModel, "Umbraco.Web.PublishedModels.Type1.Prop2", true, true);

            AssertCode.HasNotMember(semanticModel, "Umbraco.Web.PublishedModels.Type1Extensions.Prop1");
            AssertCode.HasMethod(semanticModel, "Umbraco.Web.PublishedModels.Type1Extensions.PropX", true, false, false);
            AssertCode.HasMethod(semanticModel, "Umbraco.Web.PublishedModels.Type1Extensions.Prop2", true, false, false);
        }

        [Test]
        public void IgnorePropertyType()
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
                ValueType = typeof(string),
            });
            type1.Properties.Add(new PropertyTypeModel
            {
                Alias = "prop2",
                ContentType = type1,
                ValueType = typeof(string),
            });

            var codeOptionsBuilder = new CodeOptionsBuilder();
            codeOptionsBuilder.ContentTypes.IgnorePropertyType(ContentTypeIdentity.Alias("type1"), "prop1");
            var modelBuilder = new CodeModelBuilder(new ModelsBuilderOptions(), codeOptionsBuilder.CodeOptions);
            var model = modelBuilder.Build(modelSource);

            model.ContentTypes.PropertyStyle = PropertyStyle.Property;
            model.ContentTypes.FallbackStyle = FallbackStyle.Nothing;
            AssertCode.Compiles(model, out var compilation);

            var semanticModel = compilation.GetSemanticModel("type1.generated");

            AssertCode.HasNotMember(semanticModel, "Umbraco.Web.PublishedModels.Type1.Prop1");
            AssertCode.HasProperty(semanticModel, "Umbraco.Web.PublishedModels.Type1.Prop2", true, true);

            AssertCode.HasNotMember(semanticModel, "Umbraco.Web.PublishedModels.Type1Extensions.Prop1");
            AssertCode.HasMethod(semanticModel, "Umbraco.Web.PublishedModels.Type1Extensions.Prop2", true, false, false);
        }

        [Test]
        public void IgnoreMixinPropertyType()
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

            var codeOptionsBuilder = new CodeOptionsBuilder();
            codeOptionsBuilder.ContentTypes.IgnorePropertyType(ContentTypeIdentity.Alias("type1"), "prop1");
            var modelBuilder = new CodeModelBuilder(new ModelsBuilderOptions(), codeOptionsBuilder.CodeOptions);
            var model = modelBuilder.Build(modelSource);

            model.ContentTypes.PropertyStyle = PropertyStyle.Property;
            model.ContentTypes.FallbackStyle = FallbackStyle.Nothing;
            AssertCode.Compiles(model, out var compilation);
            var semanticModel = compilation.GetSemanticModel("type2.generated");

            AssertCode.HasNotMember(semanticModel, "Umbraco.Web.PublishedModels.Type1.Prop1");
            AssertCode.HasNotMember(semanticModel, "Umbraco.Web.PublishedModels.Type2.Prop1");
            AssertCode.HasProperty(semanticModel, "Umbraco.Web.PublishedModels.Type2.Prop2", true, true);

            AssertCode.HasNotType(semanticModel, "Umbraco.Web.PublishedModels.Type1Extensions");
            AssertCode.HasMethod(semanticModel, "Umbraco.Web.PublishedModels.Type2Extensions.Prop2", true, false, false);

        }

        [Test]
        public void IgnoreParentPropertyType()
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

            var codeOptionsBuilder = new CodeOptionsBuilder();
            codeOptionsBuilder.ContentTypes.IgnorePropertyType(ContentTypeIdentity.Alias("type1"), "prop1");
            var modelBuilder = new CodeModelBuilder(new ModelsBuilderOptions(), codeOptionsBuilder.CodeOptions);
            var model = modelBuilder.Build(modelSource);

            model.ContentTypes.PropertyStyle = PropertyStyle.Property;
            model.ContentTypes.FallbackStyle = FallbackStyle.Nothing;
            AssertCode.Compiles(model, out var compilation);
            var semanticModel = compilation.GetSemanticModel("type2.generated");

            AssertCode.HasNotMember(semanticModel, "Umbraco.Web.PublishedModels.Type1.Prop1");
            AssertCode.HasNotMember(semanticModel, "Umbraco.Web.PublishedModels.Type2.Prop1");
            AssertCode.HasProperty(semanticModel, "Umbraco.Web.PublishedModels.Type2.Prop2", true, true);

            AssertCode.HasNotType(semanticModel, "Umbraco.Web.PublishedModels.Type1Extensions");
            AssertCode.HasMethod(semanticModel, "Umbraco.Web.PublishedModels.Type2Extensions.Prop2", true, false, false);

        }

        [Test]
        public void IgnorePropertyTypeWithWildcard()
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
                ValueType = typeof(string),
            });
            type1.Properties.Add(new PropertyTypeModel
            {
                Alias = "pprop2",
                ContentType = type1,
                ValueType = typeof(string),
            });
            type1.Properties.Add(new PropertyTypeModel
            {
                Alias = "prop3",
                ContentType = type1,
                ValueType = typeof(string),
            });
            type1.Properties.Add(new PropertyTypeModel
            {
                Alias = "pprop4",
                ContentType = type1,
                ValueType = typeof(string),
            });

            var codeOptionsBuilder = new CodeOptionsBuilder();
            codeOptionsBuilder.ContentTypes.IgnorePropertyType(ContentTypeIdentity.Alias("type1"), "pprop*");
            var modelBuilder = new CodeModelBuilder(new ModelsBuilderOptions(), codeOptionsBuilder.CodeOptions);
            var model = modelBuilder.Build(modelSource);

            model.ContentTypes.PropertyStyle = PropertyStyle.Property;
            model.ContentTypes.FallbackStyle = FallbackStyle.Nothing;
            AssertCode.Compiles(model, out var compilation);

            var semanticModel = compilation.GetSemanticModel("type1.generated");

            AssertCode.HasNotMember(semanticModel, "Umbraco.Web.PublishedModels.Type1.Prop2");
            AssertCode.HasNotMember(semanticModel, "Umbraco.Web.PublishedModels.Type1.Prop4");
            AssertCode.HasProperty(semanticModel, "Umbraco.Web.PublishedModels.Type1.Prop1", true, true);
            AssertCode.HasProperty(semanticModel, "Umbraco.Web.PublishedModels.Type1.Prop3", true, true);

            AssertCode.HasNotMember(semanticModel, "Umbraco.Web.PublishedModels.Type1Extensions.Prop2");
            AssertCode.HasNotMember(semanticModel, "Umbraco.Web.PublishedModels.Type1Extensions.Prop4");
            AssertCode.HasMethod(semanticModel, "Umbraco.Web.PublishedModels.Type1Extensions.Prop1", true, false, false);
            AssertCode.HasMethod(semanticModel, "Umbraco.Web.PublishedModels.Type1Extensions.Prop3", true, false, false);
        }

        [Test]
        public void CustomClrNames()
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
                ValueType = typeof(string),
                Variations = ContentVariation.Nothing
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
                ValueType = typeof(string),
                Variations = ContentVariation.Nothing
            });

            var options = new ModelsBuilderOptions();
            var codeOptionsBuilder = new CodeOptionsBuilder();
            var modelBuilder = new CustomCodeModelBuilder(options, codeOptionsBuilder.CodeOptions, new CustomClrNamesBuilder(options, codeOptionsBuilder.CodeOptions));
            var model = modelBuilder.Build(modelSource);

            model.ContentTypes.PropertyStyle = PropertyStyle.Property;
            model.ContentTypes.FallbackStyle = FallbackStyle.Nothing;

            AssertCode.Compiles(model, out var compilation);
            var semanticModel = compilation.GetSemanticModel("type1.generated");

            // these don't exist
            AssertCode.HasNotType(semanticModel, "Umbraco.Web.PublishedModels.Type1");
            AssertCode.HasNotType(semanticModel, "Umbraco.Web.PublishedModels.Type2");
            AssertCode.HasNotType(semanticModel, "Umbraco.Web.PublishedModels.IType1");
            AssertCode.HasNotType(semanticModel, "Umbraco.Web.PublishedModels.IType2");

            // Type1 and IType1 have only one Prop1 property
            AssertCode.HasType(semanticModel, "Umbraco.Web.PublishedModels.XXType1YY", out var type1Symbol);
            AssertCode.HasNotType(semanticModel, "Umbraco.Web.PublishedModels.XXIType1YY");
            AssertCode.HasProperty(semanticModel, "Umbraco.Web.PublishedModels.XXType1YY.PPProp1QQ", true, true);
            AssertCode.HasNotMember(semanticModel, "Umbraco.Web.PublishedModels.XXType1YY.PPProp2QQ");

            // Type1 has extensions with only one Prop1 static (not an extension) method
            AssertCode.HasType(semanticModel, "Umbraco.Web.PublishedModels.XXType1YYExtensions");
            AssertCode.HasMethod(semanticModel, "Umbraco.Web.PublishedModels.XXType1YYExtensions.PPProp1QQ", true, false, false, 1);

            // Type2 has only one Prop2 property
            AssertCode.HasType(semanticModel, "Umbraco.Web.PublishedModels.XXType2YY", out var type2Symbol);
            AssertCode.HasNotType(semanticModel, "Umbraco.Web.PublishedModels.XXIType2YY");
            AssertCode.HasNotMember(semanticModel, "Umbraco.Web.PublishedModels.XXType2YY.PPProp1QQ");
            AssertCode.HasProperty(semanticModel, "Umbraco.Web.PublishedModels.XXType2YY.PPProp2QQ", true, true);

            // Type2 has extensions with only one Prop2 static (not an extension) method
            AssertCode.HasType(semanticModel, "Umbraco.Web.PublishedModels.XXType2YYExtensions");
            AssertCode.HasMethod(semanticModel, "Umbraco.Web.PublishedModels.XXType2YYExtensions.PPProp2QQ", true, false, false, 1);

            // Type2 inherits Type1, no interface
            Assert.AreEqual(type1Symbol, type2Symbol.BaseType);
            Assert.AreEqual(0, type2Symbol.Interfaces.Length);
        }

        private class CustomClrNamesBuilder : ContentTypesCodeModelBuilder
        {
            public CustomClrNamesBuilder(ModelsBuilderOptions options, CodeOptions codeOptions) 
                : base(options, codeOptions)
            { }

            protected override string GetClrName(ContentTypeModel contentTypeModel)
            {
                return "XX" + base.GetClrName(contentTypeModel) + "YY";
            }

            protected override string GetClrName(PropertyTypeModel propertyModel)
            {
                return "PP" + base.GetClrName(propertyModel) + "QQ";
            }
        }

        [Test]
        public void CustomBaseClass()
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

            var type2 = new ContentTypeModel
            {
                Id = 2,
                Alias = "type2",
                ParentId = 0,
                BaseContentType = null,
                Kind = ContentTypeKind.Content,
            };
            modelSource.ContentTypes.Add(type2);

            var options = new ModelsBuilderOptions();
            var codeOptionsBuilder = new CodeOptionsBuilder();
            codeOptionsBuilder.ContentTypes.IgnorePropertyType(ContentTypeIdentity.Alias("type1"), "pprop*");
            var modelBuilder = new CustomCodeModelBuilder(options, codeOptionsBuilder.CodeOptions, new CustomBaseClassBuilder(options, codeOptionsBuilder.CodeOptions));
            var model = modelBuilder.Build(modelSource);

            model.ContentTypes.PropertyStyle = PropertyStyle.Property;
            model.ContentTypes.FallbackStyle = FallbackStyle.Nothing;

            AssertCode.Compiles(model, out var compilation);
            var semanticModel = compilation.GetSemanticModel("type1.generated");

            // have proper base classes
            AssertCode.HasType(semanticModel, "Umbraco.Web.PublishedModels.Type1", out var type1Symbol);
            AssertCode.HasType(semanticModel, "Umbraco.Web.PublishedModels.Type2", out var type2Symbol);

            Assert.AreEqual("PublishedContentModel", type1Symbol.BaseType.Name);
            Assert.AreEqual("Type1", type2Symbol.BaseType.Name);
        }

        private class CustomBaseClassBuilder : ContentTypesCodeModelBuilder
        {
            public CustomBaseClassBuilder(ModelsBuilderOptions options, CodeOptions codeOptions) 
                : base(options, codeOptions)
            { }

            protected override string GetContentTypeBaseClassClrFullName(ContentTypeModel contentTypeModel, string modelsNamespace)
            {
                return contentTypeModel.Alias == "type2"
                    ? "Umbraco.Web.PublishedModels.Type1"
                    : base.GetContentTypeBaseClassClrFullName(contentTypeModel, modelsNamespace);
            }
        }

        [Test]
        public void AddUsing()
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

            var codeOptionsBuilder = new CodeOptionsBuilder();
            var modelBuilder = new CodeModelBuilder(new ModelsBuilderOptions(), codeOptionsBuilder.CodeOptions);
            var model = modelBuilder.Build(modelSource);

            model.ContentTypes.PropertyStyle = PropertyStyle.Property;
            model.ContentTypes.FallbackStyle = FallbackStyle.Nothing;

            model.Using.Add("Some.Namespace");

            var sources = new Dictionary<string, string>()
            {
                ["some.code"] = @"
namespace Some.Namespace
{
    public class SomeClass { }
}"
            };

            AssertCode.Compiles(model, sources);

            Assert.IsTrue(sources.ContainsKey("type1.generated"));
            Assert.IsTrue(sources["type1.generated"].Contains("using Some.Namespace;"));
        }
    }
}
