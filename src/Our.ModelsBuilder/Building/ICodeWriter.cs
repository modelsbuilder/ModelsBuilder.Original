namespace Our.ModelsBuilder.Building
{
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
        void WriteModelFile(ContentTypeModel model);

        /// <summary>
        /// Writes the model infos file.
        /// </summary>
        void WriteModelInfosFile();

        /// <summary>
        /// Writes everything in one single file.
        /// </summary>
        void WriteSingleFile();
    }
}