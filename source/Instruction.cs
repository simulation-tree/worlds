using System;
using Unmanaged;

namespace Simulation
{
    /// <summary>
    /// Contains an instruction for working with <see cref="World"/>s.
    /// </summary>
    public struct Instruction : IDisposable, ISerializable, IEquatable<Instruction>
    {
        public readonly Type type;
        private readonly ulong a;
        private readonly ulong b;
        private readonly ulong c;

        public readonly ulong A => a;
        public readonly ulong B => b;
        public readonly ulong C => c;

        private Instruction(Type operation, ulong a, ulong b, ulong c)
        {
            this.type = operation;
            this.a = a;
            this.b = b;
            this.c = c;
        }

        public unsafe readonly void Dispose()
        {
            if (type == Type.AddComponent || type == Type.SetComponent || type == Type.CreateArray || type == Type.SetArrayElement)
            {
                Allocation allocation = new((void*)(nint)b);
                allocation.Dispose();
            }
        }

        public readonly override string ToString()
        {
            Span<char> buffer = stackalloc char[256];
            int length = ToString(buffer);
            return buffer[..length].ToString();
        }

        public unsafe readonly int ToString(Span<char> buffer)
        {
            int length = 0;
            if (type == Type.CreateEntity)
            {
                buffer[length++] = 'C';
                buffer[length++] = 'r';
                buffer[length++] = 'e';
                buffer[length++] = 'a';
                buffer[length++] = 't';
                buffer[length++] = 'e';
                buffer[length++] = 'E';
                buffer[length++] = 'n';
                buffer[length++] = 't';
                buffer[length++] = 'i';
                buffer[length++] = 't';
                buffer[length++] = 'y';
                buffer[length++] = '(';
                a.TryFormat(buffer[length..], out int written);
                length += written;
                buffer[length++] = ')';
            }
            else if (type == Type.DestroyEntities)
            {
                buffer[length++] = 'D';
                buffer[length++] = 'e';
                buffer[length++] = 's';
                buffer[length++] = 't';
                buffer[length++] = 'r';
                buffer[length++] = 'o';
                buffer[length++] = 'y';
                buffer[length++] = 'E';
                buffer[length++] = 'n';
                buffer[length++] = 't';
                buffer[length++] = 'i';
                buffer[length++] = 't';
                buffer[length++] = 'i';
                buffer[length++] = 'e';
                buffer[length++] = 's';
                buffer[length++] = '(';
                uint start = (uint)a;
                start.TryFormat(buffer[length..], out int written);
                length += written;
                buffer[length++] = ',';
                uint count = (uint)b;
                count.TryFormat(buffer[length..], out written);
                length += written;
                buffer[length++] = ')';
            }
            else if (type == Type.ClearSelection)
            {
                buffer[length++] = 'C';
                buffer[length++] = 'l';
                buffer[length++] = 'e';
                buffer[length++] = 'a';
                buffer[length++] = 'r';
                buffer[length++] = 'S';
                buffer[length++] = 'e';
                buffer[length++] = 'l';
                buffer[length++] = 'e';
                buffer[length++] = 'c';
                buffer[length++] = 't';
                buffer[length++] = 'i';
                buffer[length++] = 'o';
                buffer[length++] = 'n';
                buffer[length++] = '(';
                buffer[length++] = ')';
            }
            else if (type == Type.SelectEntity)
            {
                buffer[length++] = 'S';
                buffer[length++] = 'e';
                buffer[length++] = 'l';
                buffer[length++] = 'e';
                buffer[length++] = 'c';
                buffer[length++] = 't';
                buffer[length++] = 'E';
                buffer[length++] = 'n';
                buffer[length++] = 't';
                buffer[length++] = 'i';
                buffer[length++] = 't';
                buffer[length++] = 'y';
                buffer[length++] = '(';
                a.TryFormat(buffer[length..], out int written);
                length += written;
                buffer[length++] = ')';
            }
            else if (type == Type.SetParent)
            {
                buffer[length++] = 'S';
                buffer[length++] = 'e';
                buffer[length++] = 't';
                buffer[length++] = 'P';
                buffer[length++] = 'a';
                buffer[length++] = 'r';
                buffer[length++] = 'e';
                buffer[length++] = 'n';
                buffer[length++] = 't';
                buffer[length++] = '(';
                bool isRelative = b == 0;
                if (isRelative)
                {
                    buffer[length++] = 'o';
                    buffer[length++] = 'f';
                    buffer[length++] = 'f';
                    buffer[length++] = 's';
                    buffer[length++] = 'e';
                    buffer[length++] = 't';
                    buffer[length++] = ':';
                }
                else
                {
                    buffer[length++] = 'e';
                    buffer[length++] = 'n';
                    buffer[length++] = 't';
                    buffer[length++] = 'i';
                    buffer[length++] = 't';
                    buffer[length++] = 'y';
                    buffer[length++] = ':';
                }

                a.TryFormat(buffer[length..], out int written);
                length += written;
                buffer[length++] = ')';
            }
            else if (type == Type.AddReference)
            {
                buffer[length++] = 'A';
                buffer[length++] = 'd';
                buffer[length++] = 'd';
                buffer[length++] = 'R';
                buffer[length++] = 'e';
                buffer[length++] = 'f';
                buffer[length++] = 'e';
                buffer[length++] = 'r';
                buffer[length++] = 'e';
                buffer[length++] = 'n';
                buffer[length++] = 'c';
                buffer[length++] = 'e';
                buffer[length++] = '(';
                bool isRelative = b == 0;
                if (isRelative)
                {
                    buffer[length++] = 'o';
                    buffer[length++] = 'f';
                    buffer[length++] = 'f';
                    buffer[length++] = 's';
                    buffer[length++] = 'e';
                    buffer[length++] = 't';
                    buffer[length++] = ':';
                }
                else
                {
                    buffer[length++] = 'e';
                    buffer[length++] = 'n';
                    buffer[length++] = 't';
                    buffer[length++] = 'i';
                    buffer[length++] = 't';
                    buffer[length++] = 'y';
                    buffer[length++] = ':';
                }

                a.TryFormat(buffer[length..], out int written);
                length += written;
            }
            else if (type == Type.RemoveReference)
            {
                buffer[length++] = 'R';
                buffer[length++] = 'e';
                buffer[length++] = 'm';
                buffer[length++] = 'o';
                buffer[length++] = 'v';
                buffer[length++] = 'e';
                buffer[length++] = 'R';
                buffer[length++] = 'e';
                buffer[length++] = 'f';
                buffer[length++] = 'e';
                buffer[length++] = 'r';
                buffer[length++] = 'e';
                buffer[length++] = 'n';
                buffer[length++] = 'c';
                buffer[length++] = 'e';
                buffer[length++] = '(';
                a.TryFormat(buffer[length..], out int written);
                length += written;
                buffer[length++] = ')';
            }
            else if (type == Type.AddComponent)
            {
                buffer[length++] = 'A';
                buffer[length++] = 'd';
                buffer[length++] = 'd';
                buffer[length++] = 'C';
                buffer[length++] = 'o';
                buffer[length++] = 'm';
                buffer[length++] = 'p';
                buffer[length++] = 'o';
                buffer[length++] = 'n';
                buffer[length++] = 'e';
                buffer[length++] = 'n';
                buffer[length++] = 't';
                buffer[length++] = '<';
                RuntimeType componentType = new((uint)a);
                int written = componentType.ToString(buffer[length..]);
                length += written;
                buffer[length++] = '>';
                buffer[length++] = '(';
                nint allocationAddress = (nint)b;
                allocationAddress.TryFormat(buffer[length..], out written);
                length += written;
                buffer[length++] = ')';
            }
            else if (type == Type.RemoveComponent)
            {
                buffer[length++] = 'R';
                buffer[length++] = 'e';
                buffer[length++] = 'm';
                buffer[length++] = 'o';
                buffer[length++] = 'v';
                buffer[length++] = 'e';
                buffer[length++] = 'C';
                buffer[length++] = 'o';
                buffer[length++] = 'm';
                buffer[length++] = 'p';
                buffer[length++] = 'o';
                buffer[length++] = 'n';
                buffer[length++] = 'e';
                buffer[length++] = 'n';
                buffer[length++] = 't';
                buffer[length++] = '<';
                RuntimeType componentType = new((uint)a);
                int written = componentType.ToString(buffer[length..]);
                length += written;
                buffer[length++] = '>';
                buffer[length++] = '(';
                buffer[length++] = ')';
            }
            else if (type == Type.SetComponent)
            {
                buffer[length++] = 'S';
                buffer[length++] = 'e';
                buffer[length++] = 't';
                buffer[length++] = 'C';
                buffer[length++] = 'o';
                buffer[length++] = 'm';
                buffer[length++] = 'p';
                buffer[length++] = 'o';
                buffer[length++] = 'n';
                buffer[length++] = 'e';
                buffer[length++] = 'n';
                buffer[length++] = 't';
                buffer[length++] = '<';
                RuntimeType componentType = new((uint)a);
                int written = componentType.ToString(buffer[length..]);
                length += written;
                buffer[length++] = '>';
                buffer[length++] = '(';
                nint allocationAddress = (nint)b;
                allocationAddress.TryFormat(buffer[length..], out written);
                length += written;
                buffer[length++] = ')';
            }
            else if (type == Type.CreateArray)
            {
                buffer[length++] = 'C';
                buffer[length++] = 'r';
                buffer[length++] = 'e';
                buffer[length++] = 'a';
                buffer[length++] = 't';
                buffer[length++] = 'e';
                buffer[length++] = 'A';
                buffer[length++] = 'r';
                buffer[length++] = 'r';
                buffer[length++] = 'a';
                buffer[length++] = 'y';
                buffer[length++] = '<';
                RuntimeType arrayType = new((uint)a);
                int written = arrayType.ToString(buffer[length..]);
                length += written;
                buffer[length++] = '>';
                buffer[length++] = '(';
                uint count = (uint)b;
                count.TryFormat(buffer[length..], out written);
                length += written;
                buffer[length++] = ')';
            }
            else if (type == Type.DestroyArray)
            {
                buffer[length++] = 'D';
                buffer[length++] = 'e';
                buffer[length++] = 's';
                buffer[length++] = 't';
                buffer[length++] = 'r';
                buffer[length++] = 'o';
                buffer[length++] = 'y';
                buffer[length++] = 'A';
                buffer[length++] = 'r';
                buffer[length++] = 'r';
                buffer[length++] = 'a';
                buffer[length++] = 'y';
                buffer[length++] = '<';
                RuntimeType arrayType = new((uint)a);
                int written = arrayType.ToString(buffer[length..]);
                length += written;
                buffer[length++] = '>';
                buffer[length++] = '(';
                buffer[length++] = ')';
            }
            else if (type == Type.ResizeArray)
            {
                buffer[length++] = 'R';
                buffer[length++] = 'e';
                buffer[length++] = 's';
                buffer[length++] = 'i';
                buffer[length++] = 'z';
                buffer[length++] = 'e';
                buffer[length++] = 'A';
                buffer[length++] = 'r';
                buffer[length++] = 'r';
                buffer[length++] = 'a';
                buffer[length++] = 'y';
                buffer[length++] = '<';
                RuntimeType arrayType = new((uint)a);
                int written = arrayType.ToString(buffer[length..]);
                length += written;
                buffer[length++] = '>';
                buffer[length++] = '(';
                buffer[length++] = ')';
            }
            else if (type == Type.SetArrayElement)
            {
                Allocation allocation = new((void*)(nint)b);
                uint count = allocation.Read<uint>();
                uint index = (uint)c;
                buffer[length++] = 'S';
                buffer[length++] = 'e';
                buffer[length++] = 't';

                count.TryFormat(buffer[length..], out int wr);
                length += wr;

                buffer[length++] = 'E';
                buffer[length++] = 'l';
                buffer[length++] = 'e';
                buffer[length++] = 'm';
                buffer[length++] = 'e';
                buffer[length++] = 'n';
                buffer[length++] = 't';
                if (count > 1)
                {
                    buffer[length++] = 's';
                }

                buffer[length++] = '<';

                RuntimeType elementType = new((uint)a);
                int written = elementType.ToString(buffer[length..]);
                length += written;

                buffer[length++] = '>';
                buffer[length++] = '(';

                index.TryFormat(buffer[length..], out written);
                length += written;

                buffer[length++] = ',';
                buffer[length++] = ' ';

                allocation.Address.TryFormat(buffer[length..], out written);
                length += written;

                buffer[length++] = ')';
            }

            return length;
        }

