using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Options;
using Our.ModelsBuilder.Options;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Infrastructure.ModelsBuilder;

namespace Our.ModelsBuilder.Building;

/// <summary>
/// Implements the default <see cref="ICodeVisitor"/>.
/// </summary>
public class CodeVisitor : ICodeVisitor
{
    private static readonly string ImplementPropertyTypeFullName = typeof(ImplementPropertyTypeAttribute).FullName;
    private readonly ModelsBuilderOurOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="CodeVisitor"/> class.
    /// </summary>
    public CodeVisitor(IOptions<ModelsBuilderOurOptions> options)
    {
        _options = options.Value;
    }

    /// <summary>
    /// Gets or sets a value indicating whether to write diagnostics to console (for tests).
    /// </summary>
    public bool WriteDiagnostics { get; set; }

    /// <inheritdoc />
    public void Visit(IDictionary<string, string> files, IEnumerable<MetadataReference>? references = null)
    {
        var compiler = new Compiler(references, _options.LanguageVersion);
        var compilation = compiler.GetCompilation("Our.ModelsBuilder.Generated", files, out var trees);

        // debug
        if (WriteDiagnostics)
            foreach (var d in compilation.GetDiagnostics())
                Console.WriteLine(d);

        foreach (var tree in trees)
            Parse(compilation, tree);
    }

    private void Parse(Compilation compilation, SyntaxTree tree)
    {
        var model = compilation.GetSemanticModel(tree);

        // we quite probably have errors but that is normal, as we may for instance refer
        // to types that have not been generated yet, etc.
        //var diagnostics = model.GetDiagnostics();

        // classes
        var classDeclarations = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>();
        var classSymbols = classDeclarations.Select(x => model.GetDeclaredSymbol(x));
        foreach (var classSymbol in classSymbols)
        {
            if (classSymbol is null) continue; // ?

            if (classSymbol.IsStatic)
            {
                // extension class
                var methodSymbols = classSymbol.GetMembers().OfType<IMethodSymbol>().Where(x => x is { IsStatic: true, IsGenericMethod: false });
                foreach (var methodSymbol in methodSymbols)
                    ParseExtensions(methodSymbol);
            }
            else
            {
                // class
                ParseClassDeclaration(classSymbol);
            }
        }

        // interfaces
        var interfaceDeclarations = tree.GetRoot().DescendantNodes().OfType<InterfaceDeclarationSyntax>();
        var interfaceSymbols = interfaceDeclarations.Select(x => model.GetDeclaredSymbol(x));
        foreach (var interfaceSymbol in interfaceSymbols)
        {
            if (interfaceSymbol is null) continue; // ?

            ParseInterfaceDeclaration(interfaceSymbol);
        }
    }

    private static bool IsObject(ISymbol symbol)
    {
        return symbol.ContainingNamespace.ToString() == "System" && 
               symbol.Name == "Object";
    }

    // parse the partial class declaration, detecting
    // - if the partial class defines a base class
    // - the interfaces declared by the partial class
    // - whether the partial class provides the constructor
    // - FIXME properties implemented by the partial class
    private void ParseClassDeclaration(INamedTypeSymbol classSymbol)
    {
        // is a partial defining a base class?
        var baseClassSymbol = classSymbol.BaseType ?? throw new Exception("panic");
        if (!IsObject(baseClassSymbol)) // never null, but can be 'object'
        {
            var className = baseClassSymbol.Name;
            //var assemblyName = baseClassSymbol is IErrorTypeSymbol
            //    ? ""
            //    : baseClassSymbol.ContainingNamespace.ToString();
            _options.ModelsOptions.SetModelClassHasBaseClass(classSymbol.Name, className);
        }

        // discover the interfaces
        var interfaceSymbols = classSymbol.Interfaces;
        foreach (var symbol in interfaceSymbols)
            _options.ModelsOptions.SetModelClassHasInterface(classSymbol.Name, symbol.Name);

        // is a partial implementing the constructor?
        var hasConstructor = classSymbol.Constructors
            .Any(x =>
            {
                if (x.IsStatic) return false;
                if (x.Parameters.Length != 1) return false;
                var type1 = x.Parameters[0].Type;
                var type2 = typeof(IPublishedContent);
                return type1.ToDisplayString() == type2.FullName;
            });

        if (hasConstructor)
            _options.ModelsOptions.SetModelClassHasConstructor(classSymbol.Name);

        // is the partial implementing some properties?
        var propertySymbols = classSymbol.GetMembers().OfType<IPropertySymbol>();
        foreach (var propertySymbol in propertySymbols)
            ParsePropertySymbol(classSymbol, propertySymbol);
    }

    // parse the partial interface declaration, detecting
    // - the interfaces declared by the partial interface
    // - FIXME so no property?
    private void ParseInterfaceDeclaration(ITypeSymbol interfaceSymbol)
    {
        var interfaceSymbols = interfaceSymbol.Interfaces;
        foreach (var symbol in interfaceSymbols)
            _options.ModelsOptions.SetModelClassHasInterface(interfaceSymbol.Name, symbol.Name);
    }

    private void ParsePropertySymbol(ISymbol classSymbol, ISymbol symbol)
    {
        foreach (var attrData in symbol.GetAttributes())
        {
            var attrClassSymbol = attrData.AttributeClass ?? throw new Exception("panic");
            var attrClassName = SymbolDisplay.ToDisplayString(attrClassSymbol);

            // handle errors
            //if (attrClassSymbol is IErrorTypeSymbol) continue;
            if (attrData.AttributeConstructor == null) continue;

            if (attrClassName == ImplementPropertyTypeFullName)
            {
                if (attrData.ConstructorArguments.Length != 1)
                    throw new InvalidOperationException("Invalid number of arguments for ImplementPropertyTypeAttribute.");
                var propertyAliasToIgnore = (string)attrData.ConstructorArguments[0].Value;
                _options.ModelsOptions.IgnorePropertyType(ContentTypeIdentity.ClrName(classSymbol.Name), propertyAliasToIgnore);
            }
        }
    }

    // FIXME should be "extensions" don't need to be true extension methods
    private void ParseExtensions(IMethodSymbol symbol)
    {
        foreach (var attrData in symbol.GetAttributes())
        {
            var attrClassSymbol = attrData.AttributeClass;
            var attrClassName = SymbolDisplay.ToDisplayString(attrClassSymbol);

            // handle errors
            //if (attrClassSymbol is IErrorTypeSymbol) continue;
            if (attrData.AttributeConstructor == null) continue;

            // there isn't much we can guess from an extension method,
            // the attribute has to be explicit about everything

            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (attrClassName)
            {
                case "Our.ModelsBuilder.ImplementPropertyTypeAttribute":
                    if (attrData.ConstructorArguments.Length != 2)
                        throw new InvalidOperationException("Invalid number of arguments for ImplementPropertyTypeAttribute.");
                    var contentTypeAlias = (string) attrData.ConstructorArguments[0].Value;
                    var propertyTypeAlias = (string) attrData.ConstructorArguments[1].Value;
                    _options.ModelsOptions.IgnorePropertyType(ContentTypeIdentity.Alias(contentTypeAlias), propertyTypeAlias);
                    break;
            }
        }
    }
}