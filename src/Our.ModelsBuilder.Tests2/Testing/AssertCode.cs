using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis;
using NUnit.Framework;
using Our.ModelsBuilder.Building;

namespace Our.ModelsBuilder.Tests.Testing
{
    public static class AssertCode
    {
        private static ITypeSymbol GetType(SemanticModel semanticModel, string fqn)
        {
            var pos = fqn.LastIndexOf('.');
            var typeName = fqn.Substring(pos + 1);
            var declaringNamespace = fqn.Substring(0, pos);
            return semanticModel.LookupTypeSymbol(declaringNamespace, typeName);
        }

        private static ImmutableArray<ISymbol> GetMembers(SemanticModel semanticModel, string fqn)
        {
            var pos = fqn.LastIndexOf('.');
            var memberName = fqn.Substring(pos + 1);
            fqn = fqn.Substring(0, pos);
            pos = fqn.LastIndexOf('.');
            var declaringType = fqn.Substring(pos + 1);
            var declaringNamespace = fqn.Substring(0, pos);
            return semanticModel.LookupTypeSymbolMembers(declaringNamespace, declaringType, memberName);
        }

        public static void HasNamespace(SemanticModel semanticModel, string fqn)
        {
            var symbol = semanticModel.LookupNamespaceSymbol(fqn);
            Assert.IsNotNull(symbol);
        }

        public static void HasNotNamespace(SemanticModel semanticModel, string fqn)
        {
            var symbol = semanticModel.LookupNamespaceSymbol(fqn);
            Assert.IsNull(symbol);
        }

        public static void HasType(SemanticModel semanticModel, string fqn)
            => HasType(semanticModel, fqn, out _);

        public static void HasType(SemanticModel semanticModel, string fqn, out ITypeSymbol symbol)
        {
            symbol = GetType(semanticModel, fqn);
            Assert.IsNotNull(symbol);
        }

        public static void HasNotType(SemanticModel semanticModel, string fqn)
        {
            var symbol = GetType(semanticModel, fqn);
            Assert.IsNull(symbol);
        }

        public static void HasProperty(SemanticModel semanticModel, string fqn, bool isReadonly, bool isVirtual)
            => HasProperty(semanticModel, fqn, isReadonly, isVirtual, out _);

        public static void HasProperty(SemanticModel semanticModel, string fqn, bool isReadonly, bool isVirtual, out IPropertySymbol symbol)
        {
            var symbols = GetMembers(semanticModel, fqn);
            Assert.AreEqual(1, symbols.Length);
            AssertCode.IsProperty(symbols[0], isReadonly, isVirtual);
            symbol = (IPropertySymbol) symbols[0];
        }

        public static void HasMethod(SemanticModel semanticModel, string fqn, bool isStatic, bool isExtensionMethod, bool isVirtual, int paramCount = -1)
        {
            var symbols = GetMembers(semanticModel, fqn);
            Assert.AreEqual(1, symbols.Length);
            AssertCode.IsMethod(symbols[0], isStatic, isExtensionMethod, isVirtual, paramCount);
        }

        public static void HasNotMember(SemanticModel semanticModel, string fqn)
        {
            var symbols = GetMembers(semanticModel, fqn);
            Assert.AreEqual(0, symbols.Length);
        }

        public static void IsProperty(ISymbol symbol, bool isReadonly, bool isVirtual)
        {
            if (symbol is IPropertySymbol propertySymbol)
            {
                Assert.IsNotNull(propertySymbol.GetMethod);
                Assert.AreEqual(isVirtual, propertySymbol.GetMethod.IsVirtual);

                if (isReadonly)
                    Assert.IsNull(propertySymbol.SetMethod);
                else
                    Assert.IsNotNull(propertySymbol.SetMethod);
            }
            else Assert.Fail();
        }

        public static void IsMethod(ISymbol symbol, bool isStatic, bool isExtensionMethod, bool isVirtual, int paramCount = -1)
        {
            if (symbol is IMethodSymbol methodSymbol)
            {
                Assert.AreEqual(isStatic, methodSymbol.IsStatic);
                Assert.AreEqual(isExtensionMethod, methodSymbol.IsExtensionMethod);
                Assert.AreEqual(isVirtual, methodSymbol.IsVirtual);

                if (paramCount >= 0)
                    Assert.AreEqual(paramCount, methodSymbol.Parameters.Length);
            }
            else Assert.Fail();
        }

        public static void Compiles(IDictionary<string, string> sources, List<PortableExecutableReference> references = null, bool writeHiddenDiagnostics = false)
        {
            var compilation = TestUtilities.Compile(sources, references);
            Compiles(compilation.GetDiagnostics(), writeHiddenDiagnostics);
        }

        public static void Compiles(IDictionary<string, string> sources, out Compilation compilation, List<PortableExecutableReference> references = null, bool writeHiddenDiagnostics = false)
        {
            compilation = TestUtilities.Compile(sources, references);
            Compiles(compilation.GetDiagnostics(), writeHiddenDiagnostics);
        }

        public static void Compiles(CodeModel model, IDictionary<string, string> sources = null, List<PortableExecutableReference> references = null, bool writeHiddenDiagnostics = false)
        {
            var compilation = TestUtilities.Compile(model, sources, references);
            Compiles(compilation.GetDiagnostics(), writeHiddenDiagnostics);
        }

        public static void Compiles(CodeModel model, out Compilation compilation, IDictionary<string, string> sources = null, List<PortableExecutableReference> references = null, bool writeHiddenDiagnostics = false)
        {
            compilation = TestUtilities.Compile(model, sources, references);
            Compiles(compilation.GetDiagnostics(), writeHiddenDiagnostics);
        }

        private static void Compiles(ImmutableArray<Diagnostic> diagnostics, bool writeHiddenDiagnostics = false)
        {
            // report
            foreach (var diagnostic in diagnostics.Where(x => writeHiddenDiagnostics || x.Severity != DiagnosticSeverity.Hidden))
                Console.WriteLine(diagnostic);

            // assert
            Assert.IsEmpty(diagnostics.Where(x => x.Severity != DiagnosticSeverity.Hidden));
        }

        public static bool IsInvalidTypeNameOrIdentifier(string value, bool isTypeName)
            => !IsValidTypeNameOrIdentifier(value, isTypeName);

        // from reference code
        public static bool IsValidTypeNameOrIdentifier(string value, bool isTypeName)
        {
            var nextMustBeStartChar = true;

            if (value.Length == 0)
                return false;

            // each char must be Lu, Ll, Lt, Lm, Lo, Nd, Mn, Mc, Pc
            // 
            for (var i = 0; i < value.Length; i++)
            {
                var ch = value[i];
                var uc = char.GetUnicodeCategory(ch);
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