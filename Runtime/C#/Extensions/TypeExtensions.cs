using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Bebop.Attributes;
using Bebop.Exceptions;
using Bebop.Runtime;

namespace Bebop.Extensions
{
    /// <summary>
    /// A dummy type for identifying static classes in <see cref="BebopMirror"/> without using null.
    /// </summary>
    internal class StaticClass { }
    /// <summary>
    ///     Handy dandy extension methods for working with <see cref="Type"/>
    /// </summary>
    internal static class TypeExtensions
    {
        /// <summary>
        ///     Attempts to find the generated static decode method of the specified Bebop <paramref name="type"/>
        /// </summary>
        /// <param name="type">A type that was created by the Bebop compiler.</param>
        /// <returns>The <see cref="MethodInfo"/> of "Decode"</returns>
        /// <exception cref="BebopRuntimeException">
        ///     Thrown when the decode method of the specified <paramref name="type"/> cannot be loaded.
        /// </exception>
        private static MethodInfo GetStaticDecode(this Type type)
        {
            var decodeMethod = type.GetMethod("Decode",
                BindingFlags.Static | BindingFlags.Public,
                null,
                new[] {typeof(byte[])},
                null);
            if (decodeMethod is null)
            {
                throw new BebopRuntimeException($"Unable to find static decode method in '{type.FullName}'");
            }
            return decodeMethod;
        }

        /// <summary>
        ///     Converts a <see cref="Type"/> into a <see cref="BebopRecord{T}"/>
        /// </summary>
        /// <param name="type">A type that was marked with <see cref="BebopRecordAttribute"/></param>
        /// <returns>
        ///     An instance of <see cref="BebopRecord"/> that can be used to virtually access <see cref="BebopRecord{T}"/>
        /// </returns>
        /// <exception cref="BebopRuntimeException"/>
        internal static BebopRecord ToBebopRecord(this Type type)
        {
            const string opcodeFieldName = "OpCode";

            if (type.BaseType is null)
            {
                throw new BebopRuntimeException($"Provided type '{type.FullName}' does not contain a base class.");
            }

            var decodeMethod = type.GetStaticDecode();
            var encodeMethod = type.GetStaticEncode();

            var constructor = typeof(BebopRecord<>).MakeGenericType(type)
                .GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault();

            if (constructor is null)
            {
                throw new BebopRuntimeException(
                    $"Cannot locate constructor for BebopType<T> using '{type.FullName}'");
            }

            uint? opcode = type.BaseType.GetField(opcodeFieldName)?.GetRawConstantValue() is uint v ? v : null;

            return (BebopRecord) constructor.Invoke(new object[] {type, encodeMethod, decodeMethod, opcode!});
        }

        /// <summary>
        ///     Attempts to find the generated static encode method of the specified Bebop <paramref name="type"/>
        /// </summary>
        /// <param name="type">A type that was created by the Bebop compiler.</param>
        /// <returns>The <see cref="MethodInfo"/> of "Encode"</returns>
        /// <exception cref="BebopRuntimeException">
        ///     Thrown when the encode method of the specified <paramref name="type"/> cannot be loaded.
        /// </exception>
        private static MethodInfo GetStaticEncode(this Type type)
        {
            Debug.Assert(type.BaseType is not null, "type.BaseType is not null");
            var encodeMethod = type.GetMethod("Encode",
                BindingFlags.Static | BindingFlags.Public,
                null,
                new[] {type.BaseType},
                null);

            if (encodeMethod is null)
            {
                throw new BebopRuntimeException($"Unable to find static encode method in '{type.FullName}'");
            }
            return encodeMethod;
        }

        /// <summary>
        ///     Determines if a method is asynchronous.
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        internal static bool IsAsync(this MethodInfo info)
            => info.GetCustomAttribute<AsyncStateMachineAttribute>() is not null;

        internal static bool IsVoid(this MethodInfo info) => info.ReturnType == typeof(void);

        internal static bool IsValueTask(this MethodInfo info) => info.ReturnType == typeof(ValueTask);

        internal static bool IsTask(this MethodInfo info) => info.ReturnType == typeof(Task);

