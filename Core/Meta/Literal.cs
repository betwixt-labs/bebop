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
    /// <summary>
    /// A base class for literal values in a schema.
    /// </summary>
    public abstract class Literal<T>
    {
        protected Literal(T value, Type type, Span span)
        {
            Value = value;
            Type = type;
            Span = span;
        }

        /// <summary>
        /// The value of the literal.
        /// </summary>
        public T Value { get; }
        /// <summary>
        /// The Bebop type of the literal.
        /// </summary>
        public Type Type { get; }
        /// <summary>
        /// The span where the literal was found.
        /// </summary>
        public Span Span { get; }
    }

    public class BoolLiteral : Literal<bool>
    {
        public BoolLiteral(bool value, Type type, Span span)
            : base(value, type, span) {}
    }

    public class SignedIntegerLiteral : Literal<long>
    {
        public SignedIntegerLiteral(long value, Type type, Span span)
            : base(value, type, span) {}
    }

    public class UnsignedIntegerLiteral : Literal<ulong>
    {
        public UnsignedIntegerLiteral(ulong value, Type type, Span span)
            : base(value, type, span) {}
    }

    public class FloatLiteral : Literal<double>
    {
        public FloatLiteral(double value, Type type, Span span)
            : base(value, type, span) {}
    }

    public class StringLiteral : Literal<string>
    {
        public StringLiteral(string value, Type type, Span span)
            : base(value, type, span) {}
    }

    public class GuidLiteral : Literal<Guid>
    {
        public GuidLiteral(Guid value, Type type, Span span)
            : base(value, type, span) {}
    }

    public class DateLiteral : Literal<DateTime>
    {
        public DateLiteral(DateTime value, Type type, Span span)
            : base(value, type, span) {}
    }
}
