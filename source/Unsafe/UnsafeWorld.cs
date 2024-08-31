using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unmanaged;
using Unmanaged.Collections;

namespace Simulation.Unsafe
{
    public unsafe struct UnsafeWorld
    {
#if DEBUG
        internal static readonly Dictionary<uint, StackTrace> createStackTraces = new();
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
        private UnsafeList* submittedEvents;
        private UnsafeDictionary* listeners;
        private UnsafeDictionary* components;

        private UnsafeWorld(UnsafeList* slots, UnsafeList* freeEntities, UnsafeList* submittedEvents, UnsafeDictionary* listeners, UnsafeDictionary* components)
        {
            this.slots = slots;
            this.freeEntities = freeEntities;
            this.submittedEvents = submittedEvents;
            this.listeners = listeners;
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
            if (slot.IsDestroyed)
            {
                throw new NullReferenceException($"Entity `{entity}` not found");
            }
        }

        [Conditional("DEBUG")]
        public static void ThrowIfEntityIsAlreadyPresent(UnsafeWorld* world, uint entity)
        {
            if (entity == uint.MaxValue)
            {
                throw new InvalidOperationException($"Entity `{entity}` is not valid.");
            }

            uint position = entity - 1;
            uint count = UnsafeList.GetCountRef(world->slots);
            if (position < count)
            {
                ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, position);
                if (!slot.IsDestroyed)
                {
                    throw new InvalidOperationException($"Entity `{entity}` already present");
                }
            }
        }

