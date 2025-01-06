using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unmanaged;

namespace Worlds
{
    [DebuggerTypeProxy(typeof(TypeLayoutDebugView))]
    public unsafe struct TypeLayout : IEquatable<TypeLayout>, ISerializable
    {
        private static readonly Dictionary<long, TypeLayout> nameToType = new();
        private static readonly List<Type> systemTypes = new();
        private static readonly List<TypeLayout> all = new();

        /// <summary>
        /// Maximum amount of variables per type.
        /// </summary>
        public const uint Capacity = 16;

        private FixedString fullName;
        private ushort size;
        private byte count;

        private fixed byte data[(int)(Capacity * 264)];

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

        /// <summary>
        /// The underlying system type that this layout represents.
        /// </summary>
        public readonly Type SystemType
        {
            get
            {
                for (int i = 0; i < systemTypes.Count; i++)
                {
                    if (all[i] == this)
                    {
                        return systemTypes[i];
                    }
                }

                throw new InvalidOperationException($"System type not found for {this}");
            }
        }

        /// <summary>
        /// Full name of the type including the namespace.
        /// </summary>
        public readonly FixedString FullName => fullName;

        /// <summary>
        /// Size of the type in bytes.
        /// </summary>
        public readonly ushort Size => size;

        /// <summary>
        /// Name of the type.
        /// </summary>
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

        /// <inheritdoc/>
        public readonly override string ToString()
        {
            USpan<char> buffer = stackalloc char[256];
            uint length = ToString(buffer);
            return buffer.Slice(0, length).ToString();
        }

        /// <summary>
        /// Writes a string representation of this type layout to <paramref name="destination"/>.
        /// </summary>
        public readonly uint ToString(USpan<char> destination)
        {
            return fullName.CopyTo(destination);
        }

        /// <summary>
        /// Checks if this type layout matches <typeparamref name="T"/>.
        /// </summary>
        public readonly bool Is<T>() where T : unmanaged
        {
            int index = systemTypes.IndexOf(typeof(T));
            if (index >= 0)
            {
                return all[index] == this;
            }

            return false;
        }

        /// <summary>
        /// Checks if <typeparamref name="T"/> is registered.
        /// </summary>
        public static bool IsRegistered<T>() where T : unmanaged
        {
            return systemTypes.Contains(typeof(T));
        }

        public static bool IsRegistered(FixedString fullTypeName)
        {
            return nameToType.ContainsKey(fullTypeName.GetLongHashCode());
        }

        public static FixedString GetFullName<T>()
        {
            FixedString fullName = default;
            AppendType(ref fullName, typeof(T));
            return fullName;

            static void AppendType(ref FixedString text, Type type)
            {
                Type? current = type;
                string? currentNameSpace = current.Namespace;
                while (current is not null)
                {
                    Type[] genericTypes = current.GenericTypeArguments;
                    string name = current.Name;
                    if (genericTypes.Length > 0)
                    {
                        text.Insert(0, '>');
                        for (int i = genericTypes.Length - 1; i >= 0; i--)
                        {
                            AppendType(ref text, genericTypes[i]);
                            if (i > 0)
                            {
                                text.Insert(0, ", ");
                            }
                        }

                        text.Insert(0, '<');
                        text.Insert(0, name[..name.IndexOf('`')]);
                    }
                    else
                    {
                        text.Insert(0, name);
                    }

                    current = current.DeclaringType;
                    if (current is not null)
                    {
                        text.Insert(0, '.');
                    }
                }

                if (currentNameSpace is not null)
                {
                    text.Insert(0, '.');
                    text.Insert(0, currentNameSpace);
                }
            }
        }

        public unsafe static void Register<T>() where T : unmanaged
        {
            ThrowIfAlreadyRegistered<T>();

            ushort size = (ushort)sizeof(T);
            FixedString fullName = GetFullName<T>();
            TypeLayout layout = new(fullName, size, []);
            systemTypes.Add(typeof(T));
            all.Add(layout);
            nameToType.Add(fullName.GetLongHashCode(), layout);
            Cache<T>.value = layout;
        }

        public unsafe static void Register<T>(Variable var1) where T : unmanaged
        {
            ThrowIfAlreadyRegistered<T>();

            ushort size = (ushort)sizeof(T);
            FixedString fullName = GetFullName<T>();
            TypeLayout layout = new(fullName, size, [var1]);
            systemTypes.Add(typeof(T));
            all.Add(layout);
            nameToType.Add(fullName.GetLongHashCode(), layout);
            Cache<T>.value = layout;
        }

