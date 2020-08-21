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
        /// <summary>
        /// Initializes a new instance of the <see cref="CodeFactory"/> class.
        /// </summary>
        public CodeFactory(UmbracoServices umbracoServices, OptionsConfiguration optionsConfiguration)
        {
            UmbracoServices = umbracoServices;
            OptionsConfiguration = optionsConfiguration;
        }

        /// <summary>
        /// Gets the Umbraco services.
        /// </summary>
        protected UmbracoServices UmbracoServices { get; }

        /// <summary>
        /// Gets the options configuration.
        /// </summary>
        protected OptionsConfiguration OptionsConfiguration { get; }

        /// <inheritdoc />
        public virtual ICodeModelDataSource CreateCodeModelDataSource()
             => new CodeModelDataSource(UmbracoServices);

        /// <inheritdoc />
        public virtual CodeOptionsBuilder CreateCodeOptionsBuilder()
            => OptionsConfiguration.Configure(new CodeOptionsBuilder());

        /// <inheritdoc />
        public virtual ICodeParser CreateCodeParser()
            => new CodeParser(OptionsConfiguration.ModelsBuilderOptions.LanguageVersion);

        /// <inheritdoc />
        public virtual ICodeModelBuilder CreateCodeModelBuilder(ModelsBuilderOptions options, CodeOptions codeOptions)
            => new CodeModelBuilder(options, codeOptions);

        /// <inheritdoc />
        public virtual ICodeWriter CreateCodeWriter(CodeModel model, StringBuilder text = null)
            => new CodeWriter(model, text);
    }
}