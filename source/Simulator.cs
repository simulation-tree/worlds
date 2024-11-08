using Collections;
using Programs;
using Programs.Components;
using Programs.System;
using System;
using System.Diagnostics;
using Unmanaged;

namespace Simulation
{
    public unsafe struct Simulator : IDisposable
    {
        private UnsafeSimulator* value;

        public readonly World World => UnsafeSimulator.GetWorld(value);
        public readonly bool IsDisposed => UnsafeSimulator.IsDisposed(value);
        public readonly nint Address => (nint)value;
        public readonly uint SystemCount => UnsafeSimulator.GetSystems(value).Length;
        public readonly USpan<ProgramContainer> Programs => UnsafeSimulator.GetKnownPrograms(value).AsSpan();

        [Obsolete("Default constructor not supported", true)]
        public Simulator()
        {
            throw new NotImplementedException();
        }

        public Simulator(nint address)
        {
            value = (UnsafeSimulator*)address;
        }

        public Simulator(World world)
        {
            value = UnsafeSimulator.Allocate(world);
        }

        public void Dispose()
        {
            InitializeSystems();
            FinishDestroyedPrograms();

            World hostWorld = World;

            //finalize program worlds
            ref ComponentQuery<IsProgram, ProgramState> query = ref UnsafeSimulator.GetProgramQuery(value);
            query.Update(World, true);
            foreach (var x in query)
            {
                uint programEntity = x.entity;
                if (!hostWorld.ContainsComponent<uint>(programEntity))
                {
                    World programWorld = hostWorld.GetComponent<World>(programEntity);
                    ProgramAllocation allocation = programWorld.GetComponent<ProgramAllocation>(programEntity);
                    x.Component1.finish.Invoke(this, allocation.value, programWorld, default);
                    ref ProgramState state = ref x.Component2;
                    state = ProgramState.Finished;
                }
            }

            //clean up previously known programs
            ref List<ProgramContainer> knownPrograms = ref UnsafeSimulator.GetKnownPrograms(value);
            for (uint i = knownPrograms.Count - 1; i != uint.MaxValue; i--)
            {
                ref ProgramContainer program = ref knownPrograms[i];
                program.programWorld.Dispose();
                program.allocation.Dispose();
            }

            knownPrograms.Clear();
            UnsafeSimulator.Free(ref value);
        }

        private readonly void FinishDestroyedPrograms()
        {
            ref List<ProgramContainer> knownPrograms = ref UnsafeSimulator.GetKnownPrograms(value);
            for (uint i = knownPrograms.Count - 1; i != uint.MaxValue; i--)
            {
                ref ProgramContainer program = ref knownPrograms[i];
                if (!program.finished && program.program.IsDestroyed())
                {
                    program.finished = true;
                    program.finish.Invoke(this, program.allocation, program.programWorld, default);
                }
            }
        }

        /// <summary>
        /// Updates all systems forward, then all programs.
        /// </summary>
        public readonly void Update(TimeSpan delta)
        {
            UpdateSystems(delta);
            UpdatePrograms(delta);
        }

        /// <summary>
        /// Updates all programs forward.
        /// </summary>
        public readonly void UpdatePrograms(TimeSpan delta)
        {
            FinishDestroyedPrograms();
            InitializePrograms();

            //update program worlds
            ref List<ProgramContainer> knownPrograms = ref UnsafeSimulator.GetKnownPrograms(value);
            uint updatedPrograms = 0;
            for (uint p = 0; p < knownPrograms.Count; p++)
            {
                ref ProgramContainer programContainer = ref knownPrograms[p];
                if (!programContainer.finished)
                {
                    World programWorld = programContainer.programWorld;
                    Allocation allocation = programContainer.allocation;
                    uint returnCode = programContainer.update.Invoke(this, allocation, programWorld, delta);
                    if (returnCode != default)
                    {
                        //program finished
                        programContainer.program.SetComponent(ProgramState.Finished);
                        programContainer.program.AddComponent(returnCode);
                        programContainer.finished = true;
                        programContainer.finish.Invoke(this, allocation, programWorld, returnCode);
                    }
                    else
                    {
                        updatedPrograms++;
                    }
                }
            }
        }

        /// <summary>
        /// Updates all systems with the simulator host world first,
        /// then all individual program worlds.
        /// </summary>
        public readonly void UpdateSystems(TimeSpan delta)
        {
            InitializeSystems();

            World hostWorld = World;
            USpan<SystemContainer> systems = UnsafeSimulator.GetSystems(value);

            //update systems with host world
            for (uint i = 0; i < systems.Length; i++)
            {
                ref SystemContainer system = ref systems[i];
                system.Update(hostWorld, delta);
            }

            //update systems with each program worlds
            ref List<ProgramContainer> knownPrograms = ref UnsafeSimulator.GetKnownPrograms(value);
            for (uint p = 0; p < knownPrograms.Count; p++)
            {
                ref ProgramContainer programContainer = ref knownPrograms[p];
                if (!programContainer.finished)
                {
                    World programWorld = programContainer.programWorld;
                    for (uint s = 0; s < systems.Length; s++)
                    {
                        ref SystemContainer system = ref systems[s];
                        system.Update(programWorld, delta);
                    }
                }
            }
        }

