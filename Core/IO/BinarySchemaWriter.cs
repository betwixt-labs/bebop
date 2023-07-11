using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using Core.Exceptions;
using Core.Meta;
using Core.Meta.Attributes;

namespace Core.IO
{
    public class BinarySchemaWriter : IDisposable
    {
        private readonly Encoding _encoding = Encoding.UTF8;

        private readonly BebopSchema _schema;
        private readonly BinaryWriter _writer;
        private readonly List<Definition> _definedTypes;
        private readonly Dictionary<string, int> _definitionIndex;

        private readonly List<ServiceDefinition> _services;
        private bool disposedValue;

        public BinarySchemaWriter(BebopSchema schema)
        {
            _schema = schema;
            _writer = new BinaryWriter(new MemoryStream(), _encoding);
            _definedTypes = _schema.Definitions.Values.Where((d) => d is StructDefinition or MessageDefinition or UnionDefinition or EnumDefinition).ToList();
            _services = _schema.Definitions.Values.OfType<ServiceDefinition>().ToList();
            _definitionIndex = new Dictionary<string, int>();
            for (var i = 0; i < _definedTypes.Count; i++)
            {
                var definition = _definedTypes.ElementAt(i);
                _definitionIndex.Add(definition.Name, i);
            }
        }

        #region Field Writer
        private void WriteField(Definition parent, Field field)
        {
            WriteString(field.Name);
            _writer.Write(TypeToId(field.Type));
            if (field.Type is ArrayType at)
            {
                WriteArray(at);
            }
            else if (field.Type is MapType mt)
            {
                WriteMap(mt);
            }

            WriteAttributes(field.Attributes);
            // Write the constant value for message fields
            if (parent is MessageDefinition)
            {
                _writer.Write((byte)field.ConstantValue);
            }
        }

        private void WriteArray(ArrayType arrayType)
        {
            byte depth = 0;
            var memberType = arrayType.MemberType;
            while (memberType is ArrayType at)
            {
                depth++;
                if (depth > byte.MaxValue)
                {
                    throw new CompilerException("Array depth is too large");
                }
                memberType = at.MemberType;
            }
            Console.WriteLine($"Array depth: {depth}");
            Console.WriteLine($"Array member type: {memberType}");
            _writer.Write(depth);
            _writer.Write(TypeToId(memberType));
        }

        private void WriteMap(MapType mapType)
        {
            _writer.Write(TypeToId(mapType.KeyType));
            _writer.Write(TypeToId(mapType.ValueType));
            if (mapType.ValueType is ArrayType at)
            {
                WriteArray(at);
            }
            else if (mapType.ValueType is MapType mt)
            {
                WriteMap(mt);
            }
        }

        private void WriteConstant(BaseType baseType, object value)
        {
            if (value is BigInteger bigInt)
            {
                switch (baseType)
                {
                    case BaseType.Byte:
                        value = (byte)bigInt;
                        break;
                    case BaseType.UInt16:
                        value = (ushort)bigInt;
                        break;
                    case BaseType.Int16:
                        value = (short)bigInt;
                        break;
                    case BaseType.Int32:
                        value = (int)bigInt;
                        break;
                    case BaseType.UInt32:
                        value = (uint)bigInt;
                        break;
                    case BaseType.Int64:
                        value = (long)bigInt;
                        break;
                    case BaseType.UInt64:
                        value = (ulong)bigInt;
                        break;
                }
            }
            switch (baseType)
            {
                case BaseType.Bool:
                    _writer.Write((bool)value);
                    break;
                case BaseType.Byte:
                    _writer.Write((byte)value);
                    break;
                case BaseType.UInt16:
                    _writer.Write((ushort)value);
                    break;
                case BaseType.Int16:
                    _writer.Write((short)value);
                    break;
                case BaseType.UInt32:
                    _writer.Write((uint)value);
                    break;
                case BaseType.Int32:
                    _writer.Write((int)value);
                    break;
                case BaseType.UInt64:
                    _writer.Write((ulong)value);
                    break;
                case BaseType.Int64:
                    _writer.Write((long)value);
                    break;
                case BaseType.Float32:
                    _writer.Write((float)value);
                    break;
                case BaseType.Float64:
                    _writer.Write((double)value);
                    break;
                case BaseType.Guid:
                    _writer.Write(((Guid)value).ToByteArray());
                    break;
                case BaseType.String:
                    WriteString((string)value);
                    break;
                default:
                    throw new CompilerException($"Unknown base type {baseType}");
            }
        }

