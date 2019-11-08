using System.Text;

namespace ZpqrtBnk.ModelsBuilder.Building
{
    public interface ICodeFactory
    {
        CodeModel CreateModel();

        ICodeWriter CreateWriter(StringBuilder sb, CodeModel model);
    }
}