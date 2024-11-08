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
    public unsafe struct UnsafeSimulator
    {
        private World world;
        private List<SystemContainer> systems;
        private List<ProgramContainer> knownPrograms;
        private ComponentQuery<IsProgram, ProgramState> programQuery;

        public static UnsafeSimulator* Allocate(World world)
        {
            UnsafeSimulator* simulator = Allocations.Allocate<UnsafeSimulator>();
            simulator->world = world;
            simulator->systems = new();
            simulator->knownPrograms = new();
            simulator->programQuery = new();
            return simulator;
        }

        public static bool IsDisposed(UnsafeSimulator* simulator)
        {
            return simulator is null;
        }

        public static void Free(ref UnsafeSimulator* simulator)
        {
            Allocations.ThrowIfNull(simulator);

            simulator->programQuery.Dispose();
            simulator->knownPrograms.Dispose();
            simulator->systems.Dispose();
            Allocations.Free(ref simulator);
        }

        public static World GetWorld(UnsafeSimulator* simulator)
        {
            Allocations.ThrowIfNull(simulator);
            return simulator->world;
        }

        public static USpan<SystemContainer> GetSystems(UnsafeSimulator* simulator)
        {
            Allocations.ThrowIfNull(simulator);
            return simulator->systems.AsSpan();
        }

        public static ref List<ProgramContainer> GetKnownPrograms(UnsafeSimulator* simulator)
        {
            Allocations.ThrowIfNull(simulator);
            return ref simulator->knownPrograms;
        }

        public static ref ComponentQuery<IsProgram, ProgramState> GetProgramQuery(UnsafeSimulator* simulator)
        {
            Allocations.ThrowIfNull(simulator);
            return ref simulator->programQuery;
        }

        public static uint GetSystemCount(UnsafeSimulator* simulator)
        {
            Allocations.ThrowIfNull(simulator);
            return simulator->systems.Count;
        }

        public static SystemContainer<T> AddSystem<T>(UnsafeSimulator* simulator) where T : unmanaged, ISystem
        {
            Allocations.ThrowIfNull(simulator);

            World hostWorld = GetWorld(simulator);
            RuntimeType type = RuntimeType.Get<T>();
            Trace.WriteLine($"Adding system {type} to {hostWorld}");

            T template = new();
            Allocation instance = Allocation.Create(template);

            //add message handlers
            USpan<MessageHandler> buffer = stackalloc MessageHandler[32];
            uint messageHandlerCount = template.GetMessageHandlers(buffer);
            Dictionary<RuntimeType, HandleFunction> handlers;
            if (messageHandlerCount > 0)
            {
                handlers = new(messageHandlerCount);
                for (uint i = 0; i < messageHandlerCount; i++)
                {
                    MessageHandler handler = buffer[i];
                    if (handler == default)
                    {
                        throw new InvalidOperationException($"Message handler at index {i} is uninitialized in system {type}");
                    }

                    handlers.Add(handler.type, handler.function);
                }
            }
            else
            {
                handlers = new(1);
            }

            SystemContainer container = new(simulator, instance, type, handlers, template.Initialize, template.Iterate, template.Finalize);
            simulator->systems.Add(container);
            SystemContainer<T> genericContainer = new(simulator, simulator->systems.Count - 1);
            container.Initialize(hostWorld);
            return genericContainer;
        }

        public static void RemoveSystem<T>(UnsafeSimulator* simulator) where T : unmanaged, ISystem
        {
            Allocations.ThrowIfNull(simulator);

            World world = GetWorld(simulator);
            RuntimeType type = RuntimeType.Get<T>();
            Trace.WriteLine($"Removing system {type} from {world}");

            for (uint i = 0; i < simulator->systems.Count; i++)
            {
                ref SystemContainer system = ref simulator->systems[i];
                if (system.type == type)
                {
                    system.Dispose();
                    simulator->systems.RemoveAt(i);
                }
            }
        }
    }
}