        /// <summary>
        /// Submits a message for a potential system to handle.
        /// </summary>
        /// <returns><c>true</c> if it was handled.</returns>
        public readonly bool TryHandleMessage<T>(T message) where T : unmanaged
        {
            InitializeSystems();
            InitializePrograms();

            using Allocation allocation = Allocation.Create(message);
            RuntimeType type = RuntimeType.Get<T>();
            USpan<SystemContainer> systems = UnsafeSimulator.GetSystems(value);
            World hostWorld = World;
            bool handled = false;

            //tell host world
            for (uint i = 0; i < systems.Length; i++)
            {
                ref SystemContainer system = ref systems[i];
                handled |= system.TryHandleMessage(hostWorld, type, allocation);
            }

            //tell program worlds
            ref ComponentQuery<IsProgram, ProgramState> query = ref UnsafeSimulator.GetProgramQuery(value);
            query.Update(hostWorld, true);
            foreach (var x in query)
            {
                uint programEntity = x.entity;
                if (!hostWorld.ContainsComponent<uint>(programEntity))
                {
                    World programWorld = hostWorld.GetComponent<World>(programEntity);
                    for (uint i = 0; i < systems.Length; i++)
                    {
                        ref SystemContainer system = ref systems[i];
                        handled |= system.TryHandleMessage(programWorld, type, allocation);
                    }
                }
            }

            return handled;
        }

        private readonly void InitializeSystems()
        {
            ref List<ProgramContainer> knownPrograms = ref UnsafeSimulator.GetKnownPrograms(value);
            USpan<SystemContainer> systems = UnsafeSimulator.GetSystems(value);
            for (uint p = 0; p < knownPrograms.Count; p++)
            {
                ref ProgramContainer programContainer = ref knownPrograms[p];
                World programWorld = programContainer.programWorld;
                for (uint s = 0; s < systems.Length; s++)
                {
                    ref SystemContainer system = ref systems[s];
                    if (!system.IsInitializedWith(programWorld))
                    {
                        system.Initialize(programWorld);
                    }
                }
            }
        }

        private readonly void InitializePrograms()
        {
            World hostWorld = World;
            ref List<ProgramContainer> knownPrograms = ref UnsafeSimulator.GetKnownPrograms(value);
            ref ComponentQuery<IsProgram, ProgramState> query = ref UnsafeSimulator.GetProgramQuery(value);
            query.Update(hostWorld, true);
            foreach (var x in query)
            {
                Entity program = new(hostWorld, x.entity);
                if (!program.ContainsComponent<World>())
                {
                    World newProgramWorld = new();
                    ref ProgramState state = ref x.Component2;
                    state = ProgramState.Active;
                    Allocation programAllocation = new(x.Component1.type.Size);
                    ProgramContainer programContainer = new(x.Component1, newProgramWorld, program, programAllocation);
                    program.AddComponent(newProgramWorld);
                    program.AddComponent(new ProgramAllocation(programAllocation));
                    knownPrograms.Add(programContainer);

                    programContainer.start.Invoke(this, programAllocation, newProgramWorld);
                }
            }
        }

        /// <summary>
        /// Adds a system to the simulator without initializing it.
        /// </summary>
        public readonly SystemContainer<T> AddSystem<T>() where T : unmanaged, ISystem
        {
            return UnsafeSimulator.AddSystem<T>(value);
        }

        public readonly void RemoveSystem<T>() where T : unmanaged, ISystem
        {
            ThrowIfSystemIsMissing<T>();
            UnsafeSimulator.RemoveSystem<T>(value);
        }

        public readonly bool ContainsSystem<T>() where T : unmanaged, ISystem
        {
            RuntimeType type = RuntimeType.Get<T>();
            USpan<SystemContainer> systems = UnsafeSimulator.GetSystems(value);
            for (uint i = 0; i < systems.Length; i++)
            {
                ref SystemContainer system = ref systems[i];
                if (system.type == type)
                {
                    return true;
                }
            }

            return false;
        }

        public readonly SystemContainer<T> GetSystem<T>() where T : unmanaged, ISystem
        {
            RuntimeType type = RuntimeType.Get<T>();
            USpan<SystemContainer> systems = UnsafeSimulator.GetSystems(value);
            for (uint i = 0; i < systems.Length; i++)
            {
                ref SystemContainer system = ref systems[i];
                if (system.type == type)
                {
                    return new(value, i);
                }
            }

            throw new InvalidOperationException($"System `{type}` is not registered in the simulator");
        }

        [Conditional("DEBUG")]
        public readonly void ThrowIfSystemIsMissing<T>() where T : unmanaged, ISystem
        {
            USpan<SystemContainer> systems = UnsafeSimulator.GetSystems(value);
            for (uint i = 0; i < systems.Length; i++)
            {
                ref SystemContainer system = ref systems[i];
                if (system.type.Is<T>())
                {
                    return;
                }
            }

            throw new InvalidOperationException($"System `{typeof(T)}` is not registered in the simulator");
        }
    }
}
