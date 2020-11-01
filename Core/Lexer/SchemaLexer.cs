using System.Collections.Generic;
using Core.IO;
using Core.IO.Interfaces;
using Core.Lexer.Interfaces;
using Core.Lexer.Tokenization;
using Core.Lexer.Tokenization.Models;

namespace Core.Lexer
{
    public class SchemaLexer : LexerBase<Tokenizer>
    {
        private SchemaLexer(ISchemaReader schemaReader) : base(new Tokenizer(schemaReader)) { }


        public static SchemaLexer FromTextualSchema(string textualSchema)
        {
            return new SchemaLexer(SchemaReader.FromTextualSchema(textualSchema));
        }

        public static SchemaLexer FromSchemaPaths(IEnumerable<string> schemaPaths)
        {
            return new SchemaLexer(SchemaReader.FromSchemaPaths(schemaPaths));
        }

        public override IAsyncEnumerable<Token> TokenStream() => Tokenizer.TokenStream();
    }
}