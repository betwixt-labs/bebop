using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using Core.Exceptions;
using Core.Meta;
using Core.Meta.Decorators;

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

            WriteDecorators(field.Decorators);
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
                        _writer.Write((byte)bigInt);
                        return;
                    case BaseType.UInt16:
                        _writer.Write((ushort)bigInt);
                        return;
                    case BaseType.Int16:
                        _writer.Write((short)bigInt);
                        return;
                    case BaseType.UInt32:
                        _writer.Write((uint)bigInt);
                        return;
                    case BaseType.Int32:
                        _writer.Write((int)bigInt);
                        return;
                    case BaseType.UInt64:
                        _writer.Write((ulong)bigInt);
                        return;
                    case BaseType.Int64:
                        _writer.Write((long)bigInt);
                        return;
                }
            }
            else if (baseType.IsNumber() && value is string str)
            {
                switch (baseType)
                {
                    case BaseType.Byte:
                        _writer.Write(byte.Parse(str));
                        return;
                    case BaseType.UInt16:
                        _writer.Write(ushort.Parse(str));
                        return;
                    case BaseType.Int16:
                        _writer.Write(short.Parse(str));
                        return;
                    case BaseType.UInt32:
                        _writer.Write(uint.Parse(str));
                        return;
                    case BaseType.Int32:
                        _writer.Write(int.Parse(str));
                        return;
                    case BaseType.UInt64:
                        _writer.Write(ulong.Parse(str));
                        return;
                    case BaseType.Int64:
                        _writer.Write(long.Parse(str));
                        return;
                }
            }
            else if (baseType is BaseType.Guid && value is string guidStr)
            {
                _writer.Write(Guid.Parse(guidStr).ToByteArray());
                return;
            }
            else if (baseType is BaseType.Bool && value is string boolString)
            {
                _writer.Write(bool.Parse(boolString));
                return;
            }
            else if (baseType is BaseType.String && value is string ss)
            {
                WriteString(ss);
                return;
            }
            throw new CompilerException($"Unsupported base type {baseType}");
        }

        private void WriteString(string value)
        {
            var bytes = _encoding.GetBytes(value + '\0');
            _writer.Write(bytes);
        }

        #endregion

        #region Decorators
        private void WriteDecorator(SchemaDecorator decorator)
        {
            // decorator name
            WriteString(decorator.Identifier);
            var arguments = decorator.Arguments;
            // argument count
            _writer.Write((byte)arguments.Count);
            var definition = decorator.Definition;
            foreach (var argument in arguments)
            {
                var parameter = definition?.Parameters?.Where((p) => p.Identifier == argument.Key).FirstOrDefault();
                if (parameter is null)
                {
                    throw new CompilerException($"Unknown parameter {argument.Key} for decorator {decorator.Identifier}");
                }
                WriteString(argument.Key);
                _writer.Write(TypeToId(new ScalarType(parameter.Type)));
                WriteConstant(parameter.Type, argument.Value);
            }
        }


        private void WriteDecorators(List<SchemaDecorator>? decorators)
        {
            if (decorators is null)
            {
                _writer.Write((byte)0);
                return;
            }
            _writer.Write((byte)decorators.Count);
            foreach (var decorator in decorators)
            {
                WriteDecorator(decorator);
            }
        }

        private void WriteDecorators(Definition definition)
        {
            List<SchemaDecorator>? decorators = definition switch
            {
                StructDefinition sd => sd.Decorators,
                MessageDefinition md => md.Decorators,
                UnionDefinition ud => ud.Decorators,
                EnumDefinition ed => ed.Decorators,
                ServiceDefinition sd => sd.Decorators,
                _ => null,
            };
            WriteDecorators(decorators);
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
                WriteDecorators(definition);
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
                WriteDecorators(service);
                var methodCount = service.Methods.Count;
                _writer.Write((uint)methodCount);
                foreach (var method in service.Methods)
                {
                    var methodDefinition = method.Definition;
                    WriteString(methodDefinition.Name);
                    WriteDecorators(method.Decorators);
                    _writer.Write((byte)methodDefinition.Type);
                    _writer.Write(DefinitionToId(methodDefinition.RequestDefinition.AsString));
                    _writer.Write(DefinitionToId(methodDefinition.ResponseDefintion.AsString));
                    _writer.Write(method.Id);
                }
            }
        }


        private void WriteMessage(MessageDefinition definition)
        {
            _writer.Write(definition.MinimalEncodedSize(_schema));
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
            _writer.Write(definition.IsBitFlags);
            _writer.Write(definition.ScalarType.MinimalEncodedSize(_schema));
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
                WriteDecorators(member.Decorators);
                WriteConstant(definition.ScalarType.BaseType, member.ConstantValue);
            }

        }

        private void WriteStruct(StructDefinition definition)
        {
            // modifier(s)
            _writer.Write(definition.IsReadOnly);
            _writer.Write(definition.MinimalEncodedSize(_schema));
            _writer.Write(definition.IsFixedSize(_schema));

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

        private void WriteUnion(UnionDefinition definition)
        {
            var branches = definition.Dependencies();
            var branchCount = branches.Count();
            if (branchCount < 0 || branchCount > 255)
            {
                throw new CompilerException($"{definition.Name} exceeds maximum branches: has {branchCount} branches");
            }
            _writer.Write(definition.MinimalEncodedSize(_schema));
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