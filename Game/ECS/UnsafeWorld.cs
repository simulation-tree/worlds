using Game.ECS;
using System;
using System.Diagnostics;
using Unmanaged;
using Unmanaged.Collections;

namespace Game
{
    public unsafe struct UnsafeWorld
    {
        /// <summary>
        /// Invoked when any entity is created in any world.
        /// </summary>
        public static event CreatedCallback EntityCreated = delegate { };

        /// <summary>
        /// Invoked when any entity is destroyed from any world.
        /// </summary>
        public static event DestroyedCallback EntityDestroyed = delegate { };

        private readonly uint id;
        private UnsafeList* slots;
        private UnsafeList* freeEntities;
        private UnsafeList* collectionArchetypes;
        private UnsafeList* events;
        private UnsafeDictionary* listeners;
        private UnsafeDictionary* listenersWithContext;
        private UnsafeDictionary* components;

        private readonly ref CollectionOfCollections?[] Collections => ref Universe.collections[(int)id - 1];

        private UnsafeWorld(uint id, UnsafeList* slots, UnsafeList* freeEntities, UnsafeList* collectionArchetypes, UnsafeList* events, UnsafeDictionary* listeners, UnsafeDictionary* listenersWithContext, UnsafeDictionary* components)
        {
            this.id = id;
            this.slots = slots;
            this.freeEntities = freeEntities;
            this.collectionArchetypes = collectionArchetypes;
            this.events = events;
            this.listeners = listeners;
            this.listenersWithContext = listenersWithContext;
            this.components = components;
        }

        [Conditional("DEBUG")]
        private static void ThrowIfEntityMissing(UnsafeWorld* world, EntityID id)
        {
            uint index = id.value - 1;
            uint count = UnsafeList.GetCount(world->slots);
            if (index >= count)
            {
                throw new NullReferenceException($"Entity {id} not found.");
            }

            ref EntityDescription entity = ref UnsafeList.GetRef<EntityDescription>(world->slots, index);
            if (entity.id != id)
            {
                throw new NullReferenceException($"Entity {id} not found.");
            }
        }

        [Conditional("DEBUG")]
        private static void ThrowIfComponentMissing(UnsafeWorld* world, EntityID entity, RuntimeType type)
        {
            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, entity.value - 1);
            UnsafeComponentChunk* chunk = (UnsafeComponentChunk*)UnsafeDictionary.GetValueRef<int, nint>(world->components, slot.componentsKey);
            ReadOnlySpan<RuntimeType> types = UnsafeComponentChunk.GetTypes(chunk);
            if (!types.Contains(type))
            {
                throw new NullReferenceException($"Component {type} not found on {entity}.");
            }
        }

