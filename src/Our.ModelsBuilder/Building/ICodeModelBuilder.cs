namespace Our.ModelsBuilder.Building
{
    /// <summary>
    /// Builds a <see cref="CodeModel"/>.
    /// </summary>
    public interface ICodeModelBuilder
    {
        /// <summary>
        /// Builds a <see cref="CodeModel"/>.
        /// </summary>
        CodeModel Build(CodeModelData data);
    }
}