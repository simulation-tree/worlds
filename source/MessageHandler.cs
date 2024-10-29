using Simulation.Functions;
using System;
using Unmanaged;

namespace Simulation
{
    public readonly struct MessageHandler : IEquatable<MessageHandler>
    {
        public readonly RuntimeType type;
        public readonly HandleFunction function;

        public MessageHandler(RuntimeType type, HandleFunction function)
        {
            this.type = type;
            this.function = function;
        }

        public static MessageHandler Create<T>(HandleFunction function) where T : unmanaged
        {
            return new(RuntimeType.Get<T>(), function);
        }

        public readonly override bool Equals(object? obj)
        {
            return obj is MessageHandler handler && Equals(handler);
        }

        public readonly bool Equals(MessageHandler other)
        {
            return type.Equals(other.type) && function.Equals(other.function);
        }

        public readonly override int GetHashCode()
        {
            return HashCode.Combine(type, function);
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