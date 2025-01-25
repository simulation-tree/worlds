using System;
using Types;

namespace Worlds.Functions
{
    public unsafe readonly struct RegisterDataType : IEquatable<RegisterDataType>
    {
        private readonly Schema schema;
        private readonly delegate* unmanaged<Input, void> function;

        public RegisterDataType(Schema schema, delegate* unmanaged<Input, void> function)
        {
            this.schema = schema;
            this.function = function;
        }

        /// <summary>
        /// Registers a type that could exist on an entity.
        /// </summary>
        public readonly void Invoke(TypeLayout type, DataType.Kind kind)
        {
            Input input = new(schema, type, kind);
            if (Schema.OnRegister is not null)
            {
                Schema.OnRegister.Invoke(input);
            }
            else
            {
                function(input);
            }
        }

        public readonly override bool Equals(object? obj)
        {
            return obj is RegisterDataType type && Equals(type);
        }

        public readonly bool Equals(RegisterDataType other)
        {
            return (nint)function == (nint)other.function;
        }

        public readonly override int GetHashCode()
        {
            return ((nint)function).GetHashCode();
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