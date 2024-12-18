using System;
using System.Diagnostics;
using System.Reflection;
using Unmanaged;
using Worlds.Unsafe;

namespace Worlds
{
    [DebuggerTypeProxy(typeof(Entity<>.DebugView))]
    public readonly struct Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> : IEntity, IEquatable<Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12>> where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged
    {
        private readonly Entity entity;

        readonly World IEntity.World => entity.world;
        readonly uint IEntity.Value => entity.value;
        readonly Definition IEntity.Definition => new(ComponentType.GetBitSet<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12>(), default);

        public Entity(World world, uint existingEntity)
        {
            entity = new(world, existingEntity);
        }

        public Entity(World world)
        {
            entity = new(world, world.CreateEntity(default(C1), default(C2), default(C3), default(C4), default(C5), default(C6), default(C7), default(C8), default(C9), default(C10), default(C11), default(C12)));
        }

        public Entity(World world, C1 c1, C2 c2, C3 c3, C4 c4, C5 c5, C6 c6, C7 c7, C8 c8, C9 c9, C10 c10, C11 c11, C12 c12)
        {
            entity = new(world, world.CreateEntity(c1, c2, c3, c4, c5, c6, c7, c8, c9, c10, c11, c12));
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
            return obj is Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> other && Equals(other);
        }

        public readonly bool Equals(Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> other)
        {
            return entity.Equals(other.entity);
        }

        public static bool operator ==(Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> left, Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> left, Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> right)
        {
            return !left.Equals(right);
        }

        public static implicit operator Entity(Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> entity)
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
            public readonly C2 c2;
            public readonly C3 c3;
            public readonly C4 c4;
            public readonly C5 c5;
            public readonly C6 c6;
            public readonly C7 c7;
            public readonly C8 c8;
            public readonly C9 c9;
            public readonly C10 c10;
            public readonly C11 c11;
            public readonly C12 c12;

            public DebugView(Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> entity)
            {
                value = entity.GetEntityValue();
                world = entity.GetWorld();
                parent = entity.GetParent();
                creationStackTrace = UnsafeWorld.createStackTraces[entity];
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
                c2 = entity.AsEntity().GetComponent<C2>();
                c3 = entity.AsEntity().GetComponent<C3>();
                c4 = entity.AsEntity().GetComponent<C4>();
                c5 = entity.AsEntity().GetComponent<C5>();
                c6 = entity.AsEntity().GetComponent<C6>();
                c7 = entity.AsEntity().GetComponent<C7>();
                c8 = entity.AsEntity().GetComponent<C8>();
                c9 = entity.AsEntity().GetComponent<C9>();
                c10 = entity.AsEntity().GetComponent<C10>();
                c11 = entity.AsEntity().GetComponent<C11>();
                c12 = entity.AsEntity().GetComponent<C12>();
            }
#endif
        }
    }
}