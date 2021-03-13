using System;
using System.Linq;
using System.Text;
using Core.Meta.Extensions;

namespace Core.Generators
{
    public class IndentedStringBuilder
    {
        private int Spaces { get; set; }
        private StringBuilder Builder { get; }
       

        public IndentedStringBuilder(int spaces = 0)
        {
            Spaces = spaces;
            Builder = new StringBuilder();
        }

        public IndentedStringBuilder AppendLine()
        {
            return AppendLine(Environment.NewLine);
        }

        public IndentedStringBuilder Append(string text)
        {
            var indent = new string(' ', Spaces);
            var lines = text.GetLines();
            var indentedLines = lines.Select(x => (indent + x).TrimEnd()).ToArray();
            var indentedText = string.Join(Environment.NewLine, indentedLines).TrimEnd();
            Builder.Append(indentedText);
            return this;
        }

        public IndentedStringBuilder AppendLine(string text)
        {
            var indent = new string(' ', Spaces);
            var lines = text.GetLines();
            var indentedLines = lines.Select(x => (indent + x).TrimEnd()).ToArray();
            var indentedText = string.Join(Environment.NewLine, indentedLines).TrimEnd();
            Builder.AppendLine(indentedText);
            return this;
        }

        public IndentedStringBuilder Indent(int addSpaces = 0)
        {
            Spaces = Math.Max(0, Spaces + addSpaces);
            return this;
        }

        public IndentedStringBuilder Dedent(int removeSpaces = 0)
        {
            Spaces = Math.Max(0, Spaces - removeSpaces);
            return this;
        }

        public override string ToString()
        {
            return Builder.ToString();
        }
    }
}