        public unsafe static void Register<T>(Variable var1, Variable var2) where T : unmanaged
        {
            ThrowIfAlreadyRegistered<T>();

            ushort size = (ushort)sizeof(T);
            FixedString fullName = GetFullName<T>();
            TypeLayout layout = new(fullName, size, [var1, var2]);
            systemTypes.Add(typeof(T));
            all.Add(layout);
            nameToType.Add(fullName.GetLongHashCode(), layout);
            Cache<T>.value = layout;
        }

        public unsafe static void Register<T>(Variable var1, Variable var2, Variable var3) where T : unmanaged
        {
            ThrowIfAlreadyRegistered<T>();

            ushort size = (ushort)sizeof(T);
            FixedString fullName = GetFullName<T>();
            TypeLayout layout = new(fullName, size, [var1, var2, var3]);
            systemTypes.Add(typeof(T));
            all.Add(layout);
            nameToType.Add(fullName.GetLongHashCode(), layout);
            Cache<T>.value = layout;
        }

        public unsafe static void Register<T>(Variable var1, Variable var2, Variable var3, Variable var4) where T : unmanaged
        {
            ThrowIfAlreadyRegistered<T>();

            ushort size = (ushort)sizeof(T);
            FixedString fullName = GetFullName<T>();
            TypeLayout layout = new(fullName, size, [var1, var2, var3, var4]);
            systemTypes.Add(typeof(T));
            all.Add(layout);
            nameToType.Add(fullName.GetLongHashCode(), layout);
            Cache<T>.value = layout;
        }

        public unsafe static void Register<T>(Variable var1, Variable var2, Variable var3, Variable var4, Variable var5) where T : unmanaged
        {
            ThrowIfAlreadyRegistered<T>();

            ushort size = (ushort)sizeof(T);
            FixedString fullName = GetFullName<T>();
            TypeLayout layout = new(fullName, size, [var1, var2, var3, var4, var5]);
            systemTypes.Add(typeof(T));
            all.Add(layout);
            nameToType.Add(fullName.GetLongHashCode(), layout);
            Cache<T>.value = layout;
        }

        public unsafe static void Register<T>(Variable var1, Variable var2, Variable var3, Variable var4, Variable var5, Variable var6) where T : unmanaged
        {
            ThrowIfAlreadyRegistered<T>();

            ushort size = (ushort)sizeof(T);
            FixedString fullName = GetFullName<T>();
            TypeLayout layout = new(fullName, size, [var1, var2, var3, var4, var5, var6]);
            systemTypes.Add(typeof(T));
            all.Add(layout);
            nameToType.Add(fullName.GetLongHashCode(), layout);
            Cache<T>.value = layout;
        }

        public unsafe static void Register<T>(Variable var1, Variable var2, Variable var3, Variable var4, Variable var5, Variable var6, Variable var7) where T : unmanaged
        {
            ThrowIfAlreadyRegistered<T>();

            ushort size = (ushort)sizeof(T);
            FixedString fullName = GetFullName<T>();
            TypeLayout layout = new(fullName, size, [var1, var2, var3, var4, var5, var6, var7]);
            systemTypes.Add(typeof(T));
            all.Add(layout);
            nameToType.Add(fullName.GetLongHashCode(), layout);
            Cache<T>.value = layout;
        }

        public unsafe static void Register<T>(Variable var1, Variable var2, Variable var3, Variable var4, Variable var5, Variable var6, Variable var7, Variable var8) where T : unmanaged
        {
            ThrowIfAlreadyRegistered<T>();

            ushort size = (ushort)sizeof(T);
            FixedString fullName = GetFullName<T>();
            TypeLayout layout = new(fullName, size, [var1, var2, var3, var4, var5, var6, var7, var8]);
            systemTypes.Add(typeof(T));
            all.Add(layout);
            nameToType.Add(fullName.GetLongHashCode(), layout);
            Cache<T>.value = layout;
        }

        public unsafe static void Register<T>(Variable var1, Variable var2, Variable var3, Variable var4, Variable var5, Variable var6, Variable var7, Variable var8, Variable var9) where T : unmanaged
        {
            ThrowIfAlreadyRegistered<T>();

            ushort size = (ushort)sizeof(T);
            FixedString fullName = GetFullName<T>();
            TypeLayout layout = new(fullName, size, [var1, var2, var3, var4, var5, var6, var7, var8, var9]);
            systemTypes.Add(typeof(T));
            all.Add(layout);
            nameToType.Add(fullName.GetLongHashCode(), layout);
            Cache<T>.value = layout;
        }

