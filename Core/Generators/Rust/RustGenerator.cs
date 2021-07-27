﻿// unset

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using Core.Exceptions;
using Core.Meta;
using Core.Meta.Attributes;
using Core.Meta.Extensions;
using Core.Meta.Interfaces;

namespace Core.Generators.Rust
{
    enum OwnershipType
    {
        // name used when borrowed, E.g. `&'raw [u8]` or `&'raw str`. 
        Borrowed,

        // name used when owned, E.g. `Vec<u8>` or `String`.  
        Owned,

        // name used when declared as a global constant, E.g. `&[u8]` or `&str`.
        Constant
    }

    public class RustGenerator : BaseGenerator
    {
        #region state

        const int _tab = 4;

        private static readonly string[] _reservedWordsArray =
        {
            "Self", "abstract", "as", "bebop", "become", "box", "break", "const", "continue", "crate", "do", "else",
            "enum", "extern", "false", "final", "fn", "for", "if", "impl", "in", "let", "loop", "macro", "match",
            "mod", "move", "mut", "override", "priv", "pub", "ref", "return", "self", "static", "std", "struct",
            "super", "trait", "true", "try", "type", "typeof", "unsafe", "unsized", "use", "virtual", "where",
            "while", "yield",
        };

        private static readonly HashSet<string> _reservedWords = RustGenerator._reservedWordsArray.ToHashSet();
        private Dictionary<string, bool> _needsLifetime = new Dictionary<string, bool>();

        #endregion

        #region entrypoints

        public RustGenerator(ISchema schema) : base(schema) { }

        public override string Compile(Version? languageVersion)
        {
            var builder = new IndentedStringBuilder()
                .AppendLine(GeneratorUtils.GetMarkdownAutoGeneratedNotice())
                .AppendLine()
                .AppendLine("use std::io::Write as _;")
                .AppendLine();

            // TODO: do we need to do something with the namespace? Probably not since the file is itself a module.

            foreach (var definition in Schema.Definitions.Values)
            {
                WriteDocumentation(builder, definition.Documentation);
                switch (definition)
                {
                    case ConstDefinition cd:
                        WriteConstDefinition(builder, cd);
                        break;
                    case EnumDefinition ed:
                        WriteEnumDefinition(builder, ed);
                        break;
                    case MessageDefinition md:
                        WriteMessageDefinition(builder, md);
                        break;
                    case StructDefinition sd:
                        WriteStructDefinition(builder, sd);
                        break;
                    case UnionDefinition ud:
                        WriteUnionDefinition(builder, ud);
                        break;
                    default:
                        throw new InvalidOperationException($"unsupported definition {definition.GetType()}");
                        break;
                }

                builder.AppendLine();
            }

            return builder.ToString();
        }

        public override void WriteAuxiliaryFiles(string outputPath)
        {
            // Nothing to do because the runtime is a cargo package.
        }

        #endregion

        #region definition_writers

        private void WriteConstDefinition(IndentedStringBuilder builder, ConstDefinition d)
        {
            builder.AppendLine(
                $"pub const {MakeConstIdent(d.Name)}: {TypeName(d.Value.Type, OwnershipType.Constant)} = {EmitLiteral(d.Value)};");
        }

