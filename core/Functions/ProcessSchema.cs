using System;
using Types;

namespace Worlds.Functions
{
    public unsafe readonly struct ProcessSchema : IEquatable<ProcessSchema>
    {
        private readonly delegate* unmanaged<Input, void> function;

        public ProcessSchema(delegate* unmanaged<Input, void> function)
        {
            this.function = function;
        }

        public readonly override bool Equals(object? obj)
        {
            return obj is ProcessSchema schema && Equals(schema);
        }

        public readonly bool Equals(ProcessSchema other)
        {
            return (nint)function == (nint)other.function;
        }

        public readonly override int GetHashCode()
        {
            return ((nint)function).GetHashCode();
        }

        public readonly void Invoke(Schema schema, TypeLayout typeLayout, DataType.Kind type)
        {
            function(new Input(schema, typeLayout, type));
        }

        public static bool operator ==(ProcessSchema left, ProcessSchema right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ProcessSchema left, ProcessSchema right)
        {
            return !(left == right);
        }

        public readonly struct Input
        {
            public readonly Schema schema;
            public readonly TypeLayout typeLayout;
            public readonly DataType.Kind type;

            public Input(Schema schema, TypeLayout typeLayout, DataType.Kind type)
            {
                this.schema = schema;
                this.typeLayout = typeLayout;
                this.type = type;
            }
        }
    }
}