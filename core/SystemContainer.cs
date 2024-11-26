using Collections;
using Simulation.Functions;
using System;
using System.Diagnostics;
using Unmanaged;

namespace Simulation
{
    /// <summary>
    /// Contains a system added to a <see cref="Simulation.Simulator"/>.
    /// </summary>
    public unsafe struct SystemContainer : IDisposable
    {
        /// <summary>
        /// The <see cref="RuntimeTypeHandle"/> of this system.
        /// </summary>
        public readonly nint systemType;

        /// <summary>
        /// Native memory containing the system's data.
        /// </summary>
        public readonly Allocation allocation;

        private readonly Dictionary<nint, HandleFunction> handlers;
        private readonly List<World> programWorlds;
        private readonly UnsafeSimulator* simulator;
        private readonly InitializeFunction initialize;
        private readonly IterateFunction update;
        private readonly FinalizeFunction finalize;

        /// <summary>
        /// Reference to the <see cref="Simulation.Simulator"/> that this system was created in.
        /// </summary>
        public readonly Simulator Simulator => new((nint)simulator);

        /// <summary>
        /// The world that this system was created in.
        /// </summary>
        public readonly World World => UnsafeSimulator.GetWorld(simulator);

        /// <summary>
        /// The <see cref="Type"/> of this system.
        /// </summary>
        public readonly Type Type
        {
            get
            {
                RuntimeTypeHandle handle = RuntimeTypeHandle.FromIntPtr(systemType);
                return Type.GetTypeFromHandle(handle) ?? throw new();
            }
        }

        /// <summary>
        /// Creates a new <see cref="SystemContainer"/> instance.
        /// </summary>
        public SystemContainer(UnsafeSimulator* simulator, Allocation system, nint systemType, Dictionary<nint, HandleFunction> handlers, InitializeFunction initialize, IterateFunction update, FinalizeFunction finalize)
        {
            this.simulator = simulator;
            this.allocation = system;
            this.systemType = systemType;
            this.handlers = handlers;
            programWorlds = new();
            this.initialize = initialize;
            this.update = update;
            this.finalize = finalize;
        }

        /// <summary>
        /// Builds a string representation of the system.
        /// </summary>
        public readonly uint ToString(USpan<char> buffer)
        {
            string name = Type.Name;
            for (uint i = 0; i < name.Length; i++)
            {
                buffer[i] = name[(int)i];
            }

            return (uint)name.Length;
        }

        /// <inheritdoc/>
        public readonly override string ToString()
        {
            USpan<char> buffer = stackalloc char[256];
            uint length = ToString(buffer);
            return buffer.Slice(0, length).ToString();
        }

        /// <summary>
        /// Finalizes the system and disposes of its resources.
        /// </summary>
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

        /// <summary>
        /// Reads the system data of the given type.
        /// </summary>
        public readonly ref T Read<T>() where T : unmanaged, ISystem
        {
            return ref allocation.Read<T>();
        }

        /// <summary>
        /// Checks if this system is initialized with the given <paramref name="programWorld"/>.
        /// </summary>
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

        /// <summary>
        /// Attempts to handle the given message with the given type.
        /// </summary>
        public readonly bool TryHandleMessage(World programWorld, nint messageType, Allocation message)
        {
            if (handlers.TryGetValue(messageType, out HandleFunction handler))
            {
                handler.Invoke(this, programWorld, message);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Attempts to handle the given message with the given type.
        /// </summary>
        public readonly bool TryHandleMessage<T>(World programWorld, Allocation message) where T : unmanaged
        {
            nint messageType = RuntimeTypeHandle.ToIntPtr(typeof(T).TypeHandle);
            return TryHandleMessage(programWorld, messageType, message);
        }

        /// <summary>
        /// Throws an <see cref="InvalidOperationException"/> if this system is not initialized with the given <paramref name="programWorld"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        [Conditional("DEBUG")]
        public readonly void ThrowIfNotInitializedWith(World programWorld)
        {
            if (!IsInitializedWith(programWorld))
            {
                throw new InvalidOperationException($"System `{this}` is not initialized with world `{programWorld}`");
            }
        }
    }

    /// <summary>
    /// Generic container for of a <typeparamref name="T"/> system added to a <see cref="Simulation.Simulator"/>.
    /// </summary>
    public unsafe readonly struct SystemContainer<T> where T : unmanaged, ISystem
    {
        private readonly UnsafeSimulator* simulator;
        private readonly uint index;

        /// <summary>
        /// The system's data.
        /// </summary>
        public readonly ref T Value => ref Container.allocation.Read<T>();

        /// <summary>
        /// The <see cref="Simulation.Simulator"/> that this system was created in.
        /// </summary>
        public readonly Simulator Simulator => new((nint)simulator);

        /// <summary>
        /// The world that this system was created in.
        /// </summary>
        public readonly World World => UnsafeSimulator.GetWorld(simulator);

        private unsafe readonly ref SystemContainer Container => ref UnsafeSimulator.GetSystems(simulator)[index];

        /// <summary>
        /// Initializes a new <see cref="SystemContainer{T}"/> instance with an
        /// existing system index.
        /// </summary>
        public SystemContainer(UnsafeSimulator* simulator, uint index)
        {
            this.simulator = simulator;
            this.index = index;
        }

        /// <inheritdoc/>
        public static implicit operator SystemContainer(SystemContainer<T> container)
        {
            return container.Container;
        }
    }
}
