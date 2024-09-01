﻿using Simulation.Unsafe;
using System;
using System.Collections.Generic;
using Unmanaged;
using Unmanaged.Collections;

namespace Simulation
{
    /// <summary>
    /// Contains arbitrary data sorted into groups of entities for processing.
    /// </summary>
    public unsafe struct World : IDisposable, IEquatable<World>, ISerializable
    {
        internal UnsafeWorld* value;

        public readonly nint Address => (nint)value;

        /// <summary>
        /// Amount of entities that exist in the world.
        /// </summary>
        public readonly uint Count => Slots.Count - Free.Count;

        /// <summary>
        /// The current maximum amount of referrable entities.
        /// <para>Collections of this size are guaranteed to
        /// be able to store all entity values/positions.</para>
        /// </summary>
        public readonly uint MaxEntityValue => Slots.Count;

        public readonly bool IsDisposed => UnsafeWorld.IsDisposed(value);
        public readonly UnmanagedList<EntityDescription> Slots => UnsafeWorld.GetEntitySlots(value);
        public readonly UnmanagedList<uint> Free => UnsafeWorld.GetFreeEntities(value);
        public readonly UnmanagedDictionary<uint, ComponentChunk> ComponentChunks => UnsafeWorld.GetComponentChunks(value);

        /// <summary>
        /// All entities that exist in the world.
        /// </summary>
        public readonly IEnumerable<uint> Entities
        {
            get
            {
                UnmanagedList<EntityDescription> slots = Slots;
                UnmanagedList<uint> free = Free;
                for (uint i = 0; i < slots.Count; i++)
                {
                    EntityDescription description = slots[i];
                    if (!free.Contains(description.entity))
                    {
                        yield return description.entity;
                    }
                }
            }
        }

        public readonly uint this[uint index]
        {
            get
            {
                uint i = 0;
                for (uint j = 0; j < Slots.Count; j++)
                {
                    EntityDescription description = Slots[j];
                    if (!Free.Contains(description.entity))
                    {
                        if (i == index)
                        {
                            return description.entity;
                        }

                        i++;
                    }
                }

                throw new IndexOutOfRangeException();
            }
        }

#if NET5_0_OR_GREATER
        /// <summary>
        /// Creates a new disposable world.
        /// </summary>
        public World()
        {
            value = UnsafeWorld.Allocate();
        }
#endif

        public World(UnsafeWorld* value)
        {
            this.value = value;
        }

        /// <summary>
        /// Disposes everything in the world and the world itself.
        /// </summary>
        public void Dispose()
        {
            UnsafeWorld.Free(ref value);
        }

        /// <summary>
        /// Resets the world to default state.
        /// </summary>
        public readonly void Clear(bool clearEvents = false, bool clearListeners = false)
        {
            if (clearEvents)
            {
                UnsafeWorld.ClearEvents(value);
            }

            if (clearListeners)
            {
                UnsafeWorld.ClearListeners(value);
            }

            UnsafeWorld.ClearEntities(value);
        }

        public readonly override string ToString()
        {
            if (value == default)
            {
                return "World (disposed)";
            }

            return $"World {Address} (count: {Count})";
        }

        public readonly override bool Equals(object? obj)
        {
            return obj is World world && Equals(world);
        }

        public readonly bool Equals(World other)
        {
            if (IsDisposed && other.IsDisposed)
            {
                return true;
            }
            else if (IsDisposed != other.IsDisposed)
            {
                return false;
            }

            return Address == other.Address;
        }

        public readonly override int GetHashCode()
        {
            if (value is null)
            {
                return 0;
            }

            return value->GetHashCode();
        }

        readonly void ISerializable.Write(BinaryWriter writer)
        {
            //collect info about all types referenced
            using UnmanagedList<RuntimeType> uniqueTypes = UnmanagedList<RuntimeType>.Create();
            foreach (uint hash in ComponentChunks.Keys)
            {
                ComponentChunk chunk = ComponentChunks[hash];
                foreach (RuntimeType type in chunk.Types)
                {
                    if (!uniqueTypes.Contains(type))
                    {
                        uniqueTypes.Add(type);
                    }
                }
            }

            for (uint i = 0; i < Slots.Count; i++)
            {
                EntityDescription slot = Slots[i];
                if (!slot.arrayTypes.IsDisposed)
                {
                    foreach (RuntimeType type in slot.arrayTypes)
                    {
                        if (!uniqueTypes.Contains(type))
                        {
                            uniqueTypes.Add(type);
                        }
                    }
                }
            }

            //write info about the type tree
            writer.WriteValue(uniqueTypes.Count);
            for (uint a = 0; a < uniqueTypes.Count; a++)
            {
                RuntimeType type = uniqueTypes[a];
                writer.WriteValue(type);
            }

            //write each entity and its components
            writer.WriteValue(Count);
            for (uint i = 0; i < Slots.Count; i++)
            {
                EntityDescription slot = Slots[i];
                uint entity = slot.entity;
                if (!Free.Contains(entity))
                {
                    writer.WriteValue(entity);
                    writer.WriteValue(slot.parent);

                    //write component
                    ComponentChunk chunk = ComponentChunks[slot.componentsKey];
                    writer.WriteValue((uint)chunk.Types.Length);
                    foreach (RuntimeType type in chunk.Types)
                    {
                        writer.WriteValue(uniqueTypes.IndexOf(type));
                        Span<byte> componentBytes = chunk.GetComponentBytes(chunk.Entities.IndexOf(entity), type);
                        writer.WriteSpan<byte>(componentBytes);
                    }

                    //write arrays
                    if (slot.arrayTypes.IsDisposed)
                    {
                        writer.WriteValue(0u);
                    }
                    else
                    {
                        writer.WriteValue(slot.arrayTypes.Count);
                        for (uint t = 0; t < slot.arrayTypes.Count; t++)
                        {
                            RuntimeType type = slot.arrayTypes[t];
                            void* array = slot.arrays[t];
                            uint arrayLength = slot.arrayLengths[t];
                            writer.WriteValue(uniqueTypes.IndexOf(type));
                            writer.WriteValue(arrayLength);
                            if (arrayLength > 0)
                            {
                                writer.WriteSpan<byte>(new Span<byte>(array, (int)(arrayLength * type.Size)));
                            }
                        }
                    }

                    //write references
                    if (slot.references == default)
                    {
                        writer.WriteValue(0u);
                    }
                    else
                    {
                        writer.WriteValue(slot.references.Count);
                        foreach (uint referencedEntity in slot.references)
                        {
                            writer.WriteValue(referencedEntity);
                        }
                    }
                }
            }
        }

