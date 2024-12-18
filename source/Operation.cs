﻿using Collections;
using System;
using System.Diagnostics;
using Unmanaged;

namespace Worlds
{
    /// <summary>
    /// Series of world instructions to perform all in one go.
    /// </summary>
    [DebuggerTypeProxy(typeof(OperationDebugView))]
    public unsafe struct Operation : IDisposable
    {
        private UnsafeOperation* operation;

        /// <summary>
        /// Amount of instructions stored.
        /// </summary>
        public readonly uint Count => UnsafeOperation.GetCount(operation);

        /// <summary>
        /// Checks if there are any entities selected.
        /// </summary>
        public readonly bool HasSelection
        {
            get
            {
                uint length = Count;
                for (uint i = length - 1; i != uint.MaxValue; i--)
                {
                    Instruction instruction = this[i];
                    if (instruction.type == Instruction.Type.SelectEntity)
                    {
                        return true;
                    }
                    else if (instruction.type == Instruction.Type.CreateEntity)
                    {
                        return true;
                    }
                    else if (instruction.type == Instruction.Type.ClearSelection)
                    {
                        break;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Indexer for accessing each <see cref="Instruction"/>.
        /// </summary>
        public readonly Instruction this[uint index]
        {
            get
            {
                ThrowIfOutOfRange(index);

                return UnsafeOperation.GetInstructions(operation)[index];
            }
        }

        /// <summary>
        /// Checks if this operation has been disposed.
        /// </summary>
        public readonly bool IsDisposed => operation is null;

#if NET
        /// <summary>
        /// Creates a new empty operation for writing world instructions into.
        /// </summary>
        public Operation()
        {
            operation = UnsafeOperation.Allocate(0);
        }
#endif
        /// <summary>
        /// Creates a new operation for writing world instructions into.
        /// </summary>
        public Operation(uint initialCapacity = 0)
        {
            operation = UnsafeOperation.Allocate(initialCapacity);
        }

        internal Operation(UnsafeOperation* operation)
        {
            this.operation = operation;
        }

        /// <summary>
        /// Disposes the contained instructions and the operation container itself.
        /// </summary>
        public void Dispose()
        {
            Allocations.ThrowIfNull(operation);

            Clear();
            UnsafeOperation.Free(ref operation);
        }

        /// <summary>
        /// Retrieves all instructions as a span.
        /// </summary>
        /// <returns></returns>
        public readonly USpan<Instruction> AsSpan()
        {
            Allocations.ThrowIfNull(operation);

            return UnsafeOperation.GetInstructions(operation);
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
            uint thisLength = Count;
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
        private readonly void ThrowIfOutOfRange(uint index)
        {
            if (index >= Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfPastRange(uint index)
        {
            if (index > Count)
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
            if (Count == 0)
            {
                throw new InvalidOperationException("No instructions are present");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfSelectionIsEmpty()
        {
            uint length = Count;
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
            Allocations.ThrowIfNull(operation);

            uint length = Count;
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
            Allocations.ThrowIfNull(operation);

            uint length = Count;
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
        public readonly void Clear()
        {
            Allocations.ThrowIfNull(operation);

            UnsafeOperation.ClearInstructions(operation);
        }

        /// <summary>
        /// Creates a new entity and makes it the only selected entity.
        /// </summary>
        public readonly SelectedEntity CreateEntity()
        {
            AddInstruction(Instruction.CreateEntity());
            return new(this, Count);
        }

        /// <summary>
        /// Creates a given amount of entities and makes that
        /// the current selection.
        /// </summary>
        public readonly void CreateEntities(uint count)
        {
            ThrowIfNoEntities(count);

            AddInstruction(Instruction.CreateEntity(count));
        }

        /// <summary>
        /// Destroys all entities in the selection.
        /// </summary>
        public readonly void DestroySelected()
        {
            ThrowIfSelectionIsEmpty();

            AddInstruction(Instruction.DestroySelection());
        }

        /// <summary>
        /// Destroys a range of selected entities, in the order they were added.
        /// </summary>
        public readonly void DestroySelected(uint start, uint length)
        {
            ThrowIfSelectionIsEmpty();

            AddInstruction(Instruction.DestroySelection(start, length));
        }

        /// <summary>
        /// Appends the given entity to the selection.
        /// </summary>
        public readonly SelectedEntity SelectEntity(uint entity)
        {
            AddInstruction(Instruction.SelectEntity(entity));
            return new(this, Count);
        }

        /// <summary>
        /// Appends the given entity to the selection.
        /// </summary>
        public readonly SelectedEntity SelectEntity<T>(T entity) where T : unmanaged, IEntity
        {
            return SelectEntity(entity.GetEntityValue());
        }

        /// <summary>
        /// Appends the entity that was created <paramref name="offset"/> instructions ago
        /// to the selections.
        /// <para>Where 0 is the last created entity.</para>
        /// </summary>
        public readonly void SelectPreviouslyCreatedEntity(uint offset)
        {
            AddInstruction(Instruction.SelectPreviouslyCreatedEntity(offset));
        }

        /// <summary>
        /// Resets the entity selection.
        /// </summary>
        public readonly void ClearSelection()
        {
            AddInstruction(Instruction.ClearSelection());
        }

        /// <summary>
        /// Assigns the given parent of all selected entities to the
        /// given <paramref name="parent"/>.
        /// </summary>
        public readonly void SetParent(uint parent)
        {
            ThrowIfSelectionIsEmpty();

            AddInstruction(Instruction.SetParent(parent));
        }

        /// <summary>
        /// Assigns the parent of every selected entity to the given <paramref name="parent"/>.
        /// </summary>
        public readonly void SetParent<T>(T parent) where T : unmanaged, IEntity
        {
            SetParent(parent.GetEntityValue());
        }

        /// <summary>
        /// Assigns the parent of every selected entitiy to the one that was
        /// created <paramref name="offset"/> instructions ago.
        /// <para>Where 0 is the last created entity.</para>
        /// </summary>
        public readonly void SetParentToPreviouslyCreatedEntity(uint offset)
        {
            ThrowIfSelectionIsEmpty();

            AddInstruction(Instruction.SetParentToPreviouslyCreatedEntity(offset));
        }

        /// <summary>
        /// Adds a reference for every entity in the selection, to
        /// this specific given entity.
        /// </summary>
        public readonly void AddReference(uint entity)
        {
            ThrowIfSelectionIsEmpty();

            AddInstruction(Instruction.AddReference(entity));
        }

        /// <summary>
        /// Adds a reference for every entity in the selection, to the 
        /// entity that was created <paramref name="offset"/> instructions ago.
        /// <para>Where 0 is the last created entity.</para>
        /// </summary>
        public readonly void AddReferenceTowardsPreviouslyCreatedEntity(uint offset)
        {
            ThrowIfSelectionIsEmpty();

            AddInstruction(Instruction.AddReferenceTowardsPreviouslyCreatedEntity(offset));
        }

        /// <summary>
        /// Removes the given reference value from all selected entities.
        /// </summary>
        public readonly void RemoveReference(rint reference)
        {
            ThrowIfSelectionIsEmpty();

            AddInstruction(Instruction.RemoveReference(reference));
        }

        /// <summary>
        /// Adds a new component entry to every entity in the selection.
        /// </summary>
        public readonly void AddComponent<T>(T component) where T : unmanaged
        {
            ThrowIfSelectionIsEmpty();

            AddInstruction(Instruction.AddComponent(component));
        }

        public readonly void AddComponent<T>() where T : unmanaged
        {
            ThrowIfSelectionIsEmpty();

            AddInstruction(Instruction.AddComponent(new T()));
        }

        /// <summary>
        /// Assigns the given component value onto every selected entities,
        /// assuming they already contain the component entry.
        /// </summary>
        public readonly void SetComponent<T>(T component) where T : unmanaged
        {
            ThrowIfSelectionIsEmpty();

            AddInstruction(Instruction.SetComponent(component));
        }

        /// <summary>
        /// Removes the given component type from all selected entities.
        /// </summary>
        public readonly void RemoveComponent<T>() where T : unmanaged
        {
            ThrowIfSelectionIsEmpty();

            AddInstruction(Instruction.RemoveComponent<T>());
        }

        /// <summary>
        /// Creates a new array for every selected entities.
        /// </summary>
        public readonly void CreateArray<T>(uint length) where T : unmanaged
        {
            ThrowIfSelectionIsEmpty();

            AddInstruction(Instruction.CreateArray<T>(length));
        }

        /// <summary>
        /// Creates a new array for every selected entities containing the given <paramref name="values"/>.
        /// </summary>
        public readonly void CreateArray<T>(USpan<T> values) where T : unmanaged
        {
            ThrowIfSelectionIsEmpty();

            AddInstruction(Instruction.CreateArray(values));
        }

        /// <summary>
        /// Destroys an existing array on selected entities.
        /// </summary>
        public readonly void DestroyArray<T>() where T : unmanaged
        {
            ThrowIfSelectionIsEmpty();

            AddInstruction(Instruction.DestroyArray<T>());
        }

        /// <summary>
        /// Writes the given value into the array at the given index.
        /// </summary>
        public readonly void SetArrayElement<T>(uint index, T element) where T : unmanaged
        {
            ThrowIfSelectionIsEmpty();

            AddInstruction(Instruction.SetArrayElement(index, element));
        }

        /// <summary>
        /// Writes the given span into the array starting from the given index.
        /// </summary>
        public readonly void SetArrayElements<T>(uint index, USpan<T> elements) where T : unmanaged
        {
            ThrowIfSelectionIsEmpty();

            AddInstruction(Instruction.SetArrayElement(index, elements));
        }

        /// <summary>
        /// Resizes the array for every selected entity.
        /// </summary>
        public readonly void ResizeArray<T>(uint newLength) where T : unmanaged
        {
            ThrowIfSelectionIsEmpty();

            AddInstruction(Instruction.ResizeArray<T>(newLength));
        }

        /// <summary>
        /// Adds a new given <paramref name="instruction"/> to the operation.
        /// </summary>
        public readonly void AddInstruction(Instruction instruction)
        {
            Allocations.ThrowIfNull(operation);

            UnsafeOperation.AddInstruction(operation, instruction);
        }

        public readonly void InsertInstructionAt(Instruction instruction, uint index)
        {
            Allocations.ThrowIfNull(operation);
            ThrowIfPastRange(index);

            UnsafeOperation.InsertInstructionAt(operation, instruction, index);
        }

        /// <summary>
        /// Removes the instruction at the given <paramref name="index"/>.
        /// </summary>
        public readonly void RemoveInstructionAt(uint index)
        {
            Allocations.ThrowIfNull(operation);
            ThrowIfOutOfRange(index);
            ThrowIfNoInstructions();

            UnsafeOperation.RemoveInstructionAt(operation, index);
        }

        /// <summary>
        /// Creates a new empty operation for writing world instructions into.
        /// </summary>
        public static Operation Create(uint initialCapacity = 1)
        {
            return new(initialCapacity);
        }

        /// <summary>
        /// An entity local to the operation.
        /// </summary>
        public ref struct SelectedEntity
        {
            private readonly Operation operation;
            private uint index;

            internal SelectedEntity(Operation operation, uint index)
            {
                this.operation = operation;
                this.index = index;
            }

            /// <summary>
            /// Submits an instruction to add the given <paramref name="component"/> to this entity.
            /// </summary>
            public void AddComponent<T>(T component) where T : unmanaged
            {
                operation.InsertInstructionAt(Instruction.AddComponent(component), index);
                index++;
            }

            public void SetComponent<T>(T component) where T : unmanaged
            {
                operation.InsertInstructionAt(Instruction.SetComponent(component), index);
                index++;
            }

            public void RemoveComponent<T>() where T : unmanaged
            {
                operation.InsertInstructionAt(Instruction.RemoveComponent<T>(), index);
                index++;
            }

            public void CreateArray<T>(uint length) where T : unmanaged
            {
                operation.InsertInstructionAt(Instruction.CreateArray<T>(length), index);
                index++;
            }

            public void CreateArray<T>(USpan<T> values) where T : unmanaged
            {
                operation.InsertInstructionAt(Instruction.CreateArray(values), index);
                index++;
            }

            public void DestroyArray<T>() where T : unmanaged
            {
                operation.InsertInstructionAt(Instruction.DestroyArray<T>(), index);
                index++;
            }

            public void ResizeArray<T>(uint newLength) where T : unmanaged
            {
                operation.InsertInstructionAt(Instruction.ResizeArray<T>(newLength), index);
                index++;
            }

            public void SetArrayElement<T>(uint index, T element) where T : unmanaged
            {
                operation.InsertInstructionAt(Instruction.SetArrayElement(index, element), this.index);
                this.index++;
            }

            public void SetArrayElements<T>(uint index, USpan<T> elements) where T : unmanaged
            {
                operation.InsertInstructionAt(Instruction.SetArrayElement(index, elements), this.index);
                this.index++;
            }

            public void AddReferenceTowardsPreviouslyCreatedEntity(uint offset)
            {
                operation.InsertInstructionAt(Instruction.AddReferenceTowardsPreviouslyCreatedEntity(offset), index);
                index++;
            }
        }

        /// <summary>
        /// Opaque pointer implementation of an <see cref="Operation"/>.
        /// </summary>
        internal unsafe struct UnsafeOperation
        {
            private uint count;
            private uint capacity;
            private Allocation list;

            public static UnsafeOperation* Allocate(uint initialCapacity)
            {
                initialCapacity = Allocations.GetNextPowerOf2(Math.Max(1, initialCapacity));
                UnsafeOperation* operation = (UnsafeOperation*)Allocations.Allocate(TypeInfo<UnsafeOperation>.size);
                operation->count = 0;
                operation->capacity = initialCapacity;
                operation->list = new(TypeInfo<Instruction>.size * initialCapacity);
                return operation;
            }

            public static void Free(ref UnsafeOperation* operation)
            {
                Allocations.ThrowIfNull(operation);

                operation->list.Dispose();
                Allocations.Free(ref operation);
            }

            public static uint GetCount(UnsafeOperation* operation)
            {
                Allocations.ThrowIfNull(operation);

                return operation->count;
            }

            public static uint GetCapacity(UnsafeOperation* operation)
            {
                Allocations.ThrowIfNull(operation);

                return operation->capacity;
            }

            public static USpan<Instruction> GetInstructions(UnsafeOperation* operation)
            {
                Allocations.ThrowIfNull(operation);

                return operation->list.AsSpan<Instruction>(0, operation->count);
            }

            public static void AddInstruction(UnsafeOperation* operation, Instruction instruction)
            {
                Allocations.ThrowIfNull(operation);

                uint stride = TypeInfo<Instruction>.size;
                ref uint count = ref operation->count;
                uint capacity = operation->capacity;
                while (count >= capacity)
                {
                    capacity *= 2;
                    Allocation.Resize(ref operation->list, stride * capacity);
                    operation->capacity = capacity;
                }

                operation->list.Write(stride * count, instruction);
                count++;
            }

            public static void InsertInstructionAt(UnsafeOperation* operation, Instruction instruction, uint index)
            {
                Allocations.ThrowIfNull(operation);

                uint stride = TypeInfo<Instruction>.size;
                ref uint count = ref operation->count;
                uint capacity = operation->capacity;
                while (count >= capacity)
                {
                    capacity *= 2;
                    Allocation.Resize(ref operation->list, stride * capacity);
                    operation->capacity = capacity;
                }

                if (index == operation->count)
                {
                    operation->list.Write(stride * index, instruction);
                }
                else
                {
                    operation->list.CopyTo(operation->list, stride * index, stride * (index + 1), stride * (count - index));
                    operation->list.Write(stride * index, instruction);
                }

                count++;
            }

            public static void ClearInstructions(UnsafeOperation* operation)
            {
                Allocations.ThrowIfNull(operation);

                uint stride = TypeInfo<Instruction>.size;
                ref uint count = ref operation->count;
                for (uint i = 0; i < count; i++)
                {
                    ref Instruction instruction = ref operation->list.Read<Instruction>(stride * i);
                    instruction.Dispose();
                }

                count = 0;
            }

            public static void RemoveInstructionAt(UnsafeOperation* operation, uint index)
            {
                Allocations.ThrowIfNull(operation);

                uint stride = TypeInfo<Instruction>.size;
                ref Instruction instruction = ref operation->list.Read<Instruction>(stride * index);
                instruction.Dispose();

                ref uint count = ref operation->count;
                if (index == operation->capacity - 1)
                {
                    //removing last element
                }
                else
                {
                    //shift elements back
                    operation->list.CopyTo(operation->list, stride * (index + 1), stride * index, stride * (count - index - 1));
                }

                count--;
            }
        }

        internal class OperationDebugView
        {
            public readonly uint[] selected;
            public readonly Instruction[] instructions;

            public OperationDebugView(Operation operation)
            {
                using List<uint> selection = new();
                operation.ReadSelection(selection);

                selected = new uint[selection.Count];
                for (int i = 0; i < selection.Count; i++)
                {
                    selected[i] = selection[(uint)i];
                }

                instructions = operation.AsSpan().ToArray();
            }
        }
    }
}