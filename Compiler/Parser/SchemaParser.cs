using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Compiler.Exceptions;
using Compiler.Lexer;
using Compiler.Lexer.Tokenization;
using Compiler.Lexer.Tokenization.Models;
using Compiler.Meta;
using Compiler.Meta.Interfaces;
using Compiler.Parser.Extensions;

namespace Compiler.Parser
{
    public class SchemaParser
    {
        private readonly SchemaLexer _lexer;
        private Token[] _tokens;

        public SchemaParser(string file)
        {
            _lexer = new SchemaLexer();
            _lexer.CreateFileHandle(file);
        }

        private async Task Tokenize()
        {
            var collection = new List<Token>();
            await foreach (var token in _lexer.NextToken())
            {
                collection.Add(token);
            }
            _tokens = collection.ToArray();
        }


        public async Task Evaluate()
        {
            await Tokenize();
            var index = 0;


            Token Current()
            {
                return _tokens[index];
            }

            bool Eat(TokenKind kind)
            {
                if (Current().Kind == kind)
                {
                    index++;
                    return true;
                }
                return false;
            }

            void Expect(TokenKind kind)
            {
                if (!Eat(kind))
                {
                    var token = Current();
                    throw new SchemaParseException($"Expected {kind} but found {token.Lexeme} ({token.Kind})");
                }
            }

            var package = string.Empty;

            if (Eat(TokenKind.Package))
            {
                package = Current().Lexeme;
                Console.WriteLine(Current().Lexeme);
                Expect(TokenKind.Identifier);
                Expect(TokenKind.Semicolon);
            }

            var definitions = new List<IDefinition>();

            while (index < _tokens.Length && !Eat(TokenKind.EndOfFile))
            {
                AggregateKind? kind;
                if (Eat(TokenKind.Enum)) kind = AggregateKind.Enum;
                else if (Eat(TokenKind.Struct)) kind = AggregateKind.Struct;
                else if (Eat(TokenKind.Message)) kind = AggregateKind.Message;
                else throw new SchemaParseException(
                    $"Expected AggregateKind but found {Current().Lexeme} ({Current().Kind})");


                var name = Current();
                Expect(TokenKind.Identifier);
                Expect(TokenKind.OpenBrace);

                while (!Eat(TokenKind.CloseBrace))
                {
                    int typeCode;
                    bool isArray;
                    bool isDeprecated;

                    if (kind != AggregateKind.Enum)
                    {
                       
                        // try and parse the type identifier 
                        if (!Current().Lexeme.TryParseType(out typeCode))
                        {
                            Console.WriteLine(Current().Lexeme);
                            // the identifier might be a generic type, so let's check if it was previously defined.
                            var typeIndex = definitions.FindIndex(a => a.Name.Equals(Current().Lexeme, StringComparison.Ordinal));
                            
                            if (typeIndex == -1)
                            {
                              
                                // the aggregate type hasn't been parsed yet, so we need to look ahead.
                                // I could just force schemas to be written out in the order types are required, but I'm not a coward and God gave us indexes for a reason.
                                var aggregateIndex = Array.FindIndex(_tokens, t => t.Lexeme.Equals(Current().Lexeme) && t.Position != Current().Position);
                                // but I'll do that later zzz
                                if (aggregateIndex == -1)
                                {
                                    throw new SchemaParseException(
                                        $"{Current().Lexeme} is not a defined aggregate type.");
                                }
                            }
                        }
                        Expect(TokenKind.Identifier);
                        isArray = Eat(TokenKind.OpenBrace) && Eat(TokenKind.CloseBrace);
                       
                    }
                }
            }
        }
    }
}