        void ISerializable.Read(BinaryReader reader)
        {
            value = UnsafeWorld.Allocate();
            uint typeCount = reader.ReadValue<uint>();
            using UnmanagedList<RuntimeType> uniqueTypes = UnmanagedList<RuntimeType>.Create();
            Span<char> buffer = stackalloc char[256];
            for (uint i = 0; i < typeCount; i++)
            {
                RuntimeType type = reader.ReadValue<RuntimeType>();
                uniqueTypes.Add(type);
            }

            //create entities and fill them with components and arrays
            uint entityCount = reader.ReadValue<uint>();
            uint currentEntityId = 1;
            using UnmanagedList<uint> temporaryEntities = UnmanagedList<uint>.Create();
            for (uint i = 0; i < entityCount; i++)
            {
                uint entityId = reader.ReadValue<uint>();
                uint parentId = reader.ReadValue<uint>();

                //skip through the island of free entities
                uint catchup = entityId - currentEntityId;
                for (uint j = 0; j < catchup; j++)
                {
                    uint temporaryEntity = CreateEntity();
                    temporaryEntities.Add(temporaryEntity);
                }

                uint entity = CreateEntity();
                if (parentId != default)
                {
                    ref EntityDescription slot = ref Slots.GetRef(entity - 1);
                    slot.parent = parentId;
                    UnsafeWorld.NotifyParentChange(this, entity, parentId);
                }

                //read components
                uint componentCount = reader.ReadValue<uint>();
                for (uint j = 0; j < componentCount; j++)
                {
                    uint typeIndex = reader.ReadValue<uint>();
                    RuntimeType type = uniqueTypes[typeIndex];
                    Span<byte> bytes = UnsafeWorld.AddComponent(value, entity, type);
                    reader.ReadSpan<byte>(type.Size).CopyTo(bytes);
                    UnsafeWorld.NotifyComponentAdded(this, entity, type);
                }

                //read arrays
                uint arrayCount = reader.ReadValue<uint>();
                for (uint a = 0; a < arrayCount; a++)
                {
                    uint typeIndex = reader.ReadValue<uint>();
                    uint arrayLength = reader.ReadValue<uint>();
                    RuntimeType arrayType = uniqueTypes[typeIndex];
                    uint byteCount = arrayLength * arrayType.Size;
                    void* array = UnsafeWorld.CreateArray(value, entity, arrayType, arrayLength);
                    if (arrayLength > 0)
                    {
                        reader.ReadSpan<byte>(byteCount).CopyTo(new Span<byte>(array, (int)byteCount));
                    }
                }

                //read references
                uint referenceCount = reader.ReadValue<uint>();
                for (uint j = 0; j < referenceCount; j++)
                {
                    uint referencedEntity = reader.ReadValue<uint>();
                    AddReference(entity, referencedEntity);
                }

                currentEntityId = entityId + 1;
            }

            //assign children
            foreach (uint entity in Entities)
            {
                uint parent = GetParent(entity);
                if (parent != default)
                {
                    ref EntityDescription parentSlot = ref Slots.GetRef(parent - 1);
                    if (parentSlot.children == default)
                    {
                        parentSlot.children = UnmanagedList<uint>.Create();
                    }

                    parentSlot.children.Add(entity);
                }
            }

            //destroy temporary entities
            for (uint i = 0; i < temporaryEntities.Count; i++)
            {
                UnsafeWorld.DestroyEntity(value, temporaryEntities[i]);
            }
        }

        /// <summary>
        /// Creates new entities with the data from the given world.
        /// </summary>
        public readonly void Append(World sourceWorld)
        {
            uint start = Slots.Count;
            uint entityIndex = 1;
            foreach (EntityDescription sourceSlot in sourceWorld.Slots)
            {
                uint sourceEntity = sourceSlot.entity;
                if (!sourceWorld.Free.Contains(sourceEntity))
                {
                    uint destinationEntity = start + entityIndex;
                    InitializeEntity(destinationEntity, start + sourceSlot.parent);
                    entityIndex++;

                    //add components
                    ComponentChunk sourceChunk = sourceWorld.ComponentChunks[sourceSlot.componentsKey];
                    uint sourceIndex = sourceChunk.Entities.IndexOf(sourceEntity);
                    foreach (RuntimeType componentType in sourceChunk.Types)
                    {
                        Span<byte> bytes = UnsafeWorld.AddComponent(value, destinationEntity, componentType);
                        sourceChunk.GetComponentBytes(sourceIndex, componentType).CopyTo(bytes);
                        UnsafeWorld.NotifyComponentAdded(this, destinationEntity, componentType);
                    }

                    //add arrays
                    if (!sourceSlot.arrayTypes.IsDisposed)
                    {
                        for (uint t = 0; t < sourceSlot.arrayTypes.Count; t++)
                        {
                            RuntimeType sourceArrayType = sourceSlot.arrayTypes[t];
                            uint sourceArrayLength = sourceSlot.arrayLengths[t];
                            void* sourceArray = sourceSlot.arrays[t];
                            void* destinationArray = UnsafeWorld.CreateArray(value, destinationEntity, sourceArrayType, sourceArrayLength);
                            if (sourceArrayLength > 0)
                            {
                                Span<byte> sourceBytes = new(sourceArray, (int)(sourceArrayLength * sourceArrayType.Size));
                                sourceBytes.CopyTo(new Span<byte>(destinationArray, (int)(sourceArrayLength * sourceArrayType.Size)));
                            }
                        }
                    }
                }
            }

            //assign references last
            entityIndex = 1;
            foreach (EntityDescription sourceSlot in sourceWorld.Slots)
            {
                uint sourceEntity = sourceSlot.entity;
                if (!sourceWorld.Free.Contains(sourceEntity))
                {
                    if (sourceSlot.references != default)
                    {
                        uint destinationEntity = start + entityIndex;
                        foreach (uint referencedEntity in sourceSlot.references)
                        {
                            AddReference(destinationEntity, start + referencedEntity);
                        }
                    }
                }
            }
        }

        public readonly void Submit<T>(T message) where T : unmanaged
        {
            UnsafeWorld.Submit(value, Container.Create(message));
        }

        public readonly void Submit(Container container)
        {
            UnsafeWorld.Submit(value, container);
        }

        /// <summary>
        /// Iterates over all events that we submitted with <see cref="Submit"/>,
        /// and notifies the registered listeners.
        /// </summary>
        public readonly void Poll()
        {
            UnsafeWorld.Poll(value);
        }

