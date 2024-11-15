using Programs.Components;
using Simulation;
using System;
using System.Diagnostics;
using Unmanaged;

namespace Programs
{
    public readonly struct Program : IEntity
    {
        public readonly Entity entity;

        public readonly ProgramState State => entity.GetComponent<ProgramState>();
        readonly uint IEntity.Value => entity.GetEntityValue();
        readonly World IEntity.World => entity.GetWorld();
        readonly Definition IEntity.Definition => new Definition().AddComponentTypes<IsProgram, ProgramState>();

        public Program(World world, StartProgramFunction start, UpdateProgramFunction update, FinishProgramFunction finish, ushort typeSize)
        {
            entity = new(world);
            entity.AddComponent(new IsProgram(start, update, finish, typeSize));
            entity.AddComponent(ProgramState.Uninitialized);
        }

        public readonly void Dispose()
        {
            entity.Dispose();
        }

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

        public readonly ref T Read<T>() where T : unmanaged
        {
            ThrowIfNotInitialized();
            ref ProgramAllocation allocation = ref entity.GetComponentRef<ProgramAllocation>();
            return ref allocation.value.Read<T>();
        }

        [Conditional("DEBUG")]
        public readonly void ThrowIfNotInitialized()
        {
            if (State == ProgramState.Uninitialized)
            {
                throw new InvalidOperationException($"Program `{entity}` is not initialized");
            }
        }

        public static Program Create<T>(World world) where T : unmanaged, IProgram
        {
            T template = default;
            return new Program(world, template.Start, template.Update, template.Finish, (ushort)TypeInfo<T>.size);
        }
    }
}