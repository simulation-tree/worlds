using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Unmanaged;

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
            World world = entity.World;
            uint parent = world.GetParent(entity.Value);
            if (parent != default)
            {
                return new(world, parent);
            }
            else
            {
                return default;
            }
        }

        /// <summary>
        /// Attempts to retrieve the parent of this entity.
        /// </summary>
        public static bool TryGetParent<T>(this T entity, out uint parent) where T : unmanaged, IEntity
        {
            parent = entity.World.GetParent(entity.Value);
            return parent != default;
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

        public static USpan<uint> GetChildren<T>(this T entity) where T : unmanaged, IEntity
        {
            return entity.World.GetChildren(entity.Value);
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
        /// Retrieves how many references this entity has.
        /// </summary>
        public static uint GetReferenceCount<T>(this T entity) where T : unmanaged, IEntity
        {
            return entity.World.GetReferenceCount(entity.Value);
        }

        /// <summary>
        /// Retrieves the entity ID found with the given local <paramref name="reference"/> on this entity.
        /// </summary>
        public static uint GetReference<T>(this T entity, rint reference) where T : unmanaged, IEntity
        {
            return entity.World.GetReference(entity.Value, reference);
        }

        /// <summary>
        /// Assigns the given <paramref name="otherEntity"/> to an existing local <paramref name="reference"/> on this entity.
        /// </summary>
        public static void SetReference<T>(this T entity, rint reference, uint otherEntity) where T : unmanaged, IEntity
        {
            entity.World.SetReference(entity.Value, reference, otherEntity);
        }

        /// <summary>
        /// Assigns the <paramref name="otherEntity"/> to an existing local <paramref name="reference"/> on this entity.
        /// </summary>
        public static void SetReference<T, E>(this T entity, rint reference, E otherEntity) where T : unmanaged, IEntity where E : unmanaged, IEntity
        {
            entity.World.SetReference(entity.Value, reference, otherEntity.Value);
        }

        /// <summary>
        /// Adds a new local reference to the given <paramref name="otherEntity"/> on this entity.
        /// </summary>
        public static rint AddReference<T>(this T entity, uint otherEntity) where T : unmanaged, IEntity
        {
            return entity.World.AddReference(entity.Value, otherEntity);
        }

        /// <summary>
        /// Adds a new local reference to the given <paramref name="otherEntity"/> on this entity.
        /// </summary>
        public static rint AddReference<T, E>(this T entity, E otherEntity) where T : unmanaged, IEntity where E : unmanaged, IEntity
        {
            return entity.World.AddReference(entity.Value, otherEntity);
        }

        /// <summary>
        /// Removes the local reference with the given <paramref name="reference"/> on this entity.
        /// </summary>
        public static void RemoveReference<T>(this T entity, rint reference) where T : unmanaged, IEntity
        {
            entity.World.RemoveReference(entity.Value, reference);
        }

        /// <summary>
        /// Checks if this entity contains the given local <paramref name="reference"/>.
        /// </summary>
        public static bool ContainsReference<T>(this T entity, rint reference) where T : unmanaged, IEntity
        {
            return entity.World.ContainsReference(entity.Value, reference);
        }

        /// <summary>
        /// Attempts to retrieve an entity from the given local <paramref name="reference"/>.
        /// </summary>
        public static bool TryGetReference<T>(this T entity, rint reference, out uint otherEntity) where T : unmanaged, IEntity
        {
            return entity.World.TryGetReference(entity.Value, reference, out otherEntity);
        }

        /// <summary>
        /// Adds a new component of type <typeparamref name="C"/> to this entity.
        /// </summary>
        public static ref C AddComponent<T, C>(this T entity, C component) where T : unmanaged, IEntity where C : unmanaged
        {
            return ref entity.World.AddComponent(entity.Value, component);
        }

        /// <summary>
        /// Adds a new component of the given <paramref name="componentType"/>.
        /// </summary>
        public static void AddComponent<T>(this T entity, ComponentType componentType) where T : unmanaged, IEntity
        {
            entity.World.AddComponent(entity.Value, componentType);
        }

        /// <summary>
        /// Sets the component of type <typeparamref name="C"/> to this entity.
        /// </summary>
        public static void SetComponent<T, C>(this T entity, C component) where T : unmanaged, IEntity where C : unmanaged
        {
            entity.World.SetComponent(entity.Value, component);
        }

        /// <summary>
        /// Retrieves the bytes of this entity's component of type <paramref name="componentType"/>.
        /// </summary>
        public static USpan<byte> GetComponentBytes<T>(this T entity, ComponentType componentType) where T : unmanaged, IEntity
        {
            return entity.World.GetComponentBytes(entity.Value, componentType);
        }

        /// <summary>
        /// Retrieves the bytes of this entity's component of type <paramref name="componentType"/>.
        /// </summary>
        public static USpan<byte> GetComponentBytes<T>(this T entity, DataType componentType) where T : unmanaged, IEntity
        {
            return entity.World.GetComponentBytes(entity.Value, componentType);
        }

        /// <summary>
        /// Removes the component of the given <paramref name="componentType"/> from the entity.
        /// </summary>
        public static void RemoveComponent<T>(this T entity, ComponentType componentType) where T : unmanaged, IEntity
        {
            entity.World.RemoveComponent(entity.Value, componentType);
        }

        /// <summary>
        /// Removes the component of type <typeparamref name="T"/> from the entity.
        /// </summary>
        public static void RemoveComponent<T, C>(this T entity, out C removedComponent) where T : unmanaged, IEntity where C : unmanaged
        {
            removedComponent = entity.World.GetComponent<C>(entity.Value);
            entity.World.RemoveComponent<C>(entity.Value);
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
            Chunk chunk = world.GetChunk(value);
            Definition currentDefinition = chunk.Definition;

            //add missing components
            if ((currentDefinition.ComponentTypes & definition.ComponentTypes) != definition.ComponentTypes)
            {
                for (byte c = 0; c < BitMask.Capacity; c++)
                {
                    if (definition.ComponentTypes.Contains(c) && !currentDefinition.ComponentTypes.Contains(c))
                    {
                        ComponentType componentType = new(c);
                        world.AddComponent(value, componentType);
                    }
                }
            }

            //add missing arrays
            if ((currentDefinition.ArrayElementTypes & definition.ArrayElementTypes) != definition.ArrayElementTypes)
            {
                for (byte a = 0; a < BitMask.Capacity; a++)
                {
                    if (definition.ArrayElementTypes.Contains(a) && !currentDefinition.ArrayElementTypes.Contains(a))
                    {
                        ArrayElementType arrayElementType = new(a);
                        world.CreateArray(value, arrayElementType);
                    }
                }
            }

            //add missing tags
            if ((currentDefinition.TagTypes & definition.TagTypes) != definition.TagTypes)
            {
                for (byte t = 0; t < BitMask.Capacity; t++)
                {
                    if (definition.TagTypes.Contains(t) && !currentDefinition.TagTypes.Contains(t))
                    {
                        TagType tagType = new(t);
                        world.AddTag(value, tagType);
                    }
                }
            }
        }

        /// <summary>
        /// Checks if this entity is compliant with its own <see cref="Definition"/>.
        /// </summary>
        public static bool Is<T>(this T entity) where T : unmanaged, IEntity
        {
            Schema schema = entity.GetWorld().Schema;
            Archetype archetype = new(schema);
            entity.Describe(ref archetype);
            return entity.Is(archetype.definition);
        }

        /// <summary>
        /// Checks if this entity complies with the given <paramref name="definition"/>.
        /// </summary>
        public static bool Is<T>(this T entity, Definition definition) where T : unmanaged, IEntity
        {
            World world = entity.World;
            uint value = entity.Value;
            World.Implementation.ThrowIfEntityIsMissing(world, value);

            Chunk chunk = world.GetChunk(value);
            Definition currentDefinition = chunk.Definition;
            if ((currentDefinition.ComponentTypes & definition.ComponentTypes) != definition.ComponentTypes)
            {
                return false;
            }

            if ((currentDefinition.ArrayElementTypes & definition.ArrayElementTypes) != definition.ArrayElementTypes)
            {
                return false;
            }

            return (currentDefinition.TagTypes & definition.TagTypes) == definition.TagTypes;
        }

        /// <summary>
        /// Awaits <paramref name="action"/> until this entity is complies with this entity's <see cref="Definition"/>.
        /// </summary>
        public static async Task UntilCompliant<T>(this T entity, Update action, CancellationToken cancellation = default) where T : unmanaged, IEntity
        {
            World world = entity.World;
            Archetype archetype = new(world.Schema);
            entity.Describe(ref archetype);
            while (!entity.Is(archetype.definition))
            {
                await action(world, cancellation);
            }
        }

        /// <summary>
        /// Checks if this entity has a component of the given <paramref name="componentType"/>.
        /// </summary>
        public static bool ContainsComponent<T>(this T entity, ComponentType componentType) where T : unmanaged, IEntity
        {
            return entity.World.ContainsComponent(entity.Value, componentType);
        }

        /// <summary>
        /// Checks if this entity has an array of the given <paramref name="arrayElementType"/>.
        /// </summary>
        public static bool ContainsArray<T>(this T entity, ArrayElementType arrayElementType) where T : unmanaged, IEntity
        {
            return entity.World.ContainsArray(entity.Value, arrayElementType);
        }

        /// <summary>
        /// Copies all <see cref="ComponentType"/>s this entity has to the given <paramref name="destination"/>.
        /// </summary>
        public static byte CopyComponentTypesTo<T>(this T entity, USpan<ComponentType> destination) where T : unmanaged, IEntity
        {
            return entity.World.CopyComponentTypesTo(entity.Value, destination);
        }

        /// <summary>
        /// Copies all <see cref="ArrayElementType"/>s this entity has to the given <paramref name="destination"/>.
        /// </summary>
        public static byte CopyArrayElementTypesTo<T>(this T entity, USpan<ArrayElementType> destination) where T : unmanaged, IEntity
        {
            return entity.World.CopyArrayElementTypesTo(entity.Value, destination);
        }

        /// <summary>
        /// Copies all <see cref="TagType"/>s this entity contains to the given <paramref name="destination"/>.
        /// </summary>
        public static byte CopyTagTypesTo<T>(this T entity, USpan<TagType> destination) where T : unmanaged, IEntity
        {
            return entity.World.CopyTagTypesTo(entity.Value, destination);
        }

        /// <summary>
        /// Retrieves an array of type <paramref name="arrayElementType"/> on this entity.
        /// </summary>
        public static Allocation GetArray<T>(this T entity, ArrayElementType arrayElementType) where T : unmanaged, IEntity
        {
            return entity.World.GetArray(entity.Value, arrayElementType, out _);
        }

        /// <summary>
        /// Retrieves an array of type <paramref name="arrayElementType"/> on this entity.
        /// </summary>
        public static Allocation GetArray<T>(this T entity, ArrayElementType arrayElementType, out uint length) where T : unmanaged, IEntity
        {
            return entity.World.GetArray(entity.Value, arrayElementType, out length);
        }

        /// <summary>
        /// Resizes the array of type <paramref name="arrayElementType"/> to the given <paramref name="newLength"/>.
        /// </summary>
        /// <returns>Newly resized array.</returns>
        public static Allocation ResizeArray<T>(this T entity, ArrayElementType arrayElementType, uint newLength) where T : unmanaged, IEntity
        {
            return entity.World.ResizeArray(entity.Value, arrayElementType, newLength);
        }

        /// <summary>
        /// Resizes the array of type <paramref name="arrayElementType"/> to the given <paramref name="newLength"/>.
        /// </summary>
        /// <returns>Newly resized array.</returns>
        public static Allocation ResizeArray<T>(this T entity, DataType arrayElementType, uint newLength) where T : unmanaged, IEntity
        {
            return entity.World.ResizeArray(entity.Value, arrayElementType, newLength);
        }

        /// <summary>
        /// Throws an <see cref="InvalidOperationException"/> if the entity is destroyed.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        [Conditional("DEBUG")]
        public static void ThrowIfDestroyed<T>(this T entity) where T : unmanaged, IEntity
        {
            if (!entity.IsDestroyed())
            {
                throw new InvalidOperationException($"Entity `{entity.Value}` is destroyed and no longer available");
            }
        }

        /// <summary>
        /// Throws if the given type doesn't have the same layout as <see cref="Entity"/>.
        /// </summary>
        [Conditional("DEBUG")]
        public static void ThrowIfTypeLayoutMismatches<T>() where T : unmanaged, IEntity
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
    }
}