        private void WriteEnumDefinition(IndentedStringBuilder builder, EnumDefinition d)
        {
            // main definition
            var name = MakeDefIdent(d.Name);
            builder
                .AppendLine("#[repr(u32)]")
                .AppendLine("#[derive(Copy, Clone, Debug, Eq, PartialEq)]")
                .CodeBlock($"pub enum {name}", _tab, () =>
                {
                    foreach (var m in d.Members)
                    {
                        WriteDocumentation(builder, m.Documentation);
                        WriteDeprecation(builder, m.DeprecatedAttribute);
                        builder.AppendLine($"{MakeEnumVariantIdent(m.Name)} = {m.ConstantValue},");
                    }
                }).AppendLine();

            // conversion from int
            builder.CodeBlock($"impl core::convert::TryFrom<u32> for {name}", _tab, () =>
            {
                builder.AppendLine("type Error = bebop::DeserializeError;")
                    .AppendLine()
                    .CodeBlock("fn try_from(value: u32) -> bebop::DeResult<Self>", _tab, () =>
                    {
                        builder.CodeBlock("match value", _tab, () =>
                        {
                            foreach (var m in d.Members)
                            {
                                builder.AppendLine($"{m.ConstantValue} => Ok({name}::{MakeEnumVariantIdent(m.Name)}),");
                            }

                            builder.AppendLine("d => Err(bebop::DeserializeError::InvalidEnumDiscriminator(d)),");
                        });
                    });
            }).AppendLine();

            // conversion to int
            builder.CodeBlock($"impl core::convert::From<{name}> for u32", _tab, () =>
            {
                builder.CodeBlock($"fn from(value: {name}) -> Self", _tab, () =>
                {
                    builder.CodeBlock("match value", _tab, () =>
                    {
                        foreach (var m in d.Members)
                        {
                            builder.AppendLine($"{name}::{m.Name} => {m.ConstantValue},");
                        }
                    });
                });
            }).AppendLine();

            // sub record
            builder.CodeBlock($"impl<'raw> bebop::SubRecord<'raw> for {name}", _tab, () =>
            {
                builder
                    .AppendLine("const MIN_SERIALIZED_SIZE: usize = bebop::ENUM_SIZE")
                    .AppendLine()
                    .AppendLine("#[inline]")
                    .CodeBlock("fn serialize<W: std::io::Write>(&self, dest: &mut W) -> bebop::SeResult<usize>", _tab,
                        () =>
                        {
                            builder.AppendLine("u32::from(*self).serialize(dest)");
                        })
                    .AppendLine()
                    .AppendLine("#[inline]")
                    .CodeBlock("fn deserialize_chained(raw: &'raw [u8]) -> DeResult<(usize, Self)>", _tab, () =>
                    {
                        builder
                            .AppendLine("let (n, v) = u32::deserialize_chained(raw)?;")
                            .AppendLine("Ok((n, v.try_into()?))");
                    });
            }).AppendLine();

            // record
            builder.AppendLine($"impl<'raw> Record<'raw> for {name} {{}}");
        }

        private void WriteStructDefinition(IndentedStringBuilder builder, StructDefinition d)
        {
            var needsLifetime = NeedsLifetime(d);
            var name = MakeDefIdent(d.Name) + (needsLifetime ? "<'raw>" : "");

            builder
                .AppendLine("#[derive(Clone, Debug, PartialEq)]")
                .CodeBlock($"pub struct {name}", _tab, () => WriteStructDefinitionAttrs(builder, d))
                .AppendLine();

            builder
                .CodeBlock($"impl<'raw> bebop::SubRecord<'raw> for {name}", _tab, () =>
                {
                    if (d.Fields.Count == 0)
                    {
                        // special case
                        builder.AppendLine("const MIN_SERIALIZED_SIZE: usize = 0;");
                    }
                    else
                    {
                        // sum size of all fields at compile time
                        builder.AppendLine("const MIN_SERIALIZED_SIZE: usize =");
                        var parts = d.Fields.Select(f =>
                            $"<{TypeName(f.Type)}>::MIN_SERIALIZED_SIZE");
                        builder.Indent(_tab).Append(string.Join(" +\n", parts)).AppendEnd(";").Dedent(_tab)
                            .AppendLine();
                    }

                    builder.CodeBlock("fn serialize<W: std::io::Write>(&self, dest: &mut W) -> bebop::SeResult<usize>",
                        _tab, () =>
                        {
                            if (d.Fields.Count == 0)
                            {
                                builder.AppendLine("Ok(0)");
                            }
                            else
                            {
                                // just serialize all fields and sum their bytes written
                                builder.CodeBlock("Ok(", _tab, () =>
                                {
                                    builder.AppendLine(string.Join(" +\n",
                                        d.Fields.Select((f) => $"self.{f.Name}.serialize(dest)?")));
                                }, "", ")");
                            }
                        }).AppendLine();

                    builder.CodeBlock("fn deserialize_chained(raw: &'raw [u8]) -> bebop::DeResult<(usize, Self)>", _tab,
                        () =>
                        {
                            builder.CodeBlock("if raw.len() < Self::MIN_SERIALIZED_SIZE", _tab, () =>
                            {
                                builder
                                    .AppendLine("let missing = raw.len() - Self::MIN_SERIALIZED_SIZE;")
                                    .AppendLine("return Err(bebop::DeserializeError::MoreDataExpected(missing));");
                            }).AppendLine();
                            builder.AppendLine("let mut i = 0;");
                            var vars = new LinkedList<(string, string)>();
                            var j = 0;
                            foreach (var f in d.Fields)
                            {
                                builder
                                    .AppendLine(
                                        $"let (read, v{j}) = <{TypeName(f.Type)}>::deserialize_chained(&raw[i..])?;")
                                    .AppendLine("i += read;");
                                vars.AddLast((MakeAttrIdent(f.Name), $"v{j++}"));
                            }

                            builder.AppendLine().CodeBlock("Ok((i, Self {", _tab, () =>
                            {
                                foreach (var (k, v) in vars)
                                {
                                    builder.AppendLine($"{k}: {v},");
                                }
                            }, "", "}))");
                        });
                }).AppendLine();

            WriteRecordImpl(builder, name, d);
        }

