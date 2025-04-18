using Collections.Generic;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
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
        /// <summary>
        /// The version of the binary format used to serialize the world.
        /// </summary>
        public const uint DataVersion = 1;

        private WorldPointer* world;

        /// <summary>
        /// Native address of the world.
        /// </summary>
        public readonly nint Address => (nint)world;

        /// <summary>
        /// Amount of entities that exist in the world.
        /// </summary>
        public readonly int Count
        {
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
            get
            {
                MemoryAddress.ThrowIfDefault(world);

                return world->slots.Count - 1;
            }
        }

        /// <summary>
        /// Returns the version of the world.
        /// </summary>
        public readonly int Version
        {
            get
            {
                MemoryAddress.ThrowIfDefault(world);

                return world->version;
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
            get
            {
                MemoryAddress.ThrowIfDefault(world);

                return world->chunks.chunkMap->chunks.AsSpan();
            }
        }

        /// <summary>
        /// The schema containing all component and array types.
        /// </summary>
        public readonly Schema Schema
        {
            get
            {
                MemoryAddress.ThrowIfDefault(world);

                return world->schema;
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
            Schema schema = new();
            world = MemoryAddress.AllocatePointer<WorldPointer>();
            world->schema = schema;
            world->version = 0;
            world->slots = new(4);
            world->arrays = new(4);
            world->freeEntities = new(4);
            world->chunks = new(schema);
            world->entityCreatedOrDestroyed = new(4);
            world->entityParentChanged = new(4);
            world->entityDataChanged = new(4);
            world->references = new(4);
            world->entityCreatedOrDestroyedCount = 0;
            world->entityParentChangedCount = 0;
            world->entityDataChangedCount = 0;

            //add reserve values at index 0
            world->slots.AddDefault();
            world->arrays.AddDefault();
        }
#endif

        /// <summary>
        /// Creates a new world with the given <paramref name="schema"/>.
        /// </summary>
        public World(Schema schema)
        {
            world = MemoryAddress.AllocatePointer<WorldPointer>();
            world->schema = schema;
            world->version = 0;
            world->slots = new(4);
            world->arrays = new(4);
            world->freeEntities = new(4);
            world->chunks = new(schema);
            world->entityCreatedOrDestroyed = new(4);
            world->entityParentChanged = new(4);
            world->entityDataChanged = new(4);
            world->references = new(4);
            world->entityCreatedOrDestroyedCount = 0;
            world->entityParentChangedCount = 0;
            world->entityDataChangedCount = 0;

            //add reserve values at index 0
            world->slots.AddDefault();
            world->arrays.AddDefault();
        }

        /// <summary>
        /// Initializes an existing world from the given <paramref name="pointer"/>.
        /// </summary>
        public World(void* pointer)
        {
            this.world = (WorldPointer*)pointer;
        }

        /// <summary>
        /// Disposes everything in the world and the world itself.
        /// </summary>
        public void Dispose()
        {
            MemoryAddress.ThrowIfDefault(world);
            Clear();

            Span<Slot> slots = world->slots.AsSpan();
            Span<Arrays> arrays = world->arrays.AsSpan();
            for (int e = 1; e < slots.Length; e++)
            {
                Slot slot = slots[e];
                if ((slot.flags & Slot.Flags.ContainsArrays) != 0)
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
            world->chunks.Dispose();
            world->slots.Dispose();
            world->arrays.Dispose();
            MemoryAddress.Free(ref world);
        }

        /// <summary>
        /// Resets the world to <see langword="default"/> state.
        /// </summary>
        public readonly void Clear()
        {
            MemoryAddress.ThrowIfDefault(world);

            world->chunks.Clear();
            world->references.Clear();
            world->version++;

            Span<Slot> slots = world->slots.AsSpan();
            for (uint e = 1; e < slots.Length; e++)
            {
                ref Slot slot = ref slots[(int)e];
                if (slot.state == Slot.State.Free)
                {
                    continue;
                }

                slot.flags |= Slot.Flags.Outdated;
                slot.parent = default;
                slot.chunk = default;
                slot.index = default;
                slot.referenceStart = default;
                slot.referenceCount = default;
                slot.state = Slot.State.Free;
                world->freeEntities.Push(e);
            }
        }

        /// <summary>
        /// Copies the state of this world's schema to match the given <paramref name="sourceSchema"/>.
        /// </summary>
        public readonly void CopySchemaFrom(Schema sourceSchema)
        {
            MemoryAddress.ThrowIfDefault(world);

            world->schema.CopyFrom(sourceSchema);
            world->chunks.UpdateDefaultChunkStrideToMatchSchema();
        }

        /// <inheritdoc/>
        public readonly override string ToString()
        {
            if (world == default)
            {
                return "World (disposed)";
            }

            return $"World {Address} (count: {Count})";
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

        private readonly void NotifyCreation(uint entity)
        {
            int count = world->entityCreatedOrDestroyedCount;
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    (EntityCreatedOrDestroyed callback, ulong userData) = world->entityCreatedOrDestroyed[i];
                    callback.Invoke(this, entity, true, userData);
                }
            }
        }

        private readonly void NotifyParentChange(uint entity, uint oldParent, uint newParent)
        {
            int count = world->entityParentChangedCount;
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    (EntityParentChanged callback, ulong userData) = world->entityParentChanged[i];
                    callback.Invoke(this, entity, oldParent, newParent, userData);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly void NotifyComponentAdded(uint entity, int componentType)
        {
            int count = world->entityDataChangedCount;
            if (count > 0)
            {
                DataType type = world->schema.GetComponentDataType(componentType);
                for (int i = 0; i < count; i++)
                {
                    (EntityDataChanged callback, ulong userData) = world->entityDataChanged[i];
                    callback.Invoke(this, entity, type, true, userData);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly void NotifyComponentRemoved(uint entity, int componentType)
        {
            int count = world->entityDataChangedCount;
            if (count > 0)
            {
                DataType type = world->schema.GetComponentDataType(componentType);
                for (int i = 0; i < count; i++)
                {
                    (EntityDataChanged callback, ulong userData) = world->entityDataChanged[i];
                    callback.Invoke(this, entity, type, false, userData);
                }
            }
        }

        private readonly void NotifyArrayCreated(uint entity, int arrayType)
        {
            int count = world->entityDataChangedCount;
            if (count > 0)
            {
                DataType type = world->schema.GetArrayDataType(arrayType);
                for (int i = 0; i < count; i++)
                {
                    (EntityDataChanged callback, ulong userData) = world->entityDataChanged[i];
                    callback.Invoke(this, entity, type, true, userData);
                }
            }
        }

        private readonly void NotifyArrayDestroyed(uint entity, int arrayType)
        {
            int count = world->entityDataChangedCount;
            if (count > 0)
            {
                DataType type = world->schema.GetArrayDataType(arrayType);
                for (int i = 0; i < count; i++)
                {
                    (EntityDataChanged callback, ulong userData) = world->entityDataChanged[i];
                    callback.Invoke(this, entity, type, false, userData);
                }
            }
        }

        private readonly void NotifyTagAdded(uint entity, int tagType)
        {
            int count = world->entityDataChangedCount;
            if (count > 0)
            {
                DataType type = world->schema.GetTagDataType(tagType);
                for (int i = 0; i < count; i++)
                {
                    (EntityDataChanged callback, ulong userData) = world->entityDataChanged[i];
                    callback.Invoke(this, entity, type, true, userData);
                }
            }
        }

        private readonly void NotifyTagRemoved(uint entity, int tagType)
        {
            int count = world->entityDataChangedCount;
            if (count > 0)
            {
                DataType type = world->schema.GetTagDataType(tagType);
                for (int i = 0; i < count; i++)
                {
                    (EntityDataChanged callback, ulong userData) = world->entityDataChanged[i];
                    callback.Invoke(this, entity, type, false, userData);
                }
            }
        }

        readonly void ISerializable.Write(ByteWriter writer)
        {
            MemoryAddress.ThrowIfDefault(world);

            writer.WriteValue(new Signature(DataVersion));
            writer.WriteObject(world->schema);
            writer.WriteValue(Count);
            writer.WriteValue(MaxEntityValue);

            Span<Slot> slots = world->slots.AsSpan();
            for (uint e = 1; e < slots.Length; e++)
            {
                ref Slot slot = ref slots[(int)e];
                if (slot.state == Slot.State.Free)
                {
                    continue;
                }

                Chunk chunk = slot.chunk;
                Definition definition = chunk.Definition;
                writer.WriteValue(e);
                writer.WriteValue(slot.state);
                writer.WriteValue(slot.parent);

                //write components
                writer.WriteValue((byte)definition.componentTypes.Count);
                for (int c = 0; c < BitMask.Capacity; c++)
                {
                    if (definition.componentTypes.Contains(c))
                    {
                        writer.WriteValue((byte)c);
                        Span<byte> componentBytes = GetComponentBytes(e, c);
                        writer.WriteSpan(componentBytes);
                    }
                }

                //write arrays
                writer.WriteValue((byte)definition.arrayTypes.Count);
                for (int a = 0; a < BitMask.Capacity; a++)
                {
                    if (definition.arrayTypes.Contains(a))
                    {
                        writer.WriteValue((byte)a);
                        Values array = GetArray(e, a);
                        writer.WriteValue(array.Length);
                        writer.WriteSpan(array.AsSpan());
                    }
                }

                //write tags
                writer.WriteValue((byte)definition.tagTypes.Count);
                for (int t = 0; t < BitMask.Capacity; t++)
                {
                    if (definition.tagTypes.Contains(t))
                    {
                        writer.WriteValue((byte)t);
                    }
                }
            }

            //write references
            for (uint e = 1; e < slots.Length; e++)
            {
                if (slots[(int)e].state == Slot.State.Free)
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
            world = Deserialize(reader).world;
        }

        /// <summary>
        /// Appends entities from the given <paramref name="sourceWorld"/>.
        /// </summary>
        public readonly void Append(World sourceWorld)
        {
            MemoryAddress.ThrowIfDefault(world);

            World destinationWorld = this;
            Span<Slot> sourceSlots = sourceWorld.world->slots.AsSpan();
            for (uint e = 1; e < sourceSlots.Length; e++)
            {
                Slot sourceSlot = sourceSlots[(int)e];
                if (sourceSlot.state == Slot.State.Free)
                {
                    continue;
                }

                uint destinationEntity = destinationWorld.CreateEntity(sourceSlot.chunk.Definition);
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
        }

        /// <summary>
        /// Adds a function that listens to when an entity's parent changes.
        /// </summary>
        public readonly void ListenToEntityParentChanges(EntityParentChanged function, ulong userData = default)
        {
            MemoryAddress.ThrowIfDefault(world);

            world->entityParentChanged.Add((function, userData));
            world->entityParentChangedCount++;
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
            if (slot.childrenCount > 0)
            {
                Span<uint> children = stackalloc uint[slot.childrenCount];
                CopyChildrenTo(entity, children);
                if (destroyChildren)
                {
                    //destroy children
                    for (int i = 0; i < children.Length; i++)
                    {
                        uint child = children[i];
                        DestroyEntity(child, destroyChildren); //recusive
                    }
                }
                else
                {
                    //unparent children
                    for (int i = 0; i < children.Length; i++)
                    {
                        uint child = children[i];
                        ref Slot childSlot = ref slots[(int)child];
                        childSlot.parent = default;
                    }
                }
            }

            slot.flags |= Slot.Flags.Outdated;
            slot.state = Slot.State.Free;
            slot.referenceCount = default;

            ref Slot lastSlot = ref slots[(int)slot.chunk.chunk->lastEntity];
            lastSlot.index = slot.index;

            //remove from parents children list
            ref Slot parentSlot = ref slots[(int)slot.parent]; //it can be 0, which is ok
            parentSlot.childrenCount--;
            slot.chunk.RemoveEntityAt(slot.index);
            slot.parent = default;
            world->freeEntities.Push(entity);

            int count = world->entityCreatedOrDestroyedCount;
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    (EntityCreatedOrDestroyed callback, ulong userData) = world->entityCreatedOrDestroyed[i];
                    callback.Invoke(this, entity, false, userData);
                }
            }

            world->version++;
        }

        /// <summary>
        /// Copies component types from the given <paramref name="entity"/> to the destination <paramref name="destination"/>.
        /// </summary>
        public readonly int CopyComponentTypesTo(uint entity, Span<int> destination)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            return world->slots[entity].chunk.chunk->definition.CopyComponentTypesTo(destination);
        }

        /// <summary>
        /// Checks if the given <paramref name="entity"/> is enabled with respect
        /// to ancestor entities.
        /// </summary>
        public readonly bool IsEnabled(uint entity)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            return world->slots[entity].state == Slot.State.Enabled;
        }

        /// <summary>
        /// Checks if the given entity is enabled regardless
        /// of it's parents.
        /// </summary>
        public readonly bool IsLocallyEnabled(uint entity)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            Slot.State state = world->slots[entity].state;
            return state == Slot.State.Enabled || state == Slot.State.DisabledButLocallyEnabled;
        }

        /// <summary>
        /// Assigns the enabled state of the given <paramref name="entity"/>
        /// and its descendants to the given <paramref name="enabled"/>.
        /// </summary>
        public readonly void SetEnabled(uint entity, bool enabled)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            world->version++;
            Span<Slot> slots = world->slots.AsSpan();
            ref Slot entitySlot = ref slots[(int)entity];
            uint parent = entitySlot.parent;
            if (parent != default)
            {
                Slot.State parentState = slots[(int)parent].state;
                if (parentState == Slot.State.Disabled || parentState == Slot.State.DisabledButLocallyEnabled)
                {
                    entitySlot.state = enabled ? Slot.State.DisabledButLocallyEnabled : Slot.State.Disabled;
                }
                else
                {
                    entitySlot.state = enabled ? Slot.State.Enabled : Slot.State.Disabled;
                }
            }
            else
            {
                entitySlot.state = enabled ? Slot.State.Enabled : Slot.State.Disabled;
            }

            //move to different chunk
            Chunk previousChunk = entitySlot.chunk;
            Definition previousDefinition = previousChunk.Definition;
            bool oldEnabled = !previousDefinition.tagTypes.Contains(Schema.DisabledTagType);
            bool newEnabled = entitySlot.state == Slot.State.Enabled;
            if (oldEnabled != newEnabled)
            {
                Definition newDefinition = previousDefinition;
                if (newEnabled)
                {
                    newDefinition.RemoveTagType(Schema.DisabledTagType);
                }
                else
                {
                    newDefinition.AddTagType(Schema.DisabledTagType);
                }

                if (entity != entitySlot.chunk.chunk->lastEntity)
                {
                    slots[(int)entitySlot.chunk.chunk->lastEntity].index = entitySlot.index;
                }

                Chunk destinationChunk = world->chunks.GetOrCreate(newDefinition);
                Chunk.MoveEntityAt(entity, ref entitySlot.index, ref entitySlot.chunk, destinationChunk);
            }

            //modify descendants
            if ((entitySlot.flags & Slot.Flags.ContainsChildren) != 0)
            {
                //todo: this temporary allocation can be avoided by tracking how large the tree is
                //and then using stackalloc
                using Stack<uint> stack = new(4);
                PushChildrenToStack(this, stack, entity);

                while (stack.Count > 0)
                {
                    uint currentEntity = stack.Pop();
                    ref Slot currentSlot = ref slots[(int)currentEntity];
                    if (enabled && currentSlot.state == Slot.State.DisabledButLocallyEnabled)
                    {
                        currentSlot.state = Slot.State.Enabled;
                    }
                    else if (!enabled && currentSlot.state == Slot.State.Enabled)
                    {
                        currentSlot.state = Slot.State.DisabledButLocallyEnabled;
                    }

                    //move descentant to proper chunk
                    previousChunk = currentSlot.chunk;
                    previousDefinition = previousChunk.Definition;
                    oldEnabled = !previousDefinition.tagTypes.Contains(Schema.DisabledTagType);
                    if (oldEnabled != enabled)
                    {
                        Definition newDefinition = previousDefinition;
                        if (enabled)
                        {
                            newDefinition.RemoveTagType(Schema.DisabledTagType);
                        }
                        else
                        {
                            newDefinition.AddTagType(Schema.DisabledTagType);
                        }

                        if (currentEntity != currentSlot.chunk.chunk->lastEntity)
                        {
                            slots[(int)currentSlot.chunk.chunk->lastEntity].index = currentSlot.index;
                        }

                        Chunk destinationChunk = world->chunks.GetOrCreate(newDefinition);
                        Chunk.MoveEntityAt(currentEntity, ref currentSlot.index, ref currentSlot.chunk, destinationChunk);
                    }

                    //check through children
                    if ((currentSlot.flags & Slot.Flags.ContainsChildren) != 0 && (currentSlot.flags & Slot.Flags.ChildrenOutdated) == 0)
                    {
                        PushChildrenToStack(this, stack, currentEntity);
                    }
                }

                static void PushChildrenToStack(World world, Stack<uint> stack, uint entity)
                {
                    Slot slot = world.world->slots[entity];
                    Span<uint> children = stackalloc uint[slot.childrenCount];
                    world.CopyChildrenTo(entity, children);
                    stack.PushRange(children);
                }
            }
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
                world->arrays.AddDefault();
            }

            ref Slot slot = ref world->slots[entity];
            slot.state = Slot.State.Enabled;
            slot.chunk = world->chunks.chunkMap->defaultChunk;
            slot.chunk.AddEntity(entity, ref slot.index);
            world->version++;
            TraceCreation(entity);
            NotifyCreation(entity);
            return entity;
        }

        /// <summary>
        /// Creates a new entity.
        /// </summary>
        public readonly uint CreateEntity(BitMask componentTypes, BitMask tagTypes = default)
        {
            MemoryAddress.ThrowIfDefault(world);

            if (!world->freeEntities.TryPop(out uint entity))
            {
                entity = (uint)world->slots.Count;
                world->slots.AddDefault();
                world->arrays.AddDefault();
            }

            Definition definition = new(componentTypes, default, tagTypes);
            ref Slot slot = ref world->slots[entity];
            slot.state = Slot.State.Enabled;
            slot.chunk = world->chunks.GetOrCreate(definition);
            slot.chunk.AddEntity(entity, ref slot.index);
            world->version++;
            TraceCreation(entity);
            NotifyCreation(entity);
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
                world->arrays.AddDefault();
            }

            ref Slot slot = ref world->slots[entity];
            slot.state = Slot.State.Enabled;
            slot.chunk = world->chunks.GetOrCreate(definition);

            //create arrays if necessary
            BitMask arrayTypes = definition.arrayTypes;
            if (!arrayTypes.IsEmpty)
            {
                ref Arrays arrays = ref world->arrays[entity];
                for (int a = 0; a < BitMask.Capacity; a++)
                {
                    if (arrayTypes.Contains(a))
                    {
                        int arrayElementSize = world->schema.GetArraySize(a);
                        arrays[a] = new(0, arrayElementSize);
                    }
                }

                slot.flags |= Slot.Flags.ContainsArrays;
            }

            slot.chunk.AddEntity(entity, ref slot.index);
            world->version++;
            TraceCreation(entity);
            NotifyCreation(entity);
            return entity;
        }

        /// <summary>
        /// Creates a new entity with the given <paramref name="definition"/>.
        /// </summary>
        public readonly uint CreateEntity(Definition definition, out Chunk chunk, out int index)
        {
            MemoryAddress.ThrowIfDefault(world);

            if (!world->freeEntities.TryPop(out uint entity))
            {
                entity = (uint)world->slots.Count;
                world->slots.AddDefault();
                world->arrays.AddDefault();
            }

            ref Slot slot = ref world->slots[entity];
            slot.state = Slot.State.Enabled;
            slot.chunk = world->chunks.GetOrCreate(definition);

            //create arrays if necessary
            BitMask arrayTypes = definition.arrayTypes;
            if (!arrayTypes.IsEmpty)
            {
                ref Arrays arrays = ref world->arrays[entity];
                for (int a = 0; a < BitMask.Capacity; a++)
                {
                    if (arrayTypes.Contains(a))
                    {
                        int arrayElementSize = world->schema.GetArraySize(a);
                        arrays[a] = new(0, arrayElementSize);
                    }
                }

                slot.flags |= Slot.Flags.ContainsArrays;
            }

            slot.chunk.AddEntity(entity, ref slot.index);
            index = slot.index;
            chunk = slot.chunk;
            world->version++;
            TraceCreation(entity);
            NotifyCreation(entity);
            return entity;
        }

        /// <summary>
        /// Creates a new entity.
        /// </summary>
        public readonly uint CreateEntity(BitMask componentTypes, out Chunk chunk, out int index, BitMask tagTypes = default)
        {
            MemoryAddress.ThrowIfDefault(world);

            if (!world->freeEntities.TryPop(out uint entity))
            {
                entity = (uint)world->slots.Count;
                world->slots.AddDefault();
                world->arrays.AddDefault();
            }

            Definition definition = new(componentTypes, default, tagTypes);
            world->version++;
            ref Slot slot = ref world->slots[entity];
            slot.state = Slot.State.Enabled;
            slot.chunk = world->chunks.GetOrCreate(definition);
            slot.chunk.AddEntity(entity, ref slot.index);
            index = slot.index;
            chunk = slot.chunk;
            TraceCreation(entity);
            NotifyCreation(entity);
            return entity;
        }

        /// <summary>
        /// Checks if the given <paramref name="entity"/> is compliant with the 
        /// definition of the <paramref name="archetype"/>.
        /// </summary>
        public readonly bool Is(uint entity, Archetype archetype)
        {
            return Is(entity, archetype.Definition);
        }

        /// <summary>
        /// Checks if the given <paramref name="entity"/> is compliant with the
        /// <paramref name="definition"/>.
        /// </summary>
        public readonly bool Is(uint entity, Definition definition)
        {
            ThrowIfEntityIsMissing(entity);

            Definition currentDefinition = world->slots[entity].chunk.Definition;
            if (!currentDefinition.componentTypes.ContainsAll(definition.componentTypes))
            {
                return false;
            }

            if (!currentDefinition.arrayTypes.ContainsAll(definition.arrayTypes))
            {
                return false;
            }

            return currentDefinition.tagTypes.ContainsAll(definition.tagTypes);
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

            Definition currentDefinition = world->slots[entity].chunk.chunk->definition;
            for (int i = 0; i < BitMask.Capacity; i++)
            {
                if (definition.ContainsComponent(i) && !currentDefinition.ContainsComponent(i))
                {
                    AddComponentType(entity, i);
                }

                if (definition.ContainsArray(i) && !currentDefinition.ContainsArray(i))
                {
                    CreateArray(entity, i);
                }

                if (definition.ContainsTag(i) && !currentDefinition.ContainsTag(i))
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

            Definition currentDefinition = world->slots[entity].chunk.chunk->definition;
            for (int i = 0; i < BitMask.Capacity; i++)
            {
                if (archetype.ContainsComponent(i) && !currentDefinition.ContainsComponent(i))
                {
                    AddComponentType(entity, i);
                }

                if (archetype.ContainsArray(i) && !currentDefinition.ContainsArray(i))
                {
                    CreateArray(entity, i);
                }

                if (archetype.ContainsTag(i) && !currentDefinition.ContainsTag(i))
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
                world->arrays.AddDefault(newEntities);
            }

            Chunk defaultChunk = world->chunks.chunkMap->defaultChunk;
            int created = 0;
            Span<Slot> slots = world->slots.AsSpan();
            for (int i = 0; i < freeEntities; i++)
            {
                uint entity = world->freeEntities.Pop();
                ref Slot slot = ref slots[(int)entity];
                slot.state = Slot.State.Enabled;
                slot.chunk = defaultChunk;
                slot.chunk.AddEntity(entity, ref slot.index);
                TraceCreation(entity);
                NotifyCreation(entity);
                destination[created++] = entity;
            }

            for (int i = 0; i < newEntities; i++)
            {
                uint entity = (uint)(i + startIndex);
                ref Slot slot = ref slots[(int)entity];
                slot.state = Slot.State.Enabled;
                slot.chunk = defaultChunk;
                slot.chunk.AddEntity(entity, ref slot.index);
                TraceCreation(entity);
                NotifyCreation(entity);
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

            return world->slots[entity].state != Slot.State.Free;
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
            ref Slot entitySlot = ref slots[(int)entity];
            bool parentChanged = entitySlot.parent != newParent;
            if (parentChanged)
            {
                uint oldParent = entitySlot.parent;
                entitySlot.parent = newParent;

                //remove from previous parent children list
                slots[(int)oldParent].childrenCount--; //old parent can be 0, which is ok

                ref Slot newParentSlot = ref slots[(int)newParent];
                if ((newParentSlot.flags & Slot.Flags.ContainsChildren) == 0)
                {
                    newParentSlot.childrenCount = 0;
                    newParentSlot.flags |= Slot.Flags.ContainsChildren;
                    newParentSlot.flags &= ~Slot.Flags.ChildrenOutdated;
                }
                else if ((newParentSlot.flags & Slot.Flags.ChildrenOutdated) != 0)
                {
                    newParentSlot.childrenCount = 0;
                    newParentSlot.flags &= ~Slot.Flags.ChildrenOutdated;
                }

                newParentSlot.childrenCount++;

                //update state if parent is disabled
                if (entitySlot.state == Slot.State.Enabled)
                {
                    if (newParentSlot.state == Slot.State.Disabled ||
                        newParentSlot.state == Slot.State.DisabledButLocallyEnabled)
                    {
                        entitySlot.state = Slot.State.DisabledButLocallyEnabled;
                    }
                }

                //move to different chunk if disabled state changed
                Chunk previousChunk = entitySlot.chunk;
                Definition previousDefinition = previousChunk.Definition;
                bool oldEnabled = !previousDefinition.tagTypes.Contains(Schema.DisabledTagType);
                bool newEnabled = entitySlot.state == Slot.State.Enabled;
                if (oldEnabled != newEnabled)
                {
                    Definition newDefinition = previousDefinition;
                    if (newEnabled)
                    {
                        newDefinition.RemoveTagType(Schema.DisabledTagType);
                    }
                    else
                    {
                        newDefinition.AddTagType(Schema.DisabledTagType);
                    }

                    if (entity != entitySlot.chunk.chunk->lastEntity)
                    {
                        slots[(int)entitySlot.chunk.chunk->lastEntity].index = entitySlot.index;
                    }

                    Chunk destinationChunk = world->chunks.GetOrCreate(newDefinition);
                    Chunk.MoveEntityAt(entity, ref entitySlot.index, ref entitySlot.chunk, destinationChunk);
                    world->version++;
                }

                NotifyParentChange(entity, oldParent, newParent);
            }

            return parentChanged;
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

            ref Slot slot = ref world->slots[entity];
            return world->references.AsSpan(slot.referenceStart, slot.referenceCount);
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

            Span<Slot> slots = world->slots.AsSpan();
            ref Slot slot = ref slots[(int)entity];
            List<uint> references = world->references;
            if (slot.referenceCount == 0)
            {
                slot.referenceStart = references.Count;
                references.Add(referencedEntity);
            }
            else
            {
                references.Insert(slot.referenceStart + slot.referenceCount, referencedEntity);

                //shift all other ranges over by 1
                for (int e = 1; e < slots.Length; e++)
                {
                    ref Slot currentSlot = ref slots[e];
                    if (currentSlot.referenceStart > slot.referenceStart)
                    {
                        currentSlot.referenceStart++;
                    }
                }
            }

            slot.referenceCount++;
            world->version++;
            return new(slot.referenceCount);
        }

        /// <summary>
        /// Updates an existing <paramref name="reference"/> to point towards the <paramref name="referencedEntity"/>.
        /// </summary>
        public readonly void SetReference(uint entity, rint reference, uint referencedEntity)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfReferenceIsMissing(entity, reference);

            world->references[world->slots[entity].referenceStart + reference.value - 1] = referencedEntity;
        }

        /// <summary>
        /// Checks if the given entity contains a reference to the given <paramref name="referencedEntity"/>.
        /// </summary>
        public readonly bool ContainsReference(uint entity, uint referencedEntity)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            ref Slot slot = ref world->slots[entity];
            ReadOnlySpan<uint> references = world->references.AsSpan(slot.referenceStart, slot.referenceCount);
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
                return (uint)world->slots[entity].referenceCount >= ((uint)reference.value - 1);
            }
        }

        /// <summary>
        /// Retrieves the number of references the given <paramref name="entity"/> has.
        /// </summary>
        public readonly int GetReferenceCount(uint entity)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            return world->slots[entity].referenceCount;
        }

        /// <summary>
        /// Retrieves the entity referenced at the given <paramref name="reference"/> index by <paramref name="entity"/>.
        /// </summary>
        public readonly uint GetReference(uint entity, rint reference)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfReferenceIsMissing(entity, reference);

            return world->references[world->slots[entity].referenceStart + reference.value - 1];
        }

        /// <summary>
        /// Retrieves the <see cref="rint"/> value that points to the given <paramref name="referencedEntity"/> on <paramref name="entity"/>.
        /// </summary>
        public readonly rint GetReference(uint entity, uint referencedEntity)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfReferencedEntityIsMissing(entity, referencedEntity);

            ref Slot slot = ref world->slots[entity];
            Span<uint> references = world->references.AsSpan(slot.referenceStart, slot.referenceCount);
            return new(references.IndexOf(referencedEntity) + 1);
        }

        /// <summary>
        /// Attempts to retrieve the referenced entity at the given <paramref name="reference"/> on <paramref name="entity"/>.
        /// </summary>
        public readonly bool TryGetReference(uint entity, rint reference, out uint referencedEntity)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            ref Slot slot = ref world->slots[entity];
            if (slot.referenceCount < reference.value)
            {
                referencedEntity = default;
                return false;
            }
            else
            {
                referencedEntity = world->references[slot.referenceStart + reference.value - 1];
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

            Span<Slot> slots = world->slots.AsSpan();
            ref Slot slot = ref slots[(int)entity];
            List<uint> references = world->references;
            slot.referenceCount--;
            references.RemoveAt(slot.referenceStart + reference.value - 1);

            //shift all other ranges back by 1
            for (int e = 1; e < slots.Length; e++)
            {
                ref Slot currentSlot = ref slots[e];
                if (currentSlot.referenceStart > slot.referenceStart)
                {
                    currentSlot.referenceStart--;
                }
            }

            world->version++;
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

            Span<Slot> slots = world->slots.AsSpan();
            ref Slot slot = ref slots[(int)entity];
            List<uint> references = world->references;
            int index = slot.referenceStart + reference.value - 1;
            slot.referenceCount--;
            referencedEntity = references[index];
            references.RemoveAt(index);

            //shift all other ranges back by 1
            for (int e = 1; e < slots.Length; e++)
            {
                ref Slot currentSlot = ref slots[e];
                if (currentSlot.referenceStart > slot.referenceStart)
                {
                    currentSlot.referenceStart--;
                }
            }

            world->version++;
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

            Span<Slot> slots = world->slots.AsSpan();
            ref Slot slot = ref slots[(int)entity];
            List<uint> references = world->references;
            Span<uint> referenceSpan = world->references.AsSpan(slot.referenceStart, slot.referenceCount);
            slot.referenceCount--;
            references.RemoveAt(referenceSpan.IndexOf(referencedEntity));

            //shift all other ranges back by 1
            for (int e = 1; e < slots.Length; e++)
            {
                ref Slot currentSlot = ref slots[e];
                if (currentSlot.referenceStart > slot.referenceStart)
                {
                    currentSlot.referenceStart--;
                }
            }

            world->version++;
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

            Span<Slot> slots = world->slots.AsSpan();
            ref Slot slot = ref slots[(int)entity];
            List<uint> references = world->references;
            Span<uint> referenceSpan = world->references.AsSpan(slot.referenceStart, slot.referenceCount);
            int index = referenceSpan.IndexOf(referencedEntity);
            slot.referenceCount--;
            references.RemoveAt(index);

            //shift all other ranges back by 1
            for (int e = 1; e < slots.Length; e++)
            {
                ref Slot currentSlot = ref slots[e];
                if (currentSlot.referenceStart > slot.referenceStart)
                {
                    currentSlot.referenceStart--;
                }
            }

            removedReference = new(index + 1);
            world->version++;
        }

        /// <summary>
        /// Checks if the given <paramref name="entity"/> contains a tag of type <typeparamref name="T"/>.
        /// </summary>
        public readonly bool ContainsTag<T>(uint entity) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            int tagType = world->schema.GetTagType<T>();
            return world->slots[entity].chunk.chunk->definition.tagTypes.Contains(tagType);
        }

        /// <summary>
        /// Checks if the given <paramref name="entity"/> contains the given <paramref name="tagType"/>.
        /// </summary>
        public readonly bool ContainsTag(uint entity, int tagType)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            return world->slots[entity].chunk.chunk->definition.tagTypes.Contains(tagType);
        }

        /// <summary>
        /// Adds a tag of type <typeparamref name="T"/> to the given <paramref name="entity"/>.
        /// </summary>
        public readonly void AddTag<T>(uint entity) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            int tagType = world->schema.GetTagType<T>();
            ThrowIfTagAlreadyPresent(entity, tagType);

            Span<Slot> slots = world->slots.AsSpan();
            ref Slot slot = ref slots[(int)entity];
            Definition newDefinition = slot.chunk.Definition;
            newDefinition.AddTagType(tagType);

            if (entity != slot.chunk.chunk->lastEntity)
            {
                slots[(int)slot.chunk.chunk->lastEntity].index = slot.index;
            }

            Chunk destinationChunk = world->chunks.GetOrCreate(newDefinition);
            Chunk.MoveEntityAt(entity, ref slot.index, ref slot.chunk, destinationChunk);
            world->version++;
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
            Definition newDefinition = slot.chunk.Definition;
            newDefinition.AddTagType(tagType);

            if (entity != slot.chunk.chunk->lastEntity)
            {
                slots[(int)slot.chunk.chunk->lastEntity].index = slot.index;
            }

            Chunk destinationChunk = world->chunks.GetOrCreate(newDefinition);
            Chunk.MoveEntityAt(entity, ref slot.index, ref slot.chunk, destinationChunk);
            world->version++;
            NotifyTagAdded(entity, tagType);
        }

        /// <summary>
        /// Removes the <typeparamref name="T"/> tag from the <paramref name="entity"/>.
        /// </summary>
        public readonly void RemoveTag<T>(uint entity) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            int tagType = world->schema.GetTagType<T>();
            ThrowIfTagIsMissing(entity, tagType);

            Span<Slot> slots = world->slots.AsSpan();
            ref Slot slot = ref slots[(int)entity];
            Definition newDefinition = slot.chunk.Definition;
            newDefinition.RemoveTagType(tagType);

            if (entity != slot.chunk.chunk->lastEntity)
            {
                slots[(int)slot.chunk.chunk->lastEntity].index = slot.index;
            }

            Chunk destinationChunk = world->chunks.GetOrCreate(newDefinition);
            Chunk.MoveEntityAt(entity, ref slot.index, ref slot.chunk, destinationChunk);
            world->version++;
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
            Definition newDefinition = slot.chunk.Definition;
            newDefinition.RemoveTagType(tagType);

            if (entity != slot.chunk.chunk->lastEntity)
            {
                slots[(int)slot.chunk.chunk->lastEntity].index = slot.index;
            }

            Chunk destinationChunk = world->chunks.GetOrCreate(newDefinition);
            Chunk.MoveEntityAt(entity, ref slot.index, ref slot.chunk, destinationChunk);
            world->version++;
            NotifyTagRemoved(entity, tagType);
        }

        /// <summary>
        /// Retrieves the types of all arrays on this entity.
        /// </summary>
        public readonly BitMask GetArrayTypes(uint entity)
        {
            ThrowIfEntityIsMissing(entity);

            return world->slots[entity].chunk.chunk->definition.arrayTypes;
        }

        /// <summary>
        /// Retrieves the types of all tags on this entity.
        /// </summary>
        public readonly BitMask GetTagTypes(uint entity)
        {
            ThrowIfEntityIsMissing(entity);

            return world->slots[entity].chunk.chunk->definition.tagTypes;
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
            int stride = world->schema.GetArraySize(arrayType);
            ref Slot slot = ref slots[(int)entity];
            ref Arrays arrays = ref world->arrays[entity];

            if ((slot.flags & Slot.Flags.ContainsArrays) == 0)
            {
                slot.flags |= Slot.Flags.ContainsArrays;
                slot.flags &= ~Slot.Flags.ArraysOutdated;
            }
            else if ((slot.flags & Slot.Flags.ArraysOutdated) != 0)
            {
                slot.flags &= ~Slot.Flags.ArraysOutdated;
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

            Definition newDefinition = slot.chunk.Definition;
            newDefinition.AddArrayType(arrayType);

            if (entity != slot.chunk.chunk->lastEntity)
            {
                slots[(int)slot.chunk.chunk->lastEntity].index = slot.index;
            }

            Chunk destinationChunk = world->chunks.GetOrCreate(newDefinition);
            Chunk.MoveEntityAt(entity, ref slot.index, ref slot.chunk, destinationChunk);
            world->version++;
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
            ref Arrays arrays = ref world->arrays[entity];

            if ((slot.flags & Slot.Flags.ContainsArrays) == 0)
            {
                slot.flags |= Slot.Flags.ContainsArrays;
                slot.flags &= ~Slot.Flags.ArraysOutdated;
            }
            else if ((slot.flags & Slot.Flags.ArraysOutdated) != 0)
            {
                slot.flags &= ~Slot.Flags.ArraysOutdated;
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

            Definition newDefinition = slot.chunk.Definition;
            newDefinition.AddArrayType(arrayType);

            if (entity != slot.chunk.chunk->lastEntity)
            {
                slots[(int)slot.chunk.chunk->lastEntity].index = slot.index;
            }

            Chunk destinationChunk = world->chunks.GetOrCreate(newDefinition);
            Chunk.MoveEntityAt(entity, ref slot.index, ref slot.chunk, destinationChunk);
            world->version++;
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
            ref Arrays arrays = ref world->arrays[entity];

            if ((slot.flags & Slot.Flags.ContainsArrays) == 0)
            {
                slot.flags |= Slot.Flags.ContainsArrays;
                slot.flags &= ~Slot.Flags.ArraysOutdated;
            }
            else if ((slot.flags & Slot.Flags.ArraysOutdated) != 0)
            {
                slot.flags &= ~Slot.Flags.ArraysOutdated;
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

            Definition newDefinition = slot.chunk.Definition;
            newDefinition.AddArrayType(dataType.index);

            if (entity != slot.chunk.chunk->lastEntity)
            {
                slots[(int)slot.chunk.chunk->lastEntity].index = slot.index;
            }

            Chunk destinationChunk = world->chunks.GetOrCreate(newDefinition);
            Chunk.MoveEntityAt(entity, ref slot.index, ref slot.chunk, destinationChunk);
            world->version++;
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

            int arrayType = world->schema.GetArrayType<T>();
            ThrowIfArrayIsAlreadyPresent(entity, arrayType);

            Span<Slot> slots = world->slots.AsSpan();
            ref Slot slot = ref slots[(int)entity];
            ref Arrays arrays = ref world->arrays[entity];

            if ((slot.flags & Slot.Flags.ContainsArrays) == 0)
            {
                slot.flags |= Slot.Flags.ContainsArrays;
                slot.flags &= ~Slot.Flags.ArraysOutdated;
            }
            else if ((slot.flags & Slot.Flags.ArraysOutdated) != 0)
            {
                slot.flags &= ~Slot.Flags.ArraysOutdated;
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

            Definition newDefinition = slot.chunk.Definition;
            newDefinition.AddArrayType(arrayType);

            if (entity != slot.chunk.chunk->lastEntity)
            {
                slots[(int)slot.chunk.chunk->lastEntity].index = slot.index;
            }

            Chunk destinationChunk = world->chunks.GetOrCreate(newDefinition);
            Chunk.MoveEntityAt(entity, ref slot.index, ref slot.chunk, destinationChunk);
            world->version++;
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

            int arrayType = world->schema.GetArrayType<T>();
            ThrowIfArrayIsAlreadyPresent(entity, arrayType);

            Span<Slot> slots = world->slots.AsSpan();
            ref Slot slot = ref slots[(int)entity];
            ref Arrays arrays = ref world->arrays[entity];

            if ((slot.flags & Slot.Flags.ContainsArrays) == 0)
            {
                slot.flags |= Slot.Flags.ContainsArrays;
                slot.flags &= ~Slot.Flags.ArraysOutdated;
            }
            else if ((slot.flags & Slot.Flags.ArraysOutdated) != 0)
            {
                slot.flags &= ~Slot.Flags.ArraysOutdated;
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

            Definition newDefinition = slot.chunk.Definition;
            newDefinition.AddArrayType(arrayType);

            if (entity != slot.chunk.chunk->lastEntity)
            {
                slots[(int)slot.chunk.chunk->lastEntity].index = slot.index;
            }

            Chunk destinationChunk = world->chunks.GetOrCreate(newDefinition);
            Chunk.MoveEntityAt(entity, ref slot.index, ref slot.chunk, destinationChunk);
            world->version++;
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

            int arrayType = world->schema.GetArrayType<T>();
            ThrowIfArrayIsAlreadyPresent(entity, arrayType);

            Span<Slot> slots = world->slots.AsSpan();
            ref Slot slot = ref slots[(int)entity];
            ref Arrays arrays = ref world->arrays[entity];

            if ((slot.flags & Slot.Flags.ContainsArrays) == 0)
            {
                slot.flags |= Slot.Flags.ContainsArrays;
                slot.flags &= ~Slot.Flags.ArraysOutdated;
            }
            else if ((slot.flags & Slot.Flags.ArraysOutdated) != 0)
            {
                slot.flags &= ~Slot.Flags.ArraysOutdated;
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

            Definition newDefinition = slot.chunk.Definition;
            newDefinition.AddArrayType(arrayType);

            if (entity != slot.chunk.chunk->lastEntity)
            {
                slots[(int)slot.chunk.chunk->lastEntity].index = slot.index;
            }

            Chunk destinationChunk = world->chunks.GetOrCreate(newDefinition);
            Chunk.MoveEntityAt(entity, ref slot.index, ref slot.chunk, destinationChunk);
            world->version++;
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

            int arrayType = world->schema.GetArrayType<T>();
            return world->slots[entity].chunk.chunk->definition.arrayTypes.Contains(arrayType);
        }

        /// <summary>
        /// Checks if the given <paramref name="entity"/> contains an array of the given <paramref name="arrayType"/>.
        /// </summary>
        public readonly bool ContainsArray(uint entity, int arrayType)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            return world->slots[entity].chunk.chunk->definition.arrayTypes.Contains(arrayType);
        }

        /// <summary>
        /// Retrieves the array of type <typeparamref name="T"/> from the given <paramref name="entity"/>.
        /// </summary>
        public readonly Values<T> GetArray<T>(uint entity) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            int arrayType = world->schema.GetArrayType<T>();
            ThrowIfArrayIsMissing(entity, arrayType);

            return new(world->arrays[entity][arrayType].pointer);
        }

        /// <summary>
        /// Retrieves the array of type <typeparamref name="T"/> from the given <paramref name="entity"/>.
        /// </summary>
        public readonly Span<T> GetArrayOrDefault<T>(uint entity) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            int arrayType = world->schema.GetArrayType<T>();
            Collections.Pointers.ArrayPointer* pointer = world->arrays[entity][arrayType].pointer;
            return pointer == default ? default : new(pointer->items.Pointer, pointer->length);
        }

        /// <summary>
        /// Retrieves the array of type <typeparamref name="T"/> from the given <paramref name="entity"/>.
        /// </summary>
        public readonly Values<T> GetArray<T>(uint entity, int arrayType) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfArrayIsMissing(entity, arrayType);

            return new(world->arrays[entity][arrayType].pointer);
        }

        /// <summary>
        /// Retrieves the array of type <typeparamref name="T"/> from the given <paramref name="entity"/>.
        /// </summary>
        public readonly Span<T> GetArrayOrDefault<T>(uint entity, int arrayType) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            Collections.Pointers.ArrayPointer* pointer = world->arrays[entity][arrayType].pointer;
            return pointer == default ? default : new(pointer->items.Pointer, pointer->length);
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

            int arrayType = world->schema.GetArrayType<T>();
            if (world->slots[entity].chunk.chunk->definition.arrayTypes.Contains(arrayType))
            {
                array = new(world->arrays[entity][arrayType].pointer);
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

            int arrayType = world->schema.GetArrayType<T>();
            ThrowIfArrayIsMissing(entity, arrayType);

            return ref world->arrays[entity][arrayType].Get<T>(index);
        }

        /// <summary>
        /// Retrieves the length of an existing array on this entity.
        /// </summary>
        public readonly int GetArrayLength<T>(uint entity) where T : unmanaged
        {
            ThrowIfEntityIsMissing(entity);

            int arrayType = world->schema.GetArrayType<T>();
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
        /// Destroys the array of type <typeparamref name="T"/> on the given <paramref name="entity"/>.
        /// </summary>
        public readonly void DestroyArray<T>(uint entity) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            int arrayType = world->schema.GetArrayType<T>();
            ThrowIfArrayIsMissing(entity, arrayType);

            Span<Slot> slots = world->slots.AsSpan();
            ref Slot slot = ref slots[(int)entity];
            ref Arrays arrays = ref world->arrays[entity];
            Values array = arrays[arrayType];
            array.Dispose();
            arrays[arrayType] = default;

            Definition newDefinition = slot.chunk.Definition;
            newDefinition.RemoveArrayType(arrayType);

            if (entity != slot.chunk.chunk->lastEntity)
            {
                slots[(int)slot.chunk.chunk->lastEntity].index = slot.index;
            }

            Chunk destinationChunk = world->chunks.GetOrCreate(newDefinition);
            Chunk.MoveEntityAt(entity, ref slot.index, ref slot.chunk, destinationChunk);
            world->version++;
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

            Definition newDefinition = slot.chunk.Definition;
            newDefinition.RemoveArrayType(arrayType);

            if (entity != slot.chunk.chunk->lastEntity)
            {
                slots[(int)slot.chunk.chunk->lastEntity].index = slot.index;
            }

            Chunk destinationChunk = world->chunks.GetOrCreate(newDefinition);
            Chunk.MoveEntityAt(entity, ref slot.index, ref slot.chunk, destinationChunk);
            world->version++;
            NotifyArrayDestroyed(entity, arrayType);
        }

        /// <summary>
        /// Adds a new <typeparamref name="T"/> component to the given <paramref name="entity"/>.
        /// </summary>
        public readonly void AddComponent<T>(uint entity, T component) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            int componentType = world->schema.GetComponentType<T>();
            ThrowIfComponentAlreadyPresent(entity, componentType);

            Span<Slot> slots = world->slots.AsSpan();
            ref Slot slot = ref slots[(int)entity];

            if (entity != slot.chunk.chunk->lastEntity)
            {
                slots[(int)slot.chunk.chunk->lastEntity].index = slot.index;
            }

            Definition definition = slot.chunk.Definition;
            definition.AddComponentType(componentType);
            Chunk destinationChunk = world->chunks.GetOrCreate(definition);
            Chunk.MoveEntityAt(entity, ref slot.index, ref slot.chunk, destinationChunk);
            world->version++;
            destinationChunk.SetComponent(slot.index, componentType, component);
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

            if (entity != slot.chunk.chunk->lastEntity)
            {
                slots[(int)slot.chunk.chunk->lastEntity].index = slot.index;
            }

            Definition definition = slot.chunk.Definition;
            definition.AddComponentType(componentType);
            Chunk destinationChunk = world->chunks.GetOrCreate(definition);
            Chunk.MoveEntityAt(entity, ref slot.index, ref slot.chunk, destinationChunk);
            world->version++;
            destinationChunk.SetComponent(slot.index, componentType, component);
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

            int componentType = world->schema.GetComponentType<T>();
            ThrowIfComponentAlreadyPresent(entity, componentType);

            Span<Slot> slots = world->slots.AsSpan();
            ref Slot slot = ref slots[(int)entity];

            if (entity != slot.chunk.chunk->lastEntity)
            {
                slots[(int)slot.chunk.chunk->lastEntity].index = slot.index;
            }

            Definition definition = slot.chunk.Definition;
            definition.AddComponentType(componentType);
            Chunk destinationChunk = world->chunks.GetOrCreate(definition);
            Chunk.MoveEntityAt(entity, ref slot.index, ref slot.chunk, destinationChunk);
            world->version++;
            NotifyComponentAdded(entity, componentType);
            return ref destinationChunk.GetComponent<T>(slot.index, componentType);
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

            if (entity != slot.chunk.chunk->lastEntity)
            {
                slots[(int)slot.chunk.chunk->lastEntity].index = slot.index;
            }

            Definition definition = slot.chunk.Definition;
            definition.AddComponentType(componentType);
            Chunk destinationChunk = world->chunks.GetOrCreate(definition);
            Chunk.MoveEntityAt(entity, ref slot.index, ref slot.chunk, destinationChunk);
            world->version++;
            NotifyComponentAdded(entity, componentType);
            return ref destinationChunk.GetComponent<T>(slot.index, componentType);
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

            if (entity != slot.chunk.chunk->lastEntity)
            {
                slots[(int)slot.chunk.chunk->lastEntity].index = slot.index;
            }

            Definition definition = slot.chunk.Definition;
            definition.AddComponentType(componentType);
            Chunk destinationChunk = world->chunks.GetOrCreate(definition);
            Chunk.MoveEntityAt(entity, ref slot.index, ref slot.chunk, destinationChunk);
            world->version++;
            NotifyComponentAdded(entity, componentType);
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

            if (entity != slot.chunk.chunk->lastEntity)
            {
                slots[(int)slot.chunk.chunk->lastEntity].index = slot.index;
            }

            Definition definition = slot.chunk.Definition;
            definition.AddComponentType(componentType);
            Chunk destinationChunk = world->chunks.GetOrCreate(definition);
            Chunk.MoveEntityAt(entity, ref slot.index, ref slot.chunk, destinationChunk);
            world->version++;
            NotifyComponentAdded(entity, componentType);
            component = destinationChunk.GetComponent(slot.index, componentType);
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

            if (entity != slot.chunk.chunk->lastEntity)
            {
                slots[(int)slot.chunk.chunk->lastEntity].index = slot.index;
            }

            Definition definition = slot.chunk.Definition;
            definition.AddComponentType(componentType);
            Chunk destinationChunk = world->chunks.GetOrCreate(definition);
            Chunk.MoveEntityAt(entity, ref slot.index, ref slot.chunk, destinationChunk);
            world->version++;
            NotifyComponentAdded(entity, componentType);
            return destinationChunk.GetComponent(slot.index, componentType, out componentSize);
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

            if (entity != slot.chunk.chunk->lastEntity)
            {
                slots[(int)slot.chunk.chunk->lastEntity].index = slot.index;
            }

            Definition definition = slot.chunk.Definition;
            definition.AddComponentType(componentType);
            Chunk destinationChunk = world->chunks.GetOrCreate(definition);
            Chunk.MoveEntityAt(entity, ref slot.index, ref slot.chunk, destinationChunk);
            world->version++;
            MemoryAddress component = destinationChunk.GetComponent(slot.index, componentType, out int componentSize);

            //todo: efficiency: this could be eliminated, but would need awareness given to the user about the size of the component
            Span<byte> destination = component.GetSpan(Math.Min(componentSize, componentBytes.Length));
            componentBytes.CopyTo(destination);
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

            if (entity != slot.chunk.chunk->lastEntity)
            {
                slots[(int)slot.chunk.chunk->lastEntity].index = slot.index;
            }

            Definition definition = slot.chunk.Definition;
            definition.AddComponentType(componentType);
            Chunk destinationChunk = world->chunks.GetOrCreate(definition);
            Chunk.MoveEntityAt(entity, ref slot.index, ref slot.chunk, destinationChunk);
            world->version++;
            return destinationChunk.GetComponentBytes(slot.index, componentType);
        }

        /// <summary>
        /// Removes the component of type <typeparamref name="T"/> from the given <paramref name="entity"/>.
        /// </summary>
        public readonly void RemoveComponent<T>(uint entity) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            int componentType = world->schema.GetComponentType<T>();
            ThrowIfComponentMissing(entity, componentType);

            Span<Slot> slots = world->slots.AsSpan();
            ref Slot slot = ref slots[(int)entity];

            if (entity != slot.chunk.chunk->lastEntity)
            {
                slots[(int)slot.chunk.chunk->lastEntity].index = slot.index;
            }

            Definition definition = slot.chunk.Definition;
            definition.RemoveComponentType(componentType);
            Chunk destinationChunk = world->chunks.GetOrCreate(definition);
            Chunk.MoveEntityAt(entity, ref slot.index, ref slot.chunk, destinationChunk);
            world->version++;
            NotifyComponentRemoved(entity, componentType);
        }

        /// <summary>
        /// Removes the component of the given <paramref name="componentType"/> from the given <paramref name="entity"/>.
        /// </summary>
        public readonly void RemoveComponent(uint entity, int componentType)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfComponentMissing(entity, componentType);

            Span<Slot> slots = world->slots.AsSpan();
            ref Slot slot = ref slots[(int)entity];

            if (entity != slot.chunk.chunk->lastEntity)
            {
                slots[(int)slot.chunk.chunk->lastEntity].index = slot.index;
            }

            Definition definition = slot.chunk.Definition;
            definition.RemoveComponentType(componentType);
            Chunk destinationChunk = world->chunks.GetOrCreate(definition);
            Chunk.MoveEntityAt(entity, ref slot.index, ref slot.chunk, destinationChunk);
            world->version++;
            NotifyComponentRemoved(entity, componentType);
        }

        /// <summary>
        /// Checks if any entity in this world contains a component
        /// of type <typeparamref name="T"/>.
        /// </summary>
        public readonly bool ContainsAnyComponent<T>() where T : unmanaged
        {
            int componentType = world->schema.GetComponentType<T>();
            foreach (Chunk chunk in world->chunks.chunkMap->chunks)
            {
                if (chunk.Definition.componentTypes.Contains(componentType))
                {
                    if (chunk.Count > 0)
                    {
                        return true;
                    }
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

            int componentType = world->schema.GetComponentType<T>();
            return world->slots[entity].chunk.chunk->definition.componentTypes.Contains(componentType);
        }

        /// <summary>
        /// Checks if the given <paramref name="entity"/> contains the <paramref name="componentType"/>.
        /// </summary>
        public readonly bool ContainsComponent(uint entity, int componentType)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            return world->slots[entity].chunk.chunk->definition.componentTypes.Contains(componentType);
        }

        /// <summary>
        /// Retrieves a reference to the component of type <typeparamref name="T"/> from the given <paramref name="entity"/>.
        /// </summary>
        public readonly ref T GetComponent<T>(uint entity) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            int componentType = world->schema.GetComponentType<T>();
            ThrowIfComponentMissing(entity, componentType);

            ref Slot slot = ref world->slots[entity];
            return ref slot.chunk.GetComponent<T>(slot.index, componentType);
        }

        /// <summary>
        /// Retrieves the component of type <typeparamref name="T"/> if it exists, otherwise the given
        /// <paramref name="defaultValue"/> is returned.
        /// </summary>
        public readonly T GetComponentOrDefault<T>(uint entity, T defaultValue = default) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            int componentType = world->schema.GetComponentType<T>();
            ref Slot slot = ref world->slots[entity];
            if (world->slots[entity].chunk.chunk->definition.componentTypes.Contains(componentType))
            {
                return slot.chunk.GetComponent<T>(slot.index, componentType);
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

            ref Slot slot = ref world->slots[entity];
            return ref slot.chunk.GetComponent<T>(slot.index, componentType);
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

            ref Slot slot = ref world->slots[entity];
            return slot.chunk.GetComponent(slot.index, componentType);
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

            ref Slot slot = ref world->slots[entity];
            return slot.chunk.GetComponent(slot.index, componentType, out componentSize);
        }

        /// <summary>
        /// Fetches the bytes of a component from the given <paramref name="entity"/>.
        /// </summary>
        public readonly Span<byte> GetComponentBytes(uint entity, int componentType)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfComponentMissing(entity, componentType);

            ref Slot slot = ref world->slots[entity];
            MemoryAddress component = slot.chunk.GetComponent(slot.index, componentType, out int componentSize);
            return new(component.Pointer, componentSize);
        }

        /// <summary>
        /// Retrieves the component from the given <paramref name="entity"/> as an <see cref="object"/>.
        /// </summary>
        public readonly object GetComponentObject(uint entity, int componentType)
        {
            MemoryAddress.ThrowIfDefault(world);

            Types.Type layout = world->schema.GetComponentLayout(componentType);
            Span<byte> bytes = GetComponentBytes(entity, componentType);
            return layout.CreateInstance(bytes);
        }

        /// <summary>
        /// Retrieves the array from the given <paramref name="entity"/> as <see cref="object"/>s.
        /// </summary>
        public readonly object[] GetArrayObject(uint entity, int arrayType)
        {
            MemoryAddress.ThrowIfDefault(world);

            Types.Type layout = world->schema.GetArrayLayout(arrayType);
            Values array = GetArray(entity, arrayType);
            object[] arrayObject = new object[array.Length];
            for (int i = 0; i < array.Length; i++)
            {
                MemoryAddress allocation = array[i];
                arrayObject[i] = layout.CreateInstance(new(allocation.Pointer, layout.size));
            }

            return arrayObject;
        }

        /// <summary>
        /// Attempts to retrieve a reference to the component of type <typeparamref name="T"/> from the given <paramref name="entity"/>.
        /// </summary>
        /// <returns><c>true</c> if the component is found.</returns>
        public readonly ref T TryGetComponent<T>(uint entity, int componentType, out bool contains) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(world);

            ref Slot slot = ref world->slots[entity];
            contains = slot.chunk.Definition.componentTypes.Contains(componentType);
            if (contains)
            {
                return ref slot.chunk.GetComponent<T>(slot.index, componentType);
            }
            else
            {
                return ref *(T*)default(nint);
            }
        }

        /// <summary>
        /// Attempts to retrieve a reference to the component of type <typeparamref name="T"/> from the given <paramref name="entity"/>.
        /// </summary>
        /// <returns><c>true</c> if the component is found.</returns>
        public readonly ref T TryGetComponent<T>(uint entity, out bool contains) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(world);

            int componentType = world->schema.GetComponentType<T>();
            ref Slot slot = ref world->slots[entity];
            contains = slot.chunk.Definition.componentTypes.Contains(componentType);
            if (contains)
            {
                return ref slot.chunk.GetComponent<T>(slot.index, componentType);
            }
            else
            {
                return ref *(T*)default(nint);
            }
        }

        /// <summary>
        /// Attempts to retrieve the component of type <typeparamref name="T"/> from the given <paramref name="entity"/>.
        /// </summary>
        /// <returns><c>true</c> if found.</returns>
        public readonly bool TryGetComponent<T>(uint entity, int componentType, out T component) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(world);

            ref Slot slot = ref world->slots[entity];
            if (slot.chunk.Definition.componentTypes.Contains(componentType))
            {
                component = slot.chunk.GetComponent<T>(slot.index, componentType);
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

            int componentType = world->schema.GetComponentType<T>();
            ref Slot slot = ref world->slots[entity];
            if (slot.chunk.Definition.componentTypes.Contains(componentType))
            {
                component = slot.chunk.GetComponent<T>(slot.index, componentType);
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

            int componentType = world->schema.GetComponentType<T>();
            ThrowIfComponentMissing(entity, componentType);

            ref Slot slot = ref world->slots[entity];
            slot.chunk.SetComponent(slot.index, componentType, component);
        }

        /// <summary>
        /// Assigns the given <paramref name="componentBytes"/> to the given <paramref name="entity"/>.
        /// </summary>
        public readonly void SetComponentBytes(uint entity, int componentType, ReadOnlySpan<byte> componentBytes)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfComponentMissing(entity, componentType);

            ref Slot slot = ref world->slots[entity];
            MemoryAddress component = slot.chunk.GetComponent(slot.index, componentType, out int componentSize);
            Span<byte> destination = component.GetSpan(Math.Min(componentSize, componentBytes.Length));
            componentBytes.CopyTo(destination);
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
        /// Returns the chunk that contains the given <paramref name="entity"/>,
        /// along with the local index.
        /// </summary>
        public readonly Chunk GetChunk(uint entity, out int index)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            ref Slot slot = ref world->slots[entity];
            index = slot.index;
            return slot.chunk;
        }

        /// <summary>
        /// Retrieves the definition of the given <paramref name="entity"/>.
        /// </summary>
        public readonly Definition GetDefinition(uint entity)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            return world->slots[entity].chunk.chunk->definition;
        }

        /// <summary>
        /// Retrieves the definition of the given <paramref name="entity"/>.
        /// </summary>
        public readonly Definition GetDefinitionOrDefault(uint entity)
        {
            MemoryAddress.ThrowIfDefault(world);

            ref Slot slot = ref world->slots[entity];
            if (slot.state == Slot.State.Free)
            {
                return default;
            }

            return slot.chunk.chunk->definition;
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

            Slot sourceSlot = world->slots[(int)sourceEntity];
            Slot destinationSlot = destinationWorld.world->slots[(int)destinationEntity];
            for (int c = 0; c < BitMask.Capacity; c++)
            {
                if (sourceSlot.chunk.chunk->definition.componentTypes.Contains(c))
                {
                    Span<byte> destinationBytes;
                    if (!destinationSlot.chunk.chunk->definition.componentTypes.Contains(c))
                    {
                        destinationBytes = destinationWorld.AddComponentBytes(destinationEntity, c);
                    }
                    else
                    {
                        destinationBytes = destinationSlot.chunk.GetComponentBytes(destinationSlot.index, c);
                    }

                    Span<byte> sourceBytes = sourceSlot.chunk.GetComponentBytes(sourceSlot.index, c);
                    sourceBytes.CopyTo(destinationBytes);
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
        private readonly void ThrowIfEntityIsMissing(uint entity)
        {
            if (entity == default)
            {
                throw new InvalidOperationException($"Entity `{entity}` is not valid");
            }

            if (entity >= world->slots.Count)
            {
                throw new EntityIsMissingException(this, entity);
            }

            ref Slot.State state = ref world->slots[entity].state;
            if (state == Slot.State.Free)
            {
                throw new EntityIsMissingException(this, entity);
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfParentIsSameAsChild(uint entity, uint parent)
        {
            if (entity == parent)
            {
                throw new InvalidOperationException($"Entity `{entity}` cannot be its own parent");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfReferenceIsMissing(uint entity, rint reference)
        {
            ref Slot slot = ref world->slots[entity];
            if (reference.value == 0 || reference.value > slot.referenceCount)
            {
                throw new ReferenceToEntityIsMissingException(this, entity, reference);
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfReferencedEntityIsMissing(uint entity, uint referencedEntity)
        {
            ref Slot slot = ref world->slots[entity];
            Span<uint> references = world->references.AsSpan(slot.referenceStart, slot.referenceCount);
            if (!references.Contains(referencedEntity))
            {
                throw new ReferenceToEntityIsMissingException(this, entity, referencedEntity);
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfComponentMissing(uint entity, int componentType)
        {
            BitMask componentTypes = world->slots[entity].chunk.chunk->definition.componentTypes;
            if (!componentTypes.Contains(componentType))
            {
                throw new ComponentIsMissingException(this, entity, componentType);
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfComponentAlreadyPresent(uint entity, int componentType)
        {
            BitMask componentTypes = world->slots[entity].chunk.chunk->definition.componentTypes;
            if (componentTypes.Contains(componentType))
            {
                throw new ComponentIsAlreadyPresentException(this, entity, componentType);
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfTagAlreadyPresent(uint entity, int tagType)
        {
            BitMask tagTypes = world->slots[entity].chunk.chunk->definition.tagTypes;
            if (tagTypes.Contains(tagType))
            {
                throw new TagIsAlreadyPresentException(this, entity, tagType);
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfTagIsMissing(uint entity, int tagType)
        {
            BitMask tagTypes = world->slots[entity].chunk.chunk->definition.tagTypes;
            if (!tagTypes.Contains(tagType))
            {
                throw new TagIsMissingException(this, entity, tagType);
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfArrayIsMissing(uint entity, int arrayType)
        {
            BitMask arrayTypes = world->slots[entity].chunk.chunk->definition.arrayTypes;
            if (!arrayTypes.Contains(arrayType))
            {
                throw new ArrayIsMissingException(this, entity, arrayType);
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfArrayIsAlreadyPresent(uint entity, int arrayType)
        {
            BitMask arrayTypes = world->slots[entity].chunk.chunk->definition.arrayTypes;
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
        public static World Deserialize(ByteReader reader, Func<Types.Type, DataType.Kind, Types.Type>? process)
        {
            Signature signature = reader.ReadValue<Signature>();
            if (signature.Version != DataVersion)
            {
                throw new InvalidOperationException($"Invalid version `{signature.Version}` expected `{DataVersion}`");
            }

            //deserialize the schema first
            Schema schema;
            if (process is not null)
            {
                schema = Schema.Create();
                using Schema loadedSchema = reader.ReadObject<Schema>();
                foreach (int componentType in loadedSchema.ComponentTypes)
                {
                    Types.Type typeLayout = loadedSchema.GetComponentLayout(componentType);
                    typeLayout = process.Invoke(typeLayout, DataType.Kind.Component);
                    schema.RegisterComponent(typeLayout);
                }

                foreach (int arrayType in loadedSchema.ArrayTypes)
                {
                    Types.Type typeLayout = loadedSchema.GetArrayLayout(arrayType);
                    typeLayout = process.Invoke(typeLayout, DataType.Kind.Array);
                    schema.RegisterArray(typeLayout);
                }

                foreach (int tagType in loadedSchema.TagTypes)
                {
                    Types.Type typeLayout = loadedSchema.GetTagLayout(tagType);
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

            //todo: this could be a stackalloc span instead
            using Array<uint> entityMap = new(maxSlotCount + 1);

            for (uint e = 0; e < entityCount; e++)
            {
                uint entity = reader.ReadValue<uint>();
                Slot.State state = reader.ReadValue<Slot.State>();
                uint parent = reader.ReadValue<uint>();

                uint createdEntity = value.CreateEntity();
                entityMap[(int)entity] = createdEntity;
                ref Slot slot = ref value.world->slots[(int)createdEntity];
                slot.state = state;
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
            List<uint> references = value.world->references;
            for (uint e = 1; e < slots.Length; e++)
            {
                ref Slot slot = ref slots[(int)e];
                if (slot.state == Slot.State.Free)
                {
                    continue;
                }

                int referenceCount = reader.ReadValue<int>();
                if (referenceCount > 0)
                {
                    slot.referenceStart = references.Count;
                    slot.referenceCount = referenceCount;
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
                    ref Slot parentSlot = ref value.world->slots[(int)parent];
                    if ((parentSlot.flags & Slot.Flags.ContainsChildren) == 0)
                    {
                        parentSlot.flags |= Slot.Flags.ContainsChildren;
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