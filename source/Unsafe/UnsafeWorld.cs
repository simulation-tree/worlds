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
        internal static readonly Dictionary<eint, StackTrace> createStackTraces = new();
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
        private UnsafeList* dispatchingEvents;
        private UnsafeDictionary* listeners;
        private UnsafeDictionary* components;

        private UnsafeWorld(UnsafeList* slots, UnsafeList* freeEntities, UnsafeList* submittedEvents, UnsafeList* dispatchingEvents, UnsafeDictionary* listeners, UnsafeDictionary* components)
        {
            this.slots = slots;
            this.freeEntities = freeEntities;
            this.submittedEvents = submittedEvents;
            this.dispatchingEvents = dispatchingEvents;
            this.listeners = listeners;
            this.components = components;
        }

        [Conditional("DEBUG")]
        public static void ThrowIfEntityMissing(UnsafeWorld* world, eint entity)
        {
            if (entity == uint.MaxValue)
            {
                throw new InvalidOperationException($"Entity {entity} is not valid.");
            }

            uint position = entity - 1;
            uint count = UnsafeList.GetCountRef(world->slots);
            if (position >= count)
            {
                throw new NullReferenceException($"Entity {entity} not found.");
            }

            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, position);
            if (slot.IsDestroyed)
            {
                throw new NullReferenceException($"Entity {entity} not found.");
            }
        }

        [Conditional("DEBUG")]
        public static void ThrowIfEntityPresent(UnsafeWorld* world, eint entity)
        {
            if (entity == uint.MaxValue)
            {
                throw new InvalidOperationException($"Entity {entity} is not valid.");
            }

            uint position = entity - 1;
            uint count = UnsafeList.GetCountRef(world->slots);
            if (position < count)
            {
                ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, position);
                if (!slot.IsDestroyed)
                {
                    throw new InvalidOperationException($"Entity {entity} already present.");
                }
            }
        }

        [Conditional("DEBUG")]
        public static void ThrowIfComponentMissing(UnsafeWorld* world, eint entity, RuntimeType type)
        {
            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, entity - 1);
            UnmanagedDictionary<uint, ComponentChunk> components = GetComponentChunks(world);
            ComponentChunk chunk = components[slot.componentsKey];
            if (!chunk.Types.Contains(type))
            {
                throw new NullReferenceException($"Component {type} not found on {entity}.");
            }
        }

        [Conditional("DEBUG")]
        public static void ThrowIfComponentAlreadyPresent(UnsafeWorld* world, eint entity, RuntimeType type)
        {
            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, entity - 1);
            UnmanagedDictionary<uint, ComponentChunk> components = GetComponentChunks(world);
            ComponentChunk chunk = components[slot.componentsKey];
            if (chunk.Types.Contains(type))
            {
                throw new InvalidOperationException($"Component {type} already present on {entity}.");
            }
        }

        [Conditional("DEBUG")]
        public static void ThrowIfCollectionMissing(UnsafeWorld* world, eint entity, RuntimeType type)
        {
            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, entity - 1);
            if (slot.collections.IsDisposed || !slot.collections.Types.Contains(type))
            {
                throw new NullReferenceException($"Collection of type {type} not found on {entity}.");
            }
        }

        [Conditional("DEBUG")]
        public static void ThrowIfCollectionAlreadyPresent(UnsafeWorld* world, eint entity, RuntimeType type)
        {
            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, entity - 1);
            if (!slot.collections.IsDisposed && slot.collections.Types.Contains(type))
            {
                throw new InvalidOperationException($"Collection of type {type} already present on {entity}.");
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

        public static UnmanagedList<eint> GetFreeEntities(UnsafeWorld* world)
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
            UnsafeList* freeEntities = UnsafeList.Allocate<eint>();
            UnsafeList* submittedEvents = UnsafeList.Allocate<Container>();
            UnsafeList* dispatchingEvents = UnsafeList.Allocate<Container>();
            UnsafeDictionary* listeners = UnsafeDictionary.Allocate<RuntimeType, nint>();
            UnsafeDictionary* components = UnsafeDictionary.Allocate<uint, ComponentChunk>();

            ComponentChunk defaultComponentChunk = new(Array.Empty<RuntimeType>());
            uint chunkKey = defaultComponentChunk.Key;
            UnsafeDictionary.Add(components, chunkKey, defaultComponentChunk);

            UnsafeWorld* world = Allocations.Allocate<UnsafeWorld>();
            *world = new(slots, freeEntities, submittedEvents, dispatchingEvents, listeners, components);
            return world;
        }

        public static void Free(ref UnsafeWorld* world)
        {
            Allocations.ThrowIfNull(world);
            Clear(world);
            UnsafeList.Free(ref world->slots);
            UnsafeList.Free(ref world->freeEntities);
            UnsafeList.Free(ref world->submittedEvents);
            UnsafeList.Free(ref world->dispatchingEvents);
            UnsafeDictionary.Free(ref world->listeners);
            UnsafeDictionary.Free(ref world->components);
            Allocations.Free(ref world);
        }

        public static void Clear(UnsafeWorld* world)
        {
            Allocations.ThrowIfNull(world);

            //clear event containers
            uint eventCount = UnsafeList.GetCountRef(world->submittedEvents);
            for (uint i = 0; i < eventCount; i++)
            {
                Container message = UnsafeList.Get<Container>(world->submittedEvents, i);
                message.Dispose();
            }

            UnsafeList.Clear(world->submittedEvents);

            eventCount = UnsafeList.GetCountRef(world->dispatchingEvents);
            for (uint i = 0; i < eventCount; i++)
            {
                Container message = UnsafeList.Get<Container>(world->dispatchingEvents, i);
                message.Dispose();
            }

            UnsafeList.Clear(world->dispatchingEvents);

            //clear event listeners
            uint listenerTypesCount = UnsafeDictionary.GetCount(world->listeners);
            for (uint i = 0; i < listenerTypesCount; i++)
            {
                RuntimeType eventType = UnsafeDictionary.GetKeyRef<RuntimeType, nint>(world->listeners, i);
                UnsafeList* listenerList = (UnsafeList*)UnsafeDictionary.GetValueRef<RuntimeType, nint>(world->listeners, eventType);
                uint listenerCount = UnsafeList.GetCountRef(listenerList);
                for (uint j = 0; j < listenerCount; j++)
                {
                    Listener listener = UnsafeList.Get<Listener>(listenerList, j);
                    UnsafeListener* unsafeValue = listener.value;
                    UnsafeListener.Free(ref unsafeValue);
                }

                UnsafeList.Free(ref listenerList);
            }

            UnsafeDictionary.Clear(world->listeners);

            //clear chunks
            UnmanagedDictionary<uint, ComponentChunk> components = GetComponentChunks(world);
            for (uint i = 0; i < components.Count; i++)
            {
                uint key = components.Keys[(int)i];
                ComponentChunk chunk = components.Values[(int)i];
                chunk.Dispose();
            }

            UnsafeDictionary.Clear(world->components);

            uint slotCount = UnsafeList.GetCountRef(world->slots);
            for (uint i = 0; i < slotCount; i++)
            {
                ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, i);
                if (!slot.collections.IsDisposed)
                {
                    slot.collections.Dispose();
                }

                if (!slot.children.IsDisposed)
                {
                    slot.children.Dispose();
                }
            }

            UnsafeList.Clear(world->slots);

            //clear free entities
            UnsafeList.Clear(world->freeEntities);
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

            //todo: remove this temp allocation (exists to make sure iterating over this sequence of events works)
            //using UnmanagedArray<Container> tempEvents = new(UnsafeList.AsSpan<Container>(world->submittedEvents));
            uint submissionCount = UnsafeList.GetCountRef(world->submittedEvents);
            for (uint i = 0; i < submissionCount; i++)
            {
                Container message = UnsafeList.Get<Container>(world->submittedEvents, i);
                UnsafeList.Add(world->dispatchingEvents, message);
            }

            UnsafeList.Clear(world->submittedEvents);

            ref uint dispatchCount = ref UnsafeList.GetCountRef(world->dispatchingEvents);
            Exception? caughtException = null;
            while (dispatchCount > 0)
            {
                Container message = UnsafeList.RemoveAtBySwapping<Container>(world->dispatchingEvents, 0);
                RuntimeType eventType = message.type;
                if (UnsafeDictionary.ContainsKey<RuntimeType, nint>(world->listeners, eventType))
                {
                    UnsafeList* listenerList = (UnsafeList*)UnsafeDictionary.GetValueRef<RuntimeType, nint>(world->listeners, eventType);
                    uint j = 0;
                    while (j < UnsafeList.GetCountRef(listenerList))
                    {
                        Listener listener = UnsafeList.Get<Listener>(listenerList, j);
                        try
                        {
                            listener.Invoke(worldValue, message);
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
                if (caughtException is not null)
                {
                    throw caughtException;
                }
            }
        }

#if NET5_0_OR_GREATER
        public static Listener CreateListener(UnsafeWorld* world, RuntimeType eventType, delegate* unmanaged<World, Container, void> callback)
        {
            Listener listener = new(new(world), eventType, callback);
            if (!UnsafeDictionary.ContainsKey<RuntimeType, nint>(world->listeners, eventType))
            {
                UnsafeList* listenerList = UnsafeList.Allocate<Listener>();
                UnsafeList.Add(listenerList, listener);
                UnsafeDictionary.Add(world->listeners, eventType, (nint)listenerList);
            }
            else
            {
                UnsafeList* listenerList = (UnsafeList*)UnsafeDictionary.GetValueRef<RuntimeType, nint>(world->listeners, eventType);
                UnsafeList.Add(listenerList, listener);
            }

            return listener;
        }
#else
        public static Listener CreateListener(UnsafeWorld* world, RuntimeType eventType, delegate*<World, Container, void> callback)
        {
            Listener listener = new(new(world), eventType, callback);
            if (!UnsafeDictionary.ContainsKey<RuntimeType, nint>(world->listeners, eventType))
            {
                UnsafeList* listenerList = UnsafeList.Allocate<Listener>();
                UnsafeList.Add(listenerList, listener);
                UnsafeDictionary.Add(world->listeners, eventType, (nint)listenerList);
            }
            else
            {
                UnsafeList* listenerList = (UnsafeList*)UnsafeDictionary.GetValueRef<RuntimeType, nint>(world->listeners, eventType);
                UnsafeList.Add(listenerList, listener);
            }

            return listener;
        }
#endif

        internal static void RemoveListener(UnsafeWorld* world, Listener listener)
        {
            if (UnsafeDictionary.ContainsKey<RuntimeType, nint>(world->listeners, listener.eventType))
            {
                UnsafeList* listenerList = (UnsafeList*)UnsafeDictionary.GetValueRef<RuntimeType, nint>(world->listeners, listener.eventType);
                UnsafeListener* unsafeListener = listener.value;
                UnsafeListener.Free(ref unsafeListener);
                uint index = UnsafeList.IndexOf(listenerList, listener);
                UnsafeList.RemoveAtBySwapping(listenerList, index);
            }
            else
            {
                throw new NullReferenceException($"Listener for {listener.eventType} not found.");
            }
        }

        public static void DestroyEntity(UnsafeWorld* world, eint entity, bool destroyChildren = true)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityMissing(world, entity);

            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, entity - 1);

            //destroy or orphan the children
            if (!slot.children.IsDisposed)
            {
                if (destroyChildren)
                {
                    for (uint i = 0; i < slot.children.Count; i++)
                    {
                        eint child = new(slot.children[i]);
                        DestroyEntity(world, child, true);
                    }
                }
                else
                {
                    for (uint i = 0; i < slot.children.Count; i++)
                    {
                        uint childValue = slot.children[i];
                        ref EntityDescription childSlot = ref UnsafeList.GetRef<EntityDescription>(world->slots, childValue - 1);
                        childSlot.parent = default;
                    }
                }

                slot.children.Dispose();
            }

            UnmanagedDictionary<uint, ComponentChunk> components = GetComponentChunks(world);
            ComponentChunk chunk = components[slot.componentsKey];
            chunk.Remove(entity);

            if (!slot.collections.IsDisposed)
            {
                slot.collections.Dispose();
            }

            slot.entity = default;
            slot.parent = default;
            slot.componentsKey = default;
            slot.collections = default;
            slot.state = EntityDescription.State.Destroyed;
            UnsafeList.Add(world->freeEntities, entity);
            NotifyDestruction(new(world), entity);
        }

        public static ref EntityDescription GetEntitySlotRef(UnsafeWorld* world, eint entity)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityMissing(world, entity);
            return ref UnsafeList.GetRef<EntityDescription>(world->slots, entity - 1);
        }

        public static ReadOnlySpan<RuntimeType> GetComponentTypes(UnsafeWorld* world, eint entity)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityMissing(world, entity);

            EntityDescription slot = GetEntitySlotRef(world, entity);
            UnmanagedDictionary<uint, ComponentChunk> components = GetComponentChunks(world);
            ComponentChunk chunk = components[slot.componentsKey];
            return chunk.Types;
        }

        public static ReadOnlySpan<RuntimeType> GetListTypes(UnsafeWorld* world, eint entity)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityMissing(world, entity);

            EntityDescription slot = GetEntitySlotRef(world, entity);
            if (slot.collections.IsDisposed)
            {
                return ReadOnlySpan<RuntimeType>.Empty;
            }

            return slot.collections.Types;
        }

        public static eint GetParent(UnsafeWorld* world, eint entity)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityMissing(world, entity);

            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, entity - 1);
            return new(slot.parent);
        }

        public static bool SetParent(UnsafeWorld* world, eint entity, eint parent)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityMissing(world, entity);

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
        public static eint GetNextEntity(UnsafeWorld* world)
        {
            Allocations.ThrowIfNull(world);
            if (UnsafeList.GetCountRef(world->freeEntities) > 0)
            {
                return UnsafeList.Get<eint>(world->freeEntities, 0);
            }
            else
            {
                uint index = UnsafeList.GetCountRef(world->slots) + 1;
                return new(index);
            }
        }

        /// <summary>
        /// Initializes the given entity value into existence assuming 
        /// its not already present.
        /// </summary>
        public static void InitializeEntity(UnsafeWorld* world, eint entity, eint parent, ReadOnlySpan<RuntimeType> componentTypes)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityPresent(world, entity);

            UnmanagedList<EntityDescription> slots = GetEntitySlots(world);
            UnmanagedList<eint> freeEntities = GetFreeEntities(world);

            //make sure islands dont exist
            while (entity > slots.Count + 1)
            {
                EntityDescription slot = new(slots.Count + 1, default);
                UnsafeList.Add(world->slots, slot);
                freeEntities.Add(new(slot.entity));
            }

            //add child reference into parent's slot
            if (!ContainsEntity(world, parent))
            {
                parent = default;
            }
            else
            {
                ref EntityDescription parentSlot = ref slots.GetRef(parent - 1);
                if (parentSlot.children.IsDisposed)
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
                eint newEntity = new(slots.Count + 1);
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

            chunk.Add(entity);

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

        public static UnsafeList* CreateCollection(UnsafeWorld* world, eint entity, RuntimeType type, uint initialCapacity = 1)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityMissing(world, entity);
            ThrowIfCollectionAlreadyPresent(world, entity, type);

            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, entity - 1);
            if (slot.collections.IsDisposed)
            {
                slot.collections = EntityCollections.Create();
            }

            return slot.collections.CreateCollection(type, initialCapacity);
        }

        public static bool ContainsList<T>(UnsafeWorld* world, eint entity) where T : unmanaged
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityMissing(world, entity);

            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, entity - 1);
            if (!slot.collections.IsDisposed)
            {
                return slot.collections.Types.Contains(RuntimeType.Get<T>());
            }
            else
            {
                return false;
            }
        }

        public static UnsafeList* GetList(UnsafeWorld* world, eint entity, RuntimeType type)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityMissing(world, entity);
            ThrowIfCollectionMissing(world, entity, type);

            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, entity - 1);
            return slot.collections.GetCollection(type);
        }

        public static void DestroyList<T>(UnsafeWorld* world, eint entity) where T : unmanaged
        {
            DestroyList(world, entity, RuntimeType.Get<T>());
        }

        public static void DestroyList(UnsafeWorld* world, eint entity, RuntimeType type)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityMissing(world, entity);
            ThrowIfCollectionMissing(world, entity, type);

            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, entity - 1);
            slot.collections.RemoveCollection(type);
        }

        public static void* AddComponent(UnsafeWorld* world, eint entity, RuntimeType type)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityMissing(world, entity);
            ThrowIfComponentAlreadyPresent(world, entity, type);

            UnmanagedDictionary<uint, ComponentChunk> components = GetComponentChunks(world);
            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, entity - 1);
            uint previousTypesKey = slot.componentsKey;
            ComponentChunk current = components[previousTypesKey];
            ReadOnlySpan<RuntimeType> oldTypes = current.Types;
            Span<RuntimeType> newTypes = stackalloc RuntimeType[oldTypes.Length + 1];
            oldTypes.CopyTo(newTypes);
            newTypes[^1] = type;
            uint newTypesKey = RuntimeType.CombineHash(newTypes);
            slot.componentsKey = newTypesKey;

            if (!components.TryGetValue(newTypesKey, out ComponentChunk destination))
            {
                destination = new(newTypes);
                components.Add(newTypesKey, destination);
            }

            uint index = current.Move(entity, destination);
            return destination.GetComponentPointer(index, type);
        }

        public static void RemoveComponent<T>(UnsafeWorld* world, eint entity) where T : unmanaged
        {
            RemoveComponent(world, entity, RuntimeType.Get<T>());
        }

        public static void RemoveComponent(UnsafeWorld* world, eint entity, RuntimeType type)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityMissing(world, entity);
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

            current.Move(entity, destination);
            NotifyComponentRemoved(new(world), entity, type);
        }

        public static bool ContainsComponent(UnsafeWorld* world, eint entity, RuntimeType type)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityMissing(world, entity);

            uint index = entity - 1;
            UnmanagedDictionary<uint, ComponentChunk> components = GetComponentChunks(world);
            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, index);
            ComponentChunk chunk = components[slot.componentsKey];
            return chunk.Types.Contains(type);
        }

        public static ref T GetComponentRef<T>(UnsafeWorld* world, eint entity) where T : unmanaged
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityMissing(world, entity);

            RuntimeType type = RuntimeType.Get<T>();
            ThrowIfComponentMissing(world, entity, type);

            UnmanagedDictionary<uint, ComponentChunk> components = GetComponentChunks(world);
            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, entity - 1);
            ComponentChunk chunk = components[slot.componentsKey];
            return ref chunk.GetComponentRef<T>(entity);
        }

        public static Span<byte> GetComponentBytes(UnsafeWorld* world, eint entity, RuntimeType type)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityMissing(world, entity);
            ThrowIfComponentMissing(world, entity, type);

            UnmanagedDictionary<uint, ComponentChunk> components = GetComponentChunks(world);
            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, entity - 1);
            ComponentChunk chunk = components[slot.componentsKey];
            return chunk.GetComponentBytes(entity, type);
        }

        internal static void NotifyCreation(World world, eint entity)
        {
            EntityCreated(world, entity);
        }

        internal static void NotifyDestruction(World world, eint entity)
        {
            EntityDestroyed(world, entity);
        }

        internal static void NotifyParentChange(World world, eint entity, eint parent)
        {
            EntityParentChanged(world, entity, parent);
        }

        internal static void NotifyComponentAdded(World world, eint entity, RuntimeType type)
        {
            ComponentAdded(world, entity, type);
        }

        internal static void NotifyComponentRemoved(World world, eint entity, RuntimeType type)
        {
            ComponentRemoved(world, entity, type);
        }
    }
}