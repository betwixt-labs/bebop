using System;
using System.Collections.Generic;
using System.Linq;
using Bebop.Exceptions;
using Bebop.Extensions;

namespace Bebop
{
    /// <summary>
    ///     Access Bebop types dynamically
    /// </summary>
    public static class BebopWindow
    {
        /// <summary>
        ///     A read-only dictionary of <see cref="BebopType{T}"/> classes which are keyed via their opcode.
        /// </summary>
        /// <remarks>
        ///     We use a dictionary for structs/messages with opcodes because the lookup speed is O(1) while the lookup
        ///     performance of a List is an O(n) operation.
        /// </remarks>
        private static IReadOnlyDictionary<uint, BebopType> _opcodeTypes = null!;

        /// <summary>
        ///     A read-only list of all resolved <see cref="BebopType{T}"/> classes.
        /// </summary>
        public static IReadOnlyList<BebopType> DefinedTypes = null!;

        /// <summary>
        ///     Returns a <see cref="BebopType"/> of the specified name that corresponds to the constructed
        ///     <see cref="BebopType{T}"/>.
        /// </summary>
        /// <param name="definedTypeName">The name of the defined Bebop type.</param>
        /// <returns>A virtual <see cref="BebopType"/> accessor.</returns>
        /// <exception cref="InvalidOperationException"/>
        public static BebopType GetType(string definedTypeName) => DefinedTypes
                .FirstOrDefault(definedType => definedType.Class.Name.Equals(definedTypeName)) ??
            throw new BebopRuntimeException($"A Bebop type named \"{definedTypeName}\" does not exist.");


        /// <summary>
        ///     Returns a <see cref="BebopType"/> of the specified opcode that corresponds to the constructed
        ///     <see cref="BebopType{T}"/>.
        /// </summary>
        /// <param name="opcode">The unique opcode belonging to desired type</param>
        /// <returns>A virtual <see cref="BebopType"/> accessor.</returns>
        public static BebopType GetOpCodeType(uint opcode)
            => (_opcodeTypes.TryGetValue(opcode, out var type) ? type : null) ??
                throw new BebopRuntimeException(opcode);

        /// <summary>
        ///     Resolves all Bebop types across assemblies within the current application domain.
        /// </summary>
        /// <remarks>
        ///     Should only be called once at the start of your application.
        /// </remarks>
        public static void ResolveTypes()
        {
            if (DefinedTypes is {Count: > 0})
            {
                throw new BebopRuntimeException("Bebop types may only be resolved once during runtime");
            }

            var definedTypes = new List<BebopType>();
            var opcodeTypes = new Dictionary<uint, BebopType>();


            var domainAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            if (domainAssemblies.Length == 0)
            {
                throw new BebopRuntimeException("No assemblies were found in the current app domain");
            }
            foreach (var assembly in domainAssemblies)
            {
                foreach (var type in assembly.GetMarkedTypes())
                {
                    if (type is not {IsSealed: true, IsPublic: true, IsVisible: true, BaseType: not null})
                    {
                        continue;
                    }
                    var bebopType = type.ToBebopType();
                    if (bebopType is {OpCode: > -1})
                    {
                        if (opcodeTypes.ContainsKey((uint) bebopType.OpCode))
                        {
                            throw new BebopRuntimeException(opcodeTypes[(uint) bebopType.OpCode], bebopType);
                        }
                        opcodeTypes.Add((uint) bebopType.OpCode, bebopType);
                    }
                    definedTypes.Add(bebopType);
                }
            }

            _opcodeTypes = opcodeTypes;
            DefinedTypes = definedTypes;
        }
    }
}