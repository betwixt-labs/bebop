using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Lexer.Tokenization.Models;

namespace Core.Meta
{
    public abstract record Expression(Span Span) {
        public abstract long Value();
    }

    public sealed record LiteralExpression(Span Span, long LiteralValue) : Expression(Span)
    {
        public override long Value() => LiteralValue;
    }

    public sealed record ParenthesisExpression(Span Span, Expression Inner) : Expression(Span)
    {
        public override long Value() => Inner.Value();
    }

    public sealed record ShiftLeftExpression(Span Span, Expression Left, Expression Right) : Expression(Span)
    {
        public override long Value() => Left.Value() << (int)Right.Value();
    }

    public sealed record BitwiseAndExpression(Span Span, Expression Left, Expression Right) : Expression(Span)
    {
        public override long Value() => Left.Value() & Right.Value();
    }

    public sealed record BitwiseOrExpression(Span Span, Expression Left, Expression Right) : Expression(Span)
    {
        public override long Value() => Left.Value() | Right.Value();
    }
}