        private readonly void Perform(Instruction instruction, UnmanagedList<uint> selection, UnmanagedList<uint> entities)
        {
            if (instruction.type == Instruction.Type.CreateEntity)
            {
                uint count = (uint)instruction.A;
                for (uint i = 0; i < count; i++)
                {
                    uint newEntity = CreateEntity();
                    selection.Add(newEntity);
                    entities.Add(newEntity);
                }
            }
            else if (instruction.type == Instruction.Type.DestroyEntities)
            {
                uint start = (uint)instruction.A;
                uint count = (uint)instruction.B;
                if (start == 0 && count == 0)
                {
                    for (uint i = 0; i < selection.Count; i++)
                    {
                        uint entity = selection[i];
                        DestroyEntity(entity);
                    }
                }
                else
                {
                    uint end = start + count;
                    for (uint i = start; i < end; i++)
                    {
                        uint entity = entities[i];
                        DestroyEntity(entity);
                    }
                }
            }
            else if (instruction.type == Instruction.Type.ClearSelection)
            {
                selection.Clear();
            }
            else if (instruction.type == Instruction.Type.SelectEntity)
            {
                if (instruction.A == 0)
                {
                    uint relativeOffset = (uint)instruction.B;
                    uint entity = entities[(entities.Count - 1) - relativeOffset];
                    selection.Clear();
                    selection.Add(entity);
                }
                else if (instruction.A == 1)
                {
                    uint entity = (uint)instruction.B;
                    selection.Clear();
                    selection.Add(entity);
                }
            }
            else if (instruction.type == Instruction.Type.SetParent)
            {
                bool isRelative = instruction.A == 0;
                if (isRelative)
                {
                    uint relativeOffset = (uint)instruction.B;
                    uint parent = entities[(entities.Count - 1) - relativeOffset];
                    for (uint i = 0; i < selection.Count; i++)
                    {
                        uint entity = selection[i];
                        SetParent(entity, parent);
                    }
                }
                else
                {
                    uint parent = (uint)instruction.B;
                    for (uint i = 0; i < selection.Count; i++)
                    {
                        uint entity = selection[i];
                        SetParent(entity, parent);
                    }
                }
            }
            else if (instruction.type == Instruction.Type.AddReference)
            {
                bool isRelative = instruction.A == 0;
                if (isRelative)
                {
                    uint relativeOffset = (uint)instruction.B;
                    uint referencedEntity = entities[(entities.Count - 1) - relativeOffset];
                    for (uint i = 0; i < selection.Count; i++)
                    {
                        uint entity = selection[i];
                        AddReference(entity, referencedEntity);
                    }
                }
                else
                {
                    uint referencedEntity = (uint)instruction.B;
                    for (uint i = 0; i < selection.Count; i++)
                    {
                        uint entity = selection[i];
                        AddReference(entity, referencedEntity);
                    }
                }
            }
            else if (instruction.type == Instruction.Type.RemoveReference)
            {
                rint reference = new((uint)instruction.A);
                for (uint i = 0; i < selection.Count; i++)
                {
                    uint entity = selection[i];
                    RemoveReference(entity, reference);
                }
            }
            else if (instruction.type == Instruction.Type.AddComponent)
            {
                RuntimeType componentType = new((uint)instruction.A);
                Allocation allocation = new((void*)(nint)instruction.B);
                Span<byte> componentData = allocation.AsSpan<byte>(0, componentType.Size);
                for (uint i = 0; i < selection.Count; i++)
                {
                    uint entity = selection[i];
                    AddComponent(entity, componentType, componentData);
                }
            }
            else if (instruction.type == Instruction.Type.RemoveComponent)
            {
                RuntimeType componentType = new((uint)instruction.A);
                for (uint i = 0; i < selection.Count; i++)
                {
                    uint entity = selection[i];
                    RemoveComponent(entity, componentType);
                }
            }
            else if (instruction.type == Instruction.Type.SetComponent)
            {
                RuntimeType componentType = new((uint)instruction.A);
                Allocation allocation = new((void*)(nint)instruction.B);
                Span<byte> componentBytes = allocation.AsSpan<byte>(0, componentType.Size);
                for (uint i = 0; i < selection.Count; i++)
                {
                    uint entity = selection[i];
                    SetComponent(entity, componentType, componentBytes);
                }
            }
            else if (instruction.type == Instruction.Type.CreateArray)
            {
                RuntimeType arrayType = new((uint)instruction.A);
                uint arrayTypeSize = arrayType.Size;
                Allocation allocation = new((void*)(nint)instruction.B);
                uint count = (uint)instruction.C;
                for (uint i = 0; i < selection.Count; i++)
                {
                    uint entity = selection[i];
                    Allocation newArray = CreateArray(entity, arrayType, count);
                    allocation.CopyTo(newArray, 0, 0, count * arrayTypeSize);
                }
            }
            else if (instruction.type == Instruction.Type.DestroyArray)
            {
                RuntimeType arrayType = new((uint)instruction.A);
                for (uint i = 0; i < selection.Count; i++)
                {
                    uint entity = selection[i];
                    DestroyArray(entity, arrayType);
                }
            }
            else if (instruction.type == Instruction.Type.SetArrayElement)
            {
                RuntimeType arrayType = new((uint)instruction.A);
                uint arrayTypeSize = arrayType.Size;
                Allocation allocation = new((void*)(nint)instruction.B);
                uint elementCount = allocation.Read<uint>();
                uint start = (uint)instruction.C;
                Span<byte> elementBytes = allocation.AsSpan(sizeof(uint), elementCount * arrayTypeSize);
                for (uint i = 0; i < selection.Count; i++)
                {
                    uint entity = selection[i];
                    void* array = UnsafeWorld.GetArray(value, entity, arrayType, out uint entityArrayLength);
                    Span<byte> entityArray = new(array, (int)(entityArrayLength * arrayTypeSize));
                    elementBytes.CopyTo(entityArray.Slice((int)(start * arrayTypeSize), (int)(elementCount * arrayTypeSize)));
                }
            }
            else if (instruction.type == Instruction.Type.ResizeArray)
            {
                RuntimeType arrayType = new((uint)instruction.A);
                uint newLength = (uint)instruction.B;
                for (uint i = 0; i < selection.Count; i++)
                {
                    uint entity = selection[i];
                    UnsafeWorld.ResizeArray(value, entity, arrayType, newLength);
                }
            }
            else
            {
                throw new NotImplementedException($"Unknown instruction: {instruction.type}");
            }
        }

        public readonly void Perform(ReadOnlySpan<Instruction> instructions)
        {
            using UnmanagedList<uint> selection = UnmanagedList<uint>.Create();
            using UnmanagedList<uint> entities = UnmanagedList<uint>.Create();
            foreach (Instruction instruction in instructions)
            {
                Perform(instruction, selection, entities);
            }
        }

        public readonly void Perform(UnmanagedList<Instruction> instructions)
        {
            Perform(instructions.AsSpan());
        }

        public readonly void Perform(UnmanagedArray<Instruction> instructions)
        {
            Perform(instructions.AsSpan());
        }

        /// <summary>
        /// Performs all instructions in the given operation.
        /// </summary>
        public readonly void Perform(Operation operation)
        {
            using UnmanagedList<uint> selection = UnmanagedList<uint>.Create();
            using UnmanagedList<uint> entities = UnmanagedList<uint>.Create();
            uint length = operation.Length;
            for (uint i = 0; i < length; i++)
            {
                Instruction instruction = operation[i];
                Perform(instruction, selection, entities);
            }
        }

        /// <summary>
        /// Destroys the given entity assuming it exists.
        /// </summary>
        public readonly void DestroyEntity(uint entity, bool destroyChildren = true)
        {
            UnsafeWorld.DestroyEntity(value, entity, destroyChildren);
        }

#if NET5_0_OR_GREATER
        /// <summary>
        /// Creates a new listener for the given static callback.
        /// <para>Disposing the listener will unregister the callback.
        /// Their disposal is done automatically when the world is disposed.</para>
        /// </summary>
        public readonly Listener CreateListener<T>(delegate* unmanaged<World, Allocation, RuntimeType, void> callback) where T : unmanaged
        {
            return CreateListener(RuntimeType.Get<T>(), callback);
        }

        /// <summary>
        /// Creates a new listener for the given static callback.
        /// <para>Disposing the listener will unregister the callback.
        /// Their disposal is done automatically when the world is disposed.</para>
        /// </summary>
        public readonly Listener CreateListener(RuntimeType eventType, delegate* unmanaged<World, Allocation, RuntimeType, void> callback)
        {
            return UnsafeWorld.CreateListener(value, eventType, callback);
        }
#else
        /// <summary>
        /// Creates a new listener for the given static callback.
        /// <para>Disposing the listener will unregister the callback.
        /// Their disposal is done automatically when the world is disposed.</para>
        /// </summary>
        public readonly Listener CreateListener<T>(delegate*<World, Container, void> callback) where T : unmanaged
        {
            return CreateListener(RuntimeType.Get<T>(), callback);
        }

        /// <summary>
        /// Creates a new listener for the given static callback.
        /// <para>Disposing the listener will unregister the callback.
        /// Their disposal is done automatically when the world is disposed.</para>
        /// </summary>
        public readonly Listener CreateListener(RuntimeType eventType, delegate*<World, Container, void> callback)
        {
            return UnsafeWorld.CreateListener(value, eventType, callback);
        }
#endif

