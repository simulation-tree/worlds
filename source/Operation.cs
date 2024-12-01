using Collections;
using System;
using System.Diagnostics;
using Unmanaged;

namespace Worlds
{
    /// <summary>
    /// Series of world instructions to perform all in one go.
    /// </summary>
    [DebuggerTypeProxy(typeof(OperationDebugView))]
    public struct Operation : IDisposable
    {
        private Allocation list;

        /// <summary>
        /// Amount of contained instructions.
        /// </summary>
        public readonly uint Length => list.Read<uint>();

        /// <summary>
        /// Indexer for accessing each <see cref="Instruction"/>.
        /// </summary>
        public unsafe readonly Instruction this[uint index]
        {
            get
            {
                ThrowIfOutOfRange(index);
                uint start = sizeof(uint) + sizeof(uint);
                return list.Read<Instruction>(start + TypeInfo<Instruction>.size * index);
            }
        }

        /// <summary>
        /// Checks if this operation has been disposed.
        /// </summary>
        public readonly bool IsDisposed => list.IsDisposed;

#if NET
        /// <summary>
        /// Creates a new empty operation for writing world instructions into.
        /// </summary>
        public unsafe Operation() : this(0)
        {
        }
#endif
        /// <summary>
        /// Creates a new operation for writing world instructions into.
        /// </summary>
        public unsafe Operation(uint initialCapacity = 0)
        {
            uint start = sizeof(uint) + sizeof(uint);
            uint size = start + TypeInfo<Instruction>.size * initialCapacity;
            list = new(size);
            list.Write(sizeof(uint) * 0, 0);
            list.Write(sizeof(uint) * 1, initialCapacity);
        }

        private Operation(Allocation list)
        {
            this.list = list;
        }

        /// <summary>
        /// Disposes the contained instructions and the operation container itself.
        /// </summary>
        public readonly void Dispose()
        {
            ThrowIfDisposed();

            ClearInstructions();
            list.Dispose();
        }

        /// <summary>
        /// Retrieves all instructions as a span.
        /// </summary>
        /// <returns></returns>
        public readonly USpan<Instruction> AsSpan()
        {
            ThrowIfDisposed();

            uint start = sizeof(uint) + sizeof(uint);
            return new USpan<Instruction>((nint)(list.Address + start), Length);
        }

        /// <inheritdoc/>
        public readonly override string ToString()
        {
            USpan<char> buffer = stackalloc char[64];
            uint length = ToString(buffer);
            return buffer.Slice(0, length).ToString();
        }

