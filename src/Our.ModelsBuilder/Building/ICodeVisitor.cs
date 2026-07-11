using Microsoft.CodeAnalysis;

namespace Our.ModelsBuilder.Building;

/// <summary>
/// Visits existing code sources.
/// </summary>
public interface ICodeVisitor 
{
    /// <summary>
    /// Visits code sources.
    /// </summary>
    /// <param name="sources">Sources.</param>
    /// <param name="references">Optional references.</param>
    void Visit(IDictionary<string, string> sources, IEnumerable<MetadataReference>? references = null);
}