using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Unmanaged;
using Worlds.Unsafe;

namespace Worlds
{
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

        /// <summary>
        /// Is this entity enabled?
        /// </summary>
        public readonly bool IsEnabled
        {
            get => world.IsEnabled(value);
            set => world.SetEnabled(this.value, value);
        }

        /// <summary>
        /// Parent entity of this entity.
        /// <para>
        /// May be <c>default</c> if no parent is set.
        /// </para>
        /// </summary>
        public readonly Entity Parent
        {
            get
            {
                ThrowIfDestroyed();

                uint parent = world.GetParent(value);
                return parent == default ? default : new(world, parent);
            }
            set
            {
                ThrowIfDestroyed();

                world.SetParent(this.value, value.value);
            }
        }

        /// <summary>
        /// Children entities of this entity.
        /// </summary>
        public readonly USpan<uint> Children
        {
            get
            {
                ThrowIfDestroyed();

                return world.GetChildren(value);
            }
        }

        readonly World IEntity.World => world;
        readonly uint IEntity.Value => value;
        readonly Definition IEntity.Definition => new();

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

        /// <summary>
        /// Throws an <see cref="InvalidOperationException"/> if the entity is destroyed.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        [Conditional("DEBUG")]
        public readonly void ThrowIfDestroyed()
        {
            if (this.IsDestroyed())
            {
                throw new InvalidOperationException($"Entity `{value}` is destroyed and no longer available");
            }
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
            if (this.IsDestroyed())
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
            ThrowIfDestroyed();

            return world.GetArray<T>(value);
        }

        /// <summary>
        /// Retrieves an element at <paramref name="index"/> from the array of type <typeparamref name="T"/> on this entity.
        /// </summary>
        public readonly ref T GetArrayElementRef<T>(uint index) where T : unmanaged
        {
            ThrowIfDestroyed();

            return ref world.GetArrayElementRef<T>(value, index);
        }

        /// <summary>
        /// Retrieves the length of the array of type <typeparamref name="T"/> on this entity.
        /// </summary>
        public readonly uint GetArrayLength<T>() where T : unmanaged
        {
            ThrowIfDestroyed();

            return world.GetArrayLength<T>(value);
        }

        /// <summary>
        /// Copies all <see cref="ArrayType"/>s this entity has to the given <paramref name="buffer"/>.
        /// </summary>
        public readonly byte CopyArrayTypesTo(USpan<ArrayType> buffer)
        {
            ThrowIfDestroyed();

            return world.CopyArrayTypesTo(value, buffer);
        }

        /// <summary>
        /// Checks if this entity has an array of type <typeparamref name="T"/>.
        /// </summary>
        public readonly bool ContainsArray<T>() where T : unmanaged
        {
            ThrowIfDestroyed();

            return world.ContainsArray<T>(value);
        }

        /// <summary>
        /// Checks if this entity has an array of the given <paramref name="arrayType"/>.
        /// </summary>
        public readonly bool ContainsArray(ArrayType arrayType)
        {
            ThrowIfDestroyed();

            return world.ContainsArray(value, arrayType);
        }

        /// <summary>
        /// Destroys the array of type <typeparamref name="T"/> on this entity.
        /// </summary>
        public readonly void DestroyArray<T>() where T : unmanaged
        {
            ThrowIfDestroyed();

            world.DestroyArray<T>(value);
        }

        /// <summary>
        /// Creates a new uninitialized array of type <typeparamref name="T"/>.
        /// </summary>
        public readonly USpan<T> CreateArray<T>(uint length = 0) where T : unmanaged
        {
            ThrowIfDestroyed();

            return world.CreateArray<T>(value, length);
        }

        /// <summary>
        /// Creates a new array of type <typeparamref name="T"/> with the given <paramref name="values"/>.
        /// </summary>
        public readonly void CreateArray<T>(USpan<T> values) where T : unmanaged
        {
            ThrowIfDestroyed();

            world.CreateArray(value, values);
        }

        /// <summary>
        /// Resizes the array of type <typeparamref name="T"/> to the given <paramref name="newLength"/>.
        /// </summary>
        /// <returns>Newly resized array.</returns>
        public readonly USpan<T> ResizeArray<T>(uint newLength) where T : unmanaged
        {
            ThrowIfDestroyed();

            return world.ResizeArray<T>(value, newLength);
        }

