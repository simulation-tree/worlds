using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unmanaged;

namespace Worlds
{
    public unsafe struct TypeLayout : IEquatable<TypeLayout>, ISerializable
    {
        private static readonly Dictionary<int, TypeLayout> nameToType = new();
        private static readonly List<Type> systemTypes = new();
        private static readonly List<TypeLayout> all = new();

        /// <summary>
        /// Maximum amount of variables per type.
        /// </summary>
        public const uint Capacity = 32;

        private FixedString fullName;
        private ushort size;
        private byte count;

        private fixed byte data[(int)(Capacity * 260)];

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

        public readonly FixedString FullName => fullName;
        public readonly ushort Size => size;

        public readonly FixedString Name
        {
            get
            {
                if (fullName.TryLastIndexOf('.', out uint index))
                {
                    return fullName.Slice(index + 1);
                }
                else
                {
                    return fullName;
                }
            }
        }

#if NET
        [Obsolete("Default constructor not supported", true)]
        public TypeLayout()
        {
            throw new NotSupportedException();
        }
#endif

        public TypeLayout(FixedString fullName, ushort size, USpan<Variable> variables)
        {
            ThrowIfGreaterThanCapacity(variables.Length);

            this.fullName = fullName;
            this.size = size;
            count = (byte)variables.Length;
            variables.CopyTo(Variables);
        }

        public readonly override string ToString()
        {
            USpan<char> buffer = stackalloc char[256];
            uint length = ToString(buffer);
            return buffer.Slice(0, length).ToString();
        }

        public readonly uint ToString(USpan<char> destination)
        {
            uint length = fullName.CopyTo(destination);
            destination[length++] = ' ';
            destination[length++] = '(';
            destination[length++] = 's';
            destination[length++] = 'i';
            destination[length++] = 'z';
            destination[length++] = 'e';
            destination[length++] = '=';
            length += size.ToString(destination.Slice(length));
            destination[length++] = ')';
            return length;
        }

        public static bool IsRegistered<T>()
        {
            return systemTypes.Contains(typeof(T));
        }

        public unsafe static void Register<T>(FixedString fullName, USpan<Variable> variables) where T : unmanaged
        {
            ThrowIfAlreadyRegistered<T>();

            ushort size = (ushort)sizeof(T);
            TypeLayout layout = new(fullName, size, variables);
            systemTypes.Add(typeof(T));
            all.Add(layout);
            nameToType.Add(fullName.GetHashCode(), layout);
        }

        public static TypeLayout Get<T>()
        {
            ThrowIfTypeIsNotRegistered<T>();

            int index = systemTypes.IndexOf(typeof(T));
            return all[index];
        }

        public static TypeLayout Get(FixedString fullName)
        {
            ThrowIfTypeIsNotRegistered(fullName);

            return Get(fullName.GetHashCode());
        }

        public static TypeLayout Get(USpan<char> fullName)
        {
            return Get(new FixedString(fullName));
        }

        public static TypeLayout Get(string fullName)
        {
            return Get(new FixedString(fullName));
        }

        public static TypeLayout Get(int fullNameHash)
        {
            return nameToType[fullNameHash];
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
        public static void ThrowIfTypeIsNotRegistered<T>()
        {
            if (!systemTypes.Contains(typeof(T)))
            {
                throw new InvalidOperationException($"TypeLayout for {typeof(T)} is not registered");
            }
        }

        [Conditional("DEBUG")]
        public static void ThrowIfAlreadyRegistered<T>()
        {
            if (systemTypes.Contains(typeof(T)))
            {
                throw new InvalidOperationException($"TypeLayout for {typeof(T)} is already registered");
            }
        }

        [Conditional("DEBUG")]
        public static void ThrowIfTypeIsNotRegistered(FixedString fullName)
        {
            if (!nameToType.ContainsKey(fullName.GetHashCode()))
            {
                throw new InvalidOperationException($"TypeLayout for {fullName} is not registered");
            }
        }

        public readonly override bool Equals(object? obj)
        {
            return obj is TypeLayout layout && Equals(layout);
        }

        public readonly bool Equals(TypeLayout other)
        {
            return fullName.Equals(other.fullName) && count == other.count && Variables.SequenceEqual(other.Variables);
        }

        public readonly override int GetHashCode()
        {
            return HashCode.Combine(fullName, count, Variables.GetHashCode());
        }

        readonly void ISerializable.Write(BinaryWriter writer)
        {
            //full name
            writer.WriteValue(fullName.Length);
            for (byte i = 0; i < fullName.Length; i++)
            {
                writer.WriteValue((byte)fullName[i]);
            }

            writer.WriteValue(size);

            //variables
            writer.WriteValue(count);
            USpan<Variable> variables = Variables;
            foreach (Variable variable in variables)
            {
                writer.WriteObject(variable);
            }
        }

        void ISerializable.Read(BinaryReader reader)
        {
            //full name
            byte fullNameLength = reader.ReadValue<byte>();
            fullName = default;
            fullName.Length = fullNameLength;
            for (byte i = 0; i < fullNameLength; i++)
            {
                fullName[i] = (char)reader.ReadValue<byte>();
            }

            size = reader.ReadValue<ushort>();

            //variables
            count = reader.ReadValue<byte>();
            USpan<Variable> variables = stackalloc Variable[count];
            for (byte i = 0; i < count; i++)
            {
                variables[i] = reader.ReadObject<Variable>();
            }

            fixed (byte* ptr = data)
            {
                variables.CopyTo(new(ptr, count));
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

        public struct Variable : IEquatable<Variable>, ISerializable
        {
            private FixedString name;
            private int typeFullNameHash;

            public readonly FixedString Name => name;
            public readonly TypeLayout TypeLayout => Get(typeFullNameHash);
            public readonly ushort Size => TypeLayout.Size;

            public Variable(FixedString name, FixedString typeFullName)
            {
                this.name = name;
                typeFullNameHash = typeFullName.GetHashCode();
            }

            public Variable(FixedString name, int typeFullNameHash)
            {
                this.name = name;
                this.typeFullNameHash = typeFullNameHash;
            }

            public readonly override string ToString()
            {
                USpan<char> buffer = stackalloc char[256];
                uint length = ToString(buffer);
                return buffer.Slice(0, length).ToString();
            }

            public readonly uint ToString(USpan<char> buffer)
            {
                TypeLayout typeLayout = TypeLayout;
                uint length = name.CopyTo(buffer);
                buffer[length++] = ' ';
                buffer[length++] = '(';
                length += typeLayout.Name.CopyTo(buffer.Slice(length));
                buffer[length++] = ')';
                return length;
            }

            public readonly override bool Equals(object? obj)
            {
                return obj is Variable variable && Equals(variable);
            }

            public readonly bool Equals(Variable other)
            {
                return Name.Equals(other.Name) && typeFullNameHash == other.typeFullNameHash;
            }

            public readonly override int GetHashCode()
            {
                return HashCode.Combine(Name, typeFullNameHash);
            }

            void ISerializable.Write(BinaryWriter writer)
            {
                writer.WriteValue(name.Length);
                for (byte i = 0; i < name.Length; i++)
                {
                    writer.WriteValue((byte)name[i]);
                }

                writer.WriteValue(typeFullNameHash);
            }

            void ISerializable.Read(BinaryReader reader)
            {
                byte nameLength = reader.ReadValue<byte>();
                name = default;
                name.Length = nameLength;
                for (byte i = 0; i < nameLength; i++)
                {
                    name[i] = (char)reader.ReadValue<byte>();
                }

                typeFullNameHash = reader.ReadValue<int>();
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
    }
}
