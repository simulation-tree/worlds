using Collections;
using Collections.Unsafe;
using System;
using System.Diagnostics;
using Unmanaged;

namespace Simulation.Unsafe
{
    /// <summary>
    /// Opaque pointer implementation of a <see cref="World"/>.
    /// </summary>
    public unsafe struct UnsafeWorld
    {
#if DEBUG
        internal static readonly System.Collections.Generic.Dictionary<Entity, StackTrace> createStackTraces = new();
#endif

        /// <summary>
        /// Invoked after any entity is created.
        /// </summary>
        public static event EntityCreatedCallback EntityCreated = delegate { };

        /// <summary>
        /// Invoked after any entity is destroyed.
        /// </summary>
        public static event EntityDestroyedCallback EntityDestroyed = delegate { };

        /// <summary>
        /// Invoked after any entity's parent is changed.
        /// </summary>
        public static event EntityParentChangedCallback EntityParentChanged = delegate { };

        /// <summary>
        /// Invoked after any component is added to any entity.
        /// </summary>
        public static event ComponentAddedCallback ComponentAdded = delegate { };

        /// <summary>
        /// Invoked after any component is removed from any entity.
        /// </summary>
        public static event ComponentRemovedCallback ComponentRemoved = delegate { };

        private UnsafeList* slots;
        private UnsafeList* freeEntities;
        private UnsafeDictionary* components;

        private UnsafeWorld(UnsafeList* slots, UnsafeList* freeEntities, UnsafeDictionary* components)
        {
            this.slots = slots;
            this.freeEntities = freeEntities;
            this.components = components;
        }

        /// <summary>
        /// Throws an <see cref="NullReferenceException"/> if the given <paramref name="entity"/> is missing.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="NullReferenceException"></exception>
        [Conditional("DEBUG")]
        public static void ThrowIfEntityIsMissing(UnsafeWorld* world, uint entity)
        {
            if (entity == uint.MaxValue)
            {
                throw new InvalidOperationException($"Entity `{entity}` is not valid");
            }

            uint position = entity - 1;
            uint count = UnsafeList.GetCountRef(world->slots);
            if (position >= count)
            {
                throw new NullReferenceException($"Entity `{entity}` not found");
            }

            ref EntitySlot slot = ref UnsafeList.GetRef<EntitySlot>(world->slots, position);
            if (slot.state == EntitySlot.State.Destroyed)
            {
                throw new NullReferenceException($"Entity `{entity}` not found");
            }
        }

        /// <summary>
        /// Throws an <see cref="NullReferenceException"/> if the given <paramref name="reference"/> is missing.
        /// </summary>
        /// <exception cref="NullReferenceException"></exception>
        [Conditional("DEBUG")]
        public static void ThrowIfReferenceIsMissing(UnsafeWorld* world, uint entity, rint reference)
        {
            ref EntitySlot slot = ref UnsafeList.GetRef<EntitySlot>(world->slots, entity - 1);
            if (reference.value > slot.referenceCount + 1 || reference.value == 0)
            {
                throw new NullReferenceException($"Reference `{reference}` not found on entity `{entity}`");
            }
        }

        /// <summary>
        /// Throws an <see cref="NullReferenceException"/> if the given <paramref name="referencedEntity"/> is not 
        /// referenced by <paramref name="entity"/>.
        /// </summary>
        /// <exception cref="NullReferenceException"></exception>
        [Conditional("DEBUG")]
        public static void ThrowIfReferenceIsMissing(UnsafeWorld* world, uint entity, uint referencedEntity)
        {
            ref EntitySlot slot = ref UnsafeList.GetRef<EntitySlot>(world->slots, entity - 1);
            if (slot.referenceCount > 0)
            {
                if (!slot.references.Contains(referencedEntity))
                {
                    throw new NullReferenceException($"Reference to entity `{referencedEntity}` not found on entity `{entity}`");
                }
            }
        }

        /// <summary>
        /// Throws an <see cref="InvalidOperationException"/> if the given <paramref name="entity"/> is already present.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        [Conditional("DEBUG")]
        public static void ThrowIfEntityIsAlreadyPresent(UnsafeWorld* world, uint entity)
        {
            if (entity == uint.MaxValue)
            {
                throw new InvalidOperationException($"Entity `{entity}` is not valid");
            }

            uint position = entity - 1;
            uint count = UnsafeList.GetCountRef(world->slots);
            if (position < count)
            {
                ref EntitySlot slot = ref UnsafeList.GetRef<EntitySlot>(world->slots, position);
                if (slot.state != EntitySlot.State.Destroyed)
                {
                    throw new InvalidOperationException($"Entity `{entity}` already present");
                }
            }
        }

