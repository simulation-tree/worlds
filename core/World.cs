using Collections.Generic;
using System;
using System.Diagnostics;
using Types;
using Unmanaged;
using Worlds.Functions;
using Array = Collections.Array;
using Pointer = Worlds.Pointers.World;

namespace Worlds
{
    /// <summary>
    /// Contains arbitrary data sorted into groups of entities for processing.
    /// </summary>
    public unsafe struct World : IDisposable, IEquatable<World>, ISerializable
    {
#if DEBUG
        internal static readonly System.Collections.Generic.Dictionary<Entity, StackTrace> createStackTraces = new();
#endif

        public const uint Version = 1;

        private Pointer* world;

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
        /// Checks if the world has been disposed.
        /// </summary>
        public readonly bool IsDisposed => world is null;

        /// <summary>
        /// All available slots.
        /// </summary>
        private readonly ReadOnlySpan<Slot> Slots
        {
            get
            {
                MemoryAddress.ThrowIfDefault(world);

                return world->slots.AsSpan();
            }
        }

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
        /// Dictionary mapping <see cref="Definition"/>s to <see cref="Chunk"/>s.
        /// </summary>
        public readonly Dictionary<Definition, Chunk> ChunksMap
        {
            get
            {
                MemoryAddress.ThrowIfDefault(world);

                return world->chunksMap;
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

                return world->uniqueChunks.AsSpan();
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
            ref Pointer world = ref MemoryAddress.Allocate<Pointer>();
            world = new(new());
            fixed (Pointer* pointer = &world)
            {
                this.world = pointer;
            }
        }
#endif

        /// <summary>
        /// Creates a new world with the given <paramref name="schema"/>.
        /// </summary>
        public World(Schema schema)
        {
            ref Pointer world = ref MemoryAddress.Allocate<Pointer>();
            world = new(schema);
            fixed (Pointer* pointer = &world)
            {
                this.world = pointer;
            }
        }

        /// <summary>
        /// Initializes an existing world from the given <paramref name="pointer"/>.
        /// </summary>
        public World(void* pointer)
        {
            this.world = (Pointer*)pointer;
        }

        /// <summary>
        /// Disposes everything in the world and the world itself.
        /// </summary>
        public void Dispose()
        {
            MemoryAddress.ThrowIfDefault(world);

            Clear();

            Span<Slot> slots = world->slots.AsSpan();
            for (int e = 1; e < slots.Length; e++)
            {
                Slot slot = slots[e];
                if (slot.ContainsReferences)
                {
                    slot.references.Dispose();
                }

                if (slot.ContainsArrays)
                {
                    for (int a = 0; a < BitMask.Capacity; a++)
                    {
                        Values array = slot.arrays[a];
                        if (array != default)
                        {
                            array.Dispose();
                        }
                    }

                    slot.arrays.Dispose();
                }
            }

            world->entityCreatedOrDestroyed.Dispose();
            world->entityParentChanged.Dispose();
            world->entityDataChanged.Dispose();
            world->schema.Dispose();
            world->freeEntities.Dispose();
            world->chunksMap.Dispose();
            world->uniqueChunks.Dispose();
            world->slots.Dispose();
            MemoryAddress.Free(ref world);
        }

        /// <summary>
        /// Resets the world to <see langword="default"/> state.
        /// </summary>
        public readonly void Clear()
        {
            MemoryAddress.ThrowIfDefault(world);

            Span<Chunk> uniqueChunks = world->uniqueChunks.AsSpan();
            for (int i = 0; i < uniqueChunks.Length; i++)
            {
                uniqueChunks[i].Dispose();
            }

            world->uniqueChunks.Clear();

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
                slot.state = Slot.State.Free;
                world->freeEntities.Push(e);
            }

            world->chunksMap.Clear();
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
            List<(EntityCreatedOrDestroyed, ulong)> events = world->entityCreatedOrDestroyed;
            for (int i = 0; i < events.Count; i++)
            {
                (EntityCreatedOrDestroyed callback, ulong userData) = events[i];
                callback.Invoke(this, entity, ChangeType.Added, userData);
            }
        }

        private readonly void NotifyParentChange(uint entity, uint oldParent, uint newParent)
        {
            List<(EntityParentChanged, ulong)> events = world->entityParentChanged;
            for (int i = 0; i < events.Count; i++)
            {
                (EntityParentChanged callback, ulong userData) = events[i];
                callback.Invoke(this, entity, oldParent, newParent, userData);
            }
        }

        private readonly void NotifyComponentAdded(uint entity, int componentType)
        {
            List<(EntityDataChanged, ulong)> events = world->entityDataChanged;
            if (events.Count > 0)
            {
                DataType type = world->schema.GetComponentDataType(componentType);
                for (int i = 0; i < events.Count; i++)
                {
                    (EntityDataChanged callback, ulong userData) = events[i];
                    callback.Invoke(this, entity, type, ChangeType.Added, userData);
                }
            }
        }

        private readonly void NotifyComponentRemoved(uint entity, int componentType)
        {
            List<(EntityDataChanged, ulong)> events = world->entityDataChanged;
            if (events.Count > 0)
            {
                DataType type = world->schema.GetComponentDataType(componentType);
                for (int i = 0; i < events.Count; i++)
                {
                    (EntityDataChanged callback, ulong userData) = events[i];
                    callback.Invoke(this, entity, type, ChangeType.Removed, userData);
                }
            }
        }

        private readonly void NotifyArrayCreated(uint entity, int arrayType)
        {
            List<(EntityDataChanged, ulong)> events = world->entityDataChanged;
            if (events.Count > 0)
            {
                DataType type = world->schema.GetArrayDataType(arrayType);
                for (int i = 0; i < events.Count; i++)
                {
                    (EntityDataChanged callback, ulong userData) = events[i];
                    callback.Invoke(this, entity, type, ChangeType.Added, userData);
                }
            }
        }

        private readonly void NotifyArrayDestroyed(uint entity, int arrayType)
        {
            List<(EntityDataChanged, ulong)> events = world->entityDataChanged;
            if (events.Count > 0)
            {
                DataType type = world->schema.GetArrayDataType(arrayType);
                for (int i = 0; i < events.Count; i++)
                {
                    (EntityDataChanged callback, ulong userData) = events[i];
                    callback.Invoke(this, entity, type, ChangeType.Removed, userData);
                }
            }
        }

        private readonly void NotifyTagAdded(uint entity, int tagType)
        {
            List<(EntityDataChanged, ulong)> events = world->entityDataChanged;
            if (events.Count > 0)
            {
                DataType type = world->schema.GetTagDataType(tagType);
                for (int i = 0; i < events.Count; i++)
                {
                    (EntityDataChanged callback, ulong userData) = events[i];
                    callback.Invoke(this, entity, type, ChangeType.Added, userData);
                }
            }
        }

        private readonly void NotifyTagRemoved(uint entity, int tagType)
        {
            List<(EntityDataChanged, ulong)> events = world->entityDataChanged;
            if (events.Count > 0)
            {
                DataType type = world->schema.GetTagDataType(tagType);
                for (int i = 0; i < events.Count; i++)
                {
                    (EntityDataChanged callback, ulong userData) = events[i];
                    callback.Invoke(this, entity, type, ChangeType.Removed, userData);
                }
            }
        }

