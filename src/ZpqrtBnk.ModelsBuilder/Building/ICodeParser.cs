using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Our.ModelsBuilder.Options;

namespace Our.ModelsBuilder.Building
{
    /// <summary>
    /// Parses code sources.
    /// </summary>
    public interface ICodeParser
    {
        /// <summary>
        /// Parses code sources.
        /// </summary>
        /// <param name="sources">Sources.</param>
        /// <param name="optionsBuilder">An options builder.</param>
        /// <param name="references">Optional references.</param>
        void Parse(IDictionary<string, string> sources, CodeOptionsBuilder optionsBuilder, IEnumerable<PortableExecutableReference> references = null);
    }
}