        /// <summary>
        /// Throws an <see cref="InvalidOperationException"/> if the given <paramref name="componentType"/> is missing from <paramref name="entity"/>.
        /// </summary>
        /// <exception cref="NullReferenceException"></exception>
        [Conditional("DEBUG")]
        public static void ThrowIfComponentMissing(UnsafeWorld* world, uint entity, ComponentType componentType)
        {
            ref EntitySlot slot = ref UnsafeList.GetRef<EntitySlot>(world->slots, entity - 1);
            Dictionary<int, ComponentChunk> components = GetComponentChunks(world);
            ComponentChunk chunk = components[slot.chunkKey];
            if (!chunk.ContainsType(componentType))
            {
                throw new NullReferenceException($"Component `{componentType}` not found on `{entity}`");
            }
        }

        /// <summary>
        /// Throws an <see cref="InvalidOperationException"/> if the given <paramref name="componentType"/> is already present on <paramref name="entity"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        [Conditional("DEBUG")]
        public static void ThrowIfComponentAlreadyPresent(UnsafeWorld* world, uint entity, ComponentType componentType)
        {
            ref EntitySlot slot = ref UnsafeList.GetRef<EntitySlot>(world->slots, entity - 1);
            Dictionary<int, ComponentChunk> components = GetComponentChunks(world);
            ComponentChunk chunk = components[slot.chunkKey];
            if (chunk.ContainsType(componentType))
            {
                throw new InvalidOperationException($"Component `{componentType}` already present on `{entity}`");
            }
        }

        /// <summary>
        /// Throws an <see cref="NullReferenceException"/> if the given <paramref name="entity"/> is missing
        /// the given <paramref name="arrayType"/>.
        /// </summary>
        /// <exception cref="NullReferenceException"></exception>
        [Conditional("DEBUG")]
        public static void ThrowIfArrayIsMissing(UnsafeWorld* world, uint entity, ArrayType arrayType)
        {
            ref EntitySlot slot = ref UnsafeList.GetRef<EntitySlot>(world->slots, entity - 1);
            if (!slot.arrayTypes.Contains(arrayType))
            {
                throw new NullReferenceException($"Array of type `{arrayType}` not found on entity `{entity}`");
            }
        }

        /// <summary>
        /// Throws an <see cref="InvalidOperationException"/> if the given <paramref name="entity"/> already
        /// has the given <paramref name="arrayType"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        [Conditional("DEBUG")]
        public static void ThrowIfArrayIsAlreadyPresent(UnsafeWorld* world, uint entity, ArrayType arrayType)
        {
            ref EntitySlot slot = ref UnsafeList.GetRef<EntitySlot>(world->slots, entity - 1);
            if (slot.arrayTypes.Contains(arrayType))
            {
                throw new InvalidOperationException($"Array of type `{arrayType}` already present on `{entity}`");
            }
        }

        /// <summary>
        /// Retrieves entity slots list.
        /// </summary>
        public static List<EntitySlot> GetEntitySlots(UnsafeWorld* world)
        {
            Allocations.ThrowIfNull(world);

            return new(world->slots);
        }

        /// <summary>
        /// Retrieves free entities list.
        /// </summary>
        public static List<uint> GetFreeEntities(UnsafeWorld* world)
        {
            Allocations.ThrowIfNull(world);

            return new(world->freeEntities);
        }

        /// <summary>
        /// Retrieves component chunks dictionary.
        /// </summary>
        public static Dictionary<int, ComponentChunk> GetComponentChunks(UnsafeWorld* world)
        {
            Allocations.ThrowIfNull(world);

            return new(world->components);
        }

        /// <summary>
        /// Allocates a new <see cref="UnsafeWorld"/> instance.
        /// </summary>
        public static UnsafeWorld* Allocate()
        {
            UnsafeList* slots = UnsafeList.Allocate<EntitySlot>(4);
            UnsafeList* freeEntities = UnsafeList.Allocate<uint>(4);
            UnsafeDictionary* components = UnsafeDictionary.Allocate<uint, ComponentChunk>(4);

            ComponentChunk defaultComponentChunk = new(default(BitSet));
            int chunkKey = defaultComponentChunk.TypesMask.GetHashCode();
            UnsafeDictionary.Add(components, chunkKey, defaultComponentChunk);

            UnsafeWorld* world = Allocations.Allocate<UnsafeWorld>();
            world->slots = slots;
            world->freeEntities = freeEntities;
            world->components = components;
            return world;
        }

