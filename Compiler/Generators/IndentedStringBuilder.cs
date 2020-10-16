using System;
using System.Collections.Generic;
using System.Text;

namespace Compiler.Generators
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

        public IndentedStringBuilder AppendLine(string line, int addSpaces = 0)
        {
            Builder.AppendLine((new string(' ', Spaces) + line).TrimEnd());
            Indent(addSpaces);
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
