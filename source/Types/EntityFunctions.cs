using Simulation;
using System;
using System.Diagnostics;
using Unmanaged.Collections;

public static class EntityFunctions
{
    [Conditional("DEBUG")]
    public static void ThrowIfDestroyed<E>(this E entity) where E : IEntity
    {
        if (entity.IsDestroyed())
        {
            throw new InvalidOperationException($"Entity {entity} is destroyed.");
        }
    }

    /// <summary>
    /// Returns <c>true</c> if the entity complies with the given type.
    /// <para>
    /// Checks by testing the query filter against the entity.
    /// </para>
    /// </summary>
    public static bool IsCompliant<E>(this E entity) where E : IEntity
    {
        using Query query = E.GetQuery(entity.World);
        query.Update();
        return query.Contains(entity.Value);
    }

    public static World GetWorld<E>(this E entity) where E : IEntity
    {
        return entity.World;
    }

    public static eint GetEntityValue<E>(this E entity) where E : IEntity
    {
        return entity.Value;
    }

    public static Entity AsEntity<E>(this E entity) where E : IEntity
    {
        return new(entity.World, entity.Value);
    }

    /// <summary>
    /// Destroys the entity from its <see cref="World"/>.
    /// </summary>
    public static void Destroy<E>(this E entity) where E : IEntity
    {
        ThrowIfDestroyed(entity);
        entity.World.DestroyEntity(entity.Value);
    }

    /// <summary>
    /// <c>true</c> when the entity doesn't exists in its <see cref="World"/>.
    /// </summary>
    public static bool IsDestroyed<E>(this E entity) where E : IEntity
    {
        return !entity.World.ContainsEntity(entity.Value);
    }

    public static ReadOnlySpan<eint> GetChildren<E>(this E entity) where E : IEntity
    {
        ThrowIfDestroyed(entity);
        return entity.World.GetChildren(entity.Value);
    }

    public static bool TryGetParent<E>(this E entity, out eint parent) where E : IEntity
    {
        ThrowIfDestroyed(entity);
        parent = entity.World.GetParent(entity.Value);
        return parent != default;
    }

    public static eint GetParent<E>(this E entity) where E : IEntity
    {
        ThrowIfDestroyed(entity);
        return entity.World.GetParent(entity.Value);
    }

    public static void SetParent<E>(this E entity, eint parent) where E : IEntity
    {
        ThrowIfDestroyed(entity);
        entity.World.SetParent(entity.Value, parent);
    }

    public static bool ContainsComponent<E, T>(this E entity) where E : IEntity where T : unmanaged
    {
        ThrowIfDestroyed(entity);
        return entity.World.ContainsComponent<T>(entity.Value);
    }

    public static ref T GetComponentRef<E, T>(this E entity) where E : IEntity where T : unmanaged
    {
        ThrowIfDestroyed(entity);
        return ref entity.World.GetComponentRef<T>(entity.Value);
    }

    public static ref T AddComponentRef<E, T>(this E entity) where E : IEntity where T : unmanaged
    {
        ThrowIfDestroyed(entity);
        return ref entity.World.AddComponentRef<T>(entity.Value);
    }

    public static ref T TryGetComponentRef<E, T>(this E entity, out bool has) where E : IEntity where T : unmanaged
    {
        ThrowIfDestroyed(entity);
        return ref entity.World.TryGetComponentRef<T>(entity.Value, out has);
    }

    public static bool TryGetComponent<E, T>(this E entity, out T component) where E : IEntity where T : unmanaged
    {
        ThrowIfDestroyed(entity);
        if (entity.ContainsComponent<E, T>())
        {
            component = entity.GetComponent<E, T>();
            return true;
        }
        else
        {
            component = default;
            return false;
        }
    }

    public static T GetComponent<E, T>(this E entity, T defaultValue) where E : IEntity where T : unmanaged
    {
        ThrowIfDestroyed(entity);
        return entity.World.GetComponent(entity.Value, defaultValue);
    }

    public static T GetComponent<E, T>(this E world, eint entity) where E : IEntity where T : unmanaged
    {
        ThrowIfDestroyed(new Entity(world.World, entity));
        return world.World.GetComponent<T>(entity);
    }

    public static T GetComponent<E, T>(this E entity) where E : IEntity where T : unmanaged
    {
        ThrowIfDestroyed(entity);
        return entity.World.GetComponent<T>(entity.Value);
    }

    public static void SetComponent<E, T>(this E entity, T component) where E : IEntity where T : unmanaged
    {
        ThrowIfDestroyed(entity);
        entity.GetComponentRef<E, T>() = component;
    }

    public static void AddComponent<E, T>(this E entity, T component) where E : IEntity where T : unmanaged
    {
        ThrowIfDestroyed(entity);
        entity.World.AddComponent(entity.Value, component);
    }

    public static void RemoveComponent<E, T>(this E entity) where E : IEntity where T : unmanaged
    {
        ThrowIfDestroyed(entity);
        entity.World.RemoveComponent<T>(entity.Value);
    }

    public static UnmanagedList<T> GetList<E, T>(this E entity) where E : IEntity where T : unmanaged
    {
        ThrowIfDestroyed(entity);
        return entity.World.GetList<T>(entity.Value);
    }

    /// <summary>
    /// Retrieves the generic list on the given entity.
    /// </summary>
    public static UnmanagedList<T> GetList<E, T>(this E world, eint entity) where E : IEntity where T : unmanaged
    {
        ThrowIfDestroyed(new Entity(world.World, entity));
        return world.World.GetList<T>(entity);
    }

    public static bool ContainsList<E, T>(this E entity) where E : IEntity where T : unmanaged
    {
        ThrowIfDestroyed(entity);
        return entity.World.ContainsList<T>(entity.Value);
    }

    public static void DestroyList<E, T>(this E entity) where E : IEntity where T : unmanaged
    {
        ThrowIfDestroyed(entity);
        entity.World.DestroyList<T>(entity.Value);
    }

    public static UnmanagedList<T> CreateList<E, T>(this E entity) where E : IEntity where T : unmanaged
    {
        ThrowIfDestroyed(entity);
        return entity.World.CreateList<T>(entity.Value);
    }

    public static UnmanagedList<T> CreateList<E, T>(this E entity, uint initialCapacity) where E : IEntity where T : unmanaged
    {
        ThrowIfDestroyed(entity);
        return entity.World.CreateCollection<T>(entity.Value, initialCapacity);
    }
}