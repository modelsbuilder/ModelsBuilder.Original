using System.Text;

namespace ZpqrtBnk.ModelsBuilder.Building
{
    public static class CodeWriterFactoryExtensions
    {
        public static ICodeWriter CreateWriter(this ICodeWriterFactory writerFactory, CodeContext context)
            => writerFactory.CreateWriter(new StringBuilder(), context);
    }
}