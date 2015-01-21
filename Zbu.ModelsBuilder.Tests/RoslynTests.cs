using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Zbu.ModelsBuilder.Building;

namespace Zbu.ModelsBuilder.Tests
{
    public interface IRandom1
    {}

    public interface IRandom2 : IRandom1
    {}

    public class TestBuilder : Builder
    {
        public TestBuilder(IList<TypeModel> typeModels, ParseResult parseResult)
            : base(typeModels, parseResult)
        { }
    }

    [TestFixture]
    public class RoslynTests
    {
        [Test]
        public void SemTest1()
        {
            // http://social.msdn.microsoft.com/Forums/vstudio/en-US/64ee86b8-0fd7-457d-8428-a0f238133476/can-roslyn-tell-me-if-a-member-of-a-symbol-is-visible-from-a-position-in-a-document?forum=roslyn
            const string code = @"
using System; // required to properly define the attribute
using Foo;
using Zbu.ModelsBuilder.Tests;

[assembly:AsmAttribute]

class SimpleClass
{ 
    public void SimpleMethod()
    { 
        Console.WriteLine(""hop"");
    }
}
interface IBase
{}
interface IInterface : IBase
{}
class AnotherClass : SimpleClass, IInterface
{
    class Nested
    {}
}
// if using Foo then reports Foo.Hop
// else just reports Foo which does not exist...
class SoWhat : Hop
{}
[MyAttr]
[SomeAttr(""blue"", Value=555)] // this is a named argument
[SomeAttr(1234)]
[NamedArgsAttribute(s2:""x"", s1:""y"")]
class WithAttr
{}
class Random : IRandom2
{}
class SomeAttrAttribute:Attribute
{
    public SomeAttrAttribute(string s, int x = 55){}
    public int Value { get; set; }
}
class NamedArgsAttribute:Attribute
{
    public NamedArgsAttribute(string s1 = ""a"", string s2 = ""b""){}
}
namespace Foo
{
    // reported as Foo.Hop
    class Hop {}

    class MyAttrAttribute // works
    {}
}";

            // http://msdn.microsoft.com/en-gb/vstudio/hh500769.aspx
            var tree = CSharpSyntaxTree.ParseText(code);
            //var mscorlib = new AssemblyFileReference(typeof(object).Assembly.Location);
            var mscorlib = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);

            // YES! adding the reference and Random1 is found by compilation
            // SO we can get rid of the OmitWhatever attribute!!!!
            // provided that we load everything that's in BIN as references
            // => the CodeInfos must be built on the SERVER and we send files to the SERVER.
            var testslib = MetadataReference.CreateFromFile(typeof(RoslynTests).Assembly.Location);

            var compilation = CSharpCompilation.Create(
                "MyCompilation",
                syntaxTrees: new[] { tree },
                references: new MetadataReference[] { mscorlib, testslib });
            var model = compilation.GetSemanticModel(tree);

            var diags = model.GetDiagnostics();
            if (diags.Length > 0)
            {
                foreach (var diag in diags)
                {
                    Console.WriteLine(diag);
                }                
            }

            //var writer = new ConsoleDumpWalker();
            //writer.Visit(tree.GetRoot());

            //var classDeclarations = tree.GetRoot().DescendantNodes(x => x is ClassDeclarationSyntax).OfType<ClassDeclarationSyntax>();
            var classDeclarations = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>();
            foreach (var classDeclaration in classDeclarations)
            {
                Console.WriteLine("class {0}", classDeclaration.Identifier.ValueText);
                var symbol = model.GetDeclaredSymbol(classDeclaration);
                //Console.WriteLine("symbol {0}", symbol.GetType());
                //Console.WriteLine("class {0}", symbol.Name); // just the local name
                var n = SymbolDisplay.ToDisplayString(symbol);
                Console.WriteLine("class {0}", n);
                Console.WriteLine("  : {0}", symbol.BaseType);
                foreach (var i in symbol.Interfaces)
                    Console.WriteLine("  : {0}", i.Name);
                foreach (var i in symbol.AllInterfaces)
                    Console.WriteLine("  + {0} {1}", i.Name, SymbolDisplay.ToDisplayString(i));

                // note: should take care of "error types" => how can we know if there are errors?
                foreach (var asym in symbol.GetAttributes())
                {
                    var t = asym.AttributeClass;
                    Console.WriteLine("  ! {0}", t);
                    if (t is IErrorTypeSymbol)
                    {
                        Console.WriteLine("  ERR");
                    }
                }
            }

