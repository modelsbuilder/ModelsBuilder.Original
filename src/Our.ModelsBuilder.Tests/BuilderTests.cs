using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using NUnit.Framework;
using Our.ModelsBuilder.Building;
using Our.ModelsBuilder.Options;
using Our.ModelsBuilder.Tests.Testing;
using Umbraco.Core.Models;

namespace Our.ModelsBuilder.Tests
{
    [TestFixture]
    [Explicit("These tests need to be overhauled.")]
    public class BuilderTests : TestsBase
    {
        [Test]
        public void RenameContentTypeWithAttribute()
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

            var type2 = new ContentTypeModel
            {
                Id = 2,
                Alias = "type2",
                ParentId = 0,
                BaseContentType = null,
                Kind = ContentTypeKind.Content,
            };
            modelSource.ContentTypes.Add(type2);

            var code = new Dictionary<string, string>
            {
                {"assembly", @"
using Our.ModelsBuilder;
namespace Models
{
    [ImplementContentType(""type1"")]
    public partial class Renamed1
    {}
}
"}
            };

            var refs = new[]
            {
                MetadataReference.CreateFromFile(typeof (string).Assembly.Location),
                MetadataReference.CreateFromFile(typeof (ReferencedAssemblies).Assembly.Location)
            };

            var optionsBuilder = new CodeOptionsBuilder();
            new CodeParser().Parse(code, optionsBuilder, refs);
            var codeOptions = optionsBuilder.CodeOptions;
            var modelBuilder = new CodeModelBuilder(new ModelsBuilderOptions(), codeOptions);
            var model = modelBuilder.Build(modelSource);

            Assert.IsFalse(codeOptions.ContentTypes.IsContentTypeIgnored("type1"));
            Assert.IsFalse(codeOptions.ContentTypes.IsContentTypeIgnored("type2"));
            Assert.IsNotNull(codeOptions.ContentTypes.GetContentTypeClrName("type1"));
            Assert.IsNull(codeOptions.ContentTypes.GetContentTypeClrName("type2"));
            Assert.AreEqual("Renamed1", codeOptions.ContentTypes.GetContentTypeClrName("type1"));
            Assert.IsNull(codeOptions.ContentTypes.GetContentTypeClrName("type2"));

            Assert.AreEqual(2, model.ContentTypes.ContentTypes.Count);
            Assert.AreEqual("Renamed1", model.ContentTypes.ContentTypes[0].ClrName);
            Assert.AreEqual("Type2", model.ContentTypes.ContentTypes[1].ClrName);
        }

        [Test]
        public void ContentTypeCustomBaseClass()
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
            var type3 = new ContentTypeModel
            {
                Id = 3,
                Alias = "type3",
                ParentId = 1,
                BaseContentType = type1,
                Kind = ContentTypeKind.Content,
            };
            modelSource.ContentTypes.Add(type3);

            var code = new Dictionary<string, string>
            {
                {"assembly", @"
using Our.ModelsBuilder;
using Dang;
namespace Dang
{
    public abstract class MyModelBase : PublishedContentModel
    {
        public MyModelBase(IPublishedContent content)
            : base(content)
        { }
    }

    public abstract class MyType1 : Type1
    {
        public MyType1(IPublishedContent content)
            : base(content)
        { }
    }

    public partial class Type1
    {}

    public partial class Type2 : MyModelBase
    {}

    public partial class Type3 : MyType1
    { }
}
"}
            };

            var optionsBuilder = new CodeOptionsBuilder();
            new CodeParser().Parse(code, optionsBuilder);
            var codeOptions = optionsBuilder.CodeOptions;
            var modelBuilder = new CodeModelBuilder(new ModelsBuilderOptions(), codeOptions);
            var model = modelBuilder.Build(modelSource);

            var writer = new CodeWriter(model).ContentTypesCodeWriter;

            Assert.AreEqual(3, model.ContentTypes.ContentTypes.Count);
            var btype1 = model.ContentTypes.ContentTypes[0];
            Assert.AreEqual("Type1", btype1.ClrName);
            var btype2 = model.ContentTypes.ContentTypes[1];
            Assert.AreEqual("Type2", btype2.ClrName);
            var btype3 = model.ContentTypes.ContentTypes[2];
            Assert.AreEqual("Type3", btype3.ClrName);

            Assert.IsFalse(btype1.OmitBaseClass);
            Assert.IsTrue(btype2.OmitBaseClass);
            Assert.IsTrue(btype3.OmitBaseClass);

            writer.WriteModel(btype3);
            var gen = writer.Code;
            Console.WriteLine(gen);

            Assert.Greater(gen.IndexOf("public partial class Type3\n", StringComparison.InvariantCulture), 0);
            Assert.Greater(0, gen.IndexOf("public partial class Type3 : ", StringComparison.InvariantCulture));
        }