        public readonly ReadOnlySpan<RuntimeType> GetComponentTypes(uint entity)
        {
            UnsafeWorld.ThrowIfEntityIsMissing(value, entity);
            EntityDescription slot = Slots[entity - 1];
            ComponentChunk chunk = ComponentChunks[slot.componentsKey];
            return chunk.Types;
        }

        public readonly bool IsEnabled(uint entity)
        {
            UnsafeWorld.ThrowIfEntityIsMissing(value, entity);
            EntityDescription slot = Slots[entity - 1];
            return slot.IsEnabled;
        }

        public readonly void SetEnabled(uint entity, bool value)
        {
            UnsafeWorld.ThrowIfEntityIsMissing(this.value, entity);
            ref EntityDescription slot = ref Slots.GetRef(entity - 1);
            slot.SetEnabledState(value);
        }

        public readonly bool TryGetFirst<T>(out T found) where T : unmanaged
        {
            return TryGetFirst(out _, out found);
        }

        public readonly bool TryGetFirst<T>(out uint entity) where T : unmanaged
        {
            return TryGetFirst<T>(out entity, out _);
        }

        public readonly bool TryGetFirst<T>(out uint entity, out T component) where T : unmanaged
        {
            foreach (uint e in GetAll(RuntimeType.Get<T>()))
            {
                entity = e;
                component = GetComponentRef<T>(e);
                return true;
            }

            entity = default;
            component = default;
            return false;
        }

        public readonly T GetFirst<T>() where T : unmanaged
        {
            foreach (uint e in GetAll(RuntimeType.Get<T>()))
            {
                return GetComponentRef<T>(e);
            }

            throw new NullReferenceException($"No entity with component of type {typeof(T)} found.");
        }

        public readonly T GetFirst<T>(out uint entity) where T : unmanaged
        {
            foreach (uint e in GetAll(RuntimeType.Get<T>()))
            {
                entity = e;
                return GetComponentRef<T>(e);
            }

            throw new NullReferenceException($"No entity with component of type {typeof(T)} found.");
        }

        public readonly ref T GetFirstRef<T>() where T : unmanaged
        {
            foreach (uint e in GetAll(RuntimeType.Get<T>()))
            {
                return ref GetComponentRef<T>(e);
            }

            throw new NullReferenceException($"No entity with component of type {typeof(T)} found.");
        }

        /// <summary>
        /// Creates a new entity with an optionally assigned parent.
        /// </summary>
        public readonly uint CreateEntity(uint parent = default)
        {
            uint entity = GetNextEntity();
            InitializeEntity(entity, parent);
            return entity;
        }

        /// <summary>
        /// Returns the value for the next created entity.
        /// </summary>
        public readonly uint GetNextEntity()
        {
            return UnsafeWorld.GetNextEntity(value);
        }

        /// <summary>
        /// Creates an entity with the given value assuming its 
        /// not already in use (otherwise an <see cref="Exception"/> will be thrown).
        /// </summary>
        public readonly void InitializeEntity(uint value, uint parent)
        {
            UnsafeWorld.InitializeEntity(this.value, value, parent, Array.Empty<RuntimeType>());
        }

        /// <summary>
        /// Checks if the entity exists and is valid in this world.
        /// </summary>
        public readonly bool ContainsEntity(uint entity)
        {
            return UnsafeWorld.ContainsEntity(value, entity);
        }

        public readonly bool ContainsEntity<T>(T entity) where T : unmanaged, IEntity
        {
            return ContainsEntity(entity.Value);
        }

        /// <summary>
        /// Retrieves the parent of the given entity, <c>default</c> if none
        /// is assigned.
        /// </summary>
        public readonly uint GetParent(uint entity)
        {
            return UnsafeWorld.GetParent(value, entity);
        }

        /// <summary>
        /// Assigns a new parent.
        /// </summary>
        /// <returns><c>true</c> if the given parent entity was found and assigned successfuly.</returns>
        public readonly bool SetParent(uint entity, uint parent)
        {
            return UnsafeWorld.SetParent(value, entity, parent);
        }

        /// <summary>
        /// Retreives all children of the given entity.
        /// </summary>
        public readonly ReadOnlySpan<uint> GetChildren(uint entity)
        {
            UnsafeWorld.ThrowIfEntityIsMissing(value, entity);
            EntityDescription slot = Slots[entity - 1];
            if (!slot.children.IsDisposed)
            {
                return slot.children.AsSpan<uint>();
            }
            else return Array.Empty<uint>();
        }

        /// <summary>
        /// Adds a new reference to the given entity.
        /// </summary>
        /// <returns>An index offset by 1 that refers to this entity.</returns>
        public readonly rint AddReference(uint entity, uint referencedEntity)
        {
            UnsafeWorld.ThrowIfEntityIsMissing(value, entity);
            UnsafeWorld.ThrowIfEntityIsMissing(value, referencedEntity);
            ref EntityDescription slot = ref Slots.GetRef(entity - 1);
            if (slot.references == default)
            {
                slot.references = UnmanagedList<uint>.Create();
            }

            slot.references.Add(referencedEntity);
            return new(slot.references.Count);
        }

        public readonly rint AddReference<T>(uint entity, T referencedEntity) where T : unmanaged, IEntity
        {
            return AddReference(entity, referencedEntity.Value);
        }

        public readonly void SetReference(uint entity, rint reference, uint referencedEntity)
        {
            UnsafeWorld.ThrowIfEntityIsMissing(value, entity);
            UnsafeWorld.ThrowIfEntityIsMissing(value, referencedEntity);
            ref EntityDescription slot = ref Slots.GetRef(entity - 1);
            if (slot.references == default)
            {
                throw new IndexOutOfRangeException($"No references found on entity `{entity}`.");
            }

            slot.references[reference.value - 1] = referencedEntity;
        }

        public readonly void SetReference<T>(uint entity, rint reference, T referencedEntity) where T : unmanaged, IEntity
        {
            SetReference(entity, reference, referencedEntity.Value);
        }

        public readonly bool ContainsReference(uint entity, uint referencedEntity)
        {
            UnsafeWorld.ThrowIfEntityIsMissing(value, entity);
            UnsafeWorld.ThrowIfEntityIsMissing(value, referencedEntity);
            ref EntityDescription slot = ref Slots.GetRef(entity - 1);
            if (slot.references == default)
            {
                return false;
            }

            return slot.references.Contains(referencedEntity);
        }

        public readonly bool ContainsReference<T>(uint entity, T referencedEntity) where T : unmanaged, IEntity
        {
            return ContainsReference(entity, referencedEntity.Value);
        }

        public readonly bool ContainsReference(uint entity, rint reference)
        {
            UnsafeWorld.ThrowIfEntityIsMissing(value, entity);
            ref EntityDescription slot = ref Slots.GetRef(entity - 1);
            if (slot.references == default)
            {
                return false;
            }

            return (reference.value - 1) < slot.references.Count;
        }

        //todo: polish: this is kinda like `rint GetLastReference(uint entity)` <-- should it be like this instead?
        public readonly uint GetReferenceCount(uint entity)
        {
            UnsafeWorld.ThrowIfEntityIsMissing(value, entity);
            ref EntityDescription slot = ref Slots.GetRef(entity - 1);
            if (slot.references == default)
            {
                return 0;
            }

            return slot.references.Count;
        }

        public readonly uint GetReference(uint entity, rint reference)
        {
            UnsafeWorld.ThrowIfEntityIsMissing(value, entity);
            if (reference == default)
            {
                return default;
            }

            ref EntityDescription slot = ref Slots.GetRef(entity - 1);
            if (slot.references == default)
            {
                throw new IndexOutOfRangeException($"No references found on entity `{entity}`.");
            }

            return slot.references[reference - 1];
        }

