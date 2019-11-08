using System.Text;

namespace ZpqrtBnk.ModelsBuilder.Building
{
    public static class CodeWriterFactoryExtensions
    {
        public static ICodeWriter CreateWriter(this ICodeWriterFactory writerFactory, CodeModel model)
            => writerFactory.CreateWriter(new StringBuilder(), model);
    }
}