        [Conditional("DEBUG")]
        private static void ThrowIfComponentAlreadyPresent(UnsafeWorld* world, EntityID entity, RuntimeType type)
        {
            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, entity.value - 1);
            UnsafeComponentChunk* chunk = (UnsafeComponentChunk*)UnsafeDictionary.GetValueRef<int, nint>(world->components, slot.componentsKey);
            ReadOnlySpan<RuntimeType> types = UnsafeComponentChunk.GetTypes(chunk);
            if (types.Contains(type))
            {
                throw new InvalidOperationException($"Component {type} already present on {entity}.");
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

        public static UnmanagedDictionary<int, nint> GetComponentArchetypes(UnsafeWorld* world)
        {
            Allocations.ThrowIfNull(world);
            return new(world->components);
        }

        public static UnmanagedList<CollectionTypeMask> GetCollectionArchetypes(UnsafeWorld* world)
        {
            Allocations.ThrowIfNull(world);
            return new(world->collectionArchetypes);
        }

        public static UnsafeWorld* Allocate()
        {
            if (!Universe.destroyedWorlds.TryTake(out uint id))
            {
                Universe.createdWorlds++;
                id = Universe.createdWorlds;
                if (Universe.collections.Length < Universe.createdWorlds)
                {
                    Array.Resize(ref Universe.collections, (int)Universe.createdWorlds * 2);
                }

                Universe.collections[id - 1] = [];
            }

            UnsafeList* entities = UnsafeList.Allocate<EntityDescription>();
            UnsafeList* freeEntities = UnsafeList.Allocate<EntityID>();
            UnsafeList* collectionArchetypes = UnsafeList.Allocate<CollectionTypeMask>();
            UnsafeList* events = UnsafeList.Allocate<Container>();
            UnsafeDictionary* listeners = UnsafeDictionary.Allocate<RuntimeType, nint>();
            UnsafeDictionary* listenersWithContext = UnsafeDictionary.Allocate<RuntimeType, nint>();
            UnsafeDictionary* components = UnsafeDictionary.Allocate<int, nint>();

            UnsafeComponentChunk* defaultComponentChunk = UnsafeComponentChunk.Allocate([]);
            int chunkKey = UnsafeComponentChunk.GetKey(defaultComponentChunk);
            UnsafeDictionary.Add(components, chunkKey, (nint)defaultComponentChunk);

            UnsafeList.AddDefault(collectionArchetypes);
            UnsafeWorld* world = Allocations.Allocate<UnsafeWorld>();
            *world = new(id, entities, freeEntities, collectionArchetypes, events, listeners, listenersWithContext, components);
            return world;
        }

        public static void Free(ref UnsafeWorld* world)
        {
            Allocations.ThrowIfNull(world);
            uint id = GetID(world);
            uint eventCount = UnsafeList.GetCount(world->events);
            for (uint i = 0; i < eventCount; i++)
            {
                Container message = UnsafeList.Get<Container>(world->events, i);
                message.Dispose();
            }

            uint listenerTypesCount = UnsafeDictionary.GetCount(world->listeners);
            for (uint i = 0; i < listenerTypesCount; i++)
            {
                RuntimeType eventType = UnsafeDictionary.GetKeyRef<RuntimeType, nint>(world->listeners, i);
                UnsafeList* listenerList = (UnsafeList*)UnsafeDictionary.GetValueRef<RuntimeType, nint>(world->listeners, eventType);
                while (UnsafeList.GetCount(listenerList) > 0)
                {
                    UnsafeList.RemoveAtBySwapping(listenerList, 0, out Listener removedListener);
                    UnsafeListener* unsafeValue = removedListener.value;
                    UnsafeListener.Free(ref unsafeValue);
                }

                UnsafeList.Free(ref listenerList);
            }

            listenerTypesCount = UnsafeDictionary.GetCount(world->listenersWithContext);
            for (uint i = 0; i < listenerTypesCount; i++)
            {
                RuntimeType eventType = UnsafeDictionary.GetKeyRef<RuntimeType, nint>(world->listenersWithContext, i);
                UnsafeList* listenerList = (UnsafeList*)UnsafeDictionary.GetValueRef<RuntimeType, nint>(world->listenersWithContext, eventType);
                while (UnsafeList.GetCount(listenerList) > 0)
                {
                    UnsafeList.RemoveAtBySwapping(listenerList, 0, out ListenerWithContext removedListener);
                    UnsafeListener* unsafeValue = removedListener.value;
                    UnsafeListener.Free(ref unsafeValue);
                }

                UnsafeList.Free(ref listenerList);
            }

            uint componentChunkCount = UnsafeDictionary.GetCount(world->components);
            for (uint i = 0; i < componentChunkCount; i++)
            {
                int key = UnsafeDictionary.GetKeyRef<int, nint>(world->components, i);
                UnsafeComponentChunk* chunk = (UnsafeComponentChunk*)UnsafeDictionary.GetValueRef<int, nint>(world->components, key);
                UnsafeComponentChunk.Free(ref chunk);
            }

            ref CollectionOfCollections?[] collections = ref world->Collections;
            for (int i = collections.Length - 1; i >= 0; i--)
            {
                if (collections[i] is CollectionOfCollections arrayCollection)
                {
                    for (int t = 0; t < CollectionType.MaxTypes; t++)
                    {
                        ref UnsafeList* list = ref arrayCollection.lists[t];
                        if (!UnsafeList.IsDisposed(list))
                        {
                            UnsafeList.Free(ref list);
                        }
                    }
                }
            }

            UnsafeList.Free(ref world->slots);
            UnsafeList.Free(ref world->freeEntities);
            UnsafeList.Free(ref world->collectionArchetypes);
            UnsafeList.Free(ref world->events);
            UnsafeDictionary.Free(ref world->listeners);
            UnsafeDictionary.Free(ref world->listenersWithContext);
            UnsafeDictionary.Free(ref world->components);
            Allocations.Free(ref world);
            Universe.destroyedWorlds.Add(id);
        }

        public readonly override int GetHashCode()
        {
            return HashCode.Combine(id, slots->GetHashCode(), components->GetHashCode(), collectionArchetypes->GetHashCode());
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
                    while (j < UnsafeList.GetCount(listenerList))
                    {
                        Listener listener = UnsafeList.Get<Listener>(listenerList, j);
                        listener.Invoke(worldValue, message);
                        j++;
                    }
                }

                if (UnsafeDictionary.ContainsKey<RuntimeType, nint>(world->listenersWithContext, eventType))
                {
                    UnsafeList* listenerList = (UnsafeList*)UnsafeDictionary.GetValueRef<RuntimeType, nint>(world->listenersWithContext, eventType);
                    uint eventListenerCount = UnsafeList.GetCount(listenerList);
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
                UnsafeList.Remove(listenerList, listener);
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
                UnsafeList.Remove(listenerList, listener);
            }
            else
            {
                throw new NullReferenceException($"Listener for {listener.eventType} not found.");
            }
        }

