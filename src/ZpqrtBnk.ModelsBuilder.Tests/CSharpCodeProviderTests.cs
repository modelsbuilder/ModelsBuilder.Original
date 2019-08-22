using NUnit.Framework;
using System.Globalization;

namespace ZpqrtBnk.ModelsBuilder.Tests
{
    [TestFixture]
    public class CSharpCodeProviderTests
    {
        private string ToValidIdentifier(string value) => Building.Compiler.CreateValidIdentifier(value);

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