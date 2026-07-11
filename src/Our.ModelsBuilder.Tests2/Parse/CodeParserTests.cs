using System;
using System.Collections.Generic;
using System.Xml;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;
using Our.ModelsBuilder.Building;
using Our.ModelsBuilder.Options;
using Our.ModelsBuilder.Tests.Testing;
using Umbraco.Core.Models.PublishedContent;

namespace Our.ModelsBuilder.Tests.Parse
{
    [TestFixture]
    public class CodeParserTests
    {
        [Test]
        public void ExpressionBodiedPropertiesRequireCSharp6()
        {
            var code = new Dictionary<string, string>
            {
                { "assembly", @"
namespace Foo
{
    public partial class MyModel
    {
        public string MyProperty => """";
    }
}
" }
            };

            var refs = new[]
            {
                MetadataReference.CreateFromFile(typeof (string).Assembly.Location),
                MetadataReference.CreateFromFile(typeof (ReferencedAssemblies).Assembly.Location)
            };

            // Umbraco.ModelsBuilder.Building.CompilerException : Feature 'expression-bodied property' is not available in C# 5.  Please use language version 6 or greater.
            try
            {
                new CodeParser(LanguageVersion.CSharp5).Parse(code, new CodeOptionsBuilder(), refs);
                Assert.Fail("Expected CompilerException.");
            }
            catch (CompilerException e)
            {
                Console.WriteLine(e.Message);
                Assert.IsTrue(e.Message.EndsWith("(at assembly:line 6)."));
            }

            new CodeParser(LanguageVersion.CSharp6).Parse(code, new CodeOptionsBuilder(), refs);
        }

        [Test]
        public void LambdaPropertiesRequireCSharp7()
        {
            var code = new Dictionary<string, string>
            {
                { "assembly", @"
namespace Foo
{
    public partial class MyModel
    {
        private string _value;
        public string MyProperty { get => _value; set => _value = value; }
    }
}
" }
            };

            var refs = new[]
            {
                MetadataReference.CreateFromFile(typeof (string).Assembly.Location),
                MetadataReference.CreateFromFile(typeof (ReferencedAssemblies).Assembly.Location)
            };

            // Umbraco.ModelsBuilder.Building.CompilerException : { or; expected(at assembly: line 7).
            try
            {
                new CodeParser(LanguageVersion.CSharp6).Parse(code, new CodeOptionsBuilder(), refs);
                Assert.Fail("Expected CompilerException.");
            }
            catch (CompilerException e)
            {
                Console.WriteLine(e.Message);
                Assert.IsTrue(e.Message.EndsWith("(at assembly:line 7)."));
            }

            new CodeParser(LanguageVersion.CSharp7).Parse(code, new CodeOptionsBuilder(), refs);
        }

        [Test]
        public void ParsePartialConstructor()
        {
            var sources = new Dictionary<string, string>
            {
                { "assembly", @"
using Our.ModelsBuilder;
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

            var refs = TestUtilities.CreateDefaultReferences().AddReference<IPublishedContent>();

            var optionsBuilder = new CodeOptionsBuilder();
            new CodeParser().Parse(sources, optionsBuilder, refs);
            var options = optionsBuilder.CodeOptions;

            Assert.IsTrue(options.ContentTypes.OmitContentTypeConstructor("Type1"));
        }

        [Test]
        public void Temp_BaseClasses()
        {
            var sources = new Dictionary<string, string>
            {
                { "assembly", @"
using Umbraco.Core.Models.PublishedContent;

namespace SomeNamespace
{
    public class UnknownBaseClass : Bar
    { }

    public class KnownBaseClass : PublishedContentModel
    { 
        public KnownBaseClass(IPublishedContent content)
            : base(content)
        { }
    }

    public class NoBaseClass
    { }
}
" }
            };

            var optionsBuilder = new CodeOptionsBuilder();
            new CodeParser { WriteDiagnostics = true }.Parse(sources, optionsBuilder, TestUtilities.CreateDefaultReferences().AddReference<PublishedContentModel>());
            var options = optionsBuilder.CodeOptions;

            // an unknown base class
            // beware! models *may* be known if the site already runs with models
            Assert.IsTrue(options.ContentTypes.OmitContentTypeBaseClass("UnknownBaseClass"));
            Assert.AreEqual("Bar", options.ContentTypes.ContentTypeBaseClass("UnknownBaseClass"));

            // a known base class
            Assert.IsTrue(options.ContentTypes.OmitContentTypeBaseClass("KnownBaseClass"));
            Assert.AreEqual("PublishedContentModel", options.ContentTypes.ContentTypeBaseClass("KnownBaseClass"));

            // no base class
            Assert.IsFalse(options.ContentTypes.OmitContentTypeBaseClass("NoBaseClass"));
        }

        [Test]
        public void ReferencedAssemblies()
        {
            var type1 = new ContentTypeModel
            {
                Id = 1,
                Alias = "type1",
                ParentId = 0,
                BaseContentType = null,
                Kind = ContentTypeKind.Content,
            };

            var code1 = new Dictionary<string, string>
            {
                {"assembly", @"
using Our.ModelsBuilder;
public partial class Type1
{}
"}
            };

            // no base class is parsed = don't omit
            var optionsBuilder = new CodeOptionsBuilder();
            new CodeParser().Parse(code1, optionsBuilder);
            var options = optionsBuilder.CodeOptions;

            Assert.IsFalse(options.ContentTypes.OmitContentTypeBaseClass("Type1"));

            var code2 = new Dictionary<string, string>
            {
                {"assembly", @"
using Our.ModelsBuilder;
public partial class Type1 : IHasXmlNode
{}
"}
            };

            // parses IHasXmlNode, cannot verify it's an interface, so assume it's a base class = omit
            optionsBuilder = new CodeOptionsBuilder();
            new CodeParser().Parse(code2, optionsBuilder);
            options = optionsBuilder.CodeOptions;

            Assert.IsTrue(options.ContentTypes.OmitContentTypeBaseClass("Type1"));

            var code3 = new Dictionary<string, string>
            {
                {"assembly", @"
using Our.ModelsBuilder;
using System.Xml;
public partial class Type1 : IHasXmlNode
{}
"}
            };

            // figures out that IHasXmlNode is an interface, not base
            // because of using + reference
            optionsBuilder = new CodeOptionsBuilder();
            new CodeParser().Parse(code3, optionsBuilder, TestUtilities.CreateDefaultReferences().AddReference<IHasXmlNode>());
            options = optionsBuilder.CodeOptions;

            Assert.IsFalse(options.ContentTypes.OmitContentTypeBaseClass("Type1"));
        }

    }
}