        readonly void ISerializable.Write(ByteWriter writer)
        {
            MemoryAddress.ThrowIfDefault(world);

            writer.WriteValue(new Signature(Version));
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
                writer.WriteValue((byte)definition.ComponentTypes.Count);
                for (int c = 0; c < BitMask.Capacity; c++)
                {
                    if (definition.ComponentTypes.Contains(c))
                    {
                        writer.WriteValue((byte)c);
                        Span<byte> componentBytes = GetComponentBytes(e, c);
                        writer.WriteSpan(componentBytes);
                    }
                }

                //write arrays
                writer.WriteValue((byte)definition.ArrayTypes.Count);
                for (int a = 0; a < BitMask.Capacity; a++)
                {
                    if (definition.ArrayTypes.Contains(a))
                    {
                        writer.WriteValue((byte)a);
                        Values array = GetArray(e, a);
                        writer.WriteValue(array.Length);
                        writer.WriteSpan(array.AsSpan());
                    }
                }

                //write tags
                writer.WriteValue((byte)definition.TagTypes.Count);
                for (int t = 0; t < BitMask.Capacity; t++)
                {
                    if (definition.TagTypes.Contains(t))
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
                for (uint r = 0; r < references.Length; r++)
                {
                    writer.WriteValue(r);
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

            Span<Slot> sourceSlots = sourceWorld.world->slots.AsSpan();
            Span<uint> freeEntities = sourceWorld.world->freeEntities.AsSpan();
            for (uint e = 1; e < sourceSlots.Length; e++)
            {
                if (freeEntities.Contains(e))
                {
                    continue;
                }

                ref Chunk sourceChunk = ref sourceSlots[(int)e].chunk;
                Definition sourceDefinition = sourceChunk.Definition;
                uint destinationEntity = CreateEntity(sourceDefinition, out Chunk chunk, out _);
                sourceWorld.CopyComponentsTo(e, this, destinationEntity);
                sourceWorld.CopyArraysTo(e, this, destinationEntity);
                sourceWorld.CopyTagsTo(e, this, destinationEntity);
            }
        }

        /// <summary>
        /// Adds a function that listens to whenever an entity is either created, or destroyed.
        /// <para>
        /// Creation events are indicated by <see cref="ChangeType.Added"/>,
        /// while destruction events are indicated by <see cref="ChangeType.Removed"/>.
        /// </para>
        /// </summary>
        public readonly void ListenToEntityCreationOrDestruction(EntityCreatedOrDestroyed function, ulong userData = default)
        {
            MemoryAddress.ThrowIfDefault(world);

            world->entityCreatedOrDestroyed.Add((function, userData));
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
        }

        /// <summary>
        /// Adds a function that listens to when an entity's parent changes.
        /// </summary>
        public readonly void ListenToEntityParentChanges(EntityParentChanged function, ulong userData = default)
        {
            MemoryAddress.ThrowIfDefault(world);

            world->entityParentChanged.Add((function, userData));
        }

        /// <summary>
        /// Destroys the given <paramref name="entity"/> assuming it exists.
        /// </summary>
        public readonly void DestroyEntity(uint entity, bool destroyChildren = true)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            ref Slot slot = ref world->slots[(int)entity];
            if (slot.ContainsChildren)
            {
                Span<uint> children = stackalloc uint[slot.childrenCount];
                CopyChildrenTo(entity, children);
                if (destroyChildren)
                {
                    //destroy children
                    for (int i = 0; i < children.Length; i++)
                    {
                        uint child = children[i];
                        DestroyEntity(child, true);
                    }
                }
                else
                {
                    //unparent children
                    for (int i = 0; i < children.Length; i++)
                    {
                        uint child = children[i];
                        ref Slot childSlot = ref world->slots[(int)child];
                        childSlot.parent = default;
                    }
                }
            }

            slot.flags |= Slot.Flags.Outdated;
            slot.state = Slot.State.Free;

            //remove from parents children list
            ref Slot parentSlot = ref world->slots[(int)slot.parent]; //it can be 0, which is ok
            parentSlot.childrenCount--;
            slot.parent = default;
            slot.chunk.RemoveEntity(entity);
            world->freeEntities.Push(entity);

            if (world->entityCreatedOrDestroyed.Count > 0)
            {
                for (int i = 0; i < world->entityCreatedOrDestroyed.Count; i++)
                {
                    (EntityCreatedOrDestroyed callback, ulong userData) = world->entityCreatedOrDestroyed[i];
                    callback.Invoke(this, entity, ChangeType.Removed, userData);
                }
            }
        }

        /// <summary>
        /// Copies component types from the given <paramref name="entity"/> to the destination <paramref name="buffer"/>.
        /// </summary>
        public readonly int CopyComponentTypesTo(uint entity, Span<ComponentType> buffer)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            return world->slots[(int)entity].Definition.CopyComponentTypesTo(buffer);
        }

        /// <summary>
        /// Checks if the given <paramref name="entity"/> is enabled with respect
        /// to ancestor entities.
        /// </summary>
        public readonly bool IsEnabled(uint entity)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            return world->slots[(int)entity].state == Slot.State.Enabled;
        }

        /// <summary>
        /// Checks if the given entity is enabled regardless
        /// of it's parents.
        /// </summary>
        public readonly bool IsLocallyEnabled(uint entity)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            Slot.State state = world->slots[(int)entity].state;
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

