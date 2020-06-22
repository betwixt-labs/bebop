namespace Compiler.Meta
{
    /// <summary>
    ///     Pierogi scalar types are aligned with native types found in most programming languages. All scalar types support
    ///     being used as an array.
    /// </summary>
    /// <remarks>
    ///     A scalar type can only contain a "single" value whereas an <see cref="AggregateKind"/> can be constructed from
    ///     multiple scalars (and possibly references to other aggregate kinds).
    /// </remarks>
    // ReSharper disable once EnumUnderlyingTypeIsInt
    public enum ScalarType : int
    {
        /// <summary>
        ///     A simple type representing Boolean values of true or false.
        /// </summary>
        Bool = -1,

        /// <summary>
        ///     An integral type representing unsigned 8-bit integers with values between 0 and 255.
        /// </summary>
        Byte = -2,

        /// <summary>
        ///     An integral type representing unsigned 32-bit integers with values between 0 and 4294967295.
        /// </summary>
        /// <remarks>
        ///     Uses variable-length encoding.
        /// </remarks>
        UInt = -3,

        /// <summary>
        ///     An integral type representing signed 32-bit integers with values between -2147483648 and 2147483647.
        /// </summary>
        /// <remarks>
        ///     Uses variable-length encoding.
        /// </remarks>
        Int = -4,

        /// <summary>
        ///     A floating point type representing values ranging from approximately 5.0 x 10 <sup>-324</sup> to 1.7 x 10
        ///     <sup>308</sup> with a precision of 15-16 digits.
        /// </summary>
        Float = -5,

        /// <summary>
        ///     A UTF-8 encoded null-terminated string.
        /// </summary>
        String = -6,

        /// <summary>
        ///     GUID (or UUID) is an acronym for 'Globally Unique Identifier' (or 'Universally Unique Identifier').
        ///     It is a 128-bit integer number used to identify resources.
        /// </summary>
        Guid = -7
    }

    /// <summary>
    ///     Aggregate kinds are data structures that can be constructed from multiple <see cref="ScalarType"/> members, and references to other aggregate kinds.
    /// </summary>
    /// <remarks>
    ///     Wikipedia says, "In type theory, a kind is the type of a type constructor or, less commonly, the type of a
    ///     higher-order type operator. A kind system is essentially a simply typed lambda calculus 'one level up,' endowed
    ///     with a primitive type, denoted * and called 'type', which is the kind of any (monomorphic) data type."
    ///     <para/>
    ///     In layman's terms, a kind is an arity specifier, and the members defined in an <see cref="AggregateKind"/> make up
    ///     an actual Type.
    ///     <para/>
    /// </remarks>
    public enum AggregateKind : uint
    {
        /// <summary>
        ///     An enumeration type (or enum type) is a type defined by a set of named constants.
        ///     It is restricted to <see cref="ScalarType.UInt"/> integral numeric type.
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
        ///     Members of the message may be any valid Pierogi type such as
        ///     <see cref="ScalarType"/>,
        ///     <see cref="Enum"/>,
        ///     <see cref="Struct"/>,
        ///     <see cref="Message"/>,
        ///     and arrays of <see cref="T:T[]"/>.
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