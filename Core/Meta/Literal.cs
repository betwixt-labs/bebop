using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Core.Lexer.Tokenization.Models;
using Core.Meta.Attributes;
using Core.Meta.Interfaces;
using Core.Meta;

namespace Core.Meta
{
    public abstract record Literal(TypeBase Type, Span Span);
    public record BoolLiteral(TypeBase Type, Span Span, bool Value) : Literal(Type, Span);
    public record IntegerLiteral(TypeBase Type, Span Span, string Value) : Literal(Type, Span);
    public record FloatLiteral(TypeBase Type, Span Span, string Value) : Literal(Type, Span);
    public record StringLiteral(TypeBase Type, Span Span, string Value) : Literal(Type, Span);
    public record GuidLiteral(TypeBase Type, Span Span, Guid Value) : Literal(Type, Span);
    // maybe one day:
    // public record DateLiteral(TypeBase Type, Span Span, DateTime Value) : Literal(Type, Span);
}
