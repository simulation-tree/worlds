using Simulation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using System.Threading;
using Unmanaged;

public static class EntityFunctions
{
    /// <summary>
    /// Returns <c>true</c> if the entity complies with its argued type.
    /// </summary>
    public static bool Is<T>(this T entity) where T : unmanaged, IEntity
    {
        World world = entity.World;
        eint entityValue = entity.Value;
        using Query query = entity.GetQuery(world);
        foreach (RuntimeType type in query.Types)
        {
            if (!world.ContainsComponent(entityValue, type))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Awaits until the entity becomes the type that it argues.
    /// <para>The given action is only invoked when
    /// the entity isn't its type, and is expected to returns the 
    /// milliseconds to await at every step (can be 0).</para>
    /// </summary>
    public static async Task UntilIs<T>(this T entity, Wait action, CancellationToken cancellation = default) where T : unmanaged, IEntity
    {
        World world = entity.World;
        eint entityValue = entity.Value;
        using Query query = entity.GetQuery(world);
        uint typeCount = query.TypeCount;
        if (typeCount == 0)
        {
            return;
        }

        bool containsAll;
        do
        {
            containsAll = true;
            for (uint i = 0; i < typeCount; i++)
            {
                RuntimeType type = query.GetType(i);
                bool containsComponent = world.ContainsComponent(entityValue, type);
                if (!containsComponent)
                {
                    containsAll = false;
                    await action(world, cancellation);
                    break;
                }
            }
        }
        while (!containsAll);
    }

    /// <summary>
    /// Throws if the given type doesnt have a similar enough layout to <see cref="Entity"/>.
    /// Because the methods that use will perform native reinterprets.
    /// </summary>
    [Conditional("DEBUG")]
    public static void ThrowIfTypeLayoutMismatches(Type type)
    {
        BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        Stack<Type> checkStack = new();
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
                    if (first == typeof(eint) && second == typeof(World))
                    {
                        return;
                    }
                    else
                    {
                        throw new Exception($"Unexpected entity layout in `{checkingType}`. Was expecting `{nameof(eint)}`, then `{nameof(World)}`");
                    }
                }
            }
        }

        throw new Exception($"The type `{type}` does not align with the `{nameof(Entity)}` type");
    }

    public delegate Task Wait(World world, CancellationToken cancellation);
}