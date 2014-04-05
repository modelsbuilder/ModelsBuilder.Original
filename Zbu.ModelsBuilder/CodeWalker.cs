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
    class CodeWalker : CSharpSyntaxWalker
    {
        private string _propertyName;
        private string _attributeName;
        private readonly Stack<string> _classNames = new Stack<string>();

        private Action<string> _onIgnoreContentType;
        private Action<string, string> _onIgnorePropertyType;
        private Action<string, string, string> _onRenamePropertyType;
        private Action<string, string> _onRenameContentType;
        private Action<string, string> _onDefineModelBaseClass;

        public void Visit(SyntaxNode node,
            Action<string> onIgnoreContentType,
            Action<string, string> onIgnorePropertyType,
            Action<string, string, string> onRenamePropertyType,
            Action<string, string> onRenameContentType,
            Action<string, string> onDefineModelBaseClass)
        {
            _onIgnoreContentType = onIgnoreContentType;
            _onIgnorePropertyType = onIgnorePropertyType;
            _onRenamePropertyType = onRenamePropertyType;
            _onRenameContentType = onRenameContentType;
            _onDefineModelBaseClass = onDefineModelBaseClass;

            base.Visit(node);
        }

        public override void VisitLiteralExpression(LiteralExpressionSyntax node)
        {
            if (_attributeName != null)
            {
                string className;
                switch (_attributeName)
                {
                        // fixme - attributes full type name?
                    case "IgnoreContentType":
                        _onIgnoreContentType(node.Token.ValueText);
                        //Console.WriteLine("Ignore ContentType {0}", node.Token.ValueText);
                        break;
                    case "IgnorePropertyType":
                        className = _classNames.Peek();
                        //Console.WriteLine("Ignore PropertyType {0}.{1}", className, node.Token.ValueText);
                        _onIgnorePropertyType(className, node.Token.ValueText);
                        break;
                    case "PublishedContentModel":
                        className = _classNames.Peek();
                        // fixme should know if it's an interface AND then remove the IISomething?!
                        // fixme or maybe we should not rename on interfaces? name must be consistent with class?
                        //Console.WriteLine("Name {0} for ContentType {1}", className, node.Token.ValueText);
                        _onRenameContentType(className, node.Token.ValueText);
                        break;
                    case "ModelBaseClass":
                        className = _classNames.Peek();
                        _onDefineModelBaseClass(className, node.Token.ValueText);
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
                // fixme - what about .NameColon and NameEquals... could the args be swapped?
                var alias = (arg1.Expression as LiteralExpressionSyntax).Token.ValueText;
                var name = (arg2.Expression as LiteralExpressionSyntax).Token.ValueText;
                var className = _classNames.Peek();
                _onRenamePropertyType(className, alias, name);
            }

            base.VisitAttribute(node);
            _attributeName = null;
        }

        public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
        {
            _classNames.Push(node.Identifier.ToString());
            base.VisitInterfaceDeclaration(node);
            _classNames.Pop();
        }

        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            _classNames.Push(node.Identifier.ToString());
            base.VisitClassDeclaration(node);
            _classNames.Pop();
        }

        public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            _propertyName = node.Identifier.ToString();
            base.VisitPropertyDeclaration(node);
            _propertyName = null;
        }
    }
}
