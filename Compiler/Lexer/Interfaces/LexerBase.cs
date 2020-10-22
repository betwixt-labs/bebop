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

        protected LexerBase(TTokenizer tokenizer)
        {
            Tokenizer = tokenizer;
        }

        public abstract IAsyncEnumerable<Token> TokenStream();
    }
}