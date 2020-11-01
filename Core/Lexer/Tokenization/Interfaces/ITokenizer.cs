using System.Collections.Generic;
using Core.IO.Interfaces;
using Core.Lexer.Tokenization.Models;

namespace Core.Lexer.Tokenization.Interfaces
{

    public interface ITokenizer
    {
        Token? TryScan(char surrogate);

        IAsyncEnumerable<Token> TokenStream();

        void AssignReader<T>(T reader) where T : ISchemaReader;
    }
}
