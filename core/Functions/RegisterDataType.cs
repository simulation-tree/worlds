using System;
using Types;

namespace Worlds.Functions
{
    /// <summary>
    /// Function for registered a type with a <see cref="Schema"/>.
    /// </summary>
    public unsafe readonly struct RegisterDataType : IEquatable<RegisterDataType>
    {
        private readonly Schema schema;
        private readonly Action<Input> action;

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj)
        {
            return obj is RegisterDataType type && Equals(type);
        }

        /// <inheritdoc/>
        public readonly bool Equals(RegisterDataType other)
        {
            return schema == other.schema && action == other.action;
        }

        /// <inheritdoc/>
        public readonly override int GetHashCode()
        {
            return HashCode.Combine(schema, action);
        }

        /// <inheritdoc/>
        public static bool operator ==(RegisterDataType left, RegisterDataType right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(RegisterDataType left, RegisterDataType right)
        {
            return !(left == right);
        }

        /// <summary>
        /// The input data for the <see cref="RegisterDataType"/> function.
        /// </summary>
        public readonly struct Input
        {
            /// <summary>
            /// Schema thats being loaded.
            /// </summary>
            public readonly Schema schema;

            /// <summary>
            /// The type being registered.
            /// </summary>
            public readonly TypeLayout type;

            /// <summary>
            /// Specifies what kind of data type <see cref="type"/> refers to.
            /// </summary>
            public readonly DataType.Kind kind;

            /// <inheritdoc/>
            public Input(Schema schema, TypeLayout type, DataType.Kind kind)
            {
                this.schema = schema;
                this.type = type;
                this.kind = kind;
            }
        }
    }
}