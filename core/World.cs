using Collections.Generic;
using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Types;
using Unmanaged;
using Worlds.Functions;
using Worlds.Pointers;

namespace Worlds
{
    /// <summary>
    /// Contains arbitrary data sorted into groups of entities for processing.
    /// </summary>
    [SkipLocalsInit]
    public unsafe struct World : IDisposable, IEquatable<World>, ISerializable
    {
#if DEBUG
        internal static readonly System.Collections.Generic.Dictionary<Entity, StackTrace> createStackTraces = new();
#endif
        private const int InitialCapacity = 32;

        /// <summary>
        /// The version of the binary format used to serialize the world.
        /// </summary>
        public const uint Version = 1;

        internal WorldPointer* world;
        internal Schema schema;

        /// <summary>
        /// Native address of the world.
        /// </summary>
        public readonly nint Address => (nint)world;

        /// <summary>
        /// Amount of entities that exist in the world.
        /// </summary>
        public readonly int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                MemoryAddress.ThrowIfDefault(world);

                return world->slots.Count - world->freeEntities.Count - 1;
            }
        }

        /// <summary>
        /// The current maximum amount of referrable entities.
        /// <para>Collections of this size + 1 are guaranteed to
        /// be able to store all entity positions.</para>
        /// </summary>
        public readonly int MaxEntityValue
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                MemoryAddress.ThrowIfDefault(world);

                return world->slots.Count - 1;
            }
        }

        /// <summary>
        /// The maximum most depth of the entity hierarchy.
        /// </summary>
        public readonly int MaxDepth
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                MemoryAddress.ThrowIfDefault(world);

                return world->maxDepth;
            }
        }

        /// <summary>
        /// Checks if the world has been disposed.
        /// </summary>
        public readonly bool IsDisposed => world is null;

        /// <summary>
        /// All previously used entities that are now free.
        /// </summary>
        public readonly ReadOnlySpan<uint> Free
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                MemoryAddress.ThrowIfDefault(world);

                return world->freeEntities.AsSpan();
            }
        }

        /// <summary>
        /// All <see cref="Chunk"/>s in the world.
        /// </summary>
        public readonly ReadOnlySpan<Chunk> Chunks
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                MemoryAddress.ThrowIfDefault(world);

                return world->chunkMap.chunkMap->chunks.AsSpan();
            }
        }

        /// <summary>
        /// The schema containing all component and array types.
        /// </summary>
        public readonly Schema Schema
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                MemoryAddress.ThrowIfDefault(world);

                return schema;
            }
        }

        private readonly Stack<uint> FreeList
        {
            get
            {
                MemoryAddress.ThrowIfDefault(world);

                return world->freeEntities;
            }
        }

        private readonly List<Slot> SlotsList
        {
            get
            {
                MemoryAddress.ThrowIfDefault(world);

                return world->slots;
            }
        }

        /// <summary>
        /// All entities that exist in the world.
        /// </summary>
        public readonly System.Collections.Generic.IEnumerable<uint> Entities
        {
            get
            {
                Stack<uint> free = FreeList;
                List<Slot> slots = SlotsList;
                for (uint e = 1; e < slots.Count; e++)
                {
                    if (!free.Contains(e))
                    {
                        yield return e;
                    }
                }
            }
        }

        /// <summary>
        /// The slots that describe every entity position, including free/destroyed entities.
        /// </summary>
        public readonly ReadOnlySpan<Slot> Slots
        {
            get
            {
                MemoryAddress.ThrowIfDefault(world);

                return world->slots.AsSpan();
            }
        }

        /// <summary>
        /// Indexer for accessing entities by their index.
        /// </summary>
        public readonly uint this[int index]
        {
            get
            {
                MemoryAddress.ThrowIfDefault(world);

                int i = 0;
                int slotCount = world->slots.Count;
                Span<uint> freeEntities = world->freeEntities.AsSpan();
                for (uint e = 1; e < slotCount; e++)
                {
                    if (!freeEntities.Contains(e))
                    {
                        if (i == index)
                        {
                            return e;
                        }

                        i++;
                    }
                }

                throw new IndexOutOfRangeException($"Index {index} is out of range");
            }
        }

#if NET
        /// <summary>
        /// Creates a new world with an empty <see cref="Worlds.Schema"/>.
        /// </summary>
        public World()
        {
            schema = new();
            world = MemoryAddress.AllocatePointer<WorldPointer>();
            world->maxDepth = 0;
            world->schema = schema;
            world->slots = new(InitialCapacity);
            world->slotMetadata = new(InitialCapacity);
            world->arrays = new(InitialCapacity);
            world->freeEntities = new(InitialCapacity);
            world->chunkMap = new(schema.schema);
            world->entityCreatedOrDestroyed = new(4);
            world->entityParentChanged = new(4);
            world->entityDataChanged = new(4);
            world->references = new(InitialCapacity);
            world->entityCreatedOrDestroyedCount = 0;
            world->entityParentChangedCount = 0;
            world->entityDataChangedCount = 0;
            world->flags = default;

            //add reserve values at index 0
            world->slots.AddDefault();
            world->slotMetadata.AddDefault();
            world->arrays.AddDefault();
        }