            // OK but in our case, compilation of existing code would fail
            // because we haven't generated the missing code already... and yet?

            Console.WriteLine(model);
        }

        [Test]
        public void SemTestMissingReference()
        {
            const string code = @"
using System.Collections.Generic;
using Zbu.ModelsBuilder.Building;
public class MyBuilder : Zbu.ModelsBuilder.Tests.TestBuilder
{
    public MyBuilder(IList<TypeModel> typeModels, ParseResult parseResult)
        : base(typeModels, parseResult)
    { }
}
";

            var tree = CSharpSyntaxTree.ParseText(code);
            var mscorlib = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
            var testslib = MetadataReference.CreateFromFile(typeof(RoslynTests).Assembly.Location);

            var compilation = CSharpCompilation.Create(
                "MyCompilation",
                syntaxTrees: new[] { tree },
                references: new MetadataReference[] { mscorlib, testslib });
            var model = compilation.GetSemanticModel(tree);

            // CS0012: The type '...' is defined in an assembly that is not referenced
            // CS0246: The type or namespace '...' could not be found
            // CS0234: The nume or namespace name '...' does not exist in the namespace '...'
            var diags = model.GetDiagnostics();
            if (diags.Length > 0)
            {
                foreach (var diag in diags)
                {
                    Console.WriteLine(diag);
                }
            }

            //Assert.AreEqual(1, diags.Length);
            Assert.GreaterOrEqual(diags.Length, 2);
            
            Assert.AreEqual("CS0234", diags[0].Id);
            Assert.AreEqual("CS0012", diags[1].Id);
        }

        [Test]
        public void SemTestWithReferences()
        {
            const string code = @"
using System.Collections.Generic;
using Zbu.ModelsBuilder.Building;
public class MyBuilder : Zbu.ModelsBuilder.Tests.TestBuilder
{
    public MyBuilder(IList<TypeModel> typeModels, ParseResult parseResult)
        : base(typeModels, parseResult)
    { }
}
";

            var tree = CSharpSyntaxTree.ParseText(code);
            var refs = AssemblyUtility.GetAllReferencedAssemblyLocations().Select(x => MetadataReference.CreateFromFile(x));

            var compilation = CSharpCompilation.Create(
                "MyCompilation",
                syntaxTrees: new[] { tree },
                references: refs);
            var model = compilation.GetSemanticModel(tree);

            var diags = model.GetDiagnostics();
            if (diags.Length > 0)
            {
                foreach (var diag in diags)
                {
                    Console.WriteLine(diag);
                }
            }

            Assert.AreEqual(0, diags.Length);
        }

        [Test]
        public void SemTestAssemblyAttributes()
        {
            const string code = @"
using System;
[assembly: Nevgyt(""yop"")]
[assembly: Shmuit]

class Shmuit:Attribute
{}

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

            var tree = CSharpSyntaxTree.ParseText(code);
            var mscorlib = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);

