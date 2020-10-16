using System;
using System.Collections.Generic;
using System.Linq;
using Compiler.Lexer.Tokenization;
using Compiler.Lexer.Tokenization.Models;
using Compiler.Meta;
using Compiler.Meta.Interfaces;

namespace Compiler.Parser.Extensions
{
    public static class TypeExtensions
    {
        private static readonly Dictionary<string, BaseType> BaseTypeNames = new Dictionary<string, BaseType>
        {
            {"bool", BaseType.Bool},
            {"byte", BaseType.Byte},
            {"int", BaseType.Int},
            {"uint", BaseType.UInt},
            {"float", BaseType.Float},
            {"string", BaseType.String},
            {"guid", BaseType.Guid},
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

        public static bool TryParseBaseType(this Token token, out BaseType? typeCode)
        {
            if (BaseTypeNames.TryGetValue(token.Lexeme, out BaseType someType))
            {
                typeCode = someType;
                return true;
            }
            typeCode = null;
            return false;
        }
    }
}