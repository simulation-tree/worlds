using Simulation.Functions;
using System;
using Unmanaged;

namespace Simulation
{
    /// <summary>
    /// Container of message handler information.
    /// </summary>
    public readonly struct MessageHandler : IEquatable<MessageHandler>
    {
        /// <summary>
        /// The <see cref="RuntimeTypeHandle"/> of message to handle.
        /// </summary>
        public readonly nint messageType;

        /// <summary>
        /// The function for handling.
        /// </summary>
        public readonly HandleFunction function;

        /// <summary>
        /// The <see cref="Type"/> of message to handle.
        /// </summary>
        public readonly Type MessageType
        {
            get
            {
                RuntimeTypeHandle handle = RuntimeTypeHandle.FromIntPtr(messageType);
                return Type.GetTypeFromHandle(handle) ?? throw new();
            }
        }

        /// <summary>
        /// Creates a new instance of the <see cref="MessageHandler"/> struct.
        /// </summary>
        public MessageHandler(nint messageType, HandleFunction function)
        {
            this.messageType = messageType;
            this.function = function;
        }

        /// <summary>
        /// Builds a string representation of the message handler.
        /// </summary>
        public readonly uint ToString(USpan<char> buffer)
        {
            string name = MessageType.Name;
            for (uint i = 0; i < name.Length; i++)
            {
                buffer[i] = name[(int)i];
            }

            return (uint)name.Length;
        }

        /// <inheritdoc/>
        public readonly override string ToString()
        {
            USpan<char> buffer = stackalloc char[256];
            uint length = ToString(buffer);
            return buffer.Slice(0, length).ToString();
        }

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj)
        {
            return obj is MessageHandler handler && Equals(handler);
        }

        /// <inheritdoc/>
        public readonly bool Equals(MessageHandler other)
        {
            return messageType.Equals(other.messageType) && function.Equals(other.function);
        }

        /// <inheritdoc/>
        public readonly override int GetHashCode()
        {
            return HashCode.Combine(messageType, function);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="MessageHandler"/> struct.
        /// </summary>
        public static MessageHandler Create<T>(HandleFunction function) where T : unmanaged
        {
            return new(RuntimeTypeHandle.ToIntPtr(typeof(T).TypeHandle), function);
        }

        /// <inheritdoc/>
        public static bool operator ==(MessageHandler left, MessageHandler right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(MessageHandler left, MessageHandler right)
        {
            return !(left == right);
        }
    }
}