using Game.ECS;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unmanaged;
using Unmanaged.Collections;

namespace Game
{
    public unsafe struct UnmanagedWorld
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
        private uint count;
        private readonly UnsafeList* entities;
        private readonly UnsafeList* freeEntities;
        private readonly UnsafeList* componentArchetypes;
        private readonly UnsafeList* collectionArchetypes;

        private readonly ref Dictionary<ComponentTypeMask, CollectionOfComponents> Components => ref Universe.components[(int)id - 1];
        private readonly ref CollectionOfCollections?[] Collections => ref Universe.collections[(int)id - 1];
        private readonly ref ConcurrentQueue<Container> EventQueue => ref Universe.eventQueues[(int)id - 1];
        private readonly ref Dictionary<RuntimeType, List<Listener>> EventHandlers => ref Universe.listenerHandlers[(int)id - 1];
        private readonly ref Dictionary<RuntimeType, List<object?>> EventHandlerCauses => ref Universe.listenerCauses[(int)id - 1];

        private UnmanagedWorld(uint id, UnsafeList* entities, UnsafeList* freeEntities, UnsafeList* componentArchetypes, UnsafeList* collectionArchetypes)
        {
            this.id = id;
            this.count = 0;
            this.entities = entities;
            this.freeEntities = freeEntities;
            this.componentArchetypes = componentArchetypes;
            this.collectionArchetypes = collectionArchetypes;
        }

        [Conditional("DEBUG")]
        private static void ThrowIfEntityMissing(UnmanagedWorld* world, EntityID id)
        {
            uint index = id.value - 1;
            if (index >= world->entities->Count)
            {
                throw new NullReferenceException($"Entity {id} not found.");
            }

            ref EntityDescription entity = ref UnsafeList.GetRef<EntityDescription>(world->entities, index);
            if (entity.id != id)
            {
                throw new NullReferenceException($"Entity {id} not found.");
            }
        }

        public static uint GetID(UnmanagedWorld* world)
        {
            Allocations.ThrowIfNull((nint)world);

            return world->id;
        }

        public static bool IsDisposed(UnmanagedWorld* world)
        {
            return Allocations.IsNull((nint)world);
        }

        public static uint GetCount(UnmanagedWorld* world)
        {
            Allocations.ThrowIfNull((nint)world);

            return world->count;
        }

        public static UnmanagedWorld* Create()
        {
            if (!Universe.destroyedWorlds.TryTake(out uint id))
            {
                Universe.createdWorlds++;
                id = Universe.createdWorlds;
                if (Universe.components.Length < Universe.createdWorlds)
                {
                    Array.Resize(ref Universe.components, (int)Universe.createdWorlds * 2);
                    Array.Resize(ref Universe.collections, (int)Universe.createdWorlds * 2);
                    Array.Resize(ref Universe.eventQueues, (int)Universe.createdWorlds * 2);
                    Array.Resize(ref Universe.listenerHandlers, (int)Universe.createdWorlds * 2);
                    Array.Resize(ref Universe.listenerCauses, (int)Universe.createdWorlds * 2);
                }

                Universe.components[id - 1] = [];
                Universe.collections[id - 1] = [];
                Universe.eventQueues[id - 1] = [];
                Universe.listenerHandlers[id - 1] = [];
                Universe.listenerCauses[id - 1] = [];
            }

            UnsafeList* entities = UnsafeList.Allocate<EntityDescription>();
            UnsafeList* freeEntities = UnsafeList.Allocate<EntityID>();
            UnsafeList* componentArchetypes = UnsafeList.Allocate<ComponentTypeMask>();
            UnsafeList* collectionArchetypes = UnsafeList.Allocate<CollectionTypeMask>();

            ref Dictionary<ComponentTypeMask, CollectionOfComponents> components = ref Universe.components[id - 1];
            components.Add(default, new CollectionOfComponents(default));
            UnsafeList.AddDefault(componentArchetypes);
            UnsafeList.AddDefault(collectionArchetypes);

            nint pointer = Marshal.AllocHGlobal(sizeof(UnmanagedWorld));
            UnmanagedWorld* world = (UnmanagedWorld*)pointer;
            *world = new UnmanagedWorld(id, entities, freeEntities, componentArchetypes, collectionArchetypes);
            Allocations.Register((nint)world);
            return world;
        }

