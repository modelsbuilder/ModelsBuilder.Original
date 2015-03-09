using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Zbu.ModelsBuilder.Building;

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

            var parseResult = new CodeParser().Parse(code);
            
            Assert.IsTrue(parseResult.HasModelsBaseClassName);
            Assert.AreEqual("Dang.Whatever", parseResult.ModelsBaseClassName);
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

            var parseResult = new CodeParser().Parse(code);

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
                BaseTypeId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
            };

            var types = new[] { type1 };

            var code1 = new Dictionary<string, string>
            {
                {"assembly", @"
using Zbu.ModelsBuilder;
"}
            };

            var code2 = new Dictionary<string, string>
            {
                {"assembly", @"
using Zbu.ModelsBuilder;
[assembly:ModelsUsing(""Foo.Bar.Nil"")]
"}
            };

            var parseResult = new CodeParser().Parse(code1);
            var builder = new TextBuilder(types, parseResult);
            var count = builder.Using.Count;

            parseResult = new CodeParser().Parse(code2);
            builder = new TextBuilder(types, parseResult);

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

            var parseResult = new CodeParser().Parse(code);
            var builder = new TextBuilder(types, parseResult);
            var btypes = builder.TypeModels;

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
                BaseTypeId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
            };

            var types = new[] { type1 };

            var code1 = new Dictionary<string, string>
            {
                {"assembly", @"
using Zbu.ModelsBuilder;
public partial class Type1
{}
"}
            };

            Assert.IsFalse(new CodeParser().Parse(code1).HasContentBase("Type1"));

            var code2 = new Dictionary<string, string>
            {
                {"assembly", @"
using Zbu.ModelsBuilder;
public partial class Type1 : IHasXmlNode
{}
"}
            };

            // assumes base is IHasXmlNode (cannot be verified...)
            Assert.IsTrue(new CodeParser().Parse(code2).HasContentBase("Type1"));

            var code3 = new Dictionary<string, string>
            {
                {"assembly", @"
using Zbu.ModelsBuilder;
using System.Xml;
public partial class Type1 : IHasXmlNode
{}
"}
            };

            // figures out that IHasXmlNode is an interface, not base
            // because of using + reference
            var asms = new[] {typeof(System.Xml.IHasXmlNode).Assembly};
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
                BaseTypeId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
            };

            var type2 = new TypeModel
            {
                Id = 2,
                Alias = "type2",
                ClrName = "Type2",
                BaseTypeId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
            };

            var type3 = new TypeModel
            {
                Id = 3,
                Alias = "ttype3",
                ClrName = "Ttype3",
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

            var parseResult = new CodeParser().Parse(code);
            var builder = new TextBuilder(types, parseResult);
            var btypes = builder.TypeModels;

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
                BaseTypeId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
            };

            var type2 = new TypeModel
            {
                Id = 2,
                Alias = "type2",
                ClrName = "Type2",
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

            var parseResult = new CodeParser().Parse(code);
            var builder = new TextBuilder(types, parseResult);
            var btypes = builder.TypeModels;

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
                BaseTypeId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
            };

            var type2 = new TypeModel
            {
                Id = 2,
                Alias = "type2",
                ClrName = "Type2",
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

            var parseResult = new CodeParser().Parse(code);
            var builder = new TextBuilder(types, parseResult);
            var btypes = builder.TypeModels;

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
                BaseTypeId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
            };

            var type2 = new TypeModel
            {
                Id = 2,
                Alias = "type2",
                ClrName = "Type2",
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

            var parseResult = new CodeParser().Parse(code);
            var builder = new TextBuilder(types, parseResult);
            var btypes = builder.TypeModels;

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
                BaseTypeId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
            };

            var type2 = new TypeModel
            {
                Id = 2,
                Alias = "type2",
                ClrName = "Type2",
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
    [ImplementContentType(""type1"")]
    public partial class Renamed1
    {}
}
"}
            };

            var parseResult = new CodeParser().Parse(code);
            var builder = new TextBuilder(types, parseResult);
            var btypes = builder.TypeModels;

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
                BaseTypeId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
            };
            var type2 = new TypeModel
            {
                Id = 2,
                Alias = "type2",
                ClrName = "Type2",
                BaseTypeId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
            };
            var type3 = new TypeModel
            {
                Id = 3,
                Alias = "type3",
                ClrName = "Type3",
                BaseTypeId = 1,
                BaseType = type1,
                ItemType = TypeModel.ItemTypes.Content,
            };
            var types = new[] { type1, type2, type3 };

            var code = new Dictionary<string, string>
            {
                {"assembly", @"
using Zbu.ModelsBuilder;
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
            var builder = new TextBuilder(types, parseResult);
            var btypes = builder.TypeModels;

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
            builder.Generate(sb, btype3);
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
                BaseTypeId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
            };
            type1.Properties.Add(new PropertyModel
            {
                Alias = "prop1",
                ClrName = "Prop1",
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

            var parseResult = new CodeParser().Parse(code);
            var builder = new TextBuilder(types, parseResult);
            var btypes = builder.TypeModels;

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
                BaseTypeId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
            };
            type1.Properties.Add(new PropertyModel
            {
                Alias = "prop1",
                ClrName = "Prop1",
                ClrType = typeof(string),
            });
            type1.Properties.Add(new PropertyModel
            {
                Alias = "prop2",
                ClrName = "Prop2",
                ClrType = typeof(string),
            });
            type1.Properties.Add(new PropertyModel
            {
                Alias = "pprop3",
                ClrName = "Pprop3",
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

            var parseResult = new CodeParser().Parse(code);
            var builder = new TextBuilder(types, parseResult);
            var btypes = builder.TypeModels;

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
                BaseTypeId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
            };
            type1.Properties.Add(new PropertyModel
            {
                Alias = "prop1",
                ClrName = "Prop1",
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

            var parseResult = new CodeParser().Parse(code);
            var builder = new TextBuilder(types, parseResult);
            var btypes = builder.TypeModels;

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
                BaseTypeId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
            };
            type1.Properties.Add(new PropertyModel
            {
                Alias = "prop1",
                ClrName = "Prop1",
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

            var parseResult = new CodeParser().Parse(code);
            var builder = new TextBuilder(types, parseResult);
            var btypes = builder.TypeModels;

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
                BaseTypeId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
            };
            type1.Properties.Add(new PropertyModel
            {
                Alias = "prop1",
                ClrName = "Prop1",
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

            var parseResult = new CodeParser().Parse(code);
            var builder = new TextBuilder(types, parseResult);
            var btypes = builder.TypeModels;

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
                BaseTypeId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
            };
            type1.Properties.Add(new PropertyModel
            {
                Alias = "prop1",
                ClrName = "Prop1",
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
        [ImplementPropertyType(""prop1"")]
        public string Renamed1 { get { return """"; } }
    }
}
"}
            };

            var parseResult = new CodeParser().Parse(code);
            var builder = new TextBuilder(types, parseResult);
            var btypes = builder.TypeModels;

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
                BaseTypeId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
            };
            type1.Properties.Add(new PropertyModel
            {
                Alias = "prop1",
                ClrName = "Prop1",
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
        [ImplementPropertyType(""prop1"")]
        public string Renamed1 { get { return """"; } }
    }

    public partial class Type1 : Type2
    {
    }
}
"}
            };

            var parseResult = new CodeParser().Parse(code);
            var builder = new TextBuilder(types, parseResult);
            var btypes = builder.TypeModels;

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
                BaseTypeId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
            };
            type1.Properties.Add(new PropertyModel
            {
                Alias = "prop1",
                ClrName = "Prop1",
                ClrType = typeof(string),
            });
            var types = new[] { type1 };

            var code = new Dictionary<string, string>
            {
                {"assembly", @"
using Zbu.ModelsBuilder;
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

            var parseResult = new CodeParser().Parse(code);
            var builder = new TextBuilder(types, parseResult);
            var btypes = builder.TypeModels;

            Assert.AreEqual(1, btypes.Count);
            var btype1 = btypes[0];
            Assert.AreEqual("Type1", btype1.ClrName);

            var sb = new StringBuilder();
            builder.Generate(sb, btype1);
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
                BaseTypeId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
            };
            type1.Properties.Add(new PropertyModel
            {
                Alias = "prop1a",
                ClrName = "Prop1a",
                ClrType = typeof(string),
            });
            type1.Properties.Add(new PropertyModel
            {
                Alias = "prop1b",
                ClrName = "Prop1b",
                ClrType = typeof(string),
            });
            type1.Properties.Add(new PropertyModel
            {
                Alias = "prop1c",
                ClrName = "Prop1c",
                ClrType = typeof(string),
            });
            var type2 = new TypeModel
            {
                Id = 1,
                Alias = "type2",
                ClrName = "Type2",
                BaseTypeId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
            };
            type2.Properties.Add(new PropertyModel
            {
                Alias = "prop2",
                ClrName = "Prop2",
                ClrType = typeof(string),
            });
            var types = new[] { type1, type2 };

            type2.MixinTypes.Add(type1);
            type1.IsMixin = true;

            var code = new Dictionary<string, string>
            {
                {"assembly", @"
using Zbu.ModelsBuilder;
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

            var parseResult = new CodeParser().Parse(code);
            var builder = new TextBuilder(types, parseResult);
            var btypes = builder.TypeModels;

            Assert.AreEqual(2, btypes.Count);
            var btype1 = btypes[0];
            Assert.AreEqual("Type1", btype1.ClrName);
            var btype2 = btypes[1];
            Assert.AreEqual("Type2", btype2.ClrName);

            var sb = new StringBuilder();
            builder.Generate(sb, btype1);
            var gen = sb.ToString();
            Console.WriteLine(gen);

            // contains
            Assert.Greater(gen.IndexOf("string Prop1x"), 0);
            // does not contain
            Assert.Greater(0, gen.IndexOf("string Prop1a"));
            Assert.Greater(0, gen.IndexOf("string Prop1b"));
            Assert.Greater(0, gen.IndexOf("string Prop1c"));

            sb.Clear();
            builder.Generate(sb, btype2);
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
        public void GenerateSimpleType()
        {
            // Umbraco returns nice, pascal-cased names

            var type1 = new TypeModel
            {
                Id = 1,
                Alias = "type1",
                ClrName = "Type1",
                BaseTypeId = 0,
                BaseType = null,
                ItemType = TypeModel.ItemTypes.Content,
            };
            type1.Properties.Add(new PropertyModel
            {
                Alias = "prop1",
                ClrName = "Prop1",
                ClrType = typeof(string),
            });

            var types = new[] { type1 };

            var code = new Dictionary<string, string>
            {
            };

            var parseResult = new CodeParser().Parse(code);
            var builder = new TextBuilder(types, parseResult);
            var btypes = builder.TypeModels;

            var sb = new StringBuilder();
            builder.Generate(sb, builder.GetModelsToGenerate().First());
            var gen = sb.ToString();

            var version = typeof (Builder).Assembly.GetName().Version;
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

namespace Umbraco.Web.PublishedContentModels
{
	[PublishedContentModel(""type1"")]
	public partial class Type1 : PublishedContentModel
	{
#pragma warning disable 0109 // new is redundant
		public new const string ModelTypeAlias = ""type1"";
		public new const PublishedItemType ModelItemType = PublishedItemType.Content;
#pragma warning restore 0109

		public Type1(IPublishedContent content)
			: base(content)
		{ }

#pragma warning disable 0109 // new is redundant
		public new static PublishedContentType GetModelContentType()
		{
			return PublishedContentType.Get(ModelItemType, ModelTypeAlias);
		}
#pragma warning restore 0109

		public static PublishedPropertyType GetModelPropertyType<TValue>(Expression<Func<Type1, TValue>> selector)
		{
			return PublishedContentModelUtility.GetModelPropertyType(GetModelContentType(), selector);
		}

		[ImplementPropertyType(""prop1"")]
		public string Prop1
		{
			get { return this.GetPropertyValue<string>(""prop1""); }
		}
	}
}
";
            Console.WriteLine(gen);
            Assert.AreEqual(expected.Replace("\r", ""), gen);
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
