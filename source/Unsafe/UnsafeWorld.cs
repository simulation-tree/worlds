#if !DEBUG
#define IGNORE_STACKTRACES
#endif

using Game.ECS;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unmanaged;
using Unmanaged.Collections;

namespace Game.Unsafe
{
    public unsafe struct UnsafeWorld
    {
#if !IGNORE_STACKTRACES
        internal static readonly Dictionary<EntityID, StackTrace> createStackTraces = [];
#endif

        /// <summary>
        /// Invoked after any entity is created in any world.
        /// </summary>
        public static event CreatedCallback EntityCreated = delegate { };

        /// <summary>
        /// Invoked after any entity is destroyed from any world.
        /// </summary>
        public static event DestroyedCallback EntityDestroyed = delegate { };

        private readonly uint id;
        private UnsafeList* slots;
        private UnsafeList* freeEntities;
        private UnsafeList* events;
        private UnsafeDictionary* listeners;
        private UnsafeDictionary* listenersWithContext;
        private UnsafeDictionary* components;

        private UnsafeWorld(uint id, UnsafeList* slots, UnsafeList* freeEntities, UnsafeList* events, UnsafeDictionary* listeners, UnsafeDictionary* listenersWithContext, UnsafeDictionary* components)
        {
            this.id = id;
            this.slots = slots;
            this.freeEntities = freeEntities;
            this.events = events;
            this.listeners = listeners;
            this.listenersWithContext = listenersWithContext;
            this.components = components;
        }

        [Conditional("DEBUG")]
        public static void ThrowIfEntityMissing(UnsafeWorld* world, EntityID entity)
        {
            if (entity.value == uint.MaxValue)
            {
                throw new InvalidOperationException($"Entity {entity} is not valid.");
            }

            uint position = entity.value - 1;
            uint count = UnsafeList.GetCountRef(world->slots);
            if (position >= count)
            {
                throw new NullReferenceException($"Entity {entity} not found.");
            }

            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, position);
            if (slot.entity != entity)
            {
                throw new NullReferenceException($"Entity {entity} not found.");
            }
        }