        public readonly bool TryGetReference(uint entity, rint position, out uint referencedEntity)
        {
            UnsafeWorld.ThrowIfEntityIsMissing(value, entity);
            ref EntityDescription slot = ref Slots.GetRef(entity - 1);
            if (slot.references == default)
            {
                referencedEntity = default;
                return false;
            }

            uint index = position.value - 1;
            if (index < slot.references.Count)
            {
                referencedEntity = slot.references[index];
                return true;
            }

            referencedEntity = default;
            return false;
        }

        public readonly void RemoveReference(uint entity, uint referencedEntity)
        {
            UnsafeWorld.ThrowIfEntityIsMissing(value, entity);
            UnsafeWorld.ThrowIfEntityIsMissing(value, referencedEntity);
            ref EntityDescription slot = ref Slots.GetRef(entity - 1);
            if (slot.references == default)
            {
                throw new IndexOutOfRangeException($"No references found on entity `{entity}`.");
            }

            uint index = slot.references.IndexOf(referencedEntity);
            slot.references.RemoveAt(index);
        }

        public readonly void RemoveReference(uint entity, rint reference)
        {
            UnsafeWorld.ThrowIfEntityIsMissing(value, entity);
            ref EntityDescription slot = ref Slots.GetRef(entity - 1);
            if (slot.references == default)
            {
                throw new IndexOutOfRangeException($"No references found on entity `{entity}`.");
            }

            slot.references.RemoveAt(reference.value - 1);
        }

        /// <summary>
        /// Retrieves the types of all arrays on this entity.
        /// </summary>
        public readonly ReadOnlySpan<RuntimeType> GetArrayTypes(uint entity)
        {
            UnsafeWorld.ThrowIfEntityIsMissing(value, entity);
            EntityDescription slot = Slots[entity - 1];
            return slot.arrayTypes.AsSpan();
        }

        /// <summary>
        /// Creates a new uninitialized array with the given length and type.
        /// </summary>
        public readonly Allocation CreateArray(uint entity, RuntimeType arrayLength, uint length)
        {
            void* array = UnsafeWorld.CreateArray(value, entity, arrayLength, length);
            return new(array);
        }

        /// <summary>
        /// Creates a new array on this entity.
        /// </summary>
        public readonly Span<T> CreateArray<T>(uint entity, uint length) where T : unmanaged
        {
            Allocation array = CreateArray(entity, RuntimeType.Get<T>(), length);
            return array.AsSpan<T>(0, length);
        }

        /// <summary>
        /// Creates a new array containing the given span.
        /// </summary>
        public readonly void CreateArray<T>(uint entity, ReadOnlySpan<T> values) where T : unmanaged
        {
            Span<T> array = CreateArray<T>(entity, (uint)values.Length);
            values.CopyTo(array);
        }

        public readonly bool ContainsArray<T>(uint entity) where T : unmanaged
        {
            return ContainsArray(entity, RuntimeType.Get<T>());
        }

        public readonly bool ContainsArray(uint entity, RuntimeType arrayType)
        {
            return UnsafeWorld.ContainsArray(value, entity, arrayType);
        }

        /// <summary>
        /// Retrieves the array of type <typeparamref name="T"/> on the given entity.
        /// </summary>
        public readonly Span<T> GetArray<T>(uint entity) where T : unmanaged
        {
            void* array = UnsafeWorld.GetArray(value, entity, RuntimeType.Get<T>(), out uint length);
            return new Span<T>(array, (int)length);
        }

        public readonly void* GetArray(uint entity, RuntimeType arrayType, out uint length)
        {
            return UnsafeWorld.GetArray(value, entity, arrayType, out length);
        }

        public readonly Span<T> ResizeArray<T>(uint entity, uint newLength) where T : unmanaged
        {
            void* array = UnsafeWorld.ResizeArray(value, entity, RuntimeType.Get<T>(), newLength);
            return new Span<T>(array, (int)newLength);
        }

        public readonly void* ResizeArray(uint entity, RuntimeType arrayType, uint newLength)
        {
            return UnsafeWorld.ResizeArray(value, entity, arrayType, newLength);
        }

        public readonly bool TryGetArray<T>(uint entity, out Span<T> list) where T : unmanaged
        {
            if (ContainsArray<T>(entity))
            {
                list = GetArray<T>(entity);
                return true;
            }
            else
            {
                list = default;
                return false;
            }
        }

        /// <summary>
        /// Retrieves the element at the index from an existing list on this entity.
        /// </summary>
        public readonly ref T GetArrayElementRef<T>(uint entity, uint index) where T : unmanaged
        {
            UnsafeWorld.ThrowIfEntityIsMissing(value, entity);
            RuntimeType arrayType = RuntimeType.Get<T>();
            UnsafeWorld.ThrowIfArrayIsMissing(value, entity, arrayType);
            UnmanagedList<EntityDescription> slots = Slots;
            EntityDescription slot = slots[entity - 1];
            void* array = UnsafeWorld.GetArray(value, entity, arrayType, out uint arrayLength);
            Span<T> span = new(array, (int)arrayLength);
            return ref span[(int)index];
        }

        /// <summary>
        /// Retrieves the length of an existing list on this entity.
        /// </summary>
        public readonly uint GetArrayLength<T>(uint entity) where T : unmanaged
        {
            return UnsafeWorld.GetArrayLength(value, entity, RuntimeType.Get<T>());
        }

        public readonly void DestroyArray<T>(uint entity) where T : unmanaged
        {
            DestroyArray(entity, RuntimeType.Get<T>());
        }

        public readonly void DestroyArray(uint entity, RuntimeType arrayType)
        {
            UnsafeWorld.DestroyArray(value, entity, arrayType);
        }

        public readonly void AddComponent<T>(uint entity, T component) where T : unmanaged
        {
            ref T target = ref UnsafeWorld.AddComponent<T>(value, entity);
            target = component;
            UnsafeWorld.NotifyComponentAdded(this, entity, RuntimeType.Get<T>());
        }

        /// <summary>
        /// Adds a new component of the given type with uninitialized data.
        /// </summary>
        public readonly void AddComponent<T>(uint entity) where T : unmanaged
        {
            RuntimeType type = RuntimeType.Get<T>();
            UnsafeWorld.AddComponent(value, entity, type);
            UnsafeWorld.NotifyComponentAdded(this, entity, type);
        }

        /// <summary>
        /// Adds a new default component with the given type.
        /// </summary>
        public readonly void AddComponent(uint entity, RuntimeType componentType)
        {
            UnsafeWorld.AddComponent(value, entity, componentType);
            UnsafeWorld.NotifyComponentAdded(this, entity, componentType);
        }

        public readonly void AddComponent(uint entity, RuntimeType componentType, ReadOnlySpan<byte> componentData)
        {
            Span<byte> bytes = UnsafeWorld.AddComponent(value, entity, componentType);
            componentData.CopyTo(bytes);
            UnsafeWorld.NotifyComponentAdded(this, entity, componentType);
        }

        /// <summary>
        /// Adds a <c>default</c> component value and returns it by reference.
        /// </summary>
        public readonly ref T AddComponentRef<T>(uint entity) where T : unmanaged
        {
            AddComponent<T>(entity, default);
            return ref GetComponentRef<T>(entity);
        }

        public readonly void RemoveComponent<T>(uint entity) where T : unmanaged
        {
            UnsafeWorld.RemoveComponent<T>(value, entity);
        }

        public readonly void RemoveComponent(uint entity, RuntimeType componentType)
        {
            UnsafeWorld.RemoveComponent(value, entity, componentType);
        }

