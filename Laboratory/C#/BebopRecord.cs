using System;
using System.Reflection;
using Bebop.Exceptions;

namespace Bebop
{
    /// <summary>
    ///     A virtual wrapper for a Bebop record
    /// </summary>
    public class BebopRecord
    {
        /// <summary>
        ///     Don't allow this type to be created manually.
        /// </summary>
        protected BebopRecord()
        {
        }

        ///<inheritdoc cref="BebopRecord{T}.Class"/>
        public virtual Type Class { get; } = null!;

        ///<inheritdoc cref="BebopRecord{T}.OpCode"/>
        public virtual int OpCode { get; }

        ///<inheritdoc cref="BebopRecord{T}.Encode"/>
        public virtual byte[] Encode(object message) => throw new NotImplementedException();

        ///<inheritdoc cref="BebopRecord{T}.Encode{T}"/>
        public virtual byte[] Encode<T>(T message) where T : class, new() => throw new NotImplementedException();

        ///<inheritdoc cref="BebopRecord{T}.Decode{T}"/>
        public virtual T Decode<T>(byte[] data) where T : class, new() => throw new NotImplementedException();

        ///<inheritdoc cref="BebopRecord{T}.Decode"/>
        public virtual object Decode(byte[] data) => throw new NotImplementedException();

        ///<inheritdoc cref="BebopRecord{T}.Equals(BebopRecord)"/>
        public virtual bool Equals(BebopRecord? other) => throw new NotImplementedException();
    }

    /// <summary>
    ///     A concrete wrapper for a Bebop record
    /// </summary>
    /// <typeparam name="T">
    ///     The type of a sealed Bebop implementation that was marked with <see cref="BebopMarkerAttribute"/>
    ///     and corresponds to a defined Bebop type
    /// </typeparam>
    public sealed class BebopRecord<T> : BebopRecord, IEquatable<BebopRecord> where T : class, new()
    {
        /// <summary>
        ///     A delegate to the static decode method of <typeparamref name="T"/>
        /// </summary>
        private readonly Func<byte[], T> _decodeDelegate;

        /// <summary>
        ///     A delegate to the static encode method of <typeparamref name="T"/>
        /// </summary>
        private readonly Func<T, byte[]> _encodeDelegate;

        /// <summary>
        ///     Creates a new <see cref="BebopRecord{T}"/> instance
        /// </summary>
        /// <param name="type">The underlying <see cref="Type"/> that belongs to <typeparamref name="T"/></param>
        /// <param name="encodeMethod">
        ///     The <see cref="MethodInfo"/> for static encode method that corresponds to
        ///     <typeparamref name="T"/>
        /// </param>
        /// <param name="decodeMethod">
        ///     The <see cref="MethodInfo"/> for static decode method that corresponds to
        ///     <typeparamref name="T"/>
        /// </param>
        /// <param name="opcode">The opcode constant (if any) that is in <typeparamref name="T"/></param>
        private BebopRecord(Type type, MethodInfo encodeMethod, MethodInfo decodeMethod, int opcode)
        {
            // create delegates to the encode and decode methods so we have a pointer.
            _encodeDelegate = (encodeMethod.CreateDelegate(typeof(Func<T, byte[]>)) as Func<T, byte[]>)!;
            _decodeDelegate = (decodeMethod.CreateDelegate(typeof(Func<byte[], T>)) as Func<byte[], T>)!;
            Class = type;
            OpCode = opcode;
        }

        /// <summary>
        ///     The opcode attribute constant
        /// </summary>
        /// <remarks>
        ///     -1 if no opcode was defined in <typeparamref name="T"/>
        /// </remarks>
        public override int OpCode { get; }

        /// <summary>
        ///     The actual Bebop implementation class type
        /// </summary>
        public override Type Class { get; }

        /// <summary>Determines whether the specified object is equal to the current object.</summary>
        /// <param name="other">The <see cref="BebopRecord"/> to compare with the current object.</param>
        /// <returns>
        ///     <see langword="true"/> if the specified <see cref="BebopRecord"/> is equal to the current <see cref="BebopRecord"/>;
        ///     otherwise, <see langword="false"/>.
        /// </returns>
        public override bool Equals(BebopRecord? other)
            => other is not null && Class == other.Class && OpCode == other.OpCode;

        /// <summary>
        ///     Encodes the specified <paramref name="message"/> into a buffer.
        /// </summary>
        /// <param name="message">An object that aligns with <typeparamref name="T"/></param>
        /// <returns>A buffer containing the encoded bytes of the provided  <paramref name="message"/></returns>
        /// <exception cref="BebopRuntimeException">
        ///     Thrown when the specified <paramref name="message"/> does not align with
        ///     <typeparamref name="T"/>.
        /// </exception>
        public override byte[] Encode(object message) => message is not T m
            ? throw new BebopRuntimeException(typeof(T), message.GetType())
            : _encodeDelegate(m);

        /// <summary>
        ///     Decodes a buffer into <typeparamref name="T"/>
        /// </summary>
        /// <param name="data">The buffer containing the encoded <typeparamref name="T"/> data</param>
        /// <returns>An instance of <typeparamref name="T"/> as an <see cref="object"/></returns>
        public override object Decode(byte[] data) => Decode<T>(data);

        /// <summary>
        ///     Encodes <typeparamref name="T1"/> into a buffer.
        /// </summary>
        /// <typeparam name="T1">A type that aligns with <typeparamref name="T"/></typeparam>
        /// <param name="message">An instance of <typeparamref name="T1"/></param>
        /// <returns>A buffer containing the encoded bytes of the provided  <paramref name="message"/></returns>
        /// <exception cref="BebopRuntimeException">
        ///     Thrown when <typeparamref name="T"/> and <typeparamref name="T1"/> are
        ///     compatible types.
        /// </exception>
        public override byte[] Encode<T1>(T1 message) => message is not T t
            ? throw new BebopRuntimeException(typeof(T), typeof(T1))
            : _encodeDelegate(t);

        /// <summary>
        ///     Decodes a buffer into the specified <typeparamref name="T1"/>
        /// </summary>
        /// <typeparam name="T1">A type that aligns with <typeparamref name="T"/></typeparam>
        /// <param name="data">An encoded <typeparamref name="T1"/> or <typeparamref name="T"/> aligned message</param>
        /// <returns>An instance of <typeparamref name="T1"/></returns>
        /// <exception cref="BebopRuntimeException">
        ///     Thrown when <typeparamref name="T"/> and <typeparamref name="T1"/> are
        ///     compatible types.
        /// </exception>
        public override T1 Decode<T1>(byte[] data) => _decodeDelegate(data) is T1 decoded
            ? decoded
            : throw new BebopRuntimeException(typeof(T), typeof(T1));

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj switch
        {
            BebopRecord type => Equals(type),
            null => false,
            _ => false
        };

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 31 + OpCode.GetHashCode();
                hash = hash * 31 + Class.GetHashCode();
                return hash;
            }
        }
    }
}