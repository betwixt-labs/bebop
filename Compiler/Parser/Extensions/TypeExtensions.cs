using System;
using System.Collections.Generic;
using System.Linq;
using Compiler.Lexer.Tokenization;
using Compiler.Lexer.Tokenization.Models;
using Compiler.Meta;

namespace Compiler.Parser.Extensions
{
    public static class TypeExtensions
    {
        private static readonly Dictionary<string, int> TypeIndex = new Dictionary<string, int>
        {
            {"bool", (int) ScalarType.Bool},
            {"byte", (int) ScalarType.Byte},
            {"int", (int) ScalarType.Int},
            {"uint", (int) ScalarType.UInt},
            {"float", (int) ScalarType.Float},
            {"string", (int) ScalarType.String},
            {"guid", (int) ScalarType.Guid}
        };

        public static int FindToken(this Token[] tokens, Func<KeyValuePair<Token, int>, bool> predicate)
        {
            try
            {
                return tokens
                    .Select((token, index) => new KeyValuePair<Token, int>(token, index))
                    .First(predicate)
                    .Value;
            }
            catch (InvalidOperationException)
            {
                return -1;
            }
        }

        public static bool IsAggregateKind(this Token token, out AggregateKind? kind)
        {
            kind = token.Kind switch
            {
                TokenKind.Struct => AggregateKind.Struct,
                TokenKind.Enum => AggregateKind.Enum,
                TokenKind.Message => AggregateKind.Message,
                _ => null
            };
            return token.Kind switch
            {
                TokenKind.Struct => true,
                TokenKind.Message => true,
                TokenKind.Enum => true,
                _ => false
            };
        }

        public static bool TryParseType(this Token token, out int typeCode)
        {
            if (TypeIndex.TryGetValue(token.Lexeme, out typeCode))
            {
                return true;
            }
            typeCode = default;
            return false;
        }
    }
}