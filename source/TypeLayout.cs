using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unmanaged;

namespace Worlds
{
    public unsafe struct TypeLayout : IEquatable<TypeLayout>
    {
        private static readonly Dictionary<Type, TypeLayout> systemTypeToType = new();
        private static readonly List<Type> systemTypes = new();
        private static readonly List<TypeLayout> all = new();

        public const uint Capacity = 128;

        public readonly FixedString name;
        public readonly byte count;

        private fixed byte data[(int)(Capacity * Variable.Size)];

        public readonly USpan<Variable> Variables
        {
            get
            {
                fixed (byte* ptr = data)
                {
                    return new(ptr, count);
                }
            }
        }

        public readonly uint Size
        {
            get
            {
                uint size = 0;
                USpan<Variable> variables = Variables;
                foreach (Variable variable in variables)
                {
                    size += variable.size;
                }

                return size;
            }
        }

#if NET
        [Obsolete("Default constructor not supported", true)]
        public TypeLayout()
        {
            throw new NotSupportedException();
        }
#endif

        private TypeLayout(FixedString name, USpan<Variable> variables)
        {
            ThrowIfGreaterThanCapacity(variables.Length);

            this.name = name;
            count = (byte)variables.Length;
            variables.CopyTo(Variables);
        }

        public readonly override string ToString()
        {
            USpan<char> buffer = stackalloc char[256];
            uint length = ToString(buffer);
            return buffer.Slice(0, length).ToString();
        }

        public readonly uint ToString(USpan<char> buffer)
        {
            uint length = 0;
            length += name.CopyTo(buffer);
            buffer[length++] = ' ';
            buffer[length++] = '(';
            buffer[length++] = 's';
            buffer[length++] = 'i';
            buffer[length++] = 'z';
            buffer[length++] = 'e';
            buffer[length++] = '=';
            length += Size.ToString(buffer.Slice(length));
            buffer[length++] = ')';
            return length;
        }

        public readonly bool Contains(Type type)
        {
            RuntimeTypeHandle typeHandle = type.TypeHandle;
            nint typeValue = RuntimeTypeHandle.ToIntPtr(typeHandle);
            USpan<Variable> variables = Variables;
            foreach (Variable variable in variables)
            {
                if (variable.type == typeValue)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsRegistered<T>()
        {
            return systemTypeToType.ContainsKey(typeof(T));
        }

        public static bool IsRegistered(Type type)
        {
            return systemTypeToType.ContainsKey(type);
        }

        public static void Register<T>(FixedString name, USpan<Variable> variables)
        {
            ThrowIfAlreadyRegistered<T>();

            TypeLayout layout = new(name, variables);
            Type systemType = typeof(T);
            systemTypeToType.Add(systemType, layout);
            systemTypes.Add(systemType);
            all.Add(layout);
        }

        public static TypeLayout Get<T>()
        {
            ThrowIfTypeIsNotRegistered(typeof(T));

            return TypeCache<T>.layout;
        }

        public static TypeLayout Get(Type type)
        {
            ThrowIfTypeIsNotRegistered(type);

            return systemTypeToType[type];
        }

        [Conditional("DEBUG")]
        public static void ThrowIfGreaterThanCapacity(uint length)
        {
            if (length >= Capacity)
            {
                throw new InvalidOperationException($"TypeLayout has reached its capacity of {Capacity} variables");
            }
        }

        [Conditional("DEBUG")]
        public static void ThrowIfTypeIsNotRegistered(Type systemType)
        {
            if (!systemTypeToType.ContainsKey(systemType))
            {
                throw new InvalidOperationException($"TypeLayout for {systemType} is not registered");
            }
        }

        [Conditional("DEBUG")]
        public static void ThrowIfAlreadyRegistered<T>()
        {
            Type systemType = typeof(T);
            if (systemTypeToType.ContainsKey(systemType))
            {
                throw new InvalidOperationException($"TypeLayout for {systemType} is already registered");
            }
        }

        internal static class TypeCache<T>
        {
            internal static readonly TypeLayout layout = systemTypeToType[typeof(T)];
        }

        public readonly override bool Equals(object? obj)
        {
            return obj is TypeLayout layout && Equals(layout);
        }

        public readonly bool Equals(TypeLayout other)
        {
            return name.Equals(other.name) && count == other.count && Variables.SequenceEqual(other.Variables);
        }

        public readonly override int GetHashCode()
        {
            return HashCode.Combine(name, count, Variables.GetHashCode());
        }

        public readonly struct Variable : IEquatable<Variable>
        {
            public const uint Size = 256 + 4 + 8;

            public readonly FixedString name;
            public readonly ushort size;
            public readonly nint type;

            public readonly RuntimeTypeHandle TypeHandle => RuntimeTypeHandle.FromIntPtr(type);
            public readonly Type Type => Type.GetTypeFromHandle(TypeHandle) ?? throw new();
            public readonly bool IsRegistered => IsRegistered(Type);
            public readonly TypeLayout Layout => Get(Type);

            public Variable(FixedString name, ushort size, Type type)
            {
                this.type = type.TypeHandle.Value;
                this.name = name;
                this.size = size;
            }

            public readonly override string ToString()
            {
                USpan<char> buffer = stackalloc char[256];
                uint length = ToString(buffer);
                return buffer.Slice(0, length).ToString();
            }

            public readonly uint ToString(USpan<char> buffer)
            {
                uint length = 0;
                length += name.CopyTo(buffer);
                buffer[length++] = ' ';
                buffer[length++] = '(';
                buffer[length++] = 's';
                buffer[length++] = 'i';
                buffer[length++] = 'z';
                buffer[length++] = 'e';
                buffer[length++] = '=';
                length += size.ToString(buffer.Slice(length));
                buffer[length++] = ')';
                return length;
            }

            public readonly override bool Equals(object? obj)
            {
                return obj is Variable variable && Equals(variable);
            }

            public readonly bool Equals(Variable other)
            {
                return type.Equals(other.type);
            }

            public readonly override int GetHashCode()
            {
                return type.GetHashCode();
            }

            public readonly bool TryGetLayout(out TypeLayout layout)
            {
                Type type = Type;
                if (IsRegistered(type))
                {
                    layout = Get(type);
                    return true;
                }

                layout = default;
                return false;
            }

            public static bool operator ==(Variable left, Variable right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(Variable left, Variable right)
            {
                return !(left == right);
            }
        }

        public static bool operator ==(TypeLayout left, TypeLayout right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TypeLayout left, TypeLayout right)
        {
            return !(left == right);
        }
    }
}
