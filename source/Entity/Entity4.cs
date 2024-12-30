using System;
using System.Diagnostics;
using Unmanaged;

namespace Worlds
{
    [DebuggerTypeProxy(typeof(Entity<>.DebugView))]
    public readonly struct Entity<C1, C2, C3, C4> : IEntity, IEquatable<Entity<C1, C2, C3, C4>> where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged
    {
        private readonly Entity entity;

        readonly World IEntity.World => entity.world;
        readonly uint IEntity.Value => entity.value;

        readonly Definition IEntity.GetDefinition(Schema schema)
        {
            return new(schema.GetComponents<C1, C2, C3, C4>(), default, default);
        }

        public Entity(World world, uint existingEntity)
        {
            entity = new(world, existingEntity);
        }

        public Entity(World world)
        {
            entity = new(world, world.CreateEntity(default(C1), default(C2), default(C3), default(C4)));
        }

        public Entity(World world, C1 c1, C2 c2, C3 c3, C4 c4)
        {
            entity = new(world, world.CreateEntity(c1, c2, c3, c4));
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
            return obj is Entity<C1, C2, C3, C4> other && Equals(other);
        }

        public readonly bool Equals(Entity<C1, C2, C3, C4> other)
        {
            return entity.Equals(other.entity);
        }

        public static bool operator ==(Entity<C1, C2, C3, C4> left, Entity<C1, C2, C3, C4> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Entity<C1, C2, C3, C4> left, Entity<C1, C2, C3, C4> right)
        {
            return !left.Equals(right);
        }

        public static implicit operator Entity(Entity<C1, C2, C3, C4> entity)
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
            public readonly ArrayElementType[] arrayElementTypes;
            public readonly Entity[] references;
            public readonly C1 c1;
            public readonly C2 c2;
            public readonly C3 c3;
            public readonly C4 c4;

            public DebugView(Entity<C1, C2, C3, C4> entity)
            {
                value = entity.GetEntityValue();
                world = entity.GetWorld();
                parent = entity.GetParent();
                creationStackTrace = World.Implementation.createStackTraces[entity];
                USpan<ComponentType> componentBuffer = stackalloc ComponentType[BitSet.Capacity];
                uint bufferLength = entity.CopyComponentTypesTo(componentBuffer);
                componentTypes = componentBuffer.Slice(0, bufferLength).ToArray();
                USpan<ArrayElementType> arrayBuffer = stackalloc ArrayElementType[BitSet.Capacity];
                bufferLength = entity.CopyArrayElementTypesTo(arrayBuffer);
                arrayElementTypes = arrayBuffer.Slice(0, bufferLength).ToArray();
                references = new Entity[entity.GetReferenceCount()];
                for (uint i = 0; i < references.Length; i++)
                {
                    rint reference = new(i + 1);
                    references[i] = new(world, entity.GetReference(reference));
                }

                c1 = entity.AsEntity().GetComponent<C1>();
                c2 = entity.AsEntity().GetComponent<C2>();
                c3 = entity.AsEntity().GetComponent<C3>();
                c4 = entity.AsEntity().GetComponent<C4>();
            }
#endif
        }
    }
}