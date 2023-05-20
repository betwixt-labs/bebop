using System;
using System.Globalization;
using System.Linq;
using Core.Meta.Extensions;

namespace Core.Meta.Attributes
{
    /// <summary>
    /// An attribute that uniquely identifies struct and messages
    /// </summary>
    public sealed class OpcodeAttribute : BaseAttribute
    {
        private readonly bool _isNumber;

        public OpcodeAttribute(string value, bool isNumber)
        {
            Name = "opcode";
            _isNumber = isNumber;
            Value = value;
        }

        /// <summary>
        /// Validates if the inner value of the Opcode attribute is either an unsigned integer or FourCC.
        /// </summary>
        /// <returns>A tuple indicating if validation passed and if not a message describing why.</returns>
        public override bool TryValidate(out string message)
        {
            if (string.IsNullOrWhiteSpace(Value))
            {
                message = "No opcode was provided.";
               return false;
            }

            if (_isNumber)
            {
                if (!Value.TryParseUInt(out var result))
                {
                    message = $"Could not parse integer value \"{Value}\" of opcode attribute.";
                    return false;
                }
                Value = $"0x{result:X}";
                message = string.Empty;
                return true;
            }

            switch (Value.Length)
            {
                case 4 when Value.Any(ch => ch > sbyte.MaxValue):
                    message = "FourCC opcodes may only be ASCII";
                    return false;
                case 4:
                {
                    char[] c = Value.ToCharArray();
                    Value = $"0x{(c[3] << 24) | (c[2] << 16) | (c[1] << 8) | c[0]:X}";
                    message = string.Empty;
                    return true;
                }
                default:
                    message = $"\"{Value}\" is not a valid FourCC.";
                    return false;
            }
        }
    }
}
