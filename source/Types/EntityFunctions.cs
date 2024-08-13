using Simulation;
using System;
using System.Diagnostics;
using Unmanaged;
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
    public static bool Is<E, T>(this E entity) where E : IEntity where T : unmanaged, IEntity
    {
        return Entity.Is<T>(entity.World, entity.Value);
    }

    public static T As<E, T>(this E entity) where E : IEntity where T : unmanaged, IEntity
    {
        return Entity.As<T>(entity.World, entity.Value);
    }

    /// <summary>
    /// Adds all of the required components to qualify the entity of type <typeparamref name="T"/>.
    /// </summary>
    public static void Become<E, T>(this E entity) where E : IEntity where T : unmanaged, IEntity
    {
        using Query query = default(T).GetQuery(entity.World);
        foreach (RuntimeType type in query.Types)
        {
            if (!entity.ContainsComponent(type))
            {
                entity.AddComponent(type);
            }
        }
    }

    public static void SetEnabledState<E>(this E entity, bool enabled) where E : IEntity
    {
        ThrowIfDestroyed(entity);
        entity.World.SetEnabledState(entity.Value, enabled);
    }

    public static bool IsEnabled<E>(this E entity) where E : IEntity
    {
        ThrowIfDestroyed(entity);
        return entity.World.IsEnabled(entity.Value);
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

    /// <summary>
    /// Retrieves the parent of this entity.
    /// <para><c>default</c> in the absence of one.</para>
    /// </summary>
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

    public static bool ContainsComponent<E>(this E entity, RuntimeType componentType) where E : IEntity
    {
        ThrowIfDestroyed(entity);
        return entity.World.ContainsComponent(entity.Value, componentType);
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

    /// <summary>
    /// Retrieves an existing component of type <typeparamref name="T"/>, or the default value.
    /// </summary>
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

    public static ReadOnlySpan<RuntimeType> GetComponentTypes<E>(this E entity) where E : IEntity
    {
        ThrowIfDestroyed(entity);
        return entity.World.GetComponentTypes(entity.Value);
    }

    public static void SetComponent<E, T>(this E entity, T component) where E : IEntity where T : unmanaged
    {
        ThrowIfDestroyed(entity);
        entity.GetComponentRef<E, T>() = component;
    }

    /// <summary>
    /// Adds a new <typeparamref name="T"/> component to the entity.
    /// </summary>
    public static void AddComponent<E, T>(this E entity, T component) where E : IEntity where T : unmanaged
    {
        ThrowIfDestroyed(entity);
        entity.World.AddComponent(entity.Value, component);
    }

    /// <summary>
    /// Adds a new component of the given type.
    /// </summary>
    public static void AddComponent<E>(this E entity, RuntimeType componentType) where E : IEntity
    {
        ThrowIfDestroyed(entity);
        entity.World.AddComponent(entity.Value, componentType);
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