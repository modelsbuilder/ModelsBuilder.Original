namespace Our.ModelsBuilder.Building;

internal class Using
{
    private readonly HashSet<string> _written = new();
    private readonly CodeWriter _codeWriter;

    public Using(CodeWriter codeWriter)
    {
        _codeWriter = codeWriter;
    }

    public void WriteUsing(string ns)
    {
        if (_written.Add(ns))
            _codeWriter.WriteIndentLine($"using {ns};");
    }

    public void WriteUsing(IEnumerable<string> nss)
    {
        foreach (var ns in nss.Where(_written.Add))
            _codeWriter.WriteIndentLine($"using {ns};");
    }

    public static ISet<string> Defaults { get; } = new HashSet<string>
    {
        "System",
        "System.Linq",
        "System.Linq.Expressions", // ?
        "System.Collections.Generic",
        "System.CodeDom.Compiler",
        //"Umbraco.Cms.Core.Models", // why?
        "Umbraco.Cms.Core.Models.PublishedContent",
        "Our.ModelsBuilder", // yes
        //"Our.ModelsBuilder.Umbraco", // why?
        "Umbraco.Extensions", // bit of everything
        "Umbraco.Cms.Infrastructure.ModelsBuilder",
    };
}