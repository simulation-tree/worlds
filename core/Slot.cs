using Collections.Generic;
using System;

namespace Worlds
{
    /// <summary>
    /// Describes an entity slot in a <see cref="World"/>.
    /// </summary>
    internal struct Slot
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

        public int referenceStart;
        public int referenceCount;

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
        /// Checks if this slot has outdated arrays.
        /// </summary>
        public readonly bool ArraysOutdated => (flags & Flags.ArraysOutdated) != 0;

        /// <summary>
        /// Checks if this slot has outdated children.
        /// </summary>
        public readonly bool ChildrenOutdated => (flags & Flags.ChildrenOutdated) != 0;

        public Slot()
        {
            parent = 0;
            state = State.Free;
            flags = Flags.None;
            chunk = default;
            index = default;
            childrenCount = default;
            referenceStart = default;
            referenceCount = default;
            arrays = default;
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