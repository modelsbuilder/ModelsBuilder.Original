using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using Roslyn.Compilers.CSharp;

namespace Zbu.ModelsBuilder.Tests
{
    [TestFixture]
    public class RoslynTests
    {
        [Test]
        public void ParseTest1()
        {
            const string code = @"
[assembly: Nevgyt(""yop"")]
[Fooxy(""yop"")]
class SimpleClass
{ 
    [Funky(""yop"")]
    public void SimpleMethod()
    { 
        var list = new List<string>();
        list.Add(""first"");
        list.Add(""second"");
        var result = from item in list where item == ""first"" select item;
    }
}";

            var tree = SyntaxTree.ParseText(code);
            var writer = new ConsoleDumpWalker();
            writer.Visit(tree.GetRoot());
        }

        [Test]
        public void ParseTest3()
        {
            const string code = @"
class SimpleClass1 : BaseClass, ISomething, ISomethingElse
{ 
}
class SimpleClass2
{ 
}";

            var tree = SyntaxTree.ParseText(code);
            var writer = new ConsoleDumpWalker();
            writer.Visit(tree.GetRoot());
        }

        [Test]
        public void ParseTest2()
        {
            const string code = @"
using Zbu.ModelsBuilder;

[assembly: Generator.IgnoreContentType(""ccc"")]

namespace Umbrco.Web.Models.User
{
    // don't create a model for ddd
    // IGNORED should be out of the namespace
    [assembly: Generator.IgnoreContentType(""ddd"")]

    // create a mixin for MixinTest but with a different class name
    [PublishedContentModel(""MixinTest"")]
    public partial interface IMixinTestRenamed
    { }

    // create a model for bbb but with a different class name
    [PublishedContentModel(""bbb"")]
    public partial class SpecialBbb
    { }

    // create a model for ...
    [Generator.IgnorePropertyType(""nomDeLEleve"")] // but don't include that property
    public partial class LoskDalmosk
    {
    }

    // create a model for page...
    public partial class Page
    {
        // but don't include that property because I'm doing it
        [Generator.IgnorePropertyType(""alternativeText"")]
        public AlternateText AlternativeText { get { return this.GetPropertyValue<AlternateText>(""alternativeText""); } }
    }
}
";

            var tree = SyntaxTree.ParseText(code);
            var writer = new TestWalker();
            writer.Visit(tree.GetRoot());
        }
    }

    class TestWalker : SyntaxWalker
    {
        private string _propertyName;
        private string _attributeName;
        private readonly Stack<string> _classNames = new Stack<string>();

        public override void VisitLiteralExpression(LiteralExpressionSyntax node)
        {
            if (_attributeName != null)
            {
                string className;
                //Console.WriteLine("ATTRIBUTE VALUE {0}", node.Token.ValueText);
                switch (_attributeName)
                {
                    case "Generator.IgnoreContentType":
                        Console.WriteLine("Ignore ContentType {0}", node.Token.ValueText);
                        break;
                    case "Generator.IgnorePropertyType":
                        className = _classNames.Peek();
                        Console.WriteLine("Ignore PropertyType {0}.{1}", className, node.Token.ValueText);
                        break;
                    case "PublishedContentModel":
                        className = _classNames.Peek();
                        Console.WriteLine("Name {0} for ContentType {1}", className, node.Token.ValueText);
                        break;
                }
            }
            base.VisitLiteralExpression(node);
        }

        public override void VisitAttribute(AttributeSyntax node)
        {
            //Console.WriteLine("ATTRIBUTE {0}", node.Name);
            _attributeName = node.Name.ToString();
            base.VisitAttribute(node);
            _attributeName = null;
        }

        public override void VisitInterfaceDeclaration(InterfaceDeclarationSyntax node)
        {
            //Console.WriteLine("BEGIN INTERFACE {0}", node.Identifier);
            _classNames.Push(node.Identifier.ToString());
            base.VisitInterfaceDeclaration(node);
            _classNames.Pop();
            //Console.WriteLine("END INTERFACE {0}", node.Identifier);
        }

        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            //Console.WriteLine("BEGIN CLASS {0}", node.Identifier);
            _classNames.Push(node.Identifier.ToString());
            base.VisitClassDeclaration(node);
            _classNames.Pop();
            //Console.WriteLine("END CLASS {0}", node.Identifier);
        }

        public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            _propertyName = node.Identifier.ToString();
            base.VisitPropertyDeclaration(node);
            _propertyName = null;
        }

        public override void Visit(SyntaxNode node)
        {
            var padding = node.Ancestors().Count();
            var prepend = node.ChildNodes().Any() ? "[-]" : "[.]";
            var line = new string(' ', padding) + prepend + " " + node.GetType().ToString();
            Console.WriteLine(line);
            base.Visit(node);
        }
    }

    class ConsoleDumpWalker : SyntaxWalker
    {
        public override void Visit(SyntaxNode node)
        {
            var padding = node.Ancestors().Count();
            var prepend = node.ChildNodes().Any() ? "[-]" : "[.]";
            var line = new string(' ', padding) + prepend + " " + node.GetType().ToString();
            Console.WriteLine(line);

            var decl = node as ClassDeclarationSyntax;
            if (decl != null && decl.BaseList != null)
            {
                Console.Write(new string(' ', padding + 4) + decl.Identifier);
                foreach (var n in decl.BaseList.Types.OfType<IdentifierNameSyntax>())
                {
                    Console.Write(" " + n.Identifier);
                }
                Console.WriteLine();
            }

            base.Visit(node);
        }
    }
}
