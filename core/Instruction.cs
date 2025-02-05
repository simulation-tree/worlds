using System;
using System.ComponentModel;
using Unmanaged;

namespace Worlds
{
    /// <summary>
    /// Contains an instruction for working with <see cref="World"/>s.
    /// </summary>
    public struct Instruction : IDisposable, ISerializable, IEquatable<Instruction>
    {
        /// <summary>
        /// The type of this instruction.
        /// </summary>
        public readonly Type type;

        private readonly ulong a;
        private readonly ulong b;
        private readonly ulong c;

        /// <summary>
        /// First parameter of the instruction.
        /// </summary>
        public readonly ulong A => a;

        /// <summary>
        /// Second parameter of the instruction.
        /// </summary>
        public readonly ulong B => b;

        /// <summary>
        /// Third parameter of the instruction.
        /// </summary>
        public readonly ulong C => c;

        private Instruction(Type operation, ulong a, ulong b, ulong c)
        {
            type = operation;
            this.a = a;
            this.b = b;
            this.c = c;
        }

        /// <inheritdoc/>
        public unsafe readonly void Dispose()
        {
            if (type == Type.AddComponent || type == Type.SetComponent || type == Type.CreateArray || type == Type.SetArrayElement)
            {
                Allocation allocation = new((void*)(nint)b);
                allocation.Dispose();
            }
        }

        /// <inheritdoc/>
        public unsafe readonly override string ToString()
        {
            USpan<char> buffer = stackalloc char[256];
            uint length = ToString(buffer);
            return buffer.Slice(0, length).ToString();
        }