        /// <summary>
        /// Creates entities and adds them into the selection.
        /// </summary>
        public static Instruction CreateEntity(uint count = 1)
        {
            return new(Type.CreateEntity, count, 0, 0);
        }

        /// <summary>
        /// Destroys all selected entities.
        /// </summary>
        public static Instruction DestroySelection()
        {
            return new(Type.DestroyEntities, 0, 0, 0);
        }

        /// <summary>
        /// Destroys a range of selected entities in added order.
        /// </summary>
        public static Instruction DestroySelection(uint start, uint count)
        {
            return new(Type.DestroyEntities, start, count, 0);
        }

        public static Instruction ClearSelection()
        {
            return new(Type.ClearSelection, 0, 0, 0);
        }

        /// <summary>
        /// Adds the given entity to the selection.
        /// </summary>
        public static Instruction SelectEntity(uint entity)
        {
            return new(Type.SelectEntity, 1, entity, 0);
        }

        /// <summary>
        /// Adds the entity at this relative index, where 0 represents
        /// the last created entity.
        /// </summary>
        public static Instruction SelectPreviouslyCreatedEntity(uint relativeOffset)
        {
            return new(Type.SelectEntity, 0, relativeOffset, 0);
        }

        /// <summary>
        /// Assigns the parent of all entities in the selection to
        /// the given existing entity.
        /// </summary>
        public static Instruction SetParent(uint entity)
        {
            return new(Type.SetParent, 1, entity, 0);
        }

