using Collections;
using Collections.Unsafe;
using System;
using System.Diagnostics;
using Unmanaged;

namespace Simulation.Unsafe
{
    public unsafe struct UnsafeWorld
    {
#if DEBUG
        internal static readonly System.Collections.Generic.Dictionary<Entity, StackTrace> createStackTraces = new();
#endif

        /// <summary>
        /// Invoked after any entity is created in any world.
        /// </summary>
        public static event EntityCreatedCallback EntityCreated = delegate { };

        /// <summary>
        /// Invoked after any entity is destroyed from any world.
        /// </summary>
        public static event EntityDestroyedCallback EntityDestroyed = delegate { };

        public static event EntityParentChangedCallback EntityParentChanged = delegate { };
        public static event ComponentAddedCallback ComponentAdded = delegate { };
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

            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, position);
            if (slot.state == EntityDescription.State.Destroyed)
            {
                throw new NullReferenceException($"Entity `{entity}` not found");
            }
        }

        [Conditional("DEBUG")]
        public static void ThrowIfReferenceIsMissing(UnsafeWorld* world, uint entity, rint reference)
        {
            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, entity - 1);
            if (reference.value > slot.referenceCount + 1 || reference.value == 0)
            {
                throw new NullReferenceException($"Reference `{reference}` not found on entity `{entity}`");
            }
        }

        [Conditional("DEBUG")]
        public static void ThrowIfReferenceIsMissing(UnsafeWorld* world, uint entity, uint referencedEntity)
        {
            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, entity - 1);
            if (slot.referenceCount > 0)
            {
                if (!slot.references.Contains(referencedEntity))
                {
                    throw new NullReferenceException($"Reference to entity `{referencedEntity}` not found on entity `{entity}`");
                }
            }
        }

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
                ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, position);
                if (slot.state != EntityDescription.State.Destroyed)
                {
                    throw new InvalidOperationException($"Entity `{entity}` already present");
                }
            }
        }

        [Conditional("DEBUG")]
        public static void ThrowIfComponentMissing(UnsafeWorld* world, uint entity, ComponentType type)
        {
            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, entity - 1);
            Dictionary<int, ComponentChunk> components = GetComponentChunks(world);
            ComponentChunk chunk = components[slot.chunkKey];
            if (!chunk.ContainsType(type))
            {
                throw new NullReferenceException($"Component `{type}` not found on `{entity}`");
            }
        }

        [Conditional("DEBUG")]
        public static void ThrowIfComponentAlreadyPresent(UnsafeWorld* world, uint entity, ComponentType type)
        {
            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, entity - 1);
            Dictionary<int, ComponentChunk> components = GetComponentChunks(world);
            ComponentChunk chunk = components[slot.chunkKey];
            if (chunk.ContainsType(type))
            {
                throw new InvalidOperationException($"Component `{type}` already present on `{entity}`");
            }
        }

        [Conditional("DEBUG")]
        public static void ThrowIfArrayIsMissing(UnsafeWorld* world, uint entity, ArrayType type)
        {
            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, entity - 1);
            if (slot.arrayCount == 0 || !slot.arrayTypes.Contains(type))
            {
                throw new NullReferenceException($"Array of type `{type}` not found on entity `{entity}`");
            }
        }

        [Conditional("DEBUG")]
        public static void ThrowIfArrayIsAlreadyPresent(UnsafeWorld* world, uint entity, ArrayType type)
        {
            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, entity - 1);
            if (slot.arrayCount > 0 && slot.arrayTypes.Contains(type))
            {
                throw new InvalidOperationException($"Array of type `{type}` already present on `{entity}`");
            }
        }

        public static List<EntityDescription> GetEntitySlots(UnsafeWorld* world)
        {
            Allocations.ThrowIfNull(world);

            return new(world->slots);
        }

        public static List<uint> GetFreeEntities(UnsafeWorld* world)
        {
            Allocations.ThrowIfNull(world);

            return new(world->freeEntities);
        }

        public static Dictionary<int, ComponentChunk> GetComponentChunks(UnsafeWorld* world)
        {
            Allocations.ThrowIfNull(world);

            return new(world->components);
        }

        public static UnsafeWorld* Allocate()
        {
            UnsafeList* slots = UnsafeList.Allocate<EntityDescription>(4);
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

        public static void Free(ref UnsafeWorld* world)
        {
            Allocations.ThrowIfNull(world);

            ClearEntities(world);
            UnsafeList.Free(ref world->slots);
            UnsafeList.Free(ref world->freeEntities);
            UnsafeDictionary.Free(ref world->components);
            Allocations.Free(ref world);
        }

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
            List<EntityDescription> slots = GetEntitySlots(world);
            uint slotCount = slots.Count;
            for (uint s = 0; s < slotCount; s++)
            {
                ref EntityDescription slot = ref slots[s];
                if (slot.state != EntityDescription.State.Destroyed)
                {
                    if (slot.arrayCount > 0)
                    {
                        for (uint a = 0; a < slot.arrayCount; a++)
                        {
                            slot.arrays[a].Dispose();
                        }

                        slot.arrayTypes.Dispose();
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

        public static void DestroyEntity(UnsafeWorld* world, uint entity, bool destroyChildren = true)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(world, entity);

            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, entity - 1);

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
                    ref EntityDescription childSlot = ref UnsafeList.GetRef<EntityDescription>(world->slots, child - 1);
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
            if (slot.arrayCount > 0)
            {
                for (uint i = 0; i < slot.arrayCount; i++)
                {
                    slot.arrays[i].Dispose();
                }

                slot.arrayTypes.Dispose();
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
            slot.state = EntityDescription.State.Destroyed;
            UnsafeList.Add(world->freeEntities, entity);
            NotifyDestruction(new(world), entity);
        }

        public static uint CopyComponentTypes(UnsafeWorld* world, uint entity, USpan<ComponentType> buffer)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(world, entity);

            EntityDescription slot = UnsafeList.GetRef<EntityDescription>(world->slots, entity - 1);
            Dictionary<int, ComponentChunk> components = GetComponentChunks(world);
            ComponentChunk chunk = components[slot.chunkKey];
            return chunk.CopyTypesTo(buffer);
        }

        public static USpan<ArrayType> GetArrayTypes(UnsafeWorld* world, uint entity)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(world, entity);

            EntityDescription slot = UnsafeList.GetRef<EntityDescription>(world->slots, entity - 1);
            return slot.arrayTypes.AsSpan();
        }

        public static uint GetParent(UnsafeWorld* world, uint entity)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(world, entity);

            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, entity - 1);
            return slot.parent;
        }

        public static bool SetParent(UnsafeWorld* world, uint entity, uint parent)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(world, entity);

            if (entity == parent)
            {
                throw new InvalidOperationException("Entity cannot be its own parent");
            }

            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, entity - 1);
            if (slot.parent == parent)
            {
                return false;
            }

            //remove from previous parent children
            if (slot.parent != default)
            {
                ref EntityDescription previousParentSlot = ref UnsafeList.GetRef<EntityDescription>(world->slots, slot.parent - 1);
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

                if (slot.state == EntityDescription.State.EnabledButDisabledDueToAncestor)
                {
                    slot.state = EntityDescription.State.Enabled;
                }
            }

            if (parent == default || !ContainsEntity(world, parent))
            {
                slot.parent = default;
                NotifyParentChange(new(world), entity, default);
                return false;
            }
            else
            {
                slot.parent = parent;
                ref EntityDescription newParentSlot = ref UnsafeList.GetRef<EntityDescription>(world->slots, parent - 1);
                if (newParentSlot.childCount == 0)
                {
                    newParentSlot.children = new(1);
                }

                newParentSlot.children.Add(entity);
                newParentSlot.childCount++;
                if (newParentSlot.state == EntityDescription.State.Disabled || newParentSlot.state == EntityDescription.State.EnabledButDisabledDueToAncestor)
                {
                    slot.state = EntityDescription.State.EnabledButDisabledDueToAncestor;
                }

                NotifyParentChange(new(world), entity, parent);
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
        public static void InitializeEntity(UnsafeWorld* world, Definition definition, uint entity)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsAlreadyPresent(world, entity);

            List<EntityDescription> slots = GetEntitySlots(world);
            List<uint> freeEntities = GetFreeEntities(world);

            //make sure islands of free entities dont exist
            while (entity > slots.Count + 1)
            {
                EntityDescription freeSlot = new(slots.Count + 1);
                slots.Add(freeSlot);
                freeEntities.Add(freeSlot.entity);
            }

            if (!freeEntities.TryRemoveBySwapping(entity))
            {
                slots.Add(new());
            }
            else
            {
                //free slot reused
            }

            int componentsKey = definition.ComponentTypesMask.GetHashCode();
            ref EntityDescription slot = ref slots[entity - 1];
            slot.entity = entity;
            slot.chunkKey = componentsKey;
            slot.state = EntityDescription.State.Enabled;

            //put entity into correct chunk
            Dictionary<int, ComponentChunk> components = GetComponentChunks(world);
            if (!components.TryGetValue(componentsKey, out ComponentChunk chunk))
            {
                chunk = new(definition.ComponentTypesMask);
                components.Add(componentsKey, chunk);
            }

            //add arrays
            slot.arrayCount = definition.arrayTypeCount;
            if (slot.arrayCount > 0)
            {
                USpan<ArrayType> arrayTypes = stackalloc ArrayType[slot.arrayCount];
                definition.CopyArrayTypes(arrayTypes);
                slot.arrayLengths = new(slot.arrayCount);
                slot.arrayTypes = new(slot.arrayCount);
                slot.arrays = new(slot.arrayCount);
                for (uint i = 0; i < slot.arrayCount; i++)
                {
                    ArrayType arrayType = arrayTypes[i];
                    slot.arrayTypes.Add(arrayType);
                    slot.arrays.Add(new(arrayType.Size));
                    slot.arrayLengths.Add(0);
                }
            }

            chunk.AddEntity(entity);
            TraceCreation(world, entity);

            //finally
            NotifyCreation(new(world), entity);
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

        public static bool ContainsEntity(UnsafeWorld* world, uint entity)
        {
            Allocations.ThrowIfNull(world);

            uint position = entity - 1;
            if (position >= UnsafeList.GetCountRef(world->slots))
            {
                return false;
            }

            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, position);
            return slot.entity == entity;
        }

        public static void* CreateArray(UnsafeWorld* world, uint entity, ArrayType arrayType, uint length)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(world, entity);
            ThrowIfArrayIsAlreadyPresent(world, entity, arrayType);

            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, entity - 1);
            if (slot.arrayCount == 0)
            {
                slot.arrayTypes = new(1);
                slot.arrays = new(1);
                slot.arrayLengths = new(1);
            }

            Allocation newArray = new(arrayType.Size * length);
            slot.arrayTypes.Add(arrayType);
            slot.arrays.Add(newArray);
            slot.arrayLengths.Add(length);
            slot.arrayCount++;
            return slot.arrays[slot.arrays.Count - 1];
        }

        public static bool ContainsArray(UnsafeWorld* world, uint entity, ArrayType arrayType)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(world, entity);

            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, entity - 1);
            if (slot.arrayCount > 0)
            {
                return slot.arrayTypes.Contains(arrayType);
            }
            else
            {
                return false;
            }
        }

        public static void* GetArray(UnsafeWorld* world, uint entity, ArrayType arrayType, out uint length)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(world, entity);
            ThrowIfArrayIsMissing(world, entity, arrayType);

            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, entity - 1);
            uint index = slot.arrayTypes.IndexOf(arrayType);
            length = slot.arrayLengths[index];
            return slot.arrays[index];
        }

        public static uint GetArrayLength(UnsafeWorld* world, uint entity, ArrayType arrayType)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(world, entity);
            ThrowIfArrayIsMissing(world, entity, arrayType);

            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, entity - 1);
            uint index = slot.arrayTypes.IndexOf(arrayType);
            return slot.arrayLengths[index];
        }

        public static void* ResizeArray(UnsafeWorld* world, uint entity, ArrayType arrayType, uint newLength)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(world, entity);
            ThrowIfArrayIsMissing(world, entity, arrayType);

            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, entity - 1);
            uint index = slot.arrayTypes.IndexOf(arrayType);
            ref Allocation array = ref slot.arrays[index];
            Allocation.Resize(ref array, arrayType.Size * newLength);
            slot.arrayLengths[index] = newLength;
            return array;
        }

        public static void DestroyArray(UnsafeWorld* world, uint entity, ArrayType arrayType)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(world, entity);
            ThrowIfArrayIsMissing(world, entity, arrayType);

            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, entity - 1);
            uint index = slot.arrayTypes.IndexOf(arrayType);
            slot.arrays[index].Dispose();
            slot.arrayTypes.RemoveAtBySwapping(index);
            slot.arrays.RemoveAtBySwapping(index);
            slot.arrayLengths.RemoveAtBySwapping(index);
            slot.arrayCount--;

            if (slot.arrayCount == 0)
            {
                slot.arrayTypes.Dispose();
                slot.arrays.Dispose();
                slot.arrayLengths.Dispose();
            }
        }

        public static USpan<byte> AddComponent(UnsafeWorld* world, uint entity, ComponentType componentType)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(world, entity);
            ThrowIfComponentAlreadyPresent(world, entity, componentType);

            Dictionary<int, ComponentChunk> components = GetComponentChunks(world);
            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, entity - 1);
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

        public static ref T AddComponent<T>(UnsafeWorld* world, uint entity) where T : unmanaged
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(world, entity);

            ComponentType componentType = ComponentType.Get<T>();
            ThrowIfComponentAlreadyPresent(world, entity, componentType);

            Dictionary<int, ComponentChunk> components = GetComponentChunks(world);
            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, entity - 1);
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

        public static void SetComponentBytes(UnsafeWorld* world, uint entity, ComponentType type, USpan<byte> bytes)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(world, entity);
            ThrowIfComponentMissing(world, entity, type);

            Dictionary<int, ComponentChunk> components = GetComponentChunks(world);
            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, entity - 1);
            ComponentChunk chunk = components[slot.chunkKey];
            chunk.SetComponentBytes(entity, type, bytes);
        }

        public static void RemoveComponent<T>(UnsafeWorld* world, uint entity) where T : unmanaged
        {
            RemoveComponent(world, entity, ComponentType.Get<T>());
        }

        public static void RemoveComponent(UnsafeWorld* world, uint entity, ComponentType type)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(world, entity);
            ThrowIfComponentMissing(world, entity, type);

            Dictionary<int, ComponentChunk> components = GetComponentChunks(world);
            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, entity - 1);
            int previousChunkKey = slot.chunkKey;
            ComponentChunk previousChunk = components[previousChunkKey];
            BitSet newComponentTypes = previousChunk.TypesMask;
            newComponentTypes.Clear(type);
            int newChunkKey = newComponentTypes.GetHashCode();
            slot.chunkKey = newChunkKey;

            if (!components.TryGetValue(newChunkKey, out ComponentChunk destinationChunk))
            {
                destinationChunk = new(newComponentTypes);
                components.Add(newChunkKey, destinationChunk);
            }

            previousChunk.MoveEntity(entity, destinationChunk);
            NotifyComponentRemoved(new(world), entity, type);
        }

        public static bool ContainsComponent(UnsafeWorld* world, uint entity, ComponentType type)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(world, entity);

            uint index = entity - 1;
            Dictionary<int, ComponentChunk> components = GetComponentChunks(world);
            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, index);
            ComponentChunk chunk = components[slot.chunkKey];
            return chunk.ContainsType(type);
        }

        public static ref T GetComponentRef<T>(UnsafeWorld* world, uint entity) where T : unmanaged
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(world, entity);

            ComponentType type = ComponentType.Get<T>();
            ThrowIfComponentMissing(world, entity, type);

            Dictionary<int, ComponentChunk> components = GetComponentChunks(world);
            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, entity - 1);
            ComponentChunk chunk = components[slot.chunkKey];
            uint index = chunk.Entities.IndexOf(entity);
            return ref chunk.GetComponentRef<T>(index);
        }

        public static USpan<byte> GetComponentBytes(UnsafeWorld* world, uint entity, ComponentType type)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(world, entity);
            ThrowIfComponentMissing(world, entity, type);

            Dictionary<int, ComponentChunk> components = GetComponentChunks(world);
            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, entity - 1);
            ComponentChunk chunk = components[slot.chunkKey];
            uint index = chunk.Entities.IndexOf(entity);
            return chunk.GetComponentBytes(index, type);
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