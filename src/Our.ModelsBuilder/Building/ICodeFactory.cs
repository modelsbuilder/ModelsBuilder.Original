using System.Text;
using Our.ModelsBuilder.Options;

namespace Our.ModelsBuilder.Building
{
    /// <summary>
    /// Creates the services required to generate models.
    /// </summary>
    public interface ICodeFactory
    {
        /// <summary>
        /// Creates a code model data source.
        /// </summary>
        /// <returns></returns>
        ICodeModelDataSource CreateCodeModelDataSource();

        /// <summary>
        /// Creates a code options builder.
        /// </summary>
        CodeOptionsBuilder CreateCodeOptionsBuilder();

        /// <summary>
        /// Creates a code parser.
        /// </summary>
        ICodeParser CreateCodeParser();

        /// <summary>
        /// Creates a code model builder.
        /// </summary>
        ICodeModelBuilder CreateCodeModelBuilder(ModelsBuilderOptions options, CodeOptions codeOptions);

        /// <summary>
        /// Creates a code writer.
        /// </summary>
        ICodeWriter CreateCodeWriter(CodeModel model, StringBuilder text = null);
    }
}