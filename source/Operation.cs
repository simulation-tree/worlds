using System;
using System.Diagnostics;
using Unmanaged;

namespace Simulation
{
    /// <summary>
    /// Series of world instructions to perform all in one go.
    /// </summary>
    public struct Operation : IDisposable
    {
        private Allocation list;

        /// <summary>
        /// Amount of contained instructions.
        /// </summary>
        public readonly uint Length => list.Read<uint>();

        public unsafe readonly Instruction this[uint index]
        {
            get
            {
                ThrowIfOutOfRange(index);
                uint start = sizeof(uint) + sizeof(uint);
                return list.Read<Instruction>((uint)(start + (sizeof(Instruction) * index)));
            }
        }

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
            uint size = (uint)(start + (sizeof(Instruction) * initialCapacity));
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

        public readonly USpan<Instruction> AsSpan()
        {
            ThrowIfDisposed();
            uint start = sizeof(uint) + sizeof(uint);
            return new USpan<Instruction>((nint)(list.Address + start), Length);
        }

        public readonly override string ToString()
        {
            uint length = Length;
            if (length == 0)
            {
                return "Operation(Empty)";
            }
            else if (length == 1)
            {
                return $"Operation(1 instruction)";
            }
            else
            {
                return $"Operation({length} instructions)";
            }
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
        private readonly void ThrowIfNoEntities(uint count)
        {
            if (count == 0)
            {
                throw new ArgumentException("Entity count is zero.", nameof(count));
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfNoInstructions()
        {
            if (Length == 0)
            {
                throw new InvalidOperationException("No instructions are present.");
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

            throw new InvalidOperationException("Entity selection is empty, unable to proceed.");
        }

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
        /// Resets the selection.
        /// </summary>
        public void ClearSelection()
        {
            AddInstruction(Instruction.ClearSelection());
        }

        /// <summary>
        /// Assigns the given parent of all selected entities to the
        /// given existing entity.
        /// </summary>
        public void SetParent(uint parent)
        {
            ThrowIfSelectionIsEmpty();
            AddInstruction(Instruction.SetParent(parent));
        }

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
            AddInstruction(Instruction.AddComponent<T>(component));
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
        /// Creates a new array for every selected entities.
        /// </summary>
        public void CreateArray<T>(uint length) where T : unmanaged
        {
            ThrowIfSelectionIsEmpty();
            AddInstruction(Instruction.CreateArray<T>(length));
        }

        public void CreateArray<T>(USpan<T> values) where T : unmanaged
        {
            ThrowIfSelectionIsEmpty();
            AddInstruction(Instruction.CreateArray<T>(values));
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

        public void ResizeArray<T>(uint newLength) where T : unmanaged
        {
            ThrowIfSelectionIsEmpty();
            AddInstruction(Instruction.ResizeArray<T>(newLength));
        }

        private unsafe void AddInstruction(Instruction instruction)
        {
            uint length = list.Read<uint>(sizeof(uint) * 0);
            uint capacity = list.Read<uint>(sizeof(uint) * 1);
            uint start = sizeof(uint) + sizeof(uint);
            if (length == capacity)
            {
                capacity = Math.Max(capacity * 2, 4);
                Allocation.Resize(ref list, (uint)(start + (sizeof(Instruction) * capacity)));
                list.Write(sizeof(uint) * 1, capacity);
            }

            list.Write(sizeof(uint) * 0, length + 1);
            list.Write((uint)(start + (sizeof(Instruction) * length)), instruction);
        }

        public unsafe void RemoveInstructionAt(uint index)
        {
            ThrowIfOutOfRange(index);
            ThrowIfNoInstructions();

            uint length = Length;
            uint start = sizeof(uint) + sizeof(uint);
            for (uint i = index; i < length - 1; i++)
            {
                Instruction instruction = this[i + 1];
                list.Write((uint)(start + (sizeof(Instruction) * i)), instruction);
            }

            list.Write(sizeof(uint) * 0, length - 1);
        }

        public unsafe static Operation Create(uint initialCapacity = 0)
        {
            uint size = (uint)(sizeof(uint) + sizeof(uint) + (sizeof(Instruction) * initialCapacity));
            Allocation list = new(size);
            list.Write(sizeof(uint) * 0, 0);
            list.Write(sizeof(uint) * 1, initialCapacity);
            return new Operation(list);
        }

        public readonly struct SelectedEntity
        {
            private readonly Operation operation;

            internal SelectedEntity(Operation operation)
            {
                this.operation = operation;
            }

            public void AddComponent<T>(T component) where T : unmanaged
            {
                operation.AddComponent(component);
            }
        }
    }
}