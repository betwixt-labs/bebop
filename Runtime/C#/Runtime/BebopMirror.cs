using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bebop.Attributes;
using Bebop.Exceptions;
using Bebop.Extensions;

namespace Bebop.Runtime
{
    /// <summary>
    ///     A fast reflection based analysis that enables dynamic defined record access during runtime.  
    /// </summary>
    public static class BebopMirror
    {
        /// <summary>
        ///     A read-only dictionary of <see cref="BebopRecord{T}"/> types which are keyed via their opcode.
        /// </summary>
        /// <remarks>
        ///     We use a dictionary for records with opcodes because the lookup speed is O(1) while the lookup
        ///     performance of a List is an O(n) operation.
        /// </remarks>
        private static IReadOnlyDictionary<uint, BebopRecord> _opcodeRecords = null!;

        /// <summary>
        ///     A read-only dictionary of <see cref="BebopRecord{T}"/> types which are keyed via their actual record type.
        /// </summary>
        /// <remarks>
        ///     We use a dictionary for type lookups because the lookup speed is O(1) while the lookup
        ///     performance of a List is an O(n) operation.
        /// </remarks>
        private static IReadOnlyDictionary<Type, BebopRecord> _concreteRecords = null!;

        /// <summary>
        ///     A read-only dictionary of class instances that were marked by the <see cref="RecordHandlerAttribute"/>, keyed by a
        ///     <see cref="BebopRecord{T}"/> type.
        /// </summary>
        private static IReadOnlyDictionary<Type, object> _handlerInstances = null!;

        /// <summary>
        ///     A read-only list of all resolved <see cref="BebopRecord{T}"/> types.
        /// </summary>
        public static IReadOnlyList<BebopRecord> DefinedRecords = null!;

        /// <summary>
        ///     Resolve all records before any members are referenced.
        /// </summary>
        static BebopMirror()
        {
            ResolveRecords();
        }

        /// <summary>
        ///     Returns a <see cref="BebopRecord"/> of the specified name that corresponds to the constructed
        ///     <see cref="BebopRecord{T}"/>.
        /// </summary>
        /// <param name="recordName">The name of the desired Bebop record.</param>
        /// <exception cref="BebopRuntimeException">Thrown when <paramref name="recordName"/> cannot be mapped to a defined type.</exception>
        /// <returns>A virtual <see cref="BebopRecord"/> accessor.</returns>
        public static BebopRecord GetRecord(string recordName) => DefinedRecords
                .FirstOrDefault(definedType => definedType.Type.Name.Equals(recordName)) ??
            throw new BebopRuntimeException($"A record named '{recordName}' does not exist.");


        /// <summary>
        ///     Returns a <see cref="BebopRecord"/> of the specified opcode that corresponds to the constructed
        ///     <see cref="BebopRecord{T}"/>.
        /// </summary>
        /// <param name="opcode">The unique opcode belonging to desired record</param>
        /// <exception cref="BebopRuntimeException">Thrown when <paramref name="opcode"/> does not correspond to any defined type.</exception>
        /// <returns>A virtual <see cref="BebopRecord"/> accessor.</returns>
        public static BebopRecord GetRecordFromOpCode(uint opcode)
            => (_opcodeRecords.TryGetValue(opcode, out var type) ? type : null) ??
                throw new BebopRuntimeException(opcode);

        /// <summary>
        ///     Finds a <see cref="BebopRecord{T}"/> using it's defined type
        /// </summary>
        /// <typeparam name="T">The defined record type</typeparam>
        /// <exception cref="BebopRuntimeException">Thrown when <typeparamref name="T"/> does not correspond to any Bebop defined type.</exception>
        /// <returns>An instance of <see cref="BebopRecord{T}"/></returns>
        public static BebopRecord<T> FindRecordFromType<T>() where T : class, new()
        {
            if (FindRecordFromType(typeof(T)) is BebopRecord<T> record)
            {
                return record;
            }
            throw new BebopRuntimeException($"A record with the type of '{nameof(T)}' does not exist.");
        }


        /// <summary>
        ///     Finds a <see cref="BebopRecord{T}"/> using it's defined type
        /// </summary>
        /// <param name="type">The defined record type</param>
        /// <exception cref="BebopRuntimeException">Thrown when the specified <paramref name="type"/> does not correspond to any Bebop defined type.</exception>
        /// <returns>An instance of <see cref="BebopRecord{T}"/></returns>
        public static BebopRecord FindRecordFromType(Type type)
        {
            if (_concreteRecords.TryGetValue(type, out var recordType))
            {
                return recordType;
            }
            throw new BebopRuntimeException($"A record with the type of '{type.Name}' does not exist.");
        }

        /// <summary>
        ///     Invokes the handler of the specified <paramref name="record"/> marked with the <see cref="BebopRecordAttribute"/>
        /// </summary>
        /// <param name="record">An instance of the defined record marked with the <see cref="BebopRecordAttribute"/></param>
        /// <param name="state">An object that will be passed to the invoked handler.</param>
        /// <exception cref="BebopRuntimeException"/>
        public static void HandleRecord<T>(T record, object state) where T : class, new() => FindRecordFromType<T>().InvokeHandler(state, record);

