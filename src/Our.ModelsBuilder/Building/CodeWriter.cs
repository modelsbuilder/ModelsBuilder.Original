using System.Text;

namespace Our.ModelsBuilder.Building
{
    /// <summary>
    /// Provides the default code writer.
    /// </summary>
    public class CodeWriter : ModelsCodeWriter, ICodeWriter
    {
        private ContentTypesCodeWriter _contentTypesContentTypesCodeWriter;
        private InfosCodeWriter _infosCodeWriter;

        /// <summary>
        /// Initializes a new instance of the <see cref="CodeWriter"/> class.
        /// </summary>
        public CodeWriter(CodeModel model, StringBuilder text = null) 
            : base(model, text)
        { }

        /// <inheritdoc />
        public virtual ContentTypesCodeWriter ContentTypesCodeWriter 
            => _contentTypesContentTypesCodeWriter ??= new ContentTypesCodeWriter(this);

        /// <inheritdoc />
        public virtual InfosCodeWriter InfosCodeWriter
            => _infosCodeWriter ??= new InfosCodeWriter(this);

        #region Write Complete Files

        /// <summary>
        /// Writes a using statement if it is not already defined by the code model.
        /// </summary>
        protected virtual void WriteUsing(string ns)
        {
            if (!CodeModel.Using.Contains(ns))
                WriteIndentLine($"using {ns};");
        }

        /// <summary>
        /// Writes the using statements defined by the code model.
        /// </summary>
        public virtual void WriteUsing()
        {
            foreach (var t in CodeModel.Using)
                WriteIndentLine($"using {t};");
        }

        /// <inheritdoc />
        public virtual void WriteModelFile(ContentTypeModel model)
        {
            WriteFileHeader();
            WriteLine();

            WriteUsing();
            WriteUsing("System.CodeDom.Compiler");
            WriteLine();

            WriteBlockStart($"namespace {CodeModel.ModelsNamespace}");
            ContentTypesCodeWriter.WriteModel(model);
            WriteBlockEnd();
        }

        /// <inheritdoc />
        public virtual void WriteSingleFile()
        {
            WriteFileHeader();
            WriteLine();

            WriteUsing();
            WriteUsing("System");
            WriteUsing("System.Linq");
            WriteUsing("System");
            WriteUsing("System.Linq");
            WriteUsing("System.Collections.Generic");
            WriteUsing("System.CodeDom.Compiler");
            WriteUsing("Umbraco.Core.Models.PublishedContent");
            WriteUsing("Our.ModelsBuilder");
            WriteUsing("Our.ModelsBuilder.Umbraco");
            WriteLine();

            // assembly attributes marker
            WriteIndentLine("//ASSATTR");
            WriteLine();

            WriteBlockStart($"namespace {CodeModel.ModelsNamespace}");
            ContentTypesCodeWriter.WriteModels(CodeModel.ContentTypes.ContentTypes);
            WriteBlockEnd();

            WriteLine();

            WriteBlockStart($"namespace {CodeModel.ModelInfosClassNamespace}");
            InfosCodeWriter.WriteInfosClass(CodeModel);
            WriteBlockEnd();
        }

        /// <inheritdoc />
        public virtual void WriteModelInfosFile()
        {
            WriteFileHeader();
            WriteLine();

            WriteUsing();
            WriteUsing("System");
            WriteUsing("System.Linq");
            WriteUsing("System.Collections.Generic");
            WriteUsing("System.CodeDom.Compiler");
            WriteUsing("Umbraco.Core.Models.PublishedContent");
            WriteUsing("Our.ModelsBuilder");
            WriteUsing("Our.ModelsBuilder.Umbraco");
            WriteLine();

            WriteBlockStart($"namespace {CodeModel.ModelInfosClassNamespace}");
            InfosCodeWriter.WriteInfosClass(CodeModel);
            WriteBlockEnd();
        }

        #endregion
    }
}