        [Conditional("DEBUG")]
        private static void ThrowIfComponentMissing(UnsafeWorld* world, EntityID entity, RuntimeType type)
        {
            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, entity.value - 1);
            UnmanagedDictionary<int, ComponentChunk> components = GetComponentChunks(world);
            ComponentChunk chunk = components[slot.componentsKey];
            if (!chunk.Types.Contains(type))
            {
                throw new NullReferenceException($"Component {type} not found on {entity}.");
            }
        }

        [Conditional("DEBUG")]
        private static void ThrowIfComponentAlreadyPresent(UnsafeWorld* world, EntityID entity, RuntimeType type)
        {
            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, entity.value - 1);
            UnmanagedDictionary<int, ComponentChunk> components = GetComponentChunks(world);
            ComponentChunk chunk = components[slot.componentsKey];
            if (chunk.Types.Contains(type))
            {
                throw new InvalidOperationException($"Component {type} already present on {entity}.");
            }
        }

        [Conditional("DEBUG")]
        private static void ThrowIfCollectionMissing(UnsafeWorld* world, EntityID entity, RuntimeType type)
        {
            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, entity.value - 1);
            if (slot.collections.IsDisposed || !slot.collections.Types.Contains(type))
            {
                throw new NullReferenceException($"Collection of type {type} not found on {entity}.");
            }
        }

        [Conditional("DEBUG")]
        private static void ThrowIfCollectionAlreadyPresent(UnsafeWorld* world, EntityID entity, RuntimeType type)
        {
            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, entity.value - 1);
            if (!slot.collections.IsDisposed && slot.collections.Types.Contains(type))
            {
                throw new InvalidOperationException($"Collection of type {type} already present on {entity}.");
            }
        }

        public static uint GetID(UnsafeWorld* world)
        {
            Allocations.ThrowIfNull(world);
            return world->id;
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

        public static UnmanagedList<EntityID> GetFreeEntities(UnsafeWorld* world)
        {
            Allocations.ThrowIfNull(world);
            return new(world->freeEntities);
        }

        public static UnmanagedDictionary<int, ComponentChunk> GetComponentChunks(UnsafeWorld* world)
        {
            Allocations.ThrowIfNull(world);
            return new(world->components);
        }

        public static UnsafeWorld* Allocate()
        {
            if (!Universe.destroyedWorlds.TryTake(out uint id))
            {
                Universe.createdWorlds++;
                id = Universe.createdWorlds;
            }

            UnsafeList* slots = UnsafeList.Allocate<EntityDescription>();
            UnsafeList* freeEntities = UnsafeList.Allocate<EntityID>();
            UnsafeList* events = UnsafeList.Allocate<Container>();
            UnsafeDictionary* listeners = UnsafeDictionary.Allocate<RuntimeType, nint>();
            UnsafeDictionary* listenersWithContext = UnsafeDictionary.Allocate<RuntimeType, nint>();
            UnsafeDictionary* components = UnsafeDictionary.Allocate<int, ComponentChunk>();

            ComponentChunk defaultComponentChunk = new([]);
            int chunkKey = defaultComponentChunk.Key;
            UnsafeDictionary.Add(components, chunkKey, defaultComponentChunk);

            UnsafeWorld* world = Allocations.Allocate<UnsafeWorld>();
            *world = new(id, slots, freeEntities, events, listeners, listenersWithContext, components);
            return world;
        }

        public static void Free(ref UnsafeWorld* world)
        {
            Allocations.ThrowIfNull(world);
            uint id = GetID(world);
            Clear(world);
            UnsafeList.Free(ref world->slots);
            UnsafeList.Free(ref world->freeEntities);
            UnsafeList.Free(ref world->events);
            UnsafeDictionary.Free(ref world->listeners);
            UnsafeDictionary.Free(ref world->listenersWithContext);
            UnsafeDictionary.Free(ref world->components);
            Allocations.Free(ref world);
            Universe.destroyedWorlds.Add(id);
        }

        public static void Clear(UnsafeWorld* world)
        {
            Allocations.ThrowIfNull(world);

            //clear event containers
            uint eventCount = UnsafeList.GetCountRef(world->events);
            for (uint i = 0; i < eventCount; i++)
            {
                Container message = UnsafeList.Get<Container>(world->events, i);
                message.Dispose();
            }

            UnsafeList.Clear(world->events);

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

            listenerTypesCount = UnsafeDictionary.GetCount(world->listenersWithContext);
            for (uint i = 0; i < listenerTypesCount; i++)
            {
                RuntimeType eventType = UnsafeDictionary.GetKeyRef<RuntimeType, nint>(world->listenersWithContext, i);
                UnsafeList* listenerList = (UnsafeList*)UnsafeDictionary.GetValueRef<RuntimeType, nint>(world->listenersWithContext, eventType);
                uint listenerCount = UnsafeList.GetCountRef(listenerList);
                for (uint j = 0; j < listenerCount; j++)
                {
                    ListenerWithContext listener = UnsafeList.Get<ListenerWithContext>(listenerList, j);
                    UnsafeListener* unsafeValue = listener.value;
                    UnsafeListener.Free(ref unsafeValue);
                }

                UnsafeList.Free(ref listenerList);
            }

            UnsafeDictionary.Clear(world->listenersWithContext);

            //clear chunks
            UnmanagedDictionary<int, ComponentChunk> components = GetComponentChunks(world);
            for (uint i = 0; i < components.Count; i++)
            {
                int key = components.Keys[(int)i];
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
            }

            UnsafeList.Clear(world->slots);

            //clear free entities
            UnsafeList.Clear(world->freeEntities);
        }

        public static void Submit(UnsafeWorld* world, Container message)
        {
            UnsafeList.Add(world->events, message);
        }

        /// <summary>
        /// Polls all submitted events and invokes listeners.
        /// </summary>
        public static void Poll(UnsafeWorld* world)
        {
            World worldValue = new(world);
            using UnmanagedArray<Container> tempEvents = new(UnsafeList.AsSpan<Container>(world->events));
            UnsafeList.Clear(world->events);

            for (uint i = 0; i < tempEvents.Length; i++)
            {
                using Container message = tempEvents[i];
                RuntimeType eventType = message.type;
                if (UnsafeDictionary.ContainsKey<RuntimeType, nint>(world->listeners, eventType))
                {
                    UnsafeList* listenerList = (UnsafeList*)UnsafeDictionary.GetValueRef<RuntimeType, nint>(world->listeners, eventType);
                    uint j = 0;
                    while (j < UnsafeList.GetCountRef(listenerList))
                    {
                        Listener listener = UnsafeList.Get<Listener>(listenerList, j);
                        listener.Invoke(worldValue, message);
                        j++;
                    }
                }

                if (UnsafeDictionary.ContainsKey<RuntimeType, nint>(world->listenersWithContext, eventType))
                {
                    UnsafeList* listenerList = (UnsafeList*)UnsafeDictionary.GetValueRef<RuntimeType, nint>(world->listenersWithContext, eventType);
                    uint eventListenerCount = UnsafeList.GetCountRef(listenerList);
                    for (uint j = 0; j < eventListenerCount; j++)
                    {
                        ListenerWithContext listener = UnsafeList.Get<ListenerWithContext>(listenerList, j);
                        listener.Invoke(worldValue, message);
                    }
                }
            }
        }

        public static Listener Listen(UnsafeWorld* world, RuntimeType eventType, delegate* unmanaged<World, Container, void> callback)
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

        public static ListenerWithContext Listen(UnsafeWorld* world, nint pointer, RuntimeType eventType, delegate* unmanaged<nint, World, Container, void> callback)
        {
            ListenerWithContext listener = new(pointer, new(world), eventType, callback);
            if (!UnsafeDictionary.ContainsKey<RuntimeType, nint>(world->listenersWithContext, eventType))
            {
                UnsafeList* listenerList = UnsafeList.Allocate<ListenerWithContext>();
                UnsafeList.Add(listenerList, listener);
                UnsafeDictionary.Add(world->listenersWithContext, eventType, (nint)listenerList);
            }
            else
            {
                UnsafeList* listenerList = (UnsafeList*)UnsafeDictionary.GetValueRef<RuntimeType, nint>(world->listenersWithContext, eventType);
                UnsafeList.Add(listenerList, listener);
            }

            return listener;
        }

        public static void Unlisten(UnsafeWorld* world, Listener listener)
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

        public static void Unlisten(UnsafeWorld* world, ListenerWithContext listener)
        {
            if (UnsafeDictionary.ContainsKey<RuntimeType, nint>(world->listenersWithContext, listener.eventType))
            {
                UnsafeList* listenerList = (UnsafeList*)UnsafeDictionary.GetValueRef<RuntimeType, nint>(world->listenersWithContext, listener.eventType);
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

        public static void DestroyEntity(UnsafeWorld* world, EntityID entity)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityMissing(world, entity);

            UnmanagedDictionary<int, ComponentChunk> components = GetComponentChunks(world);
            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, entity.value - 1);
            ComponentChunk chunk = components[slot.componentsKey];
            chunk.Remove(entity);

            if (!slot.collections.IsDisposed)
            {
                slot.collections.Dispose();
            }

            slot.entity = default;
            slot.componentsKey = default;
            slot.collections = default;
            slot.version++;
            UnsafeList.Add(world->freeEntities, entity);
            EntityDestroyed(new(world), entity);
        }

        public static ReadOnlySpan<RuntimeType> GetComponents(UnsafeWorld* world, EntityID id)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityMissing(world, id);

            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, id.value - 1);
            UnmanagedDictionary<int, ComponentChunk> components = GetComponentChunks(world);
            ComponentChunk chunk = components[slot.componentsKey];
            return chunk.Types;
        }

        /// <summary>
        /// Creates a new entity with blank components of the given types.
        /// </summary>
        public static EntityID CreateEntity(UnsafeWorld* world, ReadOnlySpan<RuntimeType> componentTypes)
        {
            Allocations.ThrowIfNull(world);
            int componentsKey = RuntimeType.CalculateHash(componentTypes);
            UnmanagedDictionary<int, ComponentChunk> components = GetComponentChunks(world);
            if (!components.TryGetValue(componentsKey, out ComponentChunk chunk))
            {
                chunk = new(componentTypes);
                components.Add(componentsKey, chunk);
            }

            EntityID createdEntity;
            if (UnsafeList.GetCountRef(world->freeEntities) > 0)
            {
                EntityID oldEntity = UnsafeList.Get<EntityID>(world->freeEntities, 0);
                UnsafeList.RemoveAtBySwapping(world->freeEntities, 0);

                ref EntityDescription oldSlot = ref UnsafeList.GetRef<EntityDescription>(world->slots, oldEntity.value - 1);
                oldSlot.entity = oldEntity;
                oldSlot.version++;
                oldSlot.componentsKey = componentsKey;
                chunk.Add(oldEntity);
                createdEntity = oldEntity;
            }
            else
            {
                uint index = UnsafeList.GetCountRef(world->slots) + 1;
                EntityID newEntity = new(index);
                EntityDescription newSlot = new(newEntity, 0, componentsKey);
                UnsafeList.Add(world->slots, newSlot);
                chunk.Add(newEntity);
                createdEntity = newEntity;
            }

#if !IGNORE_STACKTRACES
            StackTrace stackTrace = new(2, true);
            if (stackTrace.FrameCount > 0)
            {
                string? firstFrame = stackTrace.GetFrame(0)?.GetFileName();
                if (firstFrame is not null && firstFrame.EndsWith("World.cs"))
                {
                    stackTrace = new(3, true);
                }
            }

            createStackTraces[createdEntity] = stackTrace;
#endif

            EntityCreated(new(world), createdEntity);
            return createdEntity;
        }

        public static bool ContainsEntity(UnsafeWorld* world, uint value)
        {
            Allocations.ThrowIfNull(world);
            if (value == uint.MaxValue)
            {
                return false;
            }

            uint position = value - 1;
            if (position >= UnsafeList.GetCountRef(world->slots))
            {
                return false;
            }

            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, position);
            return slot.entity.value == value;
        }

        public static UnsafeList* CreateCollection(UnsafeWorld* world, EntityID entity, RuntimeType type, uint initialCapacity = 1)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityMissing(world, entity);

            ThrowIfCollectionAlreadyPresent(world, entity, type);

            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, entity.value - 1);
            if (slot.collections.IsDisposed)
            {
                slot.collections = new();
            }

            return slot.collections.CreateCollection(type, initialCapacity);
        }

        public static bool ContainsCollection<T>(UnsafeWorld* world, EntityID entity) where T : unmanaged
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityMissing(world, entity);

            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, entity.value - 1);
            if (!slot.collections.IsDisposed)
            {
                return slot.collections.Types.Contains(RuntimeType.Get<T>());
            }
            else
            {
                return false;
            }
        }

        public static UnsafeList* GetCollection(UnsafeWorld* world, EntityID entity, RuntimeType type)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityMissing(world, entity);
            ThrowIfCollectionMissing(world, entity, type);

            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, entity.value - 1);
            return slot.collections.GetCollection(type);
        }

        public static void DestroyCollection<T>(UnsafeWorld* world, EntityID entity) where T : unmanaged
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityMissing(world, entity);

            RuntimeType type = RuntimeType.Get<T>();
            ThrowIfCollectionMissing(world, entity, type);

            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, entity.value - 1);
            slot.collections.RemoveCollection<T>();
        }

        public static ref T AddComponentRef<T>(UnsafeWorld* world, EntityID entity) where T : unmanaged
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityMissing(world, entity);

            RuntimeType addingType = RuntimeType.Get<T>();
            ThrowIfComponentAlreadyPresent(world, entity, addingType);

            UnmanagedDictionary<int, ComponentChunk> components = GetComponentChunks(world);
            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, entity.value - 1);
            int previousTypesKey = slot.componentsKey;
            ComponentChunk current = components[previousTypesKey];
            ReadOnlySpan<RuntimeType> oldTypes = current.Types;
            Span<RuntimeType> newTypes = stackalloc RuntimeType[oldTypes.Length + 1];
            oldTypes.CopyTo(newTypes);
            newTypes[^1] = addingType;
            int newTypesKey = RuntimeType.CalculateHash(newTypes);
            slot.componentsKey = newTypesKey;

            if (!components.TryGetValue(newTypesKey, out ComponentChunk destination))
            {
                destination = new(newTypes);
                components.Add(newTypesKey, destination);
            }

            uint index = current.Move(entity, destination);
            return ref destination.GetComponentRef<T>(index);
        }

        public static void* AddComponent(UnsafeWorld* world, EntityID entity, RuntimeType type)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityMissing(world, entity);

            ThrowIfComponentAlreadyPresent(world, entity, type);

            UnmanagedDictionary<int, ComponentChunk> components = GetComponentChunks(world);
            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, entity.value - 1);
            int previousTypesKey = slot.componentsKey;
            ComponentChunk current = components[previousTypesKey];
            ReadOnlySpan<RuntimeType> oldTypes = current.Types;
            Span<RuntimeType> newTypes = stackalloc RuntimeType[oldTypes.Length + 1];
            oldTypes.CopyTo(newTypes);
            newTypes[^1] = type;
            int newTypesKey = RuntimeType.CalculateHash(newTypes);
            slot.componentsKey = newTypesKey;

            if (!components.TryGetValue(newTypesKey, out ComponentChunk destination))
            {
                destination = new(newTypes);
                components.Add(newTypesKey, destination);
            }

            uint index = current.Move(entity, destination);
            return destination.GetComponent(index, type);
        }

        public static void RemoveComponent<T>(UnsafeWorld* world, EntityID entity) where T : unmanaged
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityMissing(world, entity);

            RuntimeType removingType = RuntimeType.Get<T>();
            ThrowIfComponentMissing(world, entity, removingType);

            UnmanagedDictionary<int, ComponentChunk> components = GetComponentChunks(world);
            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, entity.value - 1);
            int previousTypesKey = slot.componentsKey;
            ComponentChunk current = components[previousTypesKey];
            ReadOnlySpan<RuntimeType> oldTypes = current.Types;
            Span<RuntimeType> newTypes = stackalloc RuntimeType[oldTypes.Length - 1];
            int count = 0;
            for (int i = 0; i < oldTypes.Length; i++)
            {
                if (oldTypes[i] != removingType)
                {
                    newTypes[count] = oldTypes[i];
                    count++;
                }
            }

            int newTypesKey = RuntimeType.CalculateHash(newTypes);
            slot.componentsKey = newTypesKey;

            if (!components.TryGetValue(newTypesKey, out ComponentChunk destination))
            {
                destination = new(newTypes);
                components.Add(newTypesKey, destination);
            }

            current.Move(entity, destination);
        }

        public static bool ContainsComponent(UnsafeWorld* world, EntityID entity, RuntimeType type)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityMissing(world, entity);

            uint index = entity.value - 1;
            UnmanagedDictionary<int, ComponentChunk> components = GetComponentChunks(world);
            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, index);
            ComponentChunk chunk = components[slot.componentsKey];
            return chunk.Types.Contains(type);
        }

        public static ref T GetComponentRef<T>(UnsafeWorld* world, EntityID entity) where T : unmanaged
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityMissing(world, entity);

            RuntimeType type = RuntimeType.Get<T>();
            ThrowIfComponentMissing(world, entity, type);

            UnmanagedDictionary<int, ComponentChunk> components = GetComponentChunks(world);
            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, entity.value - 1);
            ComponentChunk chunk = components[slot.componentsKey];
            return ref chunk.GetComponentRef<T>(entity);
        }

        public static Span<byte> GetComponentBytes(UnsafeWorld* world, EntityID entity, RuntimeType type)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityMissing(world, entity);
            ThrowIfComponentMissing(world, entity, type);

            UnmanagedDictionary<int, ComponentChunk> components = GetComponentChunks(world);
            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, entity.value - 1);
            ComponentChunk chunk = components[slot.componentsKey];
            return chunk.GetComponentBytes(entity, type);
        }

        private static bool Contains(ReadOnlySpan<RuntimeType> container, ReadOnlySpan<RuntimeType> values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                if (!container.Contains(values[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public static void QueryComponents<T1>(UnsafeWorld* world, QueryCallback<T1> action) where T1 : unmanaged
        {
            Allocations.ThrowIfNull(world);
            ReadOnlySpan<RuntimeType> types = [RuntimeType.Get<T1>()];
            UnmanagedDictionary<int, ComponentChunk> components = GetComponentChunks(world);
            for (uint i = 0; i < components.Count; i++)
            {
                int key = components.Keys[(int)i];
                ComponentChunk chunk = components.Values[(int)i];
                if (Contains(chunk.Types, types))
                {
                    UnmanagedList<EntityID> entities = chunk.Entities;
                    for (uint e = 0; e < entities.Count; e++)
                    {
                        ref T1 t1 = ref chunk.GetComponentRef<T1>(e);
                        action(entities[e], ref t1);
                    }
                }
            }
        }

        public static void QueryComponents<T1, T2>(UnsafeWorld* world, QueryCallback<T1, T2> action) where T1 : unmanaged where T2 : unmanaged
        {
            Allocations.ThrowIfNull(world);
            ReadOnlySpan<RuntimeType> types = [RuntimeType.Get<T1>(), RuntimeType.Get<T2>()];
            UnmanagedDictionary<int, ComponentChunk> components = GetComponentChunks(world);
            for (uint i = 0; i < components.Count; i++)
            {
                int key = components.Keys[(int)i];
                ComponentChunk chunk = components.Values[(int)i];
                if (Contains(chunk.Types, types))
                {
                    UnmanagedList<EntityID> entities = chunk.Entities;
                    for (uint e = 0; e < entities.Count; e++)
                    {
                        ref T1 t1 = ref chunk.GetComponentRef<T1>(e);
                        ref T2 t2 = ref chunk.GetComponentRef<T2>(e);
                        action(entities[e], ref t1, ref t2);
                    }
                }
            }
        }

        public static void QueryComponents<T1, T2, T3>(UnsafeWorld* world, QueryCallback<T1, T2, T3> action) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            Allocations.ThrowIfNull(world);
            ReadOnlySpan<RuntimeType> types = [RuntimeType.Get<T1>(), RuntimeType.Get<T2>(), RuntimeType.Get<T3>()];
            UnmanagedDictionary<int, ComponentChunk> components = GetComponentChunks(world);
            for (uint i = 0; i < components.Count; i++)
            {
                int key = components.Keys[(int)i];
                ComponentChunk chunk = components.Values[(int)i];
                if (Contains(chunk.Types, types))
                {
                    UnmanagedList<EntityID> entities = chunk.Entities;
                    for (uint e = 0; e < entities.Count; e++)
                    {
                        ref T1 t1 = ref chunk.GetComponentRef<T1>(e);
                        ref T2 t2 = ref chunk.GetComponentRef<T2>(e);
                        ref T3 t3 = ref chunk.GetComponentRef<T3>(e);
                        action(entities[e], ref t1, ref t2, ref t3);
                    }
                }
            }
        }

        public static void QueryComponents<T1, T2, T3, T4>(UnsafeWorld* world, QueryCallback<T1, T2, T3, T4> action) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            Allocations.ThrowIfNull(world);
            ReadOnlySpan<RuntimeType> types = [RuntimeType.Get<T1>(), RuntimeType.Get<T2>(), RuntimeType.Get<T3>(), RuntimeType.Get<T4>()];
            UnmanagedDictionary<int, ComponentChunk> components = GetComponentChunks(world);
            for (uint i = 0; i < components.Count; i++)
            {
                int key = components.Keys[(int)i];
                ComponentChunk chunk = components.Values[(int)i];
                if (Contains(chunk.Types, types))
                {
                    UnmanagedList<EntityID> entities = chunk.Entities;
                    for (uint e = 0; e < entities.Count; e++)
                    {
                        ref T1 t1 = ref chunk.GetComponentRef<T1>(e);
                        ref T2 t2 = ref chunk.GetComponentRef<T2>(e);
                        ref T3 t3 = ref chunk.GetComponentRef<T3>(e);
                        ref T4 t4 = ref chunk.GetComponentRef<T4>(e);
                        action(entities[e], ref t1, ref t2, ref t3, ref t4);
                    }
                }
            }
        }
    }
}