        public static void Dispose(UnmanagedWorld* world)
        {
            Allocations.ThrowIfNull((nint)world);

            Allocations.Unregister((nint)world);

            uint id = world->id;
            ref Dictionary<ComponentTypeMask, CollectionOfComponents> components = ref world->Components;
            uint count = world->componentArchetypes->Count;
            while (count > 0)
            {
                ComponentTypeMask types = UnsafeList.Get<ComponentTypeMask>(world->componentArchetypes, count - 1);
                CollectionOfComponents data = components[types];
                data.Dispose();
                count--;
            }

            ref CollectionOfCollections?[] collections = ref world->Collections;
            for (int i = collections.Length - 1; i >= 0; i--)
            {
                if (collections[i] is CollectionOfCollections arrayCollection)
                {
                    for (int t = 0; t < CollectionType.MaxTypes; t++)
                    {
                        ref UnsafeList* list = ref arrayCollection.lists[t];
                        if (list != default)
                        {
                            UnsafeList.Dispose(list);
                        }
                    }
                }
            }

            ref ConcurrentQueue<Container> eventQueue = ref Universe.eventQueues[id - 1];
            while (eventQueue.TryDequeue(out Container container))
            {
                container.Dispose();
            }

            UnsafeList.Dispose(world->entities);
            UnsafeList.Dispose(world->freeEntities);
            UnsafeList.Dispose(world->componentArchetypes);
            UnsafeList.Dispose(world->collectionArchetypes);
            components.Clear();
            Universe.destroyedWorlds.Add(id);
            Universe.listenerCauses[id - 1].Clear();
            Universe.listenerHandlers[id - 1].Clear();
            Universe.eventQueues[id - 1].Clear();
        }

        public readonly override int GetHashCode()
        {
            return HashCode.Combine(id, count, entities->GetHashCode(), componentArchetypes->GetHashCode(), collectionArchetypes->GetHashCode());
        }

        public static void SubmitEvent(UnmanagedWorld* world, Container container)
        {
            Allocations.ThrowIfNull((nint)world);

            world->EventQueue.Enqueue(container);
        }

        public static void AddListener(UnmanagedWorld* world, RuntimeType type, Listener listener)
        {
            Allocations.ThrowIfNull((nint)world);

            if (!world->EventHandlers.TryGetValue(type, out System.Collections.Generic.List<Listener>? listeners))
            {
                listeners = [];
                world->EventHandlers.Add(type, listeners);
            }

            if (!world->EventHandlerCauses.TryGetValue(type, out System.Collections.Generic.List<object?>? causes))
            {
                causes = [];
                world->EventHandlerCauses.Add(type, causes);
            }

            listeners.Add(listener);
            causes.Add(null);
        }

        public static void RemoveListener(UnmanagedWorld* world, RuntimeType type, Listener listener)
        {
            Allocations.ThrowIfNull((nint)world);

            if (world->EventHandlers.TryGetValue(type, out var listeners))
            {
                int index = listeners.IndexOf(listener);
                if (index == -1)
                {
                    throw new NullReferenceException($"Listener {type} not found.");
                }

                listeners.RemoveAt(index);
                world->EventHandlerCauses[type].RemoveAt(index);
            }
            else
            {
                throw new NullReferenceException($"Listener {type} not found.");
            }
        }

        public static void AddListener<T>(UnmanagedWorld* world, Listener<T> listener) where T : unmanaged
        {
            Allocations.ThrowIfNull((nint)world);

            RuntimeType type = RuntimeType.Get<T>();
            if (!world->EventHandlers.TryGetValue(type, out System.Collections.Generic.List<Listener>? listeners))
            {
                listeners = [];
                world->EventHandlers.Add(type, listeners);
            }

            if (!world->EventHandlerCauses.TryGetValue(type, out System.Collections.Generic.List<object?>? causes))
            {
                causes = [];
                world->EventHandlerCauses.Add(type, causes);
            }

            void Handle(ref Container container)
            {
                T message = container.AsRef<T>();
                listener(ref message);
            }

            listeners.Add(Handle);
            causes.Add(listener);
        }

