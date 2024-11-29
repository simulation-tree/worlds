using System.Threading;
using System.Threading.Tasks;
using Unmanaged;

namespace Worlds
{
    /// <summary>
    /// Extensions for <see cref="IEntity"/> types.
    /// </summary>
    public static class EntityFunctions
    {
        /// <summary>
        /// Checks if this entity has been destroyed in its <see cref="World"/>.
        /// </summary>
        public static bool IsDestroyed<T>(this T entity) where T : unmanaged, IEntity
        {
            return !entity.World.ContainsEntity(entity.Value);
        }

        /// <summary>
        /// Checks if this entity is enabled with respect to its ancestors.
        /// </summary>
        public static bool IsEnabled<T>(this T entity) where T : unmanaged, IEntity
        {
            return entity.World.IsEnabled(entity.Value);
        }

        /// <summary>
        /// Assigns the enabled state of this entity.
        /// </summary>
        public static void SetEnabled<T>(this T entity, bool state) where T : unmanaged, IEntity
        {
            entity.World.SetEnabled(entity.Value, state);
        }

        /// <summary>
        /// Retrieves the <see cref="World"/> of this entity.
        /// </summary>
        public static World GetWorld<T>(this T entity) where T : unmanaged, IEntity
        {
            return entity.World;
        }

        /// <summary>
        /// Retrieves the ID of this entity.
        /// </summary>
        public static uint GetEntityValue<T>(this T entity) where T : unmanaged, IEntity
        {
            return entity.Value;
        }

        /// <summary>
        /// Converts this entity to a generic <see cref="Entity"/>.
        /// </summary>
        public static Entity AsEntity<T>(this T entity) where T : unmanaged, IEntity
        {
            return new(entity.World, entity.Value);
        }

        /// <summary>
        /// Retrieves the entity ID found with the given local <paramref name="reference"/> on this entity.
        /// </summary>
        public static uint GetReference<T>(this T entity, rint reference) where T : unmanaged, IEntity
        {
            return entity.World.GetReference(entity.Value, reference);
        }

        /// <summary>
        /// Assigns the given <paramref name="entityId"/> to an existing local <paramref name="reference"/> on this entity.
        /// </summary>
        public static void SetReference<T>(this T entity, rint reference, uint entityId) where T : unmanaged, IEntity
        {
            entity.World.SetReference(entity.Value, reference, entityId);
        }

        /// <summary>
        /// Assigns the <paramref name="otherEntity"/> to an existing local <paramref name="reference"/> on this entity.
        /// </summary>
        public static void SetReference<T, E>(this T entity, rint reference, E otherEntity) where T : unmanaged, IEntity where E : unmanaged, IEntity
        {
            entity.World.SetReference(entity.Value, reference, otherEntity.Value);
        }

        /// <summary>
        /// Adds a new local reference to the given <paramref name="value"/> on this entity.
        /// </summary>
        public static rint AddReference<T>(this T entity, uint value) where T : unmanaged, IEntity
        {
            return entity.World.AddReference(entity.Value, value);
        }

        /// <summary>
        /// Adds a new local reference to the given <paramref name="value"/> on this entity.
        /// </summary>
        public static rint AddReference<T, E>(this T entity, E value) where T : unmanaged, IEntity where E : unmanaged, IEntity
        {
            return entity.World.AddReference(entity.Value, value);
        }

        /// <summary>
        /// Removes the local reference with the given <paramref name="reference"/> on this entity.
        /// </summary>
        public static void RemoveReference<T>(this T entity, rint reference) where T : unmanaged, IEntity
        {
            entity.World.RemoveReference(entity.Value, reference);
        }

        /// <summary>
        /// Adds a new component of type <typeparamref name="C"/> to this entity.
        /// </summary>
        public static void AddComponent<T, C>(this T entity, C component) where T : unmanaged, IEntity where C : unmanaged
        {
            entity.World.AddComponent(entity.Value, component);
        }

        /// <summary>
        /// Sets the component of type <typeparamref name="C"/> to this entity.
        /// </summary>
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
        /// Makes the entity become the given <paramref name="definition"/> by adding
        /// the missing components and arrays.
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
        /// Checks if this entity is compliant with its own <see cref="Definition"/>.
        /// </summary>
        public static bool Is<T>(this T entity) where T : unmanaged, IEntity
        {
            return entity.Is(entity.Definition);
        }

        /// <summary>
        /// Checks if this entity complies with the given <paramref name="definition"/>.
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
        /// Awaits <paramref name="action"/> until this entity is complies with this entity's <see cref="Definition"/>.
        /// </summary>
        public static async Task UntilCompliant<T>(this T entity, Update action, CancellationToken cancellation = default) where T : unmanaged, IEntity
        {
            World world = entity.World;
            Definition definition = entity.Definition;
            while (!entity.Is(definition))
            {
                await action(world, cancellation);
            }
        }
    }
}