        /// <summary>
        /// Frees the given <paramref name="world"/> instance.
        /// </summary>
        public static void Free(ref UnsafeWorld* world)
        {
            Allocations.ThrowIfNull(world);

            ClearEntities(world);
            UnsafeList.Free(ref world->slots);
            UnsafeList.Free(ref world->freeEntities);
            UnsafeDictionary.Free(ref world->components);
            Allocations.Free(ref world);
        }

        /// <summary>
        /// Clears all entities from the given <paramref name="world"/>.
        /// </summary>
        public static void ClearEntities(UnsafeWorld* world)
        {
            Allocations.ThrowIfNull(world);

            //clear chunks
            Dictionary<int, ComponentChunk> chunks = GetComponentChunks(world);
            foreach (int hash in chunks.Keys)
            {
                ComponentChunk chunk = chunks[hash];
                chunk.Dispose();
            }

            chunks.Clear();

            //clear slots
            List<EntitySlot> slots = GetEntitySlots(world);
            uint slotCount = slots.Count;
            for (uint s = 0; s < slotCount; s++)
            {
                ref EntitySlot slot = ref slots[s];
                if (slot.state != EntitySlot.State.Destroyed)
                {
                    if (slot.arrayTypes.Count > 0)
                    {
                        for (byte a = 0; a < BitSet.Capacity; a++)
                        {
                            if (slot.arrayTypes.Contains(a))
                            {
                                slot.arrays[a].Dispose();
                            }
                        }

                        slot.arrays.Dispose();
                        slot.arrayLengths.Dispose();

                        slot.arrayTypes = default;
                        slot.arrays = default;
                        slot.arrayLengths = default;
                    }

                    if (slot.childCount > 0)
                    {
                        slot.children.Dispose();
                        slot.children = default;
                    }

                    if (slot.referenceCount > 0)
                    {
                        slot.references.Dispose();
                        slot.references = default;
                    }
                }
            }

            slots.Clear();

            //clear free entities
            UnsafeList.Clear(world->freeEntities);
        }

        /// <summary>
        /// Destroys the given <paramref name="world"/> instance.
        /// </summary>
        public static void DestroyEntity(UnsafeWorld* world, uint entity, bool destroyChildren = true)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(world, entity);

            ref EntitySlot slot = ref UnsafeList.GetRef<EntitySlot>(world->slots, entity - 1);

            //destroy or orphan the children
            if (destroyChildren)
            {
                for (uint i = 0; i < slot.childCount; i++)
                {
                    uint child = slot.children[i];
                    DestroyEntity(world, child, true);
                }
            }
            else
            {
                for (uint i = 0; i < slot.childCount; i++)
                {
                    uint child = slot.children[i];
                    ref EntitySlot childSlot = ref UnsafeList.GetRef<EntitySlot>(world->slots, child - 1);
                    childSlot.parent = default;
                }
            }

            if (slot.childCount > 0)
            {
                slot.children.Dispose();
                slot.children = default;
            }

            if (slot.referenceCount > 0)
            {
                slot.references.Dispose();
                slot.references = default;
            }

            Dictionary<int, ComponentChunk> components = GetComponentChunks(world);
            ComponentChunk chunk = components[slot.chunkKey];
            chunk.RemoveEntity(entity);

            //reset arrays
            if (slot.arrayTypes.Count > 0)
            {
                for (byte a = 0; a < BitSet.Capacity; a++)
                {
                    if (slot.arrayTypes.Contains(a))
                    {
                        slot.arrays[a].Dispose();
                    }
                }

                slot.arrays.Dispose();
                slot.arrayLengths.Dispose();

                slot.arrayTypes = default;
                slot.arrays = default;
                slot.arrayLengths = default;
            }

            //reset the rest
            slot.entity = default;
            slot.parent = default;
            slot.chunkKey = default;
            slot.state = EntitySlot.State.Destroyed;
            UnsafeList.Add(world->freeEntities, entity);
            NotifyDestruction(new(world), entity);
        }

