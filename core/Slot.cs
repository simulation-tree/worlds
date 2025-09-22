using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unmanaged;

namespace Worlds
{
    /// <summary>
    /// Frequently accessed information about an entity in a <see cref="World"/>.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    public struct Slot
    {
        /// <summary>
        /// The row within the chunk that contains all of the components.
        /// </summary>
        public MemoryAddress row;

        /// <summary>
        /// The chunk that the entity in this slot belongs to.
        /// </summary>
        public Chunk chunk;

        /// <summary>
        /// The entity that is the parent of the entity in this slot.
        /// </summary>
        public uint parent;

        /// <summary>
        /// The index of the entity in this slot inside its own chunk.
        /// </summary>
        public int index;

        /// <summary>
        /// How deep the entity is in the hierarchy.
        /// </summary>
        public int depth;

        /// <summary>
        /// Amount of children the entity in this slot has.
        /// </summary>
        public int childrenCount;

        /// <summary>
        /// Retrieves the memory for <paramref name="componentType"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe readonly MemoryAddress GetComponent(int componentType)
        {
            ThrowIfComponentIsMissing(componentType);

            return new(row.pointer + chunk.chunk->schema.componentOffsets[(uint)componentType]);
        }

        /// <summary>
        /// Retrieves a reference to <typeparamref name="T"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe readonly ref T GetComponent<T>() where T : unmanaged
        {
            int componentType = chunk.chunk->schema.GetComponentType<T>();
            ThrowIfComponentIsMissing(componentType);

            return ref *(T*)(row.pointer + chunk.chunk->schema.componentOffsets[(uint)componentType]);
        }

        /// <summary>
        /// Retrieves a reference to <typeparamref name="T"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe readonly ref T GetComponent<T>(int componentType) where T : unmanaged
        {
            ThrowIfComponentIsMissing(componentType);

            return ref *(T*)(row.pointer + chunk.chunk->schema.componentOffsets[(uint)componentType]);
        }

        [Conditional("DEBUG")]
        internal unsafe readonly void ThrowIfComponentIsMissing(int componentType)
        {
            if (!chunk.ComponentTypes.Contains(componentType))
            {
                throw new InvalidOperationException($"Entity does not contain component type `{componentType}`");
            }
        }
    }
}