using System;
using Types;

namespace Worlds.Functions
{
    public unsafe readonly struct RegisterDataType : IEquatable<RegisterDataType>
    {
        private readonly Schema schema;
        private readonly Action<Input> action;

        public RegisterDataType(Schema schema, Action<Input> action)
        {
            this.schema = schema;
            this.action = action;
        }

        /// <summary>
        /// Registers a type that could exist on an entity.
        /// </summary>
        public readonly void Invoke(TypeLayout type, DataType.Kind kind)
        {
            Input input = new(schema, type, kind);
            action(input);
        }

        public readonly override bool Equals(object? obj)
        {
            return obj is RegisterDataType type && Equals(type);
        }

        public readonly bool Equals(RegisterDataType other)
        {
            return schema == other.schema && action == other.action;
        }

        public readonly override int GetHashCode()
        {
            return HashCode.Combine(schema, action);
        }

        public static bool operator ==(RegisterDataType left, RegisterDataType right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(RegisterDataType left, RegisterDataType right)
        {
            return !(left == right);
        }

        public readonly struct Input
        {
            public readonly Schema schema;
            public readonly TypeLayout type;
            public readonly DataType.Kind kind;

            public Input(Schema schema, TypeLayout type, DataType.Kind kind)
            {
                this.schema = schema;
                this.type = type;
                this.kind = kind;
            }
        }
    }
}