        [Conditional("DEBUG")]
        public static void ThrowIfComponentMissing(UnsafeWorld* world, uint entity, RuntimeType type)
        {
            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, entity - 1);
            UnmanagedDictionary<uint, ComponentChunk> components = GetComponentChunks(world);
            ComponentChunk chunk = components[slot.componentsKey];
            if (!chunk.Types.Contains(type))
            {
                throw new NullReferenceException($"Component {type} not found on `{entity}`");
            }
        }

        [Conditional("DEBUG")]
        public static void ThrowIfComponentAlreadyPresent(UnsafeWorld* world, uint entity, RuntimeType type)
        {
            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, entity - 1);
            UnmanagedDictionary<uint, ComponentChunk> components = GetComponentChunks(world);
            ComponentChunk chunk = components[slot.componentsKey];
            if (chunk.Types.Contains(type))
            {
                throw new InvalidOperationException($"Component {type} already present on `{entity}`");
            }
        }

        [Conditional("DEBUG")]
        public static void ThrowIfArrayIsMissing(UnsafeWorld* world, uint entity, RuntimeType type)
        {
            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, entity - 1);
            if (slot.arrayTypes.IsDisposed || !slot.arrayTypes.Contains(type))
            {
                throw new NullReferenceException($"Array of type {type} not found on entity `{entity}`.");
            }
        }

        [Conditional("DEBUG")]
        public static void ThrowIfArrayIsAlreadyPresent(UnsafeWorld* world, uint entity, RuntimeType type)
        {
            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, entity - 1);
            if (!slot.arrayTypes.IsDisposed && slot.arrayTypes.Contains(type))
            {
                throw new InvalidOperationException($"Array of type {type} already present on `{entity}`.");
            }
        }

        public static bool IsDisposed(UnsafeWorld* world)
        {
            return Allocations.IsNull(world);
        }

        public static UnmanagedList<EntityDescription> GetEntitySlots(UnsafeWorld* world)
        {
            Allocations.ThrowIfNull(world);
            return new(world->slots);
        }

        public static UnmanagedList<uint> GetFreeEntities(UnsafeWorld* world)
        {
            Allocations.ThrowIfNull(world);
            return new(world->freeEntities);
        }

        public static UnmanagedDictionary<uint, ComponentChunk> GetComponentChunks(UnsafeWorld* world)
        {
            Allocations.ThrowIfNull(world);
            return new(world->components);
        }

        public static UnsafeWorld* Allocate()
        {
            UnsafeList* slots = UnsafeList.Allocate<EntityDescription>();
            UnsafeList* freeEntities = UnsafeList.Allocate<uint>();
            UnsafeList* submittedEvents = UnsafeList.Allocate<Container>();
            UnsafeDictionary* listeners = UnsafeDictionary.Allocate<RuntimeType, UnmanagedList<Listener>>();
            UnsafeDictionary* components = UnsafeDictionary.Allocate<uint, ComponentChunk>();

            ComponentChunk defaultComponentChunk = new(Array.Empty<RuntimeType>());
            uint chunkKey = defaultComponentChunk.Key;
            UnsafeDictionary.Add(components, chunkKey, defaultComponentChunk);

            UnsafeWorld* world = Allocations.Allocate<UnsafeWorld>();
            *world = new(slots, freeEntities, submittedEvents, listeners, components);
            return world;
        }

        public static void Free(ref UnsafeWorld* world)
        {
            Allocations.ThrowIfNull(world);
            ClearEvents(world);
            ClearListeners(world);
            ClearEntities(world);
            UnsafeList.Free(ref world->slots);
            UnsafeList.Free(ref world->freeEntities);
            UnsafeList.Free(ref world->submittedEvents);
            UnsafeDictionary.Free(ref world->listeners);
            UnsafeDictionary.Free(ref world->components);
            Allocations.Free(ref world);
        }

        public static void ClearEntities(UnsafeWorld* world)
        {
            //clear chunks
            UnmanagedDictionary<uint, ComponentChunk> chunks = GetComponentChunks(world);
            foreach (uint hash in chunks.Keys)
            {
                ComponentChunk chunk = chunks[hash];
                chunk.Dispose();
            }

            chunks.Clear();

            //clear slots
            UnmanagedList<EntityDescription> slots = GetEntitySlots(world);
            uint slotCount = slots.Count;
            for (uint s = 0; s < slotCount; s++)
            {
                ref EntityDescription slot = ref slots[s];
                if (!slot.arrayTypes.IsDisposed)
                {
                    uint arrayCount = slot.arrayTypes.Count;
                    for (uint a = 0; a < arrayCount; a++)
                    {
                        slot.arrays[a].Dispose();
                    }

                    slot.arrayTypes.Dispose();
                    slot.arrays.Dispose();
                    slot.arrayLengths.Dispose();
                }

                if (!slot.children.IsDisposed)
                {
                    slot.children.Dispose();
                }

                if (!slot.references.IsDisposed)
                {
                    slot.references.Dispose();
                }
            }

            slots.Clear();

            //clear free entities
            UnsafeList.Clear(world->freeEntities);
        }

        public static void ClearListeners(UnsafeWorld* world)
        {
            UnmanagedDictionary<RuntimeType, UnmanagedList<Listener>> listeners = new(world->listeners);
            foreach (RuntimeType eventType in listeners.Keys)
            {
                UnmanagedList<Listener> listenerList = listeners[eventType];
                uint listenerCount = listenerList.Count;
                for (uint j = 0; j < listenerCount; j++)
                {
                    ref Listener listener = ref listenerList[j];
                    UnsafeListener* unsafeListener = listener.value;
                    UnsafeListener.Free(ref unsafeListener);
                }

                listenerList.Dispose();
            }

            listeners.Clear();
        }

        public static void ClearEvents(UnsafeWorld* world)
        {
            uint eventCount = UnsafeList.GetCountRef(world->submittedEvents);
            for (uint i = 0; i < eventCount; i++)
            {
                Container message = UnsafeList.Get<Container>(world->submittedEvents, i);
                message.Dispose();
            }

            UnsafeList.Clear(world->submittedEvents);
        }

        public static void Submit(UnsafeWorld* world, Container message)
        {
            UnsafeList.Add(world->submittedEvents, message);
        }

        /// <summary>
        /// Polls all submitted events and invokes listeners.
        /// </summary>
        public static void Poll(UnsafeWorld* world)
        {
            World worldValue = new(world);

            uint submissionCount = UnsafeList.GetCountRef(world->submittedEvents);
            Span<Container> events = stackalloc Container[(int)submissionCount]; //<-- is this gonna blow up?
            UnsafeList.AsSpan<Container>(world->submittedEvents).CopyTo(events);
            UnsafeList.Clear(world->submittedEvents);

            Exception? caughtException = null;
            uint dispatchIndex = 0;
            UnmanagedDictionary<RuntimeType, UnmanagedList<Listener>> listeners = new(world->listeners);
            while (dispatchIndex < submissionCount)
            {
                Container message = events[(int)dispatchIndex];
                RuntimeType messageType = message.type;
                if (listeners.TryGetValue(messageType, out UnmanagedList<Listener> listenerList))
                {
                    uint j = 0;
                    while (j < listenerList.Count)
                    {
                        try
                        {
                            Listener listener = listenerList[j];
                            listener.Invoke(worldValue, message.AsAllocation(), messageType);
                        }
                        catch (Exception ex)
                        {
                            caughtException = ex;
                            break;
                        }

                        j++;
                    }
                }

                message.Dispose();
                dispatchIndex++;
                if (caughtException is not null)
                {
                    break;
                }
            }

            if (caughtException is not null)
            {
                throw caughtException;
            }
        }

