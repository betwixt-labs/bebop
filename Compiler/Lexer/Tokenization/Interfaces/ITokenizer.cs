using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Compiler.IO.Interfaces;
using Compiler.Lexer.Tokenization.Models;

namespace Compiler.Lexer.Tokenization.Interfaces
{

    public interface ITokenizer :  IDisposable
    {
        Token? TryScan(char surrogate);

        IAsyncEnumerable<Token> TokenStream();

        void AssignReader<T>(T reader) where T : ISchemaReader;
    }
}
