using System;
using System.Diagnostics;

namespace Worlds
{
    [DebuggerTypeProxy(typeof(Entity.DebugView))]
    public readonly struct Entity : IEntity, IEquatable<Entity>
    {
        /// <summary>
        /// The world this entity belongs to.
        /// </summary>
        public readonly World world;

        /// <summary>
        /// The position of this entity in the <see cref="world"/>.
        /// </summary>
        public readonly uint value;

        /// <summary>
        /// Checks if this entity is destroyed.
        /// </summary>
        public readonly bool IsDestroyed => !world.ContainsEntity(value);

        /// <summary>
        /// Retrieves all entities referenced by this entity.
        /// </summary>
        public readonly ReadOnlySpan<uint> References => world.GetReferences(value);

        /// <summary>
        /// Checks if the entity is enabled.
        /// </summary>
        public readonly bool IsEnabled
        {
            get => world.IsEnabled(value);
            set => world.SetEnabled(this.value, value);
        }

        /// <summary>
        /// The entity of this parent.
        /// <para>
        /// May be <see langword="default"/> if none is set.
        /// </para>
        /// </summary>
        public readonly Entity Parent
        {
            get
            {
                uint parent = world.GetParent(value);
                return parent == default ? default : new Entity(world, parent);
            }
        }

        /// <summary>
        /// Retrieves how many children this entity has.
        /// </summary>
        public readonly int ChildCount => world.GetChildCount(value);

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
            ThrowIfEntityIsMissing(world, value);

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

        public readonly bool SetParent(uint otherEntity)
        {
            return world.SetParent(value, otherEntity);
        }

        public readonly bool SetParent<T>(T otherEntity) where T : unmanaged, IEntity
        {
            return world.SetParent(value, otherEntity.GetEntityValue());
        }

        public readonly bool TryGetParent(out Entity parent)
        {
            uint parentValue = world.GetParent(value);
            if (parentValue != default)
            {
                parent = new Entity(world, parentValue);
                return true;
            }
            else
            {
                parent = default;
                return false;
            }
        }

        public readonly int CopyChildrenTo(Span<uint> destination)
        {
            return world.CopyChildrenTo(value, destination);
        }

        /// <summary>
        /// Checks if this entity complies with another entity of type <typeparamref name="T"/>
        /// </summary>
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

        /// <summary>
        /// Adds the <paramref name="otherEntity"/> as a new reference.
        /// </summary>
        public readonly rint AddReference(uint otherEntity)
        {
            return world.AddReference(value, otherEntity);
        }

        public readonly rint AddReference<T>(T otherEntity) where T : unmanaged, IEntity
        {
            return world.AddReference(value, otherEntity.GetEntityValue());
        }

        public readonly bool ContainsReference(uint otherEntity)
        {
            return world.ContainsReference(value, otherEntity);
        }

        public readonly bool ContainsReference(rint reference)
        {
            return world.ContainsReference(value, reference);
        }

        public readonly bool ContainsReference<T>(T otherEntity) where T : unmanaged, IEntity
        {
            return world.ContainsReference(value, otherEntity.GetEntityValue());
        }

        public readonly rint RemoveReference(uint otherEntity)
        {
            return world.RemoveReference(value, otherEntity);
        }

        public readonly uint RemoveReference(rint reference)
        {
            return world.RemoveReference(value, reference);
        }

        public readonly rint RemoveReference<T>(T otherEntity) where T : unmanaged, IEntity
        {
            return world.RemoveReference(value, otherEntity.GetEntityValue());
        }

        public readonly ref uint GetReference(rint reference)
        {
            return ref world.GetReference(value, reference);
        }

        public readonly rint GetReference(uint otherEntity)
        {
            return world.GetReference(value, otherEntity);
        }

        public readonly rint GetReference<T>(T otherEntity) where T : unmanaged, IEntity
        {
            return world.GetReference(value, otherEntity.GetEntityValue());
        }

        public readonly void SetReference(rint reference, uint otherEntity)
        {
            world.SetReference(value, reference, otherEntity);
        }

        public readonly void SetReference<T>(rint reference, T otherEntity) where T : unmanaged, IEntity
        {
            world.SetReference(value, reference, otherEntity.GetEntityValue());
        }

        public readonly bool TryGetReference(rint reference, out uint otherEntity)
        {
            return world.TryGetReference(value, reference, out otherEntity);
        }

        public readonly Entity Clone()
        {
            return new Entity(world, world.CloneEntity(value));
        }

        public readonly bool ContainsComponent(int componentType)
        {
            return world.ContainsComponent(value, componentType);
        }

        public readonly bool ContainsArray(int arrayType)
        {
            return world.ContainsArray(value, arrayType);
        }

        public readonly bool ContainsTag(int tagType)
        {
            return world.ContainsTag(value, tagType);
        }

        public readonly void AddComponentType(int componentType)
        {
            world.AddComponentType(value, componentType);
        }

        public readonly void RemoveComponent(int componentType)
        {
            world.RemoveComponent(value, componentType);
        }

        public readonly Values CreateArray(int arrayType, int length = 0)
        {
            return world.CreateArray(value, arrayType, length);
        }

        public readonly Values CreateArray(DataType arrayType, int length = 0)
        {
            return world.CreateArray(value, arrayType, length);
        }

        public readonly Values GetArray(int arrayType)
        {
            return world.GetArray(value, arrayType);
        }

        public readonly void DestroyArray(int arrayType)
        {
            world.DestroyArray(value, arrayType);
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

        public readonly void SetComponent<T>(T component) where T : unmanaged
        {
            world.SetComponent(value, component);
        }

        public readonly ref T AddComponent<T>() where T : unmanaged
        {
            return ref world.AddComponent<T>(value);
        }

        public readonly void AddComponent<T>(T component) where T : unmanaged
        {
            world.AddComponent(value, component);
        }

        public readonly void RemoveComponent<T>() where T : unmanaged
        {
            world.RemoveComponent<T>(value);
        }

        public readonly bool ContainsArray<T>() where T : unmanaged
        {
            return world.ContainsArray<T>(value);
        }

        public readonly int GetArrayLength<T>() where T : unmanaged
        {
            return world.GetArrayLength<T>(value);
        }

        public readonly ref T GetArrayElement<T>(int index) where T : unmanaged
        {
            return ref world.GetArrayElement<T>(value, index);
        }

        public readonly Values<T> GetArray<T>() where T : unmanaged
        {
            return world.GetArray<T>(value);
        }

        public readonly bool TryGetArray<T>(out Values<T> array) where T : unmanaged
        {
            return world.TryGetArray(value, out array);
        }

        public readonly void CreateArray<T>(ReadOnlySpan<T> elements) where T : unmanaged
        {
            world.CreateArray(value, elements);
        }

        public readonly Values<T> CreateArray<T>(int length = 0) where T : unmanaged
        {
            return world.CreateArray<T>(value, length);
        }

        public readonly void DestroyArray<T>() where T : unmanaged
        {
            world.DestroyArray<T>(value);
        }

        public readonly bool ContainsTag<T>() where T : unmanaged
        {
            return world.ContainsTag<T>(value);
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

        public readonly int ToString(Span<char> destination)
        {
            return value.ToString(destination);
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
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + world.GetHashCode();
                hash = hash * 23 + value.GetHashCode();
                return hash;
            }
        }

        [Conditional("DEBUG")]
        private static void ThrowIfEntityIsMissing(World world, uint entity)
        {
            if (!world.ContainsEntity(entity))
            {
                throw new NullReferenceException($"Entity `{entity}` is missing in `{world}`");
            }
        }

        public static bool operator ==(Entity left, Entity right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Entity left, Entity right)
        {
            return !(left == right);
        }

        public class DebugView
        {
            public readonly bool destroyed;
            public readonly bool enabled;
            public readonly World world;
            public readonly uint value;
            public readonly Entity parent;
            public readonly Entity[] children;
            public readonly Entity[] references;
            public readonly Definition definition;
            public readonly Type[] componentTypes;
            public readonly Type[] arrayTypes;
            public readonly Type[] tagTypes;
            public readonly object[] components;
            public readonly object[][] arrays;
            public readonly StackTrace? creation;

            public DebugView(Entity entity) : this(entity.world, entity.value)
            {
            }

            public DebugView(World world, uint value)
            {
                this.world = world;
                this.value = value;
                destroyed = !world.ContainsEntity(value);
                if (!destroyed)
                {
                    Entity entity = new(world, value);
                    Schema schema = world.Schema;
#if DEBUG
                    World.createStackTraces.TryGetValue(entity, out creation);
#endif
                    enabled = world.IsEnabled(value);
                    uint parent = world.GetParent(value);
                    if (parent != default)
                    {
                        this.parent = new Entity(world, parent);
                    }
                    else
                    {
                        this.parent = default;
                    }

                    Span<uint> children = stackalloc uint[world.GetChildCount(value)];
                    world.CopyChildrenTo(value, children);
                    this.children = new Entity[children.Length];
                    for (int i = 0; i < children.Length; i++)
                    {
                        this.children[i] = new Entity(world, children[i]);
                    }

                    ReadOnlySpan<uint> references = world.GetReferences(value);
                    this.references = new Entity[references.Length];
                    for (int i = 0; i < references.Length; i++)
                    {
                        this.references[i] = new Entity(world, references[i]);
                    }

                    Chunk chunk = world.GetChunk(value);
                    definition = chunk.Definition;

                    //collect all component, array, tag types, and their objects
                    Span<int> typesBuffer = stackalloc int[BitMask.Capacity];
                    int count = definition.CopyComponentTypesTo(typesBuffer);
                    components = new object[count];
                    componentTypes = new Type[count];
                    for (int i = 0; i < count; i++)
                    {
                        int componentType = typesBuffer[i];
                        components[i] = world.GetComponentObject(value, componentType);
                        componentTypes[i] = schema.GetComponentLayout(componentType).SystemType;
                    }

                    count = definition.CopyArrayTypesTo(typesBuffer);
                    arrays = new object[count][];
                    arrayTypes = new Type[count];
                    for (int i = 0; i < count; i++)
                    {
                        int arrayType = typesBuffer[i];
                        arrays[i] = world.GetArrayObject(value, arrayType);
                        arrayTypes[i] = schema.GetArrayLayout(arrayType).SystemType;
                    }

                    count = definition.CopyTagTypesTo(typesBuffer);
                    tagTypes = new Type[count];
                    for (int i = 0; i < count; i++)
                    {
                        int tagType = typesBuffer[i];
                        tagTypes[i] = schema.GetTagLayout(tagType).SystemType;
                    }
                }
                else
                {
                    definition = default;
                    parent = default;
                    children = new Entity[] { };
                    references = new Entity[] { };
                    componentTypes = new Type[] { };
                    arrayTypes = new Type[] { };
                    tagTypes = new Type[] { };
                    components = new object[] { };
                    arrays = new object[][] { };
                }
            }
        }
    }
}