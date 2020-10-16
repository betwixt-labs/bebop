using System.Collections.Generic;
using Compiler.Lexer.Tokenization.Interfaces;
using Compiler.Lexer.Tokenization.Models;

namespace Compiler.Lexer.Interfaces
{
    /// <summary>
    /// </summary>
    public abstract class LexerBase<TTokenizer> where TTokenizer : ITokenizer
    {
        private protected TTokenizer Tokenizer { get; set; }
        public abstract void CreateMemoryHandle(string schema);
        public abstract void CreateFileHandle(string schemaFile);
        public abstract IAsyncEnumerable<Token> TokenStream();
    }
}