using System;
using System.Diagnostics;

namespace Worlds
{
    /// <summary>
    /// A wrapper of an existing entity in a <see cref="World"/>.
    /// </summary>
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
        /// Retrieves the amount of references this entity has.
        /// </summary>
        public readonly int ReferenceCount => world.GetReferenceCount(value);

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
        /// <inheritdoc/>
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

        /// <summary>
        /// Destroys the entity.
        /// </summary>
        public readonly void Dispose()
        {
            world.DestroyEntity(value);
        }

        /// <summary>
        /// Assigns the parent of this entity to <paramref name="otherEntity"/>.
        /// </summary>
        public readonly bool SetParent(uint otherEntity)
        {
            return world.SetParent(value, otherEntity);
        }

        /// <summary>
        /// Assigns the parent of this entity to <paramref name="otherEntity"/>.
        /// </summary>
        public readonly bool SetParent<T>(T otherEntity) where T : unmanaged, IEntity
        {
            return world.SetParent(value, otherEntity.GetEntityValue());
        }

        /// <summary>
        /// Tries to retrieve the parent of this entity.
        /// </summary>
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

        /// <summary>
        /// Copies all children of this entity to the <paramref name="destination"/> span.
        /// </summary>
        public readonly int CopyChildrenTo(Span<uint> destination)
        {
            return world.CopyChildrenTo(value, destination);
        }

        /// <summary>
        /// Retrieves the definition of this entity.
        /// </summary>
        public readonly Definition GetDefinition()
        {
            return world.GetDefinition(value);
        }

        /// <summary>
        /// Checks if this entity complies with another entity of type <typeparamref name="T"/>
        /// </summary>
        public unsafe readonly bool Is<T>() where T : unmanaged, IEntity
        {
            Archetype archetype = Archetype.Get<T>(world.world->schema);
            return world.Is(value, archetype);
        }

        /// <summary>
        /// Checks if this entity complies with the specified <paramref name="definition"/>.
        /// </summary>
        public readonly bool Is(Definition definition)
        {
            return world.Is(value, definition);
        }

        /// <summary>
        /// Checks if this entity complies with the specified <paramref name="archetype"/>.
        /// </summary>
        public readonly bool Is(Archetype archetype)
        {
            return world.Is(value, archetype);
        }

        /// <summary>
        /// Makes this entity become another entity of type <typeparamref name="T"/>.
        /// </summary>
        public unsafe readonly T Become<T>() where T : unmanaged, IEntity
        {
            Archetype archetype = Archetype.Get<T>(world.world->schema);
            world.Become(value, archetype);
            return new Entity(world, value).As<T>();
        }

        /// <summary>
        /// Makes this entity comply with the given <paramref name="definition"/>,
        /// by adding the missing components, arrays and tags.
        /// </summary>
        public readonly void Become(Definition definition)
        {
            world.Become(value, definition);
        }

        /// <summary>
        /// Makes this entity comply with the given <paramref name="archetype"/>,
        /// by adding the missing components, arrays and tags.
        /// </summary>
        public readonly void Become(Archetype archetype)
        {
            world.Become(value, archetype);
        }

        /// <summary>
        /// Casts this entity to another entity of type <typeparamref name="T"/>.
        /// </summary>
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

        /// <summary>
        /// Adds the <paramref name="otherEntity"/> as a new reference.
        /// </summary>
        public readonly rint AddReference<T>(T otherEntity) where T : unmanaged, IEntity
        {
            return world.AddReference(value, otherEntity.GetEntityValue());
        }

        /// <summary>
        /// Checks if this entity contains a reference to the <paramref name="otherEntity"/>.
        /// </summary>
        public readonly bool ContainsReference(uint otherEntity)
        {
            return world.ContainsReference(value, otherEntity);
        }

        /// <summary>
        /// Checks if this entity contains a referenced entity at <paramref name="reference"/>.
        /// </summary>
        public readonly bool ContainsReference(rint reference)
        {
            return world.ContainsReference(value, reference);
        }

        /// <summary>
        /// Checks if this entity contains a reference to the <paramref name="otherEntity"/>.
        /// </summary>
        public readonly bool ContainsReference<T>(T otherEntity) where T : unmanaged, IEntity
        {
            return world.ContainsReference(value, otherEntity.GetEntityValue());
        }

