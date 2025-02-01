using System;
using System.Diagnostics;
using Unmanaged;

namespace Worlds
{
    public readonly struct EntityNew
    {
        public readonly uint value;
        public readonly World world;

        public EntityNew(World world, uint value)
        {
            this.world = world;
            this.value = value;
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
    }

    /// <summary>
    /// Represents an entity in the simulation relative to a <see cref="World"/>.
    /// </summary>
    [DebuggerTypeProxy(typeof(EntityDebugView))]
    public readonly struct Entity : IEntity, IEquatable<Entity>
    {
        /// <summary>
        /// The entity's unique identifier.
        /// </summary>
        public readonly uint value;

        /// <summary>
        /// The world this entity belongs to.
        /// </summary>
        public readonly World world;

        readonly World IEntity.World => world;
        readonly uint IEntity.Value => value;

        readonly void IEntity.Describe(ref Archetype archetype)
        {
        }

#if NET
        /// <summary>
        /// Not suported.
        /// </summary>
        [Obsolete("Default constructor not supported", true)]
        public Entity()
        {
            throw new NotSupportedException();
        }
#endif

        /// <summary>
        /// Initializes an existing value with the given <paramref name="existingEntity"/>.
        /// </summary>
        public Entity(World world, uint existingEntity)
        {
            value = existingEntity;
            this.world = world;
        }

        /// <summary>
        /// Creates a new entity in the given <paramref name="world"/>.
        /// </summary>
        public Entity(World world)
        {
            this.world = world;
            value = world.CreateEntity();
        }

        /// <summary>
        /// Destroys the entity.
        /// </summary>
        public readonly void Dispose()
        {
            world.DestroyEntity(value);
        }

        /// <inheritdoc/>
        public unsafe readonly override string ToString()
        {
            USpan<char> buffer = stackalloc char[32];
            uint length = ToString(buffer);
            return buffer.Slice(0, length).ToString();
        }

        /// <summary>
        /// Builds a string representation of this entity.
        /// </summary>
        public readonly uint ToString(USpan<char> buffer)
        {
            uint length = 0;
            if (world == default)
            {
                buffer[length++] = 'O';
                buffer[length++] = 'r';
                buffer[length++] = 'p';
                buffer[length++] = 'h';
                buffer[length++] = 'a';
                buffer[length++] = 'n';
                buffer[length++] = ' ';
            }
            else if (!world.ContainsEntity(value))
            {
                buffer[length++] = 'D';
                buffer[length++] = 'e';
                buffer[length++] = 's';
                buffer[length++] = 't';
                buffer[length++] = 'r';
                buffer[length++] = 'o';
                buffer[length++] = 'y';
                buffer[length++] = 'e';
                buffer[length++] = 'd';
                buffer[length++] = ' ';
            }

            length += value.ToString(buffer.Slice(length));
            return length;
        }

        /// <summary>
        /// Retrieves an array of type <typeparamref name="T"/> on this entity.
        /// </summary>
        public readonly USpan<T> GetArray<T>() where T : unmanaged
        {
            return world.GetArray<T>(value);
        }

        /// <summary>
        /// Retrieves an element at <paramref name="index"/> from the array of type <typeparamref name="T"/> on this entity.
        /// </summary>
        public readonly ref T GetArrayElement<T>(uint index) where T : unmanaged
        {
            return ref world.GetArrayElement<T>(value, index);
        }

        /// <summary>
        /// Retrieves the length of the array of type <typeparamref name="T"/> on this entity.
        /// </summary>
        public readonly uint GetArrayLength<T>() where T : unmanaged
        {
            return world.GetArrayLength<T>(value);
        }

        /// <summary>
        /// Checks if this entity has an array of type <typeparamref name="T"/>.
        /// </summary>
        public readonly bool ContainsArray<T>() where T : unmanaged
        {
            return world.ContainsArray<T>(value);
        }

        /// <summary>
        /// Destroys the array of type <typeparamref name="T"/> on this entity.
        /// </summary>
        public readonly void DestroyArray<T>() where T : unmanaged
        {
            world.DestroyArray<T>(value);
        }

        /// <summary>
        /// Creates a new uninitialized array of type <typeparamref name="T"/>.
        /// </summary>
        public readonly USpan<T> CreateArray<T>(uint length = 0) where T : unmanaged
        {
            return world.CreateArray<T>(value, length);
        }

        /// <summary>
        /// Creates a new array of type <typeparamref name="T"/> with the given <paramref name="values"/>.
        /// </summary>
        public readonly void CreateArray<T>(USpan<T> values) where T : unmanaged
        {
            world.CreateArray(value, values);
        }

        /// <summary>
        /// Resizes the array of type <typeparamref name="T"/> to the given <paramref name="newLength"/>.
        /// </summary>
        /// <returns>Newly resized array.</returns>
        public readonly USpan<T> ResizeArray<T>(uint newLength) where T : unmanaged
        {
            return world.ResizeArray<T>(value, newLength);
        }

        /// <summary>
        /// Changes the length of the existing <typeparamref name="T"/> array.
        /// </summary>
        public readonly USpan<T> ExpandArray<T>(int lengthDelta) where T : unmanaged
        {
            return world.ExpandArray<T>(value, lengthDelta);
        }

        /// <summary>
        /// Increases the length of the existing <typeparamref name="T"/> array.
        /// </summary>
        public readonly USpan<T> ExpandArray<T>(uint increment) where T : unmanaged
        {
            return world.ExpandArray<T>(value, increment);
        }

        /// <summary>
        /// Attempts to retrieve an array of type <typeparamref name="T"/> on this entity.
        /// </summary>
        public readonly bool TryGetArray<T>(out USpan<T> values) where T : unmanaged
        {
            if (ContainsArray<T>())
            {
                values = GetArray<T>();
                return true;
            }
            else
            {
                values = default;
                return false;
            }
        }

        /// <summary>
        /// Checks if this entity has a component of type <typeparamref name="T"/>.
        /// </summary>
        public readonly bool ContainsComponent<T>() where T : unmanaged
        {
            return world.ContainsComponent<T>(value);
        }

        /// <summary>
        /// Retrieves a reference for the component of type <typeparamref name="T"/>.
        /// </summary>
        public readonly ref T GetComponent<T>() where T : unmanaged
        {
            return ref world.GetComponent<T>(value);
        }

        /// <summary>
        /// Adds a new component of type <typeparamref name="T"/> to the entity.
        /// </summary>
        /// <returns>Reference to the added component.</returns>
        public readonly ref T AddComponent<T>() where T : unmanaged
        {
            return ref world.AddComponent<T>(value);
        }

        /// <summary>
        /// Attempts to retrieve a reference for the component of type <typeparamref name="T"/>.
        /// </summary>
        /// <returns>Reference to the found component if <paramref name="contains"/> is <c>true</c>.</returns>
        public readonly ref T TryGetComponent<T>(out bool contains) where T : unmanaged
        {
            return ref world.TryGetComponent<T>(value, out contains);
        }

        /// <summary>
        /// Attempts to retrieve the component value of type <typeparamref name="T"/>.
        /// </summary>
        /// <returns><c>true</c> if found.</returns>
        public readonly bool TryGetComponent<T>(out T component) where T : unmanaged
        {
            return world.TryGetComponent(value, out component);
        }

        /// <summary>
        /// Retrieves an existing component of type <typeparamref name="T"/>, or the default value.
        /// </summary>
        public readonly T GetComponent<T>(T defaultValue) where T : unmanaged
        {
            return world.GetComponent(value, defaultValue);
        }

        /// <summary>
        /// Assigns the given value to the entity's component of type <typeparamref name="T"/>.
        /// </summary>
        public readonly void SetComponent<T>(T component) where T : unmanaged
        {
            world.SetComponent(value, component);
        }

        /// <summary>
        /// Adds a new <typeparamref name="T"/> component to the entity.
        /// </summary>
        public readonly ref T AddComponent<T>(T component) where T : unmanaged
        {
            return ref world.AddComponent(value, component);
        }

        /// <summary>
        /// Removes the component of type <typeparamref name="T"/> from the entity.
        /// </summary>
        public readonly void RemoveComponent<T>() where T : unmanaged
        {
            world.RemoveComponent<T>(value);
        }

        public readonly void AddTag<T>() where T : unmanaged
        {
            world.AddTag<T>(value);
        }

        /// <summary>
        /// Interprets the entity as <typeparamref name="T"/>.
        /// </summary>
        public readonly unsafe T As<T>() where T : unmanaged, IEntity
        {
            EntityExtensions.ThrowIfTypeLayoutMismatches<T>();

            Entity self = this;
            return *(T*)&self;
        }

        /// <summary>
        /// Adds missing components and arrays that qualify the entity
        /// to be what <see cref="Definition"/> of type <typeparamref name="T"/> argues.
        /// </summary>
        public readonly T Become<T>() where T : unmanaged, IEntity
        {
            Schema schema = world.Schema;
            this.Become(Archetype.Get<T>(schema));
            return As<T>();
        }

        /// <summary>
        /// Checks if this entity complies with the <see cref="Definition"/> of type <typeparamref name="T"/>.
        /// </summary>
        public readonly bool Is<T>() where T : unmanaged, IEntity
        {
            Schema schema = world.Schema;
            return this.Is(Archetype.Get<T>(schema).Definition);
        }

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj)
        {
            return obj is Entity entity && Equals(entity);
        }

        /// <inheritdoc/>
        public readonly bool Equals(Entity other)
        {
            return world.Address * value == other.world.Address * other.value;
        }

        /// <inheritdoc/>
        public readonly override int GetHashCode()
        {
            return ((int)value * 397) ^ world.GetHashCode();
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

        public static implicit operator uint(Entity entity)
        {
            return entity.value;
        }

        internal class EntityDebugView
        {
#if DEBUG
            public readonly uint value;
            public readonly World world;
            public readonly Entity parent;
            public readonly Entity[] children;
            public readonly bool enabled;
            public readonly StackTrace creationStackTrace;
            public readonly object[] components;
            public readonly object[][] arrays;
            public readonly Type[] tags;
            public readonly Entity[] references;

            public EntityDebugView(Entity entity)
            {
                value = entity.GetEntityValue();
                world = entity.GetWorld();
                enabled = entity.IsEnabled();
                parent = entity.GetParent();
                creationStackTrace = World.Implementation.createStackTraces[entity];

                Schema schema = world.Schema;
                USpan<ComponentType> componentTypeBuffer = stackalloc ComponentType[BitMask.Capacity];
                uint bufferLength = entity.CopyComponentTypesTo(componentTypeBuffer);
                components = new object[bufferLength];
                for (uint i = 0; i < bufferLength; i++)
                {
                    components[i] = world.GetComponentObject(entity, componentTypeBuffer[i]);
                }

                USpan<ArrayElementType> arrayElementTypeBuffer = stackalloc ArrayElementType[BitMask.Capacity];
                bufferLength = entity.CopyArrayElementTypesTo(arrayElementTypeBuffer);
                arrays = new object[bufferLength][];
                for (uint i = 0; i < bufferLength; i++)
                {
                    arrays[i] = world.GetArrayObject(entity, arrayElementTypeBuffer[i]);
                }

                USpan<TagType> tagTypeBuffer = stackalloc TagType[BitMask.Capacity];
                bufferLength = entity.CopyTagTypesTo(tagTypeBuffer);
                tags = new Type[bufferLength];
                for (uint i = 0; i < bufferLength; i++)
                {
                    tags[i] = tagTypeBuffer[i].GetLayout(schema).SystemType;
                }

                references = new Entity[entity.GetReferenceCount()];
                for (ushort i = 0; i < references.Length; i++)
                {
                    rint reference = (rint)(i + 1u);
                    references[i] = new(world, entity.GetReference(reference));
                }

                USpan<uint> children = entity.GetChildren();
                this.children = new Entity[children.Length];
                for (uint i = 0; i < children.Length; i++)
                {
                    this.children[i] = new(world, children[i]);
                }
            }
#endif
        }
    }
}