        /// <summary>
        /// Assigns the parent of all entities in the selection to
        /// the entity at the relative index. Where 0 represents the
        /// last created entity.
        /// </summary>
        public static Instruction SetParentToPreviouslyCreatedEntity(uint relativeOffset)
        {
            return new(Type.SetParent, 0, relativeOffset, 0);
        }

        public static Instruction AddReference(uint entity)
        {
            return new(Type.AddReference, 1, entity, 0);
        }

        /// <summary>
        /// Adds a reference to the entity at the given relative offset for
        /// all selected entities. Where 0 is the last created entity.
        /// </summary>
        public static Instruction AddReferenceTowardsPreviouslyCreatedEntity(uint relativeOffset)
        {
            return new(Type.AddReference, 0, relativeOffset, 0);
        }

        public static Instruction RemoveReference(rint reference)
        {
            return new(Type.RemoveReference, reference.value, 0, 0);
        }

        /// <summary>
        /// Adds the given component to all entities inside the selection.
        /// </summary>
        public static Instruction AddComponent<T>(T component) where T : unmanaged
        {
            return AddComponent(component, out _);
        }

        public static Instruction AddComponent<T>(T component, out Allocation allocation) where T : unmanaged
        {
            allocation = Allocation.Create(component);
            return new(Type.AddComponent, RuntimeType.Get<T>().value, (ulong)allocation.Address, 0);
        }

