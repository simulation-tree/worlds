using System.Threading;
using System.Threading.Tasks;

namespace Worlds
{
    public static class EntityExtensions
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
        /// Retrieves the parent of the given entity, <c>default</c> if none
        /// is assigned.
        /// </summary>
        public static Entity GetParent<T>(this T entity) where T : unmanaged, IEntity
        {
            return new(entity.GetWorld(), entity.World.GetParent(entity.Value));
        }

        /// <summary>
        /// Assigns the given <paramref name="parent"/> entity to the given <paramref name="entity"/>.
        /// </summary>
        /// <returns><c>true</c> if the given parent entity was found and assigned successfuly.</returns>
        public static void SetParent<T>(this T entity, uint parent) where T : unmanaged, IEntity
        {
            entity.World.SetParent(entity.Value, parent);
        }

        /// <summary>
        /// Assigns the given <paramref name="parent"/> entity to the given <paramref name="entity"/>.
        /// </summary>
        /// <returns><c>true</c> if the given parent entity was found and assigned successfuly.</returns>
        public static void SetParent<T>(this T entity, Entity parent) where T : unmanaged, IEntity
        {
            entity.World.SetParent(entity.Value, parent.GetEntityValue());
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
            World world = entity.World;
            uint value = entity.Value;
            ref EntitySlot slot = ref world.Slots[value - 1];
            if (!slot.componentTypes.ContainsAll(definition.ComponentTypesMask))
            {
                for (byte c = 0; c < BitSet.Capacity; c++)
                {
                    if (definition.ComponentTypesMask.Contains(c) && !slot.componentTypes.Contains(c))
                    {
                        ComponentType componentType = ComponentType.All[c];
                        world.AddComponent(value, componentType);
                    }
                }
            }

            if (!slot.arrayTypes.ContainsAll(definition.ArrayTypesMask))
            {
                for (byte a = 0; a < BitSet.Capacity; a++)
                {
                    if (definition.ArrayTypesMask.Contains(a) && !slot.arrayTypes.Contains(a))
                    {
                        ArrayType arrayType = ArrayType.All[a];
                        world.CreateArray(value, arrayType);
                    }
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
            ref EntitySlot slot = ref world.Slots[value - 1];
            return slot.componentTypes.ContainsAll(definition.ComponentTypesMask) && slot.arrayTypes.ContainsAll(definition.ArrayTypesMask);
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