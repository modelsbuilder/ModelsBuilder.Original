using System.Text;

namespace ZpqrtBnk.ModelsBuilder.Building
{
    public class CodeWriterFactory : ICodeWriterFactory
    {
        public ICodeWriter CreateWriter(StringBuilder sb, CodeContext context)
        {
            return new CodeWriter(sb, context);
        }
    }
}