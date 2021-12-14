using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Core.Lexer.Tokenization.Models;

namespace Core.Meta
{
    public abstract record Expression(Span Span) {
        public abstract BigInteger Value();
    }

    public sealed record LiteralExpression(Span Span, BigInteger LiteralValue) : Expression(Span)
    {
        public override BigInteger Value() => LiteralValue;
    }

    public sealed record ParenthesisExpression(Span Span, Expression Inner) : Expression(Span)
    {
        public override BigInteger Value() => Inner.Value();
    }

    public sealed record ShiftLeftExpression(Span Span, Expression Left, Expression Right) : Expression(Span)
    {
        public override BigInteger Value() => Left.Value() << (int)Right.Value();
    }

    public sealed record BitwiseAndExpression(Span Span, Expression Left, Expression Right) : Expression(Span)
    {
        public override BigInteger Value() => Left.Value() & Right.Value();
    }

    public sealed record BitwiseOrExpression(Span Span, Expression Left, Expression Right) : Expression(Span)
    {
        public override BigInteger Value() => Left.Value() | Right.Value();
    }
}
