using System;
using System.Collections.Generic;
using System.Text;
using Compiler.Meta;

namespace Compiler.Parser.Extensions
{
    public static class TypeExtensions
    {
        private static readonly Dictionary<string, ScalarType> ScalarIndex = new Dictionary<string, ScalarType>
        {
            {"bool", ScalarType.Bool },
            {"byte", ScalarType.Byte },
            {"int", ScalarType.Int },
            {"uint", ScalarType.UInt },
            {"float", ScalarType.Float },
            {"string", ScalarType.String },
            {"guid", ScalarType.Guid }
        };

        public static bool TryParseType(this string identifier, out int typeCode)
        {
            if (ScalarIndex.TryGetValue(identifier, out var type))
            {
                typeCode = (int) type;
                return true;
            }
            typeCode = default;
            return false;
        }
    }
}
