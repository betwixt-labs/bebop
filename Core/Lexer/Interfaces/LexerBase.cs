using System.Collections.Generic;
using Core.Lexer.Tokenization.Interfaces;
using Core.Lexer.Tokenization.Models;

namespace Core.Lexer.Interfaces
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