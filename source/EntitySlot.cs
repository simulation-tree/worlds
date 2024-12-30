﻿using Collections;
using Unmanaged;

namespace Worlds
{
    /// <summary>
    /// Describes a slot where an entity may or may not be stored.
    /// </summary>
    public struct EntitySlot
    {
        /// <summary>
        /// Entity ID.
        /// </summary>
        public uint entity;

        /// <summary>
        /// ID of the parent entity.
        /// </summary>
        public uint parent;

        /// <summary>
        /// The chunk that this entity slot belongs to.
        /// </summary>
        public ComponentChunk chunk;

        /// <summary>
        /// Number of children.
        /// </summary>
        public ushort childCount;

        /// <summary>
        /// Children of the entity.
        /// </summary>
        public List<uint> children;

        /// <summary>
        /// Number of references.
        /// </summary>
        public ushort referenceCount;

        /// <summary>
        /// References to other entities.
        /// </summary>
        public List<uint> references;

        /// <summary>
        /// All arrays stored.
        /// </summary>
        public Array<Allocation> arrays;

        /// <summary>
        /// Lengths of arrays.
        /// </summary>
        public Array<uint> arrayLengths;

        /// <summary>
        /// State of the entity.
        /// </summary>
        public State state;

        /// <summary>
        /// Initializes a new instance of the <see cref="EntitySlot"/> struct.
        /// </summary>
        public EntitySlot(uint entity)
        {
            this.entity = entity;
        }

        /// <summary>
        /// Enabled state of an entity.
        /// </summary>
        public enum State : byte
        {
            /// <summary>
            /// Entity is enabled.
            /// </summary>
            Enabled,

            /// <summary>
            /// Entity is disabled.
            /// </summary>
            Disabled,

            /// <summary>
            /// Entity is destroyed.
            /// </summary>
            Destroyed,

            /// <summary>
            /// Entity is enabled on its own, but disabled because of an ancestor.
            /// </summary>
            EnabledButDisabledDueToAncestor
        }
    }
}