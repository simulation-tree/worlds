using Simulation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Unmanaged;

public static class EntityFunctions
{
    /// <summary>
    /// Destroys the entity from its world.
    /// </summary>
    public static void Destroy<T>(this T entity) where T : unmanaged, IEntity
    {
        entity.World.DestroyEntity(entity.Value);
    }

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

    /// <summary>
    /// Makes the entity become the definition by having
    /// the missing components and arrays added with a default state.
    /// </summary>
    public static void Become<T>(this T entity, Definition definition) where T : unmanaged, IEntity
    {
        //todo: efficiency: kinda expensive to perform these ops one by one, should instead add all missing at once
        USpan<RuntimeType> componentTypes = stackalloc RuntimeType[definition.ComponentTypeCount];
        definition.CopyComponentTypes(componentTypes);
        for (uint i = 0; i < componentTypes.Length; i++)
        {
            RuntimeType componentType = componentTypes[i];
            if (!entity.World.ContainsComponent(entity.Value, componentType))
            {
                entity.World.AddComponent(entity.Value, componentType);
            }
        }

        USpan<RuntimeType> arrayTypes = stackalloc RuntimeType[definition.ArrayTypeCount];
        definition.CopyArrayTypes(arrayTypes);
        for (uint i = 0; i < arrayTypes.Length; i++)
        {
            RuntimeType arrayType = arrayTypes[i];
            if (!entity.World.ContainsArray(entity.Value, arrayType))
            {
                entity.World.CreateArray(entity.Value, arrayType);
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
        USpan<RuntimeType> componentTypes = stackalloc RuntimeType[definition.ComponentTypeCount];
        definition.CopyComponentTypes(componentTypes);
        for (uint i = 0; i < componentTypes.Length; i++)
        {
            if (!world.ContainsComponent(value, componentTypes[i]))
            {
                return false;
            }
        }

        USpan<RuntimeType> arrayTypes = stackalloc RuntimeType[definition.ArrayTypeCount];
        definition.CopyArrayTypes(arrayTypes);
        for (uint i = 0; i < arrayTypes.Length; i++)
        {
            if (!world.ContainsArray(value, arrayTypes[i]))
            {
                return false;
            }
        }

        return true;
    }

    public static Definition GetDefinition<T>(this T entity) where T : unmanaged, IEntity
    {
        return entity.GetDefinition();
    }

    /// <summary>
    /// Checks if the entity complies with the definition it argues.
    /// </summary>
    public static bool IsCompliant<T>(this T entity) where T : unmanaged, IEntity
    {
        World world = entity.World;
        uint value = entity.Value;
        Definition definition = entity.Definition;
        USpan<RuntimeType> componentTypes = stackalloc RuntimeType[definition.ComponentTypeCount];
        definition.CopyComponentTypes(componentTypes);
        for (uint i = 0; i < componentTypes.Length; i++)
        {
            if (!world.ContainsComponent(value, componentTypes[i]))
            {
                return false;
            }
        }

        USpan<RuntimeType> arrayTypes = stackalloc RuntimeType[definition.ArrayTypeCount];
        definition.CopyArrayTypes(arrayTypes);
        for (uint i = 0; i < arrayTypes.Length; i++)
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
        while (true)
        {
            if (!entity.IsCompliant())
            {
                await action(entity.World, cancellation);
            }
            else
            {
                return;
            }
        }
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

    public delegate Task Wait(World world, CancellationToken cancellation);
}