        /// <summary>
        /// Retrieves the parent of the given <paramref name="entity"/>.
        /// </summary>
        public static uint GetParent(UnsafeWorld* world, uint entity)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(world, entity);

            ref EntitySlot slot = ref UnsafeList.GetRef<EntitySlot>(world->slots, entity - 1);
            return slot.parent;
        }

        /// <summary>
        /// Assigns the given <paramref name="parent"/> to the given <paramref name="entity"/>.
        /// </summary>
        public static bool SetParent(UnsafeWorld* world, uint entity, uint parent)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(world, entity);

            if (entity == parent)
            {
                throw new InvalidOperationException("Entity cannot be its own parent");
            }

            ref EntitySlot slot = ref UnsafeList.GetRef<EntitySlot>(world->slots, entity - 1);
            if (slot.parent == parent)
            {
                return false;
            }

            //remove from previous parent children
            if (slot.parent != default)
            {
                ref EntitySlot previousParentSlot = ref UnsafeList.GetRef<EntitySlot>(world->slots, slot.parent - 1);
                if (previousParentSlot.childCount > 0)
                {
                    if (previousParentSlot.children.TryRemoveBySwapping(entity))
                    {
                        previousParentSlot.childCount--;
                        if (previousParentSlot.childCount == 0)
                        {
                            previousParentSlot.children.Dispose();
                        }
                    }
                }

                if (slot.state == EntitySlot.State.EnabledButDisabledDueToAncestor)
                {
                    slot.state = EntitySlot.State.Enabled;
                }
            }

            if (parent == default || !ContainsEntity(world, parent))
            {
                if (slot.parent != default)
                {
                    slot.parent = default;
                    NotifyParentChange(new(world), entity, default);
                }

                return false;
            }
            else
            {
                if (slot.parent != parent)
                {
                    slot.parent = parent;
                    ref EntitySlot newParentSlot = ref UnsafeList.GetRef<EntitySlot>(world->slots, parent - 1);
                    if (newParentSlot.childCount == 0)
                    {
                        newParentSlot.children = new(1);
                    }

                    newParentSlot.children.Add(entity);
                    newParentSlot.childCount++;
                    if (newParentSlot.state == EntitySlot.State.Disabled || newParentSlot.state == EntitySlot.State.EnabledButDisabledDueToAncestor)
                    {
                        slot.state = EntitySlot.State.EnabledButDisabledDueToAncestor;
                    }

                    NotifyParentChange(new(world), entity, parent);
                }

                return true;
            }
        }

        /// <summary>
        /// Returns the next available entity value.
        /// </summary>
        public static uint GetNextEntity(UnsafeWorld* world)
        {
            Allocations.ThrowIfNull(world);
            if (UnsafeList.GetCountRef(world->freeEntities) > 0)
            {
                return UnsafeList.GetRef<uint>(world->freeEntities, 0);
            }
            else
            {
                return UnsafeList.GetCountRef(world->slots) + 1;
            }
        }

        /// <summary>
        /// Initializes the given entity value into existence assuming 
        /// its not already present.
        /// </summary>
        public static void InitializeEntity(UnsafeWorld* world, Definition definition, uint newEntity)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsAlreadyPresent(world, newEntity);

            List<EntitySlot> slots = GetEntitySlots(world);
            List<uint> freeEntities = GetFreeEntities(world);

            //make sure islands of free entities dont exist
            while (newEntity > slots.Count + 1)
            {
                EntitySlot freeSlot = new(slots.Count + 1);
                slots.Add(freeSlot);
                freeEntities.Add(freeSlot.entity);
            }

            if (!freeEntities.TryRemoveBySwapping(newEntity))
            {
                slots.Add(new());
            }
            else
            {
                //free slot reused
            }

            int componentsKey = definition.ComponentTypesMask.GetHashCode();
            ref EntitySlot slot = ref slots[newEntity - 1];
            slot.entity = newEntity;
            slot.chunkKey = componentsKey;
            slot.state = EntitySlot.State.Enabled;

            //put entity into correct chunk
            Dictionary<int, ComponentChunk> components = GetComponentChunks(world);
            if (!components.TryGetValue(componentsKey, out ComponentChunk chunk))
            {
                chunk = new(definition.ComponentTypesMask);
                components.Add(componentsKey, chunk);
            }

            //add arrays
            if (definition.arrayTypeCount > 0)
            {
                slot.arrayLengths = new(BitSet.Capacity);
                slot.arrayTypes = definition.ArrayTypesMask;
                slot.arrays = new(BitSet.Capacity);
                for (byte a = 0; a < BitSet.Capacity; a++)
                {
                    if (slot.arrayTypes.Contains(a))
                    {
                        ArrayType arrayType = new(a);
                        slot.arrays[arrayType] = new(arrayType.Size);
                        slot.arrayLengths[arrayType] = 0;
                    }
                }
            }

            chunk.AddEntity(newEntity);
            TraceCreation(world, newEntity);

            //finally
            NotifyCreation(new(world), newEntity);
        }

        [Conditional("DEBUG")]
        private static void TraceCreation(UnsafeWorld* world, uint entity)
        {
            StackTrace stackTrace = new(2, true);
            if (stackTrace.FrameCount > 0)
            {
                string? firstFrame = stackTrace.GetFrame(0)?.GetFileName();
                if (firstFrame != null && firstFrame.EndsWith("World.cs"))
                {
                    stackTrace = new(3, true);
                }

                firstFrame = stackTrace.GetFrame(0)?.GetFileName();
                if (firstFrame != null && firstFrame.EndsWith("Entity.cs"))
                {
                    stackTrace = new(4, true);
                }
            }

#if DEBUG
            createStackTraces[new Entity(new World(world), entity)] = stackTrace;
#endif
        }

        /// <summary>
        /// Checks if the given <paramref name="world"/> contains the given <paramref name="entity"/>.
        /// </summary>
        public static bool ContainsEntity(UnsafeWorld* world, uint entity)
        {
            Allocations.ThrowIfNull(world);

            uint position = entity - 1;
            if (position >= UnsafeList.GetCountRef(world->slots))
            {
                return false;
            }

            ref EntitySlot slot = ref UnsafeList.GetRef<EntitySlot>(world->slots, position);
            return slot.entity == entity;
        }

        /// <summary>
        /// Creates an array of the given <paramref name="arrayType"/> for the given <paramref name="entity"/>.
        /// </summary>
        public static void* CreateArray(UnsafeWorld* world, uint entity, ArrayType arrayType, uint length)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(world, entity);
            ThrowIfArrayIsAlreadyPresent(world, entity, arrayType);

            ref EntitySlot slot = ref UnsafeList.GetRef<EntitySlot>(world->slots, entity - 1);
            if (slot.arrayTypes.Count == 0)
            {
                slot.arrays = new(BitSet.Capacity);
                slot.arrayLengths = new(BitSet.Capacity);
            }

            Allocation newArray = new(arrayType.Size * length);
            slot.arrayTypes.Set(arrayType);
            slot.arrays[arrayType] = newArray;
            slot.arrayLengths[arrayType] = length;
            return slot.arrays[arrayType];
        }

        /// <summary>
        /// Checks if the given <paramref name="entity"/> contains an array of the given <paramref name="arrayType"/>.
        /// </summary>
        public static bool ContainsArray(UnsafeWorld* world, uint entity, ArrayType arrayType)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(world, entity);

            ref EntitySlot slot = ref UnsafeList.GetRef<EntitySlot>(world->slots, entity - 1);
            return slot.arrayTypes.Contains(arrayType);
        }

        /// <summary>
        /// Retrieves the array of the given <paramref name="arrayType"/> for the given <paramref name="entity"/>.
        /// </summary>
        public static void* GetArray(UnsafeWorld* world, uint entity, ArrayType arrayType, out uint length)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(world, entity);
            ThrowIfArrayIsMissing(world, entity, arrayType);

            ref EntitySlot slot = ref UnsafeList.GetRef<EntitySlot>(world->slots, entity - 1);
            length = slot.arrayLengths[arrayType];
            return slot.arrays[arrayType];
        }

        /// <summary>
        /// Retrieves the length of the array of the given <paramref name="arrayType"/> for the given <paramref name="entity"/>.
        /// </summary>
        public static uint GetArrayLength(UnsafeWorld* world, uint entity, ArrayType arrayType)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(world, entity);
            ThrowIfArrayIsMissing(world, entity, arrayType);

            ref EntitySlot slot = ref UnsafeList.GetRef<EntitySlot>(world->slots, entity - 1);
            return slot.arrayLengths[arrayType];
        }

        /// <summary>
        /// Resizes the array of the given <paramref name="arrayType"/> for the given <paramref name="entity"/>.
        /// </summary>
        public static void* ResizeArray(UnsafeWorld* world, uint entity, ArrayType arrayType, uint newLength)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(world, entity);
            ThrowIfArrayIsMissing(world, entity, arrayType);

            ref EntitySlot slot = ref UnsafeList.GetRef<EntitySlot>(world->slots, entity - 1);
            ref Allocation array = ref slot.arrays[arrayType];
            Allocation.Resize(ref array, arrayType.Size * newLength);
            slot.arrayLengths[arrayType] = newLength;
            return array;
        }

        /// <summary>
        /// Destroys the array of the given <paramref name="arrayType"/> for the given <paramref name="entity"/>.
        /// </summary>
        public static void DestroyArray(UnsafeWorld* world, uint entity, ArrayType arrayType)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(world, entity);
            ThrowIfArrayIsMissing(world, entity, arrayType);

            ref EntitySlot slot = ref UnsafeList.GetRef<EntitySlot>(world->slots, entity - 1);
            slot.arrays[arrayType].Dispose();
            slot.arrayTypes.Clear(arrayType);
            slot.arrayLengths[arrayType] = 0;

            if (slot.arrayTypes.Count == 0)
            {
                slot.arrays.Dispose();
                slot.arrayLengths.Dispose();
            }
        }

        /// <summary>
        /// Adds a new component of the given <paramref name="componentType"/> to the given <paramref name="entity"/>.
        /// </summary>
        public static USpan<byte> AddComponent(UnsafeWorld* world, uint entity, ComponentType componentType)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(world, entity);
            ThrowIfComponentAlreadyPresent(world, entity, componentType);

            Dictionary<int, ComponentChunk> components = GetComponentChunks(world);
            ref EntitySlot slot = ref UnsafeList.GetRef<EntitySlot>(world->slots, entity - 1);
            int previousChunkKey = slot.chunkKey;
            ComponentChunk previousChunk = components[previousChunkKey];
            BitSet newComponentTypes = previousChunk.TypesMask;
            newComponentTypes.Set(componentType);
            int newChunkKey = newComponentTypes.GetHashCode();
            slot.chunkKey = newChunkKey;

            if (!components.TryGetValue(newChunkKey, out ComponentChunk destinationChunk))
            {
                destinationChunk = new(newComponentTypes);
                components.Add(newChunkKey, destinationChunk);
            }

            uint index = previousChunk.MoveEntity(entity, destinationChunk);
            return destinationChunk.GetComponentBytes(index, componentType);
        }

        /// <summary>
        /// Adds a new component of the given <typeparamref name="T"/> type to the given <paramref name="entity"/>.
        /// </summary>
        public static ref T AddComponent<T>(UnsafeWorld* world, uint entity) where T : unmanaged
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(world, entity);

            ComponentType componentType = ComponentType.Get<T>();
            ThrowIfComponentAlreadyPresent(world, entity, componentType);

            Dictionary<int, ComponentChunk> components = GetComponentChunks(world);
            ref EntitySlot slot = ref UnsafeList.GetRef<EntitySlot>(world->slots, entity - 1);
            int previousChunkKey = slot.chunkKey;
            ComponentChunk previousChunk = components[previousChunkKey];
            BitSet newComponentTypes = previousChunk.TypesMask;
            newComponentTypes.Set(componentType);
            int newChunkKey = newComponentTypes.GetHashCode();
            slot.chunkKey = newChunkKey;

            if (!components.TryGetValue(newChunkKey, out ComponentChunk destinationChunk))
            {
                destinationChunk = new(newComponentTypes);
                components.Add(newChunkKey, destinationChunk);
            }

            uint index = previousChunk.MoveEntity(entity, destinationChunk);
            return ref destinationChunk.GetComponentRef<T>(index);
        }

        /// <summary>
        /// Assigns the given <paramref name="bytes"/> to the component of the given <paramref name="componentType"/> on the given <paramref name="entity"/>.
        /// </summary>
        public static void SetComponentBytes(UnsafeWorld* world, uint entity, ComponentType componentType, USpan<byte> bytes)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(world, entity);
            ThrowIfComponentMissing(world, entity, componentType);

            Dictionary<int, ComponentChunk> components = GetComponentChunks(world);
            ref EntitySlot slot = ref UnsafeList.GetRef<EntitySlot>(world->slots, entity - 1);
            ComponentChunk chunk = components[slot.chunkKey];
            chunk.SetComponentBytes(entity, componentType, bytes);
        }

        /// <summary>
        /// Removes the component of type <typeparamref name="T"/> from the given <paramref name="entity"/>.
        /// </summary>
        public static void RemoveComponent<T>(UnsafeWorld* world, uint entity) where T : unmanaged
        {
            RemoveComponent(world, entity, ComponentType.Get<T>());
        }

        /// <summary>
        /// Removes the component of the given <paramref name="componentType"/> from the given <paramref name="entity"/>.
        /// </summary>
        public static void RemoveComponent(UnsafeWorld* world, uint entity, ComponentType componentType)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(world, entity);
            ThrowIfComponentMissing(world, entity, componentType);

            Dictionary<int, ComponentChunk> components = GetComponentChunks(world);
            ref EntitySlot slot = ref UnsafeList.GetRef<EntitySlot>(world->slots, entity - 1);
            int previousChunkKey = slot.chunkKey;
            ComponentChunk previousChunk = components[previousChunkKey];
            BitSet newComponentTypes = previousChunk.TypesMask;
            newComponentTypes.Clear(componentType);
            int newChunkKey = newComponentTypes.GetHashCode();
            slot.chunkKey = newChunkKey;

            if (!components.TryGetValue(newChunkKey, out ComponentChunk destinationChunk))
            {
                destinationChunk = new(newComponentTypes);
                components.Add(newChunkKey, destinationChunk);
            }

            previousChunk.MoveEntity(entity, destinationChunk);
            NotifyComponentRemoved(new(world), entity, componentType);
        }

        /// <summary>
        /// Checks if the given <paramref name="entity"/> contains a component of the given <paramref name="componentType"/>.
        /// </summary>
        public static bool ContainsComponent(UnsafeWorld* world, uint entity, ComponentType componentType)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(world, entity);

            uint index = entity - 1;
            Dictionary<int, ComponentChunk> components = GetComponentChunks(world);
            ref EntitySlot slot = ref UnsafeList.GetRef<EntitySlot>(world->slots, index);
            ComponentChunk chunk = components[slot.chunkKey];
            return chunk.ContainsType(componentType);
        }

        /// <summary>
        /// Retrieves the component of the given <typeparamref name="T"/> type from <paramref name="entity"/>.
        /// </summary>
        public static ref T GetComponentRef<T>(UnsafeWorld* world, uint entity) where T : unmanaged
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(world, entity);

            ComponentType type = ComponentType.Get<T>();
            ThrowIfComponentMissing(world, entity, type);

            Dictionary<int, ComponentChunk> components = GetComponentChunks(world);
            ref EntitySlot slot = ref UnsafeList.GetRef<EntitySlot>(world->slots, entity - 1);
            ComponentChunk chunk = components[slot.chunkKey];
            uint index = chunk.Entities.IndexOf(entity);
            return ref chunk.GetComponentRef<T>(index);
        }

        /// <summary>
        /// Retrieves the bytes of the component of the given <paramref name="componentType"/> from <paramref name="entity"/>.
        /// </summary>
        public static USpan<byte> GetComponentBytes(UnsafeWorld* world, uint entity, ComponentType componentType)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(world, entity);
            ThrowIfComponentMissing(world, entity, componentType);

            Dictionary<int, ComponentChunk> components = GetComponentChunks(world);
            ref EntitySlot slot = ref UnsafeList.GetRef<EntitySlot>(world->slots, entity - 1);
            ComponentChunk chunk = components[slot.chunkKey];
            uint index = chunk.Entities.IndexOf(entity);
            return chunk.GetComponentBytes(index, componentType);
        }

        internal static void NotifyCreation(World world, uint entity)
        {
            EntityCreated(world, entity);
        }

        internal static void NotifyDestruction(World world, uint entity)
        {
            EntityDestroyed(world, entity);
        }

        internal static void NotifyParentChange(World world, uint entity, uint parent)
        {
            EntityParentChanged(world, entity, parent);
        }

        internal static void NotifyComponentAdded(World world, uint entity, ComponentType type)
        {
            ComponentAdded(world, entity, type);
        }

        internal static void NotifyComponentRemoved(World world, uint entity, ComponentType type)
        {
            ComponentRemoved(world, entity, type);
        }
    }
}