using System.Collections.Generic;

namespace Our.ModelsBuilder.Building
{
    /// <summary>
    /// Represents a source for code models.
    /// </summary>
    public class CodeModelData
    {
        /// <summary>
        /// Gets or sets the list of content type models.
        /// </summary>
        public List<ContentTypeModel> ContentTypes { get; set; } = new List<ContentTypeModel>();
    }
}