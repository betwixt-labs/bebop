using System;
using System.Collections.Immutable;

namespace Bebop.Runtime
{
    /// <summary>
    /// Provides methods for encoding and decoding Bebop records.
    /// </summary>
    public static class BebopSerializer
    {
        // Encoding methods

        /// <summary>
        /// Encodes the specified Bebop record into a byte array.
        /// </summary>
        /// <typeparam name="T">The type of Bebop record to encode, which must inherit from <see cref="BaseBebopRecord"/>.</typeparam>
        /// <param name="record">The Bebop record instance to encode.</param>
        /// <returns>An array of bytes which contain the encoded Bebop record.</returns>
        public static byte[] Encode<T>(T record) where T : BaseBebopRecord
        {
            return record.Encode();
        }

        /// <summary>
        /// Encodes the specified Bebop record into a byte array with an initial capacity.
        /// </summary>
        /// <typeparam name="T">The type of Bebop record to encode, which must inherit from <see cref="BaseBebopRecord"/>.</typeparam>
        /// <param name="record">The Bebop record instance to encode.</param>
        /// <param name="initialCapacity">The initial capacity of the byte array to use for encoding.</param>
        /// <returns>An array of bytes which contain the encoded Bebop record.</returns>
        public static byte[] Encode<T>(T record, int initialCapacity) where T : BaseBebopRecord
        {
            return record.Encode(initialCapacity);
        }

        /// <summary>
        /// Encodes the specified Bebop record into an immutable array of bytes.
        /// </summary>
        /// <typeparam name="T">The type of Bebop record to encode, which must inherit from <see cref="BaseBebopRecord"/>.</typeparam>
        /// <param name="record">The Bebop record instance to encode.</param>
        /// <returns>An immutable array of bytes which contain the encoded Bebop record.</returns>
        public static ImmutableArray<byte> EncodeImmutably<T>(T record) where T : BaseBebopRecord
        {
            return record.EncodeImmutably();
        }

        /// <summary>
        /// Encodes the specified Bebop record into an immutable array of bytes with an initial capacity.
        /// </summary>
        /// <typeparam name="T">The type of Bebop record to encode, which must inherit from <see cref="BaseBebopRecord"/>.</typeparam>
        /// <param name="record">The Bebop record instance to encode.</param>
        /// <param name="initialCapacity">The initial capacity of the immutable array to use for encoding.</param>
        /// <returns>An immutable array of bytes which contain the encoded Bebop record.</returns>
        public static ImmutableArray<byte> EncodeImmutably<T>(T record, int initialCapacity) where T : BaseBebopRecord
        {
            return record.EncodeImmutably(initialCapacity);
        }

        /// <summary>
        /// Encodes the specified Bebop record into the provided buffer.
        /// </summary>
        /// <typeparam name="T">The type of Bebop record to encode, which must inherit from <see cref="BaseBebopRecord"/>.</typeparam>
        /// <param name="record">The Bebop record instance to encode.</param>
        /// <param name="outBuffer">The buffer to encode the record into.</param>
        /// <returns>The number of bytes written into the buffer.</returns>
        public static int EncodeIntoBuffer<T>(T record, byte[] outBuffer) where T : BaseBebopRecord
        {
            return record.EncodeIntoBuffer(outBuffer);
        }

        // Decoding methods

        /// <summary>
        /// Decodes the specified byte array into an instance of the Bebop record <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of Bebop record to decode, which must implement <see cref="BaseBebopRecord"/> and <see cref="IDecodable{T}"/>.</typeparam>
        /// <param name="data">The byte array containing the encoded Bebop record data.</param>
        /// <returns>An instance of the Bebop record <typeparamref name="T"/> decoded from the byte array.</returns>
        public static T Decode<T>(byte[] data) where T : BaseBebopRecord, IDecodable<T>, new()
        {
            return T.Decode(data);
        }

        /// <summary>
        /// Decodes the specified read-only span of bytes into an instance of the Bebop record <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of Bebop record to decode, which must implement <see cref="BaseBebopRecord"/> and <see cref="IDecodable{T}"/>.</typeparam>
        /// <param name="data">The read-only span of bytes containing the encoded Bebop record data.</param>
        /// <returns>An instance of the Bebop record <typeparamref name="T"/> decoded from the read-only span of bytes.</returns>
        public static T Decode<T>(ReadOnlySpan<byte> data) where T : BaseBebopRecord, IDecodable<T>, new()
        {
            return T.Decode(data);
        }

        /// <summary>
        /// Decodes the specified read-only memory of bytes into an instance of the Bebop record <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of Bebop record to decode, which must implement <see cref="BaseBebopRecord"/> and <see cref="IDecodable{T}"/>.</typeparam>
        /// <param name="data">The read-only memory of bytes containing the encoded Bebop record data.</param>
        /// <returns>An instance of the Bebop record <typeparamref name="T"/> decoded from the read-only memory of bytes.</returns>
        public static T Decode<T>(ReadOnlyMemory<byte> data) where T : BaseBebopRecord, IDecodable<T>, new()
        {
            return T.Decode(data);
        }

        /// <summary>
        /// Decodes the specified array segment of bytes into an instance of the Bebop record <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of Bebop record to decode, which must implement <see cref="BaseBebopRecord"/> and <see cref="IDecodable{T}"/>.</typeparam>
        /// <param name="data">The array segment of bytes containing the encoded Bebop record data.</param>
        /// <returns>An instance of the Bebop record <typeparamref name="T"/> decoded from the array segment of bytes.</returns>
        public static T Decode<T>(ArraySegment<byte> data) where T : BaseBebopRecord, IDecodable<T>, new()
        {
            return T.Decode(data);
        }

        /// <summary>
        /// Decodes the specified immutable array of bytes into an instance of the Bebop record <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of Bebop record to decode, which must implement <see cref="BaseBebopRecord"/> and <see cref="IDecodable{T}"/>.</typeparam>
        /// <param name="data">The immutable array of bytes containing the encoded Bebop record data.</param>
        /// <returns>An instance of the Bebop record <typeparamref name="T"/> decoded from the immutable array of bytes.</returns>
        public static T Decode<T>(ImmutableArray<byte> data) where T : BaseBebopRecord, IDecodable<T>, new()
        {
            return T.Decode(data);
        }
    }
}
