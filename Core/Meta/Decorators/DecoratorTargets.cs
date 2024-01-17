using System;

namespace Core.Meta.Decorators;

[System.Flags]
public enum DecoratorTargets
{
    None = 0,
    Enum = 1 << 0,    // 1
    Message = 1 << 1, // 2
    Struct = 1 << 2,  // 4
    Union = 1 << 3,   // 8
    Field = 1 << 4,   // 16
    Service = 1 << 5, // 32
    Method = 1 << 6,  // 64
    All = Enum | Message | Struct | Union | Field | Service | Method // Combines all flags
}