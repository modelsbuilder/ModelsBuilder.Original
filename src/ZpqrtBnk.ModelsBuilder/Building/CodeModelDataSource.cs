using Our.ModelsBuilder.Umbraco;

namespace Our.ModelsBuilder.Building
{
    /// <summary>
    /// Implements the default <see cref="ICodeModelDataSource"/>.
    /// </summary>
    public class CodeModelDataSource : ICodeModelDataSource
    {
        private readonly UmbracoServices _umbracoServices;

        /// <summary>
        /// Initializes a new instance of the <see cref="CodeModelDataSource"/> class.
        /// </summary>
        public CodeModelDataSource(UmbracoServices umbracoServices)
        {
            _umbracoServices = umbracoServices;
        }

        public CodeModelData GetCodeModelData()
        {
            return new CodeModelData
            {
                ContentTypes = _umbracoServices.GetContentTypes()
            };
        }
    }
}