        public unsafe static void Register<T>(Variable var1, Variable var2, Variable var3, Variable var4, Variable var5, Variable var6, Variable var7, Variable var8, Variable var9, Variable var10) where T : unmanaged
        {
            ThrowIfAlreadyRegistered<T>();

            ushort size = (ushort)sizeof(T);
            FixedString fullName = GetFullName<T>();
            TypeLayout layout = new(fullName, size, [var1, var2, var3, var4, var5, var6, var7, var8, var9, var10]);
            systemTypes.Add(typeof(T));
            all.Add(layout);
            nameToType.Add(fullName.GetLongHashCode(), layout);
            Cache<T>.value = layout;
        }

        public unsafe static void Register<T>(Variable var1, Variable var2, Variable var3, Variable var4, Variable var5, Variable var6, Variable var7, Variable var8, Variable var9, Variable var10, Variable var11) where T : unmanaged
        {
            ThrowIfAlreadyRegistered<T>();

            ushort size = (ushort)sizeof(T);
            FixedString fullName = GetFullName<T>();
            TypeLayout layout = new(fullName, size, [var1, var2, var3, var4, var5, var6, var7, var8, var9, var10, var11]);
            systemTypes.Add(typeof(T));
            all.Add(layout);
            nameToType.Add(fullName.GetLongHashCode(), layout);
            Cache<T>.value = layout;
        }

        public unsafe static void Register<T>(Variable var1, Variable var2, Variable var3, Variable var4, Variable var5, Variable var6, Variable var7, Variable var8, Variable var9, Variable var10, Variable var11, Variable var12) where T : unmanaged
        {
            ThrowIfAlreadyRegistered<T>();

            ushort size = (ushort)sizeof(T);
            FixedString fullName = GetFullName<T>();
            TypeLayout layout = new(fullName, size, [var1, var2, var3, var4, var5, var6, var7, var8, var9, var10, var11, var12]);
            systemTypes.Add(typeof(T));
            all.Add(layout);
            nameToType.Add(fullName.GetLongHashCode(), layout);
            Cache<T>.value = layout;
        }

        public unsafe static void Register<T>(Variable var1, Variable var2, Variable var3, Variable var4, Variable var5, Variable var6, Variable var7, Variable var8, Variable var9, Variable var10, Variable var11, Variable var12, Variable var13) where T : unmanaged
        {
            ThrowIfAlreadyRegistered<T>();

            ushort size = (ushort)sizeof(T);
            FixedString fullName = GetFullName<T>();
            TypeLayout layout = new(fullName, size, [var1, var2, var3, var4, var5, var6, var7, var8, var9, var10, var11, var12, var13]);
            systemTypes.Add(typeof(T));
            all.Add(layout);
            nameToType.Add(fullName.GetLongHashCode(), layout);
            Cache<T>.value = layout;
        }

        public unsafe static void Register<T>(Variable var1, Variable var2, Variable var3, Variable var4, Variable var5, Variable var6, Variable var7, Variable var8, Variable var9, Variable var10, Variable var11, Variable var12, Variable var13, Variable var14) where T : unmanaged
        {
            ThrowIfAlreadyRegistered<T>();

            ushort size = (ushort)sizeof(T);
            FixedString fullName = GetFullName<T>();
            TypeLayout layout = new(fullName, size, [var1, var2, var3, var4, var5, var6, var7, var8, var9, var10, var11, var12, var13, var14]);
            systemTypes.Add(typeof(T));
            all.Add(layout);
            nameToType.Add(fullName.GetLongHashCode(), layout);
            Cache<T>.value = layout;
        }

        public unsafe static void Register<T>(Variable var1, Variable var2, Variable var3, Variable var4, Variable var5, Variable var6, Variable var7, Variable var8, Variable var9, Variable var10, Variable var11, Variable var12, Variable var13, Variable var14, Variable var15) where T : unmanaged
        {
            ThrowIfAlreadyRegistered<T>();

            ushort size = (ushort)sizeof(T);
            FixedString fullName = GetFullName<T>();
            TypeLayout layout = new(fullName, size, [var1, var2, var3, var4, var5, var6, var7, var8, var9, var10, var11, var12, var13, var14, var15]);
            systemTypes.Add(typeof(T));
            all.Add(layout);
            nameToType.Add(fullName.GetLongHashCode(), layout);
            Cache<T>.value = layout;
        }