        /// <summary>
        /// Write the part within the `pub struct` definition. This will just write the attributes.
        /// </summary>
        private void WriteStructDefinitionAttrs(IndentedStringBuilder builder, StructDefinition d, bool makePub = true)
        {
            foreach (var f in d.Fields)
            {
                WriteDocumentation(builder, f.Documentation);
                WriteDeprecation(builder, f.DeprecatedAttribute);
                var pub = makePub ? "pub " : "";
                builder.AppendLine($"{pub}{MakeAttrIdent(f.Name)}: {TypeName(f.Type)},");
            }
        }

        private void WriteMessageDefinition(IndentedStringBuilder builder, MessageDefinition d)
        {
            var needsLifetime = NeedsLifetime(d);
            var name = MakeDefIdent(d.Name) + (needsLifetime ? "<'raw>" : "");

            builder
                .AppendLine("#[derive(Clone, Debug, PartialEq, Default)]")
                .CodeBlock($"pub struct {name}", _tab, () => WriteMessageDefinitionAttrs(builder, d))
                .AppendLine();

            builder
                .CodeBlock($"impl<'raw> bebop::SubRecord<'raw> for {name}", _tab, () =>
                {
                    // messages are size in bytes + null byte end
                    builder
                        .AppendLine("const MIN_SERIALIZED_SIZE: usize = bebop::LEN_SIZE + 1;")
                        .CodeBlock("fn serialize<W: std::io::Write>(&self, dest: &mut W) -> bebop::SeResult<usize>",
                            _tab, () =>
                            {
                                WriteMessageSerialization(builder, d, "buf");
                                builder.AppendLine("Ok(buf.len() + bebop::LEN_SIZE)");
                            }).AppendLine();

                    builder.CodeBlock("fn deserialize_chained(raw: &'raw [u8]) -> bebop::DeResult<(usize, Self)>", _tab,
                        () => WriteMessageDeserialization(builder, d));
                }).AppendLine();

            WriteRecordImpl(builder, name, d);
        }

        /// <summary>
        /// Write the part within the `pub struct` definition. This will just write the attributes.
        /// </summary>
        private void WriteMessageDefinitionAttrs(IndentedStringBuilder builder, MessageDefinition d,
            bool makePub = true)
        {
            foreach (var f in d.Fields.OrderBy((f) => f.ConstantValue))
            {
                WriteDocumentation(builder, f.Documentation);
                WriteDocumentation(builder, $"Field {f.ConstantValue}");
                WriteDeprecation(builder, f.DeprecatedAttribute);
                var pub = makePub ? "pub " : "";
                builder.AppendLine($"{pub}{MakeAttrIdent(f.Name)}: core::option::Option<{TypeName(f.Type)}>,");
            }
        }

