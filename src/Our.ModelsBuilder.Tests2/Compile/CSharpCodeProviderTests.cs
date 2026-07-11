using NUnit.Framework;
using Our.ModelsBuilder.Building;
using Our.ModelsBuilder.Tests.Testing;

namespace Our.ModelsBuilder.Tests.Compile
{
    [TestFixture]
    public class CSharpCodeProviderTests
    {
        [TestCase(true, "Foo")]
        [TestCase(true, "Foo_Bar")]
        [TestCase(true, "_1Foo")]
        [TestCase(false, "Foo Bar")]
        [TestCase(false, "1Foo")]
        public void IsValidIdentifierTests(bool expected, string value)
        {
            if (expected)
                AssertCode.IsValidTypeNameOrIdentifier(value, true);
            else
                AssertCode.IsInvalidTypeNameOrIdentifier(value, true);
        }

        [TestCase("Foo", "Foo")]
        [TestCase("foo", "foo")]
        [TestCase("FooBar", "FooBar")]
        [TestCase("Foo_Bar", "Foo Bar")]
        [TestCase("_1Foo", "1Foo")]
        [TestCase("Foo___", "Foo[!!")]
        public void ValidIdentifierTests(string expected, string value)
        {
            var valid = Compiler.CreateValidIdentifier(value);
            Assert.AreEqual(expected, valid);
            AssertCode.IsValidTypeNameOrIdentifier(valid, true);
        }
    }
}