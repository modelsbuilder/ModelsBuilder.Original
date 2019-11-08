using System.Text;

namespace ZpqrtBnk.ModelsBuilder.Building
{
    public interface ICodeWriterFactory
    {
        ICodeWriter CreateWriter(StringBuilder sb, CodeModel model);
    }
}