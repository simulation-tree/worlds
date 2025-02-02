namespace Worlds.Generator
{
    public static class EntityTemplate
    {
        public const string Source = @"
{{Accessors}} partial struct {{TypeName}} : IEntity, IEquatable<{{TypeName}}>
{
    public readonly World world;
    public readonly uint value;

    public readonly bool IsDisposed => !world.ContainsEntity(value);

#if NET
    [Obsolete(""Default constructor not supported"", true)]
    public {{TypeName}}()
    {
        throw new NotSupportedException();
    }
#endif

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

    public readonly {{TypeName}} Clone()
    {
        return new Entity(world, world.CloneEntity(value)).As<{{TypeName}}>();
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
        return obj is {{TypeName}} entity && Equals(entity);
    }

    public readonly bool Equals({{TypeName}} other)
    {
        return world == other.world && value == other.value;
    }

    public readonly override int GetHashCode()
    {
        return HashCode.Combine(world, value);
    }

    public static bool operator ==({{TypeName}} left, {{TypeName}} right)
    {
        return left.Equals(right);
    }

    public static bool operator !=({{TypeName}} left, {{TypeName}} right)
    {
        return !(left == right);
    }

    public static implicit operator Entity({{TypeName}} entity)
    {
        return new(entity.world, entity.value);
    }
}";
    }
}