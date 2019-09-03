using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using NUnit.Framework;
using Umbraco.Core.Composing;
using Umbraco.Core.Models;
using Umbraco.Core.Models.PublishedContent;
using ZpqrtBnk.ModelsBuilder.Api;
using ZpqrtBnk.ModelsBuilder.Building;
using ZpqrtBnk.ModelsBuilder.Configuration;

namespace ZpqrtBnk.ModelsBuilder.Tests
{
    [TestFixture]
    public class BuilderTests
    {
        [SetUp]
        public void Setup()
        {
            Current.Reset();
            Current.UnlockConfigs();
            Current.Configs.Add(() => new Config());
        }

        [Test]
        public void ModelsBaseClassAttribute()
        {
            var code = new Dictionary<string, string>
            {
                {"assembly", @"
using ZpqrtBnk.ModelsBuilder;
using Dang;
[assembly:ContentModelsBaseClass(typeof(Whatever))]
namespace Dang
{
public class Whatever
{}
}
"}
            };

            var refs = new[]
            {
                MetadataReference.CreateFromFile(typeof (string).Assembly.Location),
                MetadataReference.CreateFromFile(typeof (ReferencedAssemblies).Assembly.Location)
            };

            var parseResult = new CodeParser().Parse(code, refs);

            Assert.AreEqual("Dang.Whatever", parseResult.GetModelBaseClassName(true, "bah"));
        }

        [Test]
        public void ModelsNamespaceAttribute()
        {
            var code = new Dictionary<string, string>
            {
                {"assembly", @"
using ZpqrtBnk.ModelsBuilder;
[assembly:ModelsNamespace(""Foo.Bar.Nil"")]
"}
            };

            var refs = new[]
            {
                MetadataReference.CreateFromFile(typeof (string).Assembly.Location),
                MetadataReference.CreateFromFile(typeof (ReferencedAssemblies).Assembly.Location)
            };

            var parseResult = new CodeParser().Parse(code, refs);

            Assert.IsTrue(parseResult.HasModelsNamespace);
            Assert.AreEqual("Foo.Bar.Nil", parseResult.ModelsNamespace);
        }

        [Test]
        public void ConfigureModelsNamespaceAttribute()
        {
            var code = new Dictionary<string, string>
            {
                {"assembly", @"
using ZpqrtBnk.ModelsBuilder;
[assembly:ModelsBuilderConfigure(Namespace=""Foo.Bar.Nil"")]
"}
            };

            var refs = new[]
            {
                MetadataReference.CreateFromFile(typeof (string).Assembly.Location),
                MetadataReference.CreateFromFile(typeof (ReferencedAssemblies).Assembly.Location)
            };

            var parseResult = new CodeParser().Parse(code, refs);

            Assert.IsTrue(parseResult.HasModelsNamespace);
            Assert.AreEqual("Foo.Bar.Nil", parseResult.ModelsNamespace);
        }

        [Test]
        public void ModelsUsingAttribute()
        {
            // Umbraco returns nice, pascal-cased names

            var type1 = new TypeModel
            {
                Id = 1,
                Alias = "type1",
                ClrName = "Type1",
                ParentId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
            };

            var types = new[] { type1 };

            var code1 = new Dictionary<string, string>
            {
                {"assembly", @"
using ZpqrtBnk.ModelsBuilder;
"}
            };

            var code2 = new Dictionary<string, string>
            {
                {"assembly", @"
using ZpqrtBnk.ModelsBuilder;
[assembly:ModelsUsing(""Foo.Bar.Nil"")]
"}
            };

            var refs = new[]
            {
                MetadataReference.CreateFromFile(typeof (string).Assembly.Location),
                MetadataReference.CreateFromFile(typeof (ReferencedAssemblies).Assembly.Location)
            };

            var parseResult = new CodeParser().Parse(code1, refs);
            var builder = new Builder(types, parseResult);
            var count = builder.Using.Count;

            parseResult = new CodeParser().Parse(code2, refs);
            builder = new Builder(types, parseResult);

            Assert.AreEqual(count + 1, builder.Using.Count);
            Assert.IsTrue(builder.Using.Contains("Foo.Bar.Nil"));
        }

        [Test]
        public void ContentTypeIgnore()
        {
            // Umbraco returns nice, pascal-cased names

            var type1 = new TypeModel
            {
                Id = 1,
                Alias = "type1",
                ClrName = "Type1",
                ParentId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
            };

            var types = new[] { type1 };

            var code = new Dictionary<string, string>
            {
                {"assembly", @"
using ZpqrtBnk.ModelsBuilder;
[assembly:IgnoreContentType(""type1"")]
"}
            };

            var refs = new[]
            {
                MetadataReference.CreateFromFile(typeof (string).Assembly.Location),
                MetadataReference.CreateFromFile(typeof (ReferencedAssemblies).Assembly.Location)
            };

            var parseResult = new CodeParser().Parse(code, refs);
            var builder = new Builder(types, parseResult);
            var btypes = builder.AllTypeModels;

            Assert.IsTrue(parseResult.IsIgnored("type1"));

            Assert.AreEqual(1, btypes.Count);
            Assert.IsTrue(btypes[0].IsContentIgnored);
        }

        [Test]
        public void ReferencedAssemblies()
        {
            var type1 = new TypeModel
            {
                Id = 1,
                Alias = "type1",
                ClrName = "Type1",
                ParentId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
            };

            var types = new[] { type1 };

            var code1 = new Dictionary<string, string>
            {
                {"assembly", @"
using ZpqrtBnk.ModelsBuilder;
public partial class Type1
{}
"}
            };

            Assert.IsFalse(new CodeParser().Parse(code1).HasContentBase("Type1"));

            var code2 = new Dictionary<string, string>
            {
                {"assembly", @"
using ZpqrtBnk.ModelsBuilder;
public partial class Type1 : IHasXmlNode
{}
"}
            };

            // assumes base is IHasXmlNode (cannot be verified...)
            Assert.IsTrue(new CodeParser().Parse(code2).HasContentBase("Type1"));

            var code3 = new Dictionary<string, string>
            {
                {"assembly", @"
using ZpqrtBnk.ModelsBuilder;
using System.Xml;
public partial class Type1 : IHasXmlNode
{}
"}
            };

            // figures out that IHasXmlNode is an interface, not base
            // because of using + reference
            var asms = new[] {typeof(global::System.Xml.IHasXmlNode).Assembly}.Select(x => MetadataReference.CreateFromFile(x.Location));
            Assert.IsFalse(new CodeParser().Parse(code3, asms).HasContentBase("Type1"));
        }

        [Test]
        public void ContentTypeIgnoreWildcard()
        {
            // Umbraco returns nice, pascal-cased names

            var type1 = new TypeModel
            {
                Id = 1,
                Alias = "type1",
                ClrName = "Type1",
                ParentId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
            };

            var type2 = new TypeModel
            {
                Id = 2,
                Alias = "type2",
                ClrName = "Type2",
                ParentId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
            };

            var type3 = new TypeModel
            {
                Id = 3,
                Alias = "ttype3",
                ClrName = "Ttype3",
                ParentId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
            };

            var types = new[] { type1, type2, type3 };

            var code = new Dictionary<string, string>
            {
                {"assembly", @"
using ZpqrtBnk.ModelsBuilder;
[assembly:IgnoreContentType(""type*"")]
"}
            };

            var refs = new[]
            {
                MetadataReference.CreateFromFile(typeof (string).Assembly.Location),
                MetadataReference.CreateFromFile(typeof (ReferencedAssemblies).Assembly.Location)
            };

            var parseResult = new CodeParser().Parse(code, refs);
            var builder = new Builder(types, parseResult);
            var btypes = builder.AllTypeModels;

            Assert.IsTrue(parseResult.IsIgnored("type1"));
            Assert.IsTrue(parseResult.IsIgnored("type2"));
            Assert.IsFalse(parseResult.IsIgnored("ttype3"));

            Assert.AreEqual(3, btypes.Count);
            Assert.IsTrue(btypes[0].IsContentIgnored);
            Assert.IsTrue(btypes[1].IsContentIgnored);
            Assert.IsFalse(btypes[2].IsContentIgnored);
        }

        [Test]
        public void ContentTypeIgnoreParent()
        {
            // Umbraco returns nice, pascal-cased names

            var type1 = new TypeModel
            {
                Id = 1,
                Alias = "type1",
                ClrName = "Type1",
                ParentId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
            };

            var type2 = new TypeModel
            {
                Id = 2,
                Alias = "type2",
                ClrName = "Type2",
                ParentId = 1,
                BaseType = type1,
                ItemType = TypeModel.ItemTypes.Content,
            };

            var types = new[] { type1, type2 };

            var code = new Dictionary<string, string>
            {
                {"assembly", @"
using ZpqrtBnk.ModelsBuilder;
[assembly:IgnoreContentType(""type1"")]
"}
            };

            var refs = new[]
            {
                MetadataReference.CreateFromFile(typeof (string).Assembly.Location),
                MetadataReference.CreateFromFile(typeof (ReferencedAssemblies).Assembly.Location)
            };

            var parseResult = new CodeParser().Parse(code, refs);
            var builder = new Builder(types, parseResult);
            var btypes = builder.AllTypeModels;

            Assert.IsTrue(parseResult.IsIgnored("type1"));
            Assert.IsFalse(parseResult.IsIgnored("type2"));

            Assert.AreEqual(2, btypes.Count);
            Assert.AreEqual("type1", btypes[0].Alias);
            Assert.AreEqual("type2", btypes[1].Alias);
            Assert.IsTrue(btypes[0].IsContentIgnored);
            Assert.IsTrue(btypes[1].IsContentIgnored);
        }

        [Test]
        public void ContentTypeIgnoreMixin()
        {
            // Umbraco returns nice, pascal-cased names

            var type1 = new TypeModel
            {
                Id = 1,
                Alias = "type1",
                ClrName = "Type1",
                ParentId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
            };

            var type2 = new TypeModel
            {
                Id = 2,
                Alias = "type2",
                ClrName = "Type2",
                ParentId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
            };
            type2.MixinTypes.Add(type1);

            var types = new[] { type1, type2 };

            var code = new Dictionary<string, string>
            {
                {"assembly", @"
using ZpqrtBnk.ModelsBuilder;
[assembly:IgnoreContentType(""type1"")]
"}
            };

            var refs = new[]
            {
                MetadataReference.CreateFromFile(typeof (string).Assembly.Location),
                MetadataReference.CreateFromFile(typeof (ReferencedAssemblies).Assembly.Location)
            };

            var parseResult = new CodeParser().Parse(code, refs);
            var builder = new Builder(types, parseResult);
            var btypes = builder.AllTypeModels;

            Assert.IsTrue(parseResult.IsIgnored("type1"));
            Assert.IsFalse(parseResult.IsIgnored("type2"));

            Assert.AreEqual(2, btypes.Count);
            Assert.AreEqual("type1", btypes[0].Alias);
            Assert.AreEqual("type2", btypes[1].Alias);
            Assert.IsTrue(btypes[0].IsContentIgnored);
            Assert.IsFalse(btypes[1].IsContentIgnored);

            Assert.AreEqual(0, btypes[1].DeclaringInterfaces.Count);
        }

        [Test]
        public void ContentTypeRenameOnAssembly()
        {
            // Umbraco returns nice, pascal-cased names

            var type1 = new TypeModel
            {
                Id = 1,
                Alias = "type1",
                ClrName = "Type1",
                ParentId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
            };

            var type2 = new TypeModel
            {
                Id = 2,
                Alias = "type2",
                ClrName = "Type2",
                ParentId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
            };

            var types = new[] { type1, type2 };

            var code = new Dictionary<string, string>
            {
                {"assembly", @"
using ZpqrtBnk.ModelsBuilder;
[assembly:RenameContentType(""type1"", ""Renamed1"")]
"}
            };

            var refs = new[]
            {
                MetadataReference.CreateFromFile(typeof (string).Assembly.Location),
                MetadataReference.CreateFromFile(typeof (ReferencedAssemblies).Assembly.Location)
            };

            var parseResult = new CodeParser().Parse(code, refs);
            var builder = new Builder(types, parseResult);
            var btypes = builder.AllTypeModels;

            Assert.IsFalse(parseResult.IsIgnored("type1"));
            Assert.IsFalse(parseResult.IsIgnored("type2"));
            Assert.IsTrue(parseResult.IsContentRenamed("type1"));
            Assert.IsFalse(parseResult.IsContentRenamed("type2"));
            Assert.AreEqual("Renamed1", parseResult.ContentClrName("type1"));
            Assert.IsNull(parseResult.ContentClrName("type2"));

            Assert.AreEqual(2, btypes.Count);
            Assert.IsFalse(btypes[0].IsContentIgnored);
            Assert.IsFalse(btypes[1].IsContentIgnored);
            Assert.AreEqual("Renamed1", btypes[0].ClrName);
            Assert.AreEqual("Type2", btypes[1].ClrName);
        }

        [Test]
        public void ContentTypeRenameOnClass()
        {
            // Umbraco returns nice, pascal-cased names

            var type1 = new TypeModel
            {
                Id = 1,
                Alias = "type1",
                ClrName = "Type1",
                ParentId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
            };

            var type2 = new TypeModel
            {
                Id = 2,
                Alias = "type2",
                ClrName = "Type2",
                ParentId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
            };

            var types = new[] { type1, type2 };

            var code = new Dictionary<string, string>
            {
                {"assembly", @"
using ZpqrtBnk.ModelsBuilder;
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

            var parseResult = new CodeParser().Parse(code, refs);
            var builder = new Builder(types, parseResult);
            var btypes = builder.AllTypeModels;

            Assert.IsFalse(parseResult.IsIgnored("type1"));
            Assert.IsFalse(parseResult.IsIgnored("type2"));
            Assert.IsTrue(parseResult.IsContentRenamed("type1"));
            Assert.IsFalse(parseResult.IsContentRenamed("type2"));
            Assert.AreEqual("Renamed1", parseResult.ContentClrName("type1"));
            Assert.IsNull(parseResult.ContentClrName("type2"));

            Assert.AreEqual(2, btypes.Count);
            Assert.IsFalse(btypes[0].IsContentIgnored);
            Assert.IsFalse(btypes[1].IsContentIgnored);
            Assert.AreEqual("Renamed1", btypes[0].ClrName);
            Assert.AreEqual("Type2", btypes[1].ClrName);
        }

        [Test]
        public void ContentTypeCustomBaseClass()
        {
            var type1 = new TypeModel
            {
                Id = 1,
                Alias = "type1",
                ClrName = "Type1",
                ParentId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
            };
            var type2 = new TypeModel
            {
                Id = 2,
                Alias = "type2",
                ClrName = "Type2",
                ParentId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
            };
            var type3 = new TypeModel
            {
                Id = 3,
                Alias = "type3",
                ClrName = "Type3",
                ParentId = 1,
                BaseType = type1,
                ItemType = TypeModel.ItemTypes.Content,
            };
            var types = new[] { type1, type2, type3 };

            var code = new Dictionary<string, string>
            {
                {"assembly", @"
using ZpqrtBnk.ModelsBuilder;
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

            var parseResult = new CodeParser().Parse(code);
            var builder = new Builder(types, parseResult);
            var btypes = builder.AllTypeModels;

            Assert.AreEqual(3, btypes.Count);
            var btype1 = btypes[0];
            Assert.AreEqual("Type1", btype1.ClrName);
            var btype2 = btypes[1];
            Assert.AreEqual("Type2", btype2.ClrName);
            var btype3 = btypes[2];
            Assert.AreEqual("Type3", btype3.ClrName);

            Assert.IsFalse(btype1.HasBase);
            Assert.IsTrue(btype2.HasBase);
            Assert.IsTrue(btype3.HasBase);

            var sb = new StringBuilder();
            builder.WriteContentTypeModel(sb, btype3);
            var gen = sb.ToString();
            Console.WriteLine(gen);

            Assert.Greater(gen.IndexOf("public partial class Type3\n"), 0);
            Assert.Greater(0, gen.IndexOf("public partial class Type3 : "));
        }

        [Test]
        public void PropertyTypeIgnore()
        {
            // Umbraco returns nice, pascal-cased names

            var type1 = new TypeModel
            {
                Id = 1,
                Alias = "type1",
                ClrName = "Type1",
                ParentId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
            };
            type1.Properties.Add(new PropertyModel
            {
                Alias = "prop1",
                ClrName = "Prop1",
                ModelClrType = typeof(string),
            });

            var types = new[] { type1 };

            var code = new Dictionary<string, string>
            {
                {"assembly", @"
using ZpqrtBnk.ModelsBuilder;
namespace Models
{
    [IgnorePropertyType(""prop1"")]
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

            var parseResult = new CodeParser().Parse(code, refs);
            var builder = new Builder(types, parseResult);
            var btypes = builder.AllTypeModels;

            Assert.IsTrue(parseResult.IsPropertyIgnored("Type1", "prop1"));

            Assert.AreEqual(1, btypes.Count);
            Assert.IsTrue(btypes[0].Properties[0].IsIgnored);
        }

        [Test]
        public void PropertyTypeIgnoreWildcard()
        {
            // Umbraco returns nice, pascal-cased names

            var type1 = new TypeModel
            {
                Id = 1,
                Alias = "type1",
                ClrName = "Type1",
                ParentId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
            };
            type1.Properties.Add(new PropertyModel
            {
                Alias = "prop1",
                ClrName = "Prop1",
                ModelClrType = typeof(string),
            });
            type1.Properties.Add(new PropertyModel
            {
                Alias = "prop2",
                ClrName = "Prop2",
                ModelClrType = typeof(string),
            });
            type1.Properties.Add(new PropertyModel
            {
                Alias = "pprop3",
                ClrName = "Pprop3",
                ModelClrType = typeof(string),
            });

            var types = new[] { type1 };

            var code = new Dictionary<string, string>
            {
                {"assembly", @"
using ZpqrtBnk.ModelsBuilder;
namespace Models
{
    [IgnorePropertyType(""prop*"")]
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

            var parseResult = new CodeParser().Parse(code, refs);
            var builder = new Builder(types, parseResult);
            var btypes = builder.AllTypeModels;

            Assert.IsTrue(parseResult.IsPropertyIgnored("Type1", "prop1"));
            Assert.IsTrue(parseResult.IsPropertyIgnored("Type1", "prop2"));
            Assert.IsFalse(parseResult.IsPropertyIgnored("Type1", "pprop3"));

            Assert.AreEqual(1, btypes.Count);
            Assert.IsTrue(btypes[0].Properties[0].IsIgnored);
            Assert.IsTrue(btypes[0].Properties[1].IsIgnored);
            Assert.IsFalse(btypes[0].Properties[2].IsIgnored);
        }

        [Test]
        public void PropertyTypeIgnoreInherit()
        {
            // Umbraco returns nice, pascal-cased names

            var type1 = new TypeModel
            {
                Id = 1,
                Alias = "type1",
                ClrName = "Type1",
                ParentId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
            };
            type1.Properties.Add(new PropertyModel
            {
                Alias = "prop1",
                ClrName = "Prop1",
                ModelClrType = typeof(string),
            });

            var types = new[] { type1 };

            var code = new Dictionary<string, string>
            {
                {"assembly", @"
using ZpqrtBnk.ModelsBuilder;
namespace Models
{
    [IgnorePropertyType(""prop1"")]
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

            var parseResult = new CodeParser().Parse(code, refs);
            var builder = new Builder(types, parseResult);
            var btypes = builder.AllTypeModels;

            Assert.IsTrue(parseResult.IsPropertyIgnored("Type1", "prop1"));

            Assert.AreEqual(1, btypes.Count);
            Assert.IsTrue(btypes[0].Properties[0].IsIgnored);
        }

        [Test]
        public void PropertyTypeRenameOnClass()
        {
            // Umbraco returns nice, pascal-cased names

            var type1 = new TypeModel
            {
                Id = 1,
                Alias = "type1",
                ClrName = "Type1",
                ParentId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
            };
            type1.Properties.Add(new PropertyModel
            {
                Alias = "prop1",
                ClrName = "Prop1",
                ModelClrType = typeof(string),
            });

            var types = new[] { type1 };

            var code = new Dictionary<string, string>
            {
                {"assembly", @"
using ZpqrtBnk.ModelsBuilder;
namespace Models
{
    [RenamePropertyType(""prop1"", ""Renamed1"")]
    [RenamePropertyType(""prop2"", ""Renamed2"")]
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

            var parseResult = new CodeParser().Parse(code, refs);
            var builder = new Builder(types, parseResult);
            var btypes = builder.AllTypeModels;

            Assert.AreEqual("Renamed1", parseResult.PropertyClrName("Type1", "prop1"));
            Assert.AreEqual("Renamed2", parseResult.PropertyClrName("Type1", "prop2"));

            Assert.AreEqual(1, btypes.Count);
            Assert.IsTrue(btypes[0].Properties[0].ClrName == "Renamed1");
        }

        [Test]
        public void PropertyTypeRenameOnClassInherit()
        {
            // Umbraco returns nice, pascal-cased names

            var type1 = new TypeModel
            {
                Id = 1,
                Alias = "type1",
                ClrName = "Type1",
                ParentId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
            };
            type1.Properties.Add(new PropertyModel
            {
                Alias = "prop1",
                ClrName = "Prop1",
                ModelClrType = typeof(string),
            });

            var types = new[] { type1 };

            var code = new Dictionary<string, string>
            {
                {"assembly", @"
using ZpqrtBnk.ModelsBuilder;
namespace Models
{
    [RenamePropertyType(""prop1"", ""Renamed1"")]
    [RenamePropertyType(""prop2"", ""Renamed2"")]
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

            var parseResult = new CodeParser().Parse(code, refs);
            var builder = new Builder(types, parseResult);
            var btypes = builder.AllTypeModels;

            Assert.AreEqual("Renamed1", parseResult.PropertyClrName("Type1", "prop1"));
            Assert.AreEqual("Renamed2", parseResult.PropertyClrName("Type1", "prop2"));

            Assert.AreEqual(1, btypes.Count);
            Assert.IsTrue(btypes[0].Properties[0].ClrName == "Renamed1");
        }

        [Test]
        public void PropertyTypeRenameOnProperty()
        {
            // Umbraco returns nice, pascal-cased names

            var type1 = new TypeModel
            {
                Id = 1,
                Alias = "type1",
                ClrName = "Type1",
                ParentId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
            };
            type1.Properties.Add(new PropertyModel
            {
                Alias = "prop1",
                ClrName = "Prop1",
                ModelClrType = typeof(string),
            });

            var types = new[] { type1 };

            var code = new Dictionary<string, string>
            {
                {"assembly", @"
using ZpqrtBnk.ModelsBuilder;
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

            var parseResult = new CodeParser().Parse(code, refs);
            var builder = new Builder(types, parseResult);
            var btypes = builder.AllTypeModels;

            Assert.IsTrue(parseResult.IsPropertyIgnored("Type1", "prop1"));

            Assert.AreEqual(1, btypes.Count);
            Assert.IsTrue(btypes[0].Properties[0].IsIgnored);
        }

        [Test]
        public void PropertyTypeRenameOnPropertyInherit()
        {
            // Umbraco returns nice, pascal-cased names

            var type1 = new TypeModel
            {
                Id = 1,
                Alias = "type1",
                ClrName = "Type1",
                ParentId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
            };
            type1.Properties.Add(new PropertyModel
            {
                Alias = "prop1",
                ClrName = "Prop1",
                ModelClrType = typeof(string),
            });

            var types = new[] { type1 };

            var code = new Dictionary<string, string>
            {
                {"assembly", @"
using ZpqrtBnk.ModelsBuilder;
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

            var parseResult = new CodeParser().Parse(code, refs);
            var builder = new Builder(types, parseResult);
            var btypes = builder.AllTypeModels;

            Assert.IsTrue(parseResult.IsPropertyIgnored("Type1", "prop1"));

            Assert.AreEqual(1, btypes.Count);
            Assert.IsTrue(btypes[0].Properties[0].IsIgnored);
        }

        [Test]
        public void PropertyTypeImplementOnClass()
        {
            var type1 = new TypeModel
            {
                Id = 1,
                Alias = "type1",
                ClrName = "Type1",
                ParentId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
            };
            type1.Properties.Add(new PropertyModel
            {
                Alias = "prop1",
                ClrName = "Prop1",
                ModelClrType = typeof(string),
            });
            var types = new[] { type1 };

            var code = new Dictionary<string, string>
            {
                {"assembly", @"
using ZpqrtBnk.ModelsBuilder;
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

            var parseResult = new CodeParser().Parse(code, refs);
            var builder = new Builder(types, parseResult);
            var btypes = builder.AllTypeModels;

            Assert.AreEqual(1, btypes.Count);
            var btype1 = btypes[0];
            Assert.AreEqual("Type1", btype1.ClrName);

            var sb = new StringBuilder();
            builder.WriteContentTypeModel(sb, btype1);
            var gen = sb.ToString();
            Console.WriteLine(gen);

            Assert.Greater(0, gen.IndexOf("string Prop1"));
        }

        [Test]
        public void PropertyTypeImplementOnInterface()
        {
            var type1 = new TypeModel
            {
                Id = 1,
                Alias = "type1",
                ClrName = "Type1",
                ParentId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
            };
            type1.Properties.Add(new PropertyModel
            {
                Alias = "prop1a",
                ClrName = "Prop1a",
                ModelClrType = typeof(string),
            });
            type1.Properties.Add(new PropertyModel
            {
                Alias = "prop1b",
                ClrName = "Prop1b",
                ModelClrType = typeof(string),
            });
            type1.Properties.Add(new PropertyModel
            {
                Alias = "prop1c",
                ClrName = "Prop1c",
                ModelClrType = typeof(string),
            });
            var type2 = new TypeModel
            {
                Id = 1,
                Alias = "type2",
                ClrName = "Type2",
                ParentId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
            };
            type2.Properties.Add(new PropertyModel
            {
                Alias = "prop2",
                ClrName = "Prop2",
                ModelClrType = typeof(string),
            });
            var types = new[] { type1, type2 };

            type2.MixinTypes.Add(type1);
            type1.IsMixin = true;

            var code = new Dictionary<string, string>
            {
                {"assembly", @"
using ZpqrtBnk.ModelsBuilder;
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
    [RenamePropertyType(""prop1b"", ""Prop1x"")]
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

            var parseResult = new CodeParser().Parse(code, refs);
            var builder = new Builder(types, parseResult);
            var btypes = builder.AllTypeModels;

            Assert.AreEqual(2, btypes.Count);
            var btype1 = btypes[0];
            Assert.AreEqual("Type1", btype1.ClrName);
            var btype2 = btypes[1];
            Assert.AreEqual("Type2", btype2.ClrName);

            var sb = new StringBuilder();
            builder.WriteContentTypeModel(sb, btype1);
            var gen = sb.ToString();
            Console.WriteLine(gen);

            // contains
            Assert.Greater(gen.IndexOf("string Prop1x"), 0);
            // does not contain
            Assert.Greater(0, gen.IndexOf("string Prop1a"));
            Assert.Greater(0, gen.IndexOf("string Prop1b"));
            Assert.Greater(0, gen.IndexOf("string Prop1c"));

            sb.Clear();
            builder.WriteContentTypeModel(sb, btype2);
            gen = sb.ToString();
            Console.WriteLine(gen);

            // contains
            Assert.Greater(gen.IndexOf("string Prop2"), 0);
            Assert.Greater(gen.IndexOf("string Prop1x"), 0);
            // does not contain
            Assert.Greater(0, gen.IndexOf("string Prop1a"));
            Assert.Greater(0, gen.IndexOf("string Prop1b"));
            Assert.Greater(0, gen.IndexOf("string Prop1c"));
        }

        [Test]
        public void MixinPropertyStatic()
        {
            Current.Configs.Add(() => new Config());

            var type1 = new TypeModel
            {
                Id = 1,
                Alias = "type1",
                ClrName = "Type1",
                ParentId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
                IsMixin = true,
            };
            type1.Properties.Add(new PropertyModel
            {
                Alias = "prop1a",
                ClrName = "Prop1a",
                ModelClrType = typeof(string),
            });
            type1.Properties.Add(new PropertyModel
            {
                Alias = "prop1b",
                ClrName = "Prop1b",
                ModelClrType = typeof(string),
            });

            var type2 = new TypeModel
            {
                Id = 2,
                Alias = "type2",
                ClrName = "Type2",
                ParentId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
            };
            type2.MixinTypes.Add(type1);
            type2.Properties.Add(new PropertyModel
            {
                Alias = "prop2",
                ClrName = "Prop2",
                ModelClrType = typeof(int),
            });

            var types = new[] { type1, type2 };

            var code = new Dictionary<string, string>
            {
                {"assembly", @"
using ZpqrtBnk.ModelsBuilder;
using Test;
namespace Test
{
    public partial class Type1
    {
        public static int GetProp1a(IType1 that) => that.Value<int>(""prop1a"");
    }
}
"}
            };

            var parseResult = new CodeParser().Parse(code);
            var builder = new Builder(types, parseResult);
            var btypes = builder.AllTypeModels;

            var sb = new StringBuilder();
            foreach (var model in builder.GetContentTypeModels())
                builder.WriteContentTypeModel(sb, model);
            var gen = sb.ToString();

            var version = ApiVersion.Current.Version;
            var expected = @"//------------------------------------------------------------------------------
// <auto-generated>
//   This code was generated by a tool.
//
//    ZpqrtBnk.ModelsBuilder v" + version + @"
//
//   Changes to this file will be lost if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Web;
using Umbraco.Core.Models;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web;
using ZpqrtBnk.ModelsBuilder;
using ZpqrtBnk.ModelsBuilder.Umbraco;
using System.CodeDom.Compiler;

namespace Umbraco.Web.PublishedModels
{
	// Mixin Content Type with alias ""type1""
	public partial interface IType1 : IPublishedContent
	{
		[GeneratedCodeAttribute(MB.Name, MB.VersionString)]
		string Prop1a { get; }

		[GeneratedCodeAttribute(MB.Name, MB.VersionString)]
		string Prop1b { get; }
	}

	/// <summary>Provides extension methods for the IType1 interface.</summary>
	public static partial class Type1Extensions
	{
		[GeneratedCodeAttribute(MB.Name, MB.VersionString)]
		public static string Prop1a(this IType1 that, Fallback fallback = default, string defaultValue = default)
			=> that.Value<string>(""prop1a"", fallback: fallback, defaultValue: defaultValue);

		[GeneratedCodeAttribute(MB.Name, MB.VersionString)]
		public static string Prop1b(this IType1 that, Fallback fallback = default, string defaultValue = default)
			=> that.Value<string>(""prop1b"", fallback: fallback, defaultValue: defaultValue);
	}

	[PublishedModel(""type1"")]
	public partial class Type1 : PublishedContentModel, IType1
	{
		// ctor
		public Type1(IPublishedContent content)
			: base(content)
		{ }

		// properties

		[GeneratedCodeAttribute(MB.Name, MB.VersionString)]
		[ImplementPropertyType(""prop1a"")]
		public string Prop1a => this.Prop1a();

		[GeneratedCodeAttribute(MB.Name, MB.VersionString)]
		[ImplementPropertyType(""prop1b"")]
		public string Prop1b => this.Prop1b();
	}
}
//------------------------------------------------------------------------------
// <auto-generated>
//   This code was generated by a tool.
//
//    ZpqrtBnk.ModelsBuilder v" + version + @"
//
//   Changes to this file will be lost if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Web;
using Umbraco.Core.Models;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web;
using ZpqrtBnk.ModelsBuilder;
using ZpqrtBnk.ModelsBuilder.Umbraco;
using System.CodeDom.Compiler;

namespace Umbraco.Web.PublishedModels
{
	/// <summary>Provides extension methods for the Type2 class.</summary>
	public static partial class Type2Extensions
	{
		[GeneratedCodeAttribute(MB.Name, MB.VersionString)]
		public static int Prop2(this Type2 that, Fallback fallback = default, int defaultValue = default)
			=> that.Value<int>(""prop2"", fallback: fallback, defaultValue: defaultValue);
	}

	[PublishedModel(""type2"")]
	public partial class Type2 : PublishedContentModel, IType1
	{
		// ctor
		public Type2(IPublishedContent content)
			: base(content)
		{ }

		// properties

		[GeneratedCodeAttribute(MB.Name, MB.VersionString)]
		[ImplementPropertyType(""prop2"")]
		public int Prop2 => this.Prop2();

		[GeneratedCodeAttribute(MB.Name, MB.VersionString)]
		[ImplementPropertyType(""prop1a"")]
		public string Prop1a => this.Prop1a();

		[GeneratedCodeAttribute(MB.Name, MB.VersionString)]
		[ImplementPropertyType(""prop1b"")]
		public string Prop1b => this.Prop1b();
	}
}
";

            Console.WriteLine(gen);
            Assert.AreEqual(expected.ClearLf(), gen);
        }

        [Test]
        public void GenerateSimpleType()
        {
            // Umbraco returns nice, pascal-cased names

            var type1 = new TypeModel
            {
                Id = 1,
                Alias = "type1",
                ClrName = "Type1",
                ParentId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
            };
            type1.Properties.Add(new PropertyModel
            {
                Alias = "prop1",
                ClrName = "Prop1",
                ModelClrType = typeof(string),
            });

            var types = new[] { type1 };

            var code = new Dictionary<string, string>
            {
            };

            var parseResult = new CodeParser().Parse(code);
            var builder = new Builder(types, parseResult);
            var btypes = builder.AllTypeModels;

            var sb = new StringBuilder();
            builder.WriteContentTypeModel(sb, builder.GetContentTypeModels().First());
            var gen = sb.ToString();

            var version = ApiVersion.Current.Version;
            var expected = @"//------------------------------------------------------------------------------
// <auto-generated>
//   This code was generated by a tool.
//
//    ZpqrtBnk.ModelsBuilder v" + version + @"
//
//   Changes to this file will be lost if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Web;
using Umbraco.Core.Models;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web;
using ZpqrtBnk.ModelsBuilder;
using ZpqrtBnk.ModelsBuilder.Umbraco;
using System.CodeDom.Compiler;

namespace Umbraco.Web.PublishedModels
{
	/// <summary>Provides extension methods for the Type1 class.</summary>
	public static partial class Type1Extensions
	{
		[GeneratedCodeAttribute(MB.Name, MB.VersionString)]
		public static string Prop1(this Type1 that, Fallback fallback = default, string defaultValue = default)
			=> that.Value<string>(""prop1"", fallback: fallback, defaultValue: defaultValue);
	}

	[PublishedModel(""type1"")]
	public partial class Type1 : PublishedContentModel
	{
		// ctor
		public Type1(IPublishedContent content)
			: base(content)
		{ }

		// properties

		[GeneratedCodeAttribute(MB.Name, MB.VersionString)]
		[ImplementPropertyType(""prop1"")]
		public string Prop1 => this.Prop1();
	}
}
";
            Console.WriteLine(gen);
            Assert.AreEqual(expected.ClearLf(), gen);
        }

        [Test]
        public void DontGeneratePropertyGetters()
        {
            // Umbraco returns nice, pascal-cased names

            var type1 = new TypeModel
            {
                Id = 1,
                Alias = "type1",
                ClrName = "Type1",
                ParentId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
            };
            type1.Properties.Add(new PropertyModel
            {
                Alias = "prop1",
                ClrName = "Prop1",
                ModelClrType = typeof(string),
            });

            var types = new[] { type1 };

            var code = new Dictionary<string, string>
            {
                {"assembly", @"
using ZpqrtBnk.ModelsBuilder;
[assembly:ModelsBuilderConfigure(GeneratePropertyGetters=false)]
"}
            };

            var refs = new[]
            {
                MetadataReference.CreateFromFile(typeof (string).Assembly.Location),
                MetadataReference.CreateFromFile(typeof (ReferencedAssemblies).Assembly.Location)
            };

            var parseResult = new CodeParser().Parse(code, refs);
            var builder = new Builder(types, parseResult);
            var btypes = builder.AllTypeModels;

            var sb = new StringBuilder();
            builder.WriteContentTypeModel(sb, builder.GetContentTypeModels().First());
            var gen = sb.ToString();

            var version = ApiVersion.Current.Version;
            var expected = @"//------------------------------------------------------------------------------
// <auto-generated>
//   This code was generated by a tool.
//
//    ZpqrtBnk.ModelsBuilder v" + version + @"
//
//   Changes to this file will be lost if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Web;
using Umbraco.Core.Models;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web;
using ZpqrtBnk.ModelsBuilder;
using ZpqrtBnk.ModelsBuilder.Umbraco;
using System.CodeDom.Compiler;

namespace Umbraco.Web.PublishedModels
{
	/// <summary>Provides extension methods for the Type1 class.</summary>
	public static partial class Type1Extensions
	{
		[GeneratedCodeAttribute(MB.Name, MB.VersionString)]
		public static string Prop1(this Type1 that, Fallback fallback = default, string defaultValue = default)
			=> that.Value<string>(""prop1"", fallback: fallback, defaultValue: defaultValue);
	}

	[PublishedModel(""type1"")]
	public partial class Type1 : PublishedContentModel
	{
		// ctor
		public Type1(IPublishedContent content)
			: base(content)
		{ }
	}
}
";
            Console.WriteLine(gen);
            Assert.AreEqual(expected.ClearLf(), gen);
        }

        [Test]
        public void GenerateSimpleType_AmbiguousIssue()
        {
            // Umbraco returns nice, pascal-cased names

            var type1 = new TypeModel
            {
                Id = 1,
                Alias = "type1",
                ClrName = "Type1",
                ParentId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
            };
            type1.Properties.Add(new PropertyModel
            {
                Alias = "foo",
                ClrName = "Foo",
                ModelClrType = typeof(IEnumerable<>).MakeGenericType(ModelType.For("foo")),
            });

            var type2 = new TypeModel
            {
                Id = 2,
                Alias = "foo",
                ClrName = "Foo",
                ParentId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Element,
            };

            var types = new[] { type1, type2 };

            var code = new Dictionary<string, string>
            {
                { "code", @"
namespace Umbraco.Web.PublishedModels
{
    public partial class Foo
    {
    }
}
" }
            };

            var parseResult = new CodeParser().Parse(code);
            var builder = new Builder(types, parseResult, "Umbraco.Web.PublishedModels");
            var btypes = builder.AllTypeModels;

            var sb1 = new StringBuilder();
            builder.WriteContentTypeModel(sb1, builder.GetContentTypeModels().Skip(1).First());
            var gen1 = sb1.ToString();
            Console.WriteLine(gen1);

            var sb = new StringBuilder();
            builder.WriteContentTypeModel(sb, builder.GetContentTypeModels().First());
            var gen = sb.ToString();

            var version = ApiVersion.Current.Version;
            var expected = @"//------------------------------------------------------------------------------
// <auto-generated>
//   This code was generated by a tool.
//
//    ZpqrtBnk.ModelsBuilder v" + version + @"
//
//   Changes to this file will be lost if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Web;
using Umbraco.Core.Models;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web;
using ZpqrtBnk.ModelsBuilder;
using ZpqrtBnk.ModelsBuilder.Umbraco;
using System.CodeDom.Compiler;

namespace Umbraco.Web.PublishedModels
{
	/// <summary>Provides extension methods for the Type1 class.</summary>
	public static partial class Type1Extensions
	{
		[GeneratedCodeAttribute(MB.Name, MB.VersionString)]
		public static IEnumerable<Foo> Foo(this Type1 that, Fallback fallback = default, IEnumerable<Foo> defaultValue = default)
			=> that.Value<IEnumerable<Foo>>(""foo"", fallback: fallback, defaultValue: defaultValue);
	}

	[PublishedModel(""type1"")]
	public partial class Type1 : PublishedContentModel
	{
		// ctor
		public Type1(IPublishedContent content)
			: base(content)
		{ }

		// properties

		[GeneratedCodeAttribute(MB.Name, MB.VersionString)]
		[ImplementPropertyType(""foo"")]
		public IEnumerable<Foo> Foo => this.Foo();
	}
}
";
            Console.WriteLine(gen);
            Assert.AreEqual(expected.ClearLf(), gen);
        }

        [Test]
        public void PartialConstructor()
        {
            var type1 = new TypeModel
            {
                Id = 1,
                Alias = "type1",
                ClrName = "Type1",
                ParentId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
            };
            type1.Properties.Add(new PropertyModel
            {
                Alias = "prop1",
                ClrName = "Prop1",
                ModelClrType = typeof(string),
            });

            var types = new[] { type1 };

            var code = new Dictionary<string, string>
            {
                { "assembly", @"
using ZpqrtBnk.ModelsBuilder;
using Umbraco.Core.Models;
using Umbraco.Core.Models.PublishedContent;

public partial class Type1
{
    public Type1(IPublishedContent content)
        : base(content)
    {
        // do our own stuff
    }
}
" }
            };

            var refs = new[]
            {
                MetadataReference.CreateFromFile(typeof (string).Assembly.Location),
                MetadataReference.CreateFromFile(typeof (IPublishedContent).Assembly.Location),
                MetadataReference.CreateFromFile(typeof (ReferencedAssemblies).Assembly.Location)
            };

            var parseResult = new CodeParser().Parse(code, refs);

            Assert.IsTrue(parseResult.HasCtor("Type1"));

            var builder = new Builder(types, parseResult);
            var btypes = builder.AllTypeModels;

            var sb = new StringBuilder();
            builder.WriteContentTypeModel(sb, builder.GetContentTypeModels().First());
            var gen = sb.ToString();

            var version = ApiVersion.Current.Version;
            var expected = @"//------------------------------------------------------------------------------
// <auto-generated>
//   This code was generated by a tool.
//
//    ZpqrtBnk.ModelsBuilder v" + version + @"
//
//   Changes to this file will be lost if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Web;
using Umbraco.Core.Models;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web;
using ZpqrtBnk.ModelsBuilder;
using ZpqrtBnk.ModelsBuilder.Umbraco;
using System.CodeDom.Compiler;

namespace Umbraco.Web.PublishedModels
{
	/// <summary>Provides extension methods for the Type1 class.</summary>
	public static partial class Type1Extensions
	{
		[GeneratedCodeAttribute(MB.Name, MB.VersionString)]
		public static string Prop1(this Type1 that, Fallback fallback = default, string defaultValue = default)
			=> that.Value<string>(""prop1"", fallback: fallback, defaultValue: defaultValue);
	}

	[PublishedModel(""type1"")]
	public partial class Type1 : PublishedContentModel
	{
		// properties

		[GeneratedCodeAttribute(MB.Name, MB.VersionString)]
		[ImplementPropertyType(""prop1"")]
		public string Prop1 => this.Prop1();
	}
}
";
            Console.WriteLine(gen);
            Assert.AreEqual(expected.ClearLf(), gen);
        }

        [Test]
        public void PartialConstructorWithRename()
        {
            var type1 = new TypeModel
            {
                Id = 1,
                Alias = "type1",
                ClrName = "Type1",
                ParentId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
            };
            type1.Properties.Add(new PropertyModel
            {
                Alias = "prop1",
                ClrName = "Prop1",
                ModelClrType = typeof(string),
            });

            var types = new[] { type1 };

            var code = new Dictionary<string, string>
            {
                { "assembly", @"
using ZpqrtBnk.ModelsBuilder;
using Umbraco.Core.Models;
using Umbraco.Core.Models.PublishedContent;

[assembly:RenameContentType(""type1"", ""Type2"")]

public partial class Type2
{
    public Type2(IPublishedContent content)
        : base(content)
    {
        // do our own stuff
    }
}
" }
            };

            var refs = new[]
            {
                MetadataReference.CreateFromFile(typeof (string).Assembly.Location),
                MetadataReference.CreateFromFile(typeof (IPublishedContent).Assembly.Location),
                MetadataReference.CreateFromFile(typeof (ReferencedAssemblies).Assembly.Location)
            };

            var parseResult = new CodeParser().Parse(code, refs);

            Assert.IsFalse(parseResult.HasCtor("Type1"));
            Assert.IsTrue(parseResult.HasCtor("Type2"));

            var builder = new Builder(types, parseResult);
            var btypes = builder.AllTypeModels;

            var sb = new StringBuilder();
            builder.WriteContentTypeModel(sb, builder.GetContentTypeModels().First());
            var gen = sb.ToString();

            var version = ApiVersion.Current.Version;
            var expected = @"//------------------------------------------------------------------------------
// <auto-generated>
//   This code was generated by a tool.
//
//    ZpqrtBnk.ModelsBuilder v" + version + @"
//
//   Changes to this file will be lost if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Web;
using Umbraco.Core.Models;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web;
using ZpqrtBnk.ModelsBuilder;
using ZpqrtBnk.ModelsBuilder.Umbraco;
using System.CodeDom.Compiler;

namespace Umbraco.Web.PublishedModels
{
	/// <summary>Provides extension methods for the Type2 class.</summary>
	public static partial class Type2Extensions
	{
		[GeneratedCodeAttribute(MB.Name, MB.VersionString)]
		public static string Prop1(this Type2 that, Fallback fallback = default, string defaultValue = default)
			=> that.Value<string>(""prop1"", fallback: fallback, defaultValue: defaultValue);
	}

	// Content Type with alias ""type1""
	[PublishedModel(""type1"")]
	public partial class Type2 : PublishedContentModel
	{
		// properties

		[GeneratedCodeAttribute(MB.Name, MB.VersionString)]
		[ImplementPropertyType(""prop1"")]
		public string Prop1 => this.Prop1();
	}
}
";
            Console.WriteLine(gen);
            Assert.AreEqual(expected.ClearLf(), gen);
        }

        [Test]
        public void GenerateMixinType()
        {
            // Umbraco returns nice, pascal-cased names

            Current.Configs.Add(() => new Config());

            var type1 = new TypeModel
            {
                Id = 1,
                Alias = "type1",
                ClrName = "Type1",
                ParentId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
                IsMixin =  true,
            };
            type1.Properties.Add(new PropertyModel
            {
                Alias = "prop1",
                ClrName = "Prop1",
                ModelClrType = typeof(string),
            });

            var type2 = new TypeModel
            {
                Id = 2,
                Alias = "type2",
                ClrName = "Type2",
                ParentId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
            };
            type2.MixinTypes.Add(type1);
            type2.Properties.Add(new PropertyModel
            {
                Alias = "prop2",
                ClrName = "Prop2",
                ModelClrType = typeof(int),
            });

            var types = new[] { type1, type2 };

            var code = new Dictionary<string, string>
            {
            };

            var parseResult = new CodeParser().Parse(code);
            var builder = new Builder(types, parseResult);
            var btypes = builder.AllTypeModels;

            var sb = new StringBuilder();
            foreach (var model in builder.GetContentTypeModels())
                builder.WriteContentTypeModel(sb, model);
            var gen = sb.ToString();

            var version = typeof(BuilderBase).Assembly.GetName().Version;

            var expected = @"//------------------------------------------------------------------------------
// <auto-generated>
//   This code was generated by a tool.
//
//    ZpqrtBnk.ModelsBuilder v" + version + @"
//
//   Changes to this file will be lost if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
";
            Console.WriteLine(gen);
            return;

            //Assert.AreEqual(expected.ClearLf(), gen);
        }

        [Test]
        public void GenerateAmbiguous()
        {
            var type1 = new TypeModel
            {
                Id = 1,
                Alias = "type1",
                ClrName = "Type1",
                ParentId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
                IsMixin = true,
            };
            type1.Properties.Add(new PropertyModel
            {
                Alias = "prop1",
                ClrName = "Prop1",
                ModelClrType = typeof(IPublishedContent),
            });
            type1.Properties.Add(new PropertyModel
            {
                Alias = "prop2",
                ClrName = "Prop2",
                ModelClrType = typeof(global::System.Text.StringBuilder),
            });
            type1.Properties.Add(new PropertyModel
            {
                Alias = "prop3",
                ClrName = "Prop3",
                ModelClrType = typeof(global::Umbraco.Core.IO.FileSecurityException),
            });
            var types = new[] { type1 };

            var code = new Dictionary<string, string>
            {
            };

            var parseResult = new CodeParser().Parse(code);
            var builder = new Builder(types, parseResult, "ZpqrtBnk.ModelsBuilder.Models"); // forces conflict with ZpqrtBnk.ModelsBuilder.Umbraco
            var btypes = builder.AllTypeModels;

            var sb = new StringBuilder();
            foreach (var model in builder.GetContentTypeModels())
                builder.WriteContentTypeModel(sb, model);
            var gen = sb.ToString();

            Console.WriteLine(gen);

            Assert.IsTrue(gen.Contains(" IPublishedContent Prop1"));
            Assert.IsTrue(gen.Contains(" System.Text.StringBuilder Prop2"));
            Assert.IsTrue(gen.Contains(" global::Umbraco.Core.IO.FileSecurityException Prop3"));
        }

        [TestCase("int", typeof(int))]
        [TestCase("IEnumerable<int>", typeof(IEnumerable<int>))]
        [TestCase("ZpqrtBnk.ModelsBuilder.Tests.BuilderTestsClass1", typeof(BuilderTestsClass1))]
        [TestCase("ZpqrtBnk.ModelsBuilder.Tests.BuilderTests.Class1", typeof(Class1))]
        public void WriteClrType(string expected, Type input)
        {
            var builder = new Builder(Array.Empty<TypeModel>(), ParseResult.Empty);
            builder.ModelsNamespaceForTests = "ModelsNamespace";
            var sb = new StringBuilder();
            builder.AppendClrType(sb, input);
            Assert.AreEqual(expected, sb.ToString());
        }

        [TestCase("int", typeof(int))]
        [TestCase("IEnumerable<int>", typeof(IEnumerable<int>))]
        [TestCase("BuilderTestsClass1", typeof(BuilderTestsClass1))]
        [TestCase("BuilderTests.Class1", typeof(Class1))]
        public void WriteClrTypeUsing(string expected, Type input)
        {
            var builder = new Builder(Array.Empty<TypeModel>(), ParseResult.Empty);
            builder.Using.Add("ZpqrtBnk.ModelsBuilder.Tests");
            builder.ModelsNamespaceForTests = "ModelsNamespace";
            var sb = new StringBuilder();
            builder.AppendClrType(sb, input);
            Assert.AreEqual(expected, sb.ToString());
        }

        [TestCase(true, true, "Borked", typeof(global::System.Text.ASCIIEncoding), "System.Text.ASCIIEncoding")]
        [TestCase(true, false, "Borked", typeof(global::System.Text.ASCIIEncoding), "ASCIIEncoding")]
        [TestCase(false, true, "Borked", typeof(global::System.Text.ASCIIEncoding), "System.Text.ASCIIEncoding")]
        [TestCase(false, false, "Borked", typeof(global::System.Text.ASCIIEncoding), "System.Text.ASCIIEncoding")]

        [TestCase(true, true, "ZpqrtBnk.ModelsBuilder.Tests", typeof(global::System.Text.ASCIIEncoding), "global::System.Text.ASCIIEncoding")]
        [TestCase(true, false, "ZpqrtBnk.ModelsBuilder.Tests", typeof(global::System.Text.ASCIIEncoding), "global::System.Text.ASCIIEncoding")]
        [TestCase(false, true, "ZpqrtBnk.ModelsBuilder.Tests", typeof(global::System.Text.ASCIIEncoding), "global::System.Text.ASCIIEncoding")]
        [TestCase(false, false, "ZpqrtBnk.ModelsBuilder.Tests", typeof(global::System.Text.ASCIIEncoding), "global::System.Text.ASCIIEncoding")]

        [TestCase(true, true, "Borked", typeof(StringBuilder), "StringBuilder")]
        [TestCase(true, false, "Borked", typeof(StringBuilder), "StringBuilder")]
        [TestCase(false, true, "Borked", typeof(StringBuilder), "System.Text.StringBuilder")] // magic? using = not ambiguous
        [TestCase(false, false, "Borked", typeof(StringBuilder), "System.Text.StringBuilder")]

        [TestCase(true, true, "ZpqrtBnk.ModelsBuilder.Tests", typeof(StringBuilder), "StringBuilder")]
        [TestCase(true, false, "ZpqrtBnk.ModelsBuilder.Tests", typeof(StringBuilder), "StringBuilder")]
        [TestCase(false, true, "ZpqrtBnk.ModelsBuilder.Tests", typeof(StringBuilder), "global::System.Text.StringBuilder")] // magic? in ns = ambiguous
        [TestCase(false, false, "ZpqrtBnk.ModelsBuilder.Tests", typeof(StringBuilder), "global::System.Text.StringBuilder")]
        public void WriteClrType_Ambiguous_Ns(bool usingSystem, bool usingZb, string ns, Type type, string expected)
        {
            var builder = new Builder(Array.Empty<TypeModel>(), ParseResult.Empty);
            if (usingSystem) builder.Using.Add("System.Text");
            if (usingZb) builder.Using.Add("ZpqrtBnk.ModelsBuilder.Tests");
            builder.ModelsNamespaceForTests = ns;
            var sb = new StringBuilder();
            builder.AppendClrType(sb, type);

            Assert.AreEqual(expected, sb.ToString());
        }

        [Test]
        public void WriteClrType_AmbiguousWithNested()
        {
            var builder = new Builder(Array.Empty<TypeModel>(), ParseResult.Empty);
            builder.Using.Add("System.Text");
            builder.Using.Add("ZpqrtBnk.ModelsBuilder.Tests");
            builder.ModelsNamespaceForTests = "SomeRandomNamespace";
            var sb = new StringBuilder();
            builder.AppendClrType(sb, typeof(ASCIIEncoding.Nested));

            // full type name is needed but not global::
            Assert.AreEqual("ZpqrtBnk.ModelsBuilder.Tests.ASCIIEncoding.Nested", sb.ToString());
        }

        [Test]
        public void VaryingProperties()
        {
            // Umbraco returns nice, pascal-cased names

            var type1 = new TypeModel
            {
                Id = 1,
                Alias = "type1",
                ClrName = "Type1",
                ParentId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
                Variations = ContentVariation.Culture
            };
            type1.Properties.Add(new PropertyModel
            {
                Alias = "prop1",
                ClrName = "Prop1",
                ModelClrType = typeof(string),
                Variations = ContentVariation.Culture
            });
            type1.Properties.Add(new PropertyModel
            {
                Alias = "prop2",
                ClrName = "Prop2",
                ModelClrType = typeof(string),
                Variations = ContentVariation.Culture
            });
            type1.Properties.Add(new PropertyModel
            {
                Alias = "prop3",
                ClrName = "Prop3",
                ModelClrType = typeof(string),
                Variations = ContentVariation.Segment
            });
            type1.Properties.Add(new PropertyModel
            {
                Alias = "prop4",
                ClrName = "Prop4",
                ModelClrType = typeof(string),
                Variations = ContentVariation.CultureAndSegment
            });
            type1.Properties.Add(new PropertyModel
            {
                Alias = "prop5",
                ClrName = "Prop5",
                ModelClrType = typeof(string)
            });
            type1.Properties.Add(new PropertyModel
            {
                Alias = "prop6",
                ClrName = "Prop6",
                ModelClrType = typeof(string),
                Variations = ContentVariation.Culture
            });

            var types = new[] { type1 };

            var code = new Dictionary<string, string>
            {
                {"assembly", @"
using System;
using ZpqrtBnk.ModelsBuilder;
namespace Models
{
    public static partial class Type1Extensions
    {
        public static string Prop1(this Type1 that, string culture = null, string segment = null) { return """"; }
    }

    [IgnorePropertyType(""prop6"")]
    public partial class Type1
    { }
}
"}
            };

            var refs = new[]
            {
                MetadataReference.CreateFromFile(typeof (string).Assembly.Location),
                MetadataReference.CreateFromFile(typeof (ReferencedAssemblies).Assembly.Location)
            };

            var parseResult = new CodeParser().Parse(code, refs);
            var builder = new Builder(types, parseResult);
            var btypes = builder.AllTypeModels;

            var sb = new StringBuilder();
            builder.WriteContentTypeModel(sb, builder.GetContentTypeModels().First());
            var gen = sb.ToString();

            var version = ApiVersion.Current.Version;
            var expected = @"//------------------------------------------------------------------------------
// <auto-generated>
//   This code was generated by a tool.
//
//    ZpqrtBnk.ModelsBuilder v" + version + @"
//
//   Changes to this file will be lost if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Web;
using Umbraco.Core.Models;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web;
using ZpqrtBnk.ModelsBuilder;
using ZpqrtBnk.ModelsBuilder.Umbraco;
using System.CodeDom.Compiler;

namespace Umbraco.Web.PublishedModels
{
	/// <summary>Provides extension methods for the Type1 class.</summary>
	public static partial class Type1Extensions
	{
		[GeneratedCodeAttribute(MB.Name, MB.VersionString)]
		public static string Prop2(this Type1 that, string culture = null, Fallback fallback = default, string defaultValue = default)
			=> that.Value<string>(""prop2"", culture: culture, fallback: fallback, defaultValue: defaultValue);

		[GeneratedCodeAttribute(MB.Name, MB.VersionString)]
		public static string Prop3(this Type1 that, string segment = null, Fallback fallback = default, string defaultValue = default)
			=> that.Value<string>(""prop3"", segment: segment, fallback: fallback, defaultValue: defaultValue);

		[GeneratedCodeAttribute(MB.Name, MB.VersionString)]
		public static string Prop4(this Type1 that, string culture = null, string segment = null, Fallback fallback = default, string defaultValue = default)
			=> that.Value<string>(""prop4"", culture: culture, segment: segment, fallback: fallback, defaultValue: defaultValue);

		[GeneratedCodeAttribute(MB.Name, MB.VersionString)]
		public static string Prop5(this Type1 that, Fallback fallback = default, string defaultValue = default)
			=> that.Value<string>(""prop5"", fallback: fallback, defaultValue: defaultValue);
	}

	[PublishedModel(""type1"")]
	public partial class Type1 : PublishedContentModel
	{
		// ctor
		public Type1(IPublishedContent content)
			: base(content)
		{ }

		// properties

		[GeneratedCodeAttribute(MB.Name, MB.VersionString)]
		[ImplementPropertyType(""prop1"")]
		public string Prop1 => this.Prop1();

		[GeneratedCodeAttribute(MB.Name, MB.VersionString)]
		[ImplementPropertyType(""prop2"")]
		public string Prop2 => this.Prop2();

		[GeneratedCodeAttribute(MB.Name, MB.VersionString)]
		[ImplementPropertyType(""prop3"")]
		public string Prop3 => this.Prop3();

		[GeneratedCodeAttribute(MB.Name, MB.VersionString)]
		[ImplementPropertyType(""prop4"")]
		public string Prop4 => this.Prop4();

		[GeneratedCodeAttribute(MB.Name, MB.VersionString)]
		[ImplementPropertyType(""prop5"")]
		public string Prop5 => this.Prop5();
	}
}
";
            Console.WriteLine(gen);
            Assert.AreEqual(expected.ClearLf(), gen);

        }

        [Test]
        public void SelectiveBaseClass()
        {
            // Umbraco returns nice, pascal-cased names

            var types = new List<TypeModel>();

            for (var i = 1; i < 5; i++)
            {
                types.Add(new TypeModel
                {
                    Id = i,
                    Alias = "type" + i,
                    ClrName = "Type" + i,
                    ParentId = 0,
                    BaseType = null,
                    ItemType = TypeModel.ItemTypes.Content
                });
            }

            for (var i = 1; i < 5; i++)
            {
                types.Add(new TypeModel
                {
                    Id = i + 4,
                    Alias = "type" + (i + 4),
                    ClrName = "Type" + (i + 4),
                    ParentId = 0,
                    BaseType = null,
                    ItemType = TypeModel.ItemTypes.Element
                });
            }

            var code = new Dictionary<string, string>
            {
                {"assembly", @"
using ZpqrtBnk.ModelsBuilder;

[assembly:ContentModelsBaseClass(typeof(ContentModelBase1))]
[assembly:ElementModelsBaseClass(typeof(ElementModelBase1))]

[assembly:ContentModelsBaseClass(""*3"", typeof(ContentModelBase2))]
[assembly:ElementModelsBaseClass(""*7"", typeof(ElementModelBase2))]

[assembly:ContentModelsBaseClass(""type4"", typeof(ContentModelBase3))]
[assembly:ElementModelsBaseClass(""type8"", typeof(ElementModelBase3))]

public class ContentModelBase1 {}
public class ElementModelBase1 {}
public class ContentModelBase2 {}
public class ElementModelBase2 {}
public class ContentModelBase3 {}
public class ElementModelBase3 {}
"}
            };

            var refs = new[]
            {
                MetadataReference.CreateFromFile(typeof (string).Assembly.Location),
                MetadataReference.CreateFromFile(typeof (ReferencedAssemblies).Assembly.Location)
            };

            var parseResult = new CodeParser().Parse(code, refs);
            var builder = new Builder(types, parseResult);
            var btypes = builder.AllTypeModels;

            var modelsToGenerate = builder.GetContentTypeModels().ToList();

            var sb = new StringBuilder();
            builder.WriteContentTypeModel(sb, modelsToGenerate[0]);
            var gen = sb.ToString();

            Console.WriteLine(gen);
            Assert.IsTrue(gen.Contains("public partial class Type1 : ContentModelBase1"));

            sb = new StringBuilder();
            builder.WriteContentTypeModel(sb, modelsToGenerate[1]);
            gen = sb.ToString();

            Console.WriteLine(gen);
            Assert.IsTrue(gen.Contains("public partial class Type2 : ContentModelBase1"));

            sb = new StringBuilder();
            builder.WriteContentTypeModel(sb, modelsToGenerate[2]);
            gen = sb.ToString();

            Console.WriteLine(gen);
            Assert.IsTrue(gen.Contains("public partial class Type3 : ContentModelBase2"));

            sb = new StringBuilder();
            builder.WriteContentTypeModel(sb, modelsToGenerate[3]);
            gen = sb.ToString();

            Console.WriteLine(gen);
            Assert.IsTrue(gen.Contains("public partial class Type4 : ContentModelBase3"));
            
            sb = new StringBuilder();
            builder.WriteContentTypeModel(sb, modelsToGenerate[4]);
            gen = sb.ToString();

            Console.WriteLine(gen);
            Assert.IsTrue(gen.Contains("public partial class Type5 : ElementModelBase1"));
            
            sb = new StringBuilder();
            builder.WriteContentTypeModel(sb, modelsToGenerate[5]);
            gen = sb.ToString();

            Console.WriteLine(gen);
            Assert.IsTrue(gen.Contains("public partial class Type6 : ElementModelBase1"));

            sb = new StringBuilder();
            builder.WriteContentTypeModel(sb, modelsToGenerate[6]);
            gen = sb.ToString();

            Console.WriteLine(gen);
            Assert.IsTrue(gen.Contains("public partial class Type7 : ElementModelBase2"));

            sb = new StringBuilder();
            builder.WriteContentTypeModel(sb, modelsToGenerate[7]);
            gen = sb.ToString();

            Console.WriteLine(gen);
            Assert.IsTrue(gen.Contains("public partial class Type8 : ElementModelBase3"));
        }

        [Test]
        public void GenerateSimpleMeta()
        {
            // Umbraco returns nice, pascal-cased names

            var types = new List<TypeModel>();
            
            var type1 = new TypeModel
            {
                Id = 1,
                Alias = "type1",
                ClrName = "Type1",
                ParentId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
            };
            type1.Properties.Add(new PropertyModel
            {
                Alias = "prop1",
                ClrName = "Prop1",
                ModelClrType = typeof(string),
            });
            types.Add(type1);

            var type2 = new TypeModel
            {
                Id = 2,
                Alias = "type2",
                ClrName = "Type2",
                ParentId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
            };
            type2.Properties.Add(new PropertyModel
            {
                Alias = "prop1",
                ClrName = "Prop1",
                ModelClrType = typeof(string),
            });
            type2.Properties.Add(new PropertyModel
            {
                Alias = "prop2",
                ClrName = "Prop2",
                ModelClrType = typeof(global::System.Web.IHtmlString),
            });
            type2.Properties.Add(new PropertyModel
            {
                Alias = "prop3",
                ClrName = "Prop3",
                ModelClrType = typeof(string),
            });
            types.Add(type2);

            var type3 = new TypeModel
            {
                Id = 3,
                Alias = "type3",
                ClrName = "Type3",
                ParentId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Media,
            };
            types.Add(type3);

            var code = new Dictionary<string, string>
            {
            };

            var parseResult = new CodeParser().Parse(code);
            var builder = new Builder(types, parseResult);
            var btypes = builder.AllTypeModels;

            var sb = new StringBuilder();
            builder.WriteContentTypesMetadata(sb, builder.GetContentTypeModels());
            var gen = sb.ToString();

            var version = ApiVersion.Current.Version;
            var expected = @"//------------------------------------------------------------------------------
// <auto-generated>
//   This code was generated by a tool.
//
//    ZpqrtBnk.ModelsBuilder v" + version + @"
//
//   Changes to this file will be lost if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Web;
using Umbraco.Core.Models;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web;
using ZpqrtBnk.ModelsBuilder;
using ZpqrtBnk.ModelsBuilder.Umbraco;
using System.Linq;
using System.CodeDom.Compiler;

namespace Umbraco.Web.PublishedModels
{
	/// <summary>Models Builder</summary>
	public static partial class MB
	{
		/// <summary>Gets Models Builder's generator name.</summary>
		public const string Name = ""ZpqrtBnk.ModelsBuilder"";

		/// <summary>Gets the Models Builder version that was used to generate the files.</summary>
		public const string VersionString = """ + version + @""";

		/// <summary>Provides the content type published item types.</summary>
		[GeneratedCodeAttribute(Name, VersionString)]
		public static class ItemType
		{
			/// <summary>Gets the published item type of the Type1 content type.</summary>
			[GeneratedCodeAttribute(Name, VersionString)]
			public const PublishedItemType Type1 = PublishedItemType.Content;

			/// <summary>Gets the published item type of the Type2 content type.</summary>
			[GeneratedCodeAttribute(Name, VersionString)]
			public const PublishedItemType Type2 = PublishedItemType.Content;

			/// <summary>Gets the published item type of the Type3 content type.</summary>
			[GeneratedCodeAttribute(Name, VersionString)]
			public const PublishedItemType Type3 = PublishedItemType.Media;
		}

		/// <summary>Defines the content type alias constants.</summary>
		[GeneratedCodeAttribute(Name, VersionString)]
		public static class ContentAlias
		{
			/// <summary>Gets the alias of the Type1 content type.</summary>
			[GeneratedCodeAttribute(Name, VersionString)]
			public const string Type1 = ""type1"";

			/// <summary>Gets the alias of the Type2 content type.</summary>
			[GeneratedCodeAttribute(Name, VersionString)]
			public const string Type2 = ""type2"";

			/// <summary>Gets the alias of the Type3 content type.</summary>
			[GeneratedCodeAttribute(Name, VersionString)]
			public const string Type3 = ""type3"";
		}

		/// <summary>Defines the property type alias constants.</summary>
		[GeneratedCodeAttribute(Name, VersionString)]
		public static class PropertyAlias
		{
			/// <summary>Defines the property type alias constants for the Type1 content type.</summary>
			[GeneratedCodeAttribute(Name, VersionString)]
			public static class Type1
			{
				/// <summary>Gets the alias of the Type1.Prop1 property type.</summary>
				[GeneratedCodeAttribute(Name, VersionString)]
				public const string Prop1 = ""prop1"";
			}

			/// <summary>Defines the property type alias constants for the Type2 content type.</summary>
			[GeneratedCodeAttribute(Name, VersionString)]
			public static class Type2
			{
				/// <summary>Gets the alias of the Type2.Prop1 property type.</summary>
				[GeneratedCodeAttribute(Name, VersionString)]
				public const string Prop1 = ""prop1"";

				/// <summary>Gets the alias of the Type2.Prop2 property type.</summary>
				[GeneratedCodeAttribute(Name, VersionString)]
				public const string Prop2 = ""prop2"";

				/// <summary>Gets the alias of the Type2.Prop3 property type.</summary>
				[GeneratedCodeAttribute(Name, VersionString)]
				public const string Prop3 = ""prop3"";
			}

			/// <summary>Defines the property type alias constants for the Type3 content type.</summary>
			[GeneratedCodeAttribute(Name, VersionString)]
			public static class Type3
			{
			}
		}

		/// <summary>Provides the content types.</summary>
		[GeneratedCodeAttribute(Name, VersionString)]
		public static class ContentType
		{
			/// <summary>Gets the Type1 content type.</summary>
			[GeneratedCodeAttribute(Name, VersionString)]
			public static readonly IPublishedContentType Type1 = PublishedModelUtility.GetModelContentType(ItemType.Type1, ""type1"");

			/// <summary>Gets the Type2 content type.</summary>
			[GeneratedCodeAttribute(Name, VersionString)]
			public static readonly IPublishedContentType Type2 = PublishedModelUtility.GetModelContentType(ItemType.Type2, ""type2"");

			/// <summary>Gets the Type3 content type.</summary>
			[GeneratedCodeAttribute(Name, VersionString)]
			public static readonly IPublishedContentType Type3 = PublishedModelUtility.GetModelContentType(ItemType.Type3, ""type3"");
		}

		/// <summary>Provides the property types.</summary>
		[GeneratedCodeAttribute(Name, VersionString)]
		public static class PropertyType
		{
			/// <summary>Provides the property types for the Type1 content type.</summary>
			[GeneratedCodeAttribute(Name, VersionString)]
			public static class Type1
			{
				/// <summary>Gets the Type1.Prop1 property type.</summary>
				[GeneratedCodeAttribute(Name, VersionString)]
				public static readonly IPublishedPropertyType Prop1 = ContentType.Type1.GetPropertyType(PropertyAlias.Type1.Prop1);
			}

			/// <summary>Provides the property types for the Type2 content type.</summary>
			[GeneratedCodeAttribute(Name, VersionString)]
			public static class Type2
			{
				/// <summary>Gets the Type2.Prop1 property type.</summary>
				[GeneratedCodeAttribute(Name, VersionString)]
				public static readonly IPublishedPropertyType Prop1 = ContentType.Type2.GetPropertyType(PropertyAlias.Type2.Prop1);

				/// <summary>Gets the Type2.Prop2 property type.</summary>
				[GeneratedCodeAttribute(Name, VersionString)]
				public static readonly IPublishedPropertyType Prop2 = ContentType.Type2.GetPropertyType(PropertyAlias.Type2.Prop2);

				/// <summary>Gets the Type2.Prop3 property type.</summary>
				[GeneratedCodeAttribute(Name, VersionString)]
				public static readonly IPublishedPropertyType Prop3 = ContentType.Type2.GetPropertyType(PropertyAlias.Type2.Prop3);
			}

			/// <summary>Provides the property types for the Type3 content type.</summary>
			[GeneratedCodeAttribute(Name, VersionString)]
			public static class Type3
			{
			}
		}

		[GeneratedCodeAttribute(Name, VersionString)]
		private static readonly ContentTypeModelInfo[] _models = 
		{
			new ContentTypeModelInfo(""type1"", ""Type1"", typeof(Type1),
				new PropertyTypeModelInfo(""prop1"", ""Prop1"", typeof(string))),
			new ContentTypeModelInfo(""type2"", ""Type2"", typeof(Type2),
				new PropertyTypeModelInfo(""prop1"", ""Prop1"", typeof(string)),
				new PropertyTypeModelInfo(""prop2"", ""Prop2"", typeof(IHtmlString)),
				new PropertyTypeModelInfo(""prop3"", ""Prop3"", typeof(string))),
			new ContentTypeModelInfo(""type3"", ""Type3"", typeof(Type3))
		};

		/// <summary>Gets the model infos.</summary>
		[GeneratedCodeAttribute(Name, VersionString)]
		public static IReadOnlyCollection<ContentTypeModelInfo> Models => _models;

		/// <summary>Gets the model infos for a content type.</summary>
		[GeneratedCodeAttribute(Name, VersionString)]
		public static ContentTypeModelInfo Model(string alias) => _models.FirstOrDefault(x => x.Alias == alias);

		/// <summary>Gets the model infos for a content type.</summary>
		[GeneratedCodeAttribute(Name, VersionString)]
		public static ContentTypeModelInfo Model<TModel>() => _models.FirstOrDefault(x => x.ClrType == typeof(TModel));

		/// <summary>Gets the model infos for a content type.</summary>
		[GeneratedCodeAttribute(Name, VersionString)]
		public static ContentTypeModelInfo Model(Type typeofModel) => _models.FirstOrDefault(x => x.ClrType == typeofModel);
	}
}
";
            Console.WriteLine(gen);
            Assert.AreEqual(expected.ClearLf(), gen);
        }


        public class Class1 { }
    }

    // make it public to be ambiguous (see above)
    public class ASCIIEncoding
    {
        // can we handle nested types?
        public class Nested { }
    }

    class BuilderTestsClass1 {}

    public class System { }
}
