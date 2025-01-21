#if !NET
using System;
using System.Collections.Generic;

namespace System
{
    public readonly struct RuntimeTypeHandle : IEquatable<RuntimeTypeHandle>
    {
        private static readonly Dictionary<nint, System.RuntimeTypeHandle> typeCache = new();

        internal readonly nint value;

        internal RuntimeTypeHandle(nint value)
        {
            this.value = value;
        }

        public readonly override bool Equals(object obj)
        {
            return obj is RuntimeTypeHandle typeHandle && Equals(typeHandle);
        }

        public readonly bool Equals(RuntimeTypeHandle other)
        {
            return value == other.value;
        }

        public readonly override int GetHashCode()
        {
            return HashCode.Combine(value);
        }

        public static RuntimeTypeHandle FromIntPtr(nint systemType)
        {
            return new(systemType);
        }

        public static nint ToIntPtr(RuntimeTypeHandle typeHandle)
        {
            return typeHandle.value;
        }

        public static bool operator ==(RuntimeTypeHandle left, RuntimeTypeHandle right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(RuntimeTypeHandle left, RuntimeTypeHandle right)
        {
            return !(left == right);
        }

        public static implicit operator System.RuntimeTypeHandle(RuntimeTypeHandle value)
        {
            return typeCache[value.value];
        }

        public static implicit operator RuntimeTypeHandle(System.RuntimeTypeHandle value)
        {
            typeCache[value.Value] = value;
            return new(value.Value);
        }
    }
}
#endif