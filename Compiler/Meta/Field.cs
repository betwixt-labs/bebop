using System;
using System.Collections.Generic;
using System.Text;
using Compiler.Meta.Interfaces;

namespace Compiler.Meta
{
    public readonly struct Field : IField
    {
        public Field(string name,
            in int type,
            in uint line,
            in uint column,
            in bool isArray,
            in bool isDeprecated,
            in uint constantValue)
        {
            Name = name;
            TypeCode = type;
            Line = line;
            Column = column;
            IsArray = isArray;
            IsDeprecated = isDeprecated;
            ConstantValue = constantValue;
        }

        public string Name { get; }
        public int TypeCode { get; }
        public uint Line { get; }
        public uint Column { get; }
        public bool IsArray { get; }
        public bool IsDeprecated { get; }
        public uint ConstantValue { get; }
    }
}
