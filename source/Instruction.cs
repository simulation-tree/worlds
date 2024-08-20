using System;
using Unmanaged;
using Unmanaged.Collections;

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
            if (type == Type.AddComponent || type == Type.SetComponent)
            {
                Allocation allocation = new((void*)(nint)b);
                allocation.Dispose();
            }
            else if (type == Type.InsertElement || type == Type.ModifyElement)
            {
                UnsafeArray* array = (UnsafeArray*)(nint)b;
                UnsafeArray.Free(ref array);
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
            else if (type == Type.CreateList)
            {
                buffer[length++] = 'C';
                buffer[length++] = 'r';
                buffer[length++] = 'e';
                buffer[length++] = 'a';
                buffer[length++] = 't';
                buffer[length++] = 'e';
                buffer[length++] = 'L';
                buffer[length++] = 'i';
                buffer[length++] = 's';
                buffer[length++] = 't';
                buffer[length++] = '<';
                RuntimeType elementType = new((uint)a);
                int written = elementType.ToString(buffer[length..]);
                length += written;
                buffer[length++] = '>';
                buffer[length++] = '(';
                uint count = (uint)b;
                count.TryFormat(buffer[length..], out written);
                length += written;
                buffer[length++] = ')';
            }
            else if (type == Type.DestroyList)
            {
                buffer[length++] = 'D';
                buffer[length++] = 'e';
                buffer[length++] = 's';
                buffer[length++] = 't';
                buffer[length++] = 'r';
                buffer[length++] = 'o';
                buffer[length++] = 'y';
                buffer[length++] = 'L';
                buffer[length++] = 'i';
                buffer[length++] = 's';
                buffer[length++] = 't';
                buffer[length++] = '<';
                RuntimeType elementType = new((uint)a);
                int written = elementType.ToString(buffer[length..]);
                length += written;
                buffer[length++] = '>';
                buffer[length++] = '(';
                buffer[length++] = ')';
            }
            else if (type == Type.ClearList)
            {
                buffer[length++] = 'C';
                buffer[length++] = 'l';
                buffer[length++] = 'e';
                buffer[length++] = 'a';
                buffer[length++] = 'r';
                buffer[length++] = 'L';
                buffer[length++] = 'i';
                buffer[length++] = 's';
                buffer[length++] = 't';
                buffer[length++] = '<';
                RuntimeType elementType = new((uint)a);
                int written = elementType.ToString(buffer[length..]);
                length += written;
                buffer[length++] = '>';
                buffer[length++] = '(';
                buffer[length++] = ')';
            }
            else if (type == Type.InsertElement)
            {
                uint index = (uint)c;
                UnsafeArray* array = (UnsafeArray*)(nint)b;
                uint count = UnsafeArray.GetLength(array);
                if (index == uint.MaxValue)
                {
                    buffer[length++] = 'A';
                    buffer[length++] = 'd';
                    buffer[length++] = 'd';
                    if (count > 1)
                    {
                        count.TryFormat(buffer[length..], out int wr);
                        length += wr;
                    }

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
                    ((nint)array).TryFormat(buffer[length..], out written);
                    length += written;
                    buffer[length++] = ')';
                }
                else
                {
                    buffer[length++] = 'I';
                    buffer[length++] = 'n';
                    buffer[length++] = 's';
                    buffer[length++] = 'e';
                    buffer[length++] = 'r';
                    buffer[length++] = 't';
                    if (count > 1)
                    {
                        count.TryFormat(buffer[length..], out int wr);
                        length += wr;
                    }

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
                    ((nint)array).TryFormat(buffer[length..], out written);
                    length += written;
                    buffer[length++] = ',';
                    index.TryFormat(buffer[length..], out written);
                    length += written;
                    buffer[length++] = ')';
                }
            }
            else if (type == Type.RemoveElement)
            {
                buffer[length++] = 'R';
                buffer[length++] = 'e';
                buffer[length++] = 'm';
                buffer[length++] = 'o';
                buffer[length++] = 'v';
                buffer[length++] = 'e';
                buffer[length++] = 'E';
                buffer[length++] = 'l';
                buffer[length++] = 'e';
                buffer[length++] = 'm';
                buffer[length++] = 'e';
                buffer[length++] = 'n';
                buffer[length++] = 't';
                buffer[length++] = '<';
                RuntimeType elementType = new((uint)a);
                int written = elementType.ToString(buffer[length..]);
                length += written;
                buffer[length++] = '>';
                buffer[length++] = '(';
                nint index = (nint)b;
                index.TryFormat(buffer[length..], out written);
                length += written;
                buffer[length++] = ')';
            }
            else if (type == Type.ModifyElement)
            {
                buffer[length++] = 'M';
                buffer[length++] = 'o';
                buffer[length++] = 'd';
                buffer[length++] = 'i';
                buffer[length++] = 'f';
                buffer[length++] = 'y';
                buffer[length++] = 'E';
                buffer[length++] = 'l';
                buffer[length++] = 'e';
                buffer[length++] = 'm';
                buffer[length++] = 'e';
                buffer[length++] = 'n';
                buffer[length++] = 't';
                buffer[length++] = '<';
                RuntimeType elementType = new((uint)a);
                int written = elementType.ToString(buffer[length..]);
                length += written;
                buffer[length++] = '>';
                buffer[length++] = '(';
                nint allocationAddress = (nint)b;
                allocationAddress.TryFormat(buffer[length..], out written);
                length += written;
                buffer[length++] = ',';
                nint index = (nint)c;
                index.TryFormat(buffer[length..], out written);
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
        /// Adds the entity at this relative index, where 0 represents
        /// the last created entity.
        /// </summary>
        public static Instruction SelectEntity(uint relativeOffset)
        {
            return new(Type.SelectEntity, 0, relativeOffset, 0);
        }

        /// <summary>
        /// Adds the given entity to the selection.
        /// </summary>
        public static Instruction SelectEntity(eint entity)
        {
            return new(Type.SelectEntity, 1, entity.value, 0);
        }

        /// <summary>
        /// Assigns the parent of all entities in the selection to
        /// the entity at the relative index. Where 0 represents the
        /// last created entity.
        /// </summary>
        public static Instruction SetParent(uint relativeOffset)
        {
            return new(Type.SetParent, 0, relativeOffset, 0);
        }

        /// <summary>
        /// Assigns the parent of all entities in the selection to
        /// the given existing entity.
        /// </summary>
        public static Instruction SetParent(eint entity)
        {
            return new(Type.SetParent, 1, entity.value, 0);
        }

        /// <summary>
        /// Adds a reference to the entity at the given relative offset for
        /// all selected entities. Where 0 is the last created entity.
        /// </summary>
        public static Instruction AddReference(uint relativeOffset)
        {
            return new(Type.AddReference, 0, relativeOffset, 0);
        }

        public static Instruction AddReference(eint entity)
        {
            return new(Type.AddReference, 1, entity.value, 0);
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
        /// Creates a list of the specified type for the selected entities.
        /// </summary>
        public static Instruction CreateList<T>(uint count = 0) where T : unmanaged
        {
            return new(Type.CreateList, RuntimeType.Get<T>().value, count, 0);
        }

        public static Instruction CreateList(RuntimeType elementType, uint count = 0)
        {
            return new(Type.CreateList, elementType.value, count, 0);
        }

        public static Instruction DestroyList<T>() where T : unmanaged
        {
            return new(Type.DestroyList, RuntimeType.Get<T>().value, 0, 0);
        }

        public static Instruction DestroyList(RuntimeType elementType)
        {
            return new(Type.DestroyList, elementType.value, 0, 0);
        }

        /// <summary>
        /// Clears lists of the given type on the selected entities.
        /// </summary>
        public static Instruction ClearList<T>() where T : unmanaged
        {
            return new(Type.ClearList, RuntimeType.Get<T>().value, 0, 0);
        }

        public static Instruction ClearCollection(RuntimeType elementType)
        {
            return new(Type.ClearList, elementType.value, 0, 0);
        }

        public unsafe static Instruction AppendToList<T>(T element) where T : unmanaged
        {
            UnsafeArray* array = UnsafeArray.Allocate<T>(1);
            UnsafeArray.GetRef<T>(array, 0) = element;
            return new(Type.InsertElement, RuntimeType.Get<T>().value, (ulong)(nint)array, uint.MaxValue);
        }

        public unsafe static Instruction AppendToList<T>(ReadOnlySpan<T> elements) where T : unmanaged
        {
            UnsafeArray* array = UnsafeArray.Allocate(elements);
            return new(Type.InsertElement, RuntimeType.Get<T>().value, (ulong)(nint)array, uint.MaxValue);
        }

        public unsafe static Instruction InsertElement<T>(T element, uint index) where T : unmanaged
        {
            UnsafeArray* array = UnsafeArray.Allocate<T>(1);
            UnsafeArray.GetRef<T>(array, 0) = element;
            return new(Type.InsertElement, RuntimeType.Get<T>().value, (ulong)(nint)array, index);
        }

        public unsafe static Instruction InsertElement(RuntimeType elementType, ReadOnlySpan<byte> elementData, uint index)
        {
            UnsafeArray* array = UnsafeArray.Allocate(elementData);
            return new(Type.InsertElement, elementType.value, (ulong)(nint)array, index);
        }

        public static Instruction RemoveElement<T>(uint index) where T : unmanaged
        {
            return new(Type.RemoveElement, RuntimeType.Get<T>().value, index, 0);
        }

        public static Instruction RemoveElement(RuntimeType elementType, uint index)
        {
            return new(Type.RemoveElement, elementType.value, index, 0);
        }

        public static Instruction ModifyElement<T>(T element, uint index) where T : unmanaged
        {
            Allocation allocation = Allocation.Create(element);
            return new(Type.ModifyElement, RuntimeType.Get<T>().value, (ulong)allocation.Address, index);
        }

        public static Instruction ModifyElement(RuntimeType elementType, ReadOnlySpan<byte> elementData, uint index)
        {
            Allocation allocation = Allocation.Create(elementData);
            return new(Type.ModifyElement, elementType.value, (ulong)allocation.Address, index);
        }

        void ISerializable.Write(BinaryWriter writer)
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

            CreateList,
            DestroyList,
            ClearList,

            InsertElement,
            RemoveElement,
            ModifyElement,

            AddReference,
            RemoveReference
        }
    }
}