        /// <summary>
        /// Attempts to retrieve an array of type <typeparamref name="T"/> on this entity.
        /// </summary>
        public readonly bool TryGetArray<T>(out USpan<T> values) where T : unmanaged
        {
            ThrowIfDestroyed();

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
        /// Attempts to retrieve the parent of this entity.
        /// </summary>
        public readonly bool TryGetParent(out uint parent)
        {
            ThrowIfDestroyed();

            parent = world.GetParent(value);
            return parent != default;
        }

        /// <summary>
        /// Checks if this entity has a component of type <typeparamref name="T"/>.
        /// </summary>
        public readonly bool ContainsComponent<T>() where T : unmanaged
        {
            ThrowIfDestroyed();

            return world.ContainsComponent<T>(value);
        }

        /// <summary>
        /// Checks if this entity has a component of the given <paramref name="componentType"/>.
        /// </summary>
        public readonly bool ContainsComponent(ComponentType componentType)
        {
            ThrowIfDestroyed();

            return world.ContainsComponent(value, componentType);
        }

        /// <summary>
        /// Copies all <see cref="ComponentType"/>s this entity has to the given <paramref name="buffer"/>.
        /// </summary>
        public readonly byte CopyComponentTypesTo(USpan<ComponentType> buffer)
        {
            ThrowIfDestroyed();

            return world.CopyComponentTypesTo(value, buffer);
        }

        /// <summary>
        /// Checks if this entity has a component of type <typeparamref name="T"/>.
        /// </summary>
        public readonly T GetComponent<T>() where T : unmanaged
        {
            ThrowIfDestroyed();

            return world.GetComponent<T>(value);
        }

        /// <summary>
        /// Retrieves a reference for the component of type <typeparamref name="T"/>.
        /// </summary>
        public readonly ref T GetComponentRef<T>() where T : unmanaged
        {
            ThrowIfDestroyed();

            return ref world.GetComponentRef<T>(value);
        }

        /// <summary>
        /// Adds a new component of type <typeparamref name="T"/> to the entity.
        /// </summary>
        /// <returns>Reference to the added component.</returns>
        public readonly ref T AddComponentRef<T>() where T : unmanaged
        {
            ThrowIfDestroyed();

            return ref world.AddComponentRef<T>(value);
        }

        /// <summary>
        /// Attempts to retrieve a reference for the component of type <typeparamref name="T"/>.
        /// </summary>
        /// <returns>Reference to the found component if <paramref name="contains"/> is <c>true</c>.</returns>
        public readonly ref T TryGetComponentRef<T>(out bool contains) where T : unmanaged
        {
            ThrowIfDestroyed();

            return ref world.TryGetComponentRef<T>(value, out contains);
        }

        /// <summary>
        /// Attempts to retrieve a reference for the component of type <typeparamref name="T"/>.
        /// </summary>
        /// <returns><c>true</c> if found.</returns>
        public readonly bool TryGetComponent<T>(out T component) where T : unmanaged
        {
            ThrowIfDestroyed();

            return world.TryGetComponent(value, out component);
        }

        /// <summary>
        /// Retrieves an existing component of type <typeparamref name="T"/>, or the default value.
        /// </summary>
        public readonly T GetComponent<T>(T defaultValue) where T : unmanaged
        {
            ThrowIfDestroyed();

            return world.GetComponent(value, defaultValue);
        }

        /// <summary>
        /// Retrieves the bytes of this entity's component of type <paramref name="componentType"/>.
        /// </summary>
        public readonly USpan<byte> GetComponentBytes(ComponentType componentType)
        {
            ThrowIfDestroyed();

            return world.GetComponentBytes(value, componentType);
        }

        /// <summary>
        /// Assigns the given value to the entity's component of type <typeparamref name="T"/>.
        /// </summary>
        public readonly void SetComponent<T>(T component) where T : unmanaged
        {
            ThrowIfDestroyed();

            world.SetComponent(value, component);
        }

        /// <summary>
        /// Adds a new <typeparamref name="T"/> component to the entity.
        /// </summary>
        public readonly void AddComponent<T>() where T : unmanaged
        {
            ThrowIfDestroyed();

            world.AddComponent<T>(value);
        }

        /// <summary>
        /// Adds a new <typeparamref name="T"/> component to the entity.
        /// </summary>
        public readonly void AddComponent<T>(T component) where T : unmanaged
        {
            ThrowIfDestroyed();

            world.AddComponent(value, component);
        }

        /// <summary>
        /// Adds a new component of the given <paramref name="componentType"/>.
        /// </summary>
        public readonly void AddComponent(ComponentType componentType)
        {
            ThrowIfDestroyed();

            world.AddComponent(value, componentType);
        }

        /// <summary>
        /// Removes the component of type <typeparamref name="T"/> from the entity.
        /// </summary>
        public readonly void RemoveComponent<T>() where T : unmanaged
        {
            ThrowIfDestroyed();

            world.RemoveComponent<T>(value);
        }

        /// <summary>
        /// Removes the component of the given <paramref name="componentType"/> from the entity.
        /// </summary>
        public readonly void RemoveComponent(ComponentType componentType)
        {
            ThrowIfDestroyed();

            world.RemoveComponent(value, componentType);
        }

        /// <summary>
        /// Removes the component of type <typeparamref name="T"/> from the entity.
        /// </summary>
        public readonly void RemoveComponent<T>(out T removedComponent) where T : unmanaged
        {
            ThrowIfDestroyed();

            removedComponent = GetComponentRef<T>();
            world.RemoveComponent<T>(value);
        }

        /// <summary>
        /// Adds a local reference to the <paramref name="otherEntity"/>.
        /// </summary>
        public readonly rint AddReference(uint otherEntity)
        {
            ThrowIfDestroyed();

            return world.AddReference(value, otherEntity);
        }

        /// <summary>
        /// Adds a local reference to the <paramref name="otherEntity"/>.
        /// </summary>
        public readonly rint AddReference<T>(T otherEntity) where T : unmanaged, IEntity
        {
            ThrowIfDestroyed();

            return AddReference(otherEntity.Value);
        }

        /// <summary>
        /// Retrieves the entity from the given local <paramref name="reference"/>.
        /// </summary>
        public readonly uint GetReference(rint reference)
        {
            ThrowIfDestroyed();

            return world.GetReference(value, reference);
        }

        /// <summary>
        /// Retrieves the entity from the given local <paramref name="reference"/>.
        /// </summary>
        public readonly T GetReference<T>(rint reference) where T : unmanaged, IEntity
        {
            ThrowIfDestroyed();

            return new Entity(world, GetReference(reference)).As<T>();
        }

        /// <summary>
        /// Retrieves how many references this entity has.
        /// </summary>
        public readonly uint GetReferenceCount()
        {
            ThrowIfDestroyed();

            return world.GetReferenceCount(value);
        }

        /// <summary>
        /// Reassigns an existing local reference to the <paramref name="otherEntity"/>.
        /// </summary>
        public readonly void SetReference(rint reference, uint otherEntity)
        {
            ThrowIfDestroyed();

            world.SetReference(value, reference, otherEntity);
        }

        /// <summary>
        /// Reassigns an existing local reference to the <paramref name="otherEntity"/>.
        /// </summary>
        public readonly void SetReference<T>(rint reference, T otherEntity) where T : unmanaged, IEntity
        {
            ThrowIfDestroyed();

            SetReference(reference, otherEntity.Value);
        }

        /// <summary>
        /// Checks if this entity contains the given local <paramref name="reference"/>.
        /// </summary>
        public readonly bool ContainsReference(rint reference)
        {
            ThrowIfDestroyed();

            return world.ContainsReference(value, reference);
        }

        /// <summary>
        /// Attempts to retrieve an entity from the given local <paramref name="reference"/>.
        /// </summary>
        public readonly bool TryGetReference(rint reference, out uint otherEntity)
        {
            ThrowIfDestroyed();

            return world.TryGetReference(value, reference, out otherEntity);
        }

        /// <summary>
        /// Attempts to retrieve an entity from the given local <paramref name="reference"/>.
        /// </summary>
        /// <returns><c>true</c> if reference is found.</returns>
        public readonly bool TryGetReference<T>(rint reference, out T otherEntity) where T : unmanaged, IEntity
        {
            ThrowIfDestroyed();

            if (TryGetReference(reference, out uint otherValue))
            {
                otherEntity = new Entity(world, otherValue).As<T>();
                return true;
            }
            else
            {
                otherEntity = default;
                return false;
            }
        }

        /// <summary>
        /// Interprets the entity as <typeparamref name="T"/>.
        /// </summary>
        public readonly unsafe T As<T>() where T : unmanaged, IEntity
        {
            ThrowIfTypeLayoutMismatches<T>();

            Entity self = this;
            return *(T*)&self;
        }

        /// <summary>
        /// Adds missing components and arrays that qualify the entity
        /// to be what <see cref="Definition"/> of type <typeparamref name="T"/> argues.
        /// </summary>
        public readonly T Become<T>() where T : unmanaged, IEntity
        {
            this.Become(Definition.Get<T>());
            return As<T>();
        }

        /// <summary>
        /// Checks if this entity complies with the <see cref="Definition"/> of type <typeparamref name="T"/>.
        /// </summary>
        public readonly bool Is<T>() where T : unmanaged, IEntity
        {
            Definition definition = default(T).Definition;
            return this.Is(definition);
        }

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj)
        {
            return obj is Entity entity && Equals(entity);
        }