        /// <summary>
        ///     Decodes the specified <paramref name="buffer"/> into the record that corresponds to the given
        ///     <paramref name="record"/> and invokes it's handler.
        /// </summary>
        /// <param name="record">An instance of <see cref="BebopRecord{T}"/></param>
        /// <param name="buffer">A buffer containing a record.</param>
        /// <param name="state">An object that will be passed to the invoked handler.</param>
        /// <exception cref="BebopRuntimeException"/>
        public static void HandleRecord(BebopRecord record, byte[] buffer, object state)
        {
            if (record.Decode(buffer) is { } o)
            {
                record.InvokeHandler(state, o);
            }
        }

        /// <summary>
        ///     Decodes the specified <paramref name="buffer"/> into the record that corresponds to the given
        ///     <paramref name="opcode"/> and invokes it's handler.
        /// </summary>
        /// <param name="buffer">A buffer containing A record.</param>
        /// <param name="opcode">The opcode of the record.</param>
        /// <param name="state">An object that will be passed to the invoked handler.</param>
        /// <exception cref="BebopRuntimeException"/>
        public static void HandleRecord(byte[] buffer, uint opcode, object state)
        {
            var record = GetRecordFromOpCode(opcode);
            if (record.Decode(buffer) is { } o)
            {
                record.InvokeHandler(state, o);
            }
        }

        /// <summary>
        ///     Resolves all <see cref="BebopRecord{T}"/> handlers.
        /// </summary>
        /// <exception cref="BebopRuntimeException"></exception>
        /// <returns>A collection of bound record types keyed by their parent class.</returns>
        private static Dictionary<Type, List<(MethodInfo Method, Type RecordType)>> ResolveHandlers()
        {
            var handlers = new Dictionary<Type, List<(MethodInfo Method, Type RecordType)>>();
            var domainAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in domainAssemblies)
            {
                foreach (var handlerClass in assembly.GetMarkedTypes<RecordHandlerAttribute>())
                {
                    if (handlerClass is null)
                    {
                        continue;
                    }
                    if (!handlerClass.IsStaticClass() && !handlerClass.HasDefaultConstructor())
                    {
                        throw new BebopRuntimeException(
                            $"non-static class '{handlerClass}' does not contain an accessible default constructor");
                    }

                    handlers[handlerClass] = new List<(MethodInfo Method, Type RecordType)>();
                    foreach (var (methodInfo, attribute) in handlerClass.GetBoundMethods())
                    {
                        if (attribute.RecordType is not {IsGenericType: true} ||
                            attribute.RecordType.GetGenericTypeDefinition() != typeof(BebopRecord<>))
                        {
                            throw new BebopRuntimeException($"Bound type is not a BebopRecord: {attribute.RecordType}");
                        }

                        if (methodInfo.HasReturn())
                        {
                            throw new BebopRuntimeException("Bound methods may not have a return value.");
                        }

                        // check to make sure the bound method has a proper signature
                        methodInfo.ValidateSignature(attribute.RecordType.GetGenericArguments().FirstOrDefault());


                        // add the bound method and it's triggering record to the collection
                        handlers[handlerClass].Add((methodInfo, attribute.RecordType));
                    }
                }
            }
            handlers.ValidateHandlers();
            return handlers;
        }

        /// <summary>
        ///     Resolves all Bebop records across assemblies within the current application domain.
        /// </summary>
        /// <exception cref="BebopRuntimeException"></exception>
        private static void ResolveRecords()
        {
            if (DefinedRecords is {Count: > 0})
            {
                throw new BebopRuntimeException("Bebop records may only be resolved once during runtime");
            }

            var definedRecords = new List<BebopRecord>();
            var opcodeTypes = new Dictionary<uint, BebopRecord>();
            var concreteRecords = new Dictionary<Type, BebopRecord>();

            var domainAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            if (domainAssemblies.Length == 0)
            {
                throw new BebopRuntimeException("No assemblies were found in the current app domain");
            }

            var handlers = ResolveHandlers();
            var handlerInstances = new Dictionary<Type, object>();

            foreach (var handler in handlers)
            {
                var handlerInstance = !handler.Key.IsStaticClass() ? Activator.CreateInstance(handler.Key) : new StaticClass();
                if (handlerInstance is null)
                {
                    throw new BebopRuntimeException($"Unable to create instance of '{handler.Key}'");
                }
                foreach (var binding in handler.Value)
                {
                    handlerInstances[binding.RecordType] = handlerInstance;
                }
            }
            _handlerInstances = handlerInstances;

            foreach (var assembly in domainAssemblies)
            {
                foreach (var type in assembly.GetBebopRecordTypes())
                {
                    if (type is not {IsClass: true, IsSealed: true, IsPublic: true, IsVisible: true, BaseType: not null})
                    {
                        continue;
                    }
                    var record = type.ToBebopRecord();
                    if (_handlerInstances.ContainsKey(record.GetType()) && _handlerInstances[record.GetType()] is
                        { } handlerInstance)
                    {
                        record.AssignHandler(handlers.FindBindingInfo(record.GetType()).Method, handlerInstance);
                    }
                    if (record is {OpCode: not null})
                    {
                        if (opcodeTypes.ContainsKey(record.OpCode.Value))
                        {
                            throw new BebopRuntimeException(opcodeTypes[record.OpCode.Value], record);
                        }
                        opcodeTypes.Add(record.OpCode.Value, record);
                    }
                    concreteRecords[record.Type] = record;
                    definedRecords.Add(record);
                }
            }
            _concreteRecords = concreteRecords;
            _opcodeRecords = opcodeTypes;
            DefinedRecords = definedRecords;
        }
    }
}