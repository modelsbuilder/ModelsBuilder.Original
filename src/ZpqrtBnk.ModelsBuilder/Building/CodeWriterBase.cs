using System;
using System.Text;

namespace Our.ModelsBuilder.Building
{
    /// <summary>
    /// Provides a base class for code writers.
    /// </summary>
    /// <remarks>Manages formatting, indentation, etc.</remarks>
    public abstract class CodeWriterBase
    {
        private readonly StringBuilder _text;
        private readonly CodeWriterBase _origin;
        private int _indent;

        /// <summary>
        /// Initializes a new instance of the <see cref="CodeWriterBase"/> class.
        /// </summary>
        protected CodeWriterBase(StringBuilder text = null)
        {
            _text = text ?? new StringBuilder();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CodeWriterBase"/> class.
        /// </summary>
        protected CodeWriterBase(CodeWriterBase origin)
        {
            _origin = origin ?? throw new ArgumentNullException(nameof(origin));
        }

        /// <summary>
        /// Resets the code writer.
        /// </summary>
        public void Reset()
        {
            if (_origin != null)
                throw new InvalidOperationException();

            _indent = 0;
            _text.Clear();
        }

        /// <summary>
        /// Gets the underlying <see cref="StringBuilder"/> instance.
        /// </summary>
        protected StringBuilder Text => _text ?? _origin.Text;

        /// <inheritdoc />
        public override string ToString() => Text.ToString();

        /// <summary>
        /// Gets the written code.
        /// </summary>
        public string Code => Text.ToString();

        /// <summary>
        /// Gets or sets the indentation string.
        /// </summary>
        public string IndentString { get; set; } = "    ";

        /// <summary>
        /// Gets or sets the newline string.
        /// </summary>
        public string NewLine { get; set; } = "\n";

        /// <summary>
        /// Increments the code indentation.
        /// </summary>
        public void Indent()
        {
            if (_origin == null)
            {
                _indent++;
            }
            else
            {
                _origin.Indent();
            }
        }

        /// <summary>
        /// Decrements the code indentation.
        /// </summary>
        public void Outdent()
        {
            if (_origin == null)
            {
                if (_indent == 0)
                    throw new InvalidOperationException();
                _indent--;
            }
            else
            {
                _origin.Outdent();
            }
        }

        /// <summary>
        /// Writes a text string.
        /// </summary>
        public void Write(string text)
        {
            Text.Append(text);
        }

        /// <summary>
        /// Writes the start of a code block, and increment indentation.
        /// </summary>
        public void WriteBlockStart(string text = null)
        {
            if (text != null)
                WriteIndentLine(text);
            WriteIndentLine("{");
            Indent();
        }

        /// <summary>
        /// Writes the end of a code block, and decrement indentation.
        /// </summary>
        public void WriteBlockEnd()
        {
            Outdent();
            WriteIndentLine("}");
        }

        /// <summary>
        /// Writes an indented text string.
        /// </summary>
        public void WriteIndent(string text = null)
        {
            var indent = _origin?._indent ?? _indent;

            for (var i = 0; i < indent; i++)
                Text.Append(IndentString);
            if (text != null)
                Text.Append(text);
        }

        /// <summary>
        /// Writes an indented text line.
        /// </summary>
        public void WriteIndentLine(string text)
        {
            WriteIndent();
            WriteLine(text);
        }

        /// <summary>
        /// Writes a text line.
        /// </summary>
        public void WriteLine(string text = null)
        {
            if (text != null)
                Text.Append(text);
            Text.Append(NewLine);
        }

        /// <summary>
        /// Writes a text string between fragments of text.
        /// </summary>
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

        /// <summary>
        /// Writes a text line between fragments of text.
        /// </summary>
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

        /// <summary>
        /// Determines whether to write a fragment of text, and writes a text line between fragments of text.
        /// </summary>
        protected bool WriteWithLineBetween(ref bool first, bool condition)
        {
            if (!condition) return false;

            if (!first)
                WriteLine();

            first = false;
            return true;
        }
    }
}