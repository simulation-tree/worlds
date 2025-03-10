namespace Worlds.Generator
{
    public static class EntityTemplate
    {
        public const string Source = @"
[DebuggerTypeProxy(typeof({{TypeName}}.DebugView))]
{{Accessors}} partial struct {{TypeName}} : IEntity{{EntityInterfaces}}
{
    public readonly World world;
    public readonly uint value;

    public readonly bool IsDestroyed => !world.ContainsEntity(value);
    public readonly ReadOnlySpan<uint> References => world.GetReferences(value);

    public readonly bool IsEnabled
    {
        get => world.IsEnabled(value);
        set => world.SetEnabled(this.value, value);
    }

    public readonly Entity Parent
    {
        get
        {
            uint parent = world.GetParent(value);
            return parent == default ? default : new Entity(world, parent);
        }
    }
        
    public readonly int ChildCount => world.GetChildCount(value);
    
    public readonly bool IsCompliant
    {
        get
        {
            {{ComplianceChecks}}
            return true;
        }
    }

#if NET
    [Obsolete(""Default constructor not supported"", true)]
    public {{TypeName}}()
    {
        throw new NotSupportedException();
    }
#endif
    {{DisposeMethod}}
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

    public readonly int CopyChildrenTo(Span<uint> destination)
    {
        return world.CopyChildrenTo(value, destination);
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

    public readonly async Task UntilCompliant(Update update, CancellationToken cancellationToken = default)
    {
        while (!IsCompliant)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await update(world, cancellationToken);
        }
    }

    public readonly T Become<T>() where T : unmanaged, IEntity
    {
        Archetype archetype = Archetype.Get<T>(world.Schema);
        world.Become(value, archetype);
        return As<T>();
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
        return new Entity(world, value).As<T>();
    }

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

    public readonly {{TypeName}} Clone()
    {
        return new Entity(world, world.CloneEntity(value)).As<{{TypeName}}>();
    }

    public readonly bool ContainsComponent(int componentType)
    {
        return world.ContainsComponent(value, componentType);
    }

    public readonly bool ContainsArray(int arrayType)
    {
        return world.ContainsArray(value, arrayType);
    }

    public readonly void AddComponent(int componentType)
    {
        world.AddComponent(value, componentType);
    }

    public readonly void RemoveComponent(int componentType)
    {
        world.RemoveComponent(value, componentType);
    }

    public readonly Values CreateArray(int arrayType, int length = 0)
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

    public readonly void AddTag(int tagType)
    {
        world.AddTag(value, tagType);
    }

    public readonly bool ContainsTag(int tagType)
    {
        return world.ContainsTag(value, tagType);
    }

    public readonly void RemoveTag(int tagType)
    {
        world.RemoveTag(value, tagType);
    }

    public readonly bool ContainsComponent<T>() where T : unmanaged
    {
        return world.ContainsComponent<T>(value);
    }

    public readonly ref T GetComponent<T>() where T : unmanaged
    {
        return ref world.GetComponent<T>(value);
    }

    public readonly T GetComponentOrDefault<T>(T defaultValue = default) where T : unmanaged
    {
        return world.GetComponentOrDefault<T>(value, defaultValue);
    }

    public readonly ref T GetComponent<T>(int componentType) where T : unmanaged
    {
        return ref world.GetComponent<T>(value, componentType);
    }

    public readonly bool TryGetComponent<T>(int componentType, out T component) where T : unmanaged
    {
        return world.TryGetComponent(value, componentType, out component);
    }

    public readonly bool TryGetComponent<T>(out T component) where T : unmanaged
    {
        return world.TryGetComponent(value, out component);
    }

    public readonly ref T TryGetComponent<T>(int componentType, out bool contains) where T : unmanaged
    {
        return ref world.TryGetComponent<T>(value, componentType, out contains);
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

    public readonly int GetArrayLength(int arrayType)
    {
        return world.GetArrayLength(value, arrayType);
    }

    public readonly ref T GetArrayElement<T>(int index) where T : unmanaged
    {
        return ref world.GetArrayElement<T>(value, index);
    }

    public readonly Values<T> GetArray<T>() where T : unmanaged
    {
        return world.GetArray<T>(value);
    }

    public readonly Values<T> GetArray<T>(int arrayType) where T : unmanaged
    {
        return world.GetArray<T>(value, arrayType);
    }

    public readonly bool TryGetArray<T>(out Values<T> array) where T : unmanaged
    {
        return world.TryGetArray(value, out array);
    }

    public readonly void CreateArray<T>(ReadOnlySpan<T> elements) where T : unmanaged
    {
        world.CreateArray(value, elements);
    }

    public readonly void CreateArray<T>(Span<T> elements) where T : unmanaged
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
    {{EqualityMethods}}
    public static implicit operator Entity({{TypeName}} entity)
    {
        return new(entity.world, entity.value);
    }

    public class DebugView : Entity.DebugView
    {
        public readonly bool compliant;

        public DebugView({{TypeName}} entity) : base(entity.world, entity.value)
        {
            compliant = entity.IsCompliant;
        }
    }
}";
    }
}