#if NET5_0_OR_GREATER
        public static Listener CreateListener(UnsafeWorld* world, RuntimeType eventType, delegate* unmanaged<World, Allocation, RuntimeType, void> callback)
        {
            Listener listener = new(new(world), eventType, callback);
            UnmanagedDictionary<RuntimeType, UnmanagedList<Listener>> listeners = new(world->listeners);
            if (!listeners.TryGetValue(eventType, out UnmanagedList<Listener> listenerList))
            {
                listenerList = UnmanagedList<Listener>.Create();
                listeners.Add(eventType, listenerList);
            }

            listenerList.Add(listener);
            return listener;
        }
#else
        public static Listener CreateListener(UnsafeWorld* world, RuntimeType eventType, delegate*<World, Allocation, RuntimeType, void> callback)
        {
            Listener listener = new(new(world), eventType, callback);
            UnmanagedDictionary<RuntimeType, UnmanagedList<Listener>> listeners = new(world->listeners);
            if (!listeners.TryGetValue(eventType, out UnmanagedList<Listener> listenerList))
            {
                listenerList = UnmanagedList<Listener>.Create();
                listeners.Add(eventType, listenerList);
            }

            listenerList.Add(listener);
            return listener;
        }
#endif

        internal static void RemoveListener(UnsafeWorld* world, Listener listener)
        {
            UnmanagedDictionary<RuntimeType, UnmanagedList<Listener>> listeners = new(world->listeners);
            if (listeners.TryGetValue(listener.messageType, out UnmanagedList<Listener> listenerList))
            {
                listenerList.TryRemove(listener);
                UnsafeListener* unsafeListener = listener.value;
                UnsafeListener.Free(ref unsafeListener);
            }
            else
            {
                throw new NullReferenceException($"Listener for {listener.messageType} not found.");
            }
        }

        public static void DestroyEntity(UnsafeWorld* world, uint entity, bool destroyChildren = true)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(world, entity);

            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, entity - 1);

            //destroy or orphan the children
            if (!slot.children.IsDisposed)
            {
                uint childCount = slot.children.Count;
                if (destroyChildren)
                {
                    for (uint i = 0; i < childCount; i++)
                    {
                        uint child = slot.children[i];
                        DestroyEntity(world, child, true);
                    }
                }
                else
                {
                    for (uint i = 0; i < childCount; i++)
                    {
                        uint child = slot.children[i];
                        ref EntityDescription childSlot = ref UnsafeList.GetRef<EntityDescription>(world->slots, child - 1);
                        childSlot.parent = default;
                    }
                }

                slot.children.Clear();
            }

            if (slot.references != default)
            {
                slot.references.Clear();
            }

            UnmanagedDictionary<uint, ComponentChunk> components = GetComponentChunks(world);
            ComponentChunk chunk = components[slot.componentsKey];
            chunk.RemoveEntity(entity);

            if (!slot.arrayTypes.IsDisposed)
            {
                uint arrayCount = slot.arrayTypes.Count;
                for (uint i = 0; i < arrayCount; i++)
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

            slot.entity = default;
            slot.parent = default;
            slot.componentsKey = default;
            slot.state = EntityDescription.State.Destroyed;
            UnsafeList.Add(world->freeEntities, entity);
            NotifyDestruction(new(world), entity);
        }

        public static ReadOnlySpan<RuntimeType> GetComponentTypes(UnsafeWorld* world, uint entity)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(world, entity);

            EntityDescription slot = UnsafeList.GetRef<EntityDescription>(world->slots, entity - 1);
            UnmanagedDictionary<uint, ComponentChunk> components = GetComponentChunks(world);
            ComponentChunk chunk = components[slot.componentsKey];
            return chunk.Types;
        }

        public static ReadOnlySpan<RuntimeType> GetArrayTypes(UnsafeWorld* world, uint entity)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(world, entity);

            EntityDescription slot = UnsafeList.GetRef<EntityDescription>(world->slots, entity - 1);
            if (slot.arrayTypes.IsDisposed)
            {
                return ReadOnlySpan<RuntimeType>.Empty;
            }

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
                throw new InvalidOperationException("Entity cannot be its own parent.");
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
                previousParentSlot.children.TryRemove(entity);
            }

            if (!ContainsEntity(world, parent))
            {
                slot.parent = default;
                NotifyParentChange(new(world), entity, default);
                return false;
            }
            else
            {
                slot.parent = parent;
                ref EntityDescription newParentSlot = ref UnsafeList.GetRef<EntityDescription>(world->slots, parent - 1);
                if (newParentSlot.children.IsDisposed)
                {
                    newParentSlot.children = UnmanagedList<uint>.Create();
                }

                newParentSlot.children.Add(entity);
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
                return UnsafeList.Get<uint>(world->freeEntities, 0);
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
        public static void InitializeEntity(UnsafeWorld* world, uint entity, uint parent, ReadOnlySpan<RuntimeType> componentTypes)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsAlreadyPresent(world, entity);

            UnmanagedList<EntityDescription> slots = GetEntitySlots(world);
            UnmanagedList<uint> freeEntities = GetFreeEntities(world);

            //make sure islands dont exist
            while (entity > slots.Count + 1)
            {
                EntityDescription slot = new(slots.Count + 1, default);
                UnsafeList.Add(world->slots, slot);
                freeEntities.Add(slot.entity);
            }

            //add child reference into parent's slot
            if (!ContainsEntity(world, parent))
            {
                parent = default;
            }
            else
            {
                ref EntityDescription parentSlot = ref slots.GetRef(parent - 1);
                if (parentSlot.children == default)
                {
                    parentSlot.children = UnmanagedList<uint>.Create();
                }

                parentSlot.children.Add(entity);
            }

            uint componentsKey = RuntimeType.CombineHash(componentTypes);
            if (freeEntities.TryRemove(entity))
            {
                //recycle a previously destroyed slot
                ref EntityDescription oldSlot = ref slots.GetRef(entity - 1);
                oldSlot.entity = entity;
                oldSlot.parent = parent;
                oldSlot.componentsKey = componentsKey;
                oldSlot.state = EntityDescription.State.Enabled;
            }
            else
            {
                //create new slot
                uint newEntity = slots.Count + 1;
                EntityDescription newSlot = new(newEntity, componentsKey);
                newSlot.parent = parent;
                slots.Add(newSlot);
            }

            //put entity into correct chunk
            UnmanagedDictionary<uint, ComponentChunk> components = GetComponentChunks(world);
            if (!components.TryGetValue(componentsKey, out ComponentChunk chunk))
            {
                chunk = new(componentTypes);
                components.Add(componentsKey, chunk);
            }

            chunk.AddEntity(entity);

#if DEBUG
            //trace the stack
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

            createStackTraces[entity] = stackTrace;
#endif

            //finally
            NotifyCreation(new(world), entity);
            if (parent != default)
            {
                NotifyParentChange(new(world), entity, parent);
            }
        }

        public static bool ContainsEntity(UnsafeWorld* world, uint entity)
        {
            Allocations.ThrowIfNull(world);
            if (entity == uint.MaxValue)
            {
                return false;
            }

            uint position = entity - 1;
            if (position >= UnsafeList.GetCountRef(world->slots))
            {
                return false;
            }

            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, position);
            return slot.entity == entity;
        }

        public static void* CreateArray(UnsafeWorld* world, uint entity, RuntimeType arrayType, uint length)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(world, entity);
            ThrowIfArrayIsAlreadyPresent(world, entity, arrayType);

            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, entity - 1);
            if (slot.arrayTypes.IsDisposed)
            {
                slot.arrayTypes = UnmanagedList<RuntimeType>.Create();
                slot.arrays = UnmanagedList<Allocation>.Create();
                slot.arrayLengths = UnmanagedList<uint>.Create();
            }

            Allocation newArray = new(arrayType.Size * length);
            slot.arrayTypes.Add(arrayType);
            slot.arrays.Add(newArray);
            slot.arrayLengths.Add(length);
            return slot.arrays[slot.arrays.Count - 1];
        }

        public static bool ContainsArray(UnsafeWorld* world, uint entity, RuntimeType arrayType)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(world, entity);

            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, entity - 1);
            if (!slot.arrayTypes.IsDisposed)
            {
                return slot.arrayTypes.Contains(arrayType);
            }
            else
            {
                return false;
            }
        }

        public static void* GetArray(UnsafeWorld* world, uint entity, RuntimeType arrayType, out uint length)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(world, entity);
            ThrowIfArrayIsMissing(world, entity, arrayType);
            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, entity - 1);
            uint index = slot.arrayTypes.IndexOf(arrayType);
            length = slot.arrayLengths[index];
            return slot.arrays[index];
        }

        public static uint GetArrayLength(UnsafeWorld* world, uint entity, RuntimeType arrayType)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(world, entity);
            ThrowIfArrayIsMissing(world, entity, arrayType);
            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, entity - 1);
            uint index = slot.arrayTypes.IndexOf(arrayType);
            return slot.arrayLengths[index];
        }

        public static void* ResizeArray(UnsafeWorld* world, uint entity, RuntimeType arrayType, uint newLength)
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

        public static void DestroyArray(UnsafeWorld* world, uint entity, RuntimeType arrayType)
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
        }

        public static Span<byte> AddComponent(UnsafeWorld* world, uint entity, RuntimeType componentType)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(world, entity);
            ThrowIfComponentAlreadyPresent(world, entity, componentType);

            UnmanagedDictionary<uint, ComponentChunk> components = GetComponentChunks(world);
            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, entity - 1);
            uint previousTypesKey = slot.componentsKey;
            ComponentChunk current = components[previousTypesKey];
            ReadOnlySpan<RuntimeType> oldTypes = current.Types;
            Span<RuntimeType> newTypes = stackalloc RuntimeType[oldTypes.Length + 1];
            oldTypes.CopyTo(newTypes);
            newTypes[^1] = componentType;
            uint newTypesKey = RuntimeType.CombineHash(newTypes);
            slot.componentsKey = newTypesKey;

            if (!components.TryGetValue(newTypesKey, out ComponentChunk destination))
            {
                destination = new(newTypes);
                components.Add(newTypesKey, destination);
            }

            uint index = current.MoveEntity(entity, destination);
            return destination.GetComponentBytes(index, componentType);
        }

        public static ref T AddComponent<T>(UnsafeWorld* world, uint entity) where T : unmanaged
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(world, entity);
            RuntimeType componentType = RuntimeType.Get<T>();
            ThrowIfComponentAlreadyPresent(world, entity, componentType);

            UnmanagedDictionary<uint, ComponentChunk> components = GetComponentChunks(world);
            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, entity - 1);
            uint previousTypesKey = slot.componentsKey;
            ComponentChunk current = components[previousTypesKey];
            ReadOnlySpan<RuntimeType> oldTypes = current.Types;
            Span<RuntimeType> newTypes = stackalloc RuntimeType[oldTypes.Length + 1];
            oldTypes.CopyTo(newTypes);
            newTypes[^1] = componentType;
            uint newTypesKey = RuntimeType.CombineHash(newTypes);
            slot.componentsKey = newTypesKey;

            if (!components.TryGetValue(newTypesKey, out ComponentChunk destination))
            {
                destination = new(newTypes);
                components.Add(newTypesKey, destination);
            }

            uint index = current.MoveEntity(entity, destination);
            return ref destination.GetComponentRef<T>(index);
        }

        public static void SetComponentBytes(UnsafeWorld* world, uint entity, RuntimeType type, ReadOnlySpan<byte> bytes)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(world, entity);
            ThrowIfComponentMissing(world, entity, type);

            UnmanagedDictionary<uint, ComponentChunk> components = GetComponentChunks(world);
            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, entity - 1);
            ComponentChunk chunk = components[slot.componentsKey];
            chunk.SetComponentBytes(entity, type, bytes);
        }

        public static void RemoveComponent<T>(UnsafeWorld* world, uint entity) where T : unmanaged
        {
            RemoveComponent(world, entity, RuntimeType.Get<T>());
        }

        public static void RemoveComponent(UnsafeWorld* world, uint entity, RuntimeType type)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(world, entity);
            ThrowIfComponentMissing(world, entity, type);

            UnmanagedDictionary<uint, ComponentChunk> components = GetComponentChunks(world);
            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, entity - 1);
            uint previousTypesKey = slot.componentsKey;
            ComponentChunk current = components[previousTypesKey];
            ReadOnlySpan<RuntimeType> oldTypes = current.Types;
            Span<RuntimeType> newTypes = stackalloc RuntimeType[oldTypes.Length - 1];
            int count = 0;
            for (int i = 0; i < oldTypes.Length; i++)
            {
                if (oldTypes[i] != type)
                {
                    newTypes[count] = oldTypes[i];
                    count++;
                }
            }

            uint newTypesKey = RuntimeType.CombineHash(newTypes);
            slot.componentsKey = newTypesKey;

            if (!components.TryGetValue(newTypesKey, out ComponentChunk destination))
            {
                destination = new(newTypes);
                components.Add(newTypesKey, destination);
            }

            current.MoveEntity(entity, destination);
            NotifyComponentRemoved(new(world), entity, type);
        }

        public static bool ContainsComponent(UnsafeWorld* world, uint entity, RuntimeType type)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(world, entity);

            uint index = entity - 1;
            UnmanagedDictionary<uint, ComponentChunk> components = GetComponentChunks(world);
            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, index);
            ComponentChunk chunk = components[slot.componentsKey];
            return chunk.Types.Contains(type);
        }

        public static ref T GetComponentRef<T>(UnsafeWorld* world, uint entity) where T : unmanaged
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(world, entity);

            RuntimeType type = RuntimeType.Get<T>();
            ThrowIfComponentMissing(world, entity, type);

            UnmanagedDictionary<uint, ComponentChunk> components = GetComponentChunks(world);
            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, entity - 1);
            ComponentChunk chunk = components[slot.componentsKey];
            uint index = chunk.Entities.IndexOf(entity);
            return ref chunk.GetComponentRef<T>(index);
        }

        public static Span<byte> GetComponentBytes(UnsafeWorld* world, uint entity, RuntimeType type)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(world, entity);
            ThrowIfComponentMissing(world, entity, type);

            UnmanagedDictionary<uint, ComponentChunk> components = GetComponentChunks(world);
            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, entity - 1);
            ComponentChunk chunk = components[slot.componentsKey];
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

        internal static void NotifyComponentAdded(World world, uint entity, RuntimeType type)
        {
            ComponentAdded(world, entity, type);
        }

        internal static void NotifyComponentRemoved(World world, uint entity, RuntimeType type)
        {
            ComponentRemoved(world, entity, type);
        }
    }
}