        private void WriteMessageSerialization(IndentedStringBuilder builder, MessageDefinition d, string buf,
            string dest = "dest", string obj = "self")
        {
            obj = string.IsNullOrEmpty(obj) ? "_" : $"{obj}.";
            // message needs if let for each field to only serialize if present.
            builder.AppendLine($"let mut {buf} = std::vec::Vec::new();");
            foreach (var f in d.Fields.OrderBy((f) => f.ConstantValue))
            {
                builder.CodeBlock($"if let Some(ref v) = {obj}{MakeAttrIdent(f.Name)}", _tab, () =>
                {
                    builder.AppendLine($"{buf}.push({f.ConstantValue});")
                        .AppendLine($"v.serialize(&mut {buf})?;");
                });
            }

            builder.AppendLine($"{buf}.push(0);");
            builder
                .AppendLine($"bebop::write_len({dest}, {buf}.len())?;")
                .AppendLine($"{dest}.write_all(&{buf})?;");
        }

        private void WriteMessageDeserialization(IndentedStringBuilder builder, MessageDefinition d,
            bool externalIter = false, string selfClass = "Self", bool directReturn = false)
        {
            var plusI = externalIter ? " + i" : "";
            builder
                .AppendLine($"let len = bebop::read_len(raw)?{plusI} + bebop::LEN_SIZE;")
                .AppendLine()
                .AppendLine("#[cfg(not(feature = \"unchecked\"))]")
                .CodeBlock("if len == 0", _tab, () =>
                {
                    builder.AppendLine("return Err(bebop::DeserializeError::CorruptFrame);");
                }).AppendLine();
            builder.CodeBlock($"if raw.len() < len", _tab, () =>
            {
                builder.AppendLine(
                    "return Err(bebop::DeserializeError::MoreDataExpected(len - raw.len()));");
            }).AppendLine();

            // define vars
            foreach (var f in d.Fields.OrderBy((f) => f.ConstantValue))
            {
                builder.AppendLine($"let mut _{MakeAttrIdent(f.Name)} = None;");
            }

            builder
                .AppendLine()
                .AppendLine("#[cfg(not(feature = \"unchecked\"))]")
                .AppendLine("let mut last = 0;")
                .AppendLine();
            if (!externalIter)
            {
                builder.AppendLine("let mut i = bebop::LEN_SIZE;");
            }
            builder.CodeBlock("while i < len", _tab, () =>
            {
                builder
                    .AppendLine("let di = raw[i];")
                    .AppendLine()
                    .AppendLine("#[cfg(not(feature = \"unchecked\"))]")
                    .CodeBlock("if di != 0", _tab, () =>
                    {
                        builder.CodeBlock("if di < last", _tab, () =>
                        {
                            builder.AppendLine(
                                "return Err(bebop::DeserializeError::CorruptFrame);");
                        });
                        builder.AppendLine("last = di;");
                    })
                    .AppendLine()
                    .AppendLine("i += 1;");
                builder.CodeBlock("match di", _tab, () =>
                {
                    builder.CodeBlock("0 =>", _tab, () =>
                    {
                        builder.AppendLine("break;");
                    });
                    foreach (var f in d.Fields.OrderBy((f) => f.ConstantValue))
                    {
                        var fname = MakeAttrIdent(f.Name);
                        builder.CodeBlock($"{f.ConstantValue} =>", _tab, () =>
                        {
                            builder.AppendLine("#[cfg(not(feature = \"unchecked\"))]");
                            builder.CodeBlock($"if _{fname}.is_some()", _tab, () =>
                            {
                                builder.AppendLine(
                                    "return Err(bebop::DeserializeError::DuplicateMessageField);");
                            });
                            builder
                                .AppendLine(
                                    $"let (read, value) = <{TypeName(f.Type)}>::deserialize_chained(&raw[i..])?;")
                                .AppendLine("i += read;")
                                .AppendLine($"_{fname} = Some(value)");
                        });
                    }

                    builder.CodeBlock("_ =>", _tab, () =>
                    {
                        builder
                            .AppendLine("i = len;")
                            .AppendLine("break;");
                    });
                });
            }).AppendLine();
            builder.CodeBlock("if i != len", _tab, () =>
            {
                builder
                    .AppendLine("debug_assert!(i > len);")
                    .AppendLine("return Err(bebop::DeserializeError::CorruptFrame)");
            });
            
            builder.AppendLine().CodeBlock((directReturn ? "" : "Ok((i, ") + $"{selfClass}", _tab, () =>
            {
                foreach (var f in d.Fields.OrderBy((f) => f.ConstantValue))
                {
                    var fieldName = MakeAttrIdent(f.Name);
                    builder.AppendLine($"{fieldName}: _{fieldName},");
                }
            }, "{", directReturn ? "}" : "}))");
            
        }

