using Collections;
using Programs;
using Programs.Components;
using Programs.System;
using Simulation.Functions;
using System;
using System.Diagnostics;
using Unmanaged;

namespace Simulation
{
    /// <summary>
    /// Opaque pointer implementation of a <see cref="Simulator"/>.
    /// </summary>
    public unsafe struct UnsafeSimulator
    {
        private World world;
        private List<SystemContainer> systems;
        private List<ProgramContainer> knownPrograms;
        private ComponentQuery<IsProgram, ProgramState> programQuery;

        /// <summary>
        /// Allocates a new <see cref="UnsafeSimulator"/> instance.
        /// </summary>
        public static UnsafeSimulator* Allocate(World world)
        {
            UnsafeSimulator* simulator = Allocations.Allocate<UnsafeSimulator>();
            simulator->world = world;
            simulator->systems = new();
            simulator->knownPrograms = new();
            simulator->programQuery = new();
            return simulator;
        }

        /// <summary>
        /// Frees the memory used by a <see cref="UnsafeSimulator"/>.
        /// </summary>
        public static void Free(ref UnsafeSimulator* simulator)
        {
            Allocations.ThrowIfNull(simulator);

            simulator->programQuery.Dispose();
            simulator->knownPrograms.Dispose();
            simulator->systems.Dispose();
            Allocations.Free(ref simulator);
        }

        /// <summary>
        /// Retrieves the <see cref="World"/> that a <see cref="UnsafeSimulator"/> operates in.
        /// </summary>
        public static World GetWorld(UnsafeSimulator* simulator)
        {
            Allocations.ThrowIfNull(simulator);

            return simulator->world;
        }

        /// <summary>
        /// Retrieves the systems added to a <see cref="UnsafeSimulator"/>.
        /// </summary>
        public static USpan<SystemContainer> GetSystems(UnsafeSimulator* simulator)
        {
            Allocations.ThrowIfNull(simulator);

            return simulator->systems.AsSpan();
        }

        /// <summary>
        /// Retrieves the known programs in a <see cref="UnsafeSimulator"/>.
        /// </summary>
        public static ref List<ProgramContainer> GetKnownPrograms(UnsafeSimulator* simulator)
        {
            Allocations.ThrowIfNull(simulator);

            return ref simulator->knownPrograms;
        }

        /// <summary>
        /// Retrieves the query for programs in a <see cref="UnsafeSimulator"/>.
        /// </summary>
        public static ref ComponentQuery<IsProgram, ProgramState> GetProgramQuery(UnsafeSimulator* simulator)
        {
            Allocations.ThrowIfNull(simulator);

            return ref simulator->programQuery;
        }

        /// <summary>
        /// Retrieves the number of systems in a <see cref="UnsafeSimulator"/>.
        /// </summary>
        public static uint GetSystemCount(UnsafeSimulator* simulator)
        {
            Allocations.ThrowIfNull(simulator);

            return simulator->systems.Count;
        }

        /// <summary>
        /// Adds a system of type <typeparamref name="T"/> to a <see cref="UnsafeSimulator"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public static SystemContainer<T> AddSystem<T>(UnsafeSimulator* simulator) where T : unmanaged, ISystem
        {
            Allocations.ThrowIfNull(simulator);

            World hostWorld = GetWorld(simulator);
            nint systemType = RuntimeTypeHandle.ToIntPtr(typeof(T).TypeHandle);
            Trace.WriteLine($"Adding system `{typeof(T)}` to `{hostWorld}`");

            T template = new();
            Allocation instance = Allocation.Create(template);

            //add message handlers
            USpan<MessageHandler> buffer = stackalloc MessageHandler[32];
            uint messageHandlerCount = template.GetMessageHandlers(buffer);
            Dictionary<nint, HandleFunction> handlers;
            if (messageHandlerCount > 0)
            {
                handlers = new(messageHandlerCount);
                for (uint i = 0; i < messageHandlerCount; i++)
                {
                    MessageHandler handler = buffer[i];
                    if (handler == default)
                    {
                        throw new InvalidOperationException($"Message handler at index {i} is uninitialized in system `{typeof(T)}`");
                    }

                    handlers.Add(handler.messageType, handler.function);
                }
            }
            else
            {
                handlers = new(1);
            }

            SystemContainer container = new(simulator, instance, systemType, handlers, template.Initialize, template.Iterate, template.Finalize);
            simulator->systems.Add(container);
            SystemContainer<T> genericContainer = new(simulator, simulator->systems.Count - 1);
            container.Initialize(hostWorld);
            return genericContainer;
        }

        /// <summary>
        /// Removes a system of type <typeparamref name="T"/> from a <see cref="UnsafeSimulator"/>.
        /// </summary>
        public static void RemoveSystem<T>(UnsafeSimulator* simulator) where T : unmanaged, ISystem
        {
            Allocations.ThrowIfNull(simulator);

            World world = GetWorld(simulator);
            nint systemType = RuntimeTypeHandle.ToIntPtr(typeof(T).TypeHandle);
            Trace.WriteLine($"Removing system `{typeof(T)}` from `{world}`");

            for (uint i = 0; i < simulator->systems.Count; i++)
            {
                ref SystemContainer system = ref simulator->systems[i];
                if (system.systemType == systemType)
                {
                    system.Dispose();
                    simulator->systems.RemoveAt(i);
                }
            }
        }
    }
}
