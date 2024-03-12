using System;
using System.Globalization;
using System.Linq;
using Core.Meta.Extensions;

namespace Core.Meta.Decorators
{

    public sealed record OpcodeDecorator() : DecoratorDefinition("opcode", "A decorator that uniquely identifies a struct or messages.",
    DecoratorTargets.Message | DecoratorTargets.Struct | DecoratorTargets.Union, false,
    new DecoratorParameter[]
    {
        new("fourcc", "The value of the opcode.", BaseType.String, true, null, null),
    })
    {
        public override bool TryValidate(out string reason, SchemaDecorator? schemaDecorator = null)
        {
            if (schemaDecorator is null)
            {
                reason = "Schema decorator is null.";
                return false;
            }
            if (!schemaDecorator.Arguments.TryGetValue("fourcc", out var fourcc))
            {
                reason = "No value found for 'fourcc'.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(fourcc))
            {
                reason = "The fourcc cannot be an empty string.";
                return false;
            }
            if (fourcc.TryParseUInt(out var result))
            {
                schemaDecorator.Arguments["fourcc"] = $"0x{result:X}";
                reason = string.Empty;
                return true;
            }
            switch (fourcc.Length)
            {
                case 4 when fourcc.Any(ch => ch > sbyte.MaxValue):
                    reason = "FourCC opcodes may only be ASCII";
                    return false;
                case 4:
                    {
                        char[] c = fourcc.ToCharArray();
                        schemaDecorator.Arguments["fourcc"] = $"0x{(c[3] << 24) | (c[2] << 16) | (c[1] << 8) | c[0]:X}";
                        reason = string.Empty;
                        return true;
                    }
                default:
                    reason = $"\"{fourcc}\" is not a valid FourCC.";
                    return false;
            }
        }
    }
}
