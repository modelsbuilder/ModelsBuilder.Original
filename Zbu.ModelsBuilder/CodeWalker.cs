using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Roslyn.Compilers.CSharp;

namespace Zbu.ModelsBuilder
{
    class CodeWalker : SyntaxWalker
    {
        private string _propertyName;
        private string _attributeName;
        private readonly Stack<string> _classNames = new Stack<string>();

        private Action<string> _onIgnoreContentType;
        private Action<string, string> _onIgnorePropertyType;
        private Action<string, string> _onRenameContentType;

        public void Visit(SyntaxNode node,
            Action<string> onIgnoreContentType,
            Action<string, string> onIgnorePropertyType,
            Action<string, string> onRenameContentType)
        {
            _onIgnoreContentType = onIgnoreContentType;
            _onIgnorePropertyType = onIgnorePropertyType;
            _onRenameContentType = onRenameContentType;

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
                }
            }
            base.VisitLiteralExpression(node);
        }

        public override void VisitAttribute(AttributeSyntax node)
        {
            // assuming we don't nest attributes
            _attributeName = node.Name.ToString();
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