        private void WriteUnionDefinition(IndentedStringBuilder builder, UnionDefinition d)
        {
            var scopeName = MakeDefIdent(d.Name);
            var name = scopeName + (NeedsLifetime(d) ? "<'raw>" : "");
            builder.AppendLine("#[derive(Clone, Debug, PartialEq)]");
            builder.CodeBlock($"pub enum {name}", _tab, () =>
            {
                if (d.Branches.Any((b) => b.Definition.Name == "Unknown"))
                {
                    throw new Exception("Unknown is a reserved Union branch name");
                }

                WriteDocumentation(builder,
                    "An unknown type which is likely defined in a newer version of the schema.");
                builder.AppendLine("Unknown,");

                foreach (var b in d.Branches.OrderBy((b) => b.Discriminator))
                {
                    builder.AppendLine();
                    WriteDocumentation(builder, b.Definition.Documentation);
                    WriteDocumentation(builder, $"Discriminator {b.Discriminator}");
                    builder.CodeBlock(MakeEnumVariantIdent(b.Definition.Name), _tab, () =>
                    {
                        switch (b.Definition)
                        {
                            case StructDefinition sd:
                                WriteStructDefinitionAttrs(builder, sd, false);
                                break;
                            case MessageDefinition md:
                                WriteMessageDefinitionAttrs(builder, md, false);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException(b.Definition.ToString());
                        }
                    }, "{", "},");
                }
            }).AppendLine();

            builder.CodeBlock($"impl<'raw> bebop::SubRecord<'raw> for {name}", _tab, () =>
            {
                builder
                    .AppendLine("const MIN_SERIALIZED_SIZE: usize = bebop::LEN_SIZE + 1;")
                    .AppendLine();

                builder.CodeBlock("fn serialize<W: std::io::Write>(&self, dest: &mut W) -> bebop::SeResult<usize>",
                    _tab, () =>
                    {
                        builder.AppendLine("let mut buf = std::vec::Vec::new();");
                        builder.CodeBlock("match self", _tab, () =>
                        {
                            builder.CodeBlock($"{scopeName}::Unknown =>", _tab, () =>
                            {
                                builder.AppendLine("return Err(bebop::SerializeError::CannotSerializeUnknownUnion);");
                            });
                            foreach (var b in d.Branches.OrderBy((b) => b.Discriminator))
                            {
                                var branchName = MakeEnumVariantIdent(b.Definition.Name);
                                builder.CodeBlock($"{scopeName}::{branchName}", _tab, () =>
                                {
                                    // destructuring
                                    if (b.Definition is FieldsDefinition fd)
                                    {
                                        foreach (var field in fd.Fields)
                                        {
                                            var fieldName = MakeAttrIdent(field.Name);
                                            // alias to prevent name collisions
                                            builder.AppendLine($"{fieldName}: ref _{fieldName},");
                                        }
                                    }
                                    else
                                    {
                                        throw new ArgumentOutOfRangeException(b.Definition.ToString());
                                    }
                                });
                                builder.CodeBlock("=>", _tab, () =>
                                {
                                    // serialization
                                    builder.AppendLine($"buf.push({b.Discriminator});");
                                    switch (b.Definition)
                                    {
                                        case StructDefinition sd:
                                            foreach (var sdField in sd.Fields)
                                            {
                                                builder.AppendLine(
                                                    $"_{MakeAttrIdent(sdField.Name)}.serialize(&mut buf);");
                                            }

                                            break;
                                        case MessageDefinition md:
                                            WriteMessageSerialization(builder, md, "msgbuf", "&mut buf", "");
                                            break;
                                        default:
                                            throw new ArgumentOutOfRangeException(b.Definition.ToString());
                                    }
                                });
                            }
                        });

                        builder
                            .AppendLine("bebop::write_len(dest, buf.len());")
                            .AppendLine("dest.write_all(&buf)?;")
                            .AppendLine("Ok(buf.len() + bebop::LEN_SIZE)");
                    }).AppendLine();

                builder.CodeBlock("fn deserialize_chained(raw: &'raw [u8]) -> bebop::DeResult<(usize, Self)>", _tab,
                    () =>
                    {
                        builder
                            .AppendLine("let len = bebop::read_len(&raw)? + bebop::LEN_SIZE;")
                            .CodeBlock("if raw.len() < len", _tab, () =>
                            {
                                builder.AppendLine(
                                    "return Err(bebop::DeserializeError::MoreDataExpected(len - raw.len()));");
                            })
                            .AppendLine("let mut i = bebop::LEN_SIZE + 1;");

                        builder.CodeBlock("let de = match raw[bebop::LEN_SIZE]", _tab, () =>
                        {
                            foreach (var b in d.Branches.OrderBy((b) => b.Discriminator))
                            {
                                var branchName = MakeEnumVariantIdent(b.Definition.Name);
                                builder.CodeBlock($"{b.Discriminator} =>", _tab, () =>
                                {
                                    switch (b.Definition)
                                    {
                                        case StructDefinition sd:
                                            builder.CodeBlock("if raw.len() - i < Self::MIN_SERIALIZED_SIZE", _tab,
                                                () =>
                                                {
                                                    builder
                                                        .AppendLine(
                                                            "let missing = raw.len() - Self::MIN_SERIALIZED_SIZE;")
                                                        .AppendLine(
                                                            "return Err(bebop::DeserializeError::MoreDataExpected(missing));");
                                                }).AppendLine();

                                            var vars = new LinkedList<(string, string)>();
                                            var j = 0;
                                            foreach (var sdField in sd.Fields)
                                            {
                                                builder
                                                    .AppendLine(
                                                        $"let (read, v{j}) = <{TypeName(sdField.Type)}>::deserialize_chained(&raw[i..])?;")
                                                    .AppendLine("i += read;");
                                                vars.AddLast((MakeAttrIdent(sdField.Name), $"v{j++}"));
                                            }

                                            builder.AppendLine().CodeBlock($"{scopeName}::{branchName}", _tab, () =>
                                            {
                                                foreach (var (k, v) in vars)
                                                {
                                                    builder.AppendLine($"{k}: {v},");
                                                }
                                            });
                                            break;
                                        case MessageDefinition md:
                                            WriteMessageDeserialization(builder, md, true, $"{scopeName}::{branchName}",
                                                true);
                                            break;
                                        default:
                                            throw new ArgumentOutOfRangeException(b.Definition.ToString());
                                    }
                                });
                            }

                            builder.CodeBlock($"_ =>", _tab, () =>
                            {
                                builder
                                    .AppendLine("i = len;")
                                    .AppendLine($"{scopeName}::Unknown");
                            });
                        }, "{", "};");
                        builder.CodeBlock("if !cfg!(feature = \"unchecked\") && i != len", _tab,
                            () =>
                            {
                                builder
                                    .AppendLine("debug_assert!(i > len);")
                                    .AppendLine("Err(bebop::DeserializeError::CorruptFrame)");
                            });
                        builder.CodeBlock("else", _tab, () =>
                        {
                            builder.AppendLine("Ok((i, de))");
                        });
                    }).AppendLine();
            }).AppendLine();

            WriteRecordImpl(builder, name, d);
        }

