using Simulation;
using System.Threading;
using System.Threading.Tasks;
using Unmanaged;

public static class EntityFunctions
{
    public static bool IsDestroyed<T>(this T entity) where T : unmanaged, IEntity
    {
        return !entity.World.ContainsEntity(entity.Value);
    }

    public static World GetWorld<T>(this T entity) where T : unmanaged, IEntity
    {
        return entity.World;
    }

    public static uint GetEntityValue<T>(this T entity) where T : unmanaged, IEntity
    {
        return entity.Value;
    }

    public static Entity AsEntity<T>(this T entity) where T : unmanaged, IEntity
    {
        return new(entity.World, entity.Value);
    }

    public static void SetEnabled<T>(this T entity, bool state) where T : unmanaged, IEntity
    {
        entity.World.SetEnabled(entity.Value, state);
    }

    public static uint GetReference<T>(this T entity, rint reference) where T : unmanaged, IEntity
    {
        return entity.World.GetReference(entity.Value, reference);
    }

    public static void SetReference<T>(this T entity, rint reference, uint value) where T : unmanaged, IEntity
    {
        entity.World.SetReference(entity.Value, reference, value);
    }

    public static void SetReference<T, E>(this T entity, rint reference, E value) where T : unmanaged, IEntity where E : unmanaged, IEntity
    {
        entity.World.SetReference(entity.Value, reference, value.Value);
    }

    public static rint AddReference<T>(this T entity, uint value) where T : unmanaged, IEntity
    {
        return entity.World.AddReference(entity.Value, value);
    }

    public static rint AddReference<T, E>(this T entity, E value) where T : unmanaged, IEntity where E : unmanaged, IEntity
    {
        return entity.World.AddReference(entity.Value, value);
    }

    public static void RemoveReference<T>(this T entity, rint reference) where T : unmanaged, IEntity
    {
        entity.World.RemoveReference(entity.Value, reference);
    }

    public static void AddComponent<T, C>(this T entity, C component) where T : unmanaged, IEntity where C : unmanaged
    {
        entity.World.AddComponent(entity.Value, component);
    }

    public static void SetComponent<T, C>(this T entity, C component) where T : unmanaged, IEntity where C : unmanaged
    {
        entity.World.SetComponent(entity.Value, component);
    }

    /// <summary>
    /// Creates a perfect replica of this entity.
    /// </summary>
    public static T Clone<T>(this T entity) where T : unmanaged, IEntity
    {
        World world = entity.World;
        uint sourceEntity = entity.Value;
        uint destinationEntity = world.CloneEntity(sourceEntity);
        return new Entity(world, destinationEntity).As<T>();
    }

    /// <summary>
    /// Makes the entity become the definition by having
    /// the missing components and arrays added with a default state.
    /// </summary>
    public static void Become<T>(this T entity, Definition definition) where T : unmanaged, IEntity
    {
        //todo: efficiency: kinda expensive to perform these ops one by one, should instead add all missing at once
        World world = entity.World;
        uint value = entity.Value;
        USpan<ComponentType> componentTypes = stackalloc ComponentType[definition.componentTypeCount];
        definition.CopyComponentTypesTo(componentTypes);
        for (uint i = 0; i < definition.componentTypeCount; i++)
        {
            ComponentType componentType = componentTypes[i];
            if (!world.ContainsComponent(value, componentType))
            {
                world.AddComponent(value, componentType);
            }
        }

        USpan<ArrayType> arrayTypes = stackalloc ArrayType[definition.arrayTypeCount];
        definition.CopyArrayTypesTo(arrayTypes);
        for (uint i = 0; i < definition.arrayTypeCount; i++)
        {
            ArrayType arrayType = arrayTypes[i];
            if (!world.ContainsArray(value, arrayType))
            {
                world.CreateArray(value, arrayType);
            }
        }
    }

    /// <summary>
    /// Checks if the entity is what the definition argues.
    /// </summary>
    public static bool Is<T>(this T entity, Definition definition) where T : unmanaged, IEntity
    {
        World world = entity.World;
        uint value = entity.Value;
        USpan<ComponentType> componentTypes = stackalloc ComponentType[definition.componentTypeCount];
        definition.CopyComponentTypesTo(componentTypes);
        for (uint i = 0; i < definition.componentTypeCount; i++)
        {
            if (!world.ContainsComponent(value, componentTypes[i]))
            {
                return false;
            }
        }

        USpan<ArrayType> arrayTypes = stackalloc ArrayType[definition.arrayTypeCount];
        definition.CopyArrayTypesTo(arrayTypes);
        for (uint i = 0; i < definition.arrayTypeCount; i++)
        {
            if (!world.ContainsArray(value, arrayTypes[i]))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Checks if the entity complies with the definition it argues.
    /// </summary>
    public static bool IsCompliant<T>(this T entity) where T : unmanaged, IEntity
    {
        World world = entity.World;
        uint value = entity.Value;
        Definition definition = entity.Definition;
        USpan<ComponentType> componentTypes = stackalloc ComponentType[definition.componentTypeCount];
        definition.CopyComponentTypesTo(componentTypes);
        for (uint i = 0; i < definition.componentTypeCount; i++)
        {
            if (!world.ContainsComponent(value, componentTypes[i]))
            {
                return false;
            }
        }

        USpan<ArrayType> arrayTypes = stackalloc ArrayType[definition.arrayTypeCount];
        definition.CopyArrayTypesTo(arrayTypes);
        for (uint i = 0; i < definition.arrayTypeCount; i++)
        {
            if (!world.ContainsArray(value, arrayTypes[i]))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Awaits until the entity becomes compliant with the
    /// definition it argues.
    /// <para>Callback is expected to return time to await
    /// in milliseconds.</para>
    /// </summary>
    public static async Task UntilCompliant<T>(this T entity, Wait action, CancellationToken cancellation = default) where T : unmanaged, IEntity
    {
        World world = entity.World;
        while (true)
        {
            if (!entity.IsCompliant())
            {
                await action(world, cancellation);
            }
            else
            {
                return;
            }
        }
    }

    public delegate Task Wait(World world, CancellationToken cancellation);
}