        public static void DestroyEntity(UnsafeWorld* world, EntityID id)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityMissing(world, id);

            uint index = id.value - 1;
            ref EntityDescription entity = ref UnsafeList.GetRef<EntityDescription>(world->slots, index);

            UnsafeComponentChunk* chunk = (UnsafeComponentChunk*)UnsafeDictionary.GetValueRef<int, nint>(world->components, entity.componentsKey);
            UnsafeComponentChunk.Remove(chunk, id);

            ref CollectionOfCollections?[] arrays = ref world->Collections;
            if (arrays.Length > index)
            {
                if (arrays[index] is CollectionOfCollections arrayCollection)
                {
                    for (int i = 0; i < CollectionType.MaxTypes; i++)
                    {
                        CollectionType type = new(i);
                        if (entity.collectionTypes.Contains(type))
                        {
                            ref UnsafeList* array = ref arrayCollection.lists[i];
                            UnsafeList.Free(ref array);
                        }
                    }
                }
            }

            entity.id = default;
            entity.componentsKey = default;
            entity.collectionTypes = default;
            entity.version++;
            UnsafeList.Add(world->freeEntities, id);
            EntityDestroyed(world, id);
        }

        public static ReadOnlySpan<RuntimeType> GetComponents(UnsafeWorld* world, EntityID id)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityMissing(world, id);

            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, id.value - 1);
            UnsafeComponentChunk* chunk = (UnsafeComponentChunk*)UnsafeDictionary.GetValueRef<int, nint>(world->components, slot.componentsKey);
            return UnsafeComponentChunk.GetTypes(chunk);
        }

        /// <summary>
        /// Creates a new entity with blank components of the given types.
        /// </summary>
        public static EntityID CreateEntity(UnsafeWorld* world, ReadOnlySpan<RuntimeType> componentTypes)
        {
            Allocations.ThrowIfNull(world);
            int componentsKey = UnsafeComponentChunk.CalculateKey(componentTypes);
            nint chunk;
            if (!UnsafeDictionary.ContainsKey<int, nint>(world->components, componentsKey))
            {
                chunk = (nint)UnsafeComponentChunk.Allocate(componentTypes);
                UnsafeDictionary.Add(world->components, componentsKey, chunk);
            }
            else
            {
                chunk = UnsafeDictionary.GetValueRef<int, nint>(world->components, componentsKey);
            }

            if (UnsafeList.GetCount(world->freeEntities) > 0)
            {
                //reuses a previously destroyed entity
                EntityID oldEntity = UnsafeList.Get<EntityID>(world->freeEntities, 0);
                UnsafeList.RemoveAtBySwapping(world->freeEntities, 0);

                uint oldIndex = oldEntity.value - 1;
                ref EntityDescription entity = ref UnsafeList.GetRef<EntityDescription>(world->slots, oldIndex);
                entity.id = oldEntity;
                entity.version++;
                entity.componentsKey = componentsKey;
                entity.collectionTypes = default;
                UnsafeComponentChunk.Add((UnsafeComponentChunk*)chunk, oldEntity);
                EntityCreated(world, oldEntity);
                return oldEntity;
            }
            else
            {
                uint index = UnsafeList.GetCount(world->slots) + 1;
                EntityID newEntity = new(index);
                EntityDescription newSlot = new(newEntity, 0, componentsKey, default);
                UnsafeList.Add(world->slots, newSlot);

                ref CollectionOfCollections?[] arrays = ref world->Collections;
                if (arrays.Length <= index)
                {
                    Array.Resize(ref arrays, (int)index * 2);
                }

                UnsafeComponentChunk.Add((UnsafeComponentChunk*)chunk, newEntity);
                EntityCreated(world, newEntity);
                return newEntity;
            }
        }

        public static bool ContainsEntity(UnsafeWorld* world, EntityID id)
        {
            Allocations.ThrowIfNull(world);

            uint index = id.value - 1;
            if (index >= UnsafeList.GetCount(world->slots))
            {
                return false;
            }

            ref EntityDescription entity = ref UnsafeList.GetRef<EntityDescription>(world->slots, index);
            return entity.id == id;
        }

        public static UnmanagedList<T> CreateCollection<T>(UnsafeWorld* world, EntityID id) where T : unmanaged
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityMissing(world, id);

            uint index = id.value - 1;
            ref EntityDescription entity = ref UnsafeList.GetRef<EntityDescription>(world->slots, index);
            CollectionType type = CollectionType.Get<T>();
            if (entity.collectionTypes.Contains(type))
            {
                throw new InvalidOperationException($"Collection of type {type} already present on {id}.");
            }

            entity.collectionTypes.Add(type);
            ref CollectionOfCollections? collections = ref world->Collections[index];
            if (collections is null)
            {
                collections = new CollectionOfCollections();
            }

            ref UnsafeList* list = ref collections.lists[type.value - 1];
            list = UnsafeList.Allocate(type.RuntimeType);
            return new(list);
        }

        public static bool ContainsCollection<T>(UnsafeWorld* world, EntityID id) where T : unmanaged
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityMissing(world, id);

            uint index = id.value - 1;
            ref EntityDescription entity = ref UnsafeList.GetRef<EntityDescription>(world->slots, index);
            return entity.collectionTypes.Contains<T>();
        }

        public static UnmanagedList<C> GetCollection<C>(UnsafeWorld* world, EntityID id) where C : unmanaged
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityMissing(world, id);

            uint index = id.value - 1;
            ref EntityDescription entity = ref UnsafeList.GetRef<EntityDescription>(world->slots, index);
            CollectionType type = CollectionType.Get<C>();
            if (entity.collectionTypes.Contains(type))
            {
                ref CollectionOfCollections array = ref world->Collections[index]!;
                ref UnsafeList* list = ref array.lists[type.value - 1];
                return new(list);
            }
            else
            {
                throw new NullReferenceException($"Collection of type {type} not found on {id}.");
            }
        }

        public static void DestroyCollection<C>(UnsafeWorld* world, EntityID id) where C : unmanaged
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityMissing(world, id);

            uint entityIndex = id.value - 1;
            ref EntityDescription entity = ref UnsafeList.GetRef<EntityDescription>(world->slots, entityIndex);
            CollectionType type = CollectionType.Get<C>();
            if (entity.collectionTypes.Contains(type))
            {
                ref CollectionOfCollections? array = ref world->Collections[entityIndex]!;
                ref UnsafeList* list = ref array.lists[type.value - 1];
                UnsafeList.Free(ref list);
                entity.collectionTypes.Remove(type);
                list = default;
            }
            else
            {
                throw new NullReferenceException($"Array {type} not found.");
            }
        }

        public static ref T AddComponentRef<T>(UnsafeWorld* world, EntityID entity) where T : unmanaged
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityMissing(world, entity);

            RuntimeType addingType = RuntimeType.Get<T>();
            ThrowIfComponentAlreadyPresent(world, entity, addingType);

            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, entity.value - 1);
            int previousTypesKey = slot.componentsKey;
            UnsafeComponentChunk* current = (UnsafeComponentChunk*)UnsafeDictionary.GetValueRef<int, nint>(world->components, previousTypesKey);
            ReadOnlySpan<RuntimeType> oldTypes = UnsafeComponentChunk.GetTypes(current);
            Span<RuntimeType> newTypes = stackalloc RuntimeType[oldTypes.Length + 1];
            oldTypes.CopyTo(newTypes);
            newTypes[^1] = addingType;
            int newTypesKey = UnsafeComponentChunk.CalculateKey(newTypes);
            slot.componentsKey = newTypesKey;

            nint destination;
            if (UnsafeDictionary.ContainsKey<int, nint>(world->components, newTypesKey))
            {
                destination = UnsafeDictionary.GetValueRef<int, nint>(world->components, newTypesKey);
            }
            else
            {
                destination = (nint)UnsafeComponentChunk.Allocate(newTypes);
                UnsafeDictionary.Add(world->components, newTypesKey, destination);
            }

            uint index = UnsafeComponentChunk.Move(current, entity, (UnsafeComponentChunk*)destination);
            return ref UnsafeComponentChunk.GetComponentRef<T>((UnsafeComponentChunk*)destination, index);
        }

        public static void RemoveComponent<T>(UnsafeWorld* world, EntityID entity) where T : unmanaged
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityMissing(world, entity);

            RuntimeType removingType = RuntimeType.Get<T>();
            ThrowIfComponentMissing(world, entity, removingType);

            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, entity.value - 1);
            int previousTypesKey = slot.componentsKey;
            UnsafeComponentChunk* current = (UnsafeComponentChunk*)UnsafeDictionary.GetValueRef<int, nint>(world->components, previousTypesKey);
            ReadOnlySpan<RuntimeType> oldTypes = UnsafeComponentChunk.GetTypes(current);
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

            int newTypesKey = UnsafeComponentChunk.CalculateKey(newTypes);
            slot.componentsKey = newTypesKey;

            nint destination;
            if (UnsafeDictionary.ContainsKey<int, nint>(world->components, newTypesKey))
            {
                destination = UnsafeDictionary.GetValueRef<int, nint>(world->components, newTypesKey);
            }
            else
            {
                destination = (nint)UnsafeComponentChunk.Allocate(newTypes);
                UnsafeDictionary.Add(world->components, newTypesKey, destination);
            }

            UnsafeComponentChunk.Move(current, entity, (UnsafeComponentChunk*)destination);
        }

        public static bool ContainsComponent(UnsafeWorld* world, EntityID entity, RuntimeType type)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityMissing(world, entity);

            uint index = entity.value - 1;
            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, index);
            UnsafeComponentChunk* chunk = (UnsafeComponentChunk*)UnsafeDictionary.GetValueRef<int, nint>(world->components, slot.componentsKey);
            ReadOnlySpan<RuntimeType> types = UnsafeComponentChunk.GetTypes(chunk);
            return types.Contains(type);
        }

        public static ref T GetComponentRef<T>(UnsafeWorld* world, EntityID entity) where T : unmanaged
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityMissing(world, entity);

            RuntimeType type = RuntimeType.Get<T>();
            ThrowIfComponentMissing(world, entity, type);

            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, entity.value - 1);
            UnsafeComponentChunk* chunk = (UnsafeComponentChunk*)UnsafeDictionary.GetValueRef<int, nint>(world->components, slot.componentsKey);
            return ref UnsafeComponentChunk.GetComponentRef<T>(chunk, entity);
        }

        public static Span<byte> GetComponentBytes(UnsafeWorld* world, EntityID entity, RuntimeType type)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityMissing(world, entity);
            ThrowIfComponentMissing(world, entity, type);

            ref EntityDescription slot = ref UnsafeList.GetRef<EntityDescription>(world->slots, entity.value - 1);
            UnsafeComponentChunk* chunk = (UnsafeComponentChunk*)UnsafeDictionary.GetValueRef<int, nint>(world->components, slot.componentsKey);
            return UnsafeComponentChunk.GetComponentBytes(chunk, entity, type);
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

        public static void QueryComponents(UnsafeWorld* world, ReadOnlySpan<RuntimeType> types, QueryCallback action)
        {
            Allocations.ThrowIfNull(world);
            uint count = UnsafeDictionary.GetCount(world->components);
            for (uint i = 0; i < count; i++)
            {
                int key = UnsafeDictionary.GetKeyRef<int, nint>(world->components, i);
                UnsafeComponentChunk* chunk = (UnsafeComponentChunk*)UnsafeDictionary.GetValueRef<int, nint>(world->components, key);
                ReadOnlySpan<RuntimeType> chunkTypes = UnsafeComponentChunk.GetTypes(chunk);
                if (Contains(chunkTypes, types))
                {
                    UnmanagedList<EntityID> entities = UnsafeComponentChunk.GetEntities(chunk);
                    for (uint e = 0; e < entities.Count; e++)
                    {
                        action(entities[e]);
                    }
                }
            }
        }

        public static void QueryComponents<T1>(UnsafeWorld* world, QueryCallback<T1> action) where T1 : unmanaged
        {
            Allocations.ThrowIfNull(world);
            uint count = UnsafeDictionary.GetCount(world->components);
            ReadOnlySpan<RuntimeType> types = [RuntimeType.Get<T1>()];
            for (uint i = 0; i < count; i++)
            {
                int key = UnsafeDictionary.GetKeyRef<int, nint>(world->components, i);
                UnsafeComponentChunk* chunk = (UnsafeComponentChunk*)UnsafeDictionary.GetValueRef<int, nint>(world->components, key);
                ReadOnlySpan<RuntimeType> chunkTypes = UnsafeComponentChunk.GetTypes(chunk);
                if (Contains(chunkTypes, types))
                {
                    UnmanagedList<EntityID> entities = UnsafeComponentChunk.GetEntities(chunk);
                    for (uint e = 0; e < entities.Count; e++)
                    {
                        ref T1 t1 = ref UnsafeComponentChunk.GetComponentRef<T1>(chunk, e);
                        action(entities[e], ref t1);
                    }
                }
            }
        }

        public static void QueryComponents<T1, T2>(UnsafeWorld* world, QueryCallback<T1, T2> action) where T1 : unmanaged where T2 : unmanaged
        {
            Allocations.ThrowIfNull(world);
            uint count = UnsafeDictionary.GetCount(world->components);
            ReadOnlySpan<RuntimeType> types = [RuntimeType.Get<T1>(), RuntimeType.Get<T2>()];
            for (uint i = 0; i < count; i++)
            {
                int key = UnsafeDictionary.GetKeyRef<int, nint>(world->components, i);
                UnsafeComponentChunk* chunk = (UnsafeComponentChunk*)UnsafeDictionary.GetValueRef<int, nint>(world->components, key);
                ReadOnlySpan<RuntimeType> chunkTypes = UnsafeComponentChunk.GetTypes(chunk);
                if (Contains(chunkTypes, types))
                {
                    UnmanagedList<EntityID> entities = UnsafeComponentChunk.GetEntities(chunk);
                    for (uint e = 0; e < entities.Count; e++)
                    {
                        ref T1 t1 = ref UnsafeComponentChunk.GetComponentRef<T1>(chunk, e);
                        ref T2 t2 = ref UnsafeComponentChunk.GetComponentRef<T2>(chunk, e);
                        action(entities[e], ref t1, ref t2);
                    }
                }
            }
        }

        public static void QueryComponents<T1, T2, T3>(UnsafeWorld* world, QueryCallback<T1, T2, T3> action) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            Allocations.ThrowIfNull(world);
            uint count = UnsafeDictionary.GetCount(world->components);
            ReadOnlySpan<RuntimeType> types = [RuntimeType.Get<T1>(), RuntimeType.Get<T2>(), RuntimeType.Get<T3>()];
            for (uint i = 0; i < count; i++)
            {
                int key = UnsafeDictionary.GetKeyRef<int, nint>(world->components, i);
                UnsafeComponentChunk* chunk = (UnsafeComponentChunk*)UnsafeDictionary.GetValueRef<int, nint>(world->components, key);
                ReadOnlySpan<RuntimeType> chunkTypes = UnsafeComponentChunk.GetTypes(chunk);
                if (Contains(chunkTypes, types))
                {
                    UnmanagedList<EntityID> entities = UnsafeComponentChunk.GetEntities(chunk);
                    for (uint e = 0; e < entities.Count; e++)
                    {
                        ref T1 t1 = ref UnsafeComponentChunk.GetComponentRef<T1>(chunk, e);
                        ref T2 t2 = ref UnsafeComponentChunk.GetComponentRef<T2>(chunk, e);
                        ref T3 t3 = ref UnsafeComponentChunk.GetComponentRef<T3>(chunk, e);
                        action(entities[e], ref t1, ref t2, ref t3);
                    }
                }
            }
        }

        public static void QueryComponents<T1, T2, T3, T4>(UnsafeWorld* world, QueryCallback<T1, T2, T3, T4> action) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            Allocations.ThrowIfNull(world);
            uint count = UnsafeDictionary.GetCount(world->components);
            ReadOnlySpan<RuntimeType> types = [RuntimeType.Get<T1>(), RuntimeType.Get<T2>(), RuntimeType.Get<T3>(), RuntimeType.Get<T4>()];
            for (uint i = 0; i < count; i++)
            {
                int key = UnsafeDictionary.GetKeyRef<int, nint>(world->components, i);
                UnsafeComponentChunk* chunk = (UnsafeComponentChunk*)UnsafeDictionary.GetValueRef<int, nint>(world->components, key);
                ReadOnlySpan<RuntimeType> chunkTypes = UnsafeComponentChunk.GetTypes(chunk);
                if (Contains(chunkTypes, types))
                {
                    UnmanagedList<EntityID> entities = UnsafeComponentChunk.GetEntities(chunk);
                    for (uint e = 0; e < entities.Count; e++)
                    {
                        ref T1 t1 = ref UnsafeComponentChunk.GetComponentRef<T1>(chunk, e);
                        ref T2 t2 = ref UnsafeComponentChunk.GetComponentRef<T2>(chunk, e);
                        ref T3 t3 = ref UnsafeComponentChunk.GetComponentRef<T3>(chunk, e);
                        ref T4 t4 = ref UnsafeComponentChunk.GetComponentRef<T4>(chunk, e);
                        action(entities[e], ref t1, ref t2, ref t3, ref t4);
                    }
                }
            }
        }

        public struct EntityDescription(EntityID id, uint version, int componentsKey, CollectionTypeMask collectionTypes)
        {
            public EntityID id = id;
            public uint version = version;
            public int componentsKey = componentsKey;
            public CollectionTypeMask collectionTypes = collectionTypes;
        }
    }
}
