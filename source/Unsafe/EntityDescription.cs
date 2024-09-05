﻿using Unmanaged;
using Unmanaged.Collections;

namespace Simulation.Unsafe
{
    public struct EntityDescription
    {
        public uint entity;
        public uint parent;
        public int componentsKey;
        public UnmanagedList<Allocation> arrays;
        public UnmanagedList<RuntimeType> arrayTypes;
        public UnmanagedList<uint> arrayLengths;
        public UnmanagedList<uint> children;
        public UnmanagedList<uint> references;
        public State state;

        public readonly bool IsEnabled => state == State.Enabled;
        public readonly bool IsSelfEnabled => state == State.Enabled || state == State.AncestorDisabledEnabled;
        public readonly bool IsDestroyed => state == State.Destroyed;

        public void SetEnabledState(bool value)
        {
            state = value ? State.Enabled : State.Disabled;
        }

        public enum State : byte
        {
            Enabled,
            Disabled,
            AncestorDisabledEnabled,
            AncestorDisabledDisabled,
            Destroyed
        }
    }
}