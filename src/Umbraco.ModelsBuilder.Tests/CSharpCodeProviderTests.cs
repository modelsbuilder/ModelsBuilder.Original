using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Umbraco.ModelsBuilder.Tests
{
    [TestFixture]
    public class CSharpCodeProviderTests
    {
        private string ToValidIdentifier(string value)
        {
            // these don't do what we expect them to do
            //return Microsoft.CSharp.CSharpCodeProvider.CreateProvider("C#").CreateValidIdentifier(value);
            //return System.CodeDom.Compiler.CodeDomProvider.CreateProvider("C#").CreateValidIdentifier(value);

            // but IsValidIdentifier *does* fully validate
            // so... re-use their code
            // https://referencesource.microsoft.com/#System/compmod/system/codedom/compiler/CodeGenerator.cs,b8ef446f3714a2d6

            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Null or empty value cannot be a valid identifier.", nameof(value));

            var chars = value.ToCharArray();
            var leadingDecimal = false;

            for (var i = 0; i < chars.Length; i++)
            {
                char ch = chars[i];
                UnicodeCategory uc = char.GetUnicodeCategory(ch);

                switch (uc)
                {
                    case UnicodeCategory.UppercaseLetter:        // Lu
                    case UnicodeCategory.LowercaseLetter:        // Ll
                    case UnicodeCategory.TitlecaseLetter:        // Lt
                    case UnicodeCategory.ModifierLetter:         // Lm
                    case UnicodeCategory.LetterNumber:           // Lm
                    case UnicodeCategory.OtherLetter:            // Lo
                        break;

                    case UnicodeCategory.DecimalDigitNumber:     // Nd
                        if (i == 0) leadingDecimal = true;
                        break;

                    default:
                        chars[i] = '_';
                        break;
                }
            }

            value = new string(chars);

            if (leadingDecimal)
                value = '_' + value;

            return value;
        }

        [TestCase(true, "Foo")]
        [TestCase(true, "Foo_Bar")]
        [TestCase(true, "_1Foo")]
        [TestCase(false, "Foo Bar")]
        [TestCase(false, "1Foo")]
        public void IsValidIdentifierTests(bool expected, string value)
        {
            Assert.AreEqual(expected, IsValidTypeNameOrIdentifier(value, true));
        }

        [TestCase("Foo", "Foo")]
        [TestCase("foo", "foo")]
        [TestCase("FooBar", "FooBar")]
        [TestCase("Foo_Bar", "Foo Bar")]
        [TestCase("_1Foo", "1Foo")]
        [TestCase("Foo___", "Foo[!!")]
        public void ValidIdentifierTests(string expected, string value)
        {
            var valid = ToValidIdentifier(value);
            Assert.AreEqual(expected, valid);
            Assert.IsTrue(IsValidTypeNameOrIdentifier(valid, true));
        }

        // from reference code
        private static bool IsValidTypeNameOrIdentifier(string value, bool isTypeName)
        {
            bool nextMustBeStartChar = true;

            if (value.Length == 0)
                return false;

            // each char must be Lu, Ll, Lt, Lm, Lo, Nd, Mn, Mc, Pc
            // 
            for (int i = 0; i < value.Length; i++)
            {
                char ch = value[i];
                UnicodeCategory uc = char.GetUnicodeCategory(ch);
                switch (uc)
                {
                    case UnicodeCategory.UppercaseLetter:        // Lu
                    case UnicodeCategory.LowercaseLetter:        // Ll
                    case UnicodeCategory.TitlecaseLetter:        // Lt
                    case UnicodeCategory.ModifierLetter:         // Lm
                    case UnicodeCategory.LetterNumber:           // Lm
                    case UnicodeCategory.OtherLetter:            // Lo
                        nextMustBeStartChar = false;
                        break;

                    case UnicodeCategory.NonSpacingMark:         // Mn
                    case UnicodeCategory.SpacingCombiningMark:   // Mc
                    case UnicodeCategory.ConnectorPunctuation:   // Pc
                    case UnicodeCategory.DecimalDigitNumber:     // Nd
                        // Underscore is a valid starting character, even though it is a ConnectorPunctuation.
                        if (nextMustBeStartChar && ch != '_')
                            return false;

                        nextMustBeStartChar = false;
                        break;
                    default:
                        // We only check the special Type chars for type names. 
                        if (isTypeName && IsSpecialTypeChar(ch, ref nextMustBeStartChar))
                        {
                            break;
                        }

                        return false;
                }
            }

            return true;
        }

        private static bool IsSpecialTypeChar(char ch, ref bool nextMustBeStartChar)
        {
            switch (ch)
            {
                case ':':
                case '.':
                case '$':
                case '+':
                case '<':
                case '>':
                case '-':
                case '[':
                case ']':
                case ',':
                case '&':
                case '*':
                    nextMustBeStartChar = true;
                    return true;

                case '`':
                    return true;
            }
            return false;
        }
    }
}