        /// <summary>
        /// Builds a string representation of this instruction.
        /// </summary>
        public unsafe readonly uint ToString(USpan<char> buffer)
        {
            uint length = 0;
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
                length += a.ToString(buffer.Slice(length));
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
                length += a.ToString(buffer.Slice(length));
                buffer[length++] = ',';
                length += b.ToString(buffer.Slice(length));
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
                length += a.ToString(buffer.Slice(length));
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

                length += a.ToString(buffer.Slice(length));
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

                length += a.ToString(buffer.Slice(length));
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
                length += a.ToString(buffer.Slice(length));
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
                ComponentType componentType = new((byte)a);
                length += componentType.ToString(buffer.Slice(length));
                buffer[length++] = '>';
                buffer[length++] = '(';
                nint allocationAddress = (nint)b;
                length += allocationAddress.ToString(buffer.Slice(length));
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
                ComponentType componentType = new((byte)a);
                length += componentType.ToString(buffer.Slice(length));
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
                ComponentType componentType = new((byte)a);
                length += componentType.ToString(buffer.Slice(length));
                buffer[length++] = '>';
                buffer[length++] = '(';
                nint allocationAddress = (nint)b;
                length += allocationAddress.ToString(buffer.Slice(length));
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
                ArrayElementType arrayElementType = new((byte)a);
                length += arrayElementType.ToString(buffer.Slice(length));
                buffer[length++] = '>';
                buffer[length++] = '(';
                length += b.ToString(buffer.Slice(length));
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
                ArrayElementType arrayElementType = new((byte)a);
                length += arrayElementType.ToString(buffer.Slice(length));
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
                ArrayElementType arrayElementType = new((byte)a);
                length += arrayElementType.ToString(buffer.Slice(length));
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
                length += count.ToString(buffer.Slice(length));
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

                ArrayElementType elementType = new((byte)a);
                length += elementType.ToString(buffer.Slice(length));

                buffer[length++] = '>';
                buffer[length++] = '(';
                length += index.ToString(buffer.Slice(length));
                buffer[length++] = ',';
                buffer[length++] = ' ';
                length += allocation.Address.ToString(buffer.Slice(length));
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

        /// <summary>
        /// Empty the entity selection.
        /// </summary>
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

        /// <summary>
        /// Adds a reference to the <paramref name="entity"/> for all selected entities.
        /// </summary>
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

        /// <summary>
        /// Removes the local <paramref name="reference"/> from selected entities.
        /// </summary>
        public static Instruction RemoveReference(rint reference)
        {
            return new(Type.RemoveReference, reference.value, 0, 0);
        }

        /// <summary>
        /// Adds the given component to all entities inside the selection.
        /// </summary>
        public static Instruction AddComponent<T>(T component, Schema schema) where T : unmanaged
        {
            return AddComponent(component, schema, out _);
        }

        /// <summary>
        /// Adds the given component to all entities inside the selection.
        /// <para>
        /// <paramref name="allocation"/> will contain the memory of the component.
        /// </para>
        /// </summary>
        public static Instruction AddComponent<T>(T component, Schema schema, out Allocation allocation) where T : unmanaged
        {
            allocation = Allocation.Create(component);
            return new(Type.AddComponent, schema.GetComponent<T>(), (ulong)allocation.Address, 0);
        }

        /// <summary>
        /// Adds the given component to all entities inside the selection.
        /// </summary>
        public static Instruction AddComponent(ComponentType componentType, Schema schema)
        {
            ushort componentSize = schema.GetSize(componentType);
            Allocation allocation = Allocation.Create(componentSize);
            return new(Type.AddComponent, componentType.index, (ulong)allocation.Address, 0);
        }

        /// <summary>
        /// Adds the given component to all entities inside the selection.
        /// </summary>
        public static Instruction AddComponent(ComponentType componentType, USpan<byte> componentData)
        {
            Allocation allocation = Allocation.Create(componentData);
            return new(Type.AddComponent, componentType.index, (ulong)allocation.Address, 0);
        }

        /// <summary>
        /// Removes the component of the given type from the selected entities.
        /// </summary>
        public static Instruction RemoveComponent<T>(Schema schema) where T : unmanaged
        {
            return RemoveComponent(schema.GetComponent<T>());
        }

        /// <summary>
        /// Removes the component of the given type from the selected entities.
        /// </summary>
        public static Instruction RemoveComponent(ComponentType componentType)
        {
            return new(Type.RemoveComponent, componentType.index, 0, 0);
        }

        /// <summary>
        /// Modifies the component of the given type on the selected entities.
        /// </summary>
        public static Instruction SetComponent<T>(T component, Schema schema) where T : unmanaged
        {
            Allocation allocation = Allocation.Create(component);
            return new(Type.SetComponent, schema.GetComponent<T>(), (ulong)allocation.Address, 0);
        }

        /// <summary>
        /// Modifies the component of the given type on the selected entities.
        /// </summary>
        public static Instruction SetComponent(ComponentType componentType, USpan<byte> componentData)
        {
            Allocation allocation = Allocation.Create(componentData);
            return new(Type.SetComponent, componentType.index, (ulong)allocation.Address, 0);
        }

        /// <summary>
        /// Creates an array of the specified type and length for the selected entities.
        /// </summary>
        public static Instruction CreateArray<T>(uint length, Schema schema) where T : unmanaged
        {
            ArrayElementType arrayElementType = schema.GetArrayElement<T>();
            ushort arrayElementSize = schema.GetSize(arrayElementType);
            Allocation allocaton = new(arrayElementSize * length);
            return new(Type.CreateArray, arrayElementType.index, (ulong)(nint)allocaton, length);
        }

        /// <summary>
        /// Creates an array of the specified type and length for the selected entities.
        /// </summary>
        public static Instruction CreateArray(ArrayElementType arrayElementType, uint length, Schema schema)
        {
            ushort arrayElementSize = schema.GetSize(arrayElementType);
            Allocation allocaton = new(arrayElementSize * length);
            return new(Type.CreateArray, arrayElementType, (ulong)(nint)allocaton, length);
        }

        /// <summary>
        /// Creates an array of the specified type and length for the selected entities.
        /// </summary>
        public unsafe static Instruction CreateArray<T>(USpan<T> values, Schema schema) where T : unmanaged
        {
            Allocation allocation = Allocation.Create(values);
            ArrayElementType arrayElementType = schema.GetArrayElement<T>();
            return new(Type.CreateArray, arrayElementType, (ulong)(nint)allocation, values.Length);
        }

        /// <summary>
        /// Destroys the array of the specified type for the selected entities.
        /// </summary>
        public static Instruction DestroyArray<T>(Schema schema) where T : unmanaged
        {
            ArrayElementType arrayElementType = schema.GetArrayElement<T>();
            return DestroyArray(arrayElementType);
        }

        /// <summary>
        /// Destroys the array of the specified type for the selected entities.
        /// </summary>
        public static Instruction DestroyArray(ArrayElementType arrayElementType)
        {
            return new(Type.DestroyArray, arrayElementType.index, 0, 0);
        }

        /// <summary>
        /// Sets the element at the given index in the array of the specified type.
        /// </summary>
        public unsafe static Instruction SetArrayElement<T>(uint index, T element, Schema schema) where T : unmanaged
        {
            Allocation allocation = new((uint)(sizeof(uint) + sizeof(T)));
            allocation.Write(0, 1);
            allocation.Write(sizeof(uint), element);
            ArrayElementType arrayElementType = schema.GetArrayElement<T>();
            return new(Type.SetArrayElement, arrayElementType, (ulong)(nint)allocation, index);
        }

        /// <summary>
        /// Sets the element at the given index in the array of the specified type.
        /// </summary>
        public unsafe static Instruction SetArrayElement<T>(uint index, USpan<T> elements, Schema schema) where T : unmanaged
        {
            Allocation allocation = new(sizeof(uint) + TypeInfo<T>.size * elements.Length);
            allocation.Write(0, elements.Length);
            allocation.Write(sizeof(uint), elements);
            ArrayElementType arrayElementType = schema.GetArrayElement<T>();
            return new(Type.SetArrayElement, arrayElementType, (ulong)(nint)allocation, index);
        }

        /// <summary>
        /// Resizes the array of the specified type to the new length.
        /// </summary>
        public static Instruction ResizeArray<T>(uint newLength, Schema schema) where T : unmanaged
        {
            ArrayElementType arrayElementType = schema.GetArrayElement<T>();
            return ResizeArray(arrayElementType, newLength);
        }

        /// <summary>
        /// Resizes the array of the specified type to the new length.
        /// </summary>
        public static Instruction ResizeArray(ArrayElementType arrayElementType, uint newLength)
        {
            return new(Type.ResizeArray, arrayElementType.index, newLength, 0);
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

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj)
        {
            return obj is Instruction command && Equals(command);
        }

        /// <inheritdoc/>
        public readonly bool Equals(Instruction other)
        {
            return type == other.type && a == other.a && b == other.b && c == other.c;
        }

        /// <inheritdoc/>
        public readonly override int GetHashCode()
        {
            return HashCode.Combine(type, a, b, c);
        }

        /// <inheritdoc/>
        public static bool operator ==(Instruction left, Instruction right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(Instruction left, Instruction right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Identifier for the type of instruction.
        /// </summary>
        public enum Type : byte
        {
            /// <summary>
            /// Creates entities.
            /// </summary>
            CreateEntity,

            /// <summary>
            /// Destroys entities.
            /// </summary>
            DestroyEntities,

            /// <summary>
            /// Clears selection.
            /// </summary>
            ClearSelection,

            /// <summary>
            /// Selects entities.
            /// </summary>
            SelectEntity,

            /// <summary>
            /// Assigns parents.
            /// </summary>
            SetParent,

            /// <summary>
            /// Adds components.
            /// </summary>
            AddComponent,

            /// <summary>
            /// Removes components.
            /// </summary>
            RemoveComponent,

            /// <summary>
            /// Modifies components.
            /// </summary>
            SetComponent,
            
            /// <summary>
            /// Adds tags.
            /// </summary>
            AddTag,

            /// <summary>
            /// Removes tags.
            /// </summary>
            RemoveTag,

            /// <summary>
            /// Creates arrays.
            /// </summary>
            CreateArray,

            /// <summary>
            /// Destroys arrays.
            /// </summary>
            DestroyArray,

            /// <summary>
            /// Resizes arrays.
            /// </summary>
            ResizeArray,

            /// <summary>
            /// Modifies elements in arrays.
            /// </summary>
            SetArrayElement,

            /// <summary>
            /// Adds references.
            /// </summary>
            AddReference,

            /// <summary>
            /// Removes references.
            /// </summary>
            RemoveReference
        }
    }
}