        public static Instruction AddComponent(RuntimeType componentType)
        {
            Allocation allocation = Allocation.Create(componentType.Size);
            return new(Type.AddComponent, componentType.value, (ulong)allocation.Address, 0);
        }

        public static Instruction AddComponent(RuntimeType componentType, ReadOnlySpan<byte> componentData)
        {
            Allocation allocation = Allocation.Create(componentData);
            return new(Type.AddComponent, componentType.value, (ulong)allocation.Address, 0);
        }

        public static Instruction RemoveComponent<T>() where T : unmanaged
        {
            return RemoveComponent(RuntimeType.Get<T>());
        }

        public static Instruction RemoveComponent(RuntimeType componentType)
        {
            return new(Type.RemoveComponent, componentType.value, 0, 0);
        }

        /// <summary>
        /// Modifies the component of the given type on the selected entities.
        /// </summary>
        public static Instruction SetComponent<T>(T component) where T : unmanaged
        {
            Allocation allocation = Allocation.Create(component);
            return new(Type.SetComponent, RuntimeType.Get<T>().value, (ulong)allocation.Address, 0);
        }

        public static Instruction SetComponent(RuntimeType componentType, ReadOnlySpan<byte> componentData)
        {
            Allocation allocation = Allocation.Create(componentData);
            return new(Type.SetComponent, componentType.value, (ulong)allocation.Address, 0);
        }

