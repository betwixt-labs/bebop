using System;
using System.Collections.Generic;
using System.Linq;
using Core.Lexer.Tokenization;
using Core.Lexer.Tokenization.Models;
using Core.Meta;

namespace Core.Parser.Extensions
{
    public static class TypeExtensions
    {
        private static readonly Dictionary<string, BaseType> BaseTypeNames = new Dictionary<string, BaseType>
        {
            {"bool", BaseType.Bool},
            {"byte", BaseType.Byte},
            {"uint8", BaseType.Byte},
            {"int16", BaseType.Int16},
            {"uint16", BaseType.UInt16},
            {"int32", BaseType.Int32},
            {"uint32", BaseType.UInt32},
            {"int64", BaseType.Int64},
            {"uint64", BaseType.UInt64},
            {"float32", BaseType.Float32},
            {"float64", BaseType.Float64},
            {"string", BaseType.String},
            {"guid", BaseType.Guid},
            {"date", BaseType.Date},
        };

        public static string BebopName(this BaseType baseType) => BaseTypeNames.First(kv => kv.Value == baseType).Key;

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