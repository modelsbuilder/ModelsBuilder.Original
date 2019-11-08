namespace ZpqrtBnk.ModelsBuilder.Building
{
    public interface ICodeWriter
    {
        void Reset();

        string Code { get; }

        void WriteModelFile(TypeModel model);

        void WriteModelInfosFile(CodeModel models);

        void WriteSingleFile(CodeModel models);
    }
}