            ref Slot entitySlot = ref world->slots[(int)entity];
            uint parent = entitySlot.parent;
            if (parent != default)
            {
                Slot.State parentState = world->slots[(int)parent].state;
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
            ref Chunk chunk = ref entitySlot.chunk;
            Chunk previousChunk = chunk;
            Definition previousDefinition = previousChunk.Definition;
            bool oldEnabled = !previousDefinition.TagTypes.Contains(TagType.Disabled);
            bool newEnabled = entitySlot.state == Slot.State.Enabled;
            if (oldEnabled != newEnabled)
            {
                Definition newDefinition = previousDefinition;
                if (newEnabled)
                {
                    newDefinition.RemoveTagType(TagType.Disabled);
                }
                else
                {
                    newDefinition.AddTagType(TagType.Disabled);
                }

                if (!world->chunksMap.TryGetValue(newDefinition, out Chunk newChunk))
                {
                    newChunk = new Chunk(newDefinition, world->schema);
                    world->chunksMap.Add(newDefinition, newChunk);
                    world->uniqueChunks.Add(newChunk);
                }

                chunk = newChunk;
                previousChunk.MoveEntity(entity, newChunk);
            }

            //modify descendants
            if (entitySlot.ContainsChildren)
            {
                //todo: this temporary allocation can be avoided by tracking how large the tree is
                //and then using stackalloc
                using Stack<uint> stack = new(4);
                PushChildrenToStack(this, stack, entity);

                while (stack.Count > 0)
                {
                    uint currentEntity = stack.Pop();
                    ref Slot currentSlot = ref world->slots[(int)currentEntity];
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
                    oldEnabled = !previousDefinition.TagTypes.Contains(TagType.Disabled);
                    newEnabled = currentSlot.state == Slot.State.Enabled;
                    if (oldEnabled != enabled)
                    {
                        Definition newDefinition = previousDefinition;
                        if (enabled)
                        {
                            newDefinition.RemoveTagType(TagType.Disabled);
                        }
                        else
                        {
                            newDefinition.AddTagType(TagType.Disabled);
                        }

                        if (!world->chunksMap.TryGetValue(newDefinition, out Chunk newChunk))
                        {
                            newChunk = new Chunk(newDefinition, world->schema);
                            world->chunksMap.Add(newDefinition, newChunk);
                            world->uniqueChunks.Add(newChunk);
                        }

                        currentSlot.chunk = newChunk;
                        previousChunk.MoveEntity(currentEntity, newChunk);
                    }

                    //check through children
                    if (currentSlot.ContainsChildren && !currentSlot.ChildrenOutdated)
                    {
                        PushChildrenToStack(this, stack, currentEntity);
                    }
                }

                static void PushChildrenToStack(World world, Stack<uint> stack, uint entity)
                {
                    Slot slot = world.world->slots[(int)entity];
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

            Definition definition = default;
            if (!world->chunksMap.TryGetValue(definition, out Chunk chunk))
            {
                chunk = new(definition, world->schema);
                world->chunksMap.Add(definition, chunk);
                world->uniqueChunks.Add(chunk);
            }

            if (!world->freeEntities.TryPop(out uint entity))
            {
                entity = (uint)world->slots.Count;
                world->slots.AddDefault();
            }

            ref Slot slot = ref world->slots[(int)entity];
            slot.state = Slot.State.Enabled;
            slot.chunk = chunk;
            chunk.AddEntity(entity);
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

            Definition definition = new(componentTypes, default, tagTypes);
            if (!world->chunksMap.TryGetValue(definition, out Chunk chunk))
            {
                chunk = new(definition, world->schema);
                world->chunksMap.Add(definition, chunk);
                world->uniqueChunks.Add(chunk);
            }

            if (!world->freeEntities.TryPop(out uint entity))
            {
                entity = (uint)world->slots.Count;
                world->slots.AddDefault();
            }

            ref Slot slot = ref world->slots[(int)entity];
            slot.state = Slot.State.Enabled;
            slot.chunk = chunk;
            chunk.AddEntity(entity);
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

            if (!world->chunksMap.TryGetValue(definition, out Chunk chunk))
            {
                chunk = new(definition, world->schema);
                world->chunksMap.Add(definition, chunk);
                world->uniqueChunks.Add(chunk);
            }

            if (!world->freeEntities.TryPop(out uint entity))
            {
                entity = (uint)world->slots.Count;
                world->slots.AddDefault();
            }

            ref Slot slot = ref world->slots[(int)entity];
            slot.state = Slot.State.Enabled;
            slot.chunk = chunk;

            //create arrays if necessary
            BitMask arrayElementTypes = definition.ArrayTypes;
            if (!arrayElementTypes.IsEmpty)
            {
                ref Array<Values> arrays = ref slot.arrays;
                arrays = new(BitMask.Capacity);
                for (int a = 0; a < BitMask.Capacity; a++)
                {
                    if (arrayElementTypes.Contains(a))
                    {
                        int arrayElementSize = world->schema.GetArrayTypeSize(a);
                        arrays[a] = new(new Array(0, arrayElementSize));
                    }
                }

                slot.flags |= Slot.Flags.ContainsArrays;
            }

            chunk.AddEntity(entity);
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

            if (!world->chunksMap.TryGetValue(definition, out chunk))
            {
                chunk = new(definition, world->schema);
                world->chunksMap.Add(definition, chunk);
                world->uniqueChunks.Add(chunk);
            }

            if (!world->freeEntities.TryPop(out uint entity))
            {
                entity = (uint)world->slots.Count;
                world->slots.AddDefault();
            }

            ref Slot slot = ref world->slots[(int)entity];
            slot.state = Slot.State.Enabled;
            slot.chunk = chunk;

            //create arrays if necessary
            BitMask arrayElementTypes = definition.ArrayTypes;
            if (!arrayElementTypes.IsEmpty)
            {
                ref Array<Values> arrays = ref slot.arrays;
                arrays = new(BitMask.Capacity);
                for (int a = 0; a < BitMask.Capacity; a++)
                {
                    if (arrayElementTypes.Contains(a))
                    {
                        int arrayElementSize = world->schema.GetArrayTypeSize(a);
                        arrays[a] = new(new Array(0, arrayElementSize));
                    }
                }

                slot.flags |= Slot.Flags.ContainsArrays;
            }

            index = chunk.AddEntity(entity);
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

            Definition definition = new(componentTypes, default, tagTypes);
            if (!world->chunksMap.TryGetValue(definition, out chunk))
            {
                chunk = new(definition, world->schema);
                world->chunksMap.Add(definition, chunk);
                world->uniqueChunks.Add(chunk);
            }

            if (!world->freeEntities.TryPop(out uint entity))
            {
                entity = (uint)world->slots.Count;
                world->slots.AddDefault();
            }

            ref Slot slot = ref world->slots[(int)entity];
            slot.state = Slot.State.Enabled;
            slot.chunk = chunk;
            index = chunk.AddEntity(entity);
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

            ref Chunk chunk = ref world->slots[(int)entity].chunk;
            Definition currentDefinition = chunk.Definition;
            if (!currentDefinition.ComponentTypes.ContainsAll(definition.ComponentTypes))
            {
                return false;
            }

            if (!currentDefinition.ArrayTypes.ContainsAll(definition.ArrayTypes))
            {
                return false;
            }

            return currentDefinition.TagTypes.ContainsAll(definition.TagTypes);
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

            Definition currentDefinition = world->slots[(int)entity].Definition;
            for (int i = 0; i < BitMask.Capacity; i++)
            {
                if (definition.ContainsComponent(i) && !currentDefinition.ContainsComponent(i))
                {
                    AddComponent(entity, i);
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

            Definition currentDefinition = world->slots[(int)entity].Definition;
            for (int i = 0; i < BitMask.Capacity; i++)
            {
                if (archetype.ContainsComponent(i) && !currentDefinition.ContainsComponent(i))
                {
                    AddComponent(entity, i);
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
        /// Creates entities to fill the given <paramref name="buffer"/>.
        /// </summary>
        public readonly void CreateEntities(Span<uint> buffer)
        {
            //todo: this could be more efficient
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = CreateEntity();
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

            return world->slots[(int)entity].state != Slot.State.Free;
        }

        /// <summary>
        /// Retrieves the parent of the given entity, <see langword="default"/> if none
        /// is assigned.
        /// </summary>
        public readonly uint GetParent(uint entity)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            return world->slots[(int)entity].parent;
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

            if (entity == newParent)
            {
                throw new InvalidOperationException($"Entity {entity} cannot be its own parent");
            }

            if (!ContainsEntity(newParent))
            {
                newParent = default;
            }

            ref Slot entitySlot = ref world->slots[(int)entity];
            bool parentChanged = entitySlot.parent != newParent;
            if (parentChanged)
            {
                uint oldParent = entitySlot.parent;
                entitySlot.parent = newParent;

                //remove from previous parent children list
                world->slots[(int)oldParent].childrenCount--; //old parent can be 0, which is ok

                ref Slot newParentSlot = ref world->slots[(int)newParent];
                if (!newParentSlot.ContainsChildren)
                {
                    newParentSlot.childrenCount = 0;
                    newParentSlot.flags |= Slot.Flags.ContainsChildren;
                    newParentSlot.flags &= ~Slot.Flags.ChildrenOutdated;
                }
                else if (newParentSlot.ChildrenOutdated)
                {
                    newParentSlot.childrenCount = 0;
                    newParentSlot.flags &= ~Slot.Flags.ChildrenOutdated;
                }

                newParentSlot.childrenCount++;

                //update state if parent is disabled
                if (entitySlot.state == Slot.State.Enabled)
                {
                    if (newParentSlot.state == Slot.State.Disabled || newParentSlot.state == Slot.State.DisabledButLocallyEnabled)
                    {
                        entitySlot.state = Slot.State.DisabledButLocallyEnabled;
                    }
                }

                //move to different chunk if disabled state changed
                Chunk previousChunk = entitySlot.chunk;
                Definition previousDefinition = previousChunk.Definition;
                bool oldEnabled = !previousDefinition.TagTypes.Contains(TagType.Disabled);
                bool newEnabled = entitySlot.state == Slot.State.Enabled;
                if (oldEnabled != newEnabled)
                {
                    Definition newDefinition = previousDefinition;
                    if (newEnabled)
                    {
                        newDefinition.RemoveTagType(TagType.Disabled);
                    }
                    else
                    {
                        newDefinition.AddTagType(TagType.Disabled);
                    }

                    if (!world->chunksMap.TryGetValue(newDefinition, out Chunk destinationChunk))
                    {
                        destinationChunk = new(newDefinition, world->schema);
                        world->chunksMap.Add(newDefinition, destinationChunk);
                        world->uniqueChunks.Add(destinationChunk);
                    }

                    entitySlot.chunk = destinationChunk;
                    previousChunk.MoveEntity(entity, destinationChunk);
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

            ref Slot slot = ref world->slots[(int)entity];
            if (slot.ContainsReferences && !slot.ReferencesOutdated)
            {
                return slot.references.AsSpan(1);
            }
            else
            {
                return default;
            }
        }

        /// <summary>
        /// Retrieves the number of children the given <paramref name="entity"/> has.
        /// </summary>
        public readonly int GetChildCount(uint entity)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            ref Slot slot = ref world->slots[(int)entity];
            if (slot.ContainsChildren && !slot.ChildrenOutdated)
            {
                return slot.childrenCount;
            }
            else
            {
                return 0;
            }
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

            ref Slot slot = ref world->slots[(int)entity];
            if (!slot.ContainsReferences)
            {
                slot.flags |= Slot.Flags.ContainsReferences;
                slot.flags &= ~Slot.Flags.ReferencesOutdated;
                slot.references = new(4);
                slot.references.AddDefault(); //reserved
            }
            else if (slot.ReferencesOutdated)
            {
                slot.flags &= ~Slot.Flags.ReferencesOutdated;
                slot.references.Clear();
                slot.references.AddDefault(); //reserved
            }

            int count = slot.references.Count;
            slot.references.Add(referencedEntity);
            return (rint)count;
        }

        /// <summary>
        /// Updates an existing <paramref name="reference"/> to point towards the <paramref name="referencedEntity"/>.
        /// </summary>
        public readonly void SetReference(uint entity, rint reference, uint referencedEntity)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfReferenceIsMissing(entity, reference);

            world->slots[(int)entity].references[(int)reference] = referencedEntity;
        }

        /// <summary>
        /// Checks if the given entity contains a reference to the given <paramref name="referencedEntity"/>.
        /// </summary>
        public readonly bool ContainsReference(uint entity, uint referencedEntity)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            ref Slot slot = ref world->slots[(int)entity];
            if (slot.ContainsReferences && !slot.ReferencesOutdated)
            {
                return slot.references.Contains(referencedEntity);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if the given entity contains the given local <paramref name="reference"/>.
        /// </summary>
        public readonly bool ContainsReference(uint entity, rint reference)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            ref Slot slot = ref world->slots[(int)entity];
            if (slot.ContainsReferences && !slot.ReferencesOutdated)
            {
                uint index = (uint)reference;
                return index > 0 && index <= slot.references.Count;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Retrieves the number of references the given <paramref name="entity"/> has.
        /// </summary>
        public readonly int GetReferenceCount(uint entity)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            ref Slot slot = ref world->slots[(int)entity];
            if (slot.ContainsReferences && !slot.ReferencesOutdated)
            {
                return slot.references.Count - 1;
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// Retrieves the entity referenced at the given <paramref name="reference"/> index by <paramref name="entity"/>.
        /// </summary>
        public readonly ref uint GetReference(uint entity, rint reference)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfReferenceIsMissing(entity, reference);

            return ref world->slots[(int)entity].references[(int)reference];
        }

        /// <summary>
        /// Retrieves the <see cref="rint"/> value that points to the given <paramref name="referencedEntity"/> on <paramref name="entity"/>.
        /// </summary>
        public readonly rint GetReference(uint entity, uint referencedEntity)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfReferencedEntityIsMissing(entity, referencedEntity);

            int index = world->slots[(int)entity].references.IndexOf(referencedEntity);
            return (rint)(index + 1);
        }

        /// <summary>
        /// Attempts to retrieve the referenced entity at the given <paramref name="reference"/> on <paramref name="entity"/>.
        /// </summary>
        public readonly bool TryGetReference(uint entity, rint reference, out uint referencedEntity)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            ref Slot slot = ref world->slots[(int)entity];
            if (slot.ContainsReferences && !slot.ReferencesOutdated)
            {
                int index = (int)reference;
                if (index > 0 && index <= slot.references.Count)
                {
                    referencedEntity = slot.references[index];
                    return true;
                }
            }

            referencedEntity = default;
            return false;
        }

        /// <summary>
        /// Removes the reference at the given <paramref name="reference"/> index on <paramref name="entity"/>.
        /// </summary>
        /// <returns>The other entity that was being referenced.</returns>
        public readonly uint RemoveReference(uint entity, rint reference)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfReferenceIsMissing(entity, reference);

            world->slots[(int)entity].references.RemoveAt((int)reference, out uint removed);
            return removed;
        }

        /// <summary>
        /// Removes the <paramref name="referencedEntity"/> from <paramref name="entity"/>.
        /// </summary>
        /// <returns>The reference that was removed.</returns>
        public readonly rint RemoveReference(uint entity, uint referencedEntity)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfReferencedEntityIsMissing(entity, referencedEntity);

            ref List<uint> references = ref world->slots[(int)entity].references;
            int count = references.Count;
            references.RemoveAt(references.IndexOf(referencedEntity));
            return (rint)count;
        }

        public readonly bool ContainsTag<T>(uint entity) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            int tagType = world->schema.GetTagTypeIndex<T>();
            return world->slots[(int)entity].Definition.TagTypes.Contains(tagType);
        }

        public readonly bool ContainsTag(uint entity, int tagType)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            return world->slots[(int)entity].Definition.TagTypes.Contains(tagType);
        }

        public readonly void AddTag<T>(uint entity) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            int tagType = world->schema.GetTagTypeIndex<T>();
            ThrowIfTagAlreadyPresent(entity, tagType);

            ref Slot slot = ref world->slots[(int)entity];
            Chunk previousChunk = slot.chunk;
            Definition newDefinition = previousChunk.Definition;
            newDefinition.AddTagType(tagType);

            if (!world->chunksMap.TryGetValue(newDefinition, out Chunk destinationChunk))
            {
                Schema schema = world->schema;
                destinationChunk = new(newDefinition, schema);
                world->chunksMap.Add(newDefinition, destinationChunk);
                world->uniqueChunks.Add(destinationChunk);
            }

            slot.chunk = destinationChunk;
            int index = previousChunk.MoveEntity(entity, destinationChunk);
            NotifyTagAdded(entity, tagType);
        }

        public readonly void AddTag(uint entity, int tagType)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfTagAlreadyPresent(entity, tagType);

            ref Slot slot = ref world->slots[(int)entity];
            Chunk previousChunk = slot.chunk;
            Definition newDefinition = previousChunk.Definition;
            newDefinition.AddTagType(tagType);

            if (!world->chunksMap.TryGetValue(newDefinition, out Chunk destinationChunk))
            {
                Schema schema = world->schema;
                destinationChunk = new(newDefinition, schema);
                world->chunksMap.Add(newDefinition, destinationChunk);
                world->uniqueChunks.Add(destinationChunk);
            }

            slot.chunk = destinationChunk;
            int index = previousChunk.MoveEntity(entity, destinationChunk);
            NotifyTagAdded(entity, tagType);
        }

