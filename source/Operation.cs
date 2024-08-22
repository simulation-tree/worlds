﻿using System;
using System.Diagnostics;
using Unmanaged;
using Unmanaged.Collections;

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
        public readonly uint Length => list.Read<uint>(sizeof(uint) * 0);

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
            uint size = (uint)(sizeof(uint) + sizeof(uint) + (sizeof(Instruction) * initialCapacity));
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
            uint length = Length;
            for (uint i = 0; i < length; i++)
            {
                this[i].Dispose();
            }

            list.Dispose();
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
        private readonly void ThrowIfEmpty<T>(ReadOnlySpan<T> span)
        {
            if (span.IsEmpty)
            {
                throw new ArgumentException("Span is empty.", nameof(span));
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

        /// <summary>
        /// Creates a new entity and automatically appends it to the selection.
        /// </summary>
        public void CreateEntity()
        {
            AddInstruction(Instruction.CreateEntity());
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
        public void SelectEntity(eint entity)
        {
            AddInstruction(Instruction.SelectEntity(entity));
        }

        /// <summary>
        /// Appends the entity that was created <paramref name="offset"/> instructions ago
        /// to the selections.
        /// <para>Where 0 is the last created entity.</para>
        /// </summary>
        public void SelectEntity(uint offset)
        {
            AddInstruction(Instruction.SelectEntity(offset));
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
        public void SetParent(eint parent)
        {
            ThrowIfSelectionIsEmpty();
            AddInstruction(Instruction.SetParent(parent));
        }

        /// <summary>
        /// Assigns the parent of every selected entitiy to the one that was
        /// created <paramref name="offset"/> instructions ago.
        /// <para>Where 0 is the last created entity.</para>
        /// </summary>
        public void SetParent(uint offset)
        {
            ThrowIfSelectionIsEmpty();
            AddInstruction(Instruction.SetParent(offset));
        }

        /// <summary>
        /// Adds a reference for every entity in the selection, to
        /// this specific given entity.
        /// </summary>
        public void AddReference(eint entity)
        {
            ThrowIfSelectionIsEmpty();
            AddInstruction(Instruction.AddReference(entity));
        }

        /// <summary>
        /// Adds a reference for every entity in the selection, to the 
        /// entity that was created <paramref name="offset"/> instructions ago.
        /// <para>Where 0 is the last created entity.</para>
        /// </summary>
        public void AddReference(uint offset)
        {
            ThrowIfSelectionIsEmpty();
            AddInstruction(Instruction.AddReference(offset));
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
        /// Creates a new list on every selected entities, with an optional
        /// set initial length (not capacity) for later assignment.
        /// </summary>
        public void CreateList<T>(uint initialLength = 0) where T : unmanaged
        {
            ThrowIfSelectionIsEmpty();
            AddInstruction(Instruction.CreateList<T>(initialLength));
        }

        /// <summary>
        /// Clears all lists of the given type from every selected entities.
        /// </summary>
        public void ClearList<T>() where T : unmanaged
        {
            ThrowIfSelectionIsEmpty();
            AddInstruction(Instruction.ClearList<T>());
        }

        /// <summary>
        /// Destroys the list of the given type from all entities in
        /// the selection.
        /// </summary>
        public void DestroyList<T>() where T : unmanaged
        {
            ThrowIfSelectionIsEmpty();
            AddInstruction(Instruction.DestroyList<T>());
        }

        /// <summary>
        /// Appends the given element to all lists of the given type on every selected entities.
        /// </summary>
        public void AppendToList<T>(T element) where T : unmanaged
        {
            ThrowIfSelectionIsEmpty();
            AddInstruction(Instruction.AppendToList(element));
        }

        /// <summary>
        /// Appends the given span of elements to all lists of the given type on every selected entities.
        /// </summary>
        public void AppendToList<T>(ReadOnlySpan<T> elements) where T : unmanaged
        {
            ThrowIfSelectionIsEmpty();
            ThrowIfEmpty(elements);
            AddInstruction(Instruction.AppendToList(elements));
        }

        public void AppendToList<T>(UnmanagedArray<T> array) where T : unmanaged
        {
            AppendToList<T>(array.AsSpan());
        }

        public void AppendToList<T>(UnmanagedList<T> list) where T : unmanaged
        {
            AppendToList<T>(list.AsSpan());
        }

        /// <summary>
        /// Appends the given span of elements to all lists of the given type on all selected entities.
        /// </summary>
        public unsafe void AppendToList<T>(void* pointer, uint length) where T : unmanaged
        {
            ThrowIfSelectionIsEmpty();
            Span<T> span = new(pointer, (int)length);
            ThrowIfEmpty<T>(span);
            AddInstruction(Instruction.AppendToList<T>(span));
        }

        public void SetListElement<T>(uint index, T element) where T : unmanaged
        {
            ThrowIfSelectionIsEmpty();
            AddInstruction(Instruction.ModifyElement<T>(index, element));
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
    }
}