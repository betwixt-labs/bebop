﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:1.0.0.0
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
using Bebop;
using Bebop.Attributes;
using Bebop.Runtime;

//
// This source code was auto-generated by bebopc, Version=1.0.0.0.
//
namespace Gateway.Contracts
{
    /// <summary>
    /// The current state of a user connection to a lobby
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCode("bebopc", "1.0.0.0")]
    public enum LobbyConnectionState : uint
    {
        Invalid = 0
    }

    /// <summary>
    /// Represents a message received by the gateway
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCode("bebopc", "1.0.0.0")]
    public abstract class BaseGatewayMessage
    {
        public const uint OpCode = 0x45544147;
        /// <summary>
        /// The opcode of the child message
        /// </summary>
        public uint ChildOpcode { get; init; }
        /// <summary>
        /// A bebop encoded message
        /// </summary>
        public byte[] Buffer { get; init; }
    }

    /// <inheritdoc />
    [System.CodeDom.Compiler.GeneratedCode("bebopc", "1.0.0.0")]
    [BebopRecord]
    public sealed class GatewayMessage : BaseGatewayMessage
    {
        [System.CodeDom.Compiler.GeneratedCode("bebopc", "1.0.0.0")]
        [System.Runtime.CompilerServices.MethodImpl(BebopView.HotPath)]
        public static byte[] Encode(BaseGatewayMessage message)
        {
            var view = new BebopView();
            EncodeInto(message, ref view);
            return view.ToArray();
        }

        [System.CodeDom.Compiler.GeneratedCode("bebopc", "1.0.0.0")]
        [System.Runtime.CompilerServices.MethodImpl(BebopView.HotPath)]
        public byte[] Encode()
        {
            var view = new BebopView();
            EncodeInto(this, ref view);
            return view.ToArray();
        }
        [System.Runtime.CompilerServices.MethodImpl(BebopView.HotPath)]
        [System.CodeDom.Compiler.GeneratedCode("bebopc", "1.0.0.0")]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        internal static void EncodeInto(BaseGatewayMessage message, ref BebopView view)
        {
            view.WriteUInt32(message.ChildOpcode);
            view.WriteBytes(message.Buffer);
        }

        [System.CodeDom.Compiler.GeneratedCode("bebopc", "1.0.0.0")]
        [System.Runtime.CompilerServices.MethodImpl(BebopView.HotPath)]
        public static T DecodeAs<T>(byte[] message) where T : BaseGatewayMessage, new()
        {
            var view = BebopView.From(message);
            return DecodeFrom<T>(ref view);
        }

        [System.CodeDom.Compiler.GeneratedCode("bebopc", "1.0.0.0")]
        [System.Runtime.CompilerServices.MethodImpl(BebopView.HotPath)]
        public static GatewayMessage Decode(byte[] message)
        {
            var view = BebopView.From(message);
            return DecodeFrom(ref view);
        }
        [System.Runtime.CompilerServices.MethodImpl(BebopView.HotPath)]
        [System.CodeDom.Compiler.GeneratedCode("bebopc", "1.0.0.0")]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        internal static GatewayMessage DecodeFrom(ref BebopView view)
        {
            uint field0;
            field0 = view.ReadUInt32();
            byte[] field1;
            field1 = view.ReadBytes();
            return new GatewayMessage
            {
                ChildOpcode = field0,
                Buffer = field1,
            };
        }
        [System.Runtime.CompilerServices.MethodImpl(BebopView.HotPath)]
        [System.CodeDom.Compiler.GeneratedCode("bebopc", "1.0.0.0")]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        internal static T DecodeFrom<T>(ref BebopView view) where T : BaseGatewayMessage, new()
        {
            uint field0;
            field0 = view.ReadUInt32();
            byte[] field1;
            field1 = view.ReadBytes();
            return new T
            {
                ChildOpcode = field0,
                Buffer = field1,
            };
        }
    }
    /// <summary>
    /// A structure which represents an incoming chat message sent by a user
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCode("bebopc", "1.0.0.0")]
    public abstract class BaseChatMessage
    {
        public const uint OpCode = 0x54414843;
        /// <summary>
        /// the time at which the message was sent to the gateway
        /// </summary>
        public System.DateTime SendTime { get; init; }
        /// <summary>
        /// the GUID of the lobby the message should be forwarded to
        /// </summary>
        public System.Guid LobbyId { get; init; }
        /// <summary>
        /// the contents of the chat message
        /// </summary>
        public string Content { get; init; }
    }

    /// <inheritdoc />
    [System.CodeDom.Compiler.GeneratedCode("bebopc", "1.0.0.0")]
    [BebopRecord]
    public sealed class ChatMessage : BaseChatMessage
    {
        [System.CodeDom.Compiler.GeneratedCode("bebopc", "1.0.0.0")]
        [System.Runtime.CompilerServices.MethodImpl(BebopView.HotPath)]
        public static byte[] Encode(BaseChatMessage message)
        {
            var view = new BebopView();
            EncodeInto(message, ref view);
            return view.ToArray();
        }

        [System.CodeDom.Compiler.GeneratedCode("bebopc", "1.0.0.0")]
        [System.Runtime.CompilerServices.MethodImpl(BebopView.HotPath)]
        public byte[] Encode()
        {
            var view = new BebopView();
            EncodeInto(this, ref view);
            return view.ToArray();
        }
        [System.Runtime.CompilerServices.MethodImpl(BebopView.HotPath)]
        [System.CodeDom.Compiler.GeneratedCode("bebopc", "1.0.0.0")]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        internal static void EncodeInto(BaseChatMessage message, ref BebopView view)
        {
            view.WriteDate(message.SendTime);
            view.WriteGuid(message.LobbyId);
            view.WriteString(message.Content);
        }

        [System.CodeDom.Compiler.GeneratedCode("bebopc", "1.0.0.0")]
        [System.Runtime.CompilerServices.MethodImpl(BebopView.HotPath)]
        public static T DecodeAs<T>(byte[] message) where T : BaseChatMessage, new()
        {
            var view = BebopView.From(message);
            return DecodeFrom<T>(ref view);
        }

        [System.CodeDom.Compiler.GeneratedCode("bebopc", "1.0.0.0")]
        [System.Runtime.CompilerServices.MethodImpl(BebopView.HotPath)]
        public static ChatMessage Decode(byte[] message)
        {
            var view = BebopView.From(message);
            return DecodeFrom(ref view);
        }
        [System.Runtime.CompilerServices.MethodImpl(BebopView.HotPath)]
        [System.CodeDom.Compiler.GeneratedCode("bebopc", "1.0.0.0")]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        internal static ChatMessage DecodeFrom(ref BebopView view)
        {
            System.DateTime field0;
            field0 = view.ReadDate();
            System.Guid field1;
            field1 = view.ReadGuid();
            string field2;
            field2 = view.ReadString();
            return new ChatMessage
            {
                SendTime = field0,
                LobbyId = field1,
                Content = field2,
            };
        }
        [System.Runtime.CompilerServices.MethodImpl(BebopView.HotPath)]
        [System.CodeDom.Compiler.GeneratedCode("bebopc", "1.0.0.0")]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        internal static T DecodeFrom<T>(ref BebopView view) where T : BaseChatMessage, new()
        {
            System.DateTime field0;
            field0 = view.ReadDate();
            System.Guid field1;
            field1 = view.ReadGuid();
            string field2;
            field2 = view.ReadString();
            return new T
            {
                SendTime = field0,
                LobbyId = field1,
                Content = field2,
            };
        }
    }
    /// <summary>
    /// A structure that represents a user connected to a lobby
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCode("bebopc", "1.0.0.0")]
    public abstract class BaseLobbyUser
    {
        /// <summary>
        /// the unique ID of the user
        /// </summary>
        public System.Guid Id { get; set; }
        /// <summary>
        /// the display name of the user  visible to other members of the lobby
        /// </summary>
        public string DisplayName { get; set; }
        /// <summary>
        /// the time at which the user joined the lobby
        /// </summary>
        public System.DateTime JoinTime { get; set; }
        /// <summary>
        /// the current state of the user in the lobby
        /// </summary>
        public LobbyConnectionState State { get; set; }
    }

    /// <inheritdoc />
    [System.CodeDom.Compiler.GeneratedCode("bebopc", "1.0.0.0")]
    [BebopRecord]
    public sealed class LobbyUser : BaseLobbyUser
    {
        [System.CodeDom.Compiler.GeneratedCode("bebopc", "1.0.0.0")]
        [System.Runtime.CompilerServices.MethodImpl(BebopView.HotPath)]
        public static byte[] Encode(BaseLobbyUser message)
        {
            var view = new BebopView();
            EncodeInto(message, ref view);
            return view.ToArray();
        }

        [System.CodeDom.Compiler.GeneratedCode("bebopc", "1.0.0.0")]
        [System.Runtime.CompilerServices.MethodImpl(BebopView.HotPath)]
        public byte[] Encode()
        {
            var view = new BebopView();
            EncodeInto(this, ref view);
            return view.ToArray();
        }
        [System.Runtime.CompilerServices.MethodImpl(BebopView.HotPath)]
        [System.CodeDom.Compiler.GeneratedCode("bebopc", "1.0.0.0")]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        internal static void EncodeInto(BaseLobbyUser message, ref BebopView view)
        {
            view.WriteGuid(message.Id);
            view.WriteString(message.DisplayName);
            view.WriteDate(message.JoinTime);
            view.WriteEnum<LobbyConnectionState>(message.State);
        }

        [System.CodeDom.Compiler.GeneratedCode("bebopc", "1.0.0.0")]
        [System.Runtime.CompilerServices.MethodImpl(BebopView.HotPath)]
        public static T DecodeAs<T>(byte[] message) where T : BaseLobbyUser, new()
        {
            var view = BebopView.From(message);
            return DecodeFrom<T>(ref view);
        }

        [System.CodeDom.Compiler.GeneratedCode("bebopc", "1.0.0.0")]
        [System.Runtime.CompilerServices.MethodImpl(BebopView.HotPath)]
        public static LobbyUser Decode(byte[] message)
        {
            var view = BebopView.From(message);
            return DecodeFrom(ref view);
        }
        [System.Runtime.CompilerServices.MethodImpl(BebopView.HotPath)]
        [System.CodeDom.Compiler.GeneratedCode("bebopc", "1.0.0.0")]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        internal static LobbyUser DecodeFrom(ref BebopView view)
        {
            System.Guid field0;
            field0 = view.ReadGuid();
            string field1;
            field1 = view.ReadString();
            System.DateTime field2;
            field2 = view.ReadDate();
            LobbyConnectionState field3;
            field3 = view.ReadEnum<LobbyConnectionState>();
            return new LobbyUser
            {
                Id = field0,
                DisplayName = field1,
                JoinTime = field2,
                State = field3,
            };
        }
        [System.Runtime.CompilerServices.MethodImpl(BebopView.HotPath)]
        [System.CodeDom.Compiler.GeneratedCode("bebopc", "1.0.0.0")]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        internal static T DecodeFrom<T>(ref BebopView view) where T : BaseLobbyUser, new()
        {
            System.Guid field0;
            field0 = view.ReadGuid();
            string field1;
            field1 = view.ReadString();
            System.DateTime field2;
            field2 = view.ReadDate();
            LobbyConnectionState field3;
            field3 = view.ReadEnum<LobbyConnectionState>();
            return new T
            {
                Id = field0,
                DisplayName = field1,
                JoinTime = field2,
                State = field3,
            };
        }
    }
    [System.CodeDom.Compiler.GeneratedCode("bebopc", "1.0.0.0")]
    public abstract class BaseLobby
    {
        public BaseLobbyUser[] Users { get; set; }
        public System.DateTime CreationTime { get; set; }
        public string DisplayName { get; set; }
    }

    /// <inheritdoc />
    [System.CodeDom.Compiler.GeneratedCode("bebopc", "1.0.0.0")]
    [BebopRecord]
    public sealed class Lobby : BaseLobby
    {
        [System.CodeDom.Compiler.GeneratedCode("bebopc", "1.0.0.0")]
        [System.Runtime.CompilerServices.MethodImpl(BebopView.HotPath)]
        public static byte[] Encode(BaseLobby message)
        {
            var view = new BebopView();
            EncodeInto(message, ref view);
            return view.ToArray();
        }

        [System.CodeDom.Compiler.GeneratedCode("bebopc", "1.0.0.0")]
        [System.Runtime.CompilerServices.MethodImpl(BebopView.HotPath)]
        public byte[] Encode()
        {
            var view = new BebopView();
            EncodeInto(this, ref view);
            return view.ToArray();
        }
        [System.Runtime.CompilerServices.MethodImpl(BebopView.HotPath)]
        [System.CodeDom.Compiler.GeneratedCode("bebopc", "1.0.0.0")]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        internal static void EncodeInto(BaseLobby message, ref BebopView view)
        {
            {
                var length0 = unchecked((uint)message.Users.Length);
                view.WriteUInt32(length0);
                for (var i0 = 0; i0 < length0; i0++)
                {
                    Gateway.Contracts.LobbyUser.EncodeInto(message.Users[i0], ref view);
                }
            }
            view.WriteDate(message.CreationTime);
            view.WriteString(message.DisplayName);
        }

        [System.CodeDom.Compiler.GeneratedCode("bebopc", "1.0.0.0")]
        [System.Runtime.CompilerServices.MethodImpl(BebopView.HotPath)]
        public static T DecodeAs<T>(byte[] message) where T : BaseLobby, new()
        {
            var view = BebopView.From(message);
            return DecodeFrom<T>(ref view);
        }

        [System.CodeDom.Compiler.GeneratedCode("bebopc", "1.0.0.0")]
        [System.Runtime.CompilerServices.MethodImpl(BebopView.HotPath)]
        public static Lobby Decode(byte[] message)
        {
            var view = BebopView.From(message);
            return DecodeFrom(ref view);
        }
        [System.Runtime.CompilerServices.MethodImpl(BebopView.HotPath)]
        [System.CodeDom.Compiler.GeneratedCode("bebopc", "1.0.0.0")]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        internal static Lobby DecodeFrom(ref BebopView view)
        {
            BaseLobbyUser[] field0;
            {
                var length0 = unchecked((int)view.ReadUInt32());
                field0 = new BaseLobbyUser[length0];
                for (var i0 = 0; i0 < length0; i0++)
                {
                    BaseLobbyUser x0;
                    x0 = Gateway.Contracts.LobbyUser.DecodeFrom(ref view);
                    field0[i0] = x0;
                }
            }
            System.DateTime field1;
            field1 = view.ReadDate();
            string field2;
            field2 = view.ReadString();
            return new Lobby
            {
                Users = field0,
                CreationTime = field1,
                DisplayName = field2,
            };
        }
        [System.Runtime.CompilerServices.MethodImpl(BebopView.HotPath)]
        [System.CodeDom.Compiler.GeneratedCode("bebopc", "1.0.0.0")]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        internal static T DecodeFrom<T>(ref BebopView view) where T : BaseLobby, new()
        {
            BaseLobbyUser[] field0;
            {
                var length0 = unchecked((int)view.ReadUInt32());
                field0 = new BaseLobbyUser[length0];
                for (var i0 = 0; i0 < length0; i0++)
                {
                    BaseLobbyUser x0;
                    x0 = Gateway.Contracts.LobbyUser.DecodeFrom(ref view);
                    field0[i0] = x0;
                }
            }
            System.DateTime field1;
            field1 = view.ReadDate();
            string field2;
            field2 = view.ReadString();
            return new T
            {
                Users = field0,
                CreationTime = field1,
                DisplayName = field2,
            };
        }
    }
    /// <summary>
    /// Hello World
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCode("bebopc", "1.0.0.0")]
    public enum Example : uint
    {
        A = 0,
        B = 1,
        C = 2
    }
}