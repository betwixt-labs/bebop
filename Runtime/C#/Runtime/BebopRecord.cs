using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Bebop.Attributes;
using Bebop.Exceptions;
using Bebop.Extensions;

namespace Bebop.Runtime
{
    /// <summary>
    ///     A virtual wrapper for a Bebop record
    /// </summary>
    public class BebopRecord
    {
        /// <summary>
        ///     Don't allow this type to be created manually.
        /// </summary>
        protected BebopRecord()
        {
        }

        ///<inheritdoc cref="BebopRecord{T}.Type"/>
        public virtual Type Type { get; } = null!;

        ///<inheritdoc cref="BebopRecord{T}.OpCode"/>
        // ReSharper disable once UnassignedGetOnlyAutoProperty
        public virtual uint? OpCode { get; }

        ///<inheritdoc cref="BebopRecord{T}.Encode"/>
        public virtual byte[] Encode(object record) => throw new NotImplementedException();

        ///<inheritdoc cref="BebopRecord{T}.Encode{T}"/>
        public virtual byte[] Encode<T>(T record) where T : class, new() => throw new NotImplementedException();

        ///<inheritdoc cref="BebopRecord{T}.Decode{T}"/>
        public virtual T Decode<T>(byte[] data) where T : class, new() => throw new NotImplementedException();

        ///<inheritdoc cref="BebopRecord{T}.Decode"/>
        public virtual object Decode(byte[] data) => throw new NotImplementedException();

        ///<inheritdoc cref="BebopRecord{T}.Equals(BebopRecord)"/>
        public virtual bool Equals(BebopRecord? other) => throw new NotImplementedException();

        ///<inheritdoc cref="BebopRecord{T}.AssignHandler"/>
        internal virtual void AssignHandler(MethodInfo methodInfo,
            object handlerInstance) => throw new NotImplementedException();

        ///<inheritdoc cref="BebopRecord{T}.InvokeHandler{T}"/>
        public virtual void InvokeHandler<T>(object state, T record) => throw new NotImplementedException();

        ///<inheritdoc cref="BebopRecord{T}.InvokeHandler(object, object)"/>
        public virtual void InvokeHandler(object state, object record) => throw new NotImplementedException();
    }

    /// <summary>
    ///     A concrete wrapper for defined Bebop records
    /// </summary>
    /// <typeparam name="T">
    ///     The type of a sealed Bebop implementation that was marked with <see cref="BebopRecordAttribute"/>
    ///     and corresponds to a defined Bebop type
    /// </typeparam>
    public sealed class BebopRecord<T> : BebopRecord, IEquatable<BebopRecord> where T : class, new()
    {
        /// <summary>
        ///     A delegate to the static decode method of <typeparamref name="T"/>
        /// </summary>
        private readonly Func<byte[], T> _decodeDelegate;

        /// <summary>
        ///     A delegate to the static encode method of <typeparamref name="T"/>
        /// </summary>
        private readonly Func<T, byte[]> _encodeDelegate;

        private bool _handlerIsAsync;

        private Func<object, T, Task>? _handlerTaskDelegate;

        private Func<object, T, ValueTask>? _handlerValueTaskDelegate;

        private Action<object, T>? _handlerVoidDelegate;

        /// <summary>
        ///     Creates a new <see cref="BebopRecord{T}"/> instance
        /// </summary>
        /// <param name="type">The underlying <see cref="System.Type"/> that belongs to <typeparamref name="T"/></param>
        /// <param name="encodeMethod">
        ///     The <see cref="MethodInfo"/> for static encode method that corresponds to
        ///     <typeparamref name="T"/>
        /// </param>
        /// <param name="decodeMethod">
        ///     The <see cref="MethodInfo"/> for static decode method that corresponds to
        ///     <typeparamref name="T"/>
        /// </param>
        /// <param name="opcode">The opcode constant (if any) that is in <typeparamref name="T"/></param>
        private BebopRecord(Type type, MethodInfo encodeMethod, MethodInfo decodeMethod, uint? opcode)
        {
            // create delegates to the encode and decode methods so we have a pointer.
            _encodeDelegate = (encodeMethod.CreateDelegate(typeof(Func<T, byte[]>)) as Func<T, byte[]>)!;
            _decodeDelegate = (decodeMethod.CreateDelegate(typeof(Func<byte[], T>)) as Func<byte[], T>)!;
            Type = type;
            OpCode = opcode;
        }