        /// <summary>
        /// Builds a string representation of this <see cref="Operation"/>.
        /// </summary>
        public readonly uint ToString(USpan<char> buffer)
        {
            uint thisLength = Length;
            uint length = 0;
            length += "Operation".AsUSpan().CopyTo(buffer);
            if (thisLength == 0)
            {
                length += "(Empty)".AsUSpan().CopyTo(buffer.Slice(length));
            }
            else if (thisLength == 1)
            {
                length += "(1 instruction)".AsUSpan().CopyTo(buffer.Slice(length));
            }
            else
            {
                buffer[length++] = '(';
                length += thisLength.ToString(buffer.Slice(length));
                buffer[length++] = ' ';
                length += "instructions)".AsUSpan().CopyTo(buffer.Slice(length));
            }

            return length;
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfDisposed()
        {
            if (IsDisposed)
            {
                throw new ObjectDisposedException(nameof(Operation));
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfOutOfRange(uint index)
        {
            if (index >= Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        [Conditional("DEBUG")]
        private static void ThrowIfNoEntities(uint count)
        {
            if (count == 0)
            {
                throw new ArgumentException("Entity count is zero", nameof(count));
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfNoInstructions()
        {
            if (Length == 0)
            {
                throw new InvalidOperationException("No instructions are present");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfSelectionIsEmpty()
        {
            uint length = Length;
            for (uint i = length - 1; i != uint.MaxValue; i--)
            {
                Instruction instruction = this[i];
                if (instruction.type == Instruction.Type.SelectEntity)
                {
                    return;
                }
                else if (instruction.type == Instruction.Type.CreateEntity)
                {
                    return;
                }
                else if (instruction.type == Instruction.Type.ClearSelection)
                {
                    break;
                }
            }

            throw new InvalidOperationException("Entity selection is empty, unable to proceed");
        }

        /// <summary>
        /// Fills the given <paramref name="selection"/> list with selected entities.
        /// <para>
        /// IDs referring to created entities are local and start from 1. Use the overload
        /// with the <see cref="World"/> parameter to get the actual entity IDs.
        /// </para>
        /// </summary>
        public readonly void ReadSelection(List<uint> selection)
        {
            ThrowIfDisposed();

            uint length = Length;
            using List<uint> created = new(length);
            for (uint i = 0; i < length; i++)
            {
                Instruction instruction = this[i];
                if (instruction.type == Instruction.Type.CreateEntity)
                {
                    uint createCount = (uint)instruction.A;
                    for (uint c = 0; c < createCount; c++)
                    {
                        uint pretendEntity = created.Count + 1;
                        selection.Add(pretendEntity);
                        created.Add(pretendEntity);
                    }
                }
                else if (instruction.type == Instruction.Type.ClearSelection)
                {
                    selection.Clear();
                }
                else if (instruction.type == Instruction.Type.SelectEntity)
                {
                    bool isRelative = instruction.A == 0;
                    if (isRelative)
                    {
                        uint relativeOffset = (uint)instruction.B;
                        uint entity = created[created.Count - 1 - relativeOffset];
                        selection.Add(entity);
                    }
                    else
                    {
                        uint entity = (uint)instruction.B;
                        selection.Add(entity);
                    }
                }
            }
        }

        /// <summary>
        /// Reads the selection from this operation and fills the given list with the selected entities.
        /// </summary>
        public readonly void ReadSelection(World world, List<uint> selection)
        {
            ThrowIfDisposed();

            uint length = Length;
            using List<uint> created = new(length);
            for (uint i = 0; i < length; i++)
            {
                Instruction instruction = this[i];
                if (instruction.type == Instruction.Type.CreateEntity)
                {
                    uint createCount = (uint)instruction.A;
                    for (uint c = 0; c < createCount; c++)
                    {
                        uint pretendEntity = world.GetNextEntity();
                        selection.Add(pretendEntity);
                        created.Add(pretendEntity);
                    }
                }
                else if (instruction.type == Instruction.Type.ClearSelection)
                {
                    selection.Clear();
                }
                else if (instruction.type == Instruction.Type.SelectEntity)
                {
                    bool isRelative = instruction.A == 0;
                    if (isRelative)
                    {
                        uint relativeOffset = (uint)instruction.B;
                        uint entity = created[created.Count - 1 - relativeOffset];
                        selection.Add(entity);
                    }
                    else
                    {
                        uint entity = (uint)instruction.B;
                        selection.Add(entity);
                    }
                }
            }
        }

        /// <summary>
        /// Removes all instructions inside this operation.
        /// </summary>
        public readonly void ClearInstructions()
        {
            ThrowIfDisposed();
            USpan<Instruction> instructions = AsSpan();
            for (uint i = 0; i < instructions.Length; i++)
            {
                instructions[i].Dispose();
            }

            list.Write(sizeof(uint) * 0, 0);
        }

        /// <summary>
        /// Creates a new entity and automatically appends it to the selection.
        /// </summary>
        public SelectedEntity CreateEntity()
        {
            AddInstruction(Instruction.CreateEntity());
            return new(this);
        }

        /// <summary>
        /// Creates a given amount of entities and automatically appends
        /// them into the selection.
        /// </summary>
        public void CreateEntities(uint count)
        {
            ThrowIfNoEntities(count);
            AddInstruction(Instruction.CreateEntity(count));
        }

        /// <summary>
        /// Destroys all entities in the selection.
        /// </summary>
        public void DestroySelected()
        {
            ThrowIfSelectionIsEmpty();
            AddInstruction(Instruction.DestroySelection());
        }

        /// <summary>
        /// Destroys a range of selected entities, in the order they were added.
        /// </summary>
        public void DestroySelected(uint start, uint length)
        {
            ThrowIfSelectionIsEmpty();
            AddInstruction(Instruction.DestroySelection(start, length));
        }

        /// <summary>
        /// Appends the given entity to the selection.
        /// </summary>
        public SelectedEntity SelectEntity(uint entity)
        {
            AddInstruction(Instruction.SelectEntity(entity));
            return new(this);
        }

        /// <summary>
        /// Appends the given entity to the selection.
        /// </summary>
        public SelectedEntity SelectEntity<T>(T entity) where T : unmanaged, IEntity
        {
            return SelectEntity(entity.GetEntityValue());
        }

        /// <summary>
        /// Appends the entity that was created <paramref name="offset"/> instructions ago
        /// to the selections.
        /// <para>Where 0 is the last created entity.</para>
        /// </summary>
        public void SelectPreviouslyCreatedEntity(uint offset)
        {
            AddInstruction(Instruction.SelectPreviouslyCreatedEntity(offset));
        }

        /// <summary>
        /// Resets the entity selection.
        /// </summary>
        public void ClearSelection()
        {
            AddInstruction(Instruction.ClearSelection());
        }

        /// <summary>
        /// Assigns the given parent of all selected entities to the
        /// given <paramref name="parent"/>.
        /// </summary>
        public void SetParent(uint parent)
        {
            ThrowIfSelectionIsEmpty();
            AddInstruction(Instruction.SetParent(parent));
        }

        /// <summary>
        /// Assigns the parent of every selected entity to the given <paramref name="parent"/>.
        /// </summary>
        public void SetParent<T>(T parent) where T : unmanaged, IEntity
        {
            SetParent(parent.GetEntityValue());
        }

        /// <summary>
        /// Assigns the parent of every selected entitiy to the one that was
        /// created <paramref name="offset"/> instructions ago.
        /// <para>Where 0 is the last created entity.</para>
        /// </summary>
        public void SetParentToPreviouslyCreatedEntity(uint offset)
        {
            ThrowIfSelectionIsEmpty();
            AddInstruction(Instruction.SetParentToPreviouslyCreatedEntity(offset));
        }

        /// <summary>
        /// Adds a reference for every entity in the selection, to
        /// this specific given entity.
        /// </summary>
        public void AddReference(uint entity)
        {
            ThrowIfSelectionIsEmpty();
            AddInstruction(Instruction.AddReference(entity));
        }

        /// <summary>
        /// Adds a reference for every entity in the selection, to the 
        /// entity that was created <paramref name="offset"/> instructions ago.
        /// <para>Where 0 is the last created entity.</para>
        /// </summary>
        public void AddReferenceTowardsPreviouslyCreatedEntity(uint offset)
        {
            ThrowIfSelectionIsEmpty();
            AddInstruction(Instruction.AddReferenceTowardsPreviouslyCreatedEntity(offset));
        }

        /// <summary>
        /// Removes the given reference value from all selected entities.
        /// </summary>
        public void RemoveReference(rint reference)
        {
            ThrowIfSelectionIsEmpty();
            AddInstruction(Instruction.RemoveReference(reference));
        }

        /// <summary>
        /// Adds a new component entry to every entity in the selection.
        /// </summary>
        public void AddComponent<T>(T component) where T : unmanaged
        {
            ThrowIfSelectionIsEmpty();
            AddInstruction(Instruction.AddComponent(component));
        }

        /// <summary>
        /// Assigns the given component value onto every selected entities,
        /// assuming they already contain the component entry.
        /// </summary>
        public void SetComponent<T>(T component) where T : unmanaged
        {
            ThrowIfSelectionIsEmpty();
            AddInstruction(Instruction.SetComponent(component));
        }

        /// <summary>
        /// Removes the given component type from all selected entities.
        /// </summary>
        public void RemoveComponent<T>() where T : unmanaged
        {
            ThrowIfSelectionIsEmpty();
            AddInstruction(Instruction.RemoveComponent<T>());
        }

        /// <summary>
        /// Creates a new array for every selected entities.
        /// </summary>
        public void CreateArray<T>(uint length) where T : unmanaged
        {
            ThrowIfSelectionIsEmpty();
            AddInstruction(Instruction.CreateArray<T>(length));
        }

        /// <summary>
        /// Creates a new array for every selected entities containing the given <paramref name="values"/>.
        /// </summary>
        public void CreateArray<T>(USpan<T> values) where T : unmanaged
        {
            ThrowIfSelectionIsEmpty();
            AddInstruction(Instruction.CreateArray(values));
        }

        /// <summary>
        /// Destroys an existing array on selected entities.
        /// </summary>
        public void DestroyArray<T>() where T : unmanaged
        {
            ThrowIfSelectionIsEmpty();
            AddInstruction(Instruction.DestroyArray<T>());
        }

        /// <summary>
        /// Writes the given value into the array at the given index.
        /// </summary>
        public void SetArrayElement<T>(uint index, T element) where T : unmanaged
        {
            ThrowIfSelectionIsEmpty();
            AddInstruction(Instruction.SetArrayElement(index, element));
        }

        /// <summary>
        /// Writes the given span into the array starting from the given index.
        /// </summary>
        public void SetArrayElements<T>(uint index, USpan<T> elements) where T : unmanaged
        {
            ThrowIfSelectionIsEmpty();
            AddInstruction(Instruction.SetArrayElement(index, elements));
        }

        /// <summary>
        /// Resizes the array for every selected entity.
        /// </summary>
        public void ResizeArray<T>(uint newLength) where T : unmanaged
        {
            ThrowIfSelectionIsEmpty();
            AddInstruction(Instruction.ResizeArray<T>(newLength));
        }

        /// <summary>
        /// Adds a new given <paramref name="instruction"/> to the operation.
        /// </summary>
        public unsafe void AddInstruction(Instruction instruction)
        {
            uint length = list.Read<uint>(sizeof(uint) * 0);
            uint capacity = list.Read<uint>(sizeof(uint) * 1);
            uint start = sizeof(uint) + sizeof(uint);
            if (length == capacity)
            {
                capacity = Math.Max(capacity * 2, 4);
                Allocation.Resize(ref list, start + TypeInfo<Instruction>.size * capacity);
                list.Write(sizeof(uint) * 1, capacity);
            }

            list.Write(sizeof(uint) * 0, length + 1);
            list.Write(start + TypeInfo<Instruction>.size * length, instruction);
        }

        /// <summary>
        /// Removes the instruction at the given <paramref name="index"/>.
        /// </summary>
        public readonly unsafe void RemoveInstructionAt(uint index)
        {
            ThrowIfOutOfRange(index);
            ThrowIfNoInstructions();

            uint length = Length;
            uint start = sizeof(uint) + sizeof(uint);
            for (uint i = index; i < length - 1; i++)
            {
                Instruction instruction = this[i + 1];
                list.Write(start + TypeInfo<Instruction>.size * i, instruction);
            }

            list.Write(sizeof(uint) * 0, length - 1);
        }

        /// <summary>
        /// Creates a new empty operation for writing world instructions into.
        /// </summary>
        public unsafe static Operation Create(uint initialCapacity = 1)
        {
            uint size = sizeof(uint) + sizeof(uint) + TypeInfo<Instruction>.size * initialCapacity;
            Allocation list = new(size);
            list.Write(sizeof(uint) * 0, 0);
            list.Write(sizeof(uint) * 1, initialCapacity);
            return new Operation(list);
        }

        /// <summary>
        /// An entity local to the operation.
        /// </summary>
        public readonly struct SelectedEntity
        {
            private readonly Operation operation;

            internal SelectedEntity(Operation operation)
            {
                this.operation = operation;
            }

            /// <summary>
            /// Submits an instruction to add the given <paramref name="component"/> to this entity.
            /// </summary>
            public readonly void AddComponent<T>(T component) where T : unmanaged
            {
                operation.AddComponent(component);
            }

            public readonly void SetComponent<T>(T component) where T : unmanaged
            {
                operation.SetComponent(component);
            }

            public readonly void RemoveComponent<T>() where T : unmanaged
            {
                operation.RemoveComponent<T>();
            }

            public readonly void CreateArray<T>(uint length) where T : unmanaged
            {
                operation.CreateArray<T>(length);
            }

            public readonly void CreateArray<T>(USpan<T> values) where T : unmanaged
            {
                operation.CreateArray(values);
            }

            public readonly void DestroyArray<T>() where T : unmanaged
            {
                operation.DestroyArray<T>();
            }

            public readonly void ResizeArray<T>(uint newLength) where T : unmanaged
            {
                operation.ResizeArray<T>(newLength);
            }

            public readonly void SetArrayElement<T>(uint index, T element) where T : unmanaged
            {
                operation.SetArrayElement(index, element);
            }

            public readonly void SetArrayElements<T>(uint index, USpan<T> elements) where T : unmanaged
            {
                operation.SetArrayElements(index, elements);
            }
        }

        internal class OperationDebugView
        {
            public readonly uint[] selected;

            public OperationDebugView(Operation operation)
            {
                using List<uint> selection = new();
                operation.ReadSelection(selection);

                selected = new uint[selection.Count];
                for (int i = 0; i < selection.Count; i++)
                {
                    selected[i] = selection[(uint)i];
                }
            }
        }
    }
}