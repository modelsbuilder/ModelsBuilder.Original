using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;
using Umbraco.Core.Composing;
using ZpqrtBnk.ModelsBuilder.Building;
using ZpqrtBnk.ModelsBuilder.Configuration;

namespace ZpqrtBnk.ModelsBuilder.Tests
{
    [TestFixture]
    public class ParserTests
    {
        [SetUp]
        public void Setup()
        {
            Current.Reset();
            Current.UnlockConfigs();
            Current.Configs.Add(() => new Config());
        }

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
                Current.Configs.Add(() => new Config(languageVersion: LanguageVersion.CSharp5));

                var transform1 = new CodeParser().Parse(code, refs);
                Assert.Fail("Expected CompilerException.");
            }
            catch (CompilerException e)
            {
                Console.WriteLine(e.Message);
                Assert.IsTrue(e.Message.EndsWith("(at assembly:line 6)."));
            }

            Current.Configs.Add(() => new Config(languageVersion: LanguageVersion.CSharp6));

            var transform2 = new CodeParser().Parse(code, refs);
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
                Current.Configs.Add(() => new Config(languageVersion: LanguageVersion.CSharp6));

                var transform1 = new CodeParser().Parse(code, refs);
                Assert.Fail("Expected CompilerException.");
            }
            catch (CompilerException e)
            {
                Console.WriteLine(e.Message);
                Assert.IsTrue(e.Message.EndsWith("(at assembly:line 7)."));
            }

            Current.Configs.Add(() => new Config(languageVersion: LanguageVersion.CSharp7));

            var transform2 = new CodeParser().Parse(code, refs);
        }

        [Test]
        public void Parse_ModelsBuilderConfigureAttribute()
        {
            var code = new Dictionary<string, string>
            {
                { "assembly", @"
using ZpqrtBnk.ModelsBuilder;
[assembly:ModelsBuilderConfigure(Namespace=""foo"")]
" }
            };

            var refs = new[]
            {
                MetadataReference.CreateFromFile(typeof (string).Assembly.Location),
                MetadataReference.CreateFromFile(typeof (ReferencedAssemblies).Assembly.Location)
            };

            var transform = new CodeParser().Parse(code, refs);
            Assert.AreEqual("foo", transform.ModelsNamespace);
        }
    }
}
