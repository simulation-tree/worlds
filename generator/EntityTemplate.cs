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
    public readonly Entity Parent => new(world, world.GetParent(value));
        
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

    /// <summary>
    /// Retrieves the current definition this entity possesses.
    /// </summary>
    public readonly Definition Definition => world.GetDefinition(value);

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
        if (parentValue != default)
        {
            parent = new(world, parentValue);
            return true;
        }
        else
        {
            parent = default;
            return false;
        }
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
    public readonly async Task UntilCompliant(Action update, CancellationToken cancellationToken = default)
    {
        {{UntilCompliant}}
    }

    /// <summary>
    /// Makes this entity become another entity of type <typeparamref name=""T""/>,
    /// by adding the missing components, arrays and tags.
    /// </summary>
    public readonly T Become<T>() where T : unmanaged, IEntity
    {
        Archetype archetype = Archetype.Get<T>(world.Schema);
        world.Become(value, archetype);
        return Worlds.EntityExtensions.As<T>(this);
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
        return Worlds.EntityExtensions.As<T>(this);
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
    public readonly void RemoveReference(uint otherEntity)
    {
        world.RemoveReference(value, otherEntity);
    }

    /// <summary>
    /// Removes the reference to the <paramref name=""otherEntity""/> from this entity.
    /// </summary>
    public readonly void RemoveReference(uint otherEntity, out rint removedReference)
    {
        world.RemoveReference(value, otherEntity, out removedReference);
    }

    /// <summary>
    /// Removes the given reference <paramref name=""reference""/> from this entity.
    /// </summary>
    public readonly void RemoveReference(rint reference)
    {
        world.RemoveReference(value, reference);
    }

    /// <summary>
    /// Removes the given reference <paramref name=""reference""/> from this entity.
    /// </summary>
    public readonly void RemoveReference(rint reference, out uint referencedEntity)
    {
        world.RemoveReference(value, reference, out referencedEntity);
    }

    /// <summary>
    /// Removes the reference to the <paramref name=""otherEntity""/> from this entity.
    /// </summary>
    public readonly void RemoveReference<T>(T otherEntity) where T : unmanaged, IEntity
    {
        world.RemoveReference(value, otherEntity.GetEntityValue());
    }

    /// <summary>
    /// Removes the reference to the <paramref name=""otherEntity""/> from this entity.
    /// </summary>
    public readonly void RemoveReference<T>(T otherEntity, out rint removedReference) where T : unmanaged, IEntity
    {
        world.RemoveReference(value, otherEntity.GetEntityValue(), out removedReference);
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
        return Entity.Get<{{TypeName}}>(world, world.CloneEntity(value));
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
    public readonly void RemoveComponentType(int componentType)
    {
        world.RemoveComponentType(value, componentType);
    }

    /// <summary>
    /// Adds the given <paramref name=""componentTypes""/> to this entity.
    /// </summary>
    public readonly void AddComponentTypes(BitMask componentTypes)
    {
        world.AddComponentTypes(value, componentTypes);
    }

    /// <summary>
    /// Removes the <paramref name=""componentTypes""/> from this entity.
    /// </summary>
    public readonly void RemoveComponentTypes(BitMask componentTypes)
    {
        world.RemoveComponentTypes(value, componentTypes);
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
        return world.TryGetComponent<T>(value, componentType, out component);
    }

    /// <summary>
    /// Tries to retrieve a copy of the component of type <typeparamref name=""T""/> from this entity.
    /// </summary>
    public readonly bool TryGetComponent<T>(out T component) where T : unmanaged
    {
        return world.TryGetComponent<T>(value, out component);
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
        world.SetComponent<T>(value, component);
    }

    /// <summary>
    /// Adds a new component of type <typeparamref name=""T""/> to this entity and
    /// retrieves a reference to it.
    /// </summary>
    public readonly ref T AddComponent<T>(int componentType) where T : unmanaged
    {
        return ref world.AddComponent<T>(value, componentType);
    }

    /// <summary>
    /// Adds the given <paramref name=""component""/> to this entity.
    /// </summary>
    public readonly void AddComponent<T>(int componentType, T component) where T : unmanaged
    {
        world.AddComponent<T>(value, componentType, component);
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
        world.AddComponent<T>(value, component);
    }

    /// <summary>
    /// Adds the given components to this entity.
    /// </summary>
    public readonly void AddComponents<T1, T2>(T1 component1, T2 component2) where T1 : unmanaged where T2 : unmanaged
    {
        world.AddComponents(value, component1, component2);
    }

    /// <summary>
    /// Adds the given components to this entity.
    /// </summary>
    public readonly void AddComponents<T1, T2, T3>(T1 component1, T2 component2, T3 component3) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
    {
        world.AddComponents(value, component1, component2, component3);
    }

    /// <summary>
    /// Adds the given components to this entity.
    /// </summary>
    public readonly void AddComponents<T1, T2, T3, T4>(T1 component1, T2 component2, T3 component3, T4 component4) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
    {
        world.AddComponents(value, component1, component2, component3, component4);
    }

    /// <summary>
    /// Adds the given components to this entity.
    /// </summary>
    public readonly void AddComponents<T1, T2, T3, T4, T5>(T1 component1, T2 component2, T3 component3, T4 component4, T5 component5) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
    {
        world.AddComponents(value, component1, component2, component3, component4, component5);
    }

    /// <summary>
    /// Adds the given components to this entity.
    /// </summary>
    public readonly void AddComponents<T1, T2, T3, T4, T5, T6>(T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
    {
        world.AddComponents(value, component1, component2, component3, component4, component5, component6);
    }

    /// <summary>
    /// Adds the given components to this entity.
    /// </summary>
    public readonly void AddComponents<T1, T2, T3, T4, T5, T6, T7>(T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged
    {
        world.AddComponents(value, component1, component2, component3, component4, component5, component6, component7);
    }

    /// <summary>
    /// Adds the given components to this entity.
    /// </summary>
    public readonly void AddComponents<T1, T2, T3, T4, T5, T6, T7, T8>(T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged
    {
        world.AddComponents(value, component1, component2, component3, component4, component5, component6, component7, component8);
    }

    /// <summary>
    /// Adds the given components to this entity.
    /// </summary>
    public readonly void AddComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9>(T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8, T9 component9) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged
    {
        world.AddComponents(value, component1, component2, component3, component4, component5, component6, component7, component8, component9);
    }

    /// <summary>
    /// Adds the given components to this entity.
    /// </summary>
    public readonly void AddComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8, T9 component9, T10 component10) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged
    {
        world.AddComponents(value, component1, component2, component3, component4, component5, component6, component7, component8, component9, component10);
    }

    /// <summary>
    /// Adds the given components to this entity.
    /// </summary>
    public readonly void AddComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8, T9 component9, T10 component10, T11 component11) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged
    {
        world.AddComponents(value, component1, component2, component3, component4, component5, component6, component7, component8, component9, component10, component11);
    }

    /// <summary>
    /// Adds the given components to this entity.
    /// </summary>
    public readonly void AddComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8, T9 component9, T10 component10, T11 component11, T12 component12) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged
    {
        world.AddComponents(value, component1, component2, component3, component4, component5, component6, component7, component8, component9, component10, component11, component12);
    }

    /// <summary>
    /// Adds the given components to this entity.
    /// </summary>
    public readonly void AddComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8, T9 component9, T10 component10, T11 component11, T12 component12, T13 component13) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged
    {
        world.AddComponents(value, component1, component2, component3, component4, component5, component6, component7, component8, component9, component10, component11, component12, component13);
    }

    /// <summary>
    /// Adds the given components to this entity.
    /// </summary>
    public readonly void AddComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8, T9 component9, T10 component10, T11 component11, T12 component12, T13 component13, T14 component14) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged
    {
        world.AddComponents(value, component1, component2, component3, component4, component5, component6, component7, component8, component9, component10, component11, component12, component13, component14);
    }

    /// <summary>
    /// Adds the given components to this entity.
    /// </summary>
    public readonly void AddComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8, T9 component9, T10 component10, T11 component11, T12 component12, T13 component13, T14 component14, T15 component15) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged where T15 : unmanaged
    {
        world.AddComponents(value, component1, component2, component3, component4, component5, component6, component7, component8, component9, component10, component11, component12, component13, component14, component15);
    }

    /// <summary>
    /// Adds the given components to this entity.
    /// </summary>
    public readonly void AddComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(T1 component1, T2 component2, T3 component3, T4 component4, T5 component5, T6 component6, T7 component7, T8 component8, T9 component9, T10 component10, T11 component11, T12 component12, T13 component13, T14 component14, T15 component15, T16 component16) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged where T15 : unmanaged where T16 : unmanaged
    {
        world.AddComponents(value, component1, component2, component3, component4, component5, component6, component7, component8, component9, component10, component11, component12, component13, component14, component15, component16);
    }

    /// <summary>
    /// Adds the given components to this entity.
    /// </summary>
    public readonly void AddComponentTypes<T1, T2>() where T1 : unmanaged where T2 : unmanaged
    {
        world.AddComponentTypes<T1, T2>(value);
    }

    /// <summary>
    /// Adds the given components to this entity.
    /// </summary>
    public readonly void AddComponentTypes<T1, T2, T3>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
    {
        world.AddComponentTypes<T1, T2, T3>(value);
    }

    /// <summary>
    /// Adds the given components to this entity.
    /// </summary>
    public readonly void AddComponentTypes<T1, T2, T3, T4>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
    {
        world.AddComponentTypes<T1, T2, T3, T4>(value);
    }

    /// <summary>
    /// Adds the given components to this entity.
    /// </summary>
    public readonly void AddComponentTypes<T1, T2, T3, T4, T5>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
    {
        world.AddComponentTypes<T1, T2, T3, T4, T5>(value);
    }

    /// <summary>
    /// Adds the given components to this entity.
    /// </summary>
    public readonly void AddComponentTypes<T1, T2, T3, T4, T5, T6>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
    {
        world.AddComponentTypes<T1, T2, T3, T4, T5, T6>(value);
    }

    /// <summary>
    /// Adds the given components to this entity.
    /// </summary>
    public readonly void AddComponentTypes<T1, T2, T3, T4, T5, T6, T7>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged
    {
        world.AddComponentTypes<T1, T2, T3, T4, T5, T6, T7>(value);
    }

    /// <summary>
    /// Adds the given components to this entity.
    /// </summary>
    public readonly void AddComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged
    {
        world.AddComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8>(value);
    }

    /// <summary>
    /// Adds the given components to this entity.
    /// </summary>
    public readonly void AddComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged
    {
        world.AddComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9>(value);
    }

    /// <summary>
    /// Adds the given components to this entity.
    /// </summary>
    public readonly void AddComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged
    {
        world.AddComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(value);
    }

    /// <summary>
    /// Adds the given components to this entity.
    /// </summary>
    public readonly void AddComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged
    {
        world.AddComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(value);
    }

    /// <summary>
    /// Adds the given components to this entity.
    /// </summary>
    public readonly void AddComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged
    {
        world.AddComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(value);
    }

    /// <summary>
    /// Adds the given components to this entity.
    /// </summary>
    public readonly void AddComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged
    {
        world.AddComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(value);
    }

    /// <summary>
    /// Adds the given components to this entity.
    /// </summary>
    public readonly void AddComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged
    {
        world.AddComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(value);
    }

    /// <summary>
    /// Adds the given components to this entity.
    /// </summary>
    public readonly void AddComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged where T15 : unmanaged
    {
        world.AddComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(value);
    }

    /// <summary>
    /// Adds the given components to this entity.
    /// </summary>
    public readonly void AddComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged where T15 : unmanaged where T16 : unmanaged
    {
        world.AddComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(value);
    }

    /// <summary>
    /// Removes the component of type <typeparamref name=""T""/> from this entity.
    /// </summary>
    public readonly void RemoveComponentType<T>() where T : unmanaged
    {
        world.RemoveComponentType<T>(value);
    }

    /// <summary>
    /// Removes components from this entity.
    /// </summary>
    public readonly void RemoveComponentTypes<T1, T2>() where T1 : unmanaged where T2 : unmanaged
    {
        world.RemoveComponentTypes<T1, T2>(value);
    }

    /// <summary>
    /// Removes components from this entity.
    /// </summary>
    public readonly void RemoveComponentTypes<T1, T2, T3>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
    {
        world.RemoveComponentTypes<T1, T2, T3>(value);
    }

    /// <summary>
    /// Removes components from this entity.
    /// </summary>
    public readonly void RemoveComponentTypes<T1, T2, T3, T4>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
    {
        world.RemoveComponentTypes<T1, T2, T3, T4>(value);
    }

    /// <summary>
    /// Removes components from this entity.
    /// </summary>
    public readonly void RemoveComponentTypes<T1, T2, T3, T4, T5>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
    {
        world.RemoveComponentTypes<T1, T2, T3, T4, T5>(value);
    }

    /// <summary>
    /// Removes components from this entity.
    /// </summary>
    public readonly void RemoveComponentTypes<T1, T2, T3, T4, T5, T6>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
    {
        world.RemoveComponentTypes<T1, T2, T3, T4, T5, T6>(value);
    }

    /// <summary>
    /// Removes components from this entity.
    /// </summary>
    public readonly void RemoveComponentTypes<T1, T2, T3, T4, T5, T6, T7>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged
    {
        world.RemoveComponentTypes<T1, T2, T3, T4, T5, T6, T7>(value);
    }

    /// <summary>
    /// Removes components from this entity.
    /// </summary>
    public readonly void RemoveComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged
    {
        world.RemoveComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8>(value);
    }

    /// <summary>
    /// Removes components from this entity.
    /// </summary>
    public readonly void RemoveComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged
    {
        world.RemoveComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9>(value);
    }

    /// <summary>
    /// Removes components from this entity.
    /// </summary>
    public readonly void RemoveComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged
    {
        world.RemoveComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(value);
    }

    /// <summary>
    /// Removes components from this entity.
    /// </summary>
    public readonly void RemoveComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged
    {
        world.RemoveComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(value);
    }

    /// <summary>
    /// Removes components from this entity.
    /// </summary>
    public readonly void RemoveComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged
    {
        world.RemoveComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(value);
    }

    /// <summary>
    /// Removes components from this entity.
    /// </summary>
    public readonly void RemoveComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged
    {
        world.RemoveComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(value);
    }

    /// <summary>
    /// Removes components from this entity.
    /// </summary>
    public readonly void RemoveComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged
    {
        world.RemoveComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(value);
    }

    /// <summary>
    /// Removes components from this entity.
    /// </summary>
    public readonly void RemoveComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged where T15 : unmanaged
    {
        world.RemoveComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(value);
    }

    /// <summary>
    /// Removes components from this entity.
    /// </summary>
    public readonly void RemoveComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged where T15 : unmanaged where T16 : unmanaged
    {
        world.RemoveComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(value);
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
    public readonly Span<T> GetArrayOrDefault<T>() where T : unmanaged
    {
        return world.GetArrayOrDefault<T>(value);
    }

    /// <summary>
    /// Retrieves the <typeparamref name=""T""/> array present on this entity.
    /// </summary>
    public readonly Values<T> GetArray<T>(int arrayType) where T : unmanaged
    {
        return world.GetArray<T>(value, arrayType);
    }

    /// <summary>
    /// Retrieves the <typeparamref name=""T""/> array present on this entity.
    /// </summary>
    public readonly Span<T> GetArrayOrDefault<T>(int arrayType) where T : unmanaged
    {
        return world.GetArrayOrDefault<T>(value, arrayType);
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
    {{StaticDefinitionGetter}}
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