        private void WriteString(string value)
        {
            var bytes = _encoding.GetBytes(value + '\0');
            _writer.Write(bytes);
        }

        #endregion

        #region Attributes
        private void WriteAttribute(BaseAttribute attribute)
        {
            // attribute name
            WriteString(attribute.Name);
            // has value
            bool hasValue = !string.IsNullOrWhiteSpace(attribute.Value);
            _writer.Write(hasValue);
            if (hasValue)
            {
                WriteString(attribute.Value);
            }
            if (attribute is OpcodeAttribute oa)
            {
                // eventually this should be a type id instead of a bool
                // since they would both take up the same amount of space
                // it will be forward compatible
                _writer.Write(oa.IsNumber);
            }
            else
            {
                _writer.Write(false);
            }
        }


        private void WriteAttributes(List<BaseAttribute>? attributes)
        {
            if (attributes is null)
            {
                _writer.Write((byte)0);
                return;
            }
            _writer.Write((byte)attributes.Count);
            foreach (var attribute in attributes)
            {
                WriteAttribute(attribute);
            }
        }

        private void WriteAttributes(Definition definition)
        {
            List<BaseAttribute>? attributes = definition switch
            {
                StructDefinition sd => sd.Attributes,
                MessageDefinition md => md.Attributes,
                UnionDefinition ud => ud.Attributes,
                EnumDefinition ed => ed.Attributes,
                ServiceDefinition sd => sd.Attributes,
                _ => null,
            };
            WriteAttributes(attributes);
        }

        #endregion


        public byte[] Encode()
        {
            _writer.Write(ReservedWords.SchemaLangVersion);
            WriteDefinitions();
            WriteServices();
            _writer.Flush();
            return ((MemoryStream)_writer.BaseStream).ToArray();
        }

        #region Write Definitions

        private void WriteDefinitions()
        {
            _writer.Write((uint)_definedTypes.Count);
            for (var i = 0; i < _definedTypes.Count; i++)
            {
                var definition = _definedTypes.ElementAt(i);
                WriteString(definition.Name);
                _writer.Write(DefinitionToKind(definition));
                WriteAttributes(definition);
                switch (definition)
                {
                    case StructDefinition sd:
                        WriteStruct(sd);
                        break;
                    case MessageDefinition md:
                        WriteMessage(md);
                        break;
                    case UnionDefinition ud:
                        WriteUnion(ud);
                        break;
                    case EnumDefinition ed:
                        WriteEnum(ed);
                        break;
                }
            }

        }

        private void WriteServices()
        {
            var serviceCount = _services.Count;
            _writer.Write((uint)serviceCount);
            foreach (var service in _services)
            {
                WriteString(service.Name);
                WriteAttributes(service);
                var methodCount = service.Methods.Count;
                _writer.Write((uint)methodCount);
                foreach (var method in service.Methods)
                {
                    var methodDefinition = method.Definition;
                    WriteString(methodDefinition.Name);
                    WriteAttributes(method.Attributes);
                    _writer.Write((byte)methodDefinition.Type);
                    _writer.Write(DefinitionToId(methodDefinition.RequestDefinition.AsString));
                    _writer.Write(DefinitionToId(methodDefinition.ReturnDefintion.AsString));
                    _writer.Write(method.Id);
                }
            }
        }