        /// <summary>
        ///     The opcode attribute constant
        /// </summary>
        public override uint? OpCode { get; }

        /// <summary>
        ///     The actual Bebop aggregate implementation type
        /// </summary>
        public override Type Type { get; }

        /// <summary>Determines whether the specified object is equal to the current object.</summary>
        /// <param name="other">The <see cref="BebopRecord"/> to compare with the current object.</param>
        /// <returns>
        ///     <see langword="true"/> if the specified <see cref="BebopRecord"/> is equal to the current <see cref="BebopRecord"/>
        ///     ;
        ///     otherwise, <see langword="false"/>.
        /// </returns>
        public override bool Equals(BebopRecord? other)
            => other is not null && Type == other.Type && OpCode == other.OpCode;

        /// <summary>
        ///     Invokes the method assigned to the current record
        /// </summary>
        /// <param name="state">An object that will be passed to the invoked handler.</param>
        /// <param name="record">An instance of <typeparamref name="T"/></param>
        private void InvokeHandler(object state, T record)
        {
            if (_handlerIsAsync)
            {
                Task.Run(async () =>
                    {
                        if (_handlerVoidDelegate is not null)
                        {
                            _handlerVoidDelegate(state, record);
                        }
                        else if (_handlerValueTaskDelegate is not null)
                        {
                            await _handlerValueTaskDelegate(state, record).ConfigureAwait(false);
                        }
                        else if (_handlerTaskDelegate is not null)
                        {
                            await _handlerTaskDelegate(state, record).ConfigureAwait(false);
                        }
                    })
                    .Forget();
            }
            else
            {
                if (_handlerVoidDelegate is not null)
                {
                    _handlerVoidDelegate(state, record);
                }
                else if (_handlerValueTaskDelegate is not null)
                {
                    _handlerValueTaskDelegate(state, record).ConfigureAwait(false);
                }
                else if (_handlerTaskDelegate is not null)
                {
                    _handlerTaskDelegate(state, record).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        ///     Invokes the method assigned to the current record
        /// </summary>
        /// <param name="state">An object that will be passed to the invoked handler.</param>
        /// <param name="record">An instance of <typeparamref name="T"/></param>
        /// <exception cref="BebopRuntimeException"></exception>
        public override void InvokeHandler<T1>(object state, T1 record)
        {
            if (record is not T r)
            {
                throw new BebopRuntimeException($"The provided record '{typeof(T1)}' cannot be assigned to the defined type '{typeof(T)}'");
            }
            if (_handlerTaskDelegate is null && _handlerValueTaskDelegate is null && _handlerVoidDelegate is null)
            {
                throw new BebopRuntimeException($"No handler is assigned to '{typeof(T)}'");
            }
            InvokeHandler(state, r);
        }

        /// <summary>
        ///     Invokes the method assigned to the current record
        /// </summary>
        /// <param name="state">An object that will be passed to the invoked handler.</param>
        /// <param name="record">An instance of <typeparamref name="T"/></param>
        /// <exception cref="BebopRuntimeException"></exception>
        public override void InvokeHandler(object state, object record)
        {
            if (record is not T r)
            {
                throw new BebopRuntimeException($"The provided record '{record.GetType()}' cannot be assigned to the defined type '{typeof(T)}'");
            }
            if (_handlerTaskDelegate is null && _handlerValueTaskDelegate is null && _handlerVoidDelegate is null)
            {
                throw new BebopRuntimeException($"No handler is assigned to '{typeof(T)}'");
            }
            InvokeHandler(state, r);
        }

        /// <summary>
        ///     Creates and assigns the delegate responsible for handling the current record
        /// </summary>
        /// <param name="methodInfo">
        ///     The <see cref="MethodInfo"/> of the function marked with <see cref="BindRecordAttribute"/>
        /// </param>
        /// <param name="handlerInstance">
        ///     An instance of the class marked with <see cref="RecordHandlerAttribute"/> where the
        ///     provided <paramref name="methodInfo"/> was found.
        /// </param>
        internal override void AssignHandler(MethodInfo methodInfo, object handlerInstance)
        {
            var paramTypes = methodInfo.GetParameters().Select(p => p.ParameterType);
            Type delegateType = Expression.GetDelegateType(paramTypes.Append(methodInfo.ReturnType).ToArray());
            var isStaticHandler = handlerInstance is StaticClass;

            if (methodInfo.IsValueTask())
            {
                _handlerValueTaskDelegate =
                    (Func<object, T, ValueTask>) (isStaticHandler
                        ? Delegate.CreateDelegate(delegateType,
                            methodInfo)
                        : Delegate.CreateDelegate(delegateType, handlerInstance,
                            methodInfo));
            }
            else if (methodInfo.IsTask())
            {
                _handlerTaskDelegate =
                    (Func<object, T, Task>) (isStaticHandler
                        ? Delegate.CreateDelegate(delegateType,
                            methodInfo)
                        : Delegate.CreateDelegate(delegateType, handlerInstance, methodInfo));
            }
            else
            {
                _handlerVoidDelegate =
                    (Action<object, T>) (isStaticHandler
                        ? Delegate.CreateDelegate(delegateType,
                            methodInfo)
                        : Delegate.CreateDelegate(delegateType, handlerInstance, methodInfo));
            }
            _handlerIsAsync = methodInfo.IsAsync();
        }

        /// <summary>
        ///     Encodes the specified <paramref name="record"/> into a buffer.
        /// </summary>
        /// <param name="record">An object that aligns with <typeparamref name="T"/></param>
        /// <returns>A buffer containing the encoded bytes of the provided  <paramref name="record"/></returns>
        /// <exception cref="BebopRuntimeException">
        ///     Thrown when the specified <paramref name="record"/> does not align with
        ///     <typeparamref name="T"/>.
        /// </exception>
        public override byte[] Encode(object record) => record is not T m
            ? throw new BebopRuntimeException(typeof(T), record.GetType())
            : _encodeDelegate(m);

        /// <summary>
        ///     Decodes a buffer into <typeparamref name="T"/>
        /// </summary>
        /// <param name="data">The buffer containing the encoded <typeparamref name="T"/> data</param>
        /// <returns>An instance of <typeparamref name="T"/> as an <see cref="object"/></returns>
        public override object Decode(byte[] data) => Decode<T>(data);

        /// <summary>
        ///     Encodes <typeparamref name="T1"/> into a buffer.
        /// </summary>
        /// <typeparam name="T1">A type that aligns with <typeparamref name="T"/></typeparam>
        /// <param name="record">An instance of <typeparamref name="T1"/></param>
        /// <returns>A buffer containing the encoded bytes of the provided  <paramref name="record"/></returns>
        /// <exception cref="BebopRuntimeException">
        ///     Thrown when <typeparamref name="T"/> and <typeparamref name="T1"/> are
        ///     compatible types.
        /// </exception>
        public override byte[] Encode<T1>(T1 record) => record is not T t
            ? throw new BebopRuntimeException(typeof(T), typeof(T1))
            : _encodeDelegate(t);

        /// <summary>
        ///     Decodes a buffer into the specified <typeparamref name="T1"/>
        /// </summary>
        /// <typeparam name="T1">A type that aligns with <typeparamref name="T"/></typeparam>
        /// <param name="data">An encoded <typeparamref name="T1"/> or <typeparamref name="T"/> aligned message</param>
        /// <returns>An instance of <typeparamref name="T1"/></returns>
        /// <exception cref="BebopRuntimeException">
        ///     Thrown when <typeparamref name="T"/> and <typeparamref name="T1"/> are
        ///     compatible types.
        /// </exception>
        public override T1 Decode<T1>(byte[] data) => _decodeDelegate(data) is T1 decoded
            ? decoded
            : throw new BebopRuntimeException(typeof(T), typeof(T1));

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj switch
        {
            BebopRecord type => Equals(type),
            null => false,
            _ => false
        };

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 31 + OpCode.GetHashCode();
                hash = hash * 31 + Type.GetHashCode();
                return hash;
            }
        }
    }
}