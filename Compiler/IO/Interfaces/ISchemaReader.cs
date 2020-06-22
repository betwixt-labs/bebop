using System;
using System.Threading.Tasks;

namespace Compiler.IO.Interfaces
{
    public interface ISchemaReader : IDisposable
    {
        /// <summary>
        ///     Returns the next available character but does not consume it.
        /// </summary>
        /// <returns>
        ///     An integer representing the next character to be read, or -1 if there are no characters to be read.
        /// </returns>
        public int CurrentPosition { get; }

        /// <summary>
        ///     Returns the next available character but does not consume it.
        /// </summary>
        /// <returns>
        ///     An integer representing the next character to be read, or -1 if there are no characters to be read.
        /// </returns>
        public int Peek();

        /// <summary>
        ///     Returns the next available character but does not consume it.
        /// </summary>
        /// <returns>
        ///     An integer representing the next character to be read, or -1 if there are no characters to be read.
        /// </returns>
        public char PeekChar();

        /// <summary>
        ///     Reads the next character from the input stream and advances the character position by one character.
        /// </summary>
        /// <returns>
        ///     A char literal, or \0 if there are no characters to be read.
        /// </returns>
        public char GetChar();
    }
}