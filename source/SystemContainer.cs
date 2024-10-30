using Collections;
using Simulation.Functions;
using System;
using System.Diagnostics;
using Unmanaged;

namespace Simulation
{
    public unsafe struct SystemContainer : IDisposable
    {
        public readonly RuntimeType type;
        public readonly Allocation allocation;

        private readonly Dictionary<RuntimeType, HandleFunction> handlers;
        private readonly List<World> programWorlds;
        private readonly UnsafeSimulator* simulator;
        private readonly InitializeFunction initialize;
        private readonly IterateFunction update;
        private readonly FinalizeFunction finalize;

        public readonly Simulator Simulator => new((nint)simulator);

        /// <summary>
        /// The world that this system was created in.
        /// </summary>
        public readonly World World => UnsafeSimulator.GetWorld(simulator);

        public SystemContainer(UnsafeSimulator* simulator, Allocation system, RuntimeType type, Dictionary<RuntimeType, HandleFunction> handlers, InitializeFunction initialize, IterateFunction update, FinalizeFunction finalize)
        {
            this.simulator = simulator;
            this.allocation = system;
            this.type = type;
            this.handlers = handlers;
            programWorlds = new();
            this.initialize = initialize;
            this.update = update;
            this.finalize = finalize;
        }

        public readonly uint ToString(USpan<char> buffer)
        {
            return type.ToString(buffer);
        }

        public readonly override string ToString()
        {
            USpan<char> buffer = stackalloc char[256];
            uint length = ToString(buffer);
            return buffer.Slice(0, length).ToString();
        }

        public readonly void Dispose()
        {
            for (uint i = programWorlds.Count - 1; i != uint.MaxValue; i--)
            {
                Finalize(programWorlds[i]);
            }

            allocation.Dispose();
            programWorlds.Dispose();
            handlers.Dispose();
        }

        public readonly ref T Read<T>() where T : unmanaged, ISystem
        {
            return ref allocation.Read<T>();
        }

        public readonly bool IsInitializedWith(World programWorld)
        {
            return programWorlds.Contains(programWorld);
        }

        /// <summary>
        /// Initializes this system with the given world as its context.
        /// </summary>
        public readonly void Initialize(World programWorld)
        {
            initialize.Invoke(this, programWorld);
            programWorlds.Add(programWorld);
        }

        /// <summary>
        /// Updates this system with the given world as its context.
        /// </summary>
        public readonly void Update(World programWorld, TimeSpan delta)
        {
            ThrowIfNotInitializedWith(programWorld);
            update.Invoke(this, programWorld, delta);
        }

        /// <summary>
        /// Finalizes this system with the given world as its context.
        /// </summary>
        public readonly void Finalize(World programWorld)
        {
            ThrowIfNotInitializedWith(programWorld);
            finalize.Invoke(this, programWorld);
        }

        public readonly bool TryHandleMessage(World programWorld, RuntimeType type, Allocation message)
        {
            if (handlers.TryGetValue(type, out HandleFunction handler))
            {
                handler.Invoke(this, programWorld, message);
                return true;
            }

            return false;
        }

        [Conditional("DEBUG")]
        public readonly void ThrowIfNotInitializedWith(World programWorld)
        {
            if (!IsInitializedWith(programWorld))
            {
                throw new InvalidOperationException($"System {this} is not initialized with world {programWorld}");
            }
        }
    }

    public unsafe readonly struct SystemContainer<T> where T : unmanaged, ISystem
    {
        private readonly UnsafeSimulator* simulator;
        private readonly uint index;

        public readonly ref T Value => ref Container.allocation.Read<T>();
        public readonly Simulator Simulator => new((nint)simulator);
        public readonly World World => UnsafeSimulator.GetWorld(simulator);

        private unsafe readonly ref SystemContainer Container => ref UnsafeSimulator.GetSystems(simulator)[index];

        public SystemContainer(UnsafeSimulator* simulator, uint index)
        {
            this.simulator = simulator;
            this.index = index;
        }

        public static implicit operator SystemContainer(SystemContainer<T> container)
        {
            return container.Container;
        }
    }
}