        #endregion

        #region component_writers

        private static void WriteDocumentation(IndentedStringBuilder builder, string documentation)
        {
            if (string.IsNullOrEmpty(documentation)) { return; }

            foreach (var line in documentation.GetLines())
            {
                // TODO: make the docs more friendly to rustdoc by formatting as markdown
                builder.AppendLine($"/// {line}");
            }
        }

        private static void WriteDeprecation(IndentedStringBuilder builder, BaseAttribute? attr)
        {
            if (attr is null) { return; }

            builder.Append("#[deprecated");
            if (!string.IsNullOrEmpty(attr.Value))
            {
                builder.AppendMid($"(note = \"{attr.Value}\")");
            }

            builder.AppendEnd("]");
        }

        private static void WriteRecordImpl(IndentedStringBuilder builder, string name, TopLevelDefinition d)
        {
            var opcode = d.OpcodeAttribute?.Value;
            if (string.IsNullOrEmpty(opcode))
            {
                builder.AppendLine($"impl<'raw> bebop::Record<'raw> for {name} {{}}");
            }
            else
            {
                builder.CodeBlock($"impl<'raw> bebop::Record<'raw> for {name}", _tab, () =>
                {
                    builder.AppendLine($"const OPCODE: core::option::Option<u32> = Some({opcode});");
                });
            }
        }

