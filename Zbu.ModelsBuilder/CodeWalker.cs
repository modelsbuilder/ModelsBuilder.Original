using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Zbu.ModelsBuilder
{
    // read http://social.msdn.microsoft.com/Forums/vstudio/en-US/64ee86b8-0fd7-457d-8428-a0f238133476/can-roslyn-tell-me-if-a-member-of-a-symbol-is-visible-from-a-position-in-a-document?forum=roslyn
    // goes beyond syntax to figure out symbols, etc... must test...

    class CodeWalker : CSharpSyntaxWalker
    {
        //private string _propertyName;
        private string _attributeName;
        private readonly Stack<string> _classNames = new Stack<string>();

        public class CodeWalkerState
        {
            public readonly List<string> IgnoreContentTypes 
                = new List<string>();
            public readonly Dictionary<string, List<string>> IgnorePropertyTypes 
                = new Dictionary<string, List<string>>(StringComparer.InvariantCultureIgnoreCase);
            public readonly Dictionary<string, string> RenameContentTypes
                = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            public readonly Dictionary<string, Dictionary<string, string>> RenamePropertyTypes
                = new Dictionary<string, Dictionary<string, string>>(StringComparer.InvariantCultureIgnoreCase);
            public readonly List<string> OmitModelBases
                = new List<string>();
            public readonly Dictionary<string, string[]> BaseLists
                = new Dictionary<string, string[]>();

            public void IgnoreContentType(string contentTypeNameOrAlias)
            {
                if (string.IsNullOrWhiteSpace(contentTypeNameOrAlias)) return;
                IgnoreContentTypes.Add(contentTypeNameOrAlias.ToLowerInvariant());
            }

            public void RenameContentType(string contentTypeAlias, string contentTypeName)
            {
                if (string.IsNullOrWhiteSpace(contentTypeName)
                    || string.IsNullOrWhiteSpace(contentTypeAlias)) return;
                RenameContentTypes[contentTypeAlias] = contentTypeName;
            }

            public void IgnorePropertyType(string contentTypeName, string propertyTypeAlias)
            {
                if (string.IsNullOrWhiteSpace(contentTypeName)
                    || string.IsNullOrWhiteSpace(propertyTypeAlias)) return;
                List<string> ignores;
                if (!IgnorePropertyTypes.TryGetValue(contentTypeName, out ignores))
                    ignores = IgnorePropertyTypes[contentTypeName] = new List<string>();
                ignores.Add(propertyTypeAlias.ToLowerInvariant());
            }

            public void RenamePropertyType(string contentTypeName, string propertyTypeAlias, string propertyTypeName)
            {
                if (string.IsNullOrWhiteSpace(contentTypeName)
                    || string.IsNullOrWhiteSpace(propertyTypeAlias)
                    || string.IsNullOrWhiteSpace(propertyTypeName)) return;
                Dictionary<string, string> renames;
                if (!RenamePropertyTypes.TryGetValue(contentTypeName, out renames))
                    renames = RenamePropertyTypes[contentTypeName] = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
                renames[propertyTypeAlias] = propertyTypeName;
            }

            public void OmitModelBase(string contentTypeName)
            {
                OmitModelBases.Add(contentTypeName);
            }

            public void BaseList(string contentTypeName, string[] baseList)
            {
                if (string.IsNullOrWhiteSpace(contentTypeName)
                    || baseList.Length == 0) return;
                BaseLists[contentTypeName] = baseList;
            }
        }

        private readonly CodeWalkerState _state;

        public CodeWalker(CodeWalkerState state)
        {
            _state = state;
        }

        public override void VisitLiteralExpression(LiteralExpressionSyntax node)
        {
            if (_attributeName != null)
            {
                string className;
                string contentTypeAlias;
                string contentTypeName;
                string propertyTypeAlias;
                // FIXME - should we check against attributes full type name?
                switch (_attributeName)
                {
                    case "IgnoreContentType":
                        contentTypeAlias = node.Token.ValueText;
                        _state.IgnoreContentType(contentTypeAlias);
                        break;
                    case "IgnorePropertyType":
                        contentTypeName = _classNames.Peek();
                        propertyTypeAlias = node.Token.ValueText;
                        _state.IgnorePropertyType(contentTypeName, propertyTypeAlias);
                        break;
                    case "PublishedContentModel":
                        contentTypeName = _classNames.Peek();
                        contentTypeAlias = node.Token.ValueText;
                        _state.RenameContentType(contentTypeAlias, contentTypeName);
                        break;
                    case "OmitModelBase":
                        contentTypeName = _classNames.Peek();
                        _state.OmitModelBase(contentTypeName);
                        break;
                }
            }
            base.VisitLiteralExpression(node);
        }

        public override void VisitAttribute(AttributeSyntax node)
        {
            // assuming we don't nest attributes
            _attributeName = node.Name.ToString();

            if (_attributeName == "RenamePropertyType")
            {
                var args = node.ArgumentList.Arguments;
                var arg1 = args[0];
                var arg2 = args[1];
                // FIXME - what about .NameColon and NameEquals... could the args be swapped?
                var propertyTypeAlias = (arg1.Expression as LiteralExpressionSyntax).Token.ValueText;
                var propertyTypeName = (arg2.Expression as LiteralExpressionSyntax).Token.ValueText;
                var contentTypeName = _classNames.Peek();
                _state.RenamePropertyType(contentTypeName, propertyTypeAlias, propertyTypeName);
            }

            base.VisitAttribute(node);
            _attributeName = null;
        }

        public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
        {
            string className;
            _classNames.Push(className = node.Identifier.ValueText);

            // fixme - disabled, we want to get rid of it - just build a version that's backward compatible
            //if (node.BaseList != null)
            //    _state.BaseList(className, node.BaseList.Types.Select(x =>
            //    {
            //        var identifier = x as IdentifierNameSyntax;
            //        if (identifier == null)
            //            throw new Exception(string.Format("Panic: unsupported {0} in BaseList.", x.GetType()));
            //        return identifier.Identifier.ValueText;
            //    }).ToArray());

            base.VisitInterfaceDeclaration(node);
            _classNames.Pop();
        }

        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            string className;
            _classNames.Push(className = node.Identifier.ValueText);

            // fixme - disabled, we want to get rid of it - just build a version that's backward compatible
            //if (node.BaseList != null)
            //    _state.BaseList(className, node.BaseList.Types.Select(x =>
            //    {
            //        // fixme - should use .Kind instead?
            //        var identifier = x as NameSyntax;
            //        if (identifier == null)
            //            throw new Exception(string.Format("Panic: unsupported {0} in BaseList.", x.GetType()));
            //        if (identifier is SimpleNameSyntax) return ((SimpleNameSyntax) identifier).Identifier.ValueText;
            //        //if (identifier is IdentifierNameSyntax) return ((IdentifierNameSyntax) identifier).Identifier.ValueText;
            //        if (identifier is QualifiedNameSyntax) return ((QualifiedNameSyntax) identifier).ToString(); // FIXME
            //        return identifier.ToString(); // fixme
            //    }).ToArray());

            base.VisitClassDeclaration(node);
            _classNames.Pop();
        }

        //public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        //{
        //    _propertyName = node.Identifier.ToString();
        //    base.VisitPropertyDeclaration(node);
        //    _propertyName = null;
        //}
    }
}