        /// <summary>
        ///     Retrieves all methods marked with <see cref="BindRecordAttribute"/> in the specified <paramref name="type"/>
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal static IEnumerable<(MethodInfo, BindRecordAttribute)> GetBoundMethods(this Type type)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }
            foreach (var methodInfo in type.GetMethods(type.IsStaticClass() ? BindingFlags.Public | BindingFlags.Static : BindingFlags.Public | BindingFlags.Instance))
            {
                if (methodInfo.GetCustomAttribute<BindRecordAttribute>() is {RecordType: not null} attribute)
                {
                    yield return (methodInfo, attribute);
                }
            }
        }

        internal static Type GetGenericArgument(this Type type) => type.GetGenericArguments().FirstOrDefault()!;

        /// <summary>
        ///     Resolves the binding info for the specified <paramref name="type"/>
        /// </summary>
        /// <param name="handlers"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        internal static (MethodInfo Method, Type RecordType) FindBindingInfo(
            this Dictionary<Type, List<(MethodInfo Method, Type RecordType)>> handlers,
            Type type)
        {
            foreach (var binding in from bindings in handlers.Values
                from binding in bindings
                where binding.RecordType == type
                select binding)
            {
                return binding;
            }
            throw new BebopRuntimeException($"No binding information was found for {type}");
        }

        /// <summary>
        ///     Determines if a collection of handlers is valid.
        /// </summary>
        /// <param name="handlers">The handler collection to check.</param>
        /// <exception cref="BebopRuntimeException">
        ///     Thrown when the collection contains duplicate bind entries
        /// </exception>
        internal static void ValidateHandlers(
            this Dictionary<Type, List<(MethodInfo Method, Type RecordType)>> handlers)
        {
            foreach (var handler in handlers)
            {
                foreach (var binding in handler.Value)
                {
                    foreach (var subHandler in handlers.Where(subHandler => subHandler.Key != handler.Key)
                        .Where(subHandler => subHandler.Value.Any(l => l.RecordType == binding.RecordType)))
                    {
                        throw new BebopRuntimeException(
                            $"Duplicate bindings found for '{binding.RecordType}' in '{handler.Key}' and '{subHandler.Key}'");
                    }
                }
            }
        }

        /// <summary>
        ///     Determines if a method marked with the <see cref="BindRecordAttribute"/> has the correct positional arguments.
        /// </summary>
        /// <param name="info">The method to check.</param>
        /// <param name="recordType">The expected resolved record type.</param>
        internal static void ValidateSignature(this MethodInfo info, Type? recordType)
        {
            if (recordType is null)
            {
                throw new BebopRuntimeException("record type null");
            }
            var parameters = info.GetParameters();
            if (parameters[0].ParameterType != typeof(object))
            {
                throw new BebopRuntimeException("Invalid signature: null");
            }
            if (parameters[1].ParameterType != recordType)
            {
                throw new BebopRuntimeException("Invalid signature: mismatch");
            }
        }

        /// <summary>
        ///     Determines if a method returns a value.
        /// </summary>
        /// <param name="info">The method to inspect.</param>
        /// <returns>
        ///     <see langword="true"/> if the specified method returns a value,  otherwise <see langword="false"/>.
        /// </returns>
        internal static bool HasReturn(this MethodInfo info) => !info.IsTask() && !info.IsValueTask() && !info.IsVoid();

        /// <summary>
        /// Determines if the specified <paramref name="type"/> is a static class.
        /// </summary>
        /// <param name="type">the type to check.</param>
        /// <returns>true if the class is static, otherwise false.</returns>
        internal static bool IsStaticClass(this Type type)
        {
            return type.GetConstructor(Type.EmptyTypes) == null && type.IsAbstract && type.IsSealed;
        }

        /// <summary>
        ///     Determines if the specified
        ///     <paramref name="type"/>
        ///     has a parameterless constructor
        /// </summary>
        /// <param name="type"></param>
        /// <returns>
        ///     <see langword="true"/> if the specified
        ///     <paramref name="type"/>
        ///     has a parameterless constructor;
        ///     otherwise, <see langword="false"/>.
        /// </returns>
        internal static bool HasDefaultConstructor(this Type type)
            => type.IsValueType || type.GetConstructor(Type.EmptyTypes) is not null;
        
        
        /// <summary>
        ///     Walks an assembly and finds types decorated with <see cref="BebopRecordAttribute"/>
        /// </summary>
        /// <param name="assembly">The assembly that will be scanned.</param>
        /// <returns>A collection of records that were generated by Bebop</returns>
        /// <remarks>
        ///     In the event an exception occurs due to a type being inaccessible, this method leverages the types made available
        ///     in <see cref="ReflectionTypeLoadException"/>
        /// </remarks>
        internal static IEnumerable<Type?> GetBebopRecordTypes(this Assembly assembly)
        {
            if (assembly is null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }
            try
            {
                return assembly.DefinedTypes.Where(t
                        => t?.GetCustomAttribute<BebopRecordAttribute>() is { Kind: BebopKind.Struct or BebopKind.Message})
                    .Select(t => t.AsType());
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types.Where(t => t?.GetCustomAttribute<BebopRecordAttribute>() is { Kind: BebopKind.Struct or BebopKind.Message });
            }
        }

        /// <summary>
        ///     Walks an assembly and finds types with the specified <typeparamref name="T"/>
        /// </summary>
        /// <param name="assembly">The assembly that will be scanned.</param>
        /// <returns>A collection of records that were generated by Bebop</returns>
        /// <remarks>
        ///     In the event an exception occurs due to a type being inaccessible, this method leverages the types made available
        ///     in <see cref="ReflectionTypeLoadException"/>
        /// </remarks>
        internal static IEnumerable<Type?> GetMarkedTypes<T>(this Assembly assembly) where T : Attribute
        {
            if (assembly is null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }
            try
            {
                return assembly.DefinedTypes.Where(t
                        => t.GetCustomAttribute<T>() is not null)
                    .Select(t => t.AsType());
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types.Where(t => t is not null && t.GetCustomAttribute<T>() is not null);
            }
        }
    }
}