        public unsafe static void Register<T>(Variable var1, Variable var2, Variable var3, Variable var4, Variable var5, Variable var6, Variable var7, Variable var8, Variable var9, Variable var10, Variable var11, Variable var12, Variable var13, Variable var14, Variable var15, Variable var16) where T : unmanaged
        {
            ThrowIfAlreadyRegistered<T>();

            ushort size = (ushort)sizeof(T);
            FixedString fullName = GetFullName<T>();
            TypeLayout layout = new(fullName, size, [var1, var2, var3, var4, var5, var6, var7, var8, var9, var10, var11, var12, var13, var14, var15, var16]);
            systemTypes.Add(typeof(T));
            all.Add(layout);
            nameToType.Add(fullName.GetLongHashCode(), layout);
            Cache<T>.value = layout;
        }

        public static TypeLayout Get<T>() where T : unmanaged
        {
            ThrowIfTypeIsNotRegistered<T>();

            return Cache<T>.value;
        }

        public static TypeLayout Get(FixedString fullName)
        {
            ThrowIfTypeIsNotRegistered(fullName);

            return Get(fullName.GetLongHashCode());
        }

        public static TypeLayout Get(USpan<char> fullName)
        {
            return Get(new FixedString(fullName));
        }

        public static TypeLayout Get(string fullName)
        {
            return Get(new FixedString(fullName));
        }

        public static TypeLayout Get(long fullNameHash)
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
            if (!nameToType.ContainsKey(fullName.GetLongHashCode()))
            {
                throw new InvalidOperationException($"TypeLayout for {fullName} is not registered");
            }
        }

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj)
        {
            return obj is TypeLayout layout && Equals(layout);
        }

        /// <inheritdoc/>
        public readonly bool Equals(TypeLayout other)
        {
            return fullName.Equals(other.fullName) && count == other.count && Variables.SequenceEqual(other.Variables);
        }

        /// <inheritdoc/>
        public readonly override int GetHashCode()
        {
            return fullName.GetHashCode();
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

        [DebuggerTypeProxy(typeof(VariableDebugView))]
        public struct Variable : IEquatable<Variable>, ISerializable
        {
            private FixedString name;
            private long typeFullNameHash;

            public readonly FixedString Name => name;
            public readonly TypeLayout TypeLayout => Get(typeFullNameHash);
            public readonly ushort Size => TypeLayout.Size;

            public Variable(FixedString name, FixedString typeFullName)
            {
                this.name = name;
                typeFullNameHash = typeFullName.GetLongHashCode();
            }

            public Variable(string name, string typeFullName)
            {
                this.name = name;
                typeFullNameHash = new FixedString(typeFullName).GetLongHashCode();
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
                unchecked
                {
                    int hashCode = 17;
                    for (byte i = 0; i < name.Length; i++)
                    {
                        hashCode = hashCode * 31 + name[i];
                    }

                    hashCode = hashCode * 31 + (int)typeFullNameHash;
                    return hashCode;
                }
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

                typeFullNameHash = reader.ReadValue<long>();
            }

            public static bool operator ==(Variable left, Variable right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(Variable left, Variable right)
            {
                return !(left == right);
            }

            internal class VariableDebugView
            {
                public readonly string name;
                public readonly string typeFullName;
                public readonly string typeName;
                public readonly ushort typeSize;

                public VariableDebugView(Variable variable)
                {
                    name = variable.Name.ToString();
                    if (nameToType.TryGetValue(variable.typeFullNameHash, out TypeLayout layout))
                    {
                        typeFullName = layout.FullName.ToString();
                        typeName = layout.Name.ToString();
                        typeSize = layout.Size;
                    }
                    else
                    {
                        typeFullName = variable.typeFullNameHash.ToString();
                        typeName = "Unknown";
                    }
                }
            }
        }

        internal class TypeLayoutDebugView
        {
            public readonly string fullName;
            public readonly string name;
            public readonly ushort size;

            public TypeLayoutDebugView(TypeLayout layout)
            {
                fullName = layout.FullName.ToString();
                name = layout.Name.ToString();
                size = layout.Size;
            }
        }

        internal static class Cache<T> where T : unmanaged
        {
            internal static TypeLayout value;
        }
    }
}