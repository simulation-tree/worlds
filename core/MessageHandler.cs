using Simulation.Functions;
using System;
using Unmanaged;

namespace Simulation
{
    public readonly struct MessageHandler : IEquatable<MessageHandler>
    {
        public readonly nint messageType;
        public readonly HandleFunction function;

        public readonly Type MessageType
        {
            get
            {
                RuntimeTypeHandle handle = RuntimeTypeHandle.FromIntPtr(messageType);
                return Type.GetTypeFromHandle(handle) ?? throw new();
            }
        }

        public MessageHandler(nint messageType, HandleFunction function)
        {
            this.messageType = messageType;
            this.function = function;
        }

        public readonly uint ToString(USpan<char> buffer)
        {
            string name = MessageType.Name;
            name.CopyTo(buffer.AsSystemSpan());
            return (uint)name.Length;
        }

        public readonly override string ToString()
        {
            USpan<char> buffer = stackalloc char[256];
            uint length = ToString(buffer);
            return buffer.Slice(0, length).ToString();
        }

        public readonly override bool Equals(object? obj)
        {
            return obj is MessageHandler handler && Equals(handler);
        }

        public readonly bool Equals(MessageHandler other)
        {
            return messageType.Equals(other.messageType) && function.Equals(other.function);
        }

        public readonly override int GetHashCode()
        {
            return HashCode.Combine(messageType, function);
        }

        public static MessageHandler Create<T>(HandleFunction function) where T : unmanaged
        {
            return new(RuntimeTypeHandle.ToIntPtr(typeof(T).TypeHandle), function);
        }

        public static bool operator ==(MessageHandler left, MessageHandler right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(MessageHandler left, MessageHandler right)
        {
            return !(left == right);
        }
    }
}