using System.Collections.Generic;
using System.Linq;
using Core.Meta;

namespace Compiler.LangServer
{
    public sealed class Buffer
    {
        public BebopSchema? Schema { get; }
        public int? Version { get; }
        public string Text { get; }
        public List<TextLine> Lines { get; }
        public int Length { get; }

        public Buffer(BebopSchema? schema, string text, int? version)
        {
            Schema = schema;
            Text = text;
            Version = version;
            Lines = TextCursor.Split(text);
            Length = Lines.Sum(x => x.Length + x.LineBreak.Length);
        }
    }
}