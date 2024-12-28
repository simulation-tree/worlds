using System;
using System.Diagnostics;
using Unmanaged;

namespace Worlds
{
    [DebuggerTypeProxy(typeof(Entity<>.DebugView))]
    public readonly struct Entity<C1> : IEntity, IEquatable<Entity<C1>> where C1 : unmanaged
    {
        private readonly Entity entity;

        readonly World IEntity.World => entity.world;
        readonly uint IEntity.Value => entity.value;

        readonly Definition IEntity.GetDefinition(Schema schema)
        {
            return new(schema.GetComponents<C1>(), default);
        }

        public Entity(World world, uint existingEntity)
        {
            entity = new(world, existingEntity);
        }

        public Entity(World world)
        {
            entity = new(world, world.CreateEntity(default(C1)));
        }

        public Entity(World world, C1 c1)
        {
            entity = new(world, world.CreateEntity(c1));
        }

        public readonly void Dispose()
        {
            entity.Dispose();
        }

        public readonly override string ToString()
        {
            return entity.ToString();
        }

        public readonly uint ToString(USpan<char> buffer)
        {
            return entity.ToString(buffer);
        }

        public readonly override int GetHashCode()
        {
            return entity.GetHashCode();
        }

        public readonly override bool Equals(object? obj)
        {
            return obj is Entity<C1> other && Equals(other);
        }

        public readonly bool Equals(Entity<C1> other)
        {
            return entity.Equals(other.entity);
        }

        public static bool operator ==(Entity<C1> left, Entity<C1> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Entity<C1> left, Entity<C1> right)
        {
            return !left.Equals(right);
        }

        public static implicit operator Entity(Entity<C1> entity)
        {
            return entity.entity;
        }

        internal class DebugView
        {
#if DEBUG
            public readonly uint value;
            public readonly World world;
            public readonly Entity parent;
            public readonly StackTrace creationStackTrace;
            public readonly ComponentType[] componentTypes;
            public readonly ArrayType[] arrayTypes;
            public readonly Entity[] references;
            public readonly C1 c1;

            public DebugView(Entity<C1> entity)
            {
                value = entity.GetEntityValue();
                world = entity.GetWorld();
                parent = entity.GetParent();
                creationStackTrace = World.Implementation.createStackTraces[entity];
                USpan<ComponentType> componentTypeBuffer = stackalloc ComponentType[BitSet.Capacity];
                uint bufferLength = entity.CopyComponentTypesTo(componentTypeBuffer);
                componentTypes = componentTypeBuffer.Slice(0, bufferLength).ToArray();
                USpan<ArrayType> arrayTypeBuffer = stackalloc ArrayType[BitSet.Capacity];
                bufferLength = entity.CopyArrayTypesTo(arrayTypeBuffer);
                arrayTypes = arrayTypeBuffer.Slice(0, bufferLength).ToArray();
                references = new Entity[entity.GetReferenceCount()];
                for (uint i = 0; i < references.Length; i++)
                {
                    rint reference = new(i + 1);
                    references[i] = new(world, entity.GetReference(reference));
                }

                c1 = entity.AsEntity().GetComponent<C1>();
            }
#endif
        }
    }
}