        public static void RemoveListener<T>(UnmanagedWorld* world, Listener<T> listener) where T : unmanaged
        {
            Allocations.ThrowIfNull((nint)world);

            RuntimeType type = RuntimeType.Get<T>();
            if (world->EventHandlers.TryGetValue(type, out var listeners))
            {
                System.Collections.Generic.List<object?> causes = world->EventHandlerCauses[type];
                int listenerObjectHash = listener.GetHashCode();
                for (int i = 0; i < causes.Count; i++)
                {
                    object? cause = causes[i];
                    if (cause is not null && cause.GetHashCode() == listenerObjectHash)
                    {
                        int index = causes.IndexOf(cause);
                        listeners.RemoveAt(index);
                        causes.RemoveAt(index);
                        return;
                    }
                }

                throw new NullReferenceException($"Listener {type} not found.");
            }
            else
            {
                throw new NullReferenceException($"Listener {type} not found.");
            }
        }

        public static void PollListeners(UnmanagedWorld* world)
        {
            Allocations.ThrowIfNull((nint)world);

            ref ConcurrentQueue<Container> eventQueue = ref world->EventQueue;
            while (eventQueue.TryDequeue(out Container message))
            {
                RuntimeType type = message.type;
                if (world->EventHandlers.TryGetValue(type, out System.Collections.Generic.List<Listener>? listeners))
                {
                    for (int i = 0; i < listeners.Count; i++)
                    {
                        listeners[i].Invoke(ref message);
                    }
                }

                message.Dispose();
            }
        }

        private static EntityID GenerateEntity(UnmanagedWorld* world, EntityID parent, ComponentTypeMask componentTypes)
        {
            Allocations.ThrowIfNull((nint)world);

            if (world->freeEntities->Count > 0)
            {
                EntityID oldId = UnsafeList.Get<EntityID>(world->freeEntities, 0);
                UnsafeList.RemoveAt(world->freeEntities, 0);

                uint oldIndex = oldId.value - 1;
                ref EntityDescription entity = ref UnsafeList.GetRef<EntityDescription>(world->entities, oldIndex);
                entity.id = oldId;
                entity.componentTypes = componentTypes;
                EntityCreated(world, oldId);
                return oldId;
            }

            uint index = world->entities->Count + 1;
            EntityID newId = new(index);
            EntityDescription newEntity = new(newId, 0, parent, componentTypes, default);
            UnsafeList.Add(world->entities, newEntity);

            ref CollectionOfCollections?[] arrays = ref world->Collections;
            if (arrays.Length <= index)
            {
                Array.Resize(ref arrays, (int)index * 2);
            }

            Dictionary<ComponentTypeMask, CollectionOfComponents> components = world->Components;
            if (!components.TryGetValue(componentTypes, out CollectionOfComponents? newData))
            {
                newData = new CollectionOfComponents(componentTypes);
                components.Add(componentTypes, newData);
                UnsafeList.Add(world->componentArchetypes, componentTypes);
            }

            newData.entities.Add(newId);
            EntityCreated(world, newId);
            return newId;
        }

