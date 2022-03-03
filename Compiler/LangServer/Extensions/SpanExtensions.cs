using Core.Lexer.Tokenization.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace Compiler.LangServer
{
    internal static class SpanExtensions
    {
        public static Range GetRange(this Span span)
        {
            return new Range
            {
                Start = new Position(span.StartLine, span.StartColumn),
                End = new Position(span.EndLine, span.EndColumn),
            };
        }
    }
}
