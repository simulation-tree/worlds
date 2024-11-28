using Programs.Components;
using Simulation;
using System;
using System.Diagnostics;
using Unmanaged;

namespace Programs
{
    /// <summary>
    /// An entity that represents a program running in a <see cref="World"/>,
    /// operated by a <see cref="Simulator"/>.
    /// </summary>
    public readonly struct Program : IEntity
    {
        private readonly Entity entity;

        /// <summary>
        /// Gets the state of the program.
        /// </summary>
        public readonly ProgramState State => entity.GetComponent<ProgramState>();

        readonly uint IEntity.Value => entity.GetEntityValue();
        readonly World IEntity.World => entity.GetWorld();
        readonly Definition IEntity.Definition => new Definition().AddComponentTypes<IsProgram, ProgramState>();

        /// <summary>
        /// Creates a new program in the given <see cref="World"/>.
        /// </summary>
        public Program(World world, StartProgramFunction start, UpdateProgramFunction update, FinishProgramFunction finish, ushort typeSize)
        {
            entity = new(world);
            entity.AddComponent(new IsProgram(start, update, finish, typeSize));
            entity.AddComponent(ProgramState.Uninitialized);
        }

        /// <summary>
        /// Destroys the program.
        /// </summary>
        public readonly void Dispose()
        {
            entity.Dispose();
        }

        /// <summary>
        /// Checks if the program has finished running
        /// and outputs the <paramref name="returnCode"/> if finished.
        /// </summary>
        public readonly bool IsFinished(out uint returnCode)
        {
            if (State == ProgramState.Finished)
            {
                returnCode = entity.GetComponent<uint>();
                return true;
            }
            else
            {
                returnCode = default;
                return false;
            }
        }

        /// <summary>
        /// Reads the program's data.
        /// </summary>
        public readonly ref T Read<T>() where T : unmanaged
        {
            ThrowIfNotInitialized();
            ref ProgramAllocation allocation = ref entity.GetComponentRef<ProgramAllocation>();
            return ref allocation.value.Read<T>();
        }

        /// <summary>
        /// Throws an <see cref="InvalidOperationException"/> if the program hans't been initialized
        /// by a <see cref="Simulator"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        [Conditional("DEBUG")]
        public readonly void ThrowIfNotInitialized()
        {
            if (State == ProgramState.Uninitialized)
            {
                throw new InvalidOperationException($"Program `{entity}` is not initialized");
            }
        }

        /// <summary>
        /// Creates a new program in the given <see cref="World"/>.
        /// </summary>
        public static Program Create<T>(World world) where T : unmanaged, IProgram
        {
            T template = default;
            return new Program(world, template.Start, template.Update, template.Finish, (ushort)TypeInfo<T>.size);
        }
    }
}