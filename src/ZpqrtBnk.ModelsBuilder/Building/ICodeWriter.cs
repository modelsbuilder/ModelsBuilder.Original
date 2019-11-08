namespace ZpqrtBnk.ModelsBuilder.Building
{
    public interface ICodeWriter
    {
        void Reset();

        string Code { get; }

        void WriteModelFile(ContentTypeModel model);

        void WriteModelInfosFile(CodeModel model);

        void WriteSingleFile(CodeModel model);
    }
}