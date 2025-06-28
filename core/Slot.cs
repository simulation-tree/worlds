using System;
using System.Diagnostics;
using Unmanaged;

namespace Worlds
{
    /// <summary>
    /// Describes an entity slot in a <see cref="World"/>.
    /// </summary>
    public struct Slot
    {
        /// <summary>
        /// The entity that is the parent of the entity in this slot.
        /// </summary>
        public uint parent;

        /// <summary>
        /// How deep the entity is in the hierarchy.
        /// </summary>
        public int depth;

        /// <summary>
        /// The state of this entity.
        /// </summary>
        public State state;

        /// <summary>
        /// Flags describing the contents of this slot.
        /// </summary>
        public Flags flags;

        /// <summary>
        /// The chunk that the entity in this slot belongs to.
        /// </summary>
        public Chunk chunk;

        /// <summary>
        /// The index of the entity in this slot inside its own chunk.
        /// </summary>
        public int index;

        /// <summary>
        /// The row within the chunk that contains all of the components.
        /// </summary>
        public MemoryAddress row;

        /// <summary>
        /// Amount of children the entity in this slot has.
        /// </summary>
        public int childrenCount;

        /// <summary>
        /// Where references start.
        /// </summary>
        public int referenceStart;

        /// <summary>
        /// Length of references.
        /// </summary>
        public int referenceCount;

        /// <summary>
        /// Retrieves the memory for <paramref name="componentType"/>.
        /// </summary>
        public unsafe readonly MemoryAddress GetComponent(int componentType)
        {
            ThrowIfComponentIsMissing(componentType);

            return new(row.Pointer + chunk.chunk->schema.schema->componentOffsets[(uint)componentType]);
        }

        /// <summary>
        /// Retrieves a reference to <typeparamref name="T"/>.
        /// </summary>
        public unsafe readonly ref T GetComponent<T>() where T : unmanaged
        {
            int componentType = chunk.chunk->schema.GetComponentType<T>();
            ThrowIfComponentIsMissing(componentType);

            return ref *(T*)(row.Pointer + chunk.chunk->schema.schema->componentOffsets[(uint)componentType]);
        }

        /// <summary>
        /// Retrieves a reference to <typeparamref name="T"/>.
        /// </summary>
        public unsafe readonly ref T GetComponent<T>(int componentType) where T : unmanaged
        {
            ThrowIfComponentIsMissing(componentType);

            return ref *(T*)(row.Pointer + chunk.chunk->schema.schema->componentOffsets[(uint)componentType]);
        }

        [Conditional("DEBUG")]
        internal unsafe readonly void ThrowIfComponentIsMissing(int componentType)
        {
            if (!chunk.chunk->definition.componentTypes.Contains(componentType))
            {
                throw new InvalidOperationException($"Entity does not contain component type `{componentType}`");
            }
        }

        /// <summary>
        /// All possible states of an entity.
        /// </summary>
        public enum State : byte
        {
            /// <summary>
            /// Uninitialized.
            /// </summary>
            Unknown,

            /// <summary>
            /// The slot doesn't describe a valid entity.
            /// </summary>
            Free,

            /// <summary>
            /// The entity is enabled.
            /// </summary>
            Enabled,

            /// <summary>
            /// The entity is disabled.
            /// </summary>
            Disabled,

            /// <summary>
            /// The entity is enabled on its own, but disabled due to ancestors.
            /// </summary>
            DisabledButLocallyEnabled
        }

        /// <summary>
        /// Different properties that an entity can have.
        /// </summary>
        [Flags]
        public enum Flags : byte
        {
            /// <summary>
            /// No settings.
            /// </summary>
            None = 0,

            /// <summary>
            /// Entity contains arrays.
            /// </summary>
            ContainsArrays = 1,

            /// <summary>
            /// Entity contains children.
            /// </summary>
            ContainsChildren = 2,

            /// <summary>
            /// The arrays on this entity are outdated.
            /// </summary>
            ArraysOutdated = 8,

            /// <summary>
            /// The children on this entity are outdated.
            /// </summary>
            ChildrenOutdated = 16,

            /// <summary>
            /// The entity is outdated and needs to be refreshed back to initial state.
            /// </summary>
            Outdated = ArraysOutdated | ChildrenOutdated
        }
    }
}