            var compilation = CSharpCompilation.Create(
                "MyCompilation",
                syntaxTrees: new[] { tree },
                references: new MetadataReference[] { mscorlib });
            var model = compilation.GetSemanticModel(tree);
            foreach (var attrData in compilation.Assembly.GetAttributes())
            {
                var attrClassSymbol = attrData.AttributeClass;

                // handle errors
                if (attrClassSymbol is IErrorTypeSymbol) continue;
                if (attrData.AttributeConstructor == null) continue;

                var attrClassName = SymbolDisplay.ToDisplayString(attrClassSymbol);
                Console.WriteLine(attrClassName);
            }
        }

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

            var tree = CSharpSyntaxTree.ParseText(code);
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

            var tree = CSharpSyntaxTree.ParseText(code);
            var writer = new TestWalker();
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

            var tree = CSharpSyntaxTree.ParseText(code);
            var writer = new ConsoleDumpWalker();
            writer.Visit(tree.GetRoot());
        }

        [Test]
        public void ParseTest4()
        {
            const string code = @"
[SomeAttribute(""value1"", ""value2"")]
[SomeOtherAttribute(Foo:""value1"", BaDang:""value2"")]
class SimpleClass1
{ 
}";

            var tree = CSharpSyntaxTree.ParseText(code);
            var writer = new ConsoleDumpWalker();
            writer.Visit(tree.GetRoot());
        }

        [Test]
        public void ParseTest5()
        {
            const string code = @"


[SomeAttribute(SimpleClass1.Const)]
[SomethingElse(Foo.Blue|Foo.Red|Foo.Pink)]
[SomethingElse(Foo.Blue)]
class SimpleClass1
{ 
    public const string Const = ""const"";
}";

            var tree = CSharpSyntaxTree.ParseText(code);
            var writer = new ConsoleDumpWalker();
            writer.Visit(tree.GetRoot());
        }

        [Test]
        public void ParseAndDetectErrors()
        {
            const string code = @"

class MyClass
{
poo
}";
            var tree = CSharpSyntaxTree.ParseText(code);
            var diags = tree.GetDiagnostics().ToArray();
            Assert.AreEqual(1, diags.Length);
            var diag = diags[0];
            Assert.AreEqual("CS1519", diag.Id);
        }

        [Test]
        public void ParseAndDetectNoError()
        {
            const string code = @"

[Whatever]
class MyClass
{
}";
            var tree = CSharpSyntaxTree.ParseText(code);
            var diags = tree.GetDiagnostics().ToArray();
            Assert.AreEqual(0, diags.Length);
            // unknown attribute is a semantic error
        }
    }

    class TestWalker : CSharpSyntaxWalker
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

    class ConsoleDumpWalker : CSharpSyntaxWalker
    {
        const string prefix = "Microsoft.CodeAnalysis.";

        public override void VisitToken(SyntaxToken token)
        {
            Console.WriteLine("TK:" + token);
            base.VisitToken(token);
        }

        public override void Visit(SyntaxNode node)
        {
            var padding = node.Ancestors().Count();
            var prepend = node.ChildNodes().Any() ? "[-]" : "[.]";
            var nodetype = node.GetType().FullName;
            if (nodetype.StartsWith(prefix)) nodetype = nodetype.Substring(prefix.Length);
            var line = new string(' ', padding) + prepend + " " + nodetype;
            Console.WriteLine(line);

            //var decl = node as ClassDeclarationSyntax;
            //if (decl != null && decl.BaseList != null)
            //{
            //    Console.Write(new string(' ', padding + 4) + decl.Identifier);
            //    foreach (var n in decl.BaseList.Types.OfType<IdentifierNameSyntax>())
            //    {
            //        Console.Write(" " + n.Identifier);
            //    }
            //    Console.WriteLine();
            //}

            var attr = node as AttributeSyntax;
            if (attr != null)
            {
                Console.WriteLine(new string(' ', padding + 4) + "> " + attr.Name);
                foreach (var arg in attr.ArgumentList.Arguments)
                {
                    var expr = arg.Expression as LiteralExpressionSyntax;
                    //Console.WriteLine(new string(' ', padding + 4) + "> " + arg.NameColon + " " + arg.NameEquals);
                    Console.WriteLine(new string(' ', padding + 4) + "> " + (expr == null ? null : expr.Token.Value));
                }
            }
            var attr2 = node as IdentifierNameSyntax;
            if (attr2 != null)
            {
                Console.WriteLine(new string(' ', padding + 4) + "T " + attr2.Identifier.GetType());
                Console.WriteLine(new string(' ', padding + 4) + "V " + attr2.Identifier);
            }

            var x = node as TypeSyntax;
            if (x != null)
            {
                var xtype = x.GetType().FullName;
                if (xtype.StartsWith(prefix)) xtype = nodetype.Substring(prefix.Length);
                Console.WriteLine(new string(' ', padding + 4) + "> " + xtype);
            }

            base.Visit(node);
        }
    }
}
