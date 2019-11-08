using System.Text;

namespace ZpqrtBnk.ModelsBuilder.Building
{
    public static class CodeFactoryExtensions
    {
        public static ICodeWriter CreateWriter(this ICodeFactory factory, CodeModel model)
            => factory.CreateWriter(new StringBuilder(), model);
    }
}