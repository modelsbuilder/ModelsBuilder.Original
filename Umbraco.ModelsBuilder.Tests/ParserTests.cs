using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;
using Umbraco.ModelsBuilder.Building;
using Umbraco.ModelsBuilder.Configuration;

namespace Umbraco.ModelsBuilder.Tests
{
    [TestFixture]
    public class ParserTests
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
                UmbracoConfigExtensions.ResetConfig();
                Config.Setup(new Config(languageVersion: LanguageVersion.CSharp5));

                var parseResult1 = new CodeParser().Parse(code, refs);
                Assert.Fail("Expected CompilerException.");
            }
            catch (CompilerException e)
            {
                Console.WriteLine(e.Message);
                Assert.IsTrue(e.Message.EndsWith("(at assembly:line 6)."));
            }

            UmbracoConfigExtensions.ResetConfig();
            Config.Setup(new Config(languageVersion: LanguageVersion.CSharp6));

            var parseResult2 = new CodeParser().Parse(code, refs);
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
                UmbracoConfigExtensions.ResetConfig();
                Config.Setup(new Config(languageVersion: LanguageVersion.CSharp6));

                var parseResult1 = new CodeParser().Parse(code, refs);
                Assert.Fail("Expected CompilerException.");
            }
            catch (CompilerException e)
            {
                Console.WriteLine(e.Message);
                Assert.IsTrue(e.Message.EndsWith("(at assembly:line 7)."));
            }

            UmbracoConfigExtensions.ResetConfig();
            Config.Setup(new Config(languageVersion: LanguageVersion.CSharp7));

            var parseResult2 = new CodeParser().Parse(code, refs);
        }
    }
}
