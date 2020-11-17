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
        ///     A read-only dictionary of <see cref="BebopRecord{T}"/> types which are keyed via their opcode.
        /// </summary>
        /// <remarks>
        ///     We use a dictionary for records with opcodes because the lookup speed is O(1) while the lookup
        ///     performance of a List is an O(n) operation.
        /// </remarks>
        private static IReadOnlyDictionary<uint, BebopRecord> _opcodeTypes = null!;

        /// <summary>
        ///     A read-only list of all resolved <see cref="BebopRecord{T}"/> types.
        /// </summary>
        public static IReadOnlyList<BebopRecord> DefinedTypes = null!;

        /// <summary>
        ///     Returns a <see cref="BebopRecord"/> of the specified name that corresponds to the constructed
        ///     <see cref="BebopRecord{T}"/>.
        /// </summary>
        /// <param name="definedTypeName">The name of the defined Bebop type.</param>
        /// <returns>A virtual <see cref="BebopRecord"/> accessor.</returns>
        /// <exception cref="InvalidOperationException"/>
        public static BebopRecord GetType(string definedTypeName) => DefinedTypes
                .FirstOrDefault(definedType => definedType.Class.Name.Equals(definedTypeName)) ??
            throw new BebopRuntimeException($"A Bebop type named \"{definedTypeName}\" does not exist.");


        /// <summary>
        ///     Returns a <see cref="BebopRecord"/> of the specified opcode that corresponds to the constructed
        ///     <see cref="BebopRecord{T}"/>.
        /// </summary>
        /// <param name="opcode">The unique opcode belonging to desired record</param>
        /// <returns>A virtual <see cref="BebopRecord"/> accessor.</returns>
        public static BebopRecord GetOpCodeType(uint opcode)
            => (_opcodeTypes.TryGetValue(opcode, out var type) ? type : null) ??
                throw new BebopRuntimeException(opcode);

        /// <summary>
        ///     Resolves all Bebop records across assemblies within the current application domain.
        /// </summary>
        /// <remarks>
        ///     Should only be called once at the start of your application.
        /// </remarks>
        public static void ResolveRecords()
        {
            if (DefinedTypes is {Count: > 0})
            {
                throw new BebopRuntimeException("Bebop records may only be resolved once during runtime");
            }

            var definedTypes = new List<BebopRecord>();
            var opcodeTypes = new Dictionary<uint, BebopRecord>();


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
                    var bebopType = type.ToBebopRecord();
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