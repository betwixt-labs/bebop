// unset

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using Core.Meta;
using Core.Meta.Extensions;
using Core.Meta.Interfaces;

namespace Core.Generators.Rust
{
    public class RustGenerator : BaseGenerator
    {
        const int indentStep = 4;
        
        public RustGenerator(ISchema schema) : base(schema) { }
        
        private string FormatDocumentation(string documentation, int spaces)
        {
            var builder = new IndentedStringBuilder();
            builder.Indent(spaces);
            foreach (var line in documentation.GetLines())
            {
                // TODO: make the docs more friendly to rustdoc by formatting as markdown
                builder.AppendLine($"/// {line}");
            }
            return builder.ToString();
        }
        
        /// <summary>
        /// Generate a Rust type name for the given <see cref="TypeBase"/>.
        /// </summary>
        /// <param name="type">The field type to generate code for.</param>
        /// <returns>The Rust type name.</returns>
        private string TypeName(in TypeBase type)
        {
            switch (type)
            {
                case ScalarType st:
                    return st.BaseType switch
                    {
                        BaseType.Bool => "bool",
                        BaseType.Byte => "u8",
                        BaseType.UInt16 => "u16",
                        BaseType.Int16 => "i16",
                        BaseType.UInt32 => "u32",
                        BaseType.Int32 => "i32",
                        BaseType.UInt64 => "u64",
                        BaseType.Int64 => "i64",
                        BaseType.Float32 => "f32",
                        BaseType.Float64 => "f64",
                        BaseType.String => "String",
                        BaseType.Guid => "u128",
                        BaseType.Date => "bebop::Date",
                        _ => throw new ArgumentOutOfRangeException(st.BaseType.ToString())
                    };
                case ArrayType at:
                    return $"Vec<{TypeName(at.MemberType)}>";
                case MapType mt:
                    return $"std::collections::HashMap<{TypeName(mt.KeyType)}, {TypeName(mt.ValueType)}>";
                case DefinedType dt:
                    return dt.Name;
            }
            throw new InvalidOperationException($"GetTypeName: {type}");
        }

        public override string Compile(Version? languageVersion)
        {
            var builder = new IndentedStringBuilder();
            return builder.ToString();
        }

        public override void WriteAuxiliaryFiles(string outputPath)
        {
            
        }
    }
}