        /// <summary>
        /// Returns <c>true</c> if any entity in the world contains this component.
        /// </summary>
        public readonly bool ContainsAnyComponent<T>() where T : unmanaged
        {
            UnmanagedDictionary<uint, ComponentChunk> chunks = ComponentChunks;
            RuntimeType type = RuntimeType.Get<T>();
            foreach (uint hash in chunks.Keys)
            {
                ComponentChunk chunk = chunks[hash];
                if (chunk.Types.Contains(type))
                {
                    if (chunk.Entities.Count > 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public readonly bool ContainsComponent<T>(uint entity) where T : unmanaged
        {
            return ContainsComponent(entity, RuntimeType.Get<T>());
        }

        public readonly bool ContainsComponent(uint entity, RuntimeType type)
        {
            return UnsafeWorld.ContainsComponent(value, entity, type);
        }

        public readonly ref T GetComponentRef<T>(uint entity) where T : unmanaged
        {
            return ref UnsafeWorld.GetComponentRef<T>(value, entity);
        }

        /// <summary>
        /// Returns the component of the expected type if it exists, otherwise the given default
        /// value is used.
        /// </summary>
        public readonly T GetComponent<T>(uint entity, T defaultValue) where T : unmanaged
        {
            if (ContainsComponent<T>(entity))
            {
                return GetComponentRef<T>(entity);
            }
            else
            {
                return defaultValue;
            }
        }

        public readonly T GetComponent<T>(uint entity) where T : unmanaged
        {
            return GetComponentRef<T>(entity);
        }

        /// <summary>
        /// Fetches the component from this entity as a span of bytes.
        /// </summary>
        public readonly Span<byte> GetComponentBytes(uint entity, RuntimeType type)
        {
            return UnsafeWorld.GetComponentBytes(value, entity, type);
        }

        public readonly bool TryGetComponent<T>(uint entity, out T found) where T : unmanaged
        {
            if (ContainsComponent<T>(entity))
            {
                found = GetComponentRef<T>(entity);
                return true;
            }
            else
            {
                found = default;
                return false;
            }
        }

        public readonly ref T TryGetComponentRef<T>(uint entity, out bool contains) where T : unmanaged
        {
            if (ContainsComponent<T>(entity))
            {
                contains = true;
                return ref GetComponentRef<T>(entity);
            }
            else
            {
                contains = false;
                return ref System.Runtime.CompilerServices.Unsafe.AsRef<T>(null);
            }
        }

        public readonly void SetComponent<T>(uint entity, T component) where T : unmanaged
        {
            ref T existing = ref GetComponentRef<T>(entity);
            existing = component;
        }

        public readonly void SetComponent(uint entity, RuntimeType componentType, ReadOnlySpan<byte> componentData)
        {
            Span<byte> bytes = GetComponentBytes(entity, componentType);
            componentData.CopyTo(bytes);
        }

        /// <summary>
        /// Returns the main component chunk that contains the given entity.
        /// </summary>
        public readonly ComponentChunk GetComponentChunk(uint entity)
        {
            UnsafeWorld.ThrowIfEntityIsMissing(value, entity);
            EntityDescription slot = Slots[entity - 1];
            return ComponentChunks[slot.componentsKey];
        }

        /// <summary>
        /// Returns the main component chunk that contains all of the given component types.
        /// </summary>
        public readonly ComponentChunk GetComponentChunk(ReadOnlySpan<RuntimeType> componentTypes)
        {
            uint key = RuntimeType.CombineHash(componentTypes);
            if (ComponentChunks.TryGetValue(key, out ComponentChunk chunk))
            {
                return chunk;
            }
            else
            {
                throw new NullReferenceException($"No components found for the given types.");
            }
        }

        public readonly bool ContainsComponentChunk(ReadOnlySpan<RuntimeType> componentTypes)
        {
            uint key = RuntimeType.CombineHash(componentTypes);
            return ComponentChunks.ContainsKey(key);
        }

        public readonly bool TryGetComponentChunk(ReadOnlySpan<RuntimeType> componentTypes, out ComponentChunk chunk)
        {
            uint key = RuntimeType.CombineHash(componentTypes);
            return ComponentChunks.TryGetValue(key, out chunk);
        }

        /// <summary>
        /// Counts how many entities there are with component of type <typeparamref name="T"/>.
        /// </summary>
        public readonly uint CountEntities<T>(Query.Option options = Query.Option.IncludeDisabledEntities) where T : unmanaged
        {
            RuntimeType type = RuntimeType.Get<T>();
            return CountEntities(type, options);
        }

        public readonly uint CountEntities(RuntimeType type, Query.Option options = Query.Option.IncludeDisabledEntities)
        {
            uint count = 0;
            bool includeDisabled = (options & Query.Option.IncludeDisabledEntities) == Query.Option.IncludeDisabledEntities;
            foreach (uint hash in ComponentChunks.Keys)
            {
                ComponentChunk chunk = ComponentChunks[hash];
                if (chunk.Types.Contains(type))
                {
                    if (includeDisabled)
                    {
                        count += chunk.Entities.Count;
                    }
                    else
                    {
                        for (uint e = 0; e < chunk.Entities.Count; e++)
                        {
                            uint entity = chunk.Entities[e];
                            if (IsEnabled(entity))
                            {
                                count++;
                            }
                        }
                    }
                }
            }

            return count;
        }

        /// <summary>
        /// Copies components from the source entity onto the destination.
        /// <para>Components will be added if the destination entity doesnt
        /// contain them. Existing component data will be overwritten.</para>
        /// </summary>
        public readonly void CopyComponentsTo(uint sourceEntity, World destinationWorld, uint destinationEntity)
        {
            foreach (RuntimeType type in GetComponentTypes(sourceEntity))
            {
                if (!destinationWorld.ContainsComponent(destinationEntity, type))
                {
                    destinationWorld.AddComponent(destinationEntity, type);
                }

                Span<byte> sourceBytes = GetComponentBytes(sourceEntity, type);
                Span<byte> destinationBytes = destinationWorld.GetComponentBytes(destinationEntity, type);
                sourceBytes.CopyTo(destinationBytes);
            }
        }

        /// <summary>
        /// Copies all arrays from the source entity onto the destination.
        /// <para>Arrays will be created if the destination doesn't already
        /// contain them. Data will be overwritten, and lengths will be changed.</para>
        /// </summary>
        public readonly void CopyArraysTo(uint sourceEntity, World destinationWorld, uint destinationEntity)
        {
            foreach (RuntimeType sourceListType in GetArrayTypes(sourceEntity))
            {
                void* sourceArray = UnsafeWorld.GetArray(value, sourceEntity, sourceListType, out uint sourceLength);
                void* destinationArray;
                if (!destinationWorld.ContainsArray(destinationEntity, sourceListType))
                {
                    destinationArray = UnsafeWorld.CreateArray(destinationWorld.value, destinationEntity, sourceListType, sourceLength);
                }
                else
                {
                    destinationArray = UnsafeWorld.ResizeArray(destinationWorld.value, destinationEntity, sourceListType, sourceLength);
                }

                Span<byte> sourceBytes = new(sourceArray, (int)sourceLength);
                Span<byte> destinationBytes = new(destinationArray, (int)sourceLength);
                sourceBytes.CopyTo(destinationBytes);
            }
        }

        /// <summary>
        /// Finds all entities that contain all of the given component types and
        /// adds them to the given list.
        /// </summary>
        public readonly void Fill(ReadOnlySpan<RuntimeType> componentTypes, UnmanagedList<uint> list, Query.Option options = Query.Option.IncludeDisabledEntities)
        {
            bool exact = (options & Query.Option.ExactComponentTypes) == Query.Option.ExactComponentTypes;
            bool includeDisabled = (options & Query.Option.IncludeDisabledEntities) == Query.Option.IncludeDisabledEntities;
            foreach (uint hash in ComponentChunks.Keys)
            {
                ComponentChunk chunk = ComponentChunks[hash];
                if (chunk.ContainsTypes(componentTypes, exact))
                {
                    if (includeDisabled)
                    {
                        list.AddRange(chunk.Entities);
                    }
                    else
                    {
                        for (uint e = 0; e < chunk.Entities.Count; e++)
                        {
                            uint entity = chunk.Entities[e];
                            if (IsEnabled(entity))
                            {
                                list.Add(entity);
                            }
                        }
                    }
                }
            }
        }

        public readonly void Fill<T>(UnmanagedList<T> list, Query.Option options = Query.Option.IncludeDisabledEntities) where T : unmanaged
        {
            RuntimeType type = RuntimeType.Get<T>();
            bool includeDisabled = (options & Query.Option.IncludeDisabledEntities) == Query.Option.IncludeDisabledEntities;
            foreach (uint hash in ComponentChunks.Keys)
            {
                ComponentChunk chunk = ComponentChunks[hash];
                if (chunk.Types.Contains(type))
                {
                    if (includeDisabled)
                    {
                        list.AddRange(chunk.GetComponents<T>());
                    }
                    else
                    {
                        for (uint e = 0; e < chunk.Entities.Count; e++)
                        {
                            uint entity = chunk.Entities[e];
                            if (IsEnabled(entity))
                            {
                                list.Add(chunk.GetComponentRef<T>(e));
                            }
                        }
                    }
                }
            }
        }

        public readonly void Fill<T>(UnmanagedList<uint> entities, Query.Option options = Query.Option.IncludeDisabledEntities) where T : unmanaged
        {
            RuntimeType type = RuntimeType.Get<T>();
            bool includeDisabled = (options & Query.Option.IncludeDisabledEntities) == Query.Option.IncludeDisabledEntities;
            foreach (uint hash in ComponentChunks.Keys)
            {
                ComponentChunk chunk = ComponentChunks[hash];
                if (chunk.Types.Contains(type))
                {
                    if (includeDisabled)
                    {
                        entities.AddRange(chunk.Entities);
                    }
                    else
                    {
                        for (uint e = 0; e < chunk.Entities.Count; e++)
                        {
                            uint entity = chunk.Entities[e];
                            if (IsEnabled(entity))
                            {
                                entities.Add(entity);
                            }
                        }
                    }
                }
            }
        }

        public readonly void Fill<T>(UnmanagedList<T> components, UnmanagedList<uint> entities, Query.Option options = Query.Option.IncludeDisabledEntities) where T : unmanaged
        {
            RuntimeType type = RuntimeType.Get<T>();
            bool includeDisabled = (options & Query.Option.IncludeDisabledEntities) == Query.Option.IncludeDisabledEntities;
            foreach (uint hash in ComponentChunks.Keys)
            {
                ComponentChunk chunk = ComponentChunks[hash];
                if (chunk.Types.Contains(type))
                {
                    if (includeDisabled)
                    {
                        components.AddRange(chunk.GetComponents<T>());
                        entities.AddRange(chunk.Entities);
                    }
                    else
                    {
                        for (uint e = 0; e < chunk.Entities.Count; e++)
                        {
                            uint entity = chunk.Entities[e];
                            if (IsEnabled(entity))
                            {
                                components.Add(chunk.GetComponentRef<T>(e));
                                entities.Add(entity);
                            }
                        }
                    }
                }
            }
        }

        public readonly void Fill(RuntimeType componentType, UnmanagedList<uint> entities, Query.Option options = Query.Option.IncludeDisabledEntities)
        {
            bool includeDisabled = (options & Query.Option.IncludeDisabledEntities) == Query.Option.IncludeDisabledEntities;
            foreach (uint hash in ComponentChunks.Keys)
            {
                ComponentChunk chunk = ComponentChunks[hash];
                if (chunk.Types.Contains(componentType))
                {
                    if (includeDisabled)
                    {
                        entities.AddRange(chunk.Entities);
                    }
                    else
                    {
                        for (uint e = 0; e < chunk.Entities.Count; e++)
                        {
                            uint entity = chunk.Entities[e];
                            if (IsEnabled(entity))
                            {
                                entities.Add(entity);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Iterates over all entities that contain the given component type.
        /// </summary>
        public readonly IEnumerable<uint> GetAll(RuntimeType componentType, Query.Option options = Query.Option.IncludeDisabledEntities)
        {
            bool includeDisabled = (options & Query.Option.IncludeDisabledEntities) == Query.Option.IncludeDisabledEntities;
            uint chunkCount = ComponentChunks.Count;
            for (uint i = 0; i < chunkCount; i++)
            {
                uint hash = ComponentChunks.GetKeyAtIndex(i);
                ComponentChunk chunk = ComponentChunks[hash];
                if (chunk.Types.Contains(componentType))
                {
                    for (uint e = 0; e < chunk.Entities.Count; e++)
                    {
                        if (includeDisabled)
                        {
                            yield return chunk.Entities[e];
                        }
                        else
                        {
                            uint entity = chunk.Entities[e];
                            if (IsEnabled(entity))
                            {
                                yield return entity;
                            }
                        }
                    }
                }
            }
        }

        public readonly IEnumerable<uint> GetAll<T>(Query.Option options = Query.Option.IncludeDisabledEntities) where T : unmanaged
        {
            return GetAll(RuntimeType.Get<T>(), options);
        }

        public readonly void ForEach<T>(QueryCallback callback, Query.Option options = Query.Option.IncludeDisabledEntities) where T : unmanaged
        {
            RuntimeType componentType = RuntimeType.Get<T>();
            bool includeDisabled = (options & Query.Option.IncludeDisabledEntities) == Query.Option.IncludeDisabledEntities;
            foreach (uint hash in ComponentChunks.Keys)
            {
                ComponentChunk chunk = ComponentChunks[hash];
                if (chunk.Types.Contains(componentType))
                {
                    for (uint e = 0; e < chunk.Entities.Count; e++)
                    {
                        if (includeDisabled)
                        {
                            callback(chunk.Entities[e]);
                        }
                        else
                        {
                            uint entity = chunk.Entities[e];
                            if (IsEnabled(entity))
                            {
                                callback(entity);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Finds all entities that contain all of the given component types
        /// and invokes the callback for every entity found.
        /// <para>
        /// Destroying entities inside the callback is not recommended.
        /// </para>
        /// </summary>
        public readonly void ForEach(ReadOnlySpan<RuntimeType> componentTypes, QueryCallback callback, Query.Option options = Query.Option.IncludeDisabledEntities)
        {
            bool exact = (options & Query.Option.ExactComponentTypes) == Query.Option.ExactComponentTypes;
            bool includeDisabled = (options & Query.Option.IncludeDisabledEntities) == Query.Option.IncludeDisabledEntities;
            foreach (uint hash in ComponentChunks.Keys)
            {
                ComponentChunk chunk = ComponentChunks[hash];
                if (chunk.ContainsTypes(componentTypes, exact))
                {
                    for (uint e = 0; e < chunk.Entities.Count; e++)
                    {
                        if (includeDisabled)
                        {
                            callback(chunk.Entities[e]);
                        }
                        else
                        {
                            uint entity = chunk.Entities[e];
                            if (IsEnabled(entity))
                            {
                                callback(entity);
                            }
                        }
                    }
                }
            }
        }

        public readonly void ForEach<T>(QueryCallback<T> callback, Query.Option options = Query.Option.IncludeDisabledEntities) where T : unmanaged
        {
            Span<RuntimeType> types = stackalloc RuntimeType[1];
            types[0] = RuntimeType.Get<T>();
            bool includeDisabled = (options & Query.Option.IncludeDisabledEntities) == Query.Option.IncludeDisabledEntities;
            foreach (uint hash in ComponentChunks.Keys)
            {
                ComponentChunk chunk = ComponentChunks[hash];
                if (chunk.ContainsTypes(types))
                {
                    UnmanagedList<uint> entities = chunk.Entities;
                    for (uint e = 0; e < entities.Count; e++)
                    {
                        if (includeDisabled)
                        {
                            ref T t1 = ref chunk.GetComponentRef<T>(e);
                            callback(entities[e], ref t1);
                        }
                        else
                        {
                            uint entity = entities[e];
                            if (IsEnabled(entity))
                            {
                                ref T t1 = ref chunk.GetComponentRef<T>(e);
                                callback(entity, ref t1);
                            }
                        }
                    }
                }
            }
        }

        public readonly void ForEach<T1, T2>(QueryCallback<T1, T2> callback, Query.Option options = Query.Option.IncludeDisabledEntities) where T1 : unmanaged where T2 : unmanaged
        {
            Span<RuntimeType> types = stackalloc RuntimeType[2];
            types[0] = RuntimeType.Get<T1>();
            types[1] = RuntimeType.Get<T2>();
            bool includeDisabled = (options & Query.Option.IncludeDisabledEntities) == Query.Option.IncludeDisabledEntities;
            foreach (uint hash in ComponentChunks.Keys)
            {
                ComponentChunk chunk = ComponentChunks[hash];
                if (chunk.ContainsTypes(types))
                {
                    UnmanagedList<uint> entities = chunk.Entities;
                    for (uint e = 0; e < entities.Count; e++)
                    {
                        if (includeDisabled)
                        {
                            ref T1 t1 = ref chunk.GetComponentRef<T1>(e);
                            ref T2 t2 = ref chunk.GetComponentRef<T2>(e);
                            callback(entities[e], ref t1, ref t2);
                        }
                        else
                        {
                            uint entity = entities[e];
                            if (IsEnabled(entity))
                            {
                                ref T1 t1 = ref chunk.GetComponentRef<T1>(e);
                                ref T2 t2 = ref chunk.GetComponentRef<T2>(e);
                                callback(entity, ref t1, ref t2);
                            }
                        }
                    }
                }
            }
        }

        public readonly void ForEach<T1, T2, T3>(QueryCallback<T1, T2, T3> callback, Query.Option options = Query.Option.IncludeDisabledEntities) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            Span<RuntimeType> types = stackalloc RuntimeType[3];
            types[0] = RuntimeType.Get<T1>();
            types[1] = RuntimeType.Get<T2>();
            types[2] = RuntimeType.Get<T3>();
            bool includeDisabled = (options & Query.Option.IncludeDisabledEntities) == Query.Option.IncludeDisabledEntities;
            foreach (uint hash in ComponentChunks.Keys)
            {
                ComponentChunk chunk = ComponentChunks[hash];
                if (chunk.ContainsTypes(types))
                {
                    UnmanagedList<uint> entities = chunk.Entities;
                    for (uint e = 0; e < entities.Count; e++)
                    {
                        if (includeDisabled)
                        {
                            ref T1 t1 = ref chunk.GetComponentRef<T1>(e);
                            ref T2 t2 = ref chunk.GetComponentRef<T2>(e);
                            ref T3 t3 = ref chunk.GetComponentRef<T3>(e);
                            callback(entities[e], ref t1, ref t2, ref t3);
                        }
                        else
                        {
                            uint entity = entities[e];
                            if (IsEnabled(entity))
                            {
                                ref T1 t1 = ref chunk.GetComponentRef<T1>(e);
                                ref T2 t2 = ref chunk.GetComponentRef<T2>(e);
                                ref T3 t3 = ref chunk.GetComponentRef<T3>(e);
                                callback(entity, ref t1, ref t2, ref t3);
                            }
                        }
                    }
                }
            }
        }

        public readonly void ForEach<T1, T2, T3, T4>(QueryCallback<T1, T2, T3, T4> callback, Query.Option options = Query.Option.IncludeDisabledEntities) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            Span<RuntimeType> types = stackalloc RuntimeType[4];
            types[0] = RuntimeType.Get<T1>();
            types[1] = RuntimeType.Get<T2>();
            types[2] = RuntimeType.Get<T3>();
            types[3] = RuntimeType.Get<T4>();
            bool includeDisabled = (options & Query.Option.IncludeDisabledEntities) == Query.Option.IncludeDisabledEntities;
            foreach (uint hash in ComponentChunks.Keys)
            {
                ComponentChunk chunk = ComponentChunks[hash];
                if (chunk.ContainsTypes(types))
                {
                    UnmanagedList<uint> entities = chunk.Entities;
                    for (uint e = 0; e < entities.Count; e++)
                    {
                        if (includeDisabled)
                        {
                            ref T1 t1 = ref chunk.GetComponentRef<T1>(e);
                            ref T2 t2 = ref chunk.GetComponentRef<T2>(e);
                            ref T3 t3 = ref chunk.GetComponentRef<T3>(e);
                            ref T4 t4 = ref chunk.GetComponentRef<T4>(e);
                            callback(entities[e], ref t1, ref t2, ref t3, ref t4);
                        }
                        else
                        {
                            uint entity = entities[e];
                            if (IsEnabled(entity))
                            {
                                ref T1 t1 = ref chunk.GetComponentRef<T1>(e);
                                ref T2 t2 = ref chunk.GetComponentRef<T2>(e);
                                ref T3 t3 = ref chunk.GetComponentRef<T3>(e);
                                ref T4 t4 = ref chunk.GetComponentRef<T4>(e);
                                callback(entity, ref t1, ref t2, ref t3, ref t4);
                            }
                        }
                    }
                }
            }
        }

        public static World Create()
        {
            return new(UnsafeWorld.Allocate());
        }

        public void CreateListener<T>(object update)
        {
            throw new NotImplementedException();
        }

        public static bool operator ==(World left, World right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(World left, World right)
        {
            return !(left == right);
        }
    }

    public delegate void QueryCallback(in uint id);
    public delegate void QueryCallback<T1>(in uint id, ref T1 t1) where T1 : unmanaged;
    public delegate void QueryCallback<T1, T2>(in uint id, ref T1 t1, ref T2 t2) where T1 : unmanaged where T2 : unmanaged;
    public delegate void QueryCallback<T1, T2, T3>(in uint id, ref T1 t1, ref T2 t2, ref T3 t3) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged;
    public delegate void QueryCallback<T1, T2, T3, T4>(in uint id, ref T1 t1, ref T2 t2, ref T3 t3, ref T4 t4) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged;

    public delegate void EntityCreatedCallback(World world, uint entity);
    public delegate void EntityDestroyedCallback(World world, uint entity);
    public delegate void EntityParentChangedCallback(World world, uint entity, uint parent);
    public delegate void ComponentAddedCallback(World world, uint entity, RuntimeType componentType);
    public delegate void ComponentRemovedCallback(World world, uint entity, RuntimeType componentType);
}
