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
        public void ModelsBaseClassAttribute()
        {
            var code = new Dictionary<string, string>
            {
                {"assembly", @"
using Zbu.ModelsBuilder;
using Dang;
[assembly:ModelsBaseClass(typeof(Whatever))]
namespace Dang
{
public class Whatever
{}
}
"}
            };

            var disco = new CodeDiscovery().Discover(code);
            
            Assert.AreEqual("Dang.Whatever", disco.GetModelsBaseClassName("Otherwise"));
        }

        [Test]
        public void ModelsNamespaceAttribute()
        {
            var code = new Dictionary<string, string>
            {
                {"assembly", @"
using Zbu.ModelsBuilder;
[assembly:ModelsNamespace(""Foo.Bar.Nil"")]
"}
            };

            var disco = new CodeDiscovery().Discover(code);

            Assert.AreEqual("Foo.Bar.Nil", disco.GetModelsNamespace("Otherwise"));
        }

        [Test]
        public void ModelsUsingAttribute()
        {
            // Umbraco returns nice, pascal-cased names

            var type1 = new TypeModel
            {
                Id = 1,
                Alias = "type1",
                Name = "Type1",
                BaseTypeId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
            };

            var types = new[] { type1 };

            var code = new Dictionary<string, string>
            {
                {"assembly", @"
using Zbu.ModelsBuilder;
[assembly:ModelsUsing(""Foo.Bar.Nil"")]
"}
            };

            var builder = new TextBuilder(types);
            var disco = new CodeDiscovery().Discover(code);

            var count = builder.Using.Count;

            builder.Prepare(disco);

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
                Name = "Type1",
                BaseTypeId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
            };

            var types = new[] { type1 };

            var code = new Dictionary<string, string>
            {
                {"assembly", @"
using Zbu.ModelsBuilder;
[assembly:IgnoreContentType(""type1"")]
"}
            };

            var builder = new TextBuilder(types);
            var disco = new CodeDiscovery().Discover(code);
            builder.Prepare(disco);
            var btypes = builder.TypeModels;

            Assert.IsTrue(disco.IsIgnored("type1"));

            Assert.AreEqual(1, btypes.Count);
            Assert.IsTrue(btypes[0].IsContentIgnored);
        }

        [Test]
        public void ContentTypeIgnoreWildcard()
        {
            // Umbraco returns nice, pascal-cased names

            var type1 = new TypeModel
            {
                Id = 1,
                Alias = "type1",
                Name = "Type1",
                BaseTypeId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
            };

            var type2 = new TypeModel
            {
                Id = 2,
                Alias = "type2",
                Name = "Type2",
                BaseTypeId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
            };

            var type3 = new TypeModel
            {
                Id = 3,
                Alias = "ttype3",
                Name = "Ttype3",
                BaseTypeId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
            };
            
            var types = new[] { type1, type2, type3 };

            var code = new Dictionary<string, string>
            {
                {"assembly", @"
using Zbu.ModelsBuilder;
[assembly:IgnoreContentType(""type*"")]
"}
            };

            var builder = new TextBuilder(types);
            var disco = new CodeDiscovery().Discover(code);
            builder.Prepare(disco);
            var btypes = builder.TypeModels;

            Assert.IsTrue(disco.IsIgnored("type1"));
            Assert.IsTrue(disco.IsIgnored("type2"));
            Assert.IsFalse(disco.IsIgnored("ttype3"));

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
                Name = "Type1",
                BaseTypeId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
            };

            var type2 = new TypeModel
            {
                Id = 2,
                Alias = "type2",
                Name = "Type2",
                BaseTypeId = 1,
                BaseType = type1,
                ItemType = TypeModel.ItemTypes.Content,
            };

            var types = new[] { type1, type2 };

            var code = new Dictionary<string, string>
            {
                {"assembly", @"
using Zbu.ModelsBuilder;
[assembly:IgnoreContentType(""type1"")]
"}
            };

            var builder = new TextBuilder(types);
            var disco = new CodeDiscovery().Discover(code);
            builder.Prepare(disco);
            var btypes = builder.TypeModels;

            Assert.IsTrue(disco.IsIgnored("type1"));
            Assert.IsFalse(disco.IsIgnored("type2"));

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
                Name = "Type1",
                BaseTypeId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
            };

            var type2 = new TypeModel
            {
                Id = 2,
                Alias = "type2",
                Name = "Type2",
                BaseTypeId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
            };
            type2.MixinTypes.Add(type1);

            var types = new[] { type1, type2 };

            var code = new Dictionary<string, string>
            {
                {"assembly", @"
using Zbu.ModelsBuilder;
[assembly:IgnoreContentType(""type1"")]
"}
            };

            var builder = new TextBuilder(types);
            var disco = new CodeDiscovery().Discover(code);
            builder.Prepare(disco);
            var btypes = builder.TypeModels;

            Assert.IsTrue(disco.IsIgnored("type1"));
            Assert.IsFalse(disco.IsIgnored("type2"));

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
                Name = "Type1",
                BaseTypeId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
            };

            var type2 = new TypeModel
            {
                Id = 2,
                Alias = "type2",
                Name = "Type2",
                BaseTypeId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
            };

            var types = new[] { type1, type2 };

            var code = new Dictionary<string, string>
            {
                {"assembly", @"
using Zbu.ModelsBuilder;
[assembly:RenameContentType(""type1"", ""Renamed1"")]
"}
            };

            var builder = new TextBuilder(types);
            var disco = new CodeDiscovery().Discover(code);
            builder.Prepare(disco);
            var btypes = builder.TypeModels;

            Assert.IsFalse(disco.IsIgnored("type1"));
            Assert.IsFalse(disco.IsIgnored("type2"));
            Assert.IsTrue(disco.IsContentRenamed("type1"));
            Assert.IsFalse(disco.IsContentRenamed("type2"));
            Assert.AreEqual("Renamed1", disco.ContentName("type1"));
            Assert.IsNull(disco.ContentName("type2"));

            Assert.AreEqual(2, btypes.Count);
            Assert.IsFalse(btypes[0].IsContentIgnored);
            Assert.IsFalse(btypes[1].IsContentIgnored);
            Assert.AreEqual("Renamed1", btypes[0].Name);
            Assert.AreEqual("Type2", btypes[1].Name);
        }
        
        [Test]
        public void ContentTypeRenameOnClass()
        {
            // Umbraco returns nice, pascal-cased names

            var type1 = new TypeModel
            {
                Id = 1,
                Alias = "type1",
                Name = "Type1",
                BaseTypeId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
            };

            var type2 = new TypeModel
            {
                Id = 2,
                Alias = "type2",
                Name = "Type2",
                BaseTypeId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
            };

            var types = new[] { type1, type2 };

            var code = new Dictionary<string, string>
            {
                {"assembly", @"
using Zbu.ModelsBuilder;
namespace Models
{
    [RenameContentType(""type1"")]
    public partial class Renamed1
    {}
}
"}
            };

            var builder = new TextBuilder(types);
            var disco = new CodeDiscovery().Discover(code);
            builder.Prepare(disco);
            var btypes = builder.TypeModels;

            Assert.IsFalse(disco.IsIgnored("type1"));
            Assert.IsFalse(disco.IsIgnored("type2"));
            Assert.IsTrue(disco.IsContentRenamed("type1"));
            Assert.IsFalse(disco.IsContentRenamed("type2"));
            Assert.AreEqual("Renamed1", disco.ContentName("type1"));
            Assert.IsNull(disco.ContentName("type2"));

            Assert.AreEqual(2, btypes.Count);
            Assert.IsFalse(btypes[0].IsContentIgnored);
            Assert.IsFalse(btypes[1].IsContentIgnored);
            Assert.AreEqual("Renamed1", btypes[0].Name);
            Assert.AreEqual("Type2", btypes[1].Name);
        }

        [Test]
        public void PropertyTypeIgnore()
        {
            // Umbraco returns nice, pascal-cased names

            var type1 = new TypeModel
            {
                Id = 1,
                Alias = "type1",
                Name = "Type1",
                BaseTypeId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
            };
            type1.Properties.Add(new PropertyModel
            {
                Alias = "prop1",
                Name = "Prop1",
                ClrType = typeof(string),
            });

            var types = new[] { type1 };

            var code = new Dictionary<string, string>
            {
                {"assembly", @"
using Zbu.ModelsBuilder;
namespace Models
{
    [IgnorePropertyType(""prop1"")]
    public partial class Type1
    {
    }
}
"}
            };

            var builder = new TextBuilder(types);
            var disco = new CodeDiscovery().Discover(code);
            builder.Prepare(disco);
            var btypes = builder.TypeModels;

            Assert.IsTrue(disco.IsPropertyIgnored("Type1", "prop1"));

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
                Name = "Type1",
                BaseTypeId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
            };
            type1.Properties.Add(new PropertyModel
            {
                Alias = "prop1",
                Name = "Prop1",
                ClrType = typeof(string),
            });
            type1.Properties.Add(new PropertyModel
            {
                Alias = "prop2",
                Name = "Prop2",
                ClrType = typeof(string),
            });
            type1.Properties.Add(new PropertyModel
            {
                Alias = "pprop3",
                Name = "Pprop3",
                ClrType = typeof(string),
            });

            var types = new[] { type1 };

            var code = new Dictionary<string, string>
            {
                {"assembly", @"
using Zbu.ModelsBuilder;
namespace Models
{
    [IgnorePropertyType(""prop*"")]
    public partial class Type1
    {
    }
}
"}
            };

            var builder = new TextBuilder(types);
            var disco = new CodeDiscovery().Discover(code);
            builder.Prepare(disco);
            var btypes = builder.TypeModels;

            Assert.IsTrue(disco.IsPropertyIgnored("Type1", "prop1"));
            Assert.IsTrue(disco.IsPropertyIgnored("Type1", "prop2"));
            Assert.IsFalse(disco.IsPropertyIgnored("Type1", "pprop3"));

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
                Name = "Type1",
                BaseTypeId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
            };
            type1.Properties.Add(new PropertyModel
            {
                Alias = "prop1",
                Name = "Prop1",
                ClrType = typeof(string),
            });

            var types = new[] { type1 };

            var code = new Dictionary<string, string>
            {
                {"assembly", @"
using Zbu.ModelsBuilder;
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

            var builder = new TextBuilder(types);
            var disco = new CodeDiscovery().Discover(code);
            builder.Prepare(disco);
            var btypes = builder.TypeModels;

            Assert.IsTrue(disco.IsPropertyIgnored("Type1", "prop1"));

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
                Name = "Type1",
                BaseTypeId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
            };
            type1.Properties.Add(new PropertyModel
            {
                Alias = "prop1",
                Name = "Prop1",
                ClrType = typeof(string),
            });

            var types = new[] { type1 };

            var code = new Dictionary<string, string>
            {
                {"assembly", @"
using Zbu.ModelsBuilder;
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

            var builder = new TextBuilder(types);
            var disco = new CodeDiscovery().Discover(code);
            builder.Prepare(disco);
            var btypes = builder.TypeModels;

            Assert.AreEqual("Renamed1", disco.PropertyName("Type1", "prop1"));
            Assert.AreEqual("Renamed2", disco.PropertyName("Type1", "prop2"));

            Assert.AreEqual(1, btypes.Count);
            Assert.IsTrue(btypes[0].Properties[0].Name == "Renamed1");
        }

        [Test]
        public void PropertyTypeRenameOnClassInherit()
        {
            // Umbraco returns nice, pascal-cased names

            var type1 = new TypeModel
            {
                Id = 1,
                Alias = "type1",
                Name = "Type1",
                BaseTypeId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
            };
            type1.Properties.Add(new PropertyModel
            {
                Alias = "prop1",
                Name = "Prop1",
                ClrType = typeof(string),
            });

            var types = new[] { type1 };

            var code = new Dictionary<string, string>
            {
                {"assembly", @"
using Zbu.ModelsBuilder;
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

            var builder = new TextBuilder(types);
            var disco = new CodeDiscovery().Discover(code);
            builder.Prepare(disco);
            var btypes = builder.TypeModels;

            Assert.AreEqual("Renamed1", disco.PropertyName("Type1", "prop1"));
            Assert.AreEqual("Renamed2", disco.PropertyName("Type1", "prop2"));

            Assert.AreEqual(1, btypes.Count);
            Assert.IsTrue(btypes[0].Properties[0].Name == "Renamed1");
        }

        [Test]
        public void PropertyTypeRenameOnProperty()
        {
            // Umbraco returns nice, pascal-cased names

            var type1 = new TypeModel
            {
                Id = 1,
                Alias = "type1",
                Name = "Type1",
                BaseTypeId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
            };
            type1.Properties.Add(new PropertyModel
            {
                Alias = "prop1",
                Name = "Prop1",
                ClrType = typeof(string),
            });

            var types = new[] { type1 };

            var code = new Dictionary<string, string>
            {
                {"assembly", @"
using Zbu.ModelsBuilder;
namespace Models
{
    public partial class Type1
    {
        [RenamePropertyType(""prop1"")]
        public string Renamed1 { get { return """"; } }
    }
}
"}
            };

            var builder = new TextBuilder(types);
            var disco = new CodeDiscovery().Discover(code);
            builder.Prepare(disco);
            var btypes = builder.TypeModels;

            Assert.AreEqual("Renamed1", disco.PropertyName("Type1", "prop1"));

            Assert.AreEqual(1, btypes.Count);
            Assert.IsTrue(btypes[0].Properties[0].Name == "Renamed1");
        }

        [Test]
        public void PropertyTypeRenameOnPropertyInherit()
        {
            // Umbraco returns nice, pascal-cased names

            var type1 = new TypeModel
            {
                Id = 1,
                Alias = "type1",
                Name = "Type1",
                BaseTypeId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
            };
            type1.Properties.Add(new PropertyModel
            {
                Alias = "prop1",
                Name = "Prop1",
                ClrType = typeof(string),
            });

            var types = new[] { type1 };

            var code = new Dictionary<string, string>
            {
                {"assembly", @"
using Zbu.ModelsBuilder;
namespace Models
{
    public class Type2
    {
        [RenamePropertyType(""prop1"")]
        public string Renamed1 { get { return """"; } }
    }

    public partial class Type1 : Type2
    {
    }
}
"}
            };

            var builder = new TextBuilder(types);
            var disco = new CodeDiscovery().Discover(code);
            builder.Prepare(disco);
            var btypes = builder.TypeModels;

            Assert.AreEqual("Renamed1", disco.PropertyName("Type1", "prop1"));

            Assert.AreEqual(1, btypes.Count);
            Assert.IsTrue(btypes[0].Properties[0].Name == "Renamed1");
        }

        [Test]
        public void GenerateSimpleType()
        {
            // Umbraco returns nice, pascal-cased names

            var type1 = new TypeModel
            {
                Id = 1,
                Alias = "type1",
                Name = "Type1",
                BaseTypeId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
            };
            type1.Properties.Add(new PropertyModel
            {
                Alias = "prop1",
                Name = "Prop1",
                ClrType = typeof(string),
            });

            var types = new[] { type1 };

            var code = new Dictionary<string, string>
            {
            };

            var builder = new TextBuilder(types);
            var disco = new CodeDiscovery().Discover(code);
            builder.Prepare(disco);
            var btypes = builder.TypeModels;

            var sb = new StringBuilder();
            builder.Generate(sb, builder.GetModelsToGenerate().First());
            var gen = sb.ToString();

            var version = typeof (BuilderTests).Assembly.GetName().Version;
            var expected = @"//------------------------------------------------------------------------------
// <auto-generated>
//   This code was generated by a tool.
//
//    Zbu.ModelsBuilder v" + version + @"
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
using Zbu.ModelsBuilder;
using Zbu.ModelsBuilder.Umbraco;

namespace 
{
	[PublishedContentModel(""type1"")]
	public partial class Type1 : PublishedContentModel
	{
		public new const string ModelTypeAlias = ""type1"";
		public new const PublishedItemType ModelItemType = PublishedItemType.Content;

		public Type1(IPublishedContent content)
			: base(content)
		{ }

		public new static PublishedContentType GetModelContentType()
		{
			return PublishedContentType.Get(ModelItemType, ModelTypeAlias);
		}

		public static PublishedPropertyType GetModelPropertyType<TValue>(Expression<Func<Type1, TValue>> selector)
		{
			return PublishedContentModelUtility.GetModelPropertyType(GetModelContentType(), selector);
		}

		[ModelPropertyAlias(""prop1"")]
		public string Prop1
		{
			get { return this.GetPropertyValue<string>(""prop1""); }
		}
	}
}
".Replace("\r", "");

            Console.WriteLine(gen);
            Assert.AreEqual(expected, gen);
        }

        [TestCase("int", typeof (int))]
        [TestCase("IEnumerable<int>", typeof(IEnumerable<int>))]
        [TestCase("Zbu.ModelsBuilder.Tests.BuilderTestsClass1", typeof(BuilderTestsClass1))]
        [TestCase("Zbu.ModelsBuilder.Tests.BuilderTests.Class1", typeof(Class1))]
        public void WriteClrType(string expected, Type input)
        {
            var builder = new TextBuilder(new TypeModel[] {});
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
            var builder = new TextBuilder(new TypeModel[] { });
            builder.Using.Add("Zbu.ModelsBuilder.Tests");
            var sb = new StringBuilder();
            builder.WriteClrType(sb, input);
            Assert.AreEqual(expected, sb.ToString());
        }

        public class Class1 { }
    }

    class BuilderTestsClass1 {}
}
