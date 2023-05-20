namespace Core.Parser
{
    /// <summary>
    /// Represents the type of Tempo method.
    /// Each method type corresponds to a different communication model.
    /// </summary>
    public enum MethodType
    {
        /// <summary>
        /// Unary method type.
        /// Represents a method where the client sends a single request and receives a single response.
        /// </summary>
        Unary,

        /// <summary>
        /// ServerStream method type.
        /// Represents a method where the client sends a single request and receives a stream of responses.
        /// </summary>
        ServerStream,

        /// <summary>
        /// ClientStream method type.
        /// Represents a method where the client sends a stream of requests and receives a single response.
        /// </summary>
        ClientStream,

        /// <summary>
        /// DuplexStream method type.
        /// Represents a method where the client and server send a stream of messages to each other simultaneously.
        /// </summary>
        DuplexStream,
    }

    internal static class RpcSchema
    {
        public static string GetMethodTypeName(MethodType type) {
            return type switch
            {
                MethodType.Unary => "Unary",
                MethodType.ServerStream => "ServerStream",
                MethodType.ClientStream => "ClientStream",
                MethodType.DuplexStream => "DuplexStream",
                _ => throw new System.ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }

        // Constants used in the hash algorithm for good distribution properties.
        private const uint Seed = 0x5AFE5EED;
        private const uint C1 = 0xcc9e2d51;
        private const uint C2 = 0x1b873593;
        private const uint N = 0xe6546b64;

        /// <summary>
        /// Gets the unique ID of a method
        /// </summary>
        /// <param name="serviceName">The name of the service the method is on.</param>
        /// <param name="methodName">The name of the method</param>
        /// <returns>A unique unsigned 32-bit integer.</returns>
        public static uint GetMethodId(string serviceName, string methodName)
        {
            var path = $"/${serviceName}/${methodName}";
            return HashString(path);
        }


        /// <summary>
        /// Computes a 32-bit hash of the given string.
        /// </summary>
        /// <param name="input">The string to hash.</param>
        /// <returns>A 32-bit hash value.</returns>
        /// <remarks>The hash is based on MurmurHash3 with some new finalization constants used to reduce collisions</remarks>
        private static uint HashString(string input)
        {
            // The length of the input string.
            int length = input.Length;
            // The current index in the input string.
            int currentIndex = 0;
            // Initialize the hash value using the seed constant.
            uint hash = Seed;

            // Process 4 characters at a time
            while (currentIndex + 4 <= length)
            {
                // Combine the 4 characters into a 32-bit unsigned integer (block).
                uint block = ((uint)input[currentIndex] & 0xFF) |
                             (((uint)input[currentIndex + 1] & 0xFF) << 8) |
                             (((uint)input[currentIndex + 2] & 0xFF) << 16) |
                             (((uint)input[currentIndex + 3] & 0xFF) << 24);

                // Update the block with the constants C1 and C2.
                block *= C1;
                block = (block << 15) | (block >> 17); // Rotate left by 15
                block *= C2;

                // Update the hash value with the calculated block.
                hash ^= block;
                hash = (hash << 13) | (hash >> 19); // Rotate left by 13
                hash = hash * 5 + N;

                // Move to the next group of 4 characters.
                currentIndex += 4;
            }

            // Process the remaining characters
            uint tail = 0;
            int remaining = length - currentIndex;

            // Process the remaining characters based on the number left.
            switch (remaining)
            {
                case 3:
                    tail |= (uint)(input[currentIndex + 2] & 0xFF) << 16;
                    goto case 2;
                case 2:
                    tail |= (uint)(input[currentIndex + 1] & 0xFF) << 8;
                    goto case 1;
                case 1:
                    tail |= (uint)(input[currentIndex] & 0xFF);
                    tail *= C1;
                    tail = (tail << 15) | (tail >> 17); // Rotate left by 15
                    tail *= C2;
                    hash ^= tail;
                    break;
            }

            // Finalization step to mix the hash value.
            // This is where we deviate from MurmurHash3
            hash ^= hash >> 16;
            hash *= 0x7feb352d;
            hash ^= hash >> 15;
            hash *= 0x846ca68b;
            hash ^= hash >> 16;

            // Return the final 32-bit hash value.
            return hash;
        }
    }
}