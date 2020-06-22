using System;
using System.Collections.Generic;
using System.Text;
using Compiler.Meta.Interfaces;

namespace Compiler.Meta
{
    public readonly struct Field : IField
    {
        public string Name { get; }
        public int TypeCode { get; }
        public uint Line { get; }
        public uint Column { get; }
        public bool IsArray { get; }
        public bool IsDeprecated { get; }
        public uint ConstantValue { get; }
    }
}