        /// <inheritdoc/>
        public readonly bool Equals(Entity other)
        {
            return value == other.value && world.Equals(other.world);
        }

        /// <inheritdoc/>
        public readonly override int GetHashCode()
        {
            return HashCode.Combine(value, world);
        }

        /// <summary>
        /// Throws if the given type doesnt have a similar enough layout to <see cref="Entity"/>.
        /// Because the methods that use will perform native ruinterprets.
        /// </summary>
        [Conditional("DEBUG")]
        public static void ThrowIfTypeLayoutMismatches<T>()
        {
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            Stack<Type> checkStack = new();
            Type type = typeof(T);
            checkStack.Push(type);
            while (checkStack.Count > 0)
            {
                Type checkingType = checkStack.Pop();
                if (checkingType == typeof(Entity))
                {
                    return;
                }
                else if (typeof(IEntity).IsAssignableFrom(checkingType))
                {
#pragma warning disable IL2075
                    FieldInfo[] checkingFields = checkingType.GetFields(flags);
#pragma warning restore IL2075
                    if (checkingFields.Length == 1)
                    {
                        checkStack.Push(checkingFields[0].FieldType);
                    }
                    else if (checkingFields.Length == 2)
                    {
                        Type first = checkingFields[0].FieldType;
                        Type second = checkingFields[1].FieldType;
                        if (first == typeof(uint) && second == typeof(World))
                        {
                            return;
                        }
                        else
                        {
                            throw new Exception($"Unexpected entity type layout in `{checkingType}`. Was expecting `uint`, then `{nameof(World)}`");
                        }
                    }
                }
            }

            throw new Exception($"The type `{type}` does not align with the `{nameof(Entity)}` type");
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

        internal class EntityDebugView
        {
#if DEBUG
            public readonly uint value;
            public readonly World world;
            public readonly StackTrace creationStackTrace;
            public readonly ComponentType[] componentTypes;
            public readonly ArrayType[] arrayTypes;

            public EntityDebugView(Entity entity)
            {
                value = entity.GetEntityValue();
                world = entity.GetWorld();
                creationStackTrace = UnsafeWorld.createStackTraces[entity];
                USpan<ComponentType> componentTypeBuffer = stackalloc ComponentType[BitSet.Capacity];
                uint bufferLength = entity.CopyComponentTypesTo(componentTypeBuffer);
                componentTypes = componentTypeBuffer.Slice(0, bufferLength).ToArray();
                USpan<ArrayType> arrayTypeBuffer = stackalloc ArrayType[BitSet.Capacity];
                bufferLength = entity.CopyArrayTypesTo(arrayTypeBuffer);
                arrayTypes = arrayTypeBuffer.Slice(0, bufferLength).ToArray();
            }
#endif
        }
    }
}
