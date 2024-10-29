﻿using Programs.Components;
using Programs.Functions;
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
        public readonly RuntimeType Type => entity.GetComponent<RuntimeType>();
        readonly uint IEntity.Value => entity.GetEntityValue();
        readonly World IEntity.World => entity.GetWorld();
        readonly Definition IEntity.Definition => new Definition().AddComponentTypes<IsProgram, ProgramState>();

        public Program(World world, StartFunction start, UpdateFunction update, FinishFunction finish, RuntimeType type)
        {
            entity = new(world);
            entity.AddComponent(new IsProgram(start, update, finish, type));
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
            RuntimeType type = RuntimeType.Get<T>();
            T template = default;
            return new Program(world, template.Start, template.Update, template.Finish, type);
        }
    }
}