        /// <summary>
        /// Removes a reference to the <paramref name="otherEntity"/>.
        /// </summary>
        public readonly void RemoveReference(uint otherEntity)
        {
            world.RemoveReference(value, otherEntity);
        }

        /// <summary>
        /// Removes a reference to the <paramref name="otherEntity"/>.
        /// </summary>
        public readonly void RemoveReference(uint otherEntity, out rint removedReference)
        {
            world.RemoveReference(value, otherEntity, out removedReference);
        }

        /// <summary>
        /// Removes a reference to another entity at <paramref name="reference"/>.
        /// </summary>
        public readonly void RemoveReference(rint reference)
        {
            world.RemoveReference(value, reference);
        }

        /// <summary>
        /// Removes a reference to another entity at <paramref name="reference"/>.
        /// </summary>
        public readonly void RemoveReference(rint reference, out uint referencedEntity)
        {
            world.RemoveReference(value, reference, out referencedEntity);
        }

        /// <summary>
        /// Removes a reference to the <paramref name="otherEntity"/>.
        /// </summary>
        public readonly void RemoveReference<T>(T otherEntity) where T : unmanaged, IEntity
        {
            world.RemoveReference(value, otherEntity.GetEntityValue());
        }

        /// <summary>
        /// Removes a reference to the <paramref name="otherEntity"/>.
        /// </summary>
        public readonly void RemoveReference<T>(T otherEntity, out rint removedReference) where T : unmanaged, IEntity
        {
            world.RemoveReference(value, otherEntity.GetEntityValue(), out removedReference);
        }

        /// <summary>
        /// Retrieves the referenced entity at <paramref name="reference"/>.
        /// </summary>
        public readonly uint GetReference(rint reference)
        {
            return world.GetReference(value, reference);
        }

        /// <summary>
        /// Retrieves the reference index of the <paramref name="otherEntity"/>.
        /// </summary>
        public readonly rint GetReference(uint otherEntity)
        {
            return world.GetReference(value, otherEntity);
        }

        /// <summary>
        /// Retrieves the reference index of the <paramref name="otherEntity"/>.
        /// </summary>
        public readonly rint GetReference<T>(T otherEntity) where T : unmanaged, IEntity
        {
            return world.GetReference(value, otherEntity.GetEntityValue());
        }

        /// <summary>
        /// Assigns the reference at <paramref name="reference"/> to the <paramref name="otherEntity"/>.
        /// </summary>
        public readonly void SetReference(rint reference, uint otherEntity)
        {
            world.SetReference(value, reference, otherEntity);
        }

        /// <summary>
        /// Assigns the reference at <paramref name="reference"/> to the <paramref name="otherEntity"/>.
        /// </summary>
        public readonly void SetReference<T>(rint reference, T otherEntity) where T : unmanaged, IEntity
        {
            world.SetReference(value, reference, otherEntity.GetEntityValue());
        }

        /// <summary>
        /// Tries to retrieve a referenced entity at <paramref name="reference"/>.
        /// </summary>
        public readonly bool TryGetReference(rint reference, out uint otherEntity)
        {
            return world.TryGetReference(value, reference, out otherEntity);
        }

        /// <summary>
        /// Retrieves a complete new clone of this entity.
        /// </summary>
        /// <returns></returns>
        public readonly Entity Clone()
        {
            return new Entity(world, world.CloneEntity(value));
        }

        /// <summary>
        /// Checks if this entity contains the <paramref name="componentType"/>.
        /// </summary>
        public readonly bool ContainsComponent(int componentType)
        {
            return world.ContainsComponent(value, componentType);
        }

        /// <summary>
        /// Checks if this entity contains the specified <paramref name="arrayType"/>.
        /// </summary>
        public readonly bool ContainsArray(int arrayType)
        {
            return world.ContainsArray(value, arrayType);
        }

        /// <summary>
        /// Checks if this entity contains the specified <paramref name="tagType"/>.
        /// </summary>
        public readonly bool ContainsTag(int tagType)
        {
            return world.ContainsTag(value, tagType);
        }

        /// <summary>
        /// Adds the specified <paramref name="componentType"/> to this entity.
        /// </summary>
        public readonly void AddComponentType(int componentType)
        {
            world.AddComponentType(value, componentType);
        }

        /// <summary>
        /// Removes the specified <paramref name="componentType"/> from this entity.
        /// </summary>
        public readonly void RemoveComponent(int componentType)
        {
            world.RemoveComponent(value, componentType);
        }

