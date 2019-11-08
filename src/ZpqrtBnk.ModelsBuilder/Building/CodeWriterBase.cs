using System.Text;

namespace ZpqrtBnk.ModelsBuilder.Building
{
    /// <summary>
    /// Provides a base class for code writers.
    /// </summary>
    /// <remarks>Manages formatting, indentation, etc.</remarks>
    public abstract class CodeWriterBase
    {
        private int _indent;

        protected CodeWriterBase(StringBuilder text)
        {
            Text = text;
        }

        public void Reset()
        {
            _indent = 0;
            Text.Clear();
        }

        protected StringBuilder Text { get; }

        public override string ToString() => Text.ToString();

        public string Code => Text.ToString();

        public string IndentString { get; set; } = "    ";

        public string NewLine { get; set; } = "\n";

        public void Indent() { _indent++; }

        public void Outdent() { _indent--; }

        public void Write(string value)
        {
            Text.Append(value);
        }

        public void WriteBlockStart(string text = null)
        {
            if (text != null)
                WriteIndentLine(text);
            WriteIndentLine("{");
            Indent();
        }

        public void WriteBlockEnd()
        {
            Outdent();
            WriteIndentLine("}");
        }

        public void WriteIndent(string text = null)
        {
            for (var i = 0; i < _indent; i++)
                Text.Append(IndentString);
            if (text != null)
                Text.Append(text);
        }

        public void WriteIndentLine(string text)
        {
            WriteIndent();
            WriteLine(text);
        }

        public void WriteLine(string text = null)
        {
            if (text != null)
                Text.Append(text);
            Text.Append(NewLine);
        }

        public void WriteBetween(ref bool first, string text)
        {
            if (first)
            {
                first = false;
            }
            else
            {
                Text.Append(text);
            }
        }

        public void WriteLineBetween(ref bool first, string text = null)
        {
            if (first)
            {
                first = false;
            }
            else
            {
                if (text != null)
                    Text.Append(text);
                Text.Append(NewLine);
            }
        }
    }
}