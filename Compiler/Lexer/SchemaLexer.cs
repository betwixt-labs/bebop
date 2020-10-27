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