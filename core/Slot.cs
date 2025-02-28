using Collections.Generic;
using System;

namespace Worlds
{
    /// <summary>
    /// Describes an entity slot in a <see cref="World"/>.
    /// </summary>
    public struct Slot
    {
        public uint parent;
        public State state;
        public Flags flags;
        public Chunk chunk;
        public List<uint> children;
        public List<uint> references;
        public Array<Values> arrays;

        public readonly Definition Definition => chunk.Definition;
        public readonly bool ContainsArrays => (flags & Flags.ContainsArrays) != 0;
        public readonly bool ContainsChildren => (flags & Flags.ContainsChildren) != 0;
        public readonly bool ContainsReferences => (flags & Flags.ContainsReferences) != 0;
        public readonly bool ArraysOutdated => (flags & Flags.ArraysOutdated) != 0;
        public readonly bool ChildrenOutdated => (flags & Flags.ChildrenOutdated) != 0;
        public readonly bool ReferencesOutdated => (flags & Flags.ReferencesOutdated) != 0;

        public enum State : byte
        {
            Unknown,
            Free,
            Enabled,
            Disabled,
            DisabledButLocallyEnabled
        }

        [Flags]
        public enum Flags : byte
        {
            None = 0,
            ContainsArrays = 1,
            ContainsChildren = 2,
            ContainsReferences = 4,
            ArraysOutdated = 8,
            ChildrenOutdated = 16,
            ReferencesOutdated = 32,
            Outdated = ArraysOutdated | ChildrenOutdated | ReferencesOutdated
        }
    }
}