        [Test]
        public void PropertyTypeRenameOnClass()
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
            });

            var code = new Dictionary<string, string>
            {
                {"assembly", @"
using Our.ModelsBuilder;
namespace Models
{
    //[RenamePropertyType(""prop1"", ""Renamed1"")]
    //[RenamePropertyType(""prop2"", ""Renamed2"")]
    public partial class Type1
    {
    }
}
"}
            };

            var refs = new[]
            {
                MetadataReference.CreateFromFile(typeof (string).Assembly.Location),
                MetadataReference.CreateFromFile(typeof (ReferencedAssemblies).Assembly.Location)
            };

            var optionsBuilder = new CodeOptionsBuilder();
            new CodeParser().Parse(code, optionsBuilder, refs);
            var codeOptions = optionsBuilder.CodeOptions;
            var modelBuilder = new CodeModelBuilder(new ModelsBuilderOptions(), codeOptions);
            var model = modelBuilder.Build(modelSource);

            Assert.AreEqual("Renamed1", codeOptions.ContentTypes.GetPropertyTypeClrName("Type1", "prop1"));
            Assert.AreEqual("Renamed2", codeOptions.ContentTypes.GetPropertyTypeClrName("Type1", "prop2"));

            Assert.AreEqual(1, model.ContentTypes.ContentTypes.Count);
            Assert.IsTrue(model.ContentTypes.ContentTypes[0].Properties[0].ClrName == "Renamed1");
        }

        [Test]
        public void PropertyTypeRenameOnClassInherit()
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
            });

            var code = new Dictionary<string, string>
            {
                {"assembly", @"
using Our.ModelsBuilder;
namespace Models
{
    //[RenamePropertyType(""prop1"", ""Renamed1"")]
    //[RenamePropertyType(""prop2"", ""Renamed2"")]
    public class Type2
    {
    }

    public partial class Type1 : Type2
    {
    }
}
"}
            };

            var refs = new[]
            {
                MetadataReference.CreateFromFile(typeof (string).Assembly.Location),
                MetadataReference.CreateFromFile(typeof (ReferencedAssemblies).Assembly.Location)
            };

            var optionsBuilder = new CodeOptionsBuilder();
            new CodeParser().Parse(code, optionsBuilder, refs);
            var codeOptions = optionsBuilder.CodeOptions;
            var modelBuilder = new CodeModelBuilder(new ModelsBuilderOptions(), codeOptions);
            var model = modelBuilder.Build(modelSource);

            Assert.AreEqual("Renamed1", codeOptions.ContentTypes.GetPropertyTypeClrName("Type1", "prop1"));
            Assert.AreEqual("Renamed2", codeOptions.ContentTypes.GetPropertyTypeClrName("Type1", "prop2"));

            Assert.AreEqual(1, model.ContentTypes.ContentTypes.Count);
            Assert.IsTrue(model.ContentTypes.ContentTypes[0].Properties[0].ClrName == "Renamed1");
        }

        [Test]
        public void PropertyTypeRenameOnProperty()
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
            });

            var code = new Dictionary<string, string>
            {
                {"assembly", @"
using Our.ModelsBuilder;
namespace Models
{
    public partial class Type1
    {
        [ImplementPropertyType(""prop1"")]
        public string Renamed1 { get { return """"; } }
    }
}
"}
            };

            var refs = new[]
            {
                MetadataReference.CreateFromFile(typeof (string).Assembly.Location),
                MetadataReference.CreateFromFile(typeof (ReferencedAssemblies).Assembly.Location)
            };

            var optionsBuilder = new CodeOptionsBuilder();
            new CodeParser().Parse(code, optionsBuilder, refs);
            var codeOptions = optionsBuilder.CodeOptions;
            var modelBuilder = new CodeModelBuilder(new ModelsBuilderOptions(), codeOptions);
            var model = modelBuilder.Build(modelSource);

            Assert.IsTrue(codeOptions.ContentTypes.IsPropertyIgnored("Type1", "prop1"));

            Assert.AreEqual(1, model.ContentTypes.ContentTypes.Count);
            Assert.AreEqual(0, model.ContentTypes.ContentTypes[0].Properties.Count);
        }

        [Test]
        public void PropertyTypeRenameOnPropertyInherit()
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
            });

            var code = new Dictionary<string, string>
            {
                {"assembly", @"
using Our.ModelsBuilder;
namespace Models
{
    public class Type2
    {
        [ImplementPropertyType(""prop1"")]
        public string Renamed1 { get { return """"; } }
    }

    public partial class Type1 : Type2
    {
    }
}
"}
            };

            var refs = new[]
            {
                MetadataReference.CreateFromFile(typeof (string).Assembly.Location),
                MetadataReference.CreateFromFile(typeof (ReferencedAssemblies).Assembly.Location)
            };

            var optionsBuilder = new CodeOptionsBuilder();
            new CodeParser().Parse(code, optionsBuilder, refs);
            var codeOptions = optionsBuilder.CodeOptions;
            var modelBuilder = new CodeModelBuilder(new ModelsBuilderOptions(), codeOptions);
            var model = modelBuilder.Build(modelSource);

            Assert.IsTrue(codeOptions.ContentTypes.IsPropertyIgnored("Type1", "prop1"));

            Assert.AreEqual(1, model.ContentTypes.ContentTypes.Count);
            Assert.AreEqual(0, model.ContentTypes.ContentTypes[0].Properties.Count);
        }

        [Test]
        public void PropertyTypeImplementOnClass()
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

            var code = new Dictionary<string, string>
            {
                {"assembly", @"
using Our.ModelsBuilder;
using Dang;
namespace Dang
{
    public partial class Type1
    {
        [ImplementPropertyType(""prop1"")]
        public string Foo { get { return string.Empty; } }
    }
}
"}
            };

            var refs = new[]
            {
                MetadataReference.CreateFromFile(typeof (string).Assembly.Location),
                MetadataReference.CreateFromFile(typeof (ReferencedAssemblies).Assembly.Location)
            };

            var optionsBuilder = new CodeOptionsBuilder();
            new CodeParser().Parse(code, optionsBuilder, refs);
            var codeOptions = optionsBuilder.CodeOptions;
            var modelBuilder = new CodeModelBuilder(new ModelsBuilderOptions(), codeOptions);
            var model = modelBuilder.Build(modelSource);
            var writer = new CodeWriter(model).ContentTypesCodeWriter;

            Assert.AreEqual(1, model.ContentTypes.ContentTypes.Count);
            var btype1 = model.ContentTypes.ContentTypes[0];
            Assert.AreEqual("Type1", btype1.ClrName);

            writer.WriteModel(btype1);
            var gen = writer.Code;
            Console.WriteLine(gen);

            Assert.Greater(0, gen.IndexOf("string Prop1", StringComparison.InvariantCulture));
        }

        [Test]
        public void PropertyTypeImplementOnInterface()
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
                Alias = "prop1a",
                ContentType = type1,
                ValueType = typeof(string),
            });
            type1.Properties.Add(new PropertyTypeModel
            {
                Alias = "prop1b",
                ContentType = type1,
                ValueType = typeof(string),
            });
            type1.Properties.Add(new PropertyTypeModel
            {
                Alias = "prop1c",
                ContentType = type1,
                ValueType = typeof(string),
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
            type2.Properties.Add(new PropertyTypeModel
            {
                Alias = "prop2",
                ContentType = type2,
                ValueType = typeof(string),
            });

            type2.MixinContentTypes.Add(type1);
            type1.IsMixin = true;

            var code = new Dictionary<string, string>
            {
                {"assembly", @"
using Our.ModelsBuilder;
using Dang;
namespace Dang
{
    // both attributes are ignored on the interface - do it on the class
    //[RenamePropertyType(""prop1b"", ""Prop1x"")]
    //[IgnorePropertyType(""prop1c"")]
    public partial interface IType1
    {
        // attribute is not supported (ie ignored) here
        //[ImplementPropertyType(""prop1a"")]
        // have to do this (see notes in Type1)
        public string Foo { get; }
    }

    // both attributes on the class will be mirrored on the interface
    //[RenamePropertyType(""prop1b"", ""Prop1x"")]
    [IgnorePropertyType(""prop1c"")]
    public partial class Type1
    {
        // and then,
        // - property will NOT be implemented in Type2, MUST be done manually
        // - property will NOT be mirrored on the interface, MUST be done manually
        [ImplementPropertyType(""prop1a"")]
        public string Foo { get { return string.Empty; } }
    }

    public partial class Type2
    {
        // have to do this (see notes in Type1)
        [ImplementPropertyType(""prop1a"")]
        public string Foo { get { return string.Empty; } }
    }
}
"}
            };

            var refs = new[]
            {
                MetadataReference.CreateFromFile(typeof (string).Assembly.Location),
                MetadataReference.CreateFromFile(typeof (ReferencedAssemblies).Assembly.Location)
            };

            var optionsBuilder = new CodeOptionsBuilder();
            new CodeParser().Parse(code, optionsBuilder, refs);
            var codeOptions = optionsBuilder.CodeOptions;
            var modelBuilder = new CodeModelBuilder(new ModelsBuilderOptions(), codeOptions);
            var model = modelBuilder.Build(modelSource);
            var writer = new CodeWriter(model).ContentTypesCodeWriter;

            Assert.AreEqual(2, model.ContentTypes.ContentTypes.Count);
            var btype1 = model.ContentTypes.ContentTypes[0];
            Assert.AreEqual("Type1", btype1.ClrName);
            var btype2 = model.ContentTypes.ContentTypes[1];
            Assert.AreEqual("Type2", btype2.ClrName);

            writer.WriteModel(btype1);
            var gen = writer.Code;
            Console.WriteLine(gen);

            // contains
            Assert.Greater(gen.IndexOf("string Prop1x", StringComparison.InvariantCulture), 0);
            // does not contain
            Assert.Greater(0, gen.IndexOf("string Prop1a", StringComparison.InvariantCulture));
            Assert.Greater(0, gen.IndexOf("string Prop1b", StringComparison.InvariantCulture));
            Assert.Greater(0, gen.IndexOf("string Prop1c", StringComparison.InvariantCulture));

            writer.Reset();
            writer.WriteModel(btype2);
            gen = writer.Code;
            Console.WriteLine(gen);

            // contains
            Assert.Greater(gen.IndexOf("string Prop2", StringComparison.InvariantCulture), 0);
            Assert.Greater(gen.IndexOf("string Prop1x", StringComparison.InvariantCulture), 0);
            // does not contain
            Assert.Greater(0, gen.IndexOf("string Prop1a", StringComparison.InvariantCulture));
            Assert.Greater(0, gen.IndexOf("string Prop1b", StringComparison.InvariantCulture));
            Assert.Greater(0, gen.IndexOf("string Prop1c", StringComparison.InvariantCulture));
        }


        [Test]
        public void VaryingProperties()
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
                Variations = ContentVariation.Culture
            };
            modelSource.ContentTypes.Add(type1);
            type1.Properties.Add(new PropertyTypeModel
            {
                Alias = "prop1",
                ContentType = type1,
                ValueType = typeof(string),
                Variations = ContentVariation.Culture
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
            type1.Properties.Add(new PropertyTypeModel
            {
                Alias = "prop5",
                ContentType = type1,
                ValueType = typeof(string)
            });
            type1.Properties.Add(new PropertyTypeModel
            {
                Alias = "prop6",
                ContentType = type1,
                ValueType = typeof(string),
                Variations = ContentVariation.Culture
            });

            var sources = new Dictionary<string, string>
            {
                {"ourFile1", @"
using System;
using Our.ModelsBuilder;
namespace Umbraco.Web.PublishedModels
{
    public static partial class Type1Extensions
    {
        [ImplementPropertyType(""type1"", ""prop1"")]
        public static string Prop1(this Type1 that, string culture = null, string segment = null) => """";
    }

    [IgnorePropertyType(""prop6"")]
    public partial class Type1
    { }
}
"}
            };

            // expected:
            // - the prop1 extension method is detected and not generated
            // - the prop1 property not is generated, in fact prop1 is entirely ignored
            // - the prop6 property is entirely ignored
            // - all other properties are generated

            var expected = TestUtilities.ExpectedHeader + @"

namespace Umbraco.Web.PublishedModels
{
    /// <summary>Provides extensions for the Type1 class.</summary>
    public static partial class Type1Extensions
    {
        /// <summary>Gets the value of the ""prop2"" property.</summary>
        [GeneratedCodeAttribute(ModelInfos.Name, ModelInfos.VersionString)]
        public static string Prop2(this Type1 that, string culture = null, Fallback fallback = default, string defaultValue = default)
            => that.Value<string>(ModelInfos.ContentTypes.Type1.Properties.Prop2.Alias, culture: culture, fallback: fallback, defaultValue: defaultValue);

        /// <summary>Gets the value of the ""prop3"" property.</summary>
        [GeneratedCodeAttribute(ModelInfos.Name, ModelInfos.VersionString)]
        public static string Prop3(this Type1 that, string segment = null, Fallback fallback = default, string defaultValue = default)
            => that.Value<string>(ModelInfos.ContentTypes.Type1.Properties.Prop3.Alias, segment: segment, fallback: fallback, defaultValue: defaultValue);

        /// <summary>Gets the value of the ""prop4"" property.</summary>
        [GeneratedCodeAttribute(ModelInfos.Name, ModelInfos.VersionString)]
        public static string Prop4(this Type1 that, string culture = null, string segment = null, Fallback fallback = default, string defaultValue = default)
            => that.Value<string>(ModelInfos.ContentTypes.Type1.Properties.Prop4.Alias, culture: culture, segment: segment, fallback: fallback, defaultValue: defaultValue);

        /// <summary>Gets the value of the ""prop5"" property.</summary>
        [GeneratedCodeAttribute(ModelInfos.Name, ModelInfos.VersionString)]
        public static string Prop5(this Type1 that, Fallback fallback = default, string defaultValue = default)
            => that.Value<string>(ModelInfos.ContentTypes.Type1.Properties.Prop5.Alias, fallback: fallback, defaultValue: defaultValue);
    }

    /// <summary>Represents a ""type1"" content item.</summary>
    [PublishedModel(ModelInfos.ContentTypes.Type1.Alias)]
    public partial class Type1 : PublishedContentModel
    {
        public Type1(IPublishedContent content)
            : base(content)
        { }

        /// <summary>Gets the value of the ""prop2"" property.</summary>
        [GeneratedCodeAttribute(ModelInfos.Name, ModelInfos.VersionString)]
        [ImplementPropertyType(ModelInfos.ContentTypes.Type1.Properties.Prop2.Alias)]
        public string Prop2 => this.Prop2();

        /// <summary>Gets the value of the ""prop3"" property.</summary>
        [GeneratedCodeAttribute(ModelInfos.Name, ModelInfos.VersionString)]
        [ImplementPropertyType(ModelInfos.ContentTypes.Type1.Properties.Prop3.Alias)]
        public string Prop3 => this.Prop3();

        /// <summary>Gets the value of the ""prop4"" property.</summary>
        [GeneratedCodeAttribute(ModelInfos.Name, ModelInfos.VersionString)]
        [ImplementPropertyType(ModelInfos.ContentTypes.Type1.Properties.Prop4.Alias)]
        public string Prop4 => this.Prop4();

        /// <summary>Gets the value of the ""prop5"" property.</summary>
        [GeneratedCodeAttribute(ModelInfos.Name, ModelInfos.VersionString)]
        [ImplementPropertyType(ModelInfos.ContentTypes.Type1.Properties.Prop5.Alias)]
        public string Prop5 => this.Prop5();
    }
}
";

            var refs = TestUtilities.CreateDefaultReferences();

            // get the writer
            var optionsBuilder = new CodeOptionsBuilder();
            new CodeParser().Parse(sources, optionsBuilder, refs);
            var codeOptions = optionsBuilder.CodeOptions;
            var modelBuilder = new CodeModelBuilder(new ModelsBuilderOptions(), codeOptions);
            var model = modelBuilder.Build(modelSource);
            var writer = new CodeWriter(model);

            // write
            writer.WriteModelFile(model.ContentTypes.ContentTypes.First());

            // assert generated code
            var generated = sources["generated"] = writer.Code;
            Console.WriteLine(generated);
            Assert.AreEqual(expected.ClearLf(), generated);

            // add model infos for compilation
            writer.Reset();
            writer.WriteModelInfosFile();
            sources["modelInfos"] = writer.Code;
            Console.WriteLine(sources["modelInfos"]);

            // assert generated code can compile
            AssertCode.Compiles(sources, refs);
        }
    }

    // make it public to be ambiguous (see above)
    // ReSharper disable once InconsistentNaming, reason: framework naming
    public class ASCIIEncoding
    {
        // can we handle nested types?
        public class Nested { }
    }

    class BuilderTestsClass1 {}

    public class System { }
}