#endif

        /// <summary>
        /// Creates a new world with the given <paramref name="schema"/>.
        /// </summary>
        public World(Schema schema)
        {
            this.schema = schema;
            world = MemoryAddress.AllocatePointer<WorldPointer>();
            world->maxDepth = 0;
            world->schema = schema;
            world->slots = new(InitialCapacity);
            world->slotMetadata = new(InitialCapacity);
            world->arrays = new(InitialCapacity);
            world->freeEntities = new(InitialCapacity);
            world->chunkMap = new(schema.schema);
            world->entityCreatedOrDestroyed = new(4);
            world->entityParentChanged = new(4);
            world->entityDataChanged = new(4);
            world->references = new(InitialCapacity);
            world->entityCreatedOrDestroyedCount = 0;
            world->entityParentChangedCount = 0;
            world->entityDataChangedCount = 0;
            world->flags = default;

            //add reserve values at index 0
            world->slots.AddDefault();
            world->slotMetadata.AddDefault();
            world->arrays.AddDefault();
        }

        /// <summary>
        /// Disposes everything in the world and the world itself.
        /// </summary>
        public void Dispose()
        {
            MemoryAddress.ThrowIfDefault(world);
            Clear();

            Span<SlotMetadata> slotMetadata = world->slotMetadata.AsSpan();
            Span<Arrays> arrays = world->arrays.AsSpan();
            for (int e = 1; e < slotMetadata.Length; e++)
            {
                if ((slotMetadata[e].flags & SlotFlags.ContainsArrays) != 0)
                {
                    Arrays arraySlot = arrays[e];
                    for (int a = 0; a < BitMask.Capacity; a++)
                    {
                        Values array = arraySlot[a];
                        if (array != default)
                        {
                            array.Dispose();
                        }
                    }
                }
            }

            world->references.Dispose();
            world->entityCreatedOrDestroyed.Dispose();
            world->entityParentChanged.Dispose();
            world->entityDataChanged.Dispose();
            world->schema.Dispose();
            world->freeEntities.Dispose();
            world->chunkMap.Dispose();
            world->slots.Dispose();
            world->slotMetadata.Dispose();
            world->arrays.Dispose();
            MemoryAddress.Free(ref world);
        }

        /// <summary>
        /// Destroys all entities.
        /// </summary>
        public readonly void Clear()
        {
            MemoryAddress.ThrowIfDefault(world);

            world->chunkMap.Clear();
            world->references.Clear();
            world->maxDepth = 0;

            Span<Slot> slots = world->slots.AsSpan();
            Span<SlotMetadata> slotMetadata = world->slotMetadata.AsSpan();
            for (uint e = 1; e < slots.Length; e++)
            {
                ref Slot slot = ref slots[(int)e];
                ref SlotMetadata metadata = ref slotMetadata[(int)e];
                if (metadata.state == SlotState.Free)
                {
                    continue;
                }

                metadata.flags |= SlotFlags.Outdated;
                slot.parent = default;
                slot.chunk = default;
                slot.row = default;
                slot.index = default;
                metadata.referenceRange = default;
                metadata.state = SlotState.Free;
                world->freeEntities.Push(e);
            }
        }

        /// <summary>
        /// Removes all listeners.
        /// </summary>
        public readonly void ClearListeners()
        {
            MemoryAddress.ThrowIfDefault(world);

            world->entityCreatedOrDestroyed.Clear();
            world->entityParentChanged.Clear();
            world->entityDataChanged.Clear();
            world->flags &= ~WorldPointer.Flags.HasEntityCreatedOrDestroyedListeners;
            world->flags &= ~WorldPointer.Flags.HasEntityParentChangedListeners;
            world->flags &= ~WorldPointer.Flags.HasEntityDataChangedListeners;
            world->entityCreatedOrDestroyedCount = 0;
            world->entityParentChangedCount = 0;
            world->entityDataChangedCount = 0;
        }

        /// <summary>
        /// Copies all existing entities to the <paramref name="destination"/> span.
        /// </summary>
        /// <returns>Amount of entities copied.</returns>
        public readonly int CopyEntitiesTo(Span<uint> destination)
        {
            MemoryAddress.ThrowIfDefault(world);

            int count = 0;
            Span<SlotMetadata> slotMetadata = world->slotMetadata.AsSpan();
            for (uint e = 1; e < slotMetadata.Length; e++)
            {
                if (slotMetadata[(int)e].state != SlotState.Free)
                {
                    destination[count++] = e;
                }
            }

            return count;
        }

        /// <summary>
        /// Copies the state of this world's schema to match the given <paramref name="sourceSchema"/>.
        /// </summary>
        public readonly void CopySchemaFrom(Schema sourceSchema)
        {
            MemoryAddress.ThrowIfDefault(world);

            schema.CopyFrom(sourceSchema);
            world->chunkMap.UpdateDefaultChunkStrideToMatchSchema();
        }

        /// <inheritdoc/>
        public readonly override string ToString()
        {
            if (world == default)
            {
                return "World (disposed)";
            }

            return $"World ({Address})";
        }

        /// <summary>
        /// Checks if the given world is equal to this world.
        /// </summary>
        public readonly override bool Equals(object? obj)
        {
            return obj is World world && Equals(world);
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public readonly override int GetHashCode()
        {
            return ((nint)world).GetHashCode();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly void NotifyEntityCreated(uint entity)
        {
            if ((world->flags & WorldPointer.Flags.HasEntityCreatedOrDestroyedListeners) != 0)
            {
                for (int i = 0; i < world->entityCreatedOrDestroyedCount; i++)
                {
                    (EntityCreatedOrDestroyed callback, ulong userData) = world->entityCreatedOrDestroyed[i];
                    callback.Invoke(this, entity, true, userData);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly void NotifyEntityDestroyed(uint entity)
        {
            if ((world->flags & WorldPointer.Flags.HasEntityCreatedOrDestroyedListeners) != 0)
            {
                for (int i = 0; i < world->entityCreatedOrDestroyedCount; i++)
                {
                    (EntityCreatedOrDestroyed callback, ulong userData) = world->entityCreatedOrDestroyed[i];
                    callback.Invoke(this, entity, false, userData);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly void NotifyParentChange(uint entity, uint oldParent, uint newParent)
        {
            if ((world->flags & WorldPointer.Flags.HasEntityParentChangedListeners) != 0)
            {
                for (int i = 0; i < world->entityParentChangedCount; i++)
                {
                    (EntityParentChanged callback, ulong userData) = world->entityParentChanged[i];
                    callback.Invoke(this, entity, oldParent, newParent, userData);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly void NotifyComponentAdded(uint entity, int componentType)
        {
            if ((world->flags & WorldPointer.Flags.HasEntityDataChangedListeners) != 0)
            {
                DataType type = schema.GetComponentDataType(componentType);
                for (int i = 0; i < world->entityDataChangedCount; i++)
                {
                    (EntityDataChanged callback, ulong userData) = world->entityDataChanged[i];
                    callback.Invoke(this, entity, type, true, userData);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly void NotifyComponentsAdded(uint entity, BitMask componentTypes)
        {
            if ((world->flags & WorldPointer.Flags.HasEntityDataChangedListeners) != 0)
            {
                for (int elementIndex = 0; elementIndex < 4; elementIndex++)
                {
                    ulong element = componentTypes.value.GetElement(elementIndex);
                    int baseIndex = elementIndex * 64;
                    while (element != 0)
                    {
                        int trailingZeros = BitOperations.TrailingZeroCount(element);
                        int componentType = baseIndex + trailingZeros;
                        DataType type = schema.GetComponentDataType(componentType);
                        for (int i = 0; i < world->entityDataChangedCount; i++)
                        {
                            (EntityDataChanged callback, ulong userData) = world->entityDataChanged[i];
                            callback.Invoke(this, entity, type, true, userData);
                        }

                        element &= element - 1;
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly void NotifyComponentRemoved(uint entity, int componentType)
        {
            if ((world->flags & WorldPointer.Flags.HasEntityDataChangedListeners) != 0)
            {
                DataType type = schema.GetComponentDataType(componentType);
                for (int i = 0; i < world->entityDataChangedCount; i++)
                {
                    (EntityDataChanged callback, ulong userData) = world->entityDataChanged[i];
                    callback.Invoke(this, entity, type, false, userData);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal readonly void NotifyComponentsRemoved(uint entity, BitMask componentTypes)
        {
            if ((world->flags & WorldPointer.Flags.HasEntityDataChangedListeners) != 0)
            {
                for (int elementIndex = 0; elementIndex < 4; elementIndex++)
                {
                    ulong element = componentTypes.value.GetElement(elementIndex);
                    int baseIndex = elementIndex * 64;
                    while (element != 0)
                    {
                        int trailingZeros = BitOperations.TrailingZeroCount(element);
                        int componentType = baseIndex + trailingZeros;
                        DataType type = schema.GetComponentDataType(componentType);
                        for (int i = 0; i < world->entityDataChangedCount; i++)
                        {
                            (EntityDataChanged callback, ulong userData) = world->entityDataChanged[i];
                            callback.Invoke(this, entity, type, false, userData);
                        }

                        element &= element - 1;
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly void NotifyArrayCreated(uint entity, int arrayType)
        {
            if ((world->flags & WorldPointer.Flags.HasEntityDataChangedListeners) != 0)
            {
                DataType type = schema.GetArrayDataType(arrayType);
                for (int i = 0; i < world->entityDataChangedCount; i++)
                {
                    (EntityDataChanged callback, ulong userData) = world->entityDataChanged[i];
                    callback.Invoke(this, entity, type, true, userData);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly void NotifyArrayDestroyed(uint entity, int arrayType)
        {
            if ((world->flags & WorldPointer.Flags.HasEntityDataChangedListeners) != 0)
            {
                DataType type = schema.GetArrayDataType(arrayType);
                for (int i = 0; i < world->entityDataChangedCount; i++)
                {
                    (EntityDataChanged callback, ulong userData) = world->entityDataChanged[i];
                    callback.Invoke(this, entity, type, false, userData);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly void NotifyTagAdded(uint entity, int tagType)
        {
            if ((world->flags & WorldPointer.Flags.HasEntityDataChangedListeners) != 0)
            {
                DataType type = schema.GetTagDataType(tagType);
                for (int i = 0; i < world->entityDataChangedCount; i++)
                {
                    (EntityDataChanged callback, ulong userData) = world->entityDataChanged[i];
                    callback.Invoke(this, entity, type, true, userData);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly void NotifyTagRemoved(uint entity, int tagType)
        {
            if ((world->flags & WorldPointer.Flags.HasEntityDataChangedListeners) != 0)
            {
                DataType type = schema.GetTagDataType(tagType);
                for (int i = 0; i < world->entityDataChangedCount; i++)
                {
                    (EntityDataChanged callback, ulong userData) = world->entityDataChanged[i];
                    callback.Invoke(this, entity, type, false, userData);
                }
            }
        }

        readonly void ISerializable.Write(ByteWriter writer)
        {
            MemoryAddress.ThrowIfDefault(world);

            writer.WriteValue(new Signature(Version));
            writer.WriteObject(schema);
            writer.WriteValue(Count);
            writer.WriteValue(MaxEntityValue);

            Span<Slot> slots = world->slots.AsSpan();
            Span<SlotMetadata> slotMetadata = world->slotMetadata.AsSpan();
            for (uint e = 1; e < slots.Length; e++)
            {
                ref Slot slot = ref slots[(int)e];
                ref SlotMetadata metadata = ref slotMetadata[(int)e];
                if (metadata.state == SlotState.Free)
                {
                    continue;
                }

                ChunkPointer* chunk = slot.chunk.chunk;
                BitMask componentTypes = chunk->componentTypes;
                BitMask arrayTypes = chunk->arrayTypes;
                BitMask tagTypes = chunk->tagTypes;
                writer.WriteValue(e);
                writer.WriteValue(metadata.state);
                writer.WriteValue(slot.parent);

                //write components
                writer.WriteValue((byte)componentTypes.Count);
                for (int c = 0; c < BitMask.Capacity; c++)
                {
                    if (componentTypes.Contains(c))
                    {
                        writer.WriteValue((byte)c);
                        Span<byte> componentBytes = GetComponentBytes(e, c);
                        writer.WriteSpan(componentBytes);
                    }
                }

                //write arrays
                writer.WriteValue((byte)arrayTypes.Count);
                for (int a = 0; a < BitMask.Capacity; a++)
                {
                    if (arrayTypes.Contains(a))
                    {
                        writer.WriteValue((byte)a);
                        Values array = GetArray(e, a);
                        writer.WriteValue(array.Length);
                        writer.WriteSpan(array.AsSpan());
                    }
                }

                //write tags
                writer.WriteValue((byte)tagTypes.Count);
                for (int t = 0; t < BitMask.Capacity; t++)
                {
                    if (tagTypes.Contains(t))
                    {
                        writer.WriteValue((byte)t);
                    }
                }
            }

            //write references
            for (uint e = 1; e < slots.Length; e++)
            {
                if (slotMetadata[(int)e].state == SlotState.Free)
                {
                    continue;
                }

                ReadOnlySpan<uint> references = GetReferences(e);
                writer.WriteValue(references.Length);
                for (int r = 0; r < references.Length; r++)
                {
                    writer.WriteValue(references[r]);
                }
            }
        }

        void ISerializable.Read(ByteReader reader)
        {
            this = Deserialize(reader);
        }

        /// <summary>
        /// Appends entities from the given <paramref name="sourceWorld"/>.
        /// </summary>
        public readonly void Append(World sourceWorld)
        {
            MemoryAddress.ThrowIfDefault(world);

            World destinationWorld = this;
            Span<Slot> sourceSlots = sourceWorld.world->slots.AsSpan();
            Span<SlotMetadata> sourceSlotMetadata = sourceWorld.world->slotMetadata.AsSpan();
            for (uint e = 1; e < sourceSlots.Length; e++)
            {
                if (sourceSlotMetadata[(int)e].state == SlotState.Free)
                {
                    continue;
                }

                uint destinationEntity = destinationWorld.CreateEntity(sourceSlots[(int)e].chunk.Definition);
                sourceWorld.CopyComponentsTo(e, destinationWorld, destinationEntity);
                sourceWorld.CopyArraysTo(e, destinationWorld, destinationEntity);
                sourceWorld.CopyTagsTo(e, destinationWorld, destinationEntity);
            }
        }

        /// <summary>
        /// Adds a function that listens to whenever an entity is either created, or destroyed.
        /// </summary>
        public readonly void ListenToEntityCreationOrDestruction(EntityCreatedOrDestroyed function, ulong userData = default)
        {
            MemoryAddress.ThrowIfDefault(world);

            world->entityCreatedOrDestroyed.Add((function, userData));
            world->entityCreatedOrDestroyedCount++;
            world->flags |= WorldPointer.Flags.HasEntityCreatedOrDestroyedListeners;
        }

        /// <summary>
        /// Adds a function that listens to when data on an entity changes.
        /// <para>
        /// Components, arrays and tags added or removed.
        /// </para>
        /// </summary>
        public readonly void ListenToEntityDataChanges(EntityDataChanged function, ulong userData = default)
        {
            MemoryAddress.ThrowIfDefault(world);

            world->entityDataChanged.Add((function, userData));
            world->entityDataChangedCount++;
            world->flags |= WorldPointer.Flags.HasEntityDataChangedListeners;
        }

        /// <summary>
        /// Adds a function that listens to when an entity's parent changes.
        /// </summary>
        public readonly void ListenToEntityParentChanges(EntityParentChanged function, ulong userData = default)
        {
            MemoryAddress.ThrowIfDefault(world);

            world->entityParentChanged.Add((function, userData));
            world->entityParentChangedCount++;
            world->flags |= WorldPointer.Flags.HasEntityParentChangedListeners;
        }

        /// <summary>
        /// Destroys the given <paramref name="entity"/> assuming it exists.
        /// </summary>
        public readonly void DestroyEntity(uint entity, bool destroyChildren = true)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            Span<Slot> slots = world->slots.AsSpan();
            ref Slot slot = ref slots[(int)entity];
            ref SlotMetadata metadata = ref world->slotMetadata[(int)entity];
            if (slot.childrenCount > 0)
            {
                if (destroyChildren)
                {
                    //destroy children
                    for (uint childEntity = 1; childEntity < slots.Length; childEntity++)
                    {
                        if (slots[(int)childEntity].parent == entity)
                        {
                            DestroyEntity(childEntity, destroyChildren); //recusive
                        }
                    }
                }
                else
                {
                    //unparent children
                    for (uint childEntity = 1; childEntity < slots.Length; childEntity++)
                    {
                        ref Slot childSlot = ref slots[(int)childEntity];
                        if (childSlot.parent == entity)
                        {
                            childSlot.parent = default;
                        }
                    }
                }
            }

            metadata.flags |= SlotFlags.Outdated;
            metadata.state = SlotState.Free;
            metadata.referenceRange = default;
            slot.depth = 0;

            ref Slot lastSlot = ref slots[(int)slot.chunk.chunk->lastEntity];
            lastSlot.index = slot.index;
            lastSlot.row = slot.chunk.chunk->components[lastSlot.index];

            //remove from parents children list
            ref Slot parentSlot = ref slots[(int)slot.parent]; //it can be 0, which is ok
            parentSlot.childrenCount--;
            slot.chunk.chunk->entities.RemoveAtBySwapping(slot.index);
            slot.chunk.chunk->components.RemoveAtBySwapping(slot.index);
            slot.chunk.chunk->lastEntity = slot.chunk.chunk->entities[--slot.chunk.chunk->count];
            slot.row = default;
            slot.parent = default;
            world->freeEntities.Push(entity);

            NotifyEntityDestroyed(entity);
        }

        /// <summary>
        /// Copies component types from the given <paramref name="entity"/> to the destination <paramref name="destination"/>.
        /// </summary>
        public readonly int CopyComponentTypesTo(uint entity, Span<int> destination)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            return world->slots[entity].chunk.ComponentTypes.CopyTo(destination);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void MoveEntityTo(WorldPointer* world, uint entity, ref Slot currentSlot, Chunk destinationChunk)
        {
            ChunkPointer* sourceChunkPointer = currentSlot.chunk.chunk;
            ChunkPointer* destinationChunkPointer = destinationChunk.chunk;
            int newDestinationIndex = destinationChunkPointer->count + 1;
            if (entity != sourceChunkPointer->lastEntity)
            {
                // because the move operation swaps with last element,
                // we update the previous last slot to match the resulting state
                ref Slot previousLastSlot = ref world->slots[(int)sourceChunkPointer->lastEntity];
                previousLastSlot.index = currentSlot.index;
                previousLastSlot.row = sourceChunkPointer->components[currentSlot.index];
            }

            int newSourceCount = sourceChunkPointer->count - 1;
            sourceChunkPointer->entities.RemoveAtBySwapping(currentSlot.index);
            sourceChunkPointer->count = newSourceCount;
            sourceChunkPointer->lastEntity = sourceChunkPointer->entities[newSourceCount];
            sourceChunkPointer->version++;
            sourceChunkPointer->components.RemoveAtBySwappingAndAdd(currentSlot.index, destinationChunkPointer->components, out currentSlot.row, out bool capacityIncreased);
            if (capacityIncreased)
            {
                // because capacity increased, old references to the last row are now desynced and need to be updated
                // this is preferred over marking chunks as dirty, in order to avoid speed cost at runtime
                Span<Slot> slots = world->slots.AsSpan();
                Span<uint> destinationEntities = destinationChunkPointer->entities.AsSpan();
                for (int i = 1; i < destinationEntities.Length; i++)
                {
                    slots[(int)destinationEntities[i]].row = destinationChunkPointer->components[i];
                }
            }

            currentSlot.index = newDestinationIndex;
            currentSlot.chunk = destinationChunk;
            destinationChunkPointer->entities.Add(entity);
            destinationChunkPointer->lastEntity = entity;
            destinationChunkPointer->count = newDestinationIndex;
            destinationChunkPointer->version++;
        }

        /// <summary>
        /// Checks if the given <paramref name="entity"/> is enabled with respect
        /// to ancestor entities.
        /// </summary>
        public readonly bool IsEnabled(uint entity)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            return world->slotMetadata[entity].state == SlotState.Enabled;
        }

        /// <summary>
        /// Checks if the given entity is enabled regardless
        /// of it's parents.
        /// </summary>
        public readonly bool IsLocallyEnabled(uint entity)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            SlotState state = world->slotMetadata[entity].state;
            return state == SlotState.Enabled || state == SlotState.DisabledButLocallyEnabled;
        }

        /// <summary>
        /// Assigns the enabled state of the given <paramref name="entity"/>
        /// and its descendants to the given <paramref name="enabled"/>.
        /// </summary>
        public readonly void SetEnabled(uint entity, bool enabled)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            Span<Slot> slots = world->slots.AsSpan();
            Span<SlotMetadata> slotMetadata = world->slotMetadata.AsSpan();
            ref Slot entitySlot = ref slots[(int)entity];
            ref SlotMetadata entityMetadata = ref slotMetadata[(int)entity];
            uint parent = entitySlot.parent;
            if (parent != default)
            {
                SlotState parentState = slotMetadata[(int)parent].state;
                if (parentState == SlotState.Disabled || parentState == SlotState.DisabledButLocallyEnabled)
                {
                    entityMetadata.state = enabled ? SlotState.DisabledButLocallyEnabled : SlotState.Disabled;
                }
                else
                {
                    entityMetadata.state = enabled ? SlotState.Enabled : SlotState.Disabled;
                }
            }
            else
            {
                entityMetadata.state = enabled ? SlotState.Enabled : SlotState.Disabled;
            }

            //move to different chunk
            Chunk previousChunk = entitySlot.chunk;
            Definition previousDefinition = previousChunk.Definition;
            ulong currentD = previousDefinition.tagTypes.value.GetElement(3);
            bool oldEnabled = (currentD & Schema.DisabledMask) == 0;
            bool newEnabled = entityMetadata.state == SlotState.Enabled;
            if (oldEnabled != newEnabled)
            {
                Definition newDefinition = previousDefinition;
                ulong enabledMask = (ulong)-(long)(newEnabled ? 1 : 0);
                ulong newD = (currentD & ~Schema.DisabledMask) | (Schema.DisabledMask & ~enabledMask);
                newDefinition.tagTypes.value = newDefinition.tagTypes.value.WithElement(3, newD);

                if (entity != entitySlot.chunk.chunk->lastEntity)
                {
                    ref Slot lastSlot = ref slots[(int)entitySlot.chunk.chunk->lastEntity];
                    lastSlot.index = entitySlot.index;
                    lastSlot.row = entitySlot.chunk.chunk->components[lastSlot.index];
                }

                MoveEntityTo(world, entity, ref entitySlot, world->chunkMap.GetOrCreate(newDefinition));
            }

            //modify descendants
            if ((entityMetadata.flags & SlotFlags.ContainsChildren) != 0)
            {
                //todo: this temporary allocation can be avoided by tracking how large the tree is
                //and then using stackalloc
                using Stack<uint> stack = new(4);
                PushChildrenToStack(slots, stack, entity);

                while (stack.Count > 0)
                {
                    uint currentEntity = stack.Pop();
                    ref Slot currentSlot = ref slots[(int)currentEntity];
                    ref SlotMetadata currentMetadata = ref slotMetadata[(int)currentEntity];
                    if (enabled && currentMetadata.state == SlotState.DisabledButLocallyEnabled)
                    {
                        currentMetadata.state = SlotState.Enabled;
                    }
                    else if (!enabled && currentMetadata.state == SlotState.Enabled)
                    {
                        currentMetadata.state = SlotState.DisabledButLocallyEnabled;
                    }

                    //move descentant to proper chunk
                    previousChunk = currentSlot.chunk;
                    previousDefinition = previousChunk.Definition;
                    currentD = previousDefinition.tagTypes.value.GetElement(3);
                    oldEnabled = (currentD & Schema.DisabledMask) == 0;
                    if (oldEnabled != enabled)
                    {
                        Definition newDefinition = previousDefinition;
                        ulong enabledMask = (ulong)-(long)(enabled ? 1 : 0);
                        ulong newD = (currentD & ~Schema.DisabledMask) | (Schema.DisabledMask & ~enabledMask);
                        newDefinition.tagTypes.value = newDefinition.tagTypes.value.WithElement(3, newD);

                        if (currentEntity != currentSlot.chunk.chunk->lastEntity)
                        {
                            ref Slot lastSlot = ref slots[(int)currentSlot.chunk.chunk->lastEntity];
                            lastSlot.index = currentSlot.index;
                            lastSlot.row = currentSlot.chunk.chunk->components[lastSlot.index];
                        }

                        MoveEntityTo(world, currentEntity, ref currentSlot, world->chunkMap.GetOrCreate(newDefinition));
                    }

                    //check through children
                    if ((currentMetadata.flags & SlotFlags.ContainsChildren) != 0 && (currentMetadata.flags & SlotFlags.ChildrenOutdated) == 0)
                    {
                        PushChildrenToStack(slots, stack, currentEntity);
                    }
                }

                static void PushChildrenToStack(Span<Slot> slots, Stack<uint> stack, uint entity)
                {
                    for (uint childEntity = 1; childEntity < slots.Length; childEntity++)
                    {
                        ref Slot childSlot = ref slots[(int)childEntity];
                        if (childSlot.parent == entity)
                        {
                            stack.Push(childEntity);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Retrieves the entity that would be created next.
        /// </summary>
        public readonly uint GetNextCreatedEntity(int fastForward = 0)
        {
            MemoryAddress.ThrowIfDefault(world);

            ReadOnlySpan<uint> freeEntities = world->freeEntities.AsSpan();
            if (freeEntities.Length > fastForward)
            {
                return freeEntities[freeEntities.Length - 1 - fastForward];
            }

            fastForward -= freeEntities.Length;
            Span<Slot> slots = world->slots.AsSpan();
            return (uint)(slots.Length + fastForward);
        }

        /// <summary>
        /// Creates a new entity.
        /// </summary>
        public readonly uint CreateEntity()
        {
            MemoryAddress.ThrowIfDefault(world);

            if (!world->freeEntities.TryPop(out uint entity))
            {
                entity = (uint)world->slots.Count;
                world->slots.AddDefault();
                world->slotMetadata.AddDefault();
                world->arrays.AddDefault();
            }

            ref Slot slot = ref world->slots[entity];
            ref SlotMetadata metadata = ref world->slotMetadata[entity];
            metadata.state = SlotState.Enabled;
            slot.chunk = world->chunkMap.chunkMap->defaultChunk;
            slot.index = slot.chunk.chunk->count + 1;
            slot.chunk.chunk->entities.Add(entity);
            slot.chunk.chunk->lastEntity = entity;
            slot.chunk.chunk->components.AddDefault(out slot.row);
            slot.chunk.chunk->count = slot.index;
            slot.chunk.chunk->version++;
            TraceCreation(entity);
            NotifyEntityCreated(entity);
            return entity;
        }

        /// <summary>
        /// Creates a new entity with the given <paramref name="componentTypes"/>.
        /// </summary>
        public readonly uint CreateEntity(BitMask componentTypes)
        {
            MemoryAddress.ThrowIfDefault(world);

            if (!world->freeEntities.TryPop(out uint entity))
            {
                entity = (uint)world->slots.Count;
                world->slots.AddDefault();
                world->slotMetadata.AddDefault();
                world->arrays.AddDefault();
            }

            Definition definition = new(componentTypes, BitMask.Default, BitMask.Default);
            ref Slot slot = ref world->slots[entity];
            ref SlotMetadata metadata = ref world->slotMetadata[entity];
            metadata.state = SlotState.Enabled;
            slot.chunk = world->chunkMap.GetOrCreate(definition);
            slot.index = slot.chunk.chunk->count + 1;
            slot.chunk.chunk->entities.Add(entity);
            slot.chunk.chunk->lastEntity = entity;
            slot.chunk.chunk->components.AddDefault(out slot.row);
            slot.chunk.chunk->count = slot.index;
            slot.chunk.chunk->version++;
            TraceCreation(entity);
            NotifyEntityCreated(entity);
            return entity;
        }

        /// <summary>
        /// Creates a new entity with the given <paramref name="definition"/>.
        /// </summary>
        public readonly uint CreateEntity(Definition definition)
        {
            MemoryAddress.ThrowIfDefault(world);

            if (!world->freeEntities.TryPop(out uint entity))
            {
                entity = (uint)world->slots.Count;
                world->slots.AddDefault();
                world->slotMetadata.AddDefault();
                world->arrays.AddDefault();
            }

            ref Slot slot = ref world->slots[entity];
            ref SlotMetadata metadata = ref world->slotMetadata[entity];
            metadata.state = SlotState.Enabled;
            slot.chunk = world->chunkMap.GetOrCreate(definition);

            //create arrays if necessary
            BitMask arrayTypes = definition.arrayTypes;
            if (arrayTypes != BitMask.Default)
            {
                ref Arrays arrays = ref world->arrays[entity];
                for (int vectorIndex = 0; vectorIndex < 4; vectorIndex++)
                {
                    ulong element = arrayTypes.value.GetElement(vectorIndex);
                    while (element != 0)
                    {
                        int trailingZeros = BitOperations.TrailingZeroCount(element);
                        int a = vectorIndex * 64 + trailingZeros;
                        int arrayElementSize = schema.GetArraySize(a);
                        arrays[a] = new(0, arrayElementSize);
                        element &= element - 1;
                    }
                }

                metadata.flags |= SlotFlags.ContainsArrays;
            }

            slot.index = slot.chunk.chunk->count + 1;
            slot.chunk.chunk->entities.Add(entity);
            slot.chunk.chunk->lastEntity = entity;
            slot.chunk.chunk->components.AddDefault(out slot.row);
            slot.chunk.chunk->count = slot.index;
            slot.chunk.chunk->version++;
            TraceCreation(entity);
            NotifyEntityCreated(entity);
            return entity;
        }

        /// <summary>
        /// Creates a new entity with the given <paramref name="definition"/>.
        /// </summary>
        public readonly uint CreateEntity(Definition definition, out Chunk.Row row)
        {
            MemoryAddress.ThrowIfDefault(world);

            if (!world->freeEntities.TryPop(out uint entity))
            {
                entity = (uint)world->slots.Count;
                world->slots.AddDefault();
                world->slotMetadata.AddDefault();
                world->arrays.AddDefault();
            }

            ref Slot slot = ref world->slots[entity];
            ref SlotMetadata metadata = ref world->slotMetadata[entity];
            metadata.state = SlotState.Enabled;
            slot.chunk = world->chunkMap.GetOrCreate(definition);

            //create arrays if necessary
            BitMask arrayTypes = definition.arrayTypes;
            if (arrayTypes != BitMask.Default)
            {
                ref Arrays arrays = ref world->arrays[entity];
                for (int vectorIndex = 0; vectorIndex < 4; vectorIndex++)
                {
                    ulong element = arrayTypes.value.GetElement(vectorIndex);
                    while (element != 0)
                    {
                        int trailingZeros = BitOperations.TrailingZeroCount(element);
                        int a = vectorIndex * 64 + trailingZeros;
                        int arrayElementSize = schema.GetArraySize(a);
                        arrays[a] = new(0, arrayElementSize);
                        element &= element - 1;
                    }
                }

                metadata.flags |= SlotFlags.ContainsArrays;
            }

            slot.index = slot.chunk.chunk->count + 1;
            slot.chunk.chunk->entities.Add(entity);
            slot.chunk.chunk->lastEntity = entity;
            slot.chunk.chunk->components.AddDefault(out slot.row);
            slot.chunk.chunk->count = slot.index;
            slot.chunk.chunk->version++;
            TraceCreation(entity);
            NotifyEntityCreated(entity);
            row = new(schema.schema->componentOffsets, schema.schema->sizes, slot.row);
            return entity;
        }

        /// <summary>
        /// Creates a new entity.
        /// </summary>
        public readonly uint CreateEntity(BitMask componentTypes, out Chunk.Row row, BitMask tagTypes = default)
        {
            MemoryAddress.ThrowIfDefault(world);

            if (!world->freeEntities.TryPop(out uint entity))
            {
                entity = (uint)world->slots.Count;
                world->slots.AddDefault();
                world->slotMetadata.AddDefault();
                world->arrays.AddDefault();
            }

            Definition definition = new(componentTypes, BitMask.Default, tagTypes);
            ref Slot slot = ref world->slots[entity];
            ref SlotMetadata metadata = ref world->slotMetadata[entity];
            metadata.state = SlotState.Enabled;
            slot.chunk = world->chunkMap.GetOrCreate(definition);
            slot.index = slot.chunk.chunk->count + 1;
            slot.chunk.chunk->entities.Add(entity);
            slot.chunk.chunk->lastEntity = entity;
            slot.chunk.chunk->components.AddDefault(out slot.row);
            slot.chunk.chunk->count = slot.index;
            slot.chunk.chunk->version++;
            TraceCreation(entity);
            NotifyEntityCreated(entity);
            row = new(schema.schema->componentOffsets, schema.schema->sizes, slot.row);
            return entity;
        }

        /// <summary>
        /// Checks if the given <paramref name="entity"/> is compliant with the 
        /// definition of the <paramref name="archetype"/>.
        /// </summary>
        public readonly bool Is(uint entity, Archetype archetype)
        {
            return Is(entity, archetype.definition);
        }

        /// <summary>
        /// Checks if the given <paramref name="entity"/> is compliant with the
        /// <paramref name="definition"/>.
        /// </summary>
        public readonly bool Is(uint entity, Definition definition)
        {
            ThrowIfEntityIsMissing(entity);

            Chunk chunk = world->slots[entity].chunk;
            if (!chunk.ComponentTypes.ContainsAll(definition.componentTypes))
            {
                return false;
            }

            if (!chunk.ArrayTypes.ContainsAll(definition.arrayTypes))
            {
                return false;
            }

            return chunk.TagTypes.ContainsAll(definition.tagTypes);
        }

        /// <summary>
        /// Makes the given <paramref name="entity"/> become what the 
        /// <paramref name="definition"/> argues by adding the missing components, arrays
        /// and tags.
        /// </summary>
        public readonly void Become(uint entity, Definition definition)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            Definition currentDefinition = world->slots[entity].chunk.Definition;
            BitMask missingComponentTypes = definition.componentTypes & ~currentDefinition.componentTypes;
            BitMask missingArrayTypes = definition.arrayTypes & ~currentDefinition.arrayTypes;
            BitMask missingTagTypes = definition.tagTypes & ~currentDefinition.tagTypes;
            for (int i = 0; i < BitMask.Capacity; i++)
            {
                if (missingComponentTypes.Contains(i))
                {
                    AddComponentType(entity, i);
                }

                if (missingArrayTypes.Contains(i))
                {
                    CreateArray(entity, i);
                }

                if (missingTagTypes.Contains(i))
                {
                    AddTag(entity, i);
                }
            }
        }

        /// <summary>
        /// Makes the given <paramref name="entity"/> become what the <paramref name="archetype"/>
        /// argues by adding the missing components, arrays and tags.
        /// </summary>
        public readonly void Become(uint entity, Archetype archetype)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            Definition currentDefinition = world->slots[entity].chunk.Definition;
            BitMask missingComponentTypes = archetype.definition.componentTypes & ~currentDefinition.componentTypes;
            BitMask missingArrayTypes = archetype.definition.arrayTypes & ~currentDefinition.arrayTypes;
            BitMask missingTagTypes = archetype.definition.tagTypes & ~currentDefinition.tagTypes;
            for (int i = 0; i < BitMask.Capacity; i++)
            {
                if (missingComponentTypes.Contains(i))
                {
                    AddComponentType(entity, i);
                }

                if (missingArrayTypes.Contains(i))
                {
                    CreateArray(entity, i);
                }

                if (missingTagTypes.Contains(i))
                {
                    AddTag(entity, i);
                }
            }
        }

        /// <summary>
        /// Creates entities to fill the given <paramref name="destination"/>.
        /// </summary>
        public readonly void CreateEntities(Span<uint> destination)
        {
            MemoryAddress.ThrowIfDefault(world);

            int freeEntities = world->freeEntities.Count;
            int newEntities = destination.Length - freeEntities;
            int startIndex = world->slots.Count;
            if (newEntities > 0)
            {
                world->slots.AddDefault(newEntities);
                world->slotMetadata.AddDefault(newEntities);
                world->arrays.AddDefault(newEntities);
            }

            Chunk defaultChunk = world->chunkMap.chunkMap->defaultChunk;
            int created = 0;
            Span<Slot> slots = world->slots.AsSpan();
            Span<SlotMetadata> slotMetadata = world->slotMetadata.AsSpan();
            for (int i = 0; i < freeEntities; i++)
            {
                uint entity = world->freeEntities.Pop();
                ref Slot slot = ref slots[(int)entity];
                slotMetadata[(int)entity].state = SlotState.Enabled;
                slot.chunk = defaultChunk;
                slot.index = slot.chunk.chunk->count + 1;
                slot.chunk.chunk->entities.Add(entity);
                slot.chunk.chunk->lastEntity = entity;
                slot.chunk.chunk->components.AddDefault(out slot.row);
                slot.chunk.chunk->count = slot.index;
                slot.chunk.chunk->version++;
                TraceCreation(entity);
                NotifyEntityCreated(entity);
                destination[created++] = entity;
            }

            for (int i = 0; i < newEntities; i++)
            {
                uint entity = (uint)(i + startIndex);
                ref Slot slot = ref slots[(int)entity];
                slotMetadata[(int)entity].state = SlotState.Enabled;
                slot.chunk = defaultChunk;
                slot.index = slot.chunk.chunk->count + 1;
                slot.chunk.chunk->entities.Add(entity);
                slot.chunk.chunk->lastEntity = entity;
                slot.chunk.chunk->components.AddDefault(out slot.row);
                slot.chunk.chunk->count = slot.index;
                slot.chunk.chunk->version++;
                TraceCreation(entity);
                NotifyEntityCreated(entity);
                destination[created++] = entity;
            }
        }

        /// <summary>
        /// Creates entities to fill the given <paramref name="destination"/>,
        /// with the given <paramref name="definition"/>.
        /// </summary>
        public readonly void CreateEntities(Span<uint> destination, Definition definition)
        {
            MemoryAddress.ThrowIfDefault(world);

            int freeEntities = world->freeEntities.Count;
            int newEntities = destination.Length - freeEntities;
            int startIndex = world->slots.Count;
            if (newEntities > 0)
            {
                world->slots.AddDefault(newEntities);
                world->slotMetadata.AddDefault(newEntities);
                world->arrays.AddDefault(newEntities);
            }

            Chunk chunk = world->chunkMap.GetOrCreate(definition);
            int created = 0;
            Span<Slot> slots = world->slots.AsSpan();
            Span<SlotMetadata> slotMetadata = world->slotMetadata.AsSpan();
            for (int i = 0; i < freeEntities; i++)
            {
                uint entity = world->freeEntities.Pop();
                ref Slot slot = ref slots[(int)entity];
                slotMetadata[(int)entity].state = SlotState.Enabled;
                slot.chunk = chunk;
                slot.index = slot.chunk.chunk->count + 1;
                slot.chunk.chunk->entities.Add(entity);
                slot.chunk.chunk->lastEntity = entity;
                slot.chunk.chunk->components.AddDefault(out slot.row);
                slot.chunk.chunk->count = slot.index;
                slot.chunk.chunk->version++;
                TraceCreation(entity);
                NotifyEntityCreated(entity);
                destination[created++] = entity;
            }

            for (int i = 0; i < newEntities; i++)
            {
                uint entity = (uint)(i + startIndex);
                ref Slot slot = ref slots[(int)entity];
                slotMetadata[(int)entity].state = SlotState.Enabled;
                slot.chunk = chunk;
                slot.index = slot.chunk.chunk->count + 1;
                slot.chunk.chunk->entities.Add(entity);
                slot.chunk.chunk->lastEntity = entity;
                slot.chunk.chunk->components.AddDefault(out slot.row);
                slot.chunk.chunk->count = slot.index;
                slot.chunk.chunk->version++;
                TraceCreation(entity);
                NotifyEntityCreated(entity);
                destination[created++] = entity;
            }
        }

        /// <summary>
        /// Checks if the given <paramref name="entity"/> exists and is valid in this world.
        /// </summary>
        public readonly bool ContainsEntity(uint entity)
        {
            MemoryAddress.ThrowIfDefault(world);

            if (entity >= world->slots.Count)
            {
                return false;
            }

            return world->slotMetadata[entity].state != SlotState.Free;
        }

        /// <summary>
        /// Retrieves the parent of the given entity, <see langword="default"/> if none
        /// is assigned.
        /// </summary>
        public readonly uint GetParent(uint entity)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            return world->slots[entity].parent;
        }

        /// <summary>
        /// How deep the given <paramref name="entity"/> is in the hierarchy.
        /// </summary>
        public readonly int GetDepth(uint entity)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            return world->slots[entity].depth;
        }

        /// <summary>
        /// Assigns the given <paramref name="newParent"/> to the given <paramref name="entity"/>.
        /// <para>
        /// If the given <paramref name="newParent"/> isn't valid, it will be set to <see langword="default"/>.
        /// </para>
        /// </summary>
        /// <returns><see langword="true"/> if parent changed.</returns>
        public readonly bool SetParent(uint entity, uint newParent)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfParentIsSameAsChild(entity, newParent);

            if (!ContainsEntity(newParent))
            {
                newParent = default;
            }

            Span<Slot> slots = world->slots.AsSpan();
            Span<SlotMetadata> slotMetadata = world->slotMetadata.AsSpan();
            ref Slot entitySlot = ref slots[(int)entity];
            ref SlotMetadata entityMetadata = ref slotMetadata[(int)entity];
            bool parentChanged = entitySlot.parent != newParent;
            if (parentChanged)
            {
                uint oldParent = entitySlot.parent;
                entitySlot.parent = newParent;

                //remove from previous parent children list
                slots[(int)oldParent].childrenCount--; //old parent can be 0, which is ok

                ref Slot newParentSlot = ref slots[(int)newParent];
                ref SlotMetadata newParentMetadata = ref slotMetadata[(int)newParent];
                if ((newParentMetadata.flags & SlotFlags.ContainsChildren) == 0)
                {
                    newParentSlot.childrenCount = 0;
                    newParentMetadata.flags |= SlotFlags.ContainsChildren;
                    newParentMetadata.flags &= ~SlotFlags.ChildrenOutdated;
                }
                else if ((newParentMetadata.flags & SlotFlags.ChildrenOutdated) != 0)
                {
                    newParentSlot.childrenCount = 0;
                    newParentMetadata.flags &= ~SlotFlags.ChildrenOutdated;
                }

                newParentSlot.childrenCount++;

                //assign the depth to this entity, and its descendants
                entitySlot.depth = newParentSlot.depth + 1;
                UpdateDepthOfChildren(entity, slots, slotMetadata, entitySlot.depth);

                //update state if parent is disabled
                if (entityMetadata.state == SlotState.Enabled)
                {
                    if (newParentMetadata.state == SlotState.Disabled || newParentMetadata.state == SlotState.DisabledButLocallyEnabled)
                    {
                        entityMetadata.state = SlotState.DisabledButLocallyEnabled;
                    }
                }

                //move to different chunk if disabled state changed
                Chunk previousChunk = entitySlot.chunk;
                Definition previousDefinition = previousChunk.Definition;
                ulong currentD = previousDefinition.tagTypes.value.GetElement(3);
                bool oldEnabled = (currentD & Schema.DisabledMask) == 0;
                bool newEnabled = entityMetadata.state == SlotState.Enabled;
                if (oldEnabled != newEnabled)
                {
                    Definition newDefinition = previousDefinition;
                    ulong enabledMask = (ulong)-(long)(newEnabled ? 1 : 0);
                    ulong newD = (currentD & ~Schema.DisabledMask) | (Schema.DisabledMask & ~enabledMask);
                    newDefinition.tagTypes.value = newDefinition.tagTypes.value.WithElement(3, newD);
                    MoveEntityTo(world, entity, ref entitySlot, world->chunkMap.GetOrCreate(newDefinition));
                }

                NotifyParentChange(entity, oldParent, newParent);
            }

            return parentChanged;
        }

        private readonly void UpdateDepthOfChildren(uint entity, Span<Slot> slots, Span<SlotMetadata> slotMetadata, int depth)
        {
            //calculate the max depth of the world
            if (depth > world->maxDepth)
            {
                world->maxDepth = depth;
            }

            for (uint childEntity = 1; childEntity < slots.Length; childEntity++)
            {
                ref Slot childSlot = ref slots[(int)childEntity];
                if (childSlot.parent == entity)
                {
                    childSlot.depth = depth + 1;

                    //calculate the max depth of the world
                    if (childSlot.depth > world->maxDepth)
                    {
                        world->maxDepth = childSlot.depth;
                    }

                    ref SlotMetadata childMetadata = ref slotMetadata[(int)childEntity];
                    if ((childMetadata.flags & SlotFlags.ContainsChildren) != 0 && (childMetadata.flags & SlotFlags.ChildrenOutdated) == 0)
                    {
                        UpdateDepthOfChildren(childEntity, slots, slotMetadata, depth + 1);
                    }
                }
            }
        }

        /// <summary>
        /// Retrieves all children of the <paramref name="entity"/> entity
        /// and writes them to the <paramref name="children"/> span.
        /// </summary>
        /// <returns>How many children there are.</returns>
        public readonly int CopyChildrenTo(uint entity, Span<uint> children)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            int count = 0;
            Span<Slot> slots = world->slots.AsSpan();
            for (uint e = 1; e < slots.Length; e++)
            {
                if (slots[(int)e].parent == entity)
                {
                    children[count++] = e;
                }
            }

            return count;
        }

        /// <summary>
        /// Retrieves all entities referenced by <paramref name="entity"/>.
        /// </summary>
        public readonly ReadOnlySpan<uint> GetReferences(uint entity)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            ref SlotMetadata metadata = ref world->slotMetadata[entity];
            int start = (int)(metadata.referenceRange & 0xFFFFFFFF);
            int count = (int)(metadata.referenceRange >> 32);
            return world->references.AsSpan(start, count);
        }

        /// <summary>
        /// Retrieves the number of children the given <paramref name="entity"/> has.
        /// </summary>
        public readonly int GetChildCount(uint entity)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            return world->slots[entity].childrenCount;
        }

        /// <summary>
        /// Adds a new reference to the given entity.
        /// </summary>
        /// <returns>An index offset by 1 that refers to this entity.</returns>
        public readonly rint AddReference(uint entity, uint referencedEntity)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfEntityIsMissing(referencedEntity);

            List<uint> references = world->references;
            Span<SlotMetadata> slotMetadata = world->slotMetadata.AsSpan();
            ref SlotMetadata metadata = ref slotMetadata[(int)entity];
            int start = (int)(metadata.referenceRange & 0xFFFFFFFF);
            int count = (int)(metadata.referenceRange >> 32);
            if (count == 0)
            {
                start = references.Count;
                references.Add(referencedEntity);
            }
            else
            {
                references.Insert(start + count, referencedEntity);

                //shift all other ranges over by 1
                for (int e = 1; e < slotMetadata.Length; e++)
                {
                    ref SlotMetadata currentMetadata = ref slotMetadata[e];
                    int currentStart = (int)(currentMetadata.referenceRange & 0xFFFFFFFF);
                    int currentCount = (int)(currentMetadata.referenceRange >> 32);
                    if (currentStart > start)
                    {
                        currentStart++;
                        currentMetadata.referenceRange = (uint)currentStart | ((ulong)currentCount << 32);
                    }
                }
            }

            count++;
            metadata.referenceRange = (uint)start | ((ulong)count << 32);
            return new(count);
        }

        /// <summary>
        /// Updates an existing <paramref name="reference"/> to point towards the <paramref name="referencedEntity"/>.
        /// </summary>
        public readonly void SetReference(uint entity, rint reference, uint referencedEntity)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfReferenceIsMissing(entity, reference);

            int start = (int)(world->slotMetadata[entity].referenceRange & 0xFFFFFFFF);
            world->references[start + reference.value - 1] = referencedEntity;
        }

        /// <summary>
        /// Checks if the given entity contains a reference to the given <paramref name="referencedEntity"/>.
        /// </summary>
        public readonly bool ContainsReference(uint entity, uint referencedEntity)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            ref SlotMetadata metadata = ref world->slotMetadata[entity];
            int start = (int)(metadata.referenceRange & 0xFFFFFFFF);
            int count = (int)(metadata.referenceRange >> 32);
            ReadOnlySpan<uint> references = world->references.AsSpan(start, count);
            return references.Contains(referencedEntity);
        }

        /// <summary>
        /// Checks if the given entity contains the given local <paramref name="reference"/>.
        /// </summary>
        public readonly bool ContainsReference(uint entity, rint reference)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            unchecked
            {
                int count = (int)(world->slotMetadata[entity].referenceRange >> 32);
                return (uint)count >= ((uint)reference.value - 1);
            }
        }

        /// <summary>
        /// Retrieves the number of references the given <paramref name="entity"/> has.
        /// </summary>
        public readonly int GetReferenceCount(uint entity)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            return (int)(world->slotMetadata[entity].referenceRange >> 32);
        }

        /// <summary>
        /// Retrieves the entity referenced at the given <paramref name="reference"/> index by <paramref name="entity"/>.
        /// </summary>
        public readonly uint GetReference(uint entity, rint reference)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfReferenceIsMissing(entity, reference);

            return world->references[(int)(world->slotMetadata[entity].referenceRange & 0xFFFFFFFF) + reference.value - 1];
        }

        /// <summary>
        /// Retrieves the <see cref="rint"/> value that points to the given <paramref name="referencedEntity"/> on <paramref name="entity"/>.
        /// </summary>
        public readonly rint GetReference(uint entity, uint referencedEntity)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfReferencedEntityIsMissing(entity, referencedEntity);

            ref SlotMetadata metadata = ref world->slotMetadata[entity];
            int start = (int)(metadata.referenceRange & 0xFFFFFFFF);
            int count = (int)(metadata.referenceRange >> 32);
            Span<uint> references = world->references.AsSpan(start, count);
            return new(references.IndexOf(referencedEntity) + 1);
        }

        /// <summary>
        /// Attempts to retrieve the referenced entity at the given <paramref name="reference"/> on <paramref name="entity"/>.
        /// </summary>
        public readonly bool TryGetReference(uint entity, rint reference, out uint referencedEntity)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            ref SlotMetadata metadata = ref world->slotMetadata[entity];
            int start = (int)(metadata.referenceRange & 0xFFFFFFFF);
            int count = (int)(metadata.referenceRange >> 32);
            if (count < reference.value)
            {
                referencedEntity = default;
                return false;
            }
            else
            {
                referencedEntity = world->references[(uint)(start + reference.value - 1)];
                return true;
            }
        }

        /// <summary>
        /// Removes the reference at the given <paramref name="reference"/> index on <paramref name="entity"/>.
        /// </summary>
        /// <returns>The other entity that was being referenced.</returns>
        public readonly void RemoveReference(uint entity, rint reference)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfReferenceIsMissing(entity, reference);

            List<uint> references = world->references;
            Span<SlotMetadata> slotMetadata = world->slotMetadata.AsSpan();
            ref SlotMetadata metadata = ref slotMetadata[(int)entity];
            int start = (int)(metadata.referenceRange & 0xFFFFFFFF);
            int count = (int)(metadata.referenceRange >> 32);
            count--;
            references.RemoveAt(start + reference.value - 1);

            //shift all other ranges back by 1
            for (int e = 1; e < slotMetadata.Length; e++)
            {
                ref SlotMetadata currentMetadata = ref slotMetadata[e];
                int currentStart = (int)(currentMetadata.referenceRange & 0xFFFFFFFF);
                int currentCount = (int)(currentMetadata.referenceRange >> 32);
                if (currentStart > start)
                {
                    currentStart--;
                    currentMetadata.referenceRange = (uint)currentStart | ((ulong)currentCount << 32);
                }
            }

            metadata.referenceRange = (uint)start | ((ulong)count << 32);
        }

        /// <summary>
        /// Removes the reference at the given <paramref name="reference"/> index on <paramref name="entity"/>.
        /// </summary>
        /// <returns>The other entity that was being referenced.</returns>
        public readonly void RemoveReference(uint entity, rint reference, out uint referencedEntity)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfReferenceIsMissing(entity, reference);

            List<uint> references = world->references;
            Span<SlotMetadata> slotMetadata = world->slotMetadata.AsSpan();
            ref SlotMetadata metadata = ref slotMetadata[(int)entity];
            int start = (int)(metadata.referenceRange & 0xFFFFFFFF);
            int count = (int)(metadata.referenceRange >> 32);
            int index = start + reference.value - 1;
            count--;
            referencedEntity = references[index];
            references.RemoveAt(index);

            //shift all other ranges back by 1
            for (int e = 1; e < slotMetadata.Length; e++)
            {
                ref SlotMetadata currentMetadata = ref slotMetadata[e];
                int currentStart = (int)(currentMetadata.referenceRange & 0xFFFFFFFF);
                int currentCount = (int)(currentMetadata.referenceRange >> 32);
                if (currentStart > start)
                {
                    currentStart--;
                    currentMetadata.referenceRange = (uint)currentStart | ((ulong)currentCount << 32);
                }
            }

            metadata.referenceRange = (uint)start | ((ulong)count << 32);
        }

        /// <summary>
        /// Removes the <paramref name="referencedEntity"/> from <paramref name="entity"/>.
        /// </summary>
        /// <returns>The reference that was removed.</returns>
        public readonly void RemoveReference(uint entity, uint referencedEntity)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfReferencedEntityIsMissing(entity, referencedEntity);

            List<uint> references = world->references;
            Span<SlotMetadata> slotMetadata = world->slotMetadata.AsSpan();
            ref SlotMetadata metadata = ref slotMetadata[(int)entity];
            int start = (int)(metadata.referenceRange & 0xFFFFFFFF);
            int count = (int)(metadata.referenceRange >> 32);
            Span<uint> referenceSpan = world->references.AsSpan(start, count);
            count--;
            references.RemoveAt(referenceSpan.IndexOf(referencedEntity));

            //shift all other ranges back by 1
            for (int e = 1; e < slotMetadata.Length; e++)
            {
                ref SlotMetadata currentMetadata = ref slotMetadata[e];
                int currentStart = (int)(currentMetadata.referenceRange & 0xFFFFFFFF);
                int currentCount = (int)(currentMetadata.referenceRange >> 32);
                if (currentStart > start)
                {
                    currentStart--;
                    currentMetadata.referenceRange = (uint)currentStart | ((ulong)currentCount << 32);
                }
            }

            metadata.referenceRange = (uint)start | ((ulong)count << 32);
        }

        /// <summary>
        /// Removes the <paramref name="referencedEntity"/> from <paramref name="entity"/>.
        /// </summary>
        /// <returns>The reference that was removed.</returns>
        public readonly void RemoveReference(uint entity, uint referencedEntity, out rint removedReference)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfReferencedEntityIsMissing(entity, referencedEntity);

            List<uint> references = world->references;
            Span<SlotMetadata> slotMetadata = world->slotMetadata.AsSpan();
            ref SlotMetadata metadata = ref slotMetadata[(int)entity];
            int start = (int)(metadata.referenceRange & 0xFFFFFFFF);
            int count = (int)(metadata.referenceRange >> 32);
            Span<uint> referenceSpan = world->references.AsSpan(start, count);
            int index = referenceSpan.IndexOf(referencedEntity);
            count--;
            references.RemoveAt(index);

            //shift all other ranges back by 1
            for (int e = 1; e < slotMetadata.Length; e++)
            {
                ref SlotMetadata currentMetadata = ref slotMetadata[e];
                int currentStart = (int)(currentMetadata.referenceRange & 0xFFFFFFFF);
                int currentCount = (int)(currentMetadata.referenceRange >> 32);
                if (currentStart > start)
                {
                    currentStart--;
                    currentMetadata.referenceRange = (uint)currentStart | ((ulong)currentCount << 32);
                }
            }

            metadata.referenceRange = (uint)start | ((ulong)count << 32);
            removedReference = new(index + 1);
        }

        /// <summary>
        /// Checks if the given <paramref name="entity"/> contains a tag of type <typeparamref name="T"/>.
        /// </summary>
        public readonly bool ContainsTag<T>(uint entity) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            return world->slots[entity].chunk.TagTypes.Contains(schema.GetTagType<T>());
        }

        /// <summary>
        /// Checks if the given <paramref name="entity"/> contains the given <paramref name="tagType"/>.
        /// </summary>
        public readonly bool ContainsTag(uint entity, int tagType)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            return world->slots[entity].chunk.TagTypes.Contains(tagType);
        }

        /// <summary>
        /// Adds a tag of type <typeparamref name="T"/> to the given <paramref name="entity"/>.
        /// </summary>
        public readonly void AddTag<T>(uint entity) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            int tagType = schema.GetTagType<T>();
            ThrowIfTagAlreadyPresent(entity, tagType);

            Span<Slot> slots = world->slots.AsSpan();
            ref Slot slot = ref slots[(int)entity];
            MoveEntityTo(world, entity, ref slot, world->chunkMap.GetOrCreateWithAddedTag(slot.chunk, tagType));
            NotifyTagAdded(entity, tagType);
        }

        /// <summary>
        /// Adds the <paramref name="tagType"/> to the given <paramref name="entity"/>.
        /// </summary>
        public readonly void AddTag(uint entity, int tagType)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfTagAlreadyPresent(entity, tagType);

            Span<Slot> slots = world->slots.AsSpan();
            ref Slot slot = ref slots[(int)entity];
            MoveEntityTo(world, entity, ref slot, world->chunkMap.GetOrCreateWithAddedTag(slot.chunk, tagType));
            NotifyTagAdded(entity, tagType);
        }

        /// <summary>
        /// Removes the <typeparamref name="T"/> tag from the <paramref name="entity"/>.
        /// </summary>
        public readonly void RemoveTag<T>(uint entity) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            int tagType = schema.GetTagType<T>();
            ThrowIfTagIsMissing(entity, tagType);

            Span<Slot> slots = world->slots.AsSpan();
            ref Slot slot = ref slots[(int)entity];
            MoveEntityTo(world, entity, ref slot, world->chunkMap.GetOrCreateWithRemovedTag(slot.chunk, tagType));
            NotifyTagRemoved(entity, tagType);
        }

        /// <summary>
        /// Removes the <paramref name="tagType"/> from the given <paramref name="entity"/>.
        /// </summary>
        public readonly void RemoveTag(uint entity, int tagType)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfTagIsMissing(entity, tagType);

            Span<Slot> slots = world->slots.AsSpan();
            ref Slot slot = ref slots[(int)entity];
            MoveEntityTo(world, entity, ref slot, world->chunkMap.GetOrCreateWithRemovedComponent(slot.chunk, tagType));
            NotifyTagRemoved(entity, tagType);
        }

        /// <summary>
        /// Retrieves the types of all arrays on this entity.
        /// </summary>
        public readonly BitMask GetArrayTypes(uint entity)
        {
            ThrowIfEntityIsMissing(entity);

            return world->slots[entity].chunk.ArrayTypes;
        }

        /// <summary>
        /// Retrieves the types of all tags on this entity.
        /// </summary>
        public readonly BitMask GetTagTypes(uint entity)
        {
            ThrowIfEntityIsMissing(entity);

            return world->slots[entity].chunk.TagTypes;
        }

        /// <summary>
        /// Creates a new empty array with the given <paramref name="length"/> and <paramref name="arrayType"/>.
        /// </summary>
        public readonly Values CreateArray(uint entity, int arrayType, int length = 0)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfArrayIsAlreadyPresent(entity, arrayType);

            Span<Slot> slots = world->slots.AsSpan();
            int stride = schema.GetArraySize(arrayType);
            ref Slot slot = ref slots[(int)entity];
            ref SlotMetadata metadata = ref world->slotMetadata[entity];
            ref Arrays arrays = ref world->arrays[entity];

            if ((metadata.flags & SlotFlags.ContainsArrays) == 0)
            {
                metadata.flags |= SlotFlags.ContainsArrays;
                metadata.flags &= ~SlotFlags.ArraysOutdated;
            }
            else if ((metadata.flags & SlotFlags.ArraysOutdated) != 0)
            {
                metadata.flags &= ~SlotFlags.ArraysOutdated;
                for (int a = 0; a < BitMask.Capacity; a++)
                {
                    Values array = arrays[a];
                    if (array != default)
                    {
                        array.Dispose();
                        arrays[a] = default;
                    }
                }
            }

            MoveEntityTo(world, entity, ref slot, world->chunkMap.GetOrCreateWithAddedArray(slot.chunk, arrayType));
            Values newArray = new(length, stride);
            arrays[arrayType] = newArray;
            NotifyArrayCreated(entity, arrayType);
            return newArray;
        }

        /// <summary>
        /// Creates a new empty array with the given <paramref name="length"/> and <paramref name="arrayType"/>.
        /// </summary>
        public readonly Values CreateArray(uint entity, int arrayType, int stride, int length = 0)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfArrayIsAlreadyPresent(entity, arrayType);

            Span<Slot> slots = world->slots.AsSpan();
            ref Slot slot = ref slots[(int)entity];
            ref SlotMetadata metadata = ref world->slotMetadata[entity];
            ref Arrays arrays = ref world->arrays[entity];

            if ((metadata.flags & SlotFlags.ContainsArrays) == 0)
            {
                metadata.flags |= SlotFlags.ContainsArrays;
                metadata.flags &= ~SlotFlags.ArraysOutdated;
            }
            else if ((metadata.flags & SlotFlags.ArraysOutdated) != 0)
            {
                metadata.flags &= ~SlotFlags.ArraysOutdated;
                for (int a = 0; a < BitMask.Capacity; a++)
                {
                    Values array = arrays[a];
                    if (array != default)
                    {
                        array.Dispose();
                        arrays[a] = default;
                    }
                }
            }

            MoveEntityTo(world, entity, ref slot, world->chunkMap.GetOrCreateWithAddedArray(slot.chunk, arrayType));
            Values newArray = new(length, stride);
            arrays[arrayType] = newArray;
            NotifyArrayCreated(entity, arrayType);
            return newArray;
        }

        /// <summary>
        /// Creates a new empty array with the given <paramref name="length"/> and <paramref name="dataType"/>.
        /// </summary>
        public readonly Values CreateArray(uint entity, DataType dataType, int length = 0)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfArrayIsAlreadyPresent(entity, dataType.index);

            Span<Slot> slots = world->slots.AsSpan();
            ref Slot slot = ref slots[(int)entity];
            ref SlotMetadata metadata = ref world->slotMetadata[entity];
            ref Arrays arrays = ref world->arrays[entity];

            if ((metadata.flags & SlotFlags.ContainsArrays) == 0)
            {
                metadata.flags |= SlotFlags.ContainsArrays;
                metadata.flags &= ~SlotFlags.ArraysOutdated;
            }
            else if ((metadata.flags & SlotFlags.ArraysOutdated) != 0)
            {
                metadata.flags &= ~SlotFlags.ArraysOutdated;
                for (int a = 0; a < BitMask.Capacity; a++)
                {
                    Values array = arrays[a];
                    if (array != default)
                    {
                        array.Dispose();
                        arrays[a] = default;
                    }
                }
            }

            MoveEntityTo(world, entity, ref slot, world->chunkMap.GetOrCreateWithAddedArray(slot.chunk, dataType.index));
            Values newArray = new(length, dataType.size);
            arrays[dataType.index] = newArray;
            NotifyArrayCreated(entity, dataType.index);
            return newArray;
        }

        /// <summary>
        /// Creates a new empty array on this <paramref name="entity"/>.
        /// </summary>
        public readonly Values<T> CreateArray<T>(uint entity, int length = 0) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            int arrayType = schema.GetArrayType<T>();
            ThrowIfArrayIsAlreadyPresent(entity, arrayType);

            Span<Slot> slots = world->slots.AsSpan();
            ref Slot slot = ref slots[(int)entity];
            ref SlotMetadata metadata = ref world->slotMetadata[entity];
            ref Arrays arrays = ref world->arrays[entity];

            if ((metadata.flags & SlotFlags.ContainsArrays) == 0)
            {
                metadata.flags |= SlotFlags.ContainsArrays;
                metadata.flags &= ~SlotFlags.ArraysOutdated;
            }
            else if ((metadata.flags & SlotFlags.ArraysOutdated) != 0)
            {
                metadata.flags &= ~SlotFlags.ArraysOutdated;
                for (int a = 0; a < BitMask.Capacity; a++)
                {
                    Values array = arrays[a];
                    if (array != default)
                    {
                        array.Dispose();
                        arrays[a] = default;
                    }
                }
            }

            MoveEntityTo(world, entity, ref slot, world->chunkMap.GetOrCreateWithAddedArray(slot.chunk, arrayType));
            Values<T> newArray = new(length);
            arrays[arrayType] = newArray;
            NotifyArrayCreated(entity, arrayType);
            return newArray;
        }

        /// <summary>
        /// Creates a new empty array on this <paramref name="entity"/>.
        /// </summary>
        public readonly Values<T> CreateArray<T>(uint entity, int arrayType, int length = 0) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfArrayIsAlreadyPresent(entity, arrayType);

            Span<Slot> slots = world->slots.AsSpan();
            ref Slot slot = ref slots[(int)entity];
            ref SlotMetadata metadata = ref world->slotMetadata[entity];
            ref Arrays arrays = ref world->arrays[entity];

            if ((metadata.flags & SlotFlags.ContainsArrays) == 0)
            {
                metadata.flags |= SlotFlags.ContainsArrays;
                metadata.flags &= ~SlotFlags.ArraysOutdated;
            }
            else if ((metadata.flags & SlotFlags.ArraysOutdated) != 0)
            {
                metadata.flags &= ~SlotFlags.ArraysOutdated;
                for (int a = 0; a < BitMask.Capacity; a++)
                {
                    Values array = arrays[a];
                    if (array != default)
                    {
                        array.Dispose();
                        arrays[a] = default;
                    }
                }
            }

            MoveEntityTo(world, entity, ref slot, world->chunkMap.GetOrCreateWithAddedArray(slot.chunk, arrayType));
            Values<T> newArray = new(length);
            arrays[arrayType] = newArray;
            NotifyArrayCreated(entity, arrayType);
            return newArray;
        }

        /// <summary>
        /// Creates a new array containing the given <paramref name="values"/>.
        /// </summary>
        public readonly void CreateArray<T>(uint entity, ReadOnlySpan<T> values) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            int arrayType = schema.GetArrayType<T>();
            ThrowIfArrayIsAlreadyPresent(entity, arrayType);

            Span<Slot> slots = world->slots.AsSpan();
            ref Slot slot = ref slots[(int)entity];
            ref SlotMetadata metadata = ref world->slotMetadata[entity];
            ref Arrays arrays = ref world->arrays[entity];

            if ((metadata.flags & SlotFlags.ContainsArrays) == 0)
            {
                metadata.flags |= SlotFlags.ContainsArrays;
                metadata.flags &= ~SlotFlags.ArraysOutdated;
            }
            else if ((metadata.flags & SlotFlags.ArraysOutdated) != 0)
            {
                metadata.flags &= ~SlotFlags.ArraysOutdated;
                for (int a = 0; a < BitMask.Capacity; a++)
                {
                    Values array = arrays[a];
                    if (array != default)
                    {
                        array.Dispose();
                        arrays[a] = default;
                    }
                }
            }

            MoveEntityTo(world, entity, ref slot, world->chunkMap.GetOrCreateWithAddedArray(slot.chunk, arrayType));
            arrays[arrayType] = new Values<T>(values);
            NotifyArrayCreated(entity, arrayType);
        }

        /// <summary>
        /// Creates a new array containing the given <paramref name="values"/>.
        /// </summary>
        public readonly void CreateArray<T>(uint entity, int arrayType, ReadOnlySpan<T> values) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfArrayIsAlreadyPresent(entity, arrayType);

            Span<Slot> slots = world->slots.AsSpan();
            ref Slot slot = ref slots[(int)entity];
            ref SlotMetadata metadata = ref world->slotMetadata[entity];
            ref Arrays arrays = ref world->arrays[entity];

            if ((metadata.flags & SlotFlags.ContainsArrays) == 0)
            {
                metadata.flags |= SlotFlags.ContainsArrays;
                metadata.flags &= ~SlotFlags.ArraysOutdated;
            }
            else if ((metadata.flags & SlotFlags.ArraysOutdated) != 0)
            {
                metadata.flags &= ~SlotFlags.ArraysOutdated;
                for (int a = 0; a < BitMask.Capacity; a++)
                {
                    Values array = arrays[a];
                    if (array != default)
                    {
                        array.Dispose();
                        arrays[a] = default;
                    }
                }
            }

            MoveEntityTo(world, entity, ref slot, world->chunkMap.GetOrCreateWithAddedArray(slot.chunk, arrayType));
            arrays[arrayType] = new Values<T>(values);
            NotifyArrayCreated(entity, arrayType);
        }

        /// <summary>
        /// Creates a new array containing the given <paramref name="values"/>.
        /// </summary>
        public readonly void CreateArray<T>(uint entity, Span<T> values) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            int arrayType = schema.GetArrayType<T>();
            ThrowIfArrayIsAlreadyPresent(entity, arrayType);

            Span<Slot> slots = world->slots.AsSpan();
            ref Slot slot = ref slots[(int)entity];
            ref SlotMetadata metadata = ref world->slotMetadata[entity];
            ref Arrays arrays = ref world->arrays[entity];

            if ((metadata.flags & SlotFlags.ContainsArrays) == 0)
            {
                metadata.flags |= SlotFlags.ContainsArrays;
                metadata.flags &= ~SlotFlags.ArraysOutdated;
            }
            else if ((metadata.flags & SlotFlags.ArraysOutdated) != 0)
            {
                metadata.flags &= ~SlotFlags.ArraysOutdated;
                for (int a = 0; a < BitMask.Capacity; a++)
                {
                    Values array = arrays[a];
                    if (array != default)
                    {
                        array.Dispose();
                        arrays[a] = default;
                    }
                }
            }

            MoveEntityTo(world, entity, ref slot, world->chunkMap.GetOrCreateWithAddedArray(slot.chunk, arrayType));
            arrays[arrayType] = new Values<T>(values);
            NotifyArrayCreated(entity, arrayType);
        }

        /// <summary>
        /// Creates a new array containing the given <paramref name="values"/>.
        /// </summary>
        public readonly void CreateArray<T>(uint entity, int arrayType, Span<T> values) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfArrayIsAlreadyPresent(entity, arrayType);

            Span<Slot> slots = world->slots.AsSpan();
            ref Slot slot = ref slots[(int)entity];
            ref SlotMetadata metadata = ref world->slotMetadata[entity];
            ref Arrays arrays = ref world->arrays[entity];

            if ((metadata.flags & SlotFlags.ContainsArrays) == 0)
            {
                metadata.flags |= SlotFlags.ContainsArrays;
                metadata.flags &= ~SlotFlags.ArraysOutdated;
            }
            else if ((metadata.flags & SlotFlags.ArraysOutdated) != 0)
            {
                metadata.flags &= ~SlotFlags.ArraysOutdated;
                for (int a = 0; a < BitMask.Capacity; a++)
                {
                    Values array = arrays[a];
                    if (array != default)
                    {
                        array.Dispose();
                        arrays[a] = default;
                    }
                }
            }

            MoveEntityTo(world, entity, ref slot, world->chunkMap.GetOrCreateWithAddedArray(slot.chunk, arrayType));
            arrays[arrayType] = new Values<T>(values);
            NotifyArrayCreated(entity, arrayType);
        }

        /// <summary>
        /// Checks if the given <paramref name="entity"/> contains an array of type <typeparamref name="T"/>.
        /// </summary>
        public readonly bool ContainsArray<T>(uint entity) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            return world->slots[entity].chunk.ArrayTypes.Contains(schema.GetArrayType<T>());
        }

        /// <summary>
        /// Checks if the given <paramref name="entity"/> contains an array of the given <paramref name="arrayType"/>.
        /// </summary>
        public readonly bool ContainsArray(uint entity, int arrayType)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            return world->slots[entity].chunk.ArrayTypes.Contains(arrayType);
        }

        /// <summary>
        /// Retrieves the array of type <typeparamref name="T"/> from the given <paramref name="entity"/>.
        /// </summary>
        public readonly Values<T> GetArray<T>(uint entity) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            int arrayType = schema.GetArrayType<T>();
            ThrowIfArrayIsMissing(entity, arrayType);

            Values<T> array = new(world->arrays[entity][arrayType].array);
            array.ThrowIfSizeMismatch();
            return array;
        }

        /// <summary>
        /// Retrieves the array of type <typeparamref name="T"/> from the given <paramref name="entity"/>.
        /// </summary>
        public readonly Span<T> GetArrayOrDefault<T>(uint entity) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            int arrayType = schema.GetArrayType<T>();
            Collections.Pointers.ArrayPointer* pointer = world->arrays[entity][arrayType].array;
            if (pointer == default)
            {
                return default;
            }
            else
            {
                Span<T> array = new(pointer->items.pointer, pointer->length);
                Values<T>.ThrowIfSizeMismatch(pointer->stride);
                return array;
            }
        }

        /// <summary>
        /// Retrieves the array of type <typeparamref name="T"/> from the given <paramref name="entity"/>.
        /// </summary>
        public readonly Values<T> GetArray<T>(uint entity, int arrayType) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfArrayIsMissing(entity, arrayType);

            Values<T> array = new(world->arrays[entity][arrayType].array);
            array.ThrowIfSizeMismatch();
            return array;
        }

        /// <summary>
        /// Retrieves the array of type <typeparamref name="T"/> from the given <paramref name="entity"/>.
        /// </summary>
        public readonly Span<T> GetArrayOrDefault<T>(uint entity, int arrayType) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            Collections.Pointers.ArrayPointer* pointer = world->arrays[entity][arrayType].array;
            if (pointer == default)
            {
                return default;
            }
            else
            {
                Span<T> array = new(pointer->items.pointer, pointer->length);
                Values<T>.ThrowIfSizeMismatch(pointer->stride);
                return array;
            }
        }

        /// <summary>
        /// Retrieves the array of the given <paramref name="arrayType"/> from the given <paramref name="entity"/>.
        /// </summary>
        public readonly Values GetArray(uint entity, int arrayType)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfArrayIsMissing(entity, arrayType);

            return world->arrays[entity][arrayType];
        }

        /// <summary>
        /// Retrieves the array of the given <paramref name="arrayType"/> from the given <paramref name="entity"/>.
        /// <para>
        /// If one does not exist, a <see langword="default"/> array is returned.
        /// </para>
        /// </summary>
        public readonly Values GetArrayOrDefault(uint entity, int arrayType)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            return world->arrays[entity][arrayType];
        }

        /// <summary>
        /// Attempts to retrieve an array of type <typeparamref name="T"/> from the given <paramref name="entity"/>.
        /// </summary>
        public readonly bool TryGetArray<T>(uint entity, out Values<T> array) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            int arrayType = schema.GetArrayType<T>();
            if (world->slots[entity].chunk.ArrayTypes.Contains(arrayType))
            {
                array = new(world->arrays[entity][arrayType].array);
                return true;
            }
            else
            {
                array = default;
                return false;
            }
        }

        /// <summary>
        /// Attempts to retrieve an array of type <typeparamref name="T"/> from the given <paramref name="entity"/>.
        /// </summary>
        public readonly bool TryGetArray<T>(uint entity, int arrayType, out Values<T> array) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            if (world->slots[entity].chunk.ArrayTypes.Contains(arrayType))
            {
                array = new(world->arrays[entity][arrayType].array);
                return true;
            }
            else
            {
                array = default;
                return false;
            }
        }

        /// <summary>
        /// Retrieves the element at the index from an existing array on this entity.
        /// </summary>
        public readonly ref T GetArrayElement<T>(uint entity, int index) where T : unmanaged
        {
            ThrowIfEntityIsMissing(entity);

            int arrayType = schema.GetArrayType<T>();
            ThrowIfArrayIsMissing(entity, arrayType);

            return ref world->arrays[entity][arrayType].Get<T>(index);
        }

        /// <summary>
        /// Retrieves the element at the index from an existing array on this entity.
        /// </summary>
        public readonly ref T GetArrayElement<T>(uint entity, int arrayType, int index) where T : unmanaged
        {
            ThrowIfEntityIsMissing(entity);
            ThrowIfArrayIsMissing(entity, arrayType);

            return ref world->arrays[entity][arrayType].Get<T>(index);
        }

        /// <summary>
        /// Retrieves the length of an existing array on this entity.
        /// </summary>
        public readonly int GetArrayLength<T>(uint entity) where T : unmanaged
        {
            ThrowIfEntityIsMissing(entity);

            int arrayType = schema.GetArrayType<T>();
            ThrowIfArrayIsMissing(entity, arrayType);

            return world->arrays[entity][arrayType].Length;
        }

        /// <summary>
        /// Retrieves the length of an existing array on this entity.
        /// </summary>
        public readonly int GetArrayLength(uint entity, int arrayType)
        {
            ThrowIfEntityIsMissing(entity);
            ThrowIfArrayIsMissing(entity, arrayType);

            return world->arrays[entity][arrayType].Length;
        }

        /// <summary>
        /// Adds the given <paramref name="element"/> to the end of an existing array on this <paramref name="entity"/>.
        /// </summary>
        public readonly void AddArrayElement<T>(uint entity, T element) where T : unmanaged
        {
            ThrowIfEntityIsMissing(entity);

            int arrayType = schema.GetArrayType<T>();
            ThrowIfArrayIsMissing(entity, arrayType);

            world->arrays[entity][arrayType].Add(element);
        }

        /// <summary>
        /// Adds the given <paramref name="element"/> to the end of an existing array on this <paramref name="entity"/>.
        /// </summary>
        public readonly void AddArrayElement<T>(uint entity, int arrayType, T element) where T : unmanaged
        {
            ThrowIfEntityIsMissing(entity);
            ThrowIfArrayIsMissing(entity, arrayType);

            world->arrays[entity][arrayType].Add(element);
        }

        /// <summary>
        /// Adds a <see langword="default"/> element to the end of an existing array on this <paramref name="entity"/>.
        /// </summary>
        public readonly ref T AddArrayElement<T>(uint entity) where T : unmanaged
        {
            ThrowIfEntityIsMissing(entity);

            int arrayType = schema.GetArrayType<T>();
            ThrowIfArrayIsMissing(entity, arrayType);

            return ref world->arrays[entity][arrayType].Add<T>();
        }

        /// <summary>
        /// Adds a <see langword="default"/> element to the end of an existing array on this <paramref name="entity"/>.
        /// </summary>
        public readonly ref T AddArrayElement<T>(uint entity, int arrayType) where T : unmanaged
        {
            ThrowIfEntityIsMissing(entity);
            ThrowIfArrayIsMissing(entity, arrayType);

            return ref world->arrays[entity][arrayType].Add<T>();
        }

        /// <summary>
        /// Clears all elements from an existing array on this <paramref name="entity"/>.
        /// </summary>
        public readonly void ClearArray<T>(uint entity) where T : unmanaged
        {
            ThrowIfEntityIsMissing(entity);

            int arrayType = schema.GetArrayType<T>();
            ThrowIfArrayIsMissing(entity, arrayType);

            world->arrays[entity][arrayType].Clear();
        }

        /// <summary>
        /// Clears all elements from an existing array on this <paramref name="entity"/>.
        /// </summary>
        public readonly void ClearArray(uint entity, int arrayType)
        {
            ThrowIfEntityIsMissing(entity);
            ThrowIfArrayIsMissing(entity, arrayType);

            world->arrays[entity][arrayType].Clear();
        }

        /// <summary>
        /// Destroys the array of type <typeparamref name="T"/> on the given <paramref name="entity"/>.
        /// </summary>
        public readonly void DestroyArray<T>(uint entity) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            int arrayType = schema.GetArrayType<T>();
            ThrowIfArrayIsMissing(entity, arrayType);

            Span<Slot> slots = world->slots.AsSpan();
            ref Slot slot = ref slots[(int)entity];
            ref Arrays arrays = ref world->arrays[entity];
            Values array = arrays[arrayType];
            array.Dispose();
            arrays[arrayType] = default;
            MoveEntityTo(world, entity, ref slot, world->chunkMap.GetOrCreateWithRemovedArray(slot.chunk, arrayType));
            NotifyArrayDestroyed(entity, arrayType);
        }

        /// <summary>
        /// Destroys the array of the given <paramref name="arrayType"/> on the given <paramref name="entity"/>.
        /// </summary>
        public readonly void DestroyArray(uint entity, int arrayType)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfArrayIsMissing(entity, arrayType);

            Span<Slot> slots = world->slots.AsSpan();
            ref Slot slot = ref slots[(int)entity];
            ref Arrays arrays = ref world->arrays[entity];
            Values array = arrays[arrayType];
            array.Dispose();
            arrays[arrayType] = default;
            MoveEntityTo(world, entity, ref slot, world->chunkMap.GetOrCreateWithRemovedArray(slot.chunk, arrayType));
            NotifyArrayDestroyed(entity, arrayType);
        }

        /// <summary>
        /// Adds a new <typeparamref name="T"/> component to the given <paramref name="entity"/>.
        /// </summary>
        public readonly void AddComponent<T>(uint entity, T component) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            int componentType = schema.GetComponentType<T>();
            ThrowIfComponentAlreadyPresent(entity, componentType);

            Span<Slot> slots = world->slots.AsSpan();
            ref Slot slot = ref slots[(int)entity];
            MoveEntityTo(world, entity, ref slot, world->chunkMap.GetOrCreateWithAddedComponent(slot.chunk, componentType));
            *(T*)(slot.row.pointer + schema.schema->componentOffsets[(uint)componentType]) = component;
            NotifyComponentAdded(entity, componentType);
        }

        /// <summary>
        /// Adds a new <typeparamref name="T"/> component to the given <paramref name="entity"/>.
        /// </summary>
        public readonly void AddComponent<T>(uint entity, int componentType, T component) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfComponentAlreadyPresent(entity, componentType);

            Span<Slot> slots = world->slots.AsSpan();
            ref Slot slot = ref slots[(int)entity];
            MoveEntityTo(world, entity, ref slot, world->chunkMap.GetOrCreateWithAddedComponent(slot.chunk, componentType));
            *(T*)(slot.row.pointer + schema.schema->componentOffsets[(uint)componentType]) = component;
            NotifyComponentAdded(entity, componentType);
        }

        /// <summary>
        /// Adds a <typeparamref name="T"/> component with <see langword="default"/> memory to <paramref name="entity"/>,
        /// and returns it by reference.
        /// </summary>
        public readonly ref T AddComponent<T>(uint entity) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            int componentType = schema.GetComponentType<T>();
            ThrowIfComponentAlreadyPresent(entity, componentType);

            Span<Slot> slots = world->slots.AsSpan();
            ref Slot slot = ref slots[(int)entity];
            MoveEntityTo(world, entity, ref slot, world->chunkMap.GetOrCreateWithAddedComponent(slot.chunk, componentType));
            NotifyComponentAdded(entity, componentType);
            return ref *(T*)(slot.row.pointer + schema.schema->componentOffsets[(uint)componentType]);
        }

        /// <summary>
        /// Adds a <typeparamref name="T"/> component with <see langword="default"/> memory to <paramref name="entity"/>,
        /// and returns it by reference.
        /// </summary>
        public readonly ref T AddComponent<T>(uint entity, int componentType) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfComponentAlreadyPresent(entity, componentType);

            Span<Slot> slots = world->slots.AsSpan();
            ref Slot slot = ref slots[(int)entity];
            MoveEntityTo(world, entity, ref slot, world->chunkMap.GetOrCreateWithAddedComponent(slot.chunk, componentType));
            NotifyComponentAdded(entity, componentType);
            return ref *(T*)(slot.row.pointer + schema.schema->componentOffsets[(uint)componentType]);
        }

        /// <summary>
        /// Adds a new <see langword="default"/> component of type <paramref name="componentType"/>.
        /// </summary>
        public readonly void AddComponentType(uint entity, int componentType)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfComponentAlreadyPresent(entity, componentType);

            Span<Slot> slots = world->slots.AsSpan();
            ref Slot slot = ref slots[(int)entity];
            MoveEntityTo(world, entity, ref slot, world->chunkMap.GetOrCreateWithAddedComponent(slot.chunk, componentType));
            NotifyComponentAdded(entity, componentType);
        }

        /// <summary>
        /// Adds a new <see langword="default"/> component of type <paramref name="componentType"/>.
        /// </summary>
        public readonly void AddComponentType(uint entity, int componentType, out Chunk.Row newRow)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfComponentAlreadyPresent(entity, componentType);

            Span<Slot> slots = world->slots.AsSpan();
            ref Slot slot = ref slots[(int)entity];
            MoveEntityTo(world, entity, ref slot, world->chunkMap.GetOrCreateWithAddedComponent(slot.chunk, componentType));
            newRow = new(schema.schema->componentOffsets, schema.schema->sizes, slot.row);
            NotifyComponentAdded(entity, componentType);
        }

        /// <summary>
        /// Adds a <typeparamref name="T"/> component with <see langword="default"/> memory to <paramref name="entity"/>.
        /// </summary>
        public readonly void AddComponentType<T>(uint entity) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            int componentType = schema.GetComponentType<T>();
            ThrowIfComponentAlreadyPresent(entity, componentType);

            Span<Slot> slots = world->slots.AsSpan();
            ref Slot slot = ref slots[(int)entity];
            MoveEntityTo(world, entity, ref slot, world->chunkMap.GetOrCreateWithAddedComponent(slot.chunk, componentType));
            NotifyComponentAdded(entity, componentType);
        }

        /// <summary>
        /// Adds <see langword="default"/> instances of the given <paramref name="componentTypes"/>.
        /// </summary>
        public readonly void AddComponentTypes(uint entity, BitMask componentTypes)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfComponentsAlreadyPresent(entity, componentTypes);

            Span<Slot> slots = world->slots.AsSpan();
            ref Slot slot = ref slots[(int)entity];
            MoveEntityTo(world, entity, ref slot, world->chunkMap.GetOrCreateWithAddedComponents(slot.chunk, componentTypes));
            NotifyComponentsAdded(entity, componentTypes);
        }

        /// <summary>
        /// Adds <see langword="default"/> instances of the given <paramref name="componentTypes"/>.
        /// </summary>
        public readonly void AddComponentTypes(uint entity, BitMask componentTypes, out Chunk.Row newRow)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfComponentsAlreadyPresent(entity, componentTypes);

            Span<Slot> slots = world->slots.AsSpan();
            ref Slot slot = ref slots[(int)entity];
            MoveEntityTo(world, entity, ref slot, world->chunkMap.GetOrCreateWithAddedComponents(slot.chunk, componentTypes));
            NotifyComponentsAdded(entity, componentTypes);
            newRow = new(schema.schema->componentOffsets, schema.schema->sizes, slot.row);
        }

        /// <summary>
        /// Adds a new <see langword="default"/> component of type <paramref name="componentType"/>.
        /// </summary>
        public readonly void AddComponent(uint entity, int componentType, out MemoryAddress component)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfComponentAlreadyPresent(entity, componentType);

            Span<Slot> slots = world->slots.AsSpan();
            ref Slot slot = ref slots[(int)entity];
            MoveEntityTo(world, entity, ref slot, world->chunkMap.GetOrCreateWithAddedComponent(slot.chunk, componentType));
            NotifyComponentAdded(entity, componentType);
            component = new(slot.row.pointer + schema.schema->componentOffsets[(uint)componentType]);
        }

        /// <summary>
        /// Adds a new default component with the given type.
        /// </summary>
        public readonly MemoryAddress AddComponent(uint entity, int componentType, out int componentSize)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfComponentAlreadyPresent(entity, componentType);

            Span<Slot> slots = world->slots.AsSpan();
            ref Slot slot = ref slots[(int)entity];
            MoveEntityTo(world, entity, ref slot, world->chunkMap.GetOrCreateWithAddedComponent(slot.chunk, componentType));
            NotifyComponentAdded(entity, componentType);
            componentSize = schema.schema->sizes[(uint)componentType];
            return new(slot.row.pointer + schema.schema->componentOffsets[(uint)componentType]);
        }

        /// <summary>
        /// Adds a new component of the given <paramref name="componentType"/> with <paramref name="componentBytes"/> bytes
        /// to <paramref name="entity"/>.
        /// </summary>
        public readonly void AddComponentBytes(uint entity, int componentType, ReadOnlySpan<byte> componentBytes)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfComponentAlreadyPresent(entity, componentType);

            Span<Slot> slots = world->slots.AsSpan();
            ref Slot slot = ref slots[(int)entity];
            MoveEntityTo(world, entity, ref slot, world->chunkMap.GetOrCreateWithAddedComponent(slot.chunk, componentType));
            NotifyComponentAdded(entity, componentType);
            Span<byte> component = new(slot.row.pointer + schema.schema->componentOffsets[(uint)componentType], componentBytes.Length);
            componentBytes.CopyTo(component);
        }

        /// <summary>
        /// Adds a new component of the given <paramref name="componentType"/>
        /// to <paramref name="entity"/> and retrieves its bytes.
        /// </summary>
        public readonly Span<byte> AddComponentBytes(uint entity, int componentType)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfComponentAlreadyPresent(entity, componentType);

            Span<Slot> slots = world->slots.AsSpan();
            ref Slot slot = ref slots[(int)entity];
            MoveEntityTo(world, entity, ref slot, world->chunkMap.GetOrCreateWithAddedComponent(slot.chunk, componentType));
            NotifyComponentAdded(entity, componentType);
            return new(slot.row.pointer + schema.schema->componentOffsets[(uint)componentType], schema.schema->sizes[(uint)componentType]);
        }

        /// <summary>
        /// Removes the component of type <typeparamref name="T"/> from the given <paramref name="entity"/>.
        /// </summary>
        public readonly void RemoveComponentType<T>(uint entity) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            int componentType = schema.GetComponentType<T>();
            ThrowIfComponentMissing(entity, componentType);

            Span<Slot> slots = world->slots.AsSpan();
            ref Slot slot = ref slots[(int)entity];
            MoveEntityTo(world, entity, ref slot, world->chunkMap.GetOrCreateWithRemovedComponent(slot.chunk, componentType));
            NotifyComponentRemoved(entity, componentType);
        }

        /// <summary>
        /// Removes the component of the given <paramref name="componentType"/> from the given <paramref name="entity"/>.
        /// </summary>
        public readonly void RemoveComponentType(uint entity, int componentType)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfComponentMissing(entity, componentType);

            Span<Slot> slots = world->slots.AsSpan();
            ref Slot slot = ref slots[(int)entity];
            MoveEntityTo(world, entity, ref slot, world->chunkMap.GetOrCreateWithRemovedComponent(slot.chunk, componentType));
            NotifyComponentRemoved(entity, componentType);
        }

        /// <summary>
        /// Removes the components of the given <paramref name="componentTypes"/> from the given <paramref name="entity"/>.
        /// </summary>
        public readonly void RemoveComponentTypes(uint entity, BitMask componentTypes)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfComponentsMissing(entity, componentTypes);

            Span<Slot> slots = world->slots.AsSpan();
            ref Slot slot = ref slots[(int)entity];
            MoveEntityTo(world, entity, ref slot, world->chunkMap.GetOrCreateWithRemovedComponents(slot.chunk, componentTypes));
            NotifyComponentsRemoved(entity, componentTypes);
        }

        /// <summary>
        /// Checks if any entity in this world contains a component
        /// of type <typeparamref name="T"/>.
        /// </summary>
        public readonly bool ContainsAnyComponent<T>() where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(world);

            int componentType = schema.GetComponentType<T>();
            Span<Chunk> chunks = world->chunkMap.chunkMap->chunks.AsSpan();
            for (int i = 0; i < chunks.Length; i++)
            {
                ChunkPointer* chunk = chunks[i].chunk;
                if (chunk->componentTypes.Contains(componentType) && chunk->count > 0)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if the given <paramref name="entity"/> contains a component of type <typeparamref name="T"/>.
        /// </summary>
        public readonly bool ContainsComponent<T>(uint entity) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            int componentType = schema.GetComponentType<T>();
            return world->slots[entity].chunk.ComponentTypes.Contains(componentType);
        }

        /// <summary>
        /// Checks if the given <paramref name="entity"/> contains the <paramref name="componentType"/>.
        /// </summary>
        public readonly bool ContainsComponent(uint entity, int componentType)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            return world->slots[entity].chunk.ComponentTypes.Contains(componentType);
        }

        /// <summary>
        /// Retrieves a reference to the component of type <typeparamref name="T"/> from the given <paramref name="entity"/>.
        /// </summary>
        public readonly ref T GetComponent<T>(uint entity) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            int componentType = schema.GetComponentType<T>();
            ThrowIfComponentMissing(entity, componentType);

            return ref *(T*)(world->slots[entity].row.pointer + schema.schema->componentOffsets[(uint)componentType]);
        }

        /// <summary>
        /// Retrieves the component of type <typeparamref name="T"/> if it exists,
        /// otherwise a <see langword="default"/> value.
        /// </summary>
        public readonly T GetComponentOrDefault<T>(uint entity) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            return *(T*)(world->slots[entity].row.pointer + schema.schema->componentOffsets[(uint)schema.GetComponentType<T>()]);
        }

        /// <summary>
        /// Retrieves the component of type <typeparamref name="T"/> if it exists, otherwise the given
        /// <paramref name="defaultValue"/> is returned.
        /// </summary>
        public readonly T GetComponentOrDefault<T>(uint entity, T defaultValue) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            int componentType = schema.GetComponentType<T>();
            ref Slot slot = ref world->slots[entity];
            if (slot.chunk.ComponentTypes.Contains(componentType))
            {
                return *(T*)(slot.row.pointer + schema.schema->componentOffsets[(uint)componentType]);
            }
            else
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Retrieves a reference to the component of type <typeparamref name="T"/>.
        /// </summary>
        public readonly ref T GetComponent<T>(uint entity, int componentType) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfComponentMissing(entity, componentType);

            return ref *(T*)(world->slots[entity].row.pointer + schema.schema->componentOffsets[(uint)componentType]);
        }

        /// <summary>
        /// Retrieves the component of type <typeparamref name="T"/> if it exists, otherwise the given
        /// <paramref name="defaultValue"/> is returned.
        /// </summary>
        public readonly T GetComponentOrDefault<T>(uint entity, int componentType, T defaultValue = default) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            ref Slot slot = ref world->slots[entity];
            if (slot.chunk.ComponentTypes.Contains(componentType))
            {
                return *(T*)(slot.row.pointer + schema.schema->componentOffsets[(uint)componentType]);
            }
            else
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Retrieves the memory address containing the <paramref name="componentType"/> on
        /// the given <paramref name="entity"/>.
        /// </summary>
        public readonly MemoryAddress GetComponent(uint entity, int componentType)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfComponentMissing(entity, componentType);

            return new(world->slots[entity].row.pointer + schema.schema->componentOffsets[(uint)componentType]);
        }

        /// <summary>
        /// Retrieves the component of the given <paramref name="componentType"/> from the given <paramref name="entity"/>
        /// as a pointer.
        /// </summary>
        public readonly MemoryAddress GetComponent(uint entity, int componentType, out int componentSize)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfComponentMissing(entity, componentType);

            componentSize = schema.schema->sizes[(uint)componentType];
            return new(world->slots[entity].row.pointer + schema.schema->componentOffsets[(uint)componentType]);
        }

        /// <summary>
        /// Fetches the bytes of a component from the given <paramref name="entity"/>.
        /// </summary>
        public readonly Span<byte> GetComponentBytes(uint entity, int componentType)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfComponentMissing(entity, componentType);

            return new(world->slots[entity].row.pointer + schema.schema->componentOffsets[(uint)componentType], schema.schema->sizes[(uint)componentType]);
        }

        /// <summary>
        /// Retrieves the component from the given <paramref name="entity"/> as an <see cref="object"/>.
        /// </summary>
        public readonly object GetComponentObject(uint entity, int componentType)
        {
            MemoryAddress.ThrowIfDefault(world);

            TypeMetadata layout = schema.GetComponentLayout(componentType);
            Span<byte> bytes = GetComponentBytes(entity, componentType);
            return layout.CreateInstance(bytes);
        }

        /// <summary>
        /// Retrieves the array from the given <paramref name="entity"/> as <see cref="object"/>s.
        /// </summary>
        public readonly object[] GetArrayObject(uint entity, int arrayType)
        {
            MemoryAddress.ThrowIfDefault(world);

            TypeMetadata layout = schema.GetArrayLayout(arrayType);
            Values array = GetArray(entity, arrayType);
            object[] arrayObject = new object[array.Length];
            for (int i = 0; i < array.Length; i++)
            {
                MemoryAddress allocation = array[i];
                arrayObject[i] = layout.CreateInstance(new(allocation.pointer, layout.Size));
            }

            return arrayObject;
        }

        /// <summary>
        /// Attempts to retrieve a reference to the component of type <typeparamref name="T"/> from the given <paramref name="entity"/>.
        /// </summary>
        /// <returns><see langword="true"/> if the component is found.</returns>
        public readonly ref T TryGetComponent<T>(uint entity, int componentType, out bool contains) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(world);

            ref Slot slot = ref world->slots[entity];
            contains = slot.chunk.ComponentTypes.Contains(componentType);
            if (contains)
            {
                return ref *(T*)(slot.row.pointer + schema.schema->componentOffsets[(uint)componentType]);
            }
            else
            {
                return ref *(T*)default(nint);
            }
        }

        /// <summary>
        /// Attempts to retrieve a reference to the component of type <typeparamref name="T"/> from the given <paramref name="entity"/>.
        /// </summary>
        /// <returns><see langword="true"/> if the component is found.</returns>
        public readonly ref T TryGetComponent<T>(uint entity, out bool contains) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(world);

            int componentType = schema.GetComponentType<T>();
            ref Slot slot = ref world->slots[entity];
            contains = slot.chunk.ComponentTypes.Contains(componentType);
            if (contains)
            {
                return ref *(T*)(slot.row.pointer + schema.schema->componentOffsets[(uint)componentType]);
            }
            else
            {
                return ref *(T*)default(nint);
            }
        }

        /// <summary>
        /// Attempts to retrieve the component of type <typeparamref name="T"/> from the given <paramref name="entity"/>.
        /// </summary>
        /// <returns><see langword="true"/> if found.</returns>
        public readonly bool TryGetComponent<T>(uint entity, int componentType, out T component) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(world);

            ref Slot slot = ref world->slots[entity];
            if (slot.chunk.ComponentTypes.Contains(componentType))
            {
                component = *(T*)(slot.row.pointer + schema.schema->componentOffsets[(uint)componentType]);
                return true;
            }
            else
            {
                component = default;
                return false;
            }
        }

        /// <summary>
        /// Attempts to retrieve the component of type <typeparamref name="T"/> from the given <paramref name="entity"/>.
        /// </summary>
        /// <returns><see langword="true"/> if found.</returns>
        public readonly bool TryGetComponent<T>(uint entity, out T component) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(world);

            int componentType = schema.GetComponentType<T>();
            ref Slot slot = ref world->slots[entity];
            if (slot.chunk.ComponentTypes.Contains(componentType))
            {
                component = *(T*)(slot.row.pointer + schema.schema->componentOffsets[(uint)componentType]);
                return true;
            }
            else
            {
                component = default;
                return false;
            }
        }

        /// <summary>
        /// Assigns the given <paramref name="component"/> to the given <paramref name="entity"/>.
        /// </summary>
        public readonly void SetComponent<T>(uint entity, T component) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            int componentType = schema.GetComponentType<T>();
            ThrowIfComponentMissing(entity, componentType);

            *(T*)(world->slots[entity].row.pointer + schema.schema->componentOffsets[(uint)componentType]) = component;
        }

        /// <summary>
        /// Assigns the given <paramref name="component"/> to the given <paramref name="entity"/>.
        /// </summary>
        public readonly void SetComponent<T>(uint entity, int componentType, T component) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfComponentMissing(entity, componentType);

            *(T*)(world->slots[entity].row.pointer + schema.schema->componentOffsets[(uint)componentType]) = component;
        }

        /// <summary>
        /// Assigns the given <paramref name="componentBytes"/> to the given <paramref name="entity"/>.
        /// </summary>
        public readonly void SetComponentBytes(uint entity, int componentType, ReadOnlySpan<byte> componentBytes)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfComponentMissing(entity, componentType);

            Span<byte> component = new(world->slots[entity].row.pointer + schema.schema->componentOffsets[(uint)componentType], componentBytes.Length);
            componentBytes.CopyTo(component);
        }

        /// <summary>
        /// Returns the chunk that contains the given <paramref name="entity"/>.
        /// </summary>
        public readonly Chunk GetChunk(uint entity)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            return world->slots[entity].chunk;
        }

        /// <summary>
        /// Retrieves the <see cref="Slot"/> that contains the given <paramref name="entity"/>.
        /// </summary>
        public readonly Slot GetSlot(uint entity)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            return world->slots[entity];
        }

        /// <summary>
        /// Returns the chunk that contains the given <paramref name="entity"/>,
        /// along with the row.
        /// </summary>
        public readonly Chunk.Row GetChunkRow(uint entity)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            return new(schema.schema->componentOffsets, schema.schema->sizes, world->slots[entity].row);
        }

        /// <summary>
        /// Counts how many chunks exist to contain the given <paramref name="componentTypes"/>.
        /// </summary>
        public readonly int CountChunks(BitMask componentTypes)
        {
            MemoryAddress.ThrowIfDefault(world);

            int count = 0;
            Span<Chunk> chunks = world->chunkMap.chunkMap->chunks.AsSpan();
            for (int i = 0; i < world->chunkMap.chunkMap->count; i++)
            {
                if (chunks[i].ComponentTypes.ContainsAll(componentTypes))
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Counts how many chunks exist to contain the given <paramref name="componentTypes"/>,
        /// <paramref name="arrayTypes"/>, and <paramref name="tagTypes"/>.
        /// </summary>
        public readonly int CountChunks(BitMask componentTypes, BitMask arrayTypes, BitMask tagTypes)
        {
            MemoryAddress.ThrowIfDefault(world);

            int count = 0;
            Span<Chunk> chunks = world->chunkMap.chunkMap->chunks.AsSpan();
            for (int i = 0; i < world->chunkMap.chunkMap->count; i++)
            {
                ChunkPointer* chunk = chunks[i].chunk;
                if (chunk->componentTypes.ContainsAll(componentTypes) && chunk->arrayTypes.ContainsAll(arrayTypes) && chunk->tagTypes.ContainsAll(tagTypes))
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Counts how many chunks exist to contain the components, arrays, and tags from
        /// the given <paramref name="definition"/>.
        /// </summary>
        public readonly int CountChunks(Definition definition)
        {
            MemoryAddress.ThrowIfDefault(world);

            int count = 0;
            Span<Chunk> chunks = world->chunkMap.chunkMap->chunks.AsSpan();
            for (int i = 0; i < world->chunkMap.chunkMap->count; i++)
            {
                Chunk chunk = chunks[i];
                if (chunk.ComponentTypes.ContainsAll(definition.componentTypes) && chunk.ArrayTypes.ContainsAll(definition.arrayTypes) && chunk.TagTypes.ContainsAll(definition.tagTypes))
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Counts how many chunks exist to contain the given <paramref name="componentType"/>.
        /// </summary>
        public readonly int CountChunksWithComponent(int componentType)
        {
            MemoryAddress.ThrowIfDefault(world);

            int count = 0;
            Span<Chunk> chunks = world->chunkMap.chunkMap->chunks.AsSpan();
            for (int i = 0; i < world->chunkMap.chunkMap->count; i++)
            {
                if (chunks[i].ComponentTypes.Contains(componentType))
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Counts how many chunks exist to contain the given <paramref name="arrayType"/>.
        /// </summary>
        public readonly int CountChunksWithArray(int arrayType)
        {
            MemoryAddress.ThrowIfDefault(world);

            int count = 0;
            Span<Chunk> chunks = world->chunkMap.chunkMap->chunks.AsSpan();
            for (int i = 0; i < world->chunkMap.chunkMap->count; i++)
            {
                if (chunks[i].ArrayTypes.Contains(arrayType))
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Counts how many chunks exist to contain the given <paramref name="tagType"/>.
        /// </summary>
        public readonly int CountChunksWithTag(int tagType)
        {
            MemoryAddress.ThrowIfDefault(world);

            int count = 0;
            Span<Chunk> chunks = world->chunkMap.chunkMap->chunks.AsSpan();
            for (int i = 0; i < world->chunkMap.chunkMap->count; i++)
            {
                if (chunks[i].TagTypes.Contains(tagType))
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Retrieves the definition of the given <paramref name="entity"/>.
        /// </summary>
        public readonly Definition GetDefinition(uint entity)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            return world->slots[entity].chunk.Definition;
        }

        /// <summary>
        /// Retrieves the definition of the given <paramref name="entity"/>.
        /// </summary>
        public readonly Definition GetDefinitionOrDefault(uint entity)
        {
            MemoryAddress.ThrowIfDefault(world);

            if (world->slotMetadata[entity].state == SlotState.Free)
            {
                return Definition.Default;
            }

            return world->slots[entity].chunk.Definition;
        }

        /// <summary>
        /// Copies components from the source entity onto the destination.
        /// <para>Components will be added if the destination entity doesnt
        /// contain them. Existing component data will be overwritten.</para>
        /// </summary>
        public readonly void CopyComponentsTo(uint sourceEntity, World destinationWorld, uint destinationEntity)
        {
            ThrowIfEntityIsMissing(sourceEntity);
            destinationWorld.ThrowIfEntityIsMissing(destinationEntity);

            SchemaPointer* sourceSchema = schema.schema;
            SchemaPointer* destinationSchema = destinationWorld.schema.schema;
            Slot sourceSlot = world->slots[(int)sourceEntity];
            ref Slot destinationSlot = ref destinationWorld.world->slots[(int)destinationEntity];
            for (int c = 0; c < BitMask.Capacity; c++)
            {
                if (sourceSlot.chunk.ComponentTypes.Contains(c))
                {
                    int sourceComponentSize = sourceSchema->sizes[(uint)c];
                    uint sourceComponentOffset = sourceSchema->componentOffsets[(uint)c];
                    uint destinationComponentOffset = destinationSchema->componentOffsets[(uint)c];
                    if (!destinationSlot.chunk.ComponentTypes.Contains(c))
                    {
                        destinationWorld.AddComponentType(destinationEntity, c);
                    }

                    destinationSlot.row.Write(destinationComponentOffset, sourceComponentSize, sourceSlot.row.Read(sourceComponentOffset));
                }
            }
        }

        /// <summary>
        /// Copies all arrays from the <paramref name="sourceEntity"/> to the <paramref name="destinationEntity"/>.
        /// <para>
        /// Arrays will be created if the destination doesn't already
        /// contain them. Data will be overwritten, and lengths will be changed.
        /// </para>
        /// </summary>
        public readonly void CopyArraysTo(uint sourceEntity, World destinationWorld, uint destinationEntity)
        {
            BitMask arrayTypes = GetArrayTypes(sourceEntity);
            for (int a = 0; a < BitMask.Capacity; a++)
            {
                if (arrayTypes.Contains(a))
                {
                    Values sourceArray = GetArray(sourceEntity, a);
                    Values destinationArray;
                    if (!destinationWorld.ContainsArray(destinationEntity, a))
                    {
                        destinationArray = CreateArray(destinationEntity, a, sourceArray.Stride, sourceArray.Length);
                    }
                    else
                    {
                        destinationArray = GetArray(destinationEntity, a);
                        destinationArray.Length = sourceArray.Length;
                    }

                    Span<byte> destination = destinationArray.GetSpan(sourceArray.Length * sourceArray.Stride);
                    sourceArray.AsSpan().CopyTo(destination);
                }
            }
        }

        /// <summary>
        /// Copies all tags from the <paramref name="sourceEntity"/> to the <paramref name="destinationEntity"/>.
        /// </summary>
        public readonly void CopyTagsTo(uint sourceEntity, World destinationWorld, uint destinationEntity)
        {
            BitMask tagTypes = GetTagTypes(sourceEntity);
            for (int t = 0; t < BitMask.Capacity; t++)
            {
                if (tagTypes.Contains(t))
                {
                    if (!destinationWorld.ContainsTag(destinationEntity, t))
                    {
                        destinationWorld.AddTag(destinationEntity, t);
                    }
                }
            }
        }

        /// <summary>
        /// Copies all references from the <paramref name="sourceEntity"/> to the <paramref name="destinationEntity"/>.
        /// </summary>
        public readonly void CopyReferencesTo(uint sourceEntity, World destinationWorld, uint destinationEntity)
        {
            int referenceCount = GetReferenceCount(sourceEntity);
            for (int r = 1; r <= referenceCount; r++)
            {
                uint referencedEntity = GetReference(sourceEntity, new rint(r));
                destinationWorld.AddReference(destinationEntity, referencedEntity);
            }
        }

        /// <summary>
        /// Creates a perfect replica of this entity.
        /// </summary>
        public readonly uint CloneEntity(uint entity)
        {
            uint clone = CreateEntity();
            CopyComponentsTo(entity, this, clone);
            CopyArraysTo(entity, this, clone);
            CopyTagsTo(entity, this, clone);
            CopyReferencesTo(entity, this, clone);
            return clone;
        }

        [Conditional("DEBUG")]
        private readonly void TraceCreation(uint entity)
        {
#if DEBUG
            createStackTraces[new Entity(this, entity)] = new StackTrace(2, true);
#endif
        }

        [Conditional("DEBUG")]
        internal readonly void ThrowIfEntityIsMissing(uint entity)
        {
            if (entity == default)
            {
                throw new InvalidOperationException($"Entity `{entity}` is not valid");
            }

            if (entity >= world->slots.Count)
            {
                throw new EntityIsMissingException(this, entity);
            }

            ref SlotState state = ref world->slotMetadata[entity].state;
            if (state == SlotState.Free)
            {
                throw new EntityIsMissingException(this, entity);
            }
        }

        [Conditional("DEBUG")]
        internal static void ThrowIfParentIsSameAsChild(uint entity, uint parent)
        {
            if (entity == parent)
            {
                throw new InvalidOperationException($"Entity `{entity}` cannot be its own parent");
            }
        }

        [Conditional("DEBUG")]
        internal readonly void ThrowIfReferenceIsMissing(uint entity, rint reference)
        {
            ref SlotMetadata metadata = ref world->slotMetadata[entity];
            int count = (int)(metadata.referenceRange >> 32);
            if (reference.value == 0 || reference.value > count)
            {
                throw new ReferenceToEntityIsMissingException(this, entity, reference);
            }
        }

        [Conditional("DEBUG")]
        internal readonly void ThrowIfReferencedEntityIsMissing(uint entity, uint referencedEntity)
        {
            ref SlotMetadata metadata = ref world->slotMetadata[entity];
            int start = (int)(metadata.referenceRange & 0xFFFFFFFF);
            int count = (int)(metadata.referenceRange >> 32);
            Span<uint> references = world->references.AsSpan(start, count);
            if (!references.Contains(referencedEntity))
            {
                throw new ReferenceToEntityIsMissingException(this, entity, referencedEntity);
            }
        }

        [Conditional("DEBUG")]
        internal readonly void ThrowIfComponentMissing(uint entity, int componentType)
        {
            BitMask componentTypes = world->slots[entity].chunk.ComponentTypes;
            if (!componentTypes.Contains(componentType))
            {
                throw new ComponentIsMissingException(this, entity, componentType);
            }
        }

        [Conditional("DEBUG")]
        internal readonly void ThrowIfComponentsMissing(uint entity, BitMask componentTypes)
        {
            BitMask currentComponentTypes = world->slots[entity].chunk.ComponentTypes;
            if (!currentComponentTypes.ContainsAll(componentTypes))
            {
                throw new ComponentIsMissingException(this, entity, componentTypes);
            }
        }

        [Conditional("DEBUG")]
        internal readonly void ThrowIfComponentAlreadyPresent(uint entity, int componentType)
        {
            BitMask componentTypes = world->slots[entity].chunk.ComponentTypes;
            if (componentTypes.Contains(componentType))
            {
                throw new ComponentIsAlreadyPresentException(this, entity, componentType);
            }
        }

        [Conditional("DEBUG")]
        internal readonly void ThrowIfComponentsAlreadyPresent(uint entity, BitMask componentTypes)
        {
            BitMask currentComponentTypes = world->slots[entity].chunk.ComponentTypes;
            if (currentComponentTypes.ContainsAll(componentTypes))
            {
                throw new ComponentIsAlreadyPresentException(this, entity, componentTypes);
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfTagAlreadyPresent(uint entity, int tagType)
        {
            BitMask tagTypes = world->slots[entity].chunk.TagTypes;
            if (tagTypes.Contains(tagType))
            {
                throw new TagIsAlreadyPresentException(this, entity, tagType);
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfTagIsMissing(uint entity, int tagType)
        {
            BitMask tagTypes = world->slots[entity].chunk.TagTypes;
            if (!tagTypes.Contains(tagType))
            {
                throw new TagIsMissingException(this, entity, tagType);
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfArrayIsMissing(uint entity, int arrayType)
        {
            BitMask arrayTypes = world->slots[entity].chunk.ArrayTypes;
            if (!arrayTypes.Contains(arrayType))
            {
                throw new ArrayIsMissingException(this, entity, arrayType);
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfArrayIsAlreadyPresent(uint entity, int arrayType)
        {
            BitMask arrayTypes = world->slots[entity].chunk.ArrayTypes;
            if (arrayTypes.Contains(arrayType))
            {
                throw new ArrayIsAlreadyPresentException(this, entity, arrayType);
            }
        }

        /// <summary>
        /// Creates a new world.
        /// </summary>
        public static World Create()
        {
            return new(new Schema());
        }

        /// <summary>
        /// Deserializes a world from the given <paramref name="reader"/>.
        /// </summary>
        public static World Deserialize(ByteReader reader)
        {
            return Deserialize(reader, null);
        }

        /// <summary>
        /// Deserializes a world from the given <paramref name="reader"/>
        /// with a custom schema processor.
        /// </summary>
        public static World Deserialize(ByteReader reader, ProcessSchema process)
        {
            return Deserialize(reader, process.Invoke);
        }

        /// <summary>
        /// Deserializes a world from the given <paramref name="reader"/>
        /// with a custom schema processor.
        /// </summary>
        public static World Deserialize(ByteReader reader, Func<TypeMetadata, DataType.Kind, TypeMetadata>? process)
        {
            Signature signature = reader.ReadValue<Signature>();
            if (signature.Version != Version)
            {
                throw new InvalidOperationException($"Invalid version `{signature.Version}` expected `{Version}`");
            }

            // deserialize the schema first
            Schema schema;
            if (process is not null)
            {
                schema = Schema.Create();
                using Schema loadedSchema = reader.ReadObject<Schema>();
                foreach (int componentType in loadedSchema.ComponentTypes)
                {
                    TypeMetadata typeLayout = loadedSchema.GetComponentLayout(componentType);
                    typeLayout = process.Invoke(typeLayout, DataType.Kind.Component);
                    schema.RegisterComponent(typeLayout);
                }

                foreach (int arrayType in loadedSchema.ArrayTypes)
                {
                    TypeMetadata typeLayout = loadedSchema.GetArrayLayout(arrayType);
                    typeLayout = process.Invoke(typeLayout, DataType.Kind.Array);
                    schema.RegisterArray(typeLayout);
                }

                foreach (int tagType in loadedSchema.TagTypes)
                {
                    TypeMetadata typeLayout = loadedSchema.GetTagLayout(tagType);
                    typeLayout = process.Invoke(typeLayout, DataType.Kind.Tag);
                    schema.RegisterTag(typeLayout);
                }
            }
            else
            {
                schema = reader.ReadObject<Schema>();
            }

            World value = new(schema);
            int entityCount = reader.ReadValue<int>();
            int maxSlotCount = reader.ReadValue<int>();

            // todo: this could be a stackalloc span instead
            using Array<uint> entityMap = new(maxSlotCount + 1);
            for (uint e = 0; e < entityCount; e++)
            {
                uint entity = reader.ReadValue<uint>();
                SlotState state = reader.ReadValue<SlotState>();
                uint parent = reader.ReadValue<uint>();

                uint createdEntity = value.CreateEntity();
                entityMap[(int)entity] = createdEntity;
                ref Slot slot = ref value.world->slots[(int)createdEntity];
                ref SlotMetadata metadata = ref value.world->slotMetadata[(int)createdEntity];
                metadata.state = state;
                slot.parent = parent;

                //read components
                byte componentCount = reader.ReadValue<byte>();
                for (uint i = 0; i < componentCount; i++)
                {
                    byte c = reader.ReadValue<byte>();
                    MemoryAddress component = value.AddComponent(createdEntity, c, out int componentSize);
                    Span<byte> componentData = reader.ReadSpan<byte>(componentSize);
                    component.CopyFrom(componentData);
                }

                //read arrays
                byte arrayCount = reader.ReadValue<byte>();
                for (uint i = 0; i < arrayCount; i++)
                {
                    byte a = reader.ReadValue<byte>();
                    int length = reader.ReadValue<int>();
                    int arrayElementSize = schema.GetArraySize(a);
                    Values array = value.CreateArray(createdEntity, a, arrayElementSize, length);
                    Span<byte> arrayData = reader.ReadSpan<byte>(length * arrayElementSize);
                    arrayData.CopyTo(array.AsSpan());
                }

                //read tags
                byte tagCount = reader.ReadValue<byte>();
                for (uint i = 0; i < tagCount; i++)
                {
                    byte t = reader.ReadValue<byte>();
                    value.AddTag(createdEntity, t);
                }
            }

            //assign references and children
            Span<Slot> slots = value.world->slots.AsSpan();
            Span<SlotMetadata> slotMetadata = value.world->slotMetadata.AsSpan();
            List<uint> references = value.world->references;
            for (uint e = 1; e < slots.Length; e++)
            {
                ref Slot slot = ref slots[(int)e];
                ref SlotMetadata metadata = ref slotMetadata[(int)e];
                if (metadata.state == SlotState.Free)
                {
                    continue;
                }

                int referenceCount = reader.ReadValue<int>();
                if (referenceCount > 0)
                {
                    metadata.referenceRange = (uint)references.Count | ((ulong)referenceCount << 32);
                    for (int r = 0; r < referenceCount; r++)
                    {
                        uint referencedEntity = reader.ReadValue<uint>();
                        uint createdReferencesEntity = entityMap[(int)referencedEntity];
                        references.Add(createdReferencesEntity);
                    }
                }

                uint parent = slot.parent;
                if (parent != default)
                {
                    ref Slot parentSlot = ref slots[(int)parent];
                    ref SlotMetadata parentMetadata = ref slotMetadata[(int)parent];
                    if ((parentMetadata.flags & SlotFlags.ContainsChildren) == 0)
                    {
                        parentMetadata.flags |= SlotFlags.ContainsChildren;
                    }

                    parentSlot.childrenCount++;
                }
            }

            return value;
        }

        /// <inheritdoc/>
        public static bool operator ==(World left, World right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(World left, World right)
        {
            return !(left == right);
        }
    }
}