﻿using Game.ECS;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
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
        private readonly UnsafeList* entities;
        private readonly UnsafeList* freeEntities;
        private readonly UnsafeList* componentArchetypes;
        private readonly UnsafeList* collectionArchetypes;
        private readonly UnsafeList* events;
        private readonly UnsafeDictionary* listeners;

        private readonly ref Dictionary<ComponentTypeMask, CollectionOfComponents> Components => ref Universe.components[(int)id - 1];
        private readonly ref CollectionOfCollections?[] Collections => ref Universe.collections[(int)id - 1];

        private UnsafeWorld(uint id, UnsafeList* entities, UnsafeList* freeEntities, UnsafeList* componentArchetypes, UnsafeList* collectionArchetypes, UnsafeList* events, UnsafeDictionary* listeners)
        {
            this.id = id;
            this.entities = entities;
            this.freeEntities = freeEntities;
            this.componentArchetypes = componentArchetypes;
            this.collectionArchetypes = collectionArchetypes;
            this.events = events;
            this.listeners = listeners;
        }

        [Conditional("DEBUG")]
        private static void ThrowIfEntityMissing(UnsafeWorld* world, EntityID id)
        {
            uint index = id.value - 1;
            uint count = GetEntityCount(world);
            if (index >= count)
            {
                throw new NullReferenceException($"Entity {id} not found.");
            }

            ref EntityDescription entity = ref UnsafeList.GetRef<EntityDescription>(world->entities, index);
            if (entity.id != id)
            {
                throw new NullReferenceException($"Entity {id} not found.");
            }
        }

        [Conditional("DEBUG")]
        private static void ThrowIfComponentMissing(UnsafeWorld* world, EntityID id, ComponentType type)
        {
            uint index = id.value - 1;
            ref EntityDescription entity = ref UnsafeList.GetRef<EntityDescription>(world->entities, index);
            if (!entity.componentTypes.Contains(type))
            {
                throw new NullReferenceException($"Component {type} not found on {id}.");
            }
        }

        [Conditional("DEBUG")]
        private static void ThrowIfComponentAlreadyPresent(UnsafeWorld* world, EntityID id, ComponentType type)
        {
            uint index = id.value - 1;
            ref EntityDescription entity = ref UnsafeList.GetRef<EntityDescription>(world->entities, index);
            if (entity.componentTypes.Contains(type))
            {
                throw new InvalidOperationException($"Component {type.RuntimeType} already present.");
            }
        }

        public static uint GetID(UnsafeWorld* world)
        {
            Allocations.ThrowIfNull((nint)world);

            return world->id;
        }

        public static bool IsDisposed(UnsafeWorld* world)
        {
            return Allocations.IsNull((nint)world);
        }

        public static uint GetEntityCount(UnsafeWorld* world)
        {
            Allocations.ThrowIfNull((nint)world);

            return UnsafeList.GetCount(world->entities);
        }

        public static UnsafeWorld* Allocate()
        {
            if (!Universe.destroyedWorlds.TryTake(out uint id))
            {
                Universe.createdWorlds++;
                id = Universe.createdWorlds;
                if (Universe.components.Length < Universe.createdWorlds)
                {
                    Array.Resize(ref Universe.components, (int)Universe.createdWorlds * 2);
                    Array.Resize(ref Universe.collections, (int)Universe.createdWorlds * 2);
                }

                Universe.components[id - 1] = [];
                Universe.collections[id - 1] = [];
            }

            UnsafeList* entities = UnsafeList.Allocate<EntityDescription>();
            UnsafeList* freeEntities = UnsafeList.Allocate<EntityID>();
            UnsafeList* componentArchetypes = UnsafeList.Allocate<ComponentTypeMask>();
            UnsafeList* collectionArchetypes = UnsafeList.Allocate<CollectionTypeMask>();
            UnsafeList* events = UnsafeList.Allocate<Container>();
            UnsafeDictionary* listeners = UnsafeDictionary.Allocate<RuntimeType, nint>();

            ref Dictionary<ComponentTypeMask, CollectionOfComponents> components = ref Universe.components[id - 1];
            components.Add(default, new CollectionOfComponents(default));
            UnsafeList.AddDefault(componentArchetypes);
            UnsafeList.AddDefault(collectionArchetypes);

            nint pointer = Marshal.AllocHGlobal(sizeof(UnsafeWorld));
            UnsafeWorld* world = (UnsafeWorld*)pointer;
            *world = new UnsafeWorld(id, entities, freeEntities, componentArchetypes, collectionArchetypes, events, listeners);
            Allocations.Register((nint)world);
            return world;
        }

        public static void Free(UnsafeWorld* world)
        {
            Allocations.ThrowIfNull((nint)world);
            Allocations.Unregister((nint)world);

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
                uint listenerCount = UnsafeList.GetCount(listenerList);
                for (uint j = 0; j < listenerCount; j++)
                {
                    Listener listener = UnsafeList.Get<Listener>(listenerList, j);
                }

                UnsafeList.Free(listenerList);
            }

            uint id = world->id;
            ref Dictionary<ComponentTypeMask, CollectionOfComponents> components = ref world->Components;
            uint count = UnsafeList.GetCount(world->componentArchetypes);
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
                            UnsafeList.Free(list);
                        }
                    }
                }
            }

            UnsafeList.Free(world->entities);
            UnsafeList.Free(world->freeEntities);
            UnsafeList.Free(world->componentArchetypes);
            UnsafeList.Free(world->collectionArchetypes);
            UnsafeList.Free(world->events);
            UnsafeDictionary.Free(world->listeners);
            components.Clear();
            Universe.destroyedWorlds.Add(id);
        }

        public readonly override int GetHashCode()
        {
            return HashCode.Combine(id, entities->GetHashCode(), componentArchetypes->GetHashCode(), collectionArchetypes->GetHashCode());
        }

        public static void Submit(UnsafeWorld* world, Container message)
        {
            UnsafeList.Add(world->events, message);
        }

        public static void Poll(UnsafeWorld* world)
        {
            World worldValue = new(world);
            using UnmanagedArray<Container> events = new(UnsafeList.AsSpan<Container>(world->events));
            UnsafeList.Clear(world->events);

            for (uint i = 0; i < events.Length; i++)
            {
                using Container message = events[i];
                RuntimeType eventType = message.type;
                if (UnsafeDictionary.ContainsKey<RuntimeType, nint>(world->listeners, eventType))
                {
                    UnsafeList* listenerList = (UnsafeList*)UnsafeDictionary.GetValueRef<RuntimeType, nint>(world->listeners, eventType);
                    uint listenerCount = UnsafeList.GetCount(listenerList);
                    for (uint j = 0; j < listenerCount; j++)
                    {
                        Listener listener = UnsafeList.Get<Listener>(listenerList, j);
                        listener.Invoke(worldValue, message);
                    }
                }
            }
        }

        public static void Listen(UnsafeWorld* world, RuntimeType eventType, delegate* unmanaged<World, Container, void> callback)
        {
            Listener listener = new(callback);
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
        }

        private static EntityID GenerateEntity(UnsafeWorld* world, EntityID parent, ComponentTypeMask componentTypes)
        {
            Allocations.ThrowIfNull((nint)world);

            if (UnsafeList.GetCount(world->freeEntities) > 0)
            {
                EntityID oldId = UnsafeList.Get<EntityID>(world->freeEntities, 0);
                UnsafeList.RemoveAt(world->freeEntities, 0);

                uint oldIndex = oldId.value - 1;
                ref EntityDescription entity = ref UnsafeList.GetRef<EntityDescription>(world->entities, oldIndex);
                entity.id = oldId;
                entity.version++;
                entity.parent = parent;
                entity.componentTypes = componentTypes;
                EntityCreated(world, oldId);
                return oldId;
            }

            uint index = UnsafeList.GetCount(world->entities) + 1;
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

        public static void DestroyEntity(UnsafeWorld* world, EntityID id)
        {
            Allocations.ThrowIfNull((nint)world);
            ThrowIfEntityMissing(world, id);

            uint index = id.value - 1;
            ref EntityDescription entity = ref UnsafeList.GetRef<EntityDescription>(world->entities, index);
            ComponentTypeMask oldTypes = entity.componentTypes;
            CollectionOfComponents oldData = world->Components[oldTypes];
            uint oldIndex = oldData.entities.IndexOf(id);
            oldData.entities.Remove(id);
            for (int i = 0; i < ComponentTypeMask.MaxValues; i++)
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
            EntityDestroyed(world, id);
        }

        public static ComponentTypeMask GetComponents(UnsafeWorld* world, EntityID id)
        {
            Allocations.ThrowIfNull((nint)world);
            ThrowIfEntityMissing(world, id);

            uint index = id.value - 1;
            ref EntityDescription entity = ref UnsafeList.GetRef<EntityDescription>(world->entities, index);
            return entity.componentTypes;
        }

        public static UnmanagedList<EntityID> GetChildren(UnsafeWorld* world, EntityID parentId)
        {
            Allocations.ThrowIfNull((nint)world);
            ThrowIfEntityMissing(world, parentId);

            UnsafeList* entities = world->entities;
            UnsafeList* children = UnsafeList.Allocate<EntityID>();
            for (uint i = 0; i < UnsafeList.GetCount(entities); i++)
            {
                ref EntityDescription entity = ref UnsafeList.GetRef<EntityDescription>(entities, i);
                if (entity.parent == parentId)
                {
                    UnsafeList.Add(children, entity.id);
                }
            }

            return UnsafeList.AsList<EntityID>(children);
        }

        public static uint TryReadComponents<T>(UnsafeWorld* world, ReadOnlySpan<EntityID> entities, Span<T> components, Span<bool> contains) where T : unmanaged
        {
            Allocations.ThrowIfNull((nint)world);

            ComponentType type = ComponentType.Get<T>();
            uint count = 0;
            for (uint i = 0; i < entities.Length; i++)
            {
                EntityID id = entities[(int)i];
                uint index = id.value - 1;
                ref EntityDescription entity = ref UnsafeList.GetRef<EntityDescription>(world->entities, index);
                if (entity.componentTypes.Contains(type))
                {
                    CollectionOfComponents data = world->Components[entity.componentTypes];
                    uint componentIndex = data.entities.IndexOf(id);
                    ref UnsafeList* list = ref data.lists[type.value - 1];
                    UnsafeList.CopyTo(list, componentIndex, components, i);
                    count++;
                    contains[(int)i] = true;
                }
                else
                {
                    components[(int)i] = default;
                    contains[(int)i] = false;
                }
            }

            return count;
        }

        public static void ReadComponents<T>(UnsafeWorld* world, ReadOnlySpan<EntityID> entities, Span<T> destination) where T : unmanaged
        {
            Allocations.ThrowIfNull((nint)world);

            if (destination.Length < entities.Length)
            {
                throw new ArgumentException("Destination span is too small.");
            }

            ComponentType type = ComponentType.Get<T>();
            for (uint i = 0; i < entities.Length; i++)
            {
                EntityID id = entities[(int)i];
                uint index = id.value - 1;
                ref EntityDescription entity = ref UnsafeList.GetRef<EntityDescription>(world->entities, index);
                if (entity.componentTypes.Contains(type))
                {
                    CollectionOfComponents data = world->Components[entity.componentTypes];
                    uint componentIndex = data.entities.IndexOf(id);
                    ref UnsafeList* list = ref data.lists[type.value - 1];
                    destination[(int)i] = UnsafeList.GetRef<T>(list, componentIndex);
                }
                else
                {
                    destination[(int)i] = default;
                }
            }
        }

        public static bool HasParent(UnsafeWorld* world, EntityID id)
        {
            Allocations.ThrowIfNull((nint)world);
            ThrowIfEntityMissing(world, id);

            uint index = id.value - 1;
            ref EntityDescription entity = ref UnsafeList.GetRef<EntityDescription>(world->entities, index);
            return entity.parent != default;
        }

        public static EntityID GetParent(UnsafeWorld* world, EntityID id)
        {
            Allocations.ThrowIfNull((nint)world);
            ThrowIfEntityMissing(world, id);

            uint index = id.value - 1;
            ref EntityDescription entity = ref UnsafeList.GetRef<EntityDescription>(world->entities, index);
            return entity.parent;
        }

        public static void SetParent(UnsafeWorld* world, EntityID id, EntityID parent)
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
        public static EntityID CreateEntity(UnsafeWorld* world, EntityID parent, ComponentTypeMask componentTypes)
        {
            Allocations.ThrowIfNull((nint)world);

            EntityID id = GenerateEntity(world, parent, componentTypes);
            CollectionOfComponents newData = world->Components[componentTypes];
            for (int i = 0; i < ComponentTypeMask.MaxValues; i++)
            {
                ComponentType type = new(i);
                if (componentTypes.Contains(type))
                {
                    ref UnsafeList* componentList = ref newData.lists[i];
                    UnsafeList.AddDefault(componentList);
                }
            }

            return id;
        }

        public static bool ContainsEntity(UnsafeWorld* world, EntityID id)
        {
            Allocations.ThrowIfNull((nint)world);

            uint index = id.value - 1;
            if (index >= UnsafeList.GetCount(world->entities))
            {
                return false;
            }

            ref EntityDescription entity = ref UnsafeList.GetRef<EntityDescription>(world->entities, index);
            return entity.id == id;
        }

        public static UnmanagedList<C> CreateCollection<C>(UnsafeWorld* world, EntityID id) where C : unmanaged
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

        public static void AddToCollection<C>(UnsafeWorld* world, EntityID id, C value) where C : unmanaged
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

        public static bool ContainsCollection<C>(UnsafeWorld* world, EntityID id) where C : unmanaged
        {
            Allocations.ThrowIfNull((nint)world);
            ThrowIfEntityMissing(world, id);

            uint index = id.value - 1;
            ref EntityDescription entity = ref UnsafeList.GetRef<EntityDescription>(world->entities, index);
            return entity.collectionTypes.Contains<C>();
        }

        public static UnmanagedList<C> GetCollection<C>(UnsafeWorld* world, EntityID id) where C : unmanaged
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

        public static void RemoveAtFromCollection<C>(UnsafeWorld* world, EntityID id, uint index) where C : unmanaged
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

        public static void DestroyCollection<C>(UnsafeWorld* world, EntityID id) where C : unmanaged
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
                UnsafeList.Free(list);
                entity.collectionTypes.Remove(type);
                list = default;
            }
            else
            {
                throw new NullReferenceException($"Array {type} not found.");
            }
        }

        public static void ClearCollection<C>(UnsafeWorld* world, EntityID id) where C : unmanaged
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

        public static void AddComponent<C>(UnsafeWorld* world, EntityID id, C component) where C : unmanaged
        {
            Allocations.ThrowIfNull((nint)world);
            ThrowIfEntityMissing(world, id);

            ComponentType addedType = ComponentType.Get<C>();
            ThrowIfComponentAlreadyPresent(world, id, addedType);

            uint index = id.value - 1;
            ref EntityDescription entity = ref UnsafeList.GetRef<EntityDescription>(world->entities, index);
            ComponentTypeMask previousTypes = entity.componentTypes;
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

        public static void RemoveComponent<C>(UnsafeWorld* world, EntityID id) where C : unmanaged
        {
            Allocations.ThrowIfNull((nint)world);
            ThrowIfEntityMissing(world, id);

            ComponentType removedType = ComponentType.Get<C>();
            ThrowIfComponentMissing(world, id, removedType);

            uint index = id.value - 1;
            ref EntityDescription entity = ref UnsafeList.GetRef<EntityDescription>(world->entities, index);
            ComponentTypeMask previousTypes = entity.componentTypes;
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

        public static bool ContainsComponent(UnsafeWorld* world, EntityID id, ComponentType type)
        {
            Allocations.ThrowIfNull((nint)world);
            ThrowIfEntityMissing(world, id);

            uint index = id.value - 1;
            ref EntityDescription entity = ref UnsafeList.GetRef<EntityDescription>(world->entities, index);
            return entity.componentTypes.Contains(type);
        }

        public static ref C GetComponentRef<C>(UnsafeWorld* world, EntityID id) where C : unmanaged
        {
            Allocations.ThrowIfNull((nint)world);
            ThrowIfEntityMissing(world, id);

            ComponentType type = ComponentType.Get<C>();
            ThrowIfComponentMissing(world, id, type);

            uint index = id.value - 1;
            ref EntityDescription entity = ref UnsafeList.GetRef<EntityDescription>(world->entities, index);
            CollectionOfComponents data = world->Components[entity.componentTypes];
            uint componentIndex = data.entities.IndexOf(id);
            ref UnsafeList* list = ref data.lists[type.value - 1];
            return ref UnsafeList.GetRef<C>(list, componentIndex);
        }

        public static Span<byte> GetComponentBytes(UnsafeWorld* world, EntityID id, ComponentType type)
        {
            Allocations.ThrowIfNull((nint)world);
            ThrowIfEntityMissing(world, id);
            ThrowIfComponentMissing(world, id, type);

            uint index = id.value - 1;
            ref EntityDescription entity = ref UnsafeList.GetRef<EntityDescription>(world->entities, index);
            CollectionOfComponents data = world->Components[entity.componentTypes];
            uint componentIndex = data.entities.IndexOf(id);
            ref UnsafeList* list = ref data.lists[type.value - 1];
            return UnsafeList.Get(list, componentIndex);
        }

        public static void QueryComponents(UnsafeWorld* world, ComponentTypeMask types, QueryCallback action)
        {
            Allocations.ThrowIfNull((nint)world);

            uint count = UnsafeList.GetCount(world->componentArchetypes);
            for (uint i = 0; i < count; i++)
            {
                ComponentTypeMask archetype = UnsafeList.Get<ComponentTypeMask>(world->componentArchetypes, i);
                if (archetype.Contains(types))
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

        public static void QueryComponents<C1>(UnsafeWorld* world, QueryCallback action) where C1 : unmanaged
        {
            Allocations.ThrowIfNull((nint)world);

            ComponentType t1 = ComponentType.Get<C1>();
            uint count = UnsafeList.GetCount(world->componentArchetypes);
            for (uint i = 0; i < count; i++)
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

        public static void QueryComponents<C1>(UnsafeWorld* world, QueryCallback<C1> action) where C1 : unmanaged
        {
            Allocations.ThrowIfNull((nint)world);

            ComponentType t1 = ComponentType.Get<C1>();
            uint count = UnsafeList.GetCount(world->componentArchetypes);
            for (uint i = 0; i < count; i++)
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

        public static void QueryComponents<C1, C2>(UnsafeWorld* world, QueryCallback<C1, C2> action) where C1 : unmanaged where C2 : unmanaged
        {
            Allocations.ThrowIfNull((nint)world);

            ComponentType t1 = ComponentType.Get<C1>();
            ComponentType t2 = ComponentType.Get<C2>();
            uint count = UnsafeList.GetCount(world->componentArchetypes);
            for (uint i = 0; i < count; i++)
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

        public static void QueryComponents<C1, C2, C3>(UnsafeWorld* world, QueryCallback<C1, C2, C3> action) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged
        {
            Allocations.ThrowIfNull((nint)world);

            ComponentType t1 = ComponentType.Get<C1>();
            ComponentType t2 = ComponentType.Get<C2>();
            ComponentType t3 = ComponentType.Get<C3>();
            uint count = UnsafeList.GetCount(world->componentArchetypes);
            for (uint i = 0; i < count; i++)
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

        public static void QueryComponents<C1, C2, C3, C4>(UnsafeWorld* world, QueryCallback<C1, C2, C3, C4> action) where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged
        {
            Allocations.ThrowIfNull((nint)world);

            ComponentType t1 = ComponentType.Get<C1>();
            ComponentType t2 = ComponentType.Get<C2>();
            ComponentType t3 = ComponentType.Get<C3>();
            ComponentType t4 = ComponentType.Get<C4>();
            uint count = UnsafeList.GetCount(world->componentArchetypes);
            for (uint i = 0; i < count; i++)
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

        public readonly unsafe struct Listener
        {
            public readonly delegate* unmanaged<World, Container, void> callback;

            public Listener(delegate* unmanaged<World, Container, void> callback)
            {
                this.callback = callback;
            }

            public readonly void Invoke(World world, Container message)
            {
                callback(world, message);
            }
        }
    }
}
