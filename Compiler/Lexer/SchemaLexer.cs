using System;
using System.Collections.Generic;
using System.IO;
using Compiler.IO;
using Compiler.Lexer.Interfaces;
using Compiler.Lexer.Tokenization;
using Compiler.Lexer.Tokenization.Models;

namespace Compiler.Lexer
{
    public class SchemaLexer : LexerBase<Tokenizer>, IDisposable
    {
        public SchemaLexer()
        {
            Tokenizer = new Tokenizer();
        }

        public void Dispose()
        {
            Tokenizer?.Dispose();
        }

        public override void CreateFileHandle(string schemaFile)
        {
            if (string.IsNullOrWhiteSpace(schemaFile))
            {
                throw new ArgumentNullException(nameof(schemaFile));
            }
            if (!new FileInfo(schemaFile).Exists)
            {
                throw new FileNotFoundException(schemaFile);
            }
            Tokenizer.AssignReader(new SchemaReader(File.OpenRead(schemaFile)));
        }


        public override IAsyncEnumerable<Token> NextToken() => Tokenizer.TokenStream();
    }
}