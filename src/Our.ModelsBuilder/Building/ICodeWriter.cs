namespace Our.ModelsBuilder.Building;

/// <summary>
/// Writes code.
/// </summary>
public interface ICodeWriter
{
    /// <summary>
    /// Resets the code writer.
    /// </summary>
    void Reset();

    /// <summary>
    /// Gets the written code.
    /// </summary>
    string Code { get; }

    /// <summary>
    /// Gets a code writer for writing content types.
    /// </summary>
    ContentTypesCodeWriter ContentTypesCodeWriter { get; }

    /// <summary>
    /// Gets a code writer for writing the infos class.
    /// </summary>
    InfosCodeWriter InfosCodeWriter { get; }

    /// <summary>
    /// Writes a single model file.
    /// </summary>
    /// <param name="codeModel">The code model.</param>
    /// <param name="model">The content type code model.</param>
    void WriteModelFile(CodeModel codeModel, ContentTypeModel model);

    /// <summary>
    /// Writes the model infos file.
    /// </summary>
    /// <param name="model">The code model.</param>
    void WriteModelInfosFile(CodeModel model);

    /// <summary>
    /// Writes everything in one single file.
    /// </summary>
    /// <param name="model">The code model.</param>
    void WriteSingleFile(CodeModel model);
}