        public static void DestroyEntity(UnmanagedWorld* world, EntityID id)
        {
            Allocations.ThrowIfNull((nint)world);

            uint index = id.value - 1;
            ref EntityDescription entity = ref UnsafeList.GetRef<EntityDescription>(world->entities, index);
            ComponentTypeMask oldTypes = entity.componentTypes;
            CollectionOfComponents oldData = world->Components[oldTypes];
            uint oldIndex = oldData.entities.IndexOf(id);
            oldData.entities.Remove(id);
            for (int i = 0; i < ComponentTypeMask.MaxComponents; i++)
            {
                ComponentType type = new(i);
                if (oldTypes.Contains(type))
                {
                    ref UnsafeList* oldComponents = ref oldData.lists[i];
                    UnsafeList.RemoveAt(oldComponents, oldIndex);
                }
            }

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
                            UnsafeList.Clear(array);
                        }
                    }
                }
            }

            entity.id = default;
            entity.componentTypes = default;
            entity.collectionTypes = default;
            entity.version++;
            UnsafeList.Add(world->freeEntities, id);
            world->count--;
            EntityDestroyed(world, id);
        }

        public static ComponentTypeMask GetComponents(UnmanagedWorld* world, EntityID id)
        {
            Allocations.ThrowIfNull((nint)world);

            uint index = id.value - 1;
            ref EntityDescription entity = ref UnsafeList.GetRef<EntityDescription>(world->entities, index);
            return entity.componentTypes;
        }

        public static bool HasParent(UnmanagedWorld* world, EntityID id)
        {
            Allocations.ThrowIfNull((nint)world);
            ThrowIfEntityMissing(world, id);

            uint index = id.value - 1;
            ref EntityDescription entity = ref UnsafeList.GetRef<EntityDescription>(world->entities, index);
            return entity.parent != default;
        }

        public static EntityID GetParent(UnmanagedWorld* world, EntityID id)
        {
            Allocations.ThrowIfNull((nint)world);
            ThrowIfEntityMissing(world, id);

            uint index = id.value - 1;
            ref EntityDescription entity = ref UnsafeList.GetRef<EntityDescription>(world->entities, index);
            return entity.parent;
        }

        public static void SetParent(UnmanagedWorld* world, EntityID id, EntityID parent)
        {
            Allocations.ThrowIfNull((nint)world);
            ThrowIfEntityMissing(world, id);

            uint index = id.value - 1;
            ref EntityDescription entity = ref UnsafeList.GetRef<EntityDescription>(world->entities, index);
            entity.parent = parent;
        }

        /// <summary>
        /// Creates a new entity with blank components of the given types.
        /// </summary>
        public static EntityID CreateEntity(UnmanagedWorld* world, EntityID parent, ComponentTypeMask componentTypes)
        {
            Allocations.ThrowIfNull((nint)world);

            EntityID id = GenerateEntity(world, parent, componentTypes);
            CollectionOfComponents newData = world->Components[componentTypes];
            for (int i = 0; i < ComponentTypeMask.MaxComponents; i++)
            {
                ComponentType type = new(i);
                if (componentTypes.Contains(type))
                {
                    ref UnsafeList* componentList = ref newData.lists[i];
                    UnsafeList.AddDefault(componentList);
                }
            }

            world->count++;
            return id;
        }

        public static bool ContainsEntity(UnmanagedWorld* world, EntityID id)
        {
            Allocations.ThrowIfNull((nint)world);

            uint index = id.value - 1;
            ref EntityDescription entity = ref UnsafeList.GetRef<EntityDescription>(world->entities, index);
            return entity.id == id;
        }

        public static UnmanagedList<C> CreateCollection<C>(UnmanagedWorld* world, EntityID id) where C : unmanaged
        {
            Allocations.ThrowIfNull((nint)world);
            ThrowIfEntityMissing(world, id);

            uint index = id.value - 1;
            ref EntityDescription entity = ref UnsafeList.GetRef<EntityDescription>(world->entities, index);
            CollectionType type = CollectionType.Get<C>();
            if (entity.collectionTypes.Contains(type))
            {
                throw new InvalidOperationException($"Collection of type {type} already present.");
            }

            entity.collectionTypes.Add(type);
            ref CollectionOfCollections? collections = ref world->Collections[index];
            if (collections is null)
            {
                collections = new CollectionOfCollections();
            }

            ref UnsafeList* list = ref collections.lists[type.value - 1];
            list = UnsafeList.Allocate(type.RuntimeType);
            return UnsafeList.AsList<C>(list);
        }

        public static Span<C> CreateCollection<C>(UnmanagedWorld* world, EntityID id, uint initialCount) where C : unmanaged
        {
            Allocations.ThrowIfNull((nint)world);
            ThrowIfEntityMissing(world, id);

            uint index = id.value - 1;
            ref EntityDescription entity = ref UnsafeList.GetRef<EntityDescription>(world->entities, index);
            CollectionType type = CollectionType.Get<C>();
            if (entity.collectionTypes.Contains(type))
            {
                throw new InvalidOperationException($"Collection of type {type} already present.");
            }

            entity.collectionTypes.Add(type);
            ref CollectionOfCollections? collections = ref world->Collections[index];
            if (collections is null)
            {
                collections = new CollectionOfCollections();
            }

            ref UnsafeList* list = ref collections.lists[type.value - 1];
            list = UnsafeList.Allocate(type.RuntimeType, initialCount);
            for (int i = 0; i < initialCount; i++)
            {
                UnsafeList.AddDefault(list);
            }

            return UnsafeList.AsSpan<C>(list);
        }

        public static void AddToCollection<C>(UnmanagedWorld* world, EntityID id, C value) where C : unmanaged
        {
            Allocations.ThrowIfNull((nint)world);
            ThrowIfEntityMissing(world, id);

            uint index = id.value - 1;
            ref EntityDescription entity = ref UnsafeList.GetRef<EntityDescription>(world->entities, index);
            CollectionType type = CollectionType.Get<C>();
            if (entity.collectionTypes.Contains(type))
            {
                ref CollectionOfCollections array = ref world->Collections[index]!;
                ref UnsafeList* list = ref array.lists[type.value - 1];
                UnsafeList.Add(list, value);
            }
            else
            {
                throw new NullReferenceException($"Array {type} not found.");
            }
        }

        public static bool ContainsCollection<C>(UnmanagedWorld* world, EntityID id) where C : unmanaged
        {
            Allocations.ThrowIfNull((nint)world);
            ThrowIfEntityMissing(world, id);

            uint index = id.value - 1;
            ref EntityDescription entity = ref UnsafeList.GetRef<EntityDescription>(world->entities, index);
            return entity.collectionTypes.Contains<C>();
        }

        public static UnmanagedList<C> GetCollection<C>(UnmanagedWorld* world, EntityID id) where C : unmanaged
        {
            Allocations.ThrowIfNull((nint)world);
            ThrowIfEntityMissing(world, id);

            uint index = id.value - 1;
            ref EntityDescription entity = ref UnsafeList.GetRef<EntityDescription>(world->entities, index);
            CollectionType type = CollectionType.Get<C>();
            if (entity.collectionTypes.Contains(type))
            {
                ref CollectionOfCollections array = ref world->Collections[index]!;
                ref UnsafeList* list = ref array.lists[type.value - 1];
                return UnsafeList.AsList<C>(list);
            }
            else
            {
                throw new NullReferenceException($"Collection of type {type} not found on {id}.");
            }
        }

        public static void RemoveAtFromCollection<C>(UnmanagedWorld* world, EntityID id, uint index) where C : unmanaged
        {
            Allocations.ThrowIfNull((nint)world);
            ThrowIfEntityMissing(world, id);

            uint entityIndex = id.value - 1;
            ref EntityDescription entity = ref UnsafeList.GetRef<EntityDescription>(world->entities, entityIndex);
            CollectionType type = CollectionType.Get<C>();
            if (entity.collectionTypes.Contains(type))
            {
                ref CollectionOfCollections? array = ref world->Collections[entityIndex]!;
                ref UnsafeList* list = ref array.lists[type.value - 1];
                UnsafeList.RemoveAt(list, index);
            }
            else
            {
                throw new NullReferenceException($"Array {type} not found.");
            }
        }

        public static void DestroyCollection<C>(UnmanagedWorld* world, EntityID id) where C : unmanaged
        {
            Allocations.ThrowIfNull((nint)world);
            ThrowIfEntityMissing(world, id);

            uint entityIndex = id.value - 1;
            ref EntityDescription entity = ref UnsafeList.GetRef<EntityDescription>(world->entities, entityIndex);
            CollectionType type = CollectionType.Get<C>();
            if (entity.collectionTypes.Contains(type))
            {
                ref CollectionOfCollections? array = ref world->Collections[entityIndex]!;
                ref UnsafeList* list = ref array.lists[type.value - 1];
                UnsafeList.Dispose(list);
                entity.collectionTypes.Remove(type);
                list = default;
            }
            else
            {
                throw new NullReferenceException($"Array {type} not found.");
            }
        }

        public static void ClearCollection<C>(UnmanagedWorld* world, EntityID id) where C : unmanaged
        {
            Allocations.ThrowIfNull((nint)world);
            ThrowIfEntityMissing(world, id);

            uint entityIndex = id.value - 1;
            ref EntityDescription entity = ref UnsafeList.GetRef<EntityDescription>(world->entities, entityIndex);
            CollectionType type = CollectionType.Get<C>();
            if (entity.collectionTypes.Contains(type))
            {
                ref CollectionOfCollections? array = ref world->Collections[entityIndex]!;
                ref UnsafeList* list = ref array.lists[type.value - 1];
                UnsafeList.Clear(list);
            }
            else
            {
                throw new NullReferenceException($"Array {type} not found.");
            }
        }

        public static void AddComponent<C>(UnmanagedWorld* world, EntityID id, C component) where C : unmanaged
        {
            Allocations.ThrowIfNull((nint)world);

            uint index = id.value - 1;
            ref EntityDescription entity = ref UnsafeList.GetRef<EntityDescription>(world->entities, index);
            ComponentType addedType = ComponentType.Get<C>();
            ComponentTypeMask previousTypes = entity.componentTypes;
            if (previousTypes.Contains(addedType))
            {
                throw new InvalidOperationException($"Component {addedType.RuntimeType} already present.");
            }

            ComponentTypeMask newTypes = entity.componentTypes;
            newTypes.Add(addedType);
            entity.componentTypes = newTypes;

            Dictionary<ComponentTypeMask, CollectionOfComponents> components = world->Components;
            if (!components.TryGetValue(newTypes, out CollectionOfComponents? newData))
            {
                newData = new CollectionOfComponents(newTypes);
                components.Add(newTypes, newData);
                UnsafeList.Add(world->componentArchetypes, newTypes);
            }

            CollectionOfComponents oldData = components[previousTypes];
            uint newIndex = oldData.MoveTo(id, newData);
            ref UnsafeList* list = ref newData.lists[addedType.value - 1];
            UnsafeList.Set(list, newIndex, component);
        }

        public static void RemoveComponent<C>(UnmanagedWorld* world, EntityID id) where C : unmanaged
        {
            Allocations.ThrowIfNull((nint)world);

            uint index = id.value - 1;
            ref EntityDescription entity = ref UnsafeList.GetRef<EntityDescription>(world->entities, index);
            if (entity.id != id)
            {
                throw new NullReferenceException($"Entity {id} not found.");
            }

            ComponentType removedType = ComponentType.Get<C>();
            ComponentTypeMask previousTypes = entity.componentTypes;
            if (!previousTypes.Contains(removedType))
            {
                throw new NullReferenceException($"Component {removedType.RuntimeType} not found to remove.");
            }

            ComponentTypeMask newTypes = entity.componentTypes;
            newTypes.Remove(removedType);
            entity.componentTypes = newTypes;

            Dictionary<ComponentTypeMask, CollectionOfComponents> components = world->Components;
            CollectionOfComponents oldData = components[previousTypes];
            if (!components.TryGetValue(newTypes, out CollectionOfComponents? newData))
            {
                newData = new CollectionOfComponents(newTypes);
                components.Add(newTypes, newData);
                UnsafeList.Add(world->componentArchetypes, newTypes);
            }

            oldData.MoveTo(id, newData);
        }

        public static bool ContainsComponent(UnmanagedWorld* world, EntityID id, ComponentType type)
        {
            Allocations.ThrowIfNull((nint)world);

            uint index = id.value - 1;
            ref EntityDescription entity = ref UnsafeList.GetRef<EntityDescription>(world->entities, index);
            if (entity.id != id)
            {
                throw new NullReferenceException($"Entity {id} not found.");
            }

            return entity.componentTypes.Contains(type);
        }

        public static ref C GetComponentRef<C>(UnmanagedWorld* world, EntityID id) where C : unmanaged
        {
            Allocations.ThrowIfNull((nint)world);

            uint index = id.value - 1;
            ref EntityDescription entity = ref UnsafeList.GetRef<EntityDescription>(world->entities, index);
            if (entity.id != id)
            {
                throw new NullReferenceException($"Entity {id} not found.");
            }

            ComponentType type = ComponentType.Get<C>();
            if (entity.componentTypes.Contains(type))
            {
                CollectionOfComponents data = world->Components[entity.componentTypes];
                uint componentIndex = data.entities.IndexOf(id);
                ref UnsafeList* list = ref data.lists[type.value - 1];
                return ref UnsafeList.GetRef<C>(list, componentIndex);
            }
            else
            {
                throw new NullReferenceException($"Component {type.RuntimeType} not found.");
            }
        }

        public static Span<byte> GetComponentBytes(UnmanagedWorld* world, EntityID id, ComponentType type)
        {
            Allocations.ThrowIfNull((nint)world);

            uint index = id.value - 1;
            ref EntityDescription entity = ref UnsafeList.GetRef<EntityDescription>(world->entities, index);
            if (entity.id != id)
            {
                throw new NullReferenceException($"Entity {id} not found.");
            }

            if (entity.componentTypes.Contains(type))
            {
                CollectionOfComponents data = world->Components[entity.componentTypes];
                uint componentIndex = data.entities.IndexOf(id);
                ref UnsafeList* list = ref data.lists[type.value - 1];
                return UnsafeList.Get(list, componentIndex);
            }
            else
            {
                throw new NullReferenceException($"Component {type.RuntimeType} not found.");
            }
        }

        public static ReadOnlySpan<EntityID> GetEntities(UnmanagedWorld* world, ComponentTypeMask componentTypes)
        {
            uint mostComponents = 0;
            ComponentTypeMask mostArchetype = default;
            for (uint i = 0; i < world->componentArchetypes->Count; i++)
            {
                ComponentTypeMask archetype = UnsafeList.Get<ComponentTypeMask>(world->componentArchetypes, i);
                if (archetype.Contains(componentTypes))
                {
                    if (archetype.Count > mostComponents)
                    {
                        mostComponents = archetype.Count;
                        mostArchetype = archetype;
                    }
                }
            }

            if (mostArchetype != default)
            {
                CollectionOfComponents data = world->Components[mostArchetype];
                return data.entities.AsSpan();
            }

            return default;
        }

        public static void QueryComponents(UnmanagedWorld* world, ComponentType type, QueryCallback action)
        {
            Allocations.ThrowIfNull((nint)world);

            for (uint i = 0; i < world->componentArchetypes->Count; i++)
            {
                ComponentTypeMask archetype = UnsafeList.Get<ComponentTypeMask>(world->componentArchetypes, i);
                if (archetype.Contains(type))
                {
                    CollectionOfComponents data = world->Components[archetype];
                    for (uint e = 0; e < data.entities.Count; e++)
                    {
                        EntityID id = data.entities[e];
                        action(id);
                    }
                }
            }
        }

        public static void QueryComponents<C1>(UnmanagedWorld* world, QueryCallback action) where C1 : unmanaged
        {
            Allocations.ThrowIfNull((nint)world);

            ComponentType t1 = ComponentType.Get<C1>();
            for (uint i = 0; i < world->componentArchetypes->Count; i++)
            {
                ComponentTypeMask archetype = UnsafeList.Get<ComponentTypeMask>(world->componentArchetypes, i);
                if (archetype.Contains(t1))
                {
                    CollectionOfComponents data = world->Components[archetype];
                    for (uint e = 0; e < data.entities.Count; e++)
                    {
                        EntityID id = data.entities[e];
                        action(id);
                    }
                }
            }
        }

        public static void QueryComponents<C1>(UnmanagedWorld* world, QueryCallback<C1> action) where C1 : unmanaged
        {
            Allocations.ThrowIfNull((nint)world);

            ComponentType t1 = ComponentType.Get<C1>();
            for (uint i = 0; i < world->componentArchetypes->Count; i++)
            {
                ComponentTypeMask archetype = UnsafeList.Get<ComponentTypeMask>(world->componentArchetypes, i);
                if (archetype.Contains(t1))
                {
                    CollectionOfComponents data = world->Components[archetype];
                    ref UnsafeList* list = ref data.lists[t1.value - 1];
                    for (uint e = 0; e < data.entities.Count; e++)
                    {
                        EntityID id = data.entities[e];
                        ref C1 component = ref UnsafeList.GetRef<C1>(list, e);
                        action(id, ref component);
                    }
                }
            }
        }

        public static void QueryComponents<C1, C2>(UnmanagedWorld* world, QueryCallback<C1, C2> action) where C1 : unmanaged where C2 : unmanaged
        {
            Allocations.ThrowIfNull((nint)world);

            ComponentType t1 = ComponentType.Get<C1>();
            ComponentType t2 = ComponentType.Get<C2>();
            for (uint i = 0; i < world->componentArchetypes->Count; i++)
            {
                ComponentTypeMask archetype = UnsafeList.Get<ComponentTypeMask>(world->componentArchetypes, i);
                if (archetype.Contains(t1) && archetype.Contains(t2))
                {
                    CollectionOfComponents data = world->Components[archetype];
                    ref UnsafeList* l1 = ref data.lists[t1.value - 1];
                    ref UnsafeList* l2 = ref data.lists[t2.value - 1];
                    for (uint e = 0; e < data.entities.Count; e++)
                    {
                        EntityID id = data.entities[e];
                        ref C1 c1 = ref UnsafeList.GetRef<C1>(l1, e);
                        ref C2 c2 = ref UnsafeList.GetRef<C2>(l2, e);
                        action(id, ref c1, ref c2);
                    }
                }
            }
        }

        public static void QueryComponents<C1, C2, C3>(UnmanagedWorld* world, QueryCallback<C1, C2, C3> action) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged
        {
            Allocations.ThrowIfNull((nint)world);

            ComponentType t1 = ComponentType.Get<C1>();
            ComponentType t2 = ComponentType.Get<C2>();
            ComponentType t3 = ComponentType.Get<C3>();
            for (uint i = 0; i < world->componentArchetypes->Count; i++)
            {
                ComponentTypeMask archetype = UnsafeList.Get<ComponentTypeMask>(world->componentArchetypes, i);
                if (archetype.Contains(t1) && archetype.Contains(t2) && archetype.Contains(t3))
                {
                    CollectionOfComponents data = world->Components[archetype];
                    ref UnsafeList* l1 = ref data.lists[t1.value - 1];
                    ref UnsafeList* l2 = ref data.lists[t2.value - 1];
                    ref UnsafeList* l3 = ref data.lists[t3.value - 1];
                    for (uint e = 0; e < data.entities.Count; e++)
                    {
                        EntityID id = data.entities[e];
                        ref C1 c1 = ref UnsafeList.GetRef<C1>(l1, e);
                        ref C2 c2 = ref UnsafeList.GetRef<C2>(l2, e);
                        ref C3 c3 = ref UnsafeList.GetRef<C3>(l3, e);
                        action(id, ref c1, ref c2, ref c3);
                    }
                }
            }
        }

        public static void QueryComponents<C1, C2, C3, C4>(UnmanagedWorld* world, QueryCallback<C1, C2, C3, C4> action) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged
        {
            Allocations.ThrowIfNull((nint)world);

            ComponentType t1 = ComponentType.Get<C1>();
            ComponentType t2 = ComponentType.Get<C2>();
            ComponentType t3 = ComponentType.Get<C3>();
            ComponentType t4 = ComponentType.Get<C4>();
            for (uint i = 0; i < world->componentArchetypes->Count; i++)
            {
                ComponentTypeMask archetype = UnsafeList.Get<ComponentTypeMask>(world->componentArchetypes, i);
                if (archetype.Contains(t1) && archetype.Contains(t2) && archetype.Contains(t3) && archetype.Contains(t4))
                {
                    CollectionOfComponents data = world->Components[archetype];
                    ref UnsafeList* l1 = ref data.lists[t1.value - 1];
                    ref UnsafeList* l2 = ref data.lists[t2.value - 1];
                    ref UnsafeList* l3 = ref data.lists[t3.value - 1];
                    ref UnsafeList* l4 = ref data.lists[t4.value - 1];
                    for (uint e = 0; e < data.entities.Count; e++)
                    {
                        EntityID id = data.entities[e];
                        ref C1 c1 = ref UnsafeList.GetRef<C1>(l1, e);
                        ref C2 c2 = ref UnsafeList.GetRef<C2>(l2, e);
                        ref C3 c3 = ref UnsafeList.GetRef<C3>(l3, e);
                        ref C4 c4 = ref UnsafeList.GetRef<C4>(l4, e);
                        action(id, ref c1, ref c2, ref c3, ref c4);
                    }
                }
            }
        }

        public struct EntityDescription(EntityID id, uint version, EntityID parent, ComponentTypeMask componentTypes, CollectionTypeMask collectionTypes)
        {
            public EntityID id = id;
            public uint version = version;
            public EntityID parent = parent;
            public ComponentTypeMask componentTypes = componentTypes;
            public CollectionTypeMask collectionTypes = collectionTypes;
        }
    }
}
