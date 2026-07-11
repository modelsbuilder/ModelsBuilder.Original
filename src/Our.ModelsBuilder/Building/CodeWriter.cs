using System.Text;

namespace Our.ModelsBuilder.Building;

/// <summary>
/// Provides the default code writer.
/// </summary>
public class CodeWriter : ModelsCodeWriter, ICodeWriter
{
    private ContentTypesCodeWriter? _contentTypesContentTypesCodeWriter;
    private InfosCodeWriter? _infosCodeWriter;

    /// <summary>
    /// Initializes a new instance of the <see cref="CodeWriter"/> class.
    /// </summary>
    public CodeWriter(StringBuilder? text = null) 
        : base(text)
    { }

    /// <inheritdoc />
    public virtual ContentTypesCodeWriter ContentTypesCodeWriter 
        => _contentTypesContentTypesCodeWriter ??= new ContentTypesCodeWriter(this);

    /// <inheritdoc />
    public virtual InfosCodeWriter InfosCodeWriter
        => _infosCodeWriter ??= new InfosCodeWriter(this);

    #region Write Complete Files

    /// <inheritdoc />
    public virtual void WriteModelFile(CodeModel codeModel, ContentTypeModel model)
    {
        WriteFileHeader();
        WriteLine();

        var u = new Using(this);
        u.WriteUsing(codeModel.Using);
        u.WriteUsing("System.CodeDom.Compiler"); // FIXME??
        WriteLine();

        WriteBlockStart($"namespace {codeModel.ModelsNamespace}");
        ContentTypesCodeWriter.WriteModel(codeModel, model);
        WriteBlockEnd();
    }

    /// <inheritdoc />
    public virtual void WriteSingleFile(CodeModel codeModel)
    {
        WriteFileHeader();
        WriteLine();

        var u = new Using(this);
        u.WriteUsing(codeModel.Using);
        u.WriteUsing(Using.Defaults); // FIXME??
        WriteLine();

        // assembly attributes marker
        WriteIndentLine("//ASSATTR");
        WriteLine();

        WriteBlockStart($"namespace {codeModel.ModelsNamespace}");
        ContentTypesCodeWriter.WriteModels(codeModel, codeModel.ContentTypes.ContentTypes);
        WriteBlockEnd();

        WriteLine();

        WriteBlockStart($"namespace {codeModel.ModelInfosClassNamespace}");
        InfosCodeWriter.WriteInfosClass(codeModel);
        WriteBlockEnd();
    }

    /// <inheritdoc />
    public virtual void WriteModelInfosFile(CodeModel codeModel)
    {
        WriteFileHeader();
        WriteLine();

        var u = new Using(this);
        u.WriteUsing(codeModel.Using);
        u.WriteUsing(Using.Defaults); // FIXME??
        WriteLine();

        WriteBlockStart($"namespace {codeModel.ModelInfosClassNamespace}");
        InfosCodeWriter.WriteInfosClass(codeModel);
        WriteBlockEnd();
    }

    #endregion
}