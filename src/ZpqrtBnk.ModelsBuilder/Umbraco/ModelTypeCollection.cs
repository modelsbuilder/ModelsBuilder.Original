using System;
using System.Collections.Generic;
using Umbraco.Core.Composing;

namespace ZpqrtBnk.ModelsBuilder.Umbraco
{
    /// <summary>
    /// Represents a collection of models for the PublishedModelFactory.
    /// </summary>
    public class ModelTypeCollection : BuilderCollectionBase<Type>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ModelTypeCollection"/> class.
        /// </summary>
        public ModelTypeCollection(IEnumerable<Type> items) 
            : base(items)
        { }
    }
}