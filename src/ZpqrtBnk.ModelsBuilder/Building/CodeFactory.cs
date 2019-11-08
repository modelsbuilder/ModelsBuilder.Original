using System.Text;

namespace ZpqrtBnk.ModelsBuilder.Building
{
    public class CodeFactory : ICodeFactory
    {
        public CodeModel CreateModel()
        {
            return new CodeModel();
        }

        public ICodeWriter CreateWriter(StringBuilder sb, CodeModel model)
        {
            return new CodeWriter(sb, model);
        }
    }
}