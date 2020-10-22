using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Compiler.IO;
using Compiler.IO.Interfaces;
using Compiler.Lexer.Interfaces;
using Compiler.Lexer.Tokenization;
using Compiler.Lexer.Tokenization.Models;

namespace Compiler.Lexer
{
    public class SchemaLexer : LexerBase<Tokenizer>, IDisposable
    {
        private SchemaLexer(ISchemaReader schemaReader) : base(new Tokenizer(schemaReader)) { }


        public static SchemaLexer FromTextualSchema(string textualSchema)
        {
            return new SchemaLexer(new SchemaReader(new MemoryStream(Encoding.UTF8.GetBytes(textualSchema)), "(unknown)"));
        }

        public static SchemaLexer FromSchemaPath(string schemaPath)
        {
            return new SchemaLexer(new SchemaReader(File.OpenRead(schemaPath), schemaPath));
        }

        public void Dispose()
        {
            Tokenizer?.Dispose();
        }

        public override IAsyncEnumerable<Token> TokenStream() => Tokenizer.TokenStream();
    }
}