using Collections.Generic;
using System;

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
        /// The index of the entity in this slot within the <see cref="chunk"/>.
        /// </summary>
        public int index;

        /// <summary>
        /// Amount of children the entity in this slot has.
        /// </summary>
        public int childrenCount;

        /// <summary>
        /// The other entities that are referenced.
        /// </summary>
        public List<uint> references;

        /// <summary>
        /// All arrays stored in the entity.
        /// </summary>
        public Array<Values> arrays;

        /// <summary>
        /// The definition of the <see cref="chunk"/>.
        /// </summary>
        public readonly Definition Definition => chunk.Definition;

        /// <summary>
        /// Checks if this slot contains arrays.
        /// </summary>
        public readonly bool ContainsArrays => (flags & Flags.ContainsArrays) != 0;

        /// <summary>
        /// Checks if this slot contains children.
        /// </summary>
        public readonly bool ContainsChildren => (flags & Flags.ContainsChildren) != 0;

        /// <summary>
        /// Checks if this slot contains references to other entities.
        /// </summary>
        public readonly bool ContainsReferences => (flags & Flags.ContainsReferences) != 0;

        /// <summary>
        /// Checks if this slot has outdated arrays.
        /// </summary>
        public readonly bool ArraysOutdated => (flags & Flags.ArraysOutdated) != 0;

        /// <summary>
        /// Checks if this slot has outdated children.
        /// </summary>
        public readonly bool ChildrenOutdated => (flags & Flags.ChildrenOutdated) != 0;

        /// <summary>
        /// Checks if this slot has outdated references to other entities.
        /// </summary>
        public readonly bool ReferencesOutdated => (flags & Flags.ReferencesOutdated) != 0;

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
            /// Entity contains references to other entities.
            /// </summary>
            ContainsReferences = 4,

            /// <summary>
            /// The arrays on this entity are outdated.
            /// </summary>
            ArraysOutdated = 8,

            /// <summary>
            /// The children on this entity are outdated.
            /// </summary>
            ChildrenOutdated = 16,

            /// <summary>
            /// The references to other entities are outdated.
            /// </summary>
            ReferencesOutdated = 32,

            /// <summary>
            /// The entity is outdated and needs to be refreshed back to initial state.
            /// </summary>
            Outdated = ArraysOutdated | ChildrenOutdated | ReferencesOutdated
        }
    }
}