        public readonly void RemoveTag<T>(uint entity) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            int tagType = world->schema.GetTagTypeIndex<T>();
            ThrowIfTagIsMissing(entity, tagType);

            ref Slot slot = ref world->slots[(int)entity];
            Chunk previousChunk = slot.chunk;
            Definition newComponentTypes = previousChunk.Definition;
            newComponentTypes.RemoveTagType(tagType);

            if (!world->chunksMap.TryGetValue(newComponentTypes, out Chunk destinationChunk))
            {
                Schema schema = world->schema;
                destinationChunk = new(newComponentTypes, schema);
                world->chunksMap.Add(newComponentTypes, destinationChunk);
                world->uniqueChunks.Add(destinationChunk);
            }

            slot.chunk = destinationChunk;
            previousChunk.MoveEntity(entity, destinationChunk);
            NotifyTagRemoved(entity, tagType);
        }

        public readonly void RemoveTag(uint entity, int tagType)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfTagIsMissing(entity, tagType);

            ref Slot slot = ref world->slots[(int)entity];
            Chunk previousChunk = slot.chunk;
            Definition newComponentTypes = previousChunk.Definition;
            newComponentTypes.RemoveTagType(tagType);

            if (!world->chunksMap.TryGetValue(newComponentTypes, out Chunk destinationChunk))
            {
                Schema schema = world->schema;
                destinationChunk = new(newComponentTypes, schema);
                world->chunksMap.Add(newComponentTypes, destinationChunk);
                world->uniqueChunks.Add(destinationChunk);
            }

            slot.chunk = destinationChunk;
            previousChunk.MoveEntity(entity, destinationChunk);
            NotifyTagRemoved(entity, tagType);
        }

        /// <summary>
        /// Retrieves the types of all arrays on this entity.
        /// </summary>
        public readonly BitMask GetArrayElementTypes(uint entity)
        {
            ThrowIfEntityIsMissing(entity);

            return world->slots[(int)entity].Definition.ArrayTypes;
        }

        public readonly BitMask GetTagTypes(uint entity)
        {
            ThrowIfEntityIsMissing(entity);

            return world->slots[(int)entity].Definition.TagTypes;
        }

        /// <summary>
        /// Creates a new empty array with the given <paramref name="length"/> and <paramref name="arrayType"/>.
        /// </summary>
        public readonly Values CreateArray(uint entity, int arrayType, int length = 0)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfArrayIsAlreadyPresent(entity, arrayType);

            int stride = world->schema.GetArrayTypeSize(arrayType);
            ref Slot slot = ref world->slots[(int)entity];
            Chunk previousChunk = slot.chunk;
            Definition previousDefinition = previousChunk.Definition;

            if (!slot.ContainsArrays)
            {
                slot.arrays = new(BitMask.Capacity);
                slot.flags |= Slot.Flags.ContainsArrays;
                slot.flags &= ~Slot.Flags.ArraysOutdated;
            }
            else if (slot.ArraysOutdated)
            {
                slot.flags &= ~Slot.Flags.ArraysOutdated;
                for (int i = 0; i < slot.arrays.Length; i++)
                {
                    ref Values array = ref slot.arrays[i];
                    if (array != default)
                    {
                        array.Dispose();
                        array = default;
                    }
                }
            }

            Definition newDefinition = previousDefinition;
            newDefinition.AddArrayType(arrayType);

            if (!world->chunksMap.TryGetValue(newDefinition, out Chunk destinationChunk))
            {
                destinationChunk = new(newDefinition, world->schema);
                world->chunksMap.Add(newDefinition, destinationChunk);
                world->uniqueChunks.Add(destinationChunk);
            }

            slot.chunk = destinationChunk;
            previousChunk.MoveEntity(entity, destinationChunk);

            Values newArray = new(new Array(length, stride));
            slot.arrays[arrayType] = newArray;
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

            ref Slot slot = ref world->slots[(int)entity];
            Chunk previousChunk = slot.chunk;
            Definition previousDefinition = previousChunk.Definition;

            if (!slot.ContainsArrays)
            {
                slot.arrays = new(BitMask.Capacity);
                slot.flags |= Slot.Flags.ContainsArrays;
                slot.flags &= ~Slot.Flags.ArraysOutdated;
            }
            else if (slot.ArraysOutdated)
            {
                slot.flags &= ~Slot.Flags.ArraysOutdated;
                for (int i = 0; i < slot.arrays.Length; i++)
                {
                    ref Values array = ref slot.arrays[i];
                    if (array != default)
                    {
                        array.Dispose();
                        array = default;
                    }
                }
            }

            Definition newDefinition = previousDefinition;
            newDefinition.AddArrayType(arrayType);

            if (!world->chunksMap.TryGetValue(newDefinition, out Chunk destinationChunk))
            {
                destinationChunk = new(newDefinition, world->schema);
                world->chunksMap.Add(newDefinition, destinationChunk);
                world->uniqueChunks.Add(destinationChunk);
            }

            slot.chunk = destinationChunk;
            previousChunk.MoveEntity(entity, destinationChunk);

            Values newArray = new(new Array(length, stride));
            slot.arrays[arrayType] = newArray;
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

            ArrayElementType arrayType = dataType.ArrayType;
            ThrowIfArrayIsAlreadyPresent(entity, arrayType);

            ref Slot slot = ref world->slots[(int)entity];
            Chunk previousChunk = slot.chunk;
            Definition previousDefinition = previousChunk.Definition;

            if (!slot.ContainsArrays)
            {
                slot.arrays = new(BitMask.Capacity);
                slot.flags |= Slot.Flags.ContainsArrays;
                slot.flags &= ~Slot.Flags.ArraysOutdated;
            }
            else if (slot.ArraysOutdated)
            {
                slot.flags &= ~Slot.Flags.ArraysOutdated;
                for (int i = 0; i < slot.arrays.Length; i++)
                {
                    ref Values array = ref slot.arrays[i];
                    if (array != default)
                    {
                        array.Dispose();
                        array = default;
                    }
                }
            }

            Definition newDefinition = previousDefinition;
            newDefinition.AddArrayType(arrayType);

            if (!world->chunksMap.TryGetValue(newDefinition, out Chunk destinationChunk))
            {
                destinationChunk = new(newDefinition, world->schema);
                world->chunksMap.Add(newDefinition, destinationChunk);
                world->uniqueChunks.Add(destinationChunk);
            }

            slot.chunk = destinationChunk;
            previousChunk.MoveEntity(entity, destinationChunk);

            Values newArray = new(new Array(length, dataType.size));
            slot.arrays[arrayType.index] = newArray;
            NotifyArrayCreated(entity, arrayType);
            return newArray;
        }

        /// <summary>
        /// Creates a new empty array on this <paramref name="entity"/>.
        /// </summary>
        public readonly Values<T> CreateArray<T>(uint entity, int length = 0) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            int arrayType = world->schema.GetArrayTypeIndex<T>();
            ThrowIfArrayIsAlreadyPresent(entity, arrayType);

            ref Slot slot = ref world->slots[(int)entity];
            Chunk previousChunk = slot.chunk;
            Definition previousDefinition = previousChunk.Definition;

            if (!slot.ContainsArrays)
            {
                slot.arrays = new(BitMask.Capacity);
                slot.flags |= Slot.Flags.ContainsArrays;
                slot.flags &= ~Slot.Flags.ArraysOutdated;
            }
            else if (slot.ArraysOutdated)
            {
                slot.flags &= ~Slot.Flags.ArraysOutdated;
                for (int i = 0; i < slot.arrays.Length; i++)
                {
                    ref Values array = ref slot.arrays[i];
                    if (array != default)
                    {
                        array.Dispose();
                        array = default;
                    }
                }
            }

            Definition newDefinition = previousDefinition;
            newDefinition.AddArrayType(arrayType);

            if (!world->chunksMap.TryGetValue(newDefinition, out Chunk destinationChunk))
            {
                destinationChunk = new(newDefinition, world->schema);
                world->chunksMap.Add(newDefinition, destinationChunk);
                world->uniqueChunks.Add(destinationChunk);
            }

            slot.chunk = destinationChunk;
            previousChunk.MoveEntity(entity, destinationChunk);

            Values<T> newArray = new(new Array<T>(length));
            slot.arrays[arrayType] = newArray;
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

            int arrayType = world->schema.GetArrayTypeIndex<T>();
            ThrowIfArrayIsAlreadyPresent(entity, arrayType);

            ref Slot slot = ref world->slots[(int)entity];
            Chunk previousChunk = slot.chunk;
            Definition previousDefinition = previousChunk.Definition;

            if (!slot.ContainsArrays)
            {
                slot.arrays = new(BitMask.Capacity);
                slot.flags |= Slot.Flags.ContainsArrays;
                slot.flags &= ~Slot.Flags.ArraysOutdated;
            }
            else if (slot.ArraysOutdated)
            {
                slot.flags &= ~Slot.Flags.ArraysOutdated;
                for (int i = 0; i < slot.arrays.Length; i++)
                {
                    ref Values array = ref slot.arrays[i];
                    if (array != default)
                    {
                        array.Dispose();
                        array = default;
                    }
                }
            }

            Definition newDefinition = previousDefinition;
            newDefinition.AddArrayType(arrayType);

            if (!world->chunksMap.TryGetValue(newDefinition, out Chunk destinationChunk))
            {
                destinationChunk = new(newDefinition, world->schema);
                world->chunksMap.Add(newDefinition, destinationChunk);
                world->uniqueChunks.Add(destinationChunk);
            }

            slot.chunk = destinationChunk;
            previousChunk.MoveEntity(entity, destinationChunk);

            slot.arrays[arrayType] = new(new Array<T>(values));
            NotifyArrayCreated(entity, arrayType);
        }

        /// <summary>
        /// Creates a new array containing the given <paramref name="values"/>.
        /// </summary>
        public readonly void CreateArray<T>(uint entity, Span<T> values) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            int arrayType = world->schema.GetArrayTypeIndex<T>();
            ThrowIfArrayIsAlreadyPresent(entity, arrayType);

            ref Slot slot = ref world->slots[(int)entity];
            Chunk previousChunk = slot.chunk;
            Definition previousDefinition = previousChunk.Definition;

            if (!slot.ContainsArrays)
            {
                slot.arrays = new(BitMask.Capacity);
                slot.flags |= Slot.Flags.ContainsArrays;
                slot.flags &= ~Slot.Flags.ArraysOutdated;
            }
            else if (slot.ArraysOutdated)
            {
                slot.flags &= ~Slot.Flags.ArraysOutdated;
                for (int i = 0; i < slot.arrays.Length; i++)
                {
                    ref Values array = ref slot.arrays[i];
                    if (array != default)
                    {
                        array.Dispose();
                        array = default;
                    }
                }
            }

            Definition newDefinition = previousDefinition;
            newDefinition.AddArrayType(arrayType);

            if (!world->chunksMap.TryGetValue(newDefinition, out Chunk destinationChunk))
            {
                destinationChunk = new(newDefinition, world->schema);
                world->chunksMap.Add(newDefinition, destinationChunk);
                world->uniqueChunks.Add(destinationChunk);
            }

            slot.chunk = destinationChunk;
            previousChunk.MoveEntity(entity, destinationChunk);

            slot.arrays[arrayType] = new(new Array<T>(values));
            NotifyArrayCreated(entity, arrayType);
        }

        /// <summary>
        /// Checks if the given <paramref name="entity"/> contains an array of type <typeparamref name="T"/>.
        /// </summary>
        public readonly bool ContainsArray<T>(uint entity) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            int arrayType = world->schema.GetArrayTypeIndex<T>();
            return world->slots[(int)entity].Definition.ArrayTypes.Contains(arrayType);
        }

        /// <summary>
        /// Checks if the given <paramref name="entity"/> contains an array of the given <paramref name="arrayType"/>.
        /// </summary>
        public readonly bool ContainsArray(uint entity, int arrayType)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            return world->slots[(int)entity].Definition.ArrayTypes.Contains(arrayType);
        }

        /// <summary>
        /// Retrieves the array of type <typeparamref name="T"/> from the given <paramref name="entity"/>.
        /// </summary>
        public readonly Values<T> GetArray<T>(uint entity) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            int arrayType = world->schema.GetArrayTypeIndex<T>();
            ThrowIfArrayIsMissing(entity, arrayType);

            return new(world->slots[(int)entity].arrays[arrayType].pointer);
        }

        /// <summary>
        /// Retrieves the array of type <typeparamref name="T"/> from the given <paramref name="entity"/>.
        /// </summary>
        public readonly Values<T> GetArray<T>(uint entity, int arrayType) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfArrayIsMissing(entity, arrayType);

            return new(world->slots[(int)entity].arrays[arrayType].pointer);
        }

        /// <summary>
        /// Retrieves the array of the given <paramref name="arrayType"/> from the given <paramref name="entity"/>.
        /// </summary>
        public readonly Values GetArray(uint entity, int arrayType)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfArrayIsMissing(entity, arrayType);

            return world->slots[(int)entity].arrays[arrayType];
        }

        /// <summary>
        /// Attempts to retrieve an array of type <typeparamref name="T"/> from the given <paramref name="entity"/>.
        /// </summary>
        public readonly bool TryGetArray<T>(uint entity, out Values<T> array) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            int arrayType = world->schema.GetArrayTypeIndex<T>();
            ref Slot slot = ref world->slots[(int)entity];
            if (slot.Definition.ArrayTypes.Contains(arrayType))
            {
                array = new(slot.arrays[arrayType].pointer);
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

            int arrayType = world->schema.GetArrayTypeIndex<T>();
            ThrowIfArrayIsMissing(entity, arrayType);

            return ref world->slots[(int)entity].arrays[arrayType].Get<T>(index);
        }

        /// <summary>
        /// Retrieves the length of an existing array on this entity.
        /// </summary>
        public readonly int GetArrayLength<T>(uint entity) where T : unmanaged
        {
            ThrowIfEntityIsMissing(entity);

            int arrayType = world->schema.GetArrayTypeIndex<T>();
            ThrowIfArrayIsMissing(entity, arrayType);

            return world->slots[(int)entity].arrays[arrayType].Length;
        }

        /// <summary>
        /// Retrieves the length of an existing array on this entity.
        /// </summary>
        public readonly int GetArrayLength(uint entity, int arrayType)
        {
            ThrowIfEntityIsMissing(entity);
            ThrowIfArrayIsMissing(entity, arrayType);

            return world->slots[(int)entity].arrays[arrayType].Length;
        }

        /// <summary>
        /// Destroys the array of type <typeparamref name="T"/> on the given <paramref name="entity"/>.
        /// </summary>
        public readonly void DestroyArray<T>(uint entity) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            int arrayType = world->schema.GetArrayTypeIndex<T>();
            ThrowIfArrayIsMissing(entity, arrayType);

            ref Slot slot = ref world->slots[(int)entity];
            ref Values array = ref slot.arrays[arrayType];
            array.Dispose();
            array = default;

            Chunk previousChunk = slot.chunk;
            Definition newDefinition = previousChunk.Definition;
            newDefinition.RemoveArrayType(arrayType);

            if (!world->chunksMap.TryGetValue(newDefinition, out Chunk destinationChunk))
            {
                destinationChunk = new(newDefinition, world->schema);
                world->chunksMap.Add(newDefinition, destinationChunk);
                world->uniqueChunks.Add(destinationChunk);
            }

            slot.chunk = destinationChunk;
            previousChunk.MoveEntity(entity, destinationChunk);
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

            ref Slot slot = ref world->slots[(int)entity];
            ref Values array = ref slot.arrays[arrayType];
            array.Dispose();
            array = default;

            Chunk previousChunk = slot.chunk;
            Definition newDefinition = previousChunk.Definition;
            newDefinition.RemoveArrayType(arrayType);

            if (!world->chunksMap.TryGetValue(newDefinition, out Chunk destinationChunk))
            {
                destinationChunk = new(newDefinition, world->schema);
                world->chunksMap.Add(newDefinition, destinationChunk);
                world->uniqueChunks.Add(destinationChunk);
            }

            slot.chunk = destinationChunk;
            previousChunk.MoveEntity(entity, destinationChunk);
            NotifyArrayDestroyed(entity, arrayType);
        }

        /// <summary>
        /// Adds a new <typeparamref name="T"/> component to the given <paramref name="entity"/>.
        /// </summary>
        public readonly void AddComponent<T>(uint entity, T component) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            int componentType = world->schema.GetComponentTypeIndex<T>();
            ThrowIfComponentAlreadyPresent(entity, componentType);

            ref Slot slot = ref world->slots[(int)entity];
            Chunk previousChunk = slot.chunk;
            Definition newDefinition = previousChunk.Definition;
            newDefinition.AddComponentType(componentType);

            if (!world->chunksMap.TryGetValue(newDefinition, out Chunk destinationChunk))
            {
                destinationChunk = new(newDefinition, world->schema);
                world->chunksMap.Add(newDefinition, destinationChunk);
                world->uniqueChunks.Add(destinationChunk);
            }

            slot.chunk = destinationChunk;
            int index = previousChunk.MoveEntity(entity, destinationChunk);
            destinationChunk.SetComponent(index, componentType, component);
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

            int componentType = world->schema.GetComponentTypeIndex<T>();
            ThrowIfComponentAlreadyPresent(entity, componentType);

            ref Slot slot = ref world->slots[(int)entity];
            Chunk previousChunk = slot.chunk;
            Definition newDefinition = previousChunk.Definition;
            newDefinition.AddComponentType(componentType);

            if (!world->chunksMap.TryGetValue(newDefinition, out Chunk destinationChunk))
            {
                destinationChunk = new(newDefinition, world->schema);
                world->chunksMap.Add(newDefinition, destinationChunk);
                world->uniqueChunks.Add(destinationChunk);
            }

            slot.chunk = destinationChunk;
            int index = previousChunk.MoveEntity(entity, destinationChunk);
            NotifyComponentAdded(entity, componentType);
            return ref destinationChunk.GetComponent<T>(index, componentType);
        }

        /// <summary>
        /// Adds a new default component with the given type.
        /// </summary>
        public readonly MemoryAddress AddComponent(uint entity, int componentType)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfComponentAlreadyPresent(entity, componentType);

            ref Slot slot = ref world->slots[(int)entity];
            Chunk previousChunk = slot.chunk;
            Definition newDefinition = previousChunk.Definition;
            newDefinition.AddComponentType(componentType);

            if (!world->chunksMap.TryGetValue(newDefinition, out Chunk destinationChunk))
            {
                destinationChunk = new(newDefinition, world->schema);
                world->chunksMap.Add(newDefinition, destinationChunk);
                world->uniqueChunks.Add(destinationChunk);
            }

            slot.chunk = destinationChunk;
            int index = previousChunk.MoveEntity(entity, destinationChunk);
            NotifyComponentAdded(entity, componentType);
            return destinationChunk.GetComponent(index, componentType);
        }

        /// <summary>
        /// Adds a new default component with the given type.
        /// </summary>
        public readonly MemoryAddress AddComponent(uint entity, int componentType, out int componentSize)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfComponentAlreadyPresent(entity, componentType);

            ref Slot slot = ref world->slots[(int)entity];
            Chunk previousChunk = slot.chunk;
            Definition newDefinition = previousChunk.Definition;
            newDefinition.AddComponentType(componentType);

            if (!world->chunksMap.TryGetValue(newDefinition, out Chunk destinationChunk))
            {
                destinationChunk = new(newDefinition, world->schema);
                world->chunksMap.Add(newDefinition, destinationChunk);
                world->uniqueChunks.Add(destinationChunk);
            }

            slot.chunk = destinationChunk;
            int index = previousChunk.MoveEntity(entity, destinationChunk);
            NotifyComponentAdded(entity, componentType);
            return destinationChunk.GetComponent(index, componentType, out componentSize);
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

            ref Slot slot = ref world->slots[(int)entity];
            Chunk previousChunk = slot.chunk;
            Definition newDefinition = previousChunk.Definition;
            newDefinition.AddComponentType(componentType);

            if (!world->chunksMap.TryGetValue(newDefinition, out Chunk destinationChunk))
            {
                destinationChunk = new(newDefinition, world->schema);
                world->chunksMap.Add(newDefinition, destinationChunk);
                world->uniqueChunks.Add(destinationChunk);
            }

            slot.chunk = destinationChunk;
            int index = previousChunk.MoveEntity(entity, destinationChunk);
            MemoryAddress component = destinationChunk.GetComponent(index, componentType, out int componentSize);

            //todo: efficiency: this could be eliminated, but would need awareness given to the user about the size of the component
            Span<byte> destination = component.GetSpan(Math.Min(componentSize, componentBytes.Length));
            componentBytes.CopyTo(destination);
        }

        /// <summary>
        /// Removes the component of type <typeparamref name="T"/> from the given <paramref name="entity"/>.
        /// </summary>
        public readonly void RemoveComponent<T>(uint entity) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            int componentType = world->schema.GetComponentTypeIndex<T>();
            ThrowIfComponentMissing(entity, componentType);

            ref Slot slot = ref world->slots[(int)entity];
            Chunk previousChunk = slot.chunk;
            Definition newComponentTypes = previousChunk.Definition;
            newComponentTypes.RemoveComponentType(componentType);

            if (!world->chunksMap.TryGetValue(newComponentTypes, out Chunk destinationChunk))
            {
                Schema schema = world->schema;
                destinationChunk = new(newComponentTypes, schema);
                world->chunksMap.Add(newComponentTypes, destinationChunk);
                world->uniqueChunks.Add(destinationChunk);
            }

            slot.chunk = destinationChunk;
            previousChunk.MoveEntity(entity, destinationChunk);
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

            ref Slot slot = ref world->slots[(int)entity];
            Chunk previousChunk = slot.chunk;
            Definition newComponentTypes = previousChunk.Definition;
            newComponentTypes.RemoveComponentType(componentType);

            if (!world->chunksMap.TryGetValue(newComponentTypes, out Chunk destinationChunk))
            {
                Schema schema = world->schema;
                destinationChunk = new(newComponentTypes, schema);
                world->chunksMap.Add(newComponentTypes, destinationChunk);
                world->uniqueChunks.Add(destinationChunk);
            }

            slot.chunk = destinationChunk;
            previousChunk.MoveEntity(entity, destinationChunk);
            NotifyComponentRemoved(entity, componentType);
        }

        /// <summary>
        /// Checks if any entity in this world contains a component
        /// of type <typeparamref name="T"/>.
        /// </summary>
        public readonly bool ContainsAnyComponent<T>() where T : unmanaged
        {
            int componentType = world->schema.GetComponentTypeIndex<T>();
            foreach (Chunk chunk in world->uniqueChunks)
            {
                if (chunk.Definition.ComponentTypes.Contains(componentType))
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

            int componentType = world->schema.GetComponentTypeIndex<T>();
            return world->slots[(int)entity].Definition.ComponentTypes.Contains(componentType);
        }

        /// <summary>
        /// Checks if the given <paramref name="entity"/> contains the <paramref name="componentType"/>.
        /// </summary>
        public readonly bool ContainsComponent(uint entity, int componentType)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            return world->slots[(int)entity].Definition.ComponentTypes.Contains(componentType);
        }

        /// <summary>
        /// Retrieves a reference to the component of type <typeparamref name="T"/> from the given <paramref name="entity"/>.
        /// </summary>
        public readonly ref T GetComponent<T>(uint entity) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            int componentType = world->schema.GetComponentTypeIndex<T>();
            ThrowIfComponentMissing(entity, componentType);

            return ref world->slots[(int)entity].chunk.GetComponentOfEntity<T>(entity, componentType);
        }

        /// <summary>
        /// Retrieves the component of type <typeparamref name="T"/> if it exists, otherwise the given
        /// <paramref name="defaultValue"/> is returned.
        /// </summary>
        public readonly T GetComponentOrDefault<T>(uint entity, T defaultValue = default) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);

            int componentType = world->schema.GetComponentTypeIndex<T>();
            ref Slot slot = ref world->slots[(int)entity];
            if (slot.Definition.ComponentTypes.Contains(componentType))
            {
                return slot.chunk.GetComponentOfEntity<T>(entity, componentType);
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

            return ref world->slots[(int)entity].chunk.GetComponentOfEntity<T>(entity, componentType);
        }

        public readonly MemoryAddress GetComponent(uint entity, int componentType)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfComponentMissing(entity, componentType);

            return world->slots[(int)entity].chunk.GetComponentOfEntity(entity, componentType);
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

            return world->slots[(int)entity].chunk.GetComponentOfEntity(entity, componentType, out componentSize);
        }

        /// <summary>
        /// Fetches the bytes of a component from the given <paramref name="entity"/>.
        /// </summary>
        public readonly Span<byte> GetComponentBytes(uint entity, int componentType)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfComponentMissing(entity, componentType);

            MemoryAddress component = world->slots[(int)entity].chunk.GetComponentOfEntity(entity, componentType, out int componentSize);
            return new(component.Pointer, componentSize);
        }

        /// <summary>
        /// Retrieves the component from the given <paramref name="entity"/> as an <see cref="object"/>.
        /// </summary>
        public readonly object GetComponentObject(uint entity, int componentType)
        {
            MemoryAddress.ThrowIfDefault(world);

            TypeLayout layout = world->schema.GetComponentLayout(componentType);
            Span<byte> bytes = GetComponentBytes(entity, componentType);
            return layout.CreateInstance(bytes);
        }

        /// <summary>
        /// Retrieves the array from the given <paramref name="entity"/> as <see cref="object"/>s.
        /// </summary>
        public readonly object[] GetArrayObject(uint entity, ArrayElementType arrayElementType)
        {
            MemoryAddress.ThrowIfDefault(world);

            TypeLayout layout = arrayElementType.GetLayout(world->schema);
            Values array = GetArray(entity, arrayElementType);
            int size = layout.Size;
            object[] arrayObject = new object[array.Length];
            for (int i = 0; i < array.Length; i++)
            {
                MemoryAddress allocation = array[i];
                arrayObject[i] = layout.CreateInstance(new(allocation.Pointer, size));
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

            ref Chunk chunk = ref world->slots[(int)entity].chunk;
            contains = chunk.Definition.ComponentTypes.Contains(componentType);
            if (contains)
            {
                return ref chunk.GetComponentOfEntity<T>(entity, componentType);
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

            int componentType = world->schema.GetComponentTypeIndex<T>();
            ref Chunk chunk = ref world->slots[(int)entity].chunk;
            contains = chunk.Definition.ComponentTypes.Contains(componentType);
            if (contains)
            {
                return ref chunk.GetComponentOfEntity<T>(entity, componentType);
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

            ref Chunk chunk = ref world->slots[(int)entity].chunk;
            if (chunk.Definition.ComponentTypes.Contains(componentType))
            {
                component = chunk.GetComponentOfEntity<T>(entity, componentType);
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
        /// <returns><c>true</c> if found.</returns>
        public readonly bool TryGetComponent<T>(uint entity, out T component) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(world);

            int componentType = world->schema.GetComponentTypeIndex<T>();
            ref Chunk chunk = ref world->slots[(int)entity].chunk;
            if (chunk.Definition.ComponentTypes.Contains(componentType))
            {
                component = chunk.GetComponentOfEntity<T>(entity, componentType);
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

            int componentType = world->schema.GetComponentTypeIndex<T>();
            ThrowIfComponentMissing(entity, componentType);

            world->slots[(int)entity].chunk.SetComponentOfEntity(entity, componentType, component);
        }

        /// <summary>
        /// Assigns the given <paramref name="componentBytes"/> to the given <paramref name="entity"/>.
        /// </summary>
        public readonly void SetComponentBytes(uint entity, int componentType, ReadOnlySpan<byte> componentBytes)
        {
            MemoryAddress.ThrowIfDefault(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfComponentMissing(entity, componentType);

            MemoryAddress component = world->slots[(int)entity].chunk.GetComponentOfEntity(entity, componentType, out int componentSize);
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

            return world->slots[(int)entity].chunk;
        }

        /// <summary>
        /// Copies components from the source entity onto the destination.
        /// <para>Components will be added if the destination entity doesnt
        /// contain them. Existing component data will be overwritten.</para>
        /// </summary>
        public readonly void CopyComponentsTo(uint sourceEntity, World destinationWorld, uint destinationEntity)
        {
            ThrowIfEntityIsMissing(sourceEntity);

            Chunk sourceChunk = world->slots[(int)sourceEntity].chunk;
            Definition sourceComponentTypes = sourceChunk.Definition;
            int sourceIndex = sourceChunk.Entities.IndexOf(sourceEntity);
            for (int c = 0; c < BitMask.Capacity; c++)
            {
                if (sourceComponentTypes.ComponentTypes.Contains(c))
                {
                    if (!destinationWorld.ContainsComponent(destinationEntity, c))
                    {
                        destinationWorld.AddComponent(destinationEntity, c);
                    }

                    MemoryAddress sourceComponent = sourceChunk.GetComponent(sourceIndex, c, out int componentSize);
                    MemoryAddress destinationComponent = destinationWorld.GetComponent(destinationEntity, c);
                    sourceComponent.CopyTo(destinationComponent, componentSize);
                }
            }
        }

        /// <summary>
        /// Copies all arrays from the source entity onto the destination.
        /// <para>Arrays will be created if the destination doesn't already
        /// contain them. Data will be overwritten, and lengths will be changed.</para>
        /// </summary>
        public readonly void CopyArraysTo(uint sourceEntity, World destinationWorld, uint destinationEntity)
        {
            BitMask arrayElementTypes = GetArrayElementTypes(sourceEntity);
            for (int a = 0; a < BitMask.Capacity; a++)
            {
                if (arrayElementTypes.Contains(a))
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

        public readonly void CopyReferencesTo(uint sourceEntity, World destinationWorld, uint destinationEntity)
        {
            int referenceCount = GetReferenceCount(sourceEntity);
            for (uint r = 1; r <= referenceCount; r++)
            {
                uint referencedEntity = GetReference(sourceEntity, (rint)r);
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

            if (entity > world->slots.Count)
            {
                throw new NullReferenceException($"Entity `{entity}` not found");
            }

            ref Slot.State state = ref world->slots[(int)entity].state;
            if (state == Slot.State.Free)
            {
                throw new NullReferenceException($"Entity `{entity}` not found");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfReferenceIsMissing(uint entity, rint reference)
        {
            ref Slot slot = ref world->slots[(int)entity];
            if (!slot.ContainsReferences || slot.ReferencesOutdated)
            {
                throw new NullReferenceException($"Reference `{reference}` not found on entity `{entity}`");
            }

            uint index = (uint)reference;
            if (index == 0 || index > slot.references.Count)
            {
                throw new NullReferenceException($"Reference `{reference}` not found on entity `{entity}`");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfReferencedEntityIsMissing(uint entity, uint referencedEntity)
        {
            ref Slot slot = ref world->slots[(int)entity];
            if (!slot.ContainsReferences || slot.ReferencesOutdated)
            {
                throw new NullReferenceException($"Entity `{entity}` does not reference `{referencedEntity}`");
            }

            if (!slot.references.Contains(referencedEntity))
            {
                throw new NullReferenceException($"Entity `{entity}` does not reference `{referencedEntity}`");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfComponentMissing(uint entity, int componentType)
        {
            BitMask componentTypes = world->slots[(int)entity].Definition.ComponentTypes;
            if (!componentTypes.Contains(componentType))
            {
                Entity thisEntity = new(new(world), entity);
                throw new NullReferenceException($"Component `{new ComponentType(componentType).ToString(world->schema)}` not found on `{entity}`");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfComponentAlreadyPresent(uint entity, int componentType)
        {
            BitMask componentTypes = world->slots[(int)entity].Definition.ComponentTypes;
            if (componentTypes.Contains(componentType))
            {
                throw new InvalidOperationException($"Component `{new ComponentType(componentType).ToString(world->schema)}` already present on `{entity}`");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfTagAlreadyPresent(uint entity, int tagType)
        {
            BitMask tagTypes = world->slots[(int)entity].Definition.TagTypes;
            if (tagTypes.Contains(tagType))
            {
                throw new InvalidOperationException($"Tag `{new TagType(tagType).ToString(world->schema)}` already present on `{entity}`");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfTagIsMissing(uint entity, int tagType)
        {
            BitMask tagTypes = world->slots[(int)entity].Definition.TagTypes;
            if (!tagTypes.Contains(tagType))
            {
                throw new NullReferenceException($"Tag `{new TagType(tagType).ToString(world->schema)}` not found on `{entity}`");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfArrayIsMissing(uint entity, int arrayType)
        {
            BitMask arrayElementTypes = world->slots[(int)entity].Definition.ArrayTypes;
            if (!arrayElementTypes.Contains(arrayType))
            {
                throw new NullReferenceException($"Array of type `{new ArrayElementType(arrayType).ToString(world->schema)}` not found on entity `{entity}`");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfArrayIsAlreadyPresent(uint entity, int arrayType)
        {
            BitMask arrayElementTypes = world->slots[(int)entity].Definition.ArrayTypes;
            if (arrayElementTypes.Contains(arrayType))
            {
                throw new InvalidOperationException($"Array of type `{new ArrayElementType(arrayType).ToString(world->schema)}` already present on `{entity}`");
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
        public static World Deserialize(ByteReader reader, Func<TypeLayout, DataType.Kind, TypeLayout>? process)
        {
            Signature signature = reader.ReadValue<Signature>();
            if (signature.Version != Version)
            {
                throw new InvalidOperationException($"Invalid version `{signature.Version}` expected `{Version}`");
            }

            //deserialize the schema first
            Schema schema = Schema.Create();
            using Schema loadedSchema = reader.ReadObject<Schema>();
            if (process is not null)
            {
                foreach (ComponentType componentType in loadedSchema.ComponentTypes)
                {
                    TypeLayout typeLayout = loadedSchema.GetComponentLayout(componentType);
                    typeLayout = process.Invoke(typeLayout, DataType.Kind.Component);
                    schema.RegisterComponent(typeLayout);
                }

                foreach (ArrayElementType arrayElementType in loadedSchema.ArrayElementTypes)
                {
                    TypeLayout typeLayout = loadedSchema.GetArrayLayout(arrayElementType);
                    typeLayout = process.Invoke(typeLayout, DataType.Kind.ArrayElement);
                    schema.RegisterArrayElement(typeLayout);
                }

                foreach (TagType tagType in loadedSchema.TagTypes)
                {
                    TypeLayout typeLayout = loadedSchema.GetTagLayout(tagType);
                    typeLayout = process.Invoke(typeLayout, DataType.Kind.Tag);
                    schema.RegisterTag(typeLayout);
                }
            }
            else
            {
                schema.CopyFrom(loadedSchema);
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

                uint createdEntity = value.CreateEntity(default, out _, out _);
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
                    int arrayElementSize = schema.GetArrayTypeSize(a);
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
                    slot.flags |= Slot.Flags.ContainsReferences;
                    ref List<uint> references = ref slot.references;
                    references = new(referenceCount + 1);
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
                    if (!parentSlot.ContainsChildren)
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