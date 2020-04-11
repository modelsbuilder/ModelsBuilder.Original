namespace Our.ModelsBuilder.Building
{
    /// <summary>
    /// Provides <see cref="CodeModelData"/>.
    /// </summary>
    public interface ICodeModelDataSource
    {
        /// <summary>
        /// Gets <see cref="CodeModelData"/>.
        /// </summary>
        CodeModelData GetCodeModelData();
    }
}