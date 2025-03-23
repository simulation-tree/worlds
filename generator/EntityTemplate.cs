namespace Worlds
{
    public static class EntityTemplate
    {
        public const string Source = @"
/// <inheritdoc/>
[DebuggerTypeProxy(typeof({{TypeName}}.DebugView))]
{{Accessors}} partial struct {{TypeName}} : IEntity{{EntityInterfaces}}
{
    /// <summary>
    /// The world that this entity belongs to.
    /// </summary>
    public readonly World world;

    /// <summary>
    /// The position of this entity in the <see cref=""world""/>.
    /// </summary>
    public readonly uint value;

    /// <summary>
    /// Checks if the entity is destroyed.
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
    /// May be <see langword=""default""/> if none is set.
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
    
    /// <summary>
    /// Checks if this entity complies with the type that it argues.
    /// </summary>
    public readonly bool IsCompliant
    {
        get
        {
            {{ComplianceChecks}}
            return true;
        }
    }

#if NET
    /// <inheritdoc/>
    [Obsolete(""Default constructor not supported"", true)]
    public {{TypeName}}()
    {
        throw new NotSupportedException();
    }
#endif
    {{DisposeMethod}}
    /// <summary>
    /// Assigns the parent of this entity to <paramref name=""otherEntity""/>.
    /// </summary>
    public readonly bool SetParent(uint otherEntity)
    {
        return world.SetParent(value, otherEntity);
    }

    /// <summary>
    /// Assigns the parent of this entity to <paramref name=""otherEntity""/>.
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
        bool hasParent = parentValue != default;
        if (hasParent)
        {
            parent = new Entity(world, parentValue);
        }
        else
        {
            parent = default;
        }

        return hasParent;
    }

    /// <summary>
    /// Copies all children of this entity to the <paramref name=""destination""/> span.
    /// </summary>
    public readonly int CopyChildrenTo(Span<uint> destination)
    {
        return world.CopyChildrenTo(value, destination);
    }

    /// <summary>
    /// Checks if this entity complies with another entity of type <typeparamref name=""T""/>
    /// </summary>
    public readonly bool Is<T>() where T : unmanaged, IEntity
    {
        Archetype archetype = Archetype.Get<T>(world.Schema);
        return world.Is(value, archetype);
    }

    /// <summary>
    /// Checks if this entity complies with the given <paramref name=""definition""/>.
    /// </summary>
    public readonly bool Is(Definition definition)
    {
        return world.Is(value, definition);
    }

    /// <summary>
    /// Checks if this entity complies with the given <paramref name=""archetype""/>.
    /// </summary>
    public readonly bool Is(Archetype archetype)
    {
        return world.Is(value, archetype);
    }

    /// <summary>
    /// Waits until the entity becomes compliant with its own type.
    /// </summary>
    public readonly async Task UntilCompliant(Update update, CancellationToken cancellationToken = default)
    {
        while (!IsCompliant)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await update(world, cancellationToken);
        }
    }

    /// <summary>
    /// Makes this entity become another entity of type <typeparamref name=""T""/>,
    /// by adding the missing components, arrays and tags.
    /// </summary>
    public readonly T Become<T>() where T : unmanaged, IEntity
    {
        Archetype archetype = Archetype.Get<T>(world.Schema);
        world.Become(value, archetype);
        return As<T>();
    }

    /// <summary>
    /// Makes this entity fulfill the requirements of the given <paramref name=""definition""/>.
    /// </summary>
    public readonly void Become(Definition definition)
    {
        world.Become(value, definition);
    }

    /// <summary>
    /// Makes this entity fulfill the requirements of the given <paramref name=""archetype""/>.
    /// </summary>
    public readonly void Become(Archetype archetype)
    {
        world.Become(value, archetype);
    }

    /// <summary>
    /// Retrieves this entity as another entity of type <typeparamref name=""T""/>.
    /// </summary>
    public readonly T As<T>() where T : unmanaged, IEntity
    {
        return new Entity(world, value).As<T>();
    }

    /// <summary>
    /// Adds a reference to the <paramref name=""otherEntity""/>.
    /// </summary>
    public readonly rint AddReference(uint otherEntity)
    {
        return world.AddReference(value, otherEntity);
    }

    /// <summary>
    /// Adds a reference to the <paramref name=""otherEntity""/>.
    /// </summary>
    public readonly rint AddReference<T>(T otherEntity) where T : unmanaged, IEntity
    {
        return world.AddReference(value, otherEntity.GetEntityValue());
    }

    /// <summary>
    /// Checks if this entity references the <paramref name=""otherEntity""/>.
    /// </summary>
    public readonly bool ContainsReference(uint otherEntity)
    {
        return world.ContainsReference(value, otherEntity);
    }

    /// <summary>
    /// Checks if this entity contains the given <paramref name=""reference""/> value.
    /// </summary>
    public readonly bool ContainsReference(rint reference)
    {
        return world.ContainsReference(value, reference);
    }

    /// <summary>
    /// Checks if this entity references the <paramref name=""otherEntity""/>.
    /// </summary>
    public readonly bool ContainsReference<T>(T otherEntity) where T : unmanaged, IEntity
    {
        return world.ContainsReference(value, otherEntity.GetEntityValue());
    }

    /// <summary>
    /// Removes the reference to the <paramref name=""otherEntity""/> from this entity.
    /// </summary>
    public readonly rint RemoveReference(uint otherEntity)
    {
        return world.RemoveReference(value, otherEntity);
    }

    /// <summary>
    /// Removes the given reference <paramref name=""reference""/> from this entity.
    /// </summary>
    public readonly uint RemoveReference(rint reference)
    {
        return world.RemoveReference(value, reference);
    }

    /// <summary>
    /// Removes the reference to the <paramref name=""otherEntity""/> from this entity.
    /// </summary>
    public readonly rint RemoveReference<T>(T otherEntity) where T : unmanaged, IEntity
    {
        return world.RemoveReference(value, otherEntity.GetEntityValue());
    }

    /// <summary>
    /// Retrieves the entity found with the given <paramref name=""reference""/> value.
    /// </summary>
    public readonly uint GetReference(rint reference)
    {
        return world.GetReference(value, reference);
    }

    /// <summary>
    /// Retrieves the reference value of the <paramref name=""otherEntity""/>.
    /// </summary>
    public readonly rint GetReference(uint otherEntity)
    {
        return world.GetReference(value, otherEntity);
    }

    /// <summary>
    /// Retrieves the reference value of the <paramref name=""otherEntity""/>.
    /// </summary>
    public readonly rint GetReference<T>(T otherEntity) where T : unmanaged, IEntity
    {
        return world.GetReference(value, otherEntity.GetEntityValue());
    }

    /// <summary>
    /// Assigns <paramref name=""otherEntity""/> to the given <paramref name=""reference""/> value.
    /// </summary>
    public readonly void SetReference(rint reference, uint otherEntity)
    {
        world.SetReference(value, reference, otherEntity);
    }

    /// <summary>
    /// Assigns <paramref name=""otherEntity""/> to the given <paramref name=""reference""/> value.
    /// </summary>
    public readonly void SetReference<T>(rint reference, T otherEntity) where T : unmanaged, IEntity
    {
        world.SetReference(value, reference, otherEntity.GetEntityValue());
    }

    /// <summary>
    /// Tries to retrieve the entity found with the given <paramref name=""reference""/> value.
    /// </summary>
    public readonly bool TryGetReference(rint reference, out uint otherEntity)
    {
        return world.TryGetReference(value, reference, out otherEntity);
    }

    /// <summary>
    /// Retrieves a complete clone of this entity.
    /// </summary>
    public readonly {{TypeName}} Clone()
    {
        return new Entity(world, world.CloneEntity(value)).As<{{TypeName}}>();
    }

    /// <summary>
    /// Checks if this entity contains the given <paramref name=""componentType""/>.
    /// </summary>
    public readonly bool ContainsComponent(int componentType)
    {
        return world.ContainsComponent(value, componentType);
    }

    /// <summary>
    /// Checks if this entity contains the given <paramref name=""arrayType""/>.
    /// </summary>
    public readonly bool ContainsArray(int arrayType)
    {
        return world.ContainsArray(value, arrayType);
    }

    /// <summary>
    /// Adds the given <paramref name=""componentType""/> to this entity.
    /// </summary>
    public readonly void AddComponentType(int componentType)
    {
        world.AddComponentType(value, componentType);
    }

    /// <summary>
    /// Removes the <paramref name=""componentType""/> from this entity.
    /// </summary>
    public readonly void RemoveComponent(int componentType)
    {
        world.RemoveComponent(value, componentType);
    }

    /// <summary>
    /// Creates a new array of type <paramref name=""arrayType""/> with an optional
    /// custom length.
    /// </summary>
    public readonly Values CreateArray(int arrayType, int length = 0)
    {
        return world.CreateArray(value, arrayType, length);
    }

    /// <summary>
    /// Retrieves the array of type <paramref name=""arrayType""/> from this entity.
    /// </summary>
    public readonly Values GetArray(int arrayType)
    {
        return world.GetArray(value, arrayType);
    }

    /// <summary>
    /// Destroys the array of type <paramref name=""arrayType""/> from this entity.
    /// </summary>
    public readonly void DestroyArray(int arrayType)
    {
        world.DestroyArray(value, arrayType);
    }

    /// <summary>
    /// Adds the given <paramref name=""tagType""/> to this entity.
    /// </summary>
    public readonly void AddTag(int tagType)
    {
        world.AddTag(value, tagType);
    }

    /// <summary>
    /// Checks if this entity contains the given <paramref name=""tagType""/>.
    /// </summary>
    public readonly bool ContainsTag(int tagType)
    {
        return world.ContainsTag(value, tagType);
    }

    /// <summary>
    /// Removes the given <paramref name=""tagType""/> from this entity.
    /// </summary>
    public readonly void RemoveTag(int tagType)
    {
        world.RemoveTag(value, tagType);
    }

    /// <summary>
    /// Checks if this entity contains the given component of type <typeparamref name=""T""/>.
    /// </summary>
    public readonly bool ContainsComponent<T>() where T : unmanaged
    {
        return world.ContainsComponent<T>(value);
    }

    /// <summary>
    /// Retrieves a reference to the component of type <typeparamref name=""T""/> from this entity.
    /// </summary>
    public readonly ref T GetComponent<T>() where T : unmanaged
    {
        return ref world.GetComponent<T>(value);
    }

    /// <summary>
    /// Retrieves a copy of a <typeparamref name=""T""/> component from this entity, or the
    /// <paramref name=""defaultValue""/> if it does not exist.
    /// </summary>
    public readonly T GetComponentOrDefault<T>(T defaultValue = default) where T : unmanaged
    {
        return world.GetComponentOrDefault<T>(value, defaultValue);
    }

    /// <summary>
    /// Retrieves a reference to the component of type <typeparamref name=""T""/> from this entity.
    /// </summary>
    public readonly ref T GetComponent<T>(int componentType) where T : unmanaged
    {
        return ref world.GetComponent<T>(value, componentType);
    }

    /// <summary>
    /// Tries to retrieve a copy of the component of type <typeparamref name=""T""/> from this entity.
    /// </summary>
    public readonly bool TryGetComponent<T>(int componentType, out T component) where T : unmanaged
    {
        return world.TryGetComponent(value, componentType, out component);
    }

    /// <summary>
    /// Tries to retrieve a copy of the component of type <typeparamref name=""T""/> from this entity.
    /// </summary>
    public readonly bool TryGetComponent<T>(out T component) where T : unmanaged
    {
        return world.TryGetComponent(value, out component);
    }

    /// <summary>
    /// Tries to retrieve a reference to the component of type <typeparamref name=""T""/> from this entity.
    /// </summary>
    public readonly ref T TryGetComponent<T>(int componentType, out bool contains) where T : unmanaged
    {
        return ref world.TryGetComponent<T>(value, componentType, out contains);
    }

    /// <summary>
    /// Tries to retrieve a reference to the component of type <typeparamref name=""T""/> from this entity.
    /// </summary>
    public readonly ref T TryGetComponent<T>(out bool contains) where T : unmanaged
    {
        return ref world.TryGetComponent<T>(value, out contains);
    }

    /// <summary>
    /// Assigns the given <paramref name=""component""/> to this entity.
    /// </summary>
    public readonly void SetComponent<T>(T component) where T : unmanaged
    {
        world.SetComponent(value, component);
    }

    /// <summary>
    /// Adds a new component of type <typeparamref name=""T""/> to this entity and
    /// retrieves a reference to it.
    /// </summary>
    public readonly ref T AddComponent<T>() where T : unmanaged
    {
        return ref world.AddComponent<T>(value);
    }

    /// <summary>
    /// Adds the given <paramref name=""component""/> to this entity.
    /// </summary>
    public readonly void AddComponent<T>(T component) where T : unmanaged
    {
        world.AddComponent(value, component);
    }

    /// <summary>
    /// Removes the component of type <typeparamref name=""T""/> from this entity.
    /// </summary>
    public readonly void RemoveComponent<T>() where T : unmanaged
    {
        world.RemoveComponent<T>(value);
    }

    /// <summary>
    /// Checks if this entity contains the given array of type <typeparamref name=""T""/>.
    /// </summary>
    public readonly bool ContainsArray<T>() where T : unmanaged
    {
        return world.ContainsArray<T>(value);
    }

    /// <summary>
    /// Retrieves the length of the <typeparamref name=""T""/> array on this entity.
    /// </summary>
    public readonly int GetArrayLength<T>() where T : unmanaged
    {
        return world.GetArrayLength<T>(value);
    }

    /// <summary>
    /// Retrieves the length of the <paramref name=""arrayType""/> array on this entity.
    /// </summary>
    public readonly int GetArrayLength(int arrayType)
    {
        return world.GetArrayLength(value, arrayType);
    }

    /// <summary>
    /// Retrieves an <typeparamref name=""T""/> array element at the given <paramref name=""index""/> from this entity.
    /// </summary>
    public readonly ref T GetArrayElement<T>(int index) where T : unmanaged
    {
        return ref world.GetArrayElement<T>(value, index);
    }

    /// <summary>
    /// Retrieves the <typeparamref name=""T""/> array present on this entity.
    /// </summary>
    public readonly Values<T> GetArray<T>() where T : unmanaged
    {
        return world.GetArray<T>(value);
    }

    /// <summary>
    /// Retrieves the <typeparamref name=""T""/> array present on this entity.
    /// </summary>
    public readonly Values<T> GetArray<T>(int arrayType) where T : unmanaged
    {
        return world.GetArray<T>(value, arrayType);
    }

    /// <summary>
    /// Tries to retrieve the <typeparamref name=""T""/> array present on this entity.
    /// </summary>
    public readonly bool TryGetArray<T>(out Values<T> array) where T : unmanaged
    {
        return world.TryGetArray(value, out array);
    }

    /// <summary>
    /// Creates an array of type <typeparamref name=""T""/> on this entity from the
    /// given <paramref name=""elements""/> span.
    /// </summary>
    public readonly void CreateArray<T>(ReadOnlySpan<T> elements) where T : unmanaged
    {
        world.CreateArray(value, elements);
    }

    /// <summary>
    /// Creates an array of type <typeparamref name=""T""/> on this entity from the
    /// given <paramref name=""elements""/> span.
    /// </summary>
    public readonly void CreateArray<T>(Span<T> elements) where T : unmanaged
    {
        world.CreateArray(value, elements);
    }

    /// <summary>
    /// Creates an array of type <typeparamref name=""T""/> on this entity.
    /// </summary>
    public readonly Values<T> CreateArray<T>(int length = 0) where T : unmanaged
    {
        return world.CreateArray<T>(value, length);
    }

    /// <summary>
    /// Destroys the array of type <typeparamref name=""T""/> from this entity.
    /// </summary>
    public readonly void DestroyArray<T>() where T : unmanaged
    {
        world.DestroyArray<T>(value);
    }

    /// <summary>
    /// Checks if this entity contains the <typeparamref name=""T""/> tag.
    /// </summary>
    public readonly bool ContainsTag<T>() where T : unmanaged
    {
        return world.ContainsTag<T>(value);
    }

    /// <summary>
    /// Adds a <typeparamref name=""T""/> tag to this entity.
    /// </summary>
    public readonly void AddTag<T>() where T : unmanaged
    {
        world.AddTag<T>(value);
    }

    /// <summary>
    /// Removes the tag of type <typeparamref name=""T""/> from this entity.
    /// </summary>
    public readonly void RemoveTag<T>() where T : unmanaged
    {
        world.RemoveTag<T>(value);
    }
    {{EqualityMethods}}
    /// <summary>
    /// Converts <paramref name=""entity""/> to an <see cref=""Entity""/> instance.
    /// </summary>
    public static implicit operator Entity({{TypeName}} entity)
    {
        return new(entity.world, entity.value);
    }

    /// <inheritdoc/>
    public class DebugView : Entity.DebugView
    {
        /// <inheritdoc/>
        public readonly bool compliant;
    
        /// <inheritdoc/>
        public DebugView({{TypeName}} entity) : base(entity.world, entity.value)
        {
            compliant = entity.IsCompliant;
        }
    }
}";
    }
}