        /// <summary>
        /// Creates a new array of the specified <paramref name="arrayType"/> and length.
        /// </summary>
        public readonly Values CreateArray(int arrayType, int length = 0)
        {
            return world.CreateArray(value, arrayType, length);
        }

        /// <summary>
        /// Creates a new array of the specified <paramref name="arrayType"/> and length.
        /// </summary>
        public readonly Values CreateArray(DataType arrayType, int length = 0)
        {
            return world.CreateArray(value, arrayType, length);
        }

        /// <summary>
        /// Retrieves an existing array of the specified <paramref name="arrayType"/>.
        /// </summary>x
        public readonly Values GetArray(int arrayType)
        {
            return world.GetArray(value, arrayType);
        }

        /// <summary>
        /// Destroys the existing array of the specified <paramref name="arrayType"/>.
        /// </summary>
        public readonly void DestroyArray(int arrayType)
        {
            world.DestroyArray(value, arrayType);
        }

        /// <summary>
        /// Checks if this entity contains a component of type <typeparamref name="T"/>.
        /// </summary>
        public readonly bool ContainsComponent<T>() where T : unmanaged
        {
            return world.ContainsComponent<T>(value);
        }

        /// <summary>
        /// Retrieves the reference to the component of type <typeparamref name="T"/>.
        /// </summary>
        public readonly ref T GetComponent<T>() where T : unmanaged
        {
            return ref world.GetComponent<T>(value);
        }

        /// <summary>
        /// Tries to retrieve an existing copy of a <typeparamref name="T"/> component.
        /// </summary>
        public readonly bool TryGetComponent<T>(out T component) where T : unmanaged
        {
            return world.TryGetComponent(value, out component);
        }

        /// <summary>
        /// Tries to retrieve the reference to the component of type <typeparamref name="T"/>.
        /// </summary>
        public readonly ref T TryGetComponent<T>(out bool contains) where T : unmanaged
        {
            return ref world.TryGetComponent<T>(value, out contains);
        }

        /// <summary>
        /// Assigns the given <paramref name="component"/>.
        /// </summary>
        public readonly void SetComponent<T>(T component) where T : unmanaged
        {
            world.SetComponent(value, component);
        }

        /// <summary>
        /// Adds a new component of type <typeparamref name="T"/> and retrieves its reference.
        /// </summary>
        public readonly ref T AddComponent<T>() where T : unmanaged
        {
            return ref world.AddComponent<T>(value);
        }

        /// <summary>
        /// Adds a new component of type <typeparamref name="T"/> and retrieves its reference.
        /// </summary>
        public readonly ref T AddComponent<T>(int componentType) where T : unmanaged
        {
            return ref world.AddComponent<T>(value, componentType);
        }

        /// <summary>
        /// Adds the <paramref name="component"/> to the entity.
        /// </summary>
        public readonly void AddComponent<T>(T component) where T : unmanaged
        {
            world.AddComponent(value, component);
        }

        /// <summary>
        /// Adds the <paramref name="component"/> to the entity.
        /// </summary>
        public readonly void AddComponent<T>(int componentType, T component) where T : unmanaged
        {
            world.AddComponent(value, componentType, component);
        }

        /// <summary>
        /// Removes the component of type <typeparamref name="T"/> from this entity.
        /// </summary>
        public readonly void RemoveComponent<T>() where T : unmanaged
        {
            world.RemoveComponent<T>(value);
        }

        /// <summary>
        /// Checks if the entity contains an array of type <typeparamref name="T"/>.
        /// </summary>
        public readonly bool ContainsArray<T>() where T : unmanaged
        {
            return world.ContainsArray<T>(value);
        }

        /// <summary>
        /// Retrieves the length of the existing array of type <typeparamref name="T"/>.
        /// </summary>
        public readonly int GetArrayLength<T>() where T : unmanaged
        {
            return world.GetArrayLength<T>(value);
        }

        /// <summary>
        /// Retrieves a reference to the <typeparamref name="T"/> array element at <paramref name="index"/>.
        /// </summary>
        public readonly ref T GetArrayElement<T>(int index) where T : unmanaged
        {
            return ref world.GetArrayElement<T>(value, index);
        }

        /// <summary>
        /// Retrieves the entire existing array of type <typeparamref name="T"/>.
        /// </summary>
        public readonly Values<T> GetArray<T>() where T : unmanaged
        {
            return world.GetArray<T>(value);
        }