        private void WriteMessage(MessageDefinition definition)
        {
            var fieldCount = definition.Fields.Count;
            if (fieldCount < 0 || fieldCount > 255)
            {
                throw new CompilerException($"{definition.Name} exceeds maximum fields: has {fieldCount} fields");
            }
            _writer.Write((byte)fieldCount);
            foreach (var field in definition.Fields)
            {
                WriteField(definition, field);
            }
        }

        private void WriteEnum(EnumDefinition definition)
        {
            _writer.Write(TypeToId(definition.ScalarType));
            // TODO formalize 255 as the max number of fields
            var memberCount = definition.Members.Count;
            if (memberCount < 0 || memberCount > 255)
            {
                throw new CompilerException($"{definition.Name} exceeds maximum members: has {memberCount} members");
            }
            _writer.Write((byte)memberCount);
            foreach (var member in definition.Members)
            {
                WriteString(member.Name);
                WriteAttributes(member.Attributes);
                WriteConstant(definition.ScalarType.BaseType, member.ConstantValue);
            }
        }

        private void WriteStruct(StructDefinition definition)
        {
            // modifier(s)
            _writer.Write(definition.IsReadOnly);
            var fieldCount = definition.Fields.Count;
            if (fieldCount < 0 || fieldCount > 255)
            {
                throw new CompilerException($"{definition.Name} exceeds maximum fields: has {fieldCount} fields");
            }
            _writer.Write((byte)fieldCount);
            Console.WriteLine($"Writing {fieldCount} fields for {definition.Name}");
            foreach (var field in definition.Fields)
            {
                WriteField(definition, field);
            }
        }

        private void WriteUnion(UnionDefinition definition)
        {
            var branches = definition.Dependencies();
            var branchCount = branches.Count();
            if (branchCount < 0 || branchCount > 255)
            {
                throw new CompilerException($"{definition.Name} exceeds maximum branches: has {branchCount} branches");
            }
            _writer.Write((byte)branchCount);
            for (var i = 0; i < branches.Count(); i++)
            {
                var branch = branches.ElementAt(i);
                // discriminator
                _writer.Write((byte)(i + 1));
                _writer.Write(DefinitionToId(branch));
            }
        }

        #endregion

        #region Utils

        private static byte DefinitionToKind(Definition definition)
        {
            return definition switch
            {
                StructDefinition => 1,
                MessageDefinition => 2,
                UnionDefinition => 3,
                EnumDefinition => 4,
                _ => throw new CompilerException($"Invalid definition type {definition.GetType()}")
            };
        }

        private int TypeToId(TypeBase typeBase)
        {
            if (typeBase is DefinedType dt)
            {
                return DefinitionToId(dt.Name);
            }
            // negate the built-in type ids so that they don't conflict with the user defined types
            return ~(typeBase switch
            {
                ScalarType st => st.BaseType switch
                {
                    BaseType.Bool => 0,
                    BaseType.Byte => 1,
                    BaseType.UInt16 => 2,
                    BaseType.Int16 => 3,
                    BaseType.UInt32 => 4,
                    BaseType.Int32 => 5,
                    BaseType.UInt64 => 6,
                    BaseType.Int64 => 7,
                    BaseType.Float32 => 8,
                    BaseType.Float64 => 9,
                    BaseType.String => 10,
                    BaseType.Guid => 11,
                    BaseType.Date => 12,
                    _ => throw new CompilerException($"Invalid scalar type {st.BaseType}")
                },
                ArrayType => 13,
                MapType => 14,
                _ => throw new CompilerException($"Invalid type {typeBase.GetType()}")
            });
        }

        private int DefinitionToId(string definitionName)
        {
            return _definitionIndex[definitionName];
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _writer.Flush();
                    _writer.Dispose();
                }
                _definitionIndex.Clear();
                _services.Clear();
                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}