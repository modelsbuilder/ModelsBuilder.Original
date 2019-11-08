namespace ZpqrtBnk.ModelsBuilder.Building
{
    public interface ICodeWriter
    {
        void Reset();

        string Code { get; }

        void WriteModelFile(ContentTypeModel model);

        void WriteModelInfosFile(CodeModel models);

        void WriteSingleFile(CodeModel models);
    }
}