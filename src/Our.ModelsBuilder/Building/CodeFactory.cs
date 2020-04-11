using System.Text;
using Our.ModelsBuilder.Options;
using Our.ModelsBuilder.Umbraco;

namespace Our.ModelsBuilder.Building
{
    /// <summary>
    /// Implements the default code factory.
    /// </summary>
    public class CodeFactory : ICodeFactory
    {
        private readonly UmbracoServices _umbracoServices;
        private readonly OptionsConfiguration _optionsConfiguration;

        /// <summary>
        /// Initializes a new instance of the <see cref="CodeFactory"/> class.
        /// </summary>
        public CodeFactory(UmbracoServices umbracoServices, OptionsConfiguration optionsConfiguration)
        {
            _umbracoServices = umbracoServices;
            _optionsConfiguration = optionsConfiguration;
        }

        /// <inheritdoc />
        public ICodeModelDataSource CreateCodeModelDataSource()
             => new CodeModelDataSource(_umbracoServices);

        /// <inheritdoc />
        public virtual CodeOptionsBuilder CreateCodeOptionsBuilder()
            => _optionsConfiguration.Configure(new CodeOptionsBuilder());

        /// <inheritdoc />
        public virtual ICodeParser CreateCodeParser()
            => new CodeParser(_optionsConfiguration.ModelsBuilderOptions.LanguageVersion);

        /// <inheritdoc />
        public virtual ICodeModelBuilder CreateCodeModelBuilder(ModelsBuilderOptions options, CodeOptions codeOptions)
            => new CodeModelBuilder(options, codeOptions);

        /// <inheritdoc />
        public virtual ICodeWriter CreateCodeWriter(CodeModel model, StringBuilder text = null)
            => new CodeWriter(model, text);
    }
}