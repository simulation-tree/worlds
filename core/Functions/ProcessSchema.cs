﻿using System;
using Types;

namespace Worlds.Functions
{
    public unsafe readonly struct ProcessSchema : IEquatable<ProcessSchema>
    {
#if NET
        private readonly delegate* unmanaged<Input, TypeLayout> function;

        public ProcessSchema(delegate* unmanaged<Input, TypeLayout> function)
        {
            this.function = function;
        }
#else
        private readonly delegate*<Input, TypeLayout> function;
        
        public ProcessSchema(delegate*<Input, TypeLayout> function)
        {
            this.function = function;
        }
#endif

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

        public readonly void Invoke(ref TypeLayout type, DataType.Kind dataType)
        {
            TypeLayout newType = function(new Input(type, dataType));
            type = newType;
        }

        public readonly TypeLayout Invoke(TypeLayout type, DataType.Kind dataType)
        {
            return function(new Input(type, dataType));
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
            public readonly TypeLayout type;
            public readonly DataType.Kind dataType;

            public Input(TypeLayout type, DataType.Kind dataType)
            {
                this.type = type;
                this.dataType = dataType;
            }
        }
    }
}