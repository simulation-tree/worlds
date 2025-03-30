using System;
using Types;

namespace Worlds.Functions
{
    /// <summary>
    /// Function pointer for processing types deserialized from a <see cref="World"/>
    /// binary.
    /// </summary>
    public unsafe readonly struct ProcessSchema : IEquatable<ProcessSchema>
    {
#if NET
        private readonly delegate* unmanaged<Input, Types.Type> function;

        /// <inheritdoc/>
        public ProcessSchema(delegate* unmanaged<Input, Types.Type> function)
        {
            this.function = function;
        }
#else
        private readonly delegate*<Input, Types.Type> function;
        
        /// <inheritdoc/>
        public ProcessSchema(delegate*<Input, Types.Type> function)
        {
            this.function = function;
        }
#endif

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj)
        {
            return obj is ProcessSchema schema && Equals(schema);
        }

        /// <inheritdoc/>
        public readonly bool Equals(ProcessSchema other)
        {
            return (nint)function == (nint)other.function;
        }

        /// <inheritdoc/>
        public readonly override int GetHashCode()
        {
            return ((nint)function).GetHashCode();
        }

        /// <inheritdoc/>
        public readonly void Invoke(ref Types.Type type, DataType.Kind dataType)
        {
            Types.Type newType = function(new Input(type, dataType));
            type = newType;
        }

        /// <inheritdoc/>
        public readonly Types.Type Invoke(Types.Type type, DataType.Kind dataType)
        {
            return function(new Input(type, dataType));
        }

        /// <inheritdoc/>
        public static bool operator ==(ProcessSchema left, ProcessSchema right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(ProcessSchema left, ProcessSchema right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Input variable for the <see cref="ProcessSchema"/> delegate.
        /// </summary>
        public readonly struct Input
        {
            /// <summary>
            /// The type being deserialized.
            /// </summary>
            public readonly Types.Type type;

            /// <summary>
            /// The kind of data type that <see cref="type"/> is describing.
            /// </summary>
            public readonly DataType.Kind dataType;

            /// <inheritdoc/>
            public Input(Types.Type type, DataType.Kind dataType)
            {
                this.type = type;
                this.dataType = dataType;
            }
        }
    }
}