        /// <summary>
        /// Creates an array of the specified type and length for the selected entities.
        /// </summary>
        public static Instruction CreateArray<T>(uint length) where T : unmanaged
        {
            return CreateArray(RuntimeType.Get<T>(), length);
        }

        public static Instruction CreateArray(RuntimeType arrayType, uint length)
        {
            Allocation allocaton = new(arrayType.Size * length);
            return new(Type.CreateArray, arrayType.value, (ulong)(nint)allocaton, length);
        }

        public unsafe static Instruction CreateArray<T>(ReadOnlySpan<T> values) where T : unmanaged
        {
            Allocation allocation = Allocation.Create(values);
            return new(Type.CreateArray, RuntimeType.Get<T>().value, (ulong)(nint)allocation, (uint)values.Length);
        }

        public static Instruction DestroyArray<T>() where T : unmanaged
        {
            return DestroyArray(RuntimeType.Get<T>());
        }

        public static Instruction DestroyArray(RuntimeType elementType)
        {
            return new(Type.DestroyArray, elementType.value, 0, 0);
        }

        public unsafe static Instruction SetArrayElement<T>(uint index, T element) where T : unmanaged
        {
            Allocation allocation = new(sizeof(uint) + (uint)sizeof(T));
            allocation.Write(0, 1);
            allocation.Write(sizeof(uint), element);
            return new(Type.SetArrayElement, RuntimeType.Get<T>().value, (ulong)(nint)allocation, index);
        }

        public unsafe static Instruction SetArrayElement<T>(uint index, Span<T> elements) where T : unmanaged
        {
            Allocation allocation = new(sizeof(uint) + (uint)(sizeof(T) * elements.Length));
            allocation.Write(0, (uint)elements.Length);
            allocation.Write(sizeof(uint), elements);
            return new(Type.SetArrayElement, RuntimeType.Get<T>().value, (ulong)(nint)allocation, index);
        }

        public unsafe static Instruction SetArrayElement<T>(uint index, ReadOnlySpan<T> elements) where T : unmanaged
        {
            Allocation allocation = new(sizeof(uint) + (uint)(sizeof(T) * elements.Length));
            allocation.Write(0, (uint)elements.Length);
            allocation.Write(sizeof(uint), elements);
            return new(Type.SetArrayElement, RuntimeType.Get<T>().value, (ulong)(nint)allocation, index);
        }

        public static Instruction ResizeArray<T>(uint newLength) where T : unmanaged
        {
            return ResizeArray(RuntimeType.Get<T>(), newLength);
        }

        public static Instruction ResizeArray(RuntimeType arrayType, uint newLength)
        {
            return new(Type.ResizeArray, arrayType.value, newLength, 0);
        }

        readonly void ISerializable.Write(BinaryWriter writer)
        {
            writer.WriteValue(type);
            writer.WriteValue(a);
            writer.WriteValue(b);
            writer.WriteValue(c);
        }

        void ISerializable.Read(BinaryReader reader)
        {
            Type operation = reader.ReadValue<Type>();
            ulong a = reader.ReadValue<ulong>();
            ulong b = reader.ReadValue<ulong>();
            ulong c = reader.ReadValue<ulong>();
            this = new(operation, a, b, c);
        }

        public readonly override bool Equals(object? obj)
        {
            return obj is Instruction command && Equals(command);
        }

        public readonly bool Equals(Instruction other)
        {
            return type == other.type && a == other.a && b == other.b && c == other.c;
        }

        public readonly override int GetHashCode()
        {
            return HashCode.Combine(type, a, b, c);
        }

        public static bool operator ==(Instruction left, Instruction right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Instruction left, Instruction right)
        {
            return !(left == right);
        }

        public enum Type : byte
        {
            CreateEntity,
            DestroyEntities,

            ClearSelection,
            SelectEntity,

            SetParent,

            AddComponent,
            RemoveComponent,
            SetComponent,

            CreateArray,
            DestroyArray,
            ResizeArray,
            SetArrayElement,

            AddReference,
            RemoveReference
        }
    }
}