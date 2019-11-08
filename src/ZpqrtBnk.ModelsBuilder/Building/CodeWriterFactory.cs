using System.Text;

namespace ZpqrtBnk.ModelsBuilder.Building
{
    public class CodeWriterFactory : ICodeWriterFactory
    {
        public ICodeWriter CreateWriter(StringBuilder sb, CodeModel model)
        {
            return new CodeWriter(sb, model);
        }
    }
}