        /// <summary>
        /// Retrieves the entire existing array of type <typeparamref name="T"/>.
        /// </summary>
        public readonly Values<T> GetArray<T>(int arrayType) where T : unmanaged
        {
            return world.GetArray<T>(value, arrayType);
        }

        /// <summary>
        /// Tries to retrieve the array of type <typeparamref name="T"/>.
        /// </summary>
        public readonly bool TryGetArray<T>(out Values<T> array) where T : unmanaged
        {
            return world.TryGetArray(value, out array);
        }

        /// <summary>
        /// Creates a new array with the given <paramref name="elements"/>.
        /// </summary>
        public readonly void CreateArray<T>(ReadOnlySpan<T> elements) where T : unmanaged
        {
            world.CreateArray(value, elements);
        }

        /// <summary>
        /// Creates a new array of type <typeparamref name="T"/> with the specified <paramref name="length"/>.
        /// </summary>
        public readonly Values<T> CreateArray<T>(int length = 0) where T : unmanaged
        {
            return world.CreateArray<T>(value, length);
        }

        /// <summary>
        /// Destroys the array of type <typeparamref name="T"/>.
        /// </summary>
        public readonly void DestroyArray<T>() where T : unmanaged
        {
            world.DestroyArray<T>(value);
        }

        /// <summary>
        /// Checks if this entity contains a tag of type <typeparamref name="T"/>.
        /// </summary>
        public readonly bool ContainsTag<T>() where T : unmanaged
        {
            return world.ContainsTag<T>(value);
        }

        /// <summary>
        /// Adds a new tag of type <typeparamref name="T"/> to this entity.
        /// </summary>
        public readonly void AddTag<T>() where T : unmanaged
        {
            world.AddTag<T>(value);
        }

        /// <summary>
        /// Removes the tag of type <typeparamref name="T"/> from this entity.
        /// </summary>
        public readonly void RemoveTag<T>() where T : unmanaged
        {
            world.RemoveTag<T>(value);
        }

        /// <inheritdoc/>
        public readonly override string ToString()
        {
            return value.ToString();
        }

        /// <inheritdoc/>
        public readonly int ToString(Span<char> destination)
        {
            return value.ToString(destination);
        }

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj)
        {
            return obj is Entity entity && Equals(entity);
        }

        /// <inheritdoc/>
        public readonly bool Equals(Entity other)
        {
            return world == other.world && value == other.value;
        }

        /// <inheritdoc/>
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

        /// <summary>
        /// Retrieves an existing entity of type <typeparamref name="T"/>.
        /// </summary>
        public static T Get<T>(World world, uint value) where T : unmanaged, IEntity
        {
            return new Entity(world, value).As<T>();
        }

        /// <inheritdoc/>
        public static bool operator ==(Entity left, Entity right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(Entity left, Entity right)
        {
            return !(left == right);
        }

        /// <inheritdoc/>
        public class DebugView
        {
            /// <inheritdoc/>
            public readonly bool destroyed;
            /// <inheritdoc/>
            public readonly bool enabled;
            /// <inheritdoc/>
            public readonly World world;
            /// <inheritdoc/>
            public readonly uint value;
            /// <inheritdoc/>
            public readonly Entity parent;
            /// <inheritdoc/>
            public readonly Entity[] children;
            /// <inheritdoc/>
            public readonly Entity[] references;
            /// <inheritdoc/>
            public readonly Definition definition;
            /// <inheritdoc/>
            public readonly Type[] componentTypes;
            /// <inheritdoc/>
            public readonly Type[] arrayTypes;
            /// <inheritdoc/>
            public readonly Type[] tagTypes;
            /// <inheritdoc/>
            public readonly object[] components;
            /// <inheritdoc/>
            public readonly object[][] arrays;
            /// <inheritdoc/>
            public readonly StackTrace? creation;

            /// <inheritdoc/>
            public DebugView(Entity entity) : this(entity.world, entity.value)
            {
            }

            /// <inheritdoc/>
            public unsafe DebugView(World world, uint value)
            {
                this.world = world;
                this.value = value;
                destroyed = !world.ContainsEntity(value);
                if (!destroyed)
                {
                    Entity entity = new(world, value);
                    Schema schema = world.world->schema;
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

                    definition = world.GetDefinition(value);

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