        #endregion

        #region types_and_identifiers

        private static string MakeConstIdent(string ident)
        {
            var reCased = ident.ToSnakeCase().ToUpper();
            return _reservedWords.Contains(reCased)
                ? $"_{reCased}"
                : reCased;
        }

        private string MakeEnumVariantIdent(string ident) => MakeDefIdent(ident);

        private static string MakeDefIdent(string ident)
        {
            var reCased = ident.ToPascalCase();
            return _reservedWords.Contains(reCased)
                ? $"_{reCased}"
                : reCased;
        }

        private static string MakeAttrIdent(string ident)
        {
            var reCased = ident.ToSnakeCase();
            return _reservedWords.Contains(reCased)
                ? $"_{reCased}"
                : reCased;
        }

        /// <summary>
        /// Generate a Rust type name for the given <see cref="TypeBase"/>.
        /// </summary>
        /// <param name="type">The field type to generate code for.</param>
        /// <param name="ot">Ownership type, e.g. <c>&'raw str</c> versus <c>String</c>.</param>
        /// <returns>The Rust type name.</returns>
        private string TypeName(in TypeBase type, OwnershipType ot = OwnershipType.Borrowed)
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
                        BaseType.String => ot switch
                        {
                            OwnershipType.Borrowed => "&'raw str",
                            OwnershipType.Constant => "&str",
                            OwnershipType.Owned => "String",
                            _ => throw new ArgumentOutOfRangeException(nameof(ot))
                        },
                        BaseType.Guid => "bebop::Guid",
                        BaseType.Date => "bebop::Date",
                        _ => throw new ArgumentOutOfRangeException(st.BaseType.ToString())
                    };
                case ArrayType at:
                    if (at.MemberType is ScalarType mst && ot is OwnershipType.Borrowed or OwnershipType.Constant)
                    {
                        var lifetime = ot is OwnershipType.Borrowed ? "'raw" : "'static";
                        if (at.IsOneByteUnits())
                        {
                            return $"&{lifetime} [{TypeName(at.MemberType)}]";
                        }

                        var pmba = $"bebop::PrimitiveMultiByteArray<{lifetime}, {TypeName(at.MemberType)}>";
                        return mst.BaseType switch
                        {
                            BaseType.UInt16 => pmba,
                            BaseType.Int16 => pmba,
                            BaseType.UInt32 => pmba,
                            BaseType.Int32 => pmba,
                            BaseType.UInt64 => pmba,
                            BaseType.Int64 => pmba,
                            BaseType.Float32 => pmba,
                            BaseType.Float64 => pmba,
                            BaseType.String => $"std::vec::Vec<{TypeName(at.MemberType)}>",
                            // this one does not care what endian the system is
                            BaseType.Guid => $"&{lifetime} [bebop::Guid]",
                            BaseType.Date => pmba,
                            _ => throw new ArgumentOutOfRangeException(mst.BaseType.ToString())
                        };
                    }
                    else
                    {
                        return $"std::vec::Vec<{TypeName(at.MemberType)}>";
                    }
                case MapType mt:
                    return $"std::collections::HashMap<{TypeName(mt.KeyType)}, {TypeName(mt.ValueType)}>";
                case DefinedType dt:
                    return ot switch
                    {
                        OwnershipType.Borrowed => NeedsLifetime(Schema.Definitions[dt.Name])
                            ? $"{dt.Name}<'raw>"
                            : dt.Name,
                        OwnershipType.Owned => NeedsLifetime(Schema.Definitions[dt.Name])
                            ? $"{dt.Name}<'raw>"
                            : dt.Name,
                        OwnershipType.Constant => throw new NotSupportedException("Cannot have a const defined type"),
                        _ => throw new ArgumentOutOfRangeException(nameof(ot), ot, null)
                    };
            }

            throw new InvalidOperationException($"GetTypeName: {type}");
        }

        private bool TypeNeedsLifetime(TypeBase type, OwnershipType ot = OwnershipType.Borrowed) =>
            type switch
            {
                DefinedType dt => NeedsLifetime(Schema.Definitions[dt.Name]),
                MapType mt => TypeNeedsLifetime(mt.KeyType) || TypeNeedsLifetime(mt.ValueType),
                { } tb => TypeName(tb, ot).Contains("'raw")
            };

        private bool FieldNeedsLifetime(Definition d, IField f, OwnershipType ot = OwnershipType.Borrowed)
        {
            var key = $"{d.Name}::{f.Name}";
            if (_needsLifetime.ContainsKey(key))
            {
                return _needsLifetime[key];
            }

            _needsLifetime[key] = TypeNeedsLifetime(f.Type, ot);
            return _needsLifetime[key];
        }

        private bool NeedsLifetime(Definition d, OwnershipType ot = OwnershipType.Borrowed)
        {
            if (_needsLifetime.ContainsKey(d.Name))
            {
                return _needsLifetime[d.Name];
            }

            _needsLifetime[d.Name] = d switch
            {
                ConstDefinition cd => false,
                EnumDefinition ed => false,
                FieldsDefinition fd => fd.Fields.Any(f => FieldNeedsLifetime(d, f, ot)),
                UnionDefinition ud => ud.Branches.Any(b => NeedsLifetime(b.Definition)),
                _ => throw new ArgumentOutOfRangeException(d.Name)
            };
            return _needsLifetime[d.Name];
        }

        #endregion

        #region literals

        private string EmitLiteral(Literal literal) =>
            literal switch
            {
                BoolLiteral bl => bl.Value ? "true" : "false",
                IntegerLiteral il => il.Value,
                FloatLiteral {Value: "inf"} => $"{TypeName(literal.Type)}::INFINITY",
                FloatLiteral {Value: "-inf"} => $"{TypeName(literal.Type)}::NEG_INFINITY",
                FloatLiteral {Value: "nan"} => $"{TypeName(literal.Type)}::NAN",
                FloatLiteral fl => fl.Value.Contains('.') ? fl.Value : fl.Value + '.',
                StringLiteral sl => MakeStringLiteral(sl.Value),
                GuidLiteral gl => MakeGuidLiteral(gl.Value),
                _ => throw new ArgumentOutOfRangeException(literal.ToString()),
            };

        private static string MakeStringLiteral(string value) =>
            // rust accepts full UTF-8 strings in code AND even supports newlines
            value.Contains("\"#") ? value.Replace("\\", "\\\\").Replace("\"", "\\\"") : $"r#\"{value}\"#";

        private static string MakeGuidLiteral(Guid guid)
        {
            var g = guid.ToString("N").ToCharArray();
            var builder = new StringBuilder();
            builder.Append("bebop::Guid::from_be_bytes([");
            for (var i = 0; i < g.Length; i += 2)
            {
                builder.Append("0x")
                    .Append(g[i])
                    .Append(g[i + 1])
                    .Append(',');
            }

            builder.Append("])");
            return builder.ToString();
        }

        #endregion
    }
}