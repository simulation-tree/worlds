using System;
using Unmanaged;

namespace Worlds
{
    public readonly struct Entity : IEntity, IEquatable<Entity>
    {
        public readonly World world;
        public readonly uint value;

        public readonly bool IsDisposed => !world.ContainsEntity(value);

#if NET
        [Obsolete("Default constructor not supported", true)]
        public Entity()
        {
            throw new NotSupportedException();
        }
#endif

        /// <summary>
        /// Initializes an existing container of an entity.
        /// </summary>
        public Entity(World world, uint value)
        {
            //todo: emit an error saying that "hey 0 is not allowed"
            World.Implementation.ThrowIfEntityIsMissing(world, value);

            this.world = world;
            this.value = value;
        }

        /// <summary>
        /// Creates a new entity in the specified <paramref name="world"/>.
        /// </summary>
        public Entity(World world)
        {
            this.world = world;
            this.value = world.CreateEntity();
        }

        readonly void IEntity.Describe(ref Archetype archetype)
        {
        }

        public readonly void Dispose()
        {
            world.DestroyEntity(value);
        }

        public readonly bool Is<T>() where T : unmanaged, IEntity
        {
            Archetype archetype = Archetype.Get<T>(world.Schema);
            return world.Is(value, archetype);
        }

        public readonly bool Is(Definition definition)
        {
            return world.Is(value, definition);
        }

        public readonly bool Is(Archetype archetype)
        {
            return world.Is(value, archetype);
        }

        public readonly T Become<T>() where T : unmanaged, IEntity
        {
            Archetype archetype = Archetype.Get<T>(world.Schema);
            world.Become(value, archetype);
            return new Entity(world, value).As<T>();
        }

        public readonly void Become(Definition definition)
        {
            world.Become(value, definition);
        }

        public readonly void Become(Archetype archetype)
        {
            world.Become(value, archetype);
        }

        public readonly T As<T>() where T : unmanaged, IEntity
        {
            return EntityExtensions.As<T>(this);
        }

        public readonly Entity Clone()
        {
            return new Entity(world, world.CloneEntity(value));
        }

        public readonly bool Contains(ComponentType componentType)
        {
            return world.Contains(value, componentType);
        }

        public readonly bool Contains(ArrayElementType arrayElementType)
        {
            return world.Contains(value, arrayElementType);
        }

        public readonly bool Contains(TagType tagType)
        {
            return world.Contains(value, tagType);
        }

        public readonly void AddComponent(ComponentType componentType)
        {
            world.AddComponent(value, componentType);
        }

        public readonly void RemoveComponent(ComponentType componentType)
        {
            world.RemoveComponent(value, componentType);
        }

        public readonly Allocation CreateArray(ArrayElementType arrayElementType, uint length = 0)
        {
            return world.CreateArray(value, arrayElementType, length);
        }

        public readonly Allocation GetArray(ArrayElementType arrayElementType, out uint length)
        {
            return world.GetArray(value, arrayElementType, out length);
        }

        public readonly Allocation ResizeArray(ArrayElementType arrayElementType, uint newLength)
        {
            return world.ResizeArray(value, arrayElementType, newLength);
        }

        public readonly void DestroyArray(ArrayElementType arrayElementType)
        {
            world.DestroyArray(value, arrayElementType);
        }

        public readonly bool ContainsComponent<T>() where T : unmanaged
        {
            return world.ContainsComponent<T>(value);
        }

        public readonly ref T GetComponent<T>() where T : unmanaged
        {
            return ref world.GetComponent<T>(value);
        }

        public readonly bool TryGetComponent<T>(out T component) where T : unmanaged
        {
            return world.TryGetComponent(value, out component);
        }

        public readonly ref T TryGetComponent<T>(out bool contains) where T : unmanaged
        {
            return ref world.TryGetComponent<T>(value, out contains);
        }

        public readonly ref T AddComponent<T>() where T : unmanaged
        {
            return ref world.AddComponent<T>(value);
        }

        public readonly ref T AddComponent<T>(T component) where T : unmanaged
        {
            return ref world.AddComponent(value, component);
        }

        public readonly void RemoveComponent<T>() where T : unmanaged
        {
            world.RemoveComponent<T>(value);
        }

        public readonly bool ContainsArray<T>() where T : unmanaged
        {
            return world.ContainsArray<T>(value);
        }

        public readonly uint GetArrayLength<T>() where T : unmanaged
        {
            return world.GetArrayLength<T>(value);
        }

        public readonly ref T GetArrayElement<T>(uint index) where T : unmanaged
        {
            return ref world.GetArrayElement<T>(value, index);
        }

        public readonly USpan<T> GetArray<T>() where T : unmanaged
        {
            return world.GetArray<T>(value);
        }

        public readonly bool TryGetArray<T>(out USpan<T> array) where T : unmanaged
        {
            return world.TryGetArray(value, out array);
        }

        public readonly void CreateArray<T>(USpan<T> elements) where T : unmanaged
        {
            world.CreateArray(value, elements);
        }

        public readonly USpan<T> CreateArray<T>(uint length = 0) where T : unmanaged
        {
            return world.CreateArray<T>(value, length);
        }

        public readonly USpan<T> ResizeArray<T>(uint newLength) where T : unmanaged
        {
            return world.ResizeArray<T>(value, newLength);
        }

        public readonly void DestroyArray<T>() where T : unmanaged
        {
            world.DestroyArray<T>(value);
        }

        public readonly void ContainsTag<T>() where T : unmanaged
        {
            world.ContainsTag<T>(value);
        }

        public readonly void AddTag<T>() where T : unmanaged
        {
            world.AddTag<T>(value);
        }

        public readonly void RemoveTag<T>() where T : unmanaged
        {
            world.RemoveTag<T>(value);
        }

        public readonly override string ToString()
        {
            return value.ToString();
        }

        public readonly uint ToString(USpan<char> buffer)
        {
            return value.ToString(buffer);
        }

        public readonly override bool Equals(object? obj)
        {
            return obj is Entity entity && Equals(entity);
        }

        public readonly bool Equals(Entity other)
        {
            return world == other.world && value == other.value;
        }

        public readonly override int GetHashCode()
        {
            return HashCode.Combine(world, value);
        }

        public static Entity Create<T1>(World world, T1 c1) where T1 : unmanaged
        {
            return new Entity(world, world.CreateEntity(c1));
        }

        public static Entity Create<T1, T2>(World world, T1 c1, T2 c2) where T1 : unmanaged where T2 : unmanaged
        {
            return new Entity(world, world.CreateEntity(c1, c2));
        }

        public static Entity Create<T1, T2, T3>(World world, T1 c1, T2 c2, T3 c3) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            return new Entity(world, world.CreateEntity(c1, c2, c3));
        }

        public static Entity Create<T1, T2, T3, T4>(World world, T1 c1, T2 c2, T3 c3, T4 c4) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            return new Entity(world, world.CreateEntity(c1, c2, c3, c4));
        }

        public static Entity Create<T1, T2, T3, T4, T5>(World world, T1 c1, T2 c2, T3 c3, T4 c4, T5 c5) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
        {
            return new Entity(world, world.CreateEntity(c1, c2, c3, c4, c5));
        }

        public static Entity Create<T1, T2, T3, T4, T5, T6>(World world, T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
        {
            return new Entity(world, world.CreateEntity(c1, c2, c3, c4, c5, c6));
        }

        public static Entity Create<T1, T2, T3, T4, T5, T6, T7>(World world, T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged
        {
            return new Entity(world, world.CreateEntity(c1, c2, c3, c4, c5, c6, c7));
        }

        public static Entity Create<T1, T2, T3, T4, T5, T6, T7, T8>(World world, T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7, T8 c8) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged
        {
            return new Entity(world, world.CreateEntity(c1, c2, c3, c4, c5, c6, c7, c8));
        }

        public static Entity Create<T1, T2, T3, T4, T5, T6, T7, T8, T9>(World world, T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7, T8 c8, T9 c9) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged
        {
            return new Entity(world, world.CreateEntity(c1, c2, c3, c4, c5, c6, c7, c8, c9));
        }

        public static Entity Create<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(World world, T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7, T8 c8, T9 c9, T10 c10) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged
        {
            return new Entity(world, world.CreateEntity(c1, c2, c3, c4, c5, c6, c7, c8, c9, c10));
        }

        public static Entity Create<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(World world, T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7, T8 c8, T9 c9, T10 c10, T11 c11) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged
        {
            return new Entity(world, world.CreateEntity(c1, c2, c3, c4, c5, c6, c7, c8, c9, c10, c11));
        }

        public static Entity Create<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(World world, T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7, T8 c8, T9 c9, T10 c10, T11 c11, T12 c12) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged
        {
            return new Entity(world, world.CreateEntity(c1, c2, c3, c4, c5, c6, c7, c8, c9, c10, c11, c12));
        }

        public static Entity Create<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(World world, T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7, T8 c8, T9 c9, T10 c10, T11 c11, T12 c12, T13 c13) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged
        {
            return new Entity(world, world.CreateEntity(c1, c2, c3, c4, c5, c6, c7, c8, c9, c10, c11, c12, c13));
        }

        public static Entity Create<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(World world, T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7, T8 c8, T9 c9, T10 c10, T11 c11, T12 c12, T13 c13, T14 c14) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged
        {
            return new Entity(world, world.CreateEntity(c1, c2, c3, c4, c5, c6, c7, c8, c9, c10, c11, c12, c13, c14));
        }

        public static Entity Create<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(World world, T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7, T8 c8, T9 c9, T10 c10, T11 c11, T12 c12, T13 c13, T14 c14, T15 c15) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged where T15 : unmanaged
        {
            return new Entity(world, world.CreateEntity(c1, c2, c3, c4, c5, c6, c7, c8, c9, c10, c11, c12, c13, c14, c15));
        }

        public static Entity Create<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(World world, T1 c1, T2 c2, T3 c3, T4 c4, T5 c5, T6 c6, T7 c7, T8 c8, T9 c9, T10 c10, T11 c11, T12 c12, T13 c13, T14 c14, T15 c15, T16 c16) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged where T15 : unmanaged where T16 : unmanaged
        {
            return new Entity(world, world.CreateEntity(c1, c2, c3, c4, c5, c6, c7, c8, c9, c10, c11, c12, c13, c14, c15, c16));
        }

        public static bool operator ==(Entity left, Entity right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Entity left, Entity right)
        {
            return !(left == right);
        }
    }
}