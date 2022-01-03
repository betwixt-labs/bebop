using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using Core.Lexer.Extensions;

namespace Core.Meta
{
    public static class ReservedWords
    {
        public const string CompilerName = "bebopc";
        public const string SchemaExt = "bop";

        public static HashSet<string> Identifiers = new()
        {
            "BebopView",
            "BebopReader",
            "BebopWriter",
            "BebopRecord",
            "BebopMirror",
            "BebopConstants",
            "BopConstants"
        };
    }

    /// <summary>
    ///     Bebop base types are aligned with native types found in most programming languages.
    /// </summary>
    public enum BaseType
    {
        /// <summary>
        ///     A simple type representing Boolean values of true or false.
        ///     It is encoded as a single byte (false is 0, true is 1).
        /// </summary>
        Bool,

        /// <summary>
        ///     An integral type representing unsigned 8-bit integers with values between 0 and 255.
        ///     It is encoded as a single byte.
        /// </summary>
        Byte,

        /// <summary>
        ///     An integral type representing unsigned 16-bit integers with values between 0 and 65535.
        /// </summary>
        UInt16,

        /// <summary>
        ///     An integral type representing signed 16-bit integers with values between -32768 and 32767.
        /// </summary>
        Int16,

        /// <summary>
        ///     An integral type representing unsigned 32-bit integers with values between 0 and 4294967295.
        /// </summary>
        UInt32,

        /// <summary>
        ///     An integral type representing signed 32-bit integers with values between -2147483648 and 2147483647.
        /// </summary>
        Int32,

        /// <summary>
        ///     An integral type representing unsigned 64-bit integers with values between 0 and 2^64-1.
        /// </summary>
        UInt64,

        /// <summary>
        ///     An integral type representing signed 64-bit integers with values between -2^63 and 2^63-1.
        /// </summary>
        Int64,

        /// <summary>
        ///     A 32-bit (single-precision) IEEE 754 floating point number.
        ///     It is encoded as 4 bytes.
        /// </summary>
        Float32,

        /// <summary>
        ///     A 64-bit (double-precision) IEEE 754 floating point number.
        ///     It is encoded as 8 bytes.
        /// </summary>
        Float64,

        /// <summary>
        ///     A UTF-8 encoded null-terminated string.
        /// </summary>
        String,

        /// <summary>
        ///     GUID (or UUID) is an acronym for 'Globally Unique Identifier' (or 'Universally Unique Identifier').
        ///     It is a 128-bit integer number used to identify resources.
        ///     It is encoded as 16 bytes (as returned by <see cref="System.Guid.ToByteArray"/>).
        /// </summary>
        Guid,

        /// <summary>
        ///    A UTC-based date, stored as a 62-bit number of 100-nanosecond "ticks" since 00:00 on January 1 of year 1 AD.
        ///    It is stored as a 64-bit unsigned integer whose top two bits are to be ignored.
        ///    This binary format is compatible with C#'s DateTime.ToBinary().
        /// </summary>
        Date,
    }

    public static class BaseTypeHelpers
    {
        public static BigInteger MinimumInteger(BaseType type)
        {
            switch (type)
            {
                case BaseType.Byte:
                case BaseType.UInt16:
                case BaseType.UInt32:
                case BaseType.UInt64:
                    return 0;
                case BaseType.Int16:
                    return -(BigInteger.One << 15);
                case BaseType.Int32:
                    return -(BigInteger.One << 31);
                case BaseType.Int64:
                    return -(BigInteger.One << 63);
                default:
                    throw new ArgumentException("MinimumInteger: non-integer type");
            }
        }

        public static BigInteger MaximumInteger(BaseType type)
        {
            switch (type)
            {
                case BaseType.Byte:
                    return (BigInteger.One << 8) - 1;
                case BaseType.UInt16:
                    return (BigInteger.One << 16) - 1;
                case BaseType.UInt32:
                    return (BigInteger.One << 32) - 1;
                case BaseType.UInt64:
                    return (BigInteger.One << 64) - 1;
                case BaseType.Int16:
                    return (BigInteger.One << 15) - 1;
                case BaseType.Int32:
                    return (BigInteger.One << 31) - 1;
                case BaseType.Int64:
                    return (BigInteger.One << 63) - 1;
                default:
                    throw new ArgumentException("MaximumInteger: non-integer type");
            }
        }

        public static bool InRange(BaseType type, BigInteger value)
        {
            return value >= MinimumInteger(type) && value <= MaximumInteger(type);
        }
    }

    /// <summary>
    ///     Aggregate kinds are data structures that can be constructed from multiple <see cref="ScalarType"/> members, and references to other aggregate kinds.
    /// </summary>
    public enum AggregateKind : uint
    {
        /// <summary>
        ///     An enumeration type (or enum type) is a type defined by a set of named constants.
        ///     It is restricted to the <see cref="BaseType.UInt32"/> integral numeric type.
        /// </summary>
        /// <remarks>
        ///     It is possible to add new members to an enum in use by a <see cref="Message"/> while maintaining backwards
        ///     compatibility.
        /// </remarks>
        Enum = 0,

        /// <summary>
        ///     A structure type (or struct type) is a type that can encapsulate data. All members are guaranteed to be present.
        ///     The members of the struct are laid out sequentially, and are stored in the order in which they appear.
        /// </summary>
        /// <remarks>
        ///     It is not possible for new members to be added to a struct once it is in use by a <see cref="Message"/>.
        ///     <para/>
        ///     Structures should be used when there is a need for performance or a guarantee data is available.
        /// </remarks>
        Struct = 1,

        /// <summary>
        ///     The message type is a data structure that combines state (members) as a single type-safe unit. All members of a
        ///     message are optional.
        ///     <para/>
        ///     Members of the message may be any valid Bebop type.
        ///     <para/>
        /// </summary>
        /// <remarks>
        ///     It is possible to add new members to a members a <see cref="Message"/> while maintaining backwards comparability.
        ///     <para/>
        ///     Messages should be used to model more complex behavior, or data that is intended to be modified.
        /// </remarks>
        Message = 2
    }
}