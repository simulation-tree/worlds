using Collections;
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
        public readonly uint Count
        {
            get
            {
                Allocations.ThrowIfNull(world);

                return world->slots.Count - world->freeEntities.Count - 1;
            }
        }

        /// <summary>
        /// The current maximum amount of referrable entities.
        /// <para>Collections of this size + 1 are guaranteed to
        /// be able to store all entity positions.</para>
        /// </summary>
        public readonly uint MaxEntityValue
        {
            get
            {
                Allocations.ThrowIfNull(world);

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
        private readonly List<Slot> Slots
        {
            get
            {
                Allocations.ThrowIfNull(world);

                return world->slots;
            }
        }

        /// <summary>
        /// All previously used entities that are now free.
        /// </summary>
        public readonly Stack<uint> Free
        {
            get
            {
                Allocations.ThrowIfNull(world);

                return world->freeEntities;
            }
        }

        /// <summary>
        /// Dictionary mapping <see cref="Definition"/>s to <see cref="Chunk"/>s.
        /// </summary>
        public readonly Dictionary<Definition, Chunk> ChunksMap
        {
            get
            {
                Allocations.ThrowIfNull(world);

                return world->chunksMap;
            }
        }

        /// <summary>
        /// All <see cref="Chunk"/>s in the world.
        /// </summary>
        public readonly USpan<Chunk> Chunks
        {
            get
            {
                Allocations.ThrowIfNull(world);

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
                Allocations.ThrowIfNull(world);

                return world->schema;
            }
        }

        /// <summary>
        /// All entities that exist in the world.
        /// </summary>
        public readonly System.Collections.Generic.IEnumerable<uint> Entities
        {
            get
            {
                Stack<uint> free = Free;
                List<Slot> slots = Slots;
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
        public readonly uint this[uint index]
        {
            get
            {
                Allocations.ThrowIfNull(world);

                uint i = 0;
                for (uint e = 1; e < world->slots.Count; e++)
                {
                    if (!world->freeEntities.Contains(e))
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
            ref Pointer world = ref Allocations.Allocate<Pointer>();
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
            ref Pointer world = ref Allocations.Allocate<Pointer>();
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
            Allocations.ThrowIfNull(world);

            Clear();

            for (uint e = 1; e < world->slots.Count; e++)
            {
                ref Slot slot = ref world->slots[e];
                if (slot.ContainsChildren)
                {
                    slot.children.Dispose();
                }

                if (slot.ContainsReferences)
                {
                    slot.references.Dispose();
                }

                if (slot.ContainsArrays)
                {
                    for (uint a = 0; a < BitMask.Capacity; a++)
                    {
                        ref Array array = ref slot.arrays[a];
                        if (!array.IsDisposed)
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
            Allocations.Free(ref world);
        }

        /// <summary>
        /// Resets the world to <see langword="default"/> state.
        /// </summary>
        public readonly void Clear()
        {
            Allocations.ThrowIfNull(world);

            for (uint i = 0; i < world->uniqueChunks.Count; i++)
            {
                world->uniqueChunks[i].Dispose();
            }

            world->uniqueChunks.Clear();

            for (uint e = 1; e < world->slots.Count; e++)
            {
                ref Slot slot = ref world->slots[e];
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
            for (uint i = 0; i < events.Count; i++)
            {
                (EntityCreatedOrDestroyed callback, ulong userData) = events[i];
                callback.Invoke(this, entity, ChangeType.Added, userData);
            }
        }

        private readonly void NotifyDestruction(uint entity)
        {
            List<(EntityCreatedOrDestroyed, ulong)> events = world->entityCreatedOrDestroyed;
            for (uint i = 0; i < events.Count; i++)
            {
                (EntityCreatedOrDestroyed callback, ulong userData) = events[i];
                callback.Invoke(this, entity, ChangeType.Removed, userData);
            }
        }

        private readonly void NotifyParentChange(uint entity, uint oldParent, uint newParent)
        {
            List<(EntityParentChanged, ulong)> events = world->entityParentChanged;
            for (uint i = 0; i < events.Count; i++)
            {
                (EntityParentChanged callback, ulong userData) = events[i];
                callback.Invoke(this, entity, oldParent, newParent, userData);
            }
        }

        private readonly void NotifyComponentAdded(uint entity, ComponentType componentType)
        {
            List<(EntityDataChanged, ulong)> events = world->entityDataChanged;
            if (events.Count > 0)
            {
                DataType type = world->schema.GetDataType(componentType);
                for (uint i = 0; i < events.Count; i++)
                {
                    (EntityDataChanged callback, ulong userData) = events[i];
                    callback.Invoke(this, entity, type, ChangeType.Added, userData);
                }
            }
        }

        private readonly void NotifyComponentRemoved(uint entity, ComponentType componentType)
        {
            List<(EntityDataChanged, ulong)> events = world->entityDataChanged;
            if (events.Count > 0)
            {
                DataType type = world->schema.GetDataType(componentType);
                for (uint i = 0; i < events.Count; i++)
                {
                    (EntityDataChanged callback, ulong userData) = events[i];
                    callback.Invoke(this, entity, type, ChangeType.Removed, userData);
                }
            }
        }

        private readonly void NotifyArrayCreated(uint entity, ArrayElementType arrayElementType)
        {
            List<(EntityDataChanged, ulong)> events = world->entityDataChanged;
            if (events.Count > 0)
            {
                DataType type = world->schema.GetDataType(arrayElementType);
                for (uint i = 0; i < events.Count; i++)
                {
                    (EntityDataChanged callback, ulong userData) = events[i];
                    callback.Invoke(this, entity, type, ChangeType.Added, userData);
                }
            }
        }

        private readonly void NotifyArrayDestroyed(uint entity, ArrayElementType arrayElementType)
        {
            List<(EntityDataChanged, ulong)> events = world->entityDataChanged;
            if (events.Count > 0)
            {
                DataType type = world->schema.GetDataType(arrayElementType);
                for (uint i = 0; i < events.Count; i++)
                {
                    (EntityDataChanged callback, ulong userData) = events[i];
                    callback.Invoke(this, entity, type, ChangeType.Removed, userData);
                }
            }
        }

        private readonly void NotifyTagAdded(uint entity, TagType tagType)
        {
            List<(EntityDataChanged, ulong)> events = world->entityDataChanged;
            if (events.Count > 0)
            {
                DataType type = world->schema.GetDataType(tagType);
                for (uint i = 0; i < events.Count; i++)
                {
                    (EntityDataChanged callback, ulong userData) = events[i];
                    callback.Invoke(this, entity, type, ChangeType.Added, userData);
                }
            }
        }

        private readonly void NotifyTagRemoved(uint entity, TagType tagType)
        {
            List<(EntityDataChanged, ulong)> events = world->entityDataChanged;
            if (events.Count > 0)
            {
                DataType type = world->schema.GetDataType(tagType);
                for (uint i = 0; i < events.Count; i++)
                {
                    (EntityDataChanged callback, ulong userData) = events[i];
                    callback.Invoke(this, entity, type, ChangeType.Removed, userData);
                }
            }
        }

        readonly void ISerializable.Write(ByteWriter writer)
        {
            Allocations.ThrowIfNull(world);

            writer.WriteValue(new Signature(Version));
            writer.WriteObject(world->schema);
            writer.WriteValue(Count);
            writer.WriteValue(MaxEntityValue);
            for (uint e = 1; e < world->slots.Count; e++)
            {
                ref Slot slot = ref world->slots[e];
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
                writer.WriteValue(definition.ComponentTypes.Count);
                for (uint c = 0; c < BitMask.Capacity; c++)
                {
                    ComponentType componentType = new(c);
                    if (definition.ComponentTypes.Contains(componentType))
                    {
                        writer.WriteValue(componentType);
                        USpan<byte> componentBytes = GetComponentBytes(e, componentType);
                        writer.WriteSpan(componentBytes);
                    }
                }

                //write arrays
                writer.WriteValue(definition.ArrayElementTypes.Count);
                for (uint a = 0; a < BitMask.Capacity; a++)
                {
                    ArrayElementType arrayElementType = new(a);
                    if (definition.ArrayElementTypes.Contains(arrayElementType))
                    {
                        writer.WriteValue(arrayElementType);
                        Array array = GetArray(e, arrayElementType);
                        writer.WriteValue(array.Length);
                        writer.WriteSpan(array.AsSpan());
                    }
                }

                //write tags
                writer.WriteValue(definition.TagTypes.Count);
                for (uint t = 0; t < BitMask.Capacity; t++)
                {
                    TagType tagType = new(t);
                    if (definition.TagTypes.Contains(tagType))
                    {
                        writer.WriteValue(tagType);
                    }
                }
            }

            //write references
            for (uint e = 1; e < world->slots.Count; e++)
            {
                if (world->slots[e].state == Slot.State.Free)
                {
                    continue;
                }

                if (TryGetReferences(e, out USpan<uint> references))
                {
                    writer.WriteValue(references.Length);
                    writer.WriteSpan(references);
                }
                else
                {
                    writer.WriteValue(0);
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
            Allocations.ThrowIfNull(world);

            List<Slot> sourceSlots = sourceWorld.world->slots;
            for (uint e = 1; e < sourceSlots.Count; e++)
            {
                if (sourceWorld.world->freeEntities.Contains(e))
                {
                    continue;
                }

                ref Chunk sourceChunk = ref sourceSlots[e].chunk;
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
            Allocations.ThrowIfNull(world);

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
            Allocations.ThrowIfNull(world);

            world->entityDataChanged.Add((function, userData));
        }

        /// <summary>
        /// Adds a function that listens to when an entity's parent changes.
        /// </summary>
        public readonly void ListenToEntityParentChanges(EntityParentChanged function, ulong userData = default)
        {
            Allocations.ThrowIfNull(world);

            world->entityParentChanged.Add((function, userData));
        }

        /// <summary>
        /// Destroys the given <paramref name="entity"/> assuming it exists.
        /// </summary>
        public readonly void DestroyEntity(uint entity, bool destroyChildren = true)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(entity);

            ref Slot slot = ref world->slots[entity];
            slot.flags |= Slot.Flags.Outdated;
            slot.state = Slot.State.Free;
            if (slot.ContainsChildren)
            {
                ref List<uint> children = ref slot.children;
                USpan<uint> childrenSpan = children.AsSpan();
                if (destroyChildren)
                {
                    //destroy children
                    for (uint i = 0; i < childrenSpan.Length; i++)
                    {
                        uint child = childrenSpan[i];
                        DestroyEntity(child, true);
                    }
                }
                else
                {
                    //unparent children
                    for (uint i = 0; i < childrenSpan.Length; i++)
                    {
                        uint child = childrenSpan[i];
                        ref Slot childSlot = ref world->slots[child];
                        childSlot.parent = default;
                    }
                }
            }

            //remove from parents children list
            if (slot.parent != default)
            {
                ref List<uint> parentChildren = ref world->slots[slot.parent].children;
                parentChildren.RemoveAtBySwapping(parentChildren.IndexOf(entity));
                slot.parent = default;
            }

            slot.chunk.RemoveEntity(entity);
            world->freeEntities.Push(entity);
            NotifyDestruction(entity);
        }

        /// <summary>
        /// Copies component types from the given <paramref name="entity"/> to the destination <paramref name="buffer"/>.
        /// </summary>
        public readonly byte CopyComponentTypesTo(uint entity, USpan<ComponentType> buffer)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(entity);

            return world->slots[entity].Definition.CopyComponentTypesTo(buffer);
        }

        /// <summary>
        /// Checks if the given <paramref name="entity"/> is enabled with respect
        /// to ancestor entities.
        /// </summary>
        public readonly bool IsEnabled(uint entity)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(entity);

            return world->slots[entity].state == Slot.State.Enabled;
        }

        /// <summary>
        /// Checks if the given entity is enabled regardless
        /// of it's parents.
        /// </summary>
        public readonly bool IsLocallyEnabled(uint entity)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(entity);

            ref Slot.State state = ref world->slots[entity].state;
            return state == Slot.State.Enabled || state == Slot.State.DisabledButLocallyEnabled;
        }

        /// <summary>
        /// Assigns the enabled state of the given <paramref name="entity"/>
        /// and its descendants to the given <paramref name="enabled"/>.
        /// </summary>
        public readonly void SetEnabled(uint entity, bool enabled)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(entity);

            ref Slot entitySlot = ref world->slots[entity];
            uint parent = entitySlot.parent;
            if (parent != default)
            {
                Slot.State parentState = world->slots[parent].state;
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
                List<uint> children = entitySlot.children;

                //todo: this temporary allocation can be avoided by tracking how large the tree is
                //and then using stackalloc
                using Stack<uint> stack = new(children.Count * 2u);
                stack.PushRange(children.AsSpan());

                while (stack.Count > 0)
                {
                    uint current = stack.Pop();
                    ref Slot currentSlot = ref world->slots[current];
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
                        previousChunk.MoveEntity(current, newChunk);
                    }

                    //check through children
                    if (currentSlot.ContainsChildren && !currentSlot.ChildrenOutdated)
                    {
                        stack.PushRange(currentSlot.children.AsSpan());
                    }
                }
            }
        }

        /// <summary>
        /// Creates a new entity.
        /// </summary>
        public readonly uint CreateEntity()
        {
            Allocations.ThrowIfNull(world);

            Definition definition = default;
            if (!world->chunksMap.TryGetValue(definition, out Chunk chunk))
            {
                chunk = new(definition, world->schema);
                world->chunksMap.Add(definition, chunk);
                world->uniqueChunks.Add(chunk);
            }

            if (!world->freeEntities.TryPop(out uint entity))
            {
                entity = world->slots.Count;
                world->slots.AddDefault();
            }

            ref Slot slot = ref world->slots[entity];
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
            Allocations.ThrowIfNull(world);

            Definition definition = new(componentTypes, default, tagTypes);
            if (!world->chunksMap.TryGetValue(definition, out Chunk chunk))
            {
                chunk = new(definition, world->schema);
                world->chunksMap.Add(definition, chunk);
                world->uniqueChunks.Add(chunk);
            }

            if (!world->freeEntities.TryPop(out uint entity))
            {
                entity = world->slots.Count;
                world->slots.AddDefault();
            }

            ref Slot slot = ref world->slots[entity];
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
            Allocations.ThrowIfNull(world);

            if (!world->chunksMap.TryGetValue(definition, out Chunk chunk))
            {
                chunk = new(definition, world->schema);
                world->chunksMap.Add(definition, chunk);
                world->uniqueChunks.Add(chunk);
            }

            if (!world->freeEntities.TryPop(out uint entity))
            {
                entity = world->slots.Count;
                world->slots.AddDefault();
            }

            ref Slot slot = ref world->slots[entity];
            slot.state = Slot.State.Enabled;
            slot.chunk = chunk;

            //create arrays if necessary
            BitMask arrayElementTypes = definition.ArrayElementTypes;
            if (!arrayElementTypes.IsEmpty)
            {
                ref Array<Array> arrays = ref slot.arrays;
                arrays = new(BitMask.Capacity);
                for (uint a = 0; a < BitMask.Capacity; a++)
                {
                    if (arrayElementTypes.Contains(a))
                    {
                        ArrayElementType arrayElementType = new(a);
                        ushort arrayElementSize = world->schema.GetSize(arrayElementType);
                        arrays[arrayElementType] = new(0, arrayElementSize);
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
        public readonly uint CreateEntity(Definition definition, out Chunk chunk, out uint index)
        {
            Allocations.ThrowIfNull(world);

            if (!world->chunksMap.TryGetValue(definition, out chunk))
            {
                chunk = new(definition, world->schema);
                world->chunksMap.Add(definition, chunk);
                world->uniqueChunks.Add(chunk);
            }

            if (!world->freeEntities.TryPop(out uint entity))
            {
                entity = world->slots.Count;
                world->slots.AddDefault();
            }

            ref Slot slot = ref world->slots[entity];
            slot.state = Slot.State.Enabled;
            slot.chunk = chunk;

            //create arrays if necessary
            BitMask arrayElementTypes = definition.ArrayElementTypes;
            if (!arrayElementTypes.IsEmpty)
            {
                ref Array<Array> arrays = ref slot.arrays;
                arrays = new(BitMask.Capacity);
                for (uint a = 0; a < BitMask.Capacity; a++)
                {
                    if (arrayElementTypes.Contains(a))
                    {
                        ArrayElementType arrayElementType = new(a);
                        ushort arrayElementSize = world->schema.GetSize(arrayElementType);
                        arrays[arrayElementType] = new(0, arrayElementSize);
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
        public readonly uint CreateEntity(BitMask componentTypes, out Chunk chunk, out uint index, BitMask tagTypes = default)
        {
            Allocations.ThrowIfNull(world);

            Definition definition = new(componentTypes, default, tagTypes);
            if (!world->chunksMap.TryGetValue(definition, out chunk))
            {
                chunk = new(definition, world->schema);
                world->chunksMap.Add(definition, chunk);
                world->uniqueChunks.Add(chunk);
            }

            if (!world->freeEntities.TryPop(out uint entity))
            {
                entity = world->slots.Count;
                world->slots.AddDefault();
            }

            ref Slot slot = ref world->slots[entity];
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

            ref Chunk chunk = ref world->slots[entity].chunk;
            Definition currentDefinition = chunk.Definition;
            if (!currentDefinition.ComponentTypes.ContainsAll(definition.ComponentTypes))
            {
                return false;
            }

            if (!currentDefinition.ArrayElementTypes.ContainsAll(definition.ArrayElementTypes))
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
            Archetype archetype = new(definition, world->schema);
            Become(entity, archetype);
        }

        /// <summary>
        /// Makes the given <paramref name="entity"/> become what the <paramref name="archetype"/>
        /// argues by adding the missing components, arrays and tags.
        /// </summary>
        public readonly void Become(uint entity, Archetype archetype)
        {
            ThrowIfEntityIsMissing(entity);

            Definition currentDefinition = world->slots[entity].Definition;
            for (uint i = 0; i < BitMask.Capacity; i++)
            {
                ComponentType componentType = new(i);
                if (archetype.Contains(componentType) && !currentDefinition.Contains(componentType))
                {
                    AddComponent(entity, componentType);
                }

                ArrayElementType arrayElementType = new(i);
                if (archetype.Contains(arrayElementType) && !currentDefinition.Contains(arrayElementType))
                {
                    CreateArray(entity, arrayElementType);
                }

                TagType tagType = new(i);
                if (archetype.Contains(tagType) && !currentDefinition.Contains(tagType))
                {
                    AddTag(entity, tagType);
                }
            }
        }

        /// <summary>
        /// Creates entities to fill the given <paramref name="buffer"/>.
        /// </summary>
        public readonly void CreateEntities(USpan<uint> buffer)
        {
            for (uint i = 0; i < buffer.Length; i++)
            {
                buffer[i] = CreateEntity();
            }
        }

        /// <summary>
        /// Checks if the given <paramref name="entity"/> exists and is valid in this world.
        /// </summary>
        public readonly bool ContainsEntity(uint entity)
        {
            Allocations.ThrowIfNull(world);

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
            Allocations.ThrowIfNull(world);
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
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(entity);

            if (entity == newParent)
            {
                throw new InvalidOperationException($"Entity {entity} cannot be its own parent");
            }

            if (!ContainsEntity(newParent))
            {
                newParent = default;
            }

            ref Slot entitySlot = ref world->slots[entity];
            bool parentChanged = entitySlot.parent != newParent;
            if (parentChanged)
            {
                uint oldParent = entitySlot.parent;
                entitySlot.parent = newParent;

                //remove from previous parent children list
                if (oldParent != default)
                {
                    ref Slot oldParentSlot = ref world->slots[oldParent];
                    oldParentSlot.children.RemoveAtBySwapping(oldParentSlot.children.IndexOf(entity));
                }

                ref Slot newParentSlot = ref world->slots[newParent];
                if (!newParentSlot.ContainsChildren)
                {
                    newParentSlot.children = new(4);
                    newParentSlot.flags |= Slot.Flags.ContainsChildren;
                    newParentSlot.flags &= ~Slot.Flags.ChildrenOutdated;
                }
                else if (newParentSlot.ChildrenOutdated)
                {
                    newParentSlot.children.Clear();
                    newParentSlot.flags &= ~Slot.Flags.ChildrenOutdated;
                }

                newParentSlot.children.Add(entity);

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
        /// Retrieves all children of the <paramref name="entity"/> entity.
        /// </summary>
        public readonly USpan<uint> GetChildren(uint entity)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(entity);

            ref Slot slot = ref world->slots[entity];
            if (slot.ContainsChildren && !slot.ChildrenOutdated)
            {
                return world->slots[entity].children.AsSpan();
            }
            else
            {
                return default;
            }
        }

        /// <summary>
        /// Tries to retrieve all children of the <paramref name="entity"/> entity.
        /// </summary>
        public readonly bool TryGetChildren(uint entity, out USpan<uint> children)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(entity);

            ref Slot slot = ref world->slots[entity];
            if (slot.ContainsChildren && !slot.ChildrenOutdated)
            {
                children = world->slots[entity].children.AsSpan();
                return true;
            }
            else
            {
                children = default;
                return false;
            }
        }

        /// <summary>
        /// Retrieves all entities referenced by <paramref name="entity"/>.
        /// </summary>
        public readonly USpan<uint> GetReferences(uint entity)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(entity);

            ref Slot slot = ref world->slots[entity];
            if (slot.ContainsReferences && !slot.ReferencesOutdated)
            {
                return world->slots[entity].references.AsSpan(1);
            }
            else
            {
                return default;
            }
        }

        /// <summary>
        /// Tries to retrieve all entities referenced by <paramref name="entity"/>.
        /// </summary>
        public readonly bool TryGetReferences(uint entity, out USpan<uint> references)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(entity);

            ref Slot slot = ref world->slots[entity];
            if (slot.ContainsReferences && !slot.ReferencesOutdated)
            {
                references = world->slots[entity].references.AsSpan(1);
                return true;
            }
            else
            {
                references = default;
                return false;
            }
        }

        /// <summary>
        /// Retrieves the number of children the given <paramref name="entity"/> has.
        /// </summary>
        public readonly uint GetChildCount(uint entity)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(entity);

            ref Slot slot = ref world->slots[entity];
            if (slot.ContainsChildren && !slot.ChildrenOutdated)
            {
                return slot.children.Count;
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
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfEntityIsMissing(referencedEntity);

            ref Slot slot = ref world->slots[entity];
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

            uint count = slot.references.Count;
            slot.references.Add(referencedEntity);
            return (rint)count;
        }

        /// <summary>
        /// Updates an existing <paramref name="reference"/> to point towards the <paramref name="referencedEntity"/>.
        /// </summary>
        public readonly void SetReference(uint entity, rint reference, uint referencedEntity)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfReferenceIsMissing(entity, reference);

            world->slots[entity].references[(uint)reference] = referencedEntity;
        }

        /// <summary>
        /// Checks if the given entity contains a reference to the given <paramref name="referencedEntity"/>.
        /// </summary>
        public readonly bool ContainsReference(uint entity, uint referencedEntity)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(entity);

            ref Slot slot = ref world->slots[entity];
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
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(entity);

            ref Slot slot = ref world->slots[entity];
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
        public readonly uint GetReferenceCount(uint entity)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(entity);

            ref Slot slot = ref world->slots[entity];
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
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfReferenceIsMissing(entity, reference);

            return ref world->slots[entity].references[(uint)reference];
        }

        /// <summary>
        /// Retrieves the <see cref="rint"/> value that points to the given <paramref name="referencedEntity"/> on <paramref name="entity"/>.
        /// </summary>
        public readonly rint GetReference(uint entity, uint referencedEntity)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfReferencedEntityIsMissing(entity, referencedEntity);

            ref Slot slot = ref world->slots[entity];
            uint index = slot.references.IndexOf(referencedEntity);
            return (rint)(index + 1);
        }

        /// <summary>
        /// Attempts to retrieve the referenced entity at the given <paramref name="reference"/> on <paramref name="entity"/>.
        /// </summary>
        public readonly bool TryGetReference(uint entity, rint reference, out uint referencedEntity)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(entity);

            ref Slot slot = ref world->slots[entity];
            if (slot.ContainsReferences && !slot.ReferencesOutdated)
            {
                uint index = (uint)reference;
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
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfReferenceIsMissing(entity, reference);

            world->slots[entity].references.RemoveAt((uint)reference, out uint removed);
            return removed;
        }

        /// <summary>
        /// Removes the <paramref name="referencedEntity"/> from <paramref name="entity"/>.
        /// </summary>
        /// <returns>The reference that was removed.</returns>
        public readonly rint RemoveReference(uint entity, uint referencedEntity)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfReferencedEntityIsMissing(entity, referencedEntity);

            ref List<uint> references = ref world->slots[entity].references;
            uint count = references.Count;
            references.RemoveAt(references.IndexOf(referencedEntity));
            return (rint)count;
        }

        /// <summary>
        /// Writes all tag types on <paramref name="entity"/> to <paramref name="destination"/>.
        /// </summary>
        public readonly byte CopyTagTypesTo(uint entity, USpan<TagType> destination)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(entity);

            Definition definition = world->slots[entity].Definition;
            return definition.CopyTagTypesTo(destination);
        }

        public readonly bool ContainsTag<T>(uint entity) where T : unmanaged
        {
            Allocations.ThrowIfNull(world);

            TagType tagType = world->schema.GetTag<T>();
            return ContainsTag(entity, tagType);
        }

        public readonly bool ContainsTag(uint entity, TagType tagType)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(entity);

            return world->slots[entity].Definition.TagTypes.Contains(tagType);
        }

        public readonly void AddTag<T>(uint entity) where T : unmanaged
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(entity);

            TagType tagType = world->schema.GetTag<T>();
            ThrowIfTagAlreadyPresent(entity, tagType);

            ref Slot slot = ref world->slots[entity];
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
            uint index = previousChunk.MoveEntity(entity, destinationChunk);
            NotifyTagAdded(entity, tagType);
        }

        public readonly void AddTag(uint entity, TagType tagType)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfTagAlreadyPresent(entity, tagType);

            ref Slot slot = ref world->slots[entity];
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
            uint index = previousChunk.MoveEntity(entity, destinationChunk);
            NotifyTagAdded(entity, tagType);
        }

        public readonly void RemoveTag<T>(uint entity) where T : unmanaged
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(entity);

            TagType tagType = world->schema.GetTag<T>();
            ThrowIfTagIsMissing(entity, tagType);

            ref Slot slot = ref world->slots[entity];
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

        public readonly void RemoveTag(uint entity, TagType tagType)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfTagIsMissing(entity, tagType);

            ref Slot slot = ref world->slots[entity];
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
        public readonly byte CopyArrayElementTypesTo(uint entity, USpan<ArrayElementType> destination)
        {
            ThrowIfEntityIsMissing(entity);

            ref Chunk chunk = ref world->slots[entity].chunk;
            BitMask arrayElementTypes = chunk.Definition.ArrayElementTypes;
            byte count = 0;
            for (uint a = 0; a < BitMask.Capacity; a++)
            {
                if (arrayElementTypes.Contains(a))
                {
                    destination[count++] = new(a);
                }
            }

            return count;
        }

        /// <summary>
        /// Retrieves the types of all arrays on this entity.
        /// </summary>
        public readonly BitMask GetArrayElementTypes(uint entity)
        {
            ThrowIfEntityIsMissing(entity);

            return world->slots[entity].Definition.ArrayElementTypes;
        }

        public readonly BitMask GetTagTypes(uint entity)
        {
            ThrowIfEntityIsMissing(entity);

            return world->slots[entity].Definition.TagTypes;
        }

        /// <summary>
        /// Creates a new empty array with the given <paramref name="length"/> and <paramref name="arrayType"/>.
        /// </summary>
        public readonly Array CreateArray(uint entity, ArrayElementType arrayType, uint length = 0)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfArrayIsAlreadyPresent(entity, arrayType);

            ushort stride = world->schema.GetSize(arrayType);
            ref Slot slot = ref world->slots[entity];
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
                for (uint i = 0; i < slot.arrays.Length; i++)
                {
                    ref Array array = ref slot.arrays[i];
                    if (!array.IsDisposed)
                    {
                        array.Dispose();
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

            Array newArray = new(length, stride);
            slot.arrays[arrayType] = newArray;
            NotifyArrayCreated(entity, arrayType);
            return newArray;
        }

        /// <summary>
        /// Creates a new empty array with the given <paramref name="length"/> and <paramref name="arrayType"/>.
        /// </summary>
        public readonly Array CreateArray(uint entity, ArrayElementType arrayType, uint stride, uint length = 0)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfArrayIsAlreadyPresent(entity, arrayType);

            ref Slot slot = ref world->slots[entity];
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
                for (uint i = 0; i < slot.arrays.Length; i++)
                {
                    ref Array array = ref slot.arrays[i];
                    if (!array.IsDisposed)
                    {
                        array.Dispose();
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

            Array newArray = new(length, stride);
            slot.arrays[arrayType] = newArray;
            NotifyArrayCreated(entity, arrayType);
            return newArray;
        }

        /// <summary>
        /// Creates a new empty array with the given <paramref name="length"/> and <paramref name="dataType"/>.
        /// </summary>
        public readonly Array CreateArray(uint entity, DataType dataType, uint length = 0)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(entity);

            ArrayElementType arrayType = dataType.ArrayElementType;
            ThrowIfArrayIsAlreadyPresent(entity, arrayType);

            ref Slot slot = ref world->slots[entity];
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
                for (uint i = 0; i < slot.arrays.Length; i++)
                {
                    ref Array array = ref slot.arrays[i];
                    if (!array.IsDisposed)
                    {
                        array.Dispose();
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

            Array newArray = new(length, dataType.size);
            slot.arrays[arrayType] = newArray;
            NotifyArrayCreated(entity, arrayType);
            return newArray;
        }

        /// <summary>
        /// Creates a new empty array on this <paramref name="entity"/>.
        /// </summary>
        public readonly Array<T> CreateArray<T>(uint entity, uint length = 0) where T : unmanaged
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(entity);

            ArrayElementType arrayType = world->schema.GetArrayElement<T>();
            ThrowIfArrayIsAlreadyPresent(entity, arrayType);

            ref Slot slot = ref world->slots[entity];
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
                for (uint i = 0; i < slot.arrays.Length; i++)
                {
                    ref Array array = ref slot.arrays[i];
                    if (!array.IsDisposed)
                    {
                        array.Dispose();
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

            Array newArray = new(length, (ushort)sizeof(T));
            slot.arrays[arrayType] = newArray;
            NotifyArrayCreated(entity, arrayType);
            return newArray.AsArray<T>();
        }

        /// <summary>
        /// Creates a new array containing the given <paramref name="values"/>.
        /// </summary>
        public readonly void CreateArray<T>(uint entity, USpan<T> values) where T : unmanaged
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(entity);

            Array<T> array = CreateArray<T>(entity, values.Length);
            values.CopyTo(array.AsSpan());
        }

        /// <summary>
        /// Checks if the given <paramref name="entity"/> contains an array of type <typeparamref name="T"/>.
        /// </summary>
        public readonly bool ContainsArray<T>(uint entity) where T : unmanaged
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(entity);

            ArrayElementType arrayType = world->schema.GetArrayElement<T>();
            return world->slots[entity].Definition.ArrayElementTypes.Contains(arrayType);
        }

        /// <summary>
        /// Checks if the given <paramref name="entity"/> contains an array of the given <paramref name="arrayType"/>.
        /// </summary>
        public readonly bool ContainsArray(uint entity, ArrayElementType arrayType)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(entity);

            return world->slots[entity].Definition.ArrayElementTypes.Contains(arrayType);
        }

        /// <summary>
        /// Retrieves the array of type <typeparamref name="T"/> from the given <paramref name="entity"/>.
        /// </summary>
        public readonly Array<T> GetArray<T>(uint entity) where T : unmanaged
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(entity);

            ArrayElementType arrayType = world->schema.GetArrayElement<T>();
            ThrowIfArrayIsMissing(entity, arrayType);

            ref Slot slot = ref world->slots[entity];
            return slot.arrays[(uint)arrayType].AsArray<T>();
        }

        /// <summary>
        /// Retrieves the array of type <typeparamref name="T"/> from the given <paramref name="entity"/>.
        /// </summary>
        public readonly Array<T> GetArray<T>(uint entity, ArrayElementType arrayType) where T : unmanaged
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfArrayIsMissing(entity, arrayType);

            ref Slot slot = ref world->slots[entity];
            return slot.arrays[(uint)arrayType].AsArray<T>();
        }

        /// <summary>
        /// Retrieves the array of the given <paramref name="arrayType"/> from the given <paramref name="entity"/>.
        /// </summary>
        public readonly Array GetArray(uint entity, ArrayElementType arrayType)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfArrayIsMissing(entity, arrayType);

            ref Slot slot = ref world->slots[entity];
            return slot.arrays[(uint)arrayType];
        }

        /// <summary>
        /// Attempts to retrieve an array of type <typeparamref name="T"/> from the given <paramref name="entity"/>.
        /// </summary>
        public readonly bool TryGetArray<T>(uint entity, out Array<T> array) where T : unmanaged
        {
            ArrayElementType arrayType = world->schema.GetArrayElement<T>();
            if (ContainsArray(entity, arrayType))
            {
                array = GetArray<T>(entity, arrayType);
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
        public readonly ref T GetArrayElement<T>(uint entity, uint index) where T : unmanaged
        {
            ThrowIfEntityIsMissing(entity);

            ArrayElementType arrayType = world->schema.GetArrayElement<T>();
            ThrowIfArrayIsMissing(entity, arrayType);

            ref Slot slot = ref world->slots[entity];
            return ref slot.arrays[(uint)arrayType].Get<T>(index);
        }

        /// <summary>
        /// Retrieves the length of an existing array on this entity.
        /// </summary>
        public readonly uint GetArrayLength<T>(uint entity) where T : unmanaged
        {
            ThrowIfEntityIsMissing(entity);

            ArrayElementType arrayType = world->schema.GetArrayElement<T>();
            ThrowIfArrayIsMissing(entity, arrayType);

            ref Slot slot = ref world->slots[entity];
            return slot.arrays[(uint)arrayType].Length;
        }

        /// <summary>
        /// Retrieves the length of an existing array on this entity.
        /// </summary>
        public readonly uint GetArrayLength(uint entity, ArrayElementType arrayType)
        {
            ThrowIfEntityIsMissing(entity);
            ThrowIfArrayIsMissing(entity, arrayType);

            ref Slot slot = ref world->slots[entity];
            return slot.arrays[(uint)arrayType].Length;
        }

        /// <summary>
        /// Destroys the array of type <typeparamref name="T"/> on the given <paramref name="entity"/>.
        /// </summary>
        public readonly void DestroyArray<T>(uint entity) where T : unmanaged
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(entity);

            ArrayElementType arrayType = world->schema.GetArrayElement<T>();
            ThrowIfArrayIsMissing(entity, arrayType);

            ref Slot slot = ref world->slots[entity];
            ref Array array = ref slot.arrays[arrayType];
            array.Dispose();

            Chunk previousChunk = slot.chunk;
            Definition newDefinition = previousChunk.Definition;
            newDefinition.RemoveArrayElementType(arrayType);

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
        public readonly void DestroyArray(uint entity, ArrayElementType arrayType)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfArrayIsMissing(entity, arrayType);

            ref Slot slot = ref world->slots[entity];
            ref Array array = ref slot.arrays[arrayType];
            array.Dispose();

            Chunk previousChunk = slot.chunk;
            Definition newDefinition = previousChunk.Definition;
            newDefinition.RemoveArrayElementType(arrayType);

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
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(entity);

            ComponentType componentType = world->schema.GetComponent<T>();
            ThrowIfComponentAlreadyPresent(entity, componentType);

            ref Slot slot = ref world->slots[entity];
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
            uint index = previousChunk.MoveEntity(entity, destinationChunk);
            destinationChunk.SetComponent(index, componentType, component);
            NotifyComponentAdded(entity, componentType);
        }

        /// <summary>
        /// Adds a <typeparamref name="T"/> component with <see langword="default"/> memory to <paramref name="entity"/>,
        /// and returns it by reference.
        /// </summary>
        public readonly ref T AddComponent<T>(uint entity) where T : unmanaged
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(entity);

            ComponentType componentType = world->schema.GetComponent<T>();
            ThrowIfComponentAlreadyPresent(entity, componentType);

            ref Slot slot = ref world->slots[entity];
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
            uint index = previousChunk.MoveEntity(entity, destinationChunk);
            NotifyComponentAdded(entity, componentType);
            return ref destinationChunk.GetComponent<T>(index, componentType);
        }

        /// <summary>
        /// Adds a new default component with the given type.
        /// </summary>
        public readonly Allocation AddComponent(uint entity, ComponentType componentType)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfComponentAlreadyPresent(entity, componentType);

            ref Slot slot = ref world->slots[entity];
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
            uint index = previousChunk.MoveEntity(entity, destinationChunk);
            NotifyComponentAdded(entity, componentType);
            return destinationChunk.GetComponent(index, componentType);
        }

        /// <summary>
        /// Adds a new default component with the given type.
        /// </summary>
        public readonly Allocation AddComponent(uint entity, ComponentType componentType, out ushort componentSize)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfComponentAlreadyPresent(entity, componentType);

            ref Slot slot = ref world->slots[entity];
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
            uint index = previousChunk.MoveEntity(entity, destinationChunk);
            NotifyComponentAdded(entity, componentType);
            return destinationChunk.GetComponent(index, componentType, out componentSize);
        }

        /// <summary>
        /// Adds a new component of the given <paramref name="componentType"/> with <paramref name="source"/> bytes
        /// to <paramref name="entity"/>.
        /// </summary>
        public readonly void AddComponent(uint entity, ComponentType componentType, USpan<byte> source)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfComponentAlreadyPresent(entity, componentType);

            ref Slot slot = ref world->slots[entity];
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
            uint index = previousChunk.MoveEntity(entity, destinationChunk);
            Allocation component = destinationChunk.GetComponent(index, componentType, out ushort componentSize);
            source.CopyTo(component, Math.Min(componentSize, source.Length)); //todo: efficiency: this could be eliminated, but would need awareness given to the user about the size of the component
        }

        /// <summary>
        /// Removes the component of type <typeparamref name="T"/> from the given <paramref name="entity"/>.
        /// </summary>
        public readonly void RemoveComponent<T>(uint entity) where T : unmanaged
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(entity);

            ComponentType componentType = world->schema.GetComponent<T>();
            ThrowIfComponentMissing(entity, componentType);

            ref Slot slot = ref world->slots[entity];
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
        public readonly void RemoveComponent(uint entity, ComponentType componentType)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfComponentMissing(entity, componentType);

            ref Slot slot = ref world->slots[entity];
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
            ComponentType componentType = world->schema.GetComponent<T>();
            foreach (Chunk chunk in world->uniqueChunks)
            {
                if (chunk.Definition.Contains(componentType))
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
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(entity);

            ComponentType componentType = world->schema.GetComponent<T>();
            return world->slots[entity].Definition.ComponentTypes.Contains(componentType);
        }

        /// <summary>
        /// Checks if the given <paramref name="entity"/> contains a component of <paramref name="componentType"/>.
        /// </summary>
        public readonly bool ContainsComponent(uint entity, ComponentType componentType)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(entity);

            return world->slots[entity].Definition.ComponentTypes.Contains(componentType);
        }

        /// <summary>
        /// Retrieves a reference to the component of type <typeparamref name="T"/> from the given <paramref name="entity"/>.
        /// </summary>
        public readonly ref T GetComponent<T>(uint entity) where T : unmanaged
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(entity);

            ComponentType componentType = world->schema.GetComponent<T>();
            ThrowIfComponentMissing(entity, componentType);

            ref Chunk chunk = ref world->slots[entity].chunk;
            uint index = chunk.Entities.IndexOf(entity);
            return ref chunk.GetComponent<T>(index, componentType);
        }

        /// <summary>
        /// Retrieves the component of type <typeparamref name="T"/> if it exists, otherwise the given
        /// <paramref name="defaultValue"/> is returned.
        /// </summary>
        public readonly T GetComponentOrDefault<T>(uint entity, T defaultValue = default) where T : unmanaged
        {
            ComponentType componentType = world->schema.GetComponent<T>();
            if (ContainsComponent(entity, componentType))
            {
                return GetComponent<T>(entity, componentType);
            }
            else
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Retrieves a reference to the component of type <typeparamref name="T"/>.
        /// </summary>
        public readonly ref T GetComponent<T>(uint entity, ComponentType componentType) where T : unmanaged
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfComponentMissing(entity, componentType);

            ref Chunk chunk = ref world->slots[entity].chunk;
            uint index = chunk.Entities.IndexOf(entity);
            return ref chunk.GetComponent<T>(index, componentType);
        }

        /// <summary>
        /// Retrieves the component of the given <paramref name="componentType"/> from the given <paramref name="entity"/>
        /// as a pointer.
        /// </summary>
        public readonly Allocation GetComponent(uint entity, ComponentType componentType)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfComponentMissing(entity, componentType);

            ref Chunk chunk = ref world->slots[entity].chunk;
            uint index = chunk.Entities.IndexOf(entity);
            return chunk.GetComponent(index, componentType);
        }

        /// <summary>
        /// Retrieves the component of the given <paramref name="componentType"/> from the given <paramref name="entity"/>
        /// as a pointer.
        /// </summary>
        public readonly Allocation GetComponent(uint entity, ComponentType componentType, out ushort componentSize)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfComponentMissing(entity, componentType);

            ref Chunk chunk = ref world->slots[entity].chunk;
            uint index = chunk.Entities.IndexOf(entity);
            return chunk.GetComponent(index, componentType, out componentSize);
        }

        /// <summary>
        /// Fetches the bytes of a component from the given <paramref name="entity"/>.
        /// </summary>
        public readonly USpan<byte> GetComponentBytes(uint entity, ComponentType componentType)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfComponentMissing(entity, componentType);

            ref Chunk chunk = ref world->slots[entity].chunk;
            uint index = chunk.Entities.IndexOf(entity);
            Allocation component = chunk.GetComponent(index, componentType, out ushort componentSize);
            return new(component.Pointer, componentSize);
        }

        /// <summary>
        /// Retrieves the component from the given <paramref name="entity"/> as an <see cref="object"/>.
        /// </summary>
        public readonly object GetComponentObject(uint entity, ComponentType componentType)
        {
            Allocations.ThrowIfNull(world);

            TypeLayout layout = componentType.GetLayout(world->schema);
            USpan<byte> bytes = GetComponentBytes(entity, componentType);
            return layout.CreateInstance(bytes);
        }

        /// <summary>
        /// Retrieves the array from the given <paramref name="entity"/> as <see cref="object"/>s.
        /// </summary>
        public readonly object[] GetArrayObject(uint entity, ArrayElementType arrayElementType)
        {
            Allocations.ThrowIfNull(world);

            TypeLayout layout = arrayElementType.GetLayout(world->schema);
            Array array = GetArray(entity, arrayElementType);
            ushort size = layout.Size;
            object[] arrayObject = new object[array.Length];
            for (uint i = 0; i < array.Length; i++)
            {
                Allocation allocation = array[i];
                arrayObject[i] = layout.CreateInstance(new(allocation.Pointer, size));
            }

            return arrayObject;
        }

        /// <summary>
        /// Attempts to retrieve a reference to the component of type <typeparamref name="T"/> from the given <paramref name="entity"/>.
        /// </summary>
        /// <returns><c>true</c> if the component is found.</returns>
        public readonly ref T TryGetComponent<T>(uint entity, out bool contains) where T : unmanaged
        {
            Allocations.ThrowIfNull(world);

            ComponentType componentType = world->schema.GetComponent<T>();
            ref Chunk chunk = ref world->slots[entity].chunk;
            contains = chunk.Definition.ComponentTypes.Contains(componentType);
            if (contains)
            {
                uint index = chunk.Entities.IndexOf(entity);
                return ref chunk.GetComponent<T>(index, componentType);
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
        public readonly bool TryGetComponent<T>(uint entity, out T component) where T : unmanaged
        {
            Allocations.ThrowIfNull(world);

            ComponentType componentType = world->schema.GetComponent<T>();
            ref Chunk chunk = ref world->slots[entity].chunk;
            if (chunk.Definition.ComponentTypes.Contains(componentType))
            {
                uint index = chunk.Entities.IndexOf(entity);
                component = chunk.GetComponent<T>(index, componentType);
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
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(entity);

            ComponentType componentType = world->schema.GetComponent<T>();
            ThrowIfComponentMissing(entity, componentType);

            ref Chunk chunk = ref world->slots[entity].chunk;
            uint index = chunk.Entities.IndexOf(entity);
            chunk.SetComponent(index, componentType, component);
        }

        /// <summary>
        /// Assigns the given <paramref name="componentData"/> to the given <paramref name="entity"/>.
        /// </summary>
        public readonly void SetComponentBytes(uint entity, ComponentType componentType, USpan<byte> componentData)
        {
            Allocations.ThrowIfNull(world);
            ThrowIfEntityIsMissing(entity);
            ThrowIfComponentMissing(entity, componentType);

            ref Chunk chunk = ref world->slots[entity].chunk;
            uint index = chunk.Entities.IndexOf(entity);
            Allocation component = chunk.GetComponent(index, componentType, out ushort componentSize);
            componentData.CopyTo(component, componentSize);
        }

        /// <summary>
        /// Returns the chunk that contains the given <paramref name="entity"/>.
        /// </summary>
        public readonly Chunk GetChunk(uint entity)
        {
            ThrowIfEntityIsMissing(entity);

            return world->slots[entity].chunk;
        }

        /// <summary>
        /// Copies components from the source entity onto the destination.
        /// <para>Components will be added if the destination entity doesnt
        /// contain them. Existing component data will be overwritten.</para>
        /// </summary>
        public readonly void CopyComponentsTo(uint sourceEntity, World destinationWorld, uint destinationEntity)
        {
            ThrowIfEntityIsMissing(sourceEntity);

            Chunk sourceChunk = world->slots[sourceEntity].chunk;
            Definition sourceComponentTypes = sourceChunk.Definition;
            uint sourceIndex = sourceChunk.Entities.IndexOf(sourceEntity);
            for (uint c = 0; c < BitMask.Capacity; c++)
            {
                if (sourceComponentTypes.ComponentTypes.Contains(c))
                {
                    ComponentType componentType = new(c);
                    if (!destinationWorld.ContainsComponent(destinationEntity, componentType))
                    {
                        destinationWorld.AddComponent(destinationEntity, componentType);
                    }

                    Allocation sourceComponent = sourceChunk.GetComponent(sourceIndex, componentType, out ushort componentSize);
                    Allocation destinationComponent = destinationWorld.GetComponent(destinationEntity, componentType);
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
            for (uint a = 0; a < BitMask.Capacity; a++)
            {
                if (arrayElementTypes.Contains(a))
                {
                    ArrayElementType arrayElementType = new(a);
                    Array sourceArray = GetArray(sourceEntity, arrayElementType);
                    Array destinationArray;
                    if (!destinationWorld.ContainsArray(destinationEntity, arrayElementType))
                    {
                        destinationArray = CreateArray(destinationEntity, arrayElementType, sourceArray.Stride, sourceArray.Length);
                    }
                    else
                    {
                        destinationArray = GetArray(destinationEntity, arrayElementType);
                        destinationArray.Length = sourceArray.Length;
                    }

                    sourceArray.AsSpan().CopyTo(destinationArray.AsSpan(), sourceArray.Length * sourceArray.Stride);
                }
            }
        }

        public readonly void CopyTagsTo(uint sourceEntity, World destinationWorld, uint destinationEntity)
        {
            BitMask tagTypes = GetTagTypes(sourceEntity);
            for (uint t = 0; t < BitMask.Capacity; t++)
            {
                if (tagTypes.Contains(t))
                {
                    TagType tagType = new(t);
                    if (!destinationWorld.ContainsTag(destinationEntity, tagType))
                    {
                        destinationWorld.AddTag(destinationEntity, tagType);
                    }
                }
            }
        }

        public readonly void CopyReferencesTo(uint sourceEntity, World destinationWorld, uint destinationEntity)
        {
            uint referenceCount = GetReferenceCount(sourceEntity);
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

            ref Slot.State state = ref world->slots[entity].state;
            if (state == Slot.State.Free)
            {
                throw new NullReferenceException($"Entity `{entity}` not found");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfReferenceIsMissing(uint entity, rint reference)
        {
            ref Slot slot = ref world->slots[entity];
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
            ref Slot slot = ref world->slots[entity];
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
        private readonly void ThrowIfComponentMissing(uint entity, ComponentType componentType)
        {
            BitMask componentTypes = world->slots[entity].Definition.ComponentTypes;
            if (!componentTypes.Contains(componentType))
            {
                Entity thisEntity = new(new(world), entity);
                throw new NullReferenceException($"Component `{componentType.ToString(world->schema)}` not found on `{entity}`");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfComponentAlreadyPresent(uint entity, ComponentType componentType)
        {
            BitMask componentTypes = world->slots[entity].Definition.ComponentTypes;
            if (componentTypes.Contains(componentType))
            {
                throw new InvalidOperationException($"Component `{componentType.ToString(world->schema)}` already present on `{entity}`");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfTagAlreadyPresent(uint entity, TagType tagType)
        {
            BitMask tagTypes = world->slots[entity].Definition.TagTypes;
            if (tagTypes.Contains(tagType))
            {
                throw new InvalidOperationException($"Tag `{tagType.ToString(world->schema)}` already present on `{entity}`");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfTagIsMissing(uint entity, TagType tagType)
        {
            BitMask tagTypes = world->slots[entity].Definition.TagTypes;
            if (!tagTypes.Contains(tagType))
            {
                throw new NullReferenceException($"Tag `{tagType.ToString(world->schema)}` not found on `{entity}`");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfArrayIsMissing(uint entity, ArrayElementType arrayElementType)
        {
            BitMask arrayElementTypes = world->slots[entity].Definition.ArrayElementTypes;
            if (!arrayElementTypes.Contains(arrayElementType))
            {
                throw new NullReferenceException($"Array of type `{arrayElementType.ToString(world->schema)}` not found on entity `{entity}`");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfArrayIsAlreadyPresent(uint entity, ArrayElementType arrayElementType)
        {
            BitMask arrayElementTypes = world->slots[entity].Definition.ArrayElementTypes;
            if (arrayElementTypes.Contains(arrayElementType))
            {
                throw new InvalidOperationException($"Array of type `{arrayElementType.ToString(world->schema)}` already present on `{entity}`");
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
            uint entityCount = reader.ReadValue<uint>();
            uint slotCount = reader.ReadValue<uint>();

            //todo: this could be a stackalloc span instead
            using Array<uint> entityMap = new(slotCount + 1);

            for (uint i = 0; i < entityCount; i++)
            {
                uint entity = reader.ReadValue<uint>();
                Slot.State state = reader.ReadValue<Slot.State>();
                uint parent = reader.ReadValue<uint>();

                uint createdEntity = value.CreateEntity(default, out _, out _);
                entityMap[entity] = createdEntity;
                ref Slot slot = ref value.world->slots[createdEntity];
                slot.state = state;
                slot.parent = parent;

                //read components
                byte componentCount = reader.ReadValue<byte>();
                for (uint c = 0; c < componentCount; c++)
                {
                    ComponentType componentType = reader.ReadValue<ComponentType>();
                    Allocation component = value.AddComponent(createdEntity, componentType, out ushort componentSize);
                    USpan<byte> componentData = reader.ReadSpan<byte>(componentSize);
                    componentData.CopyTo(component, componentSize);
                }

                //read arrays
                byte arrayCount = reader.ReadValue<byte>();
                for (uint a = 0; a < arrayCount; a++)
                {
                    ArrayElementType arrayElementType = reader.ReadValue<ArrayElementType>();
                    uint length = reader.ReadValue<uint>();
                    ushort arrayElementSize = schema.GetSize(arrayElementType);
                    Array array = value.CreateArray(createdEntity, arrayElementType, arrayElementSize, length);
                    USpan<byte> arrayData = reader.ReadSpan<byte>(length * arrayElementSize);
                    arrayData.CopyTo(array.AsSpan());
                }

                //read tags
                byte tagCount = reader.ReadValue<byte>();
                for (uint t = 0; t < tagCount; t++)
                {
                    TagType tagType = reader.ReadValue<TagType>();
                    value.AddTag(createdEntity, tagType);
                }
            }

            //assign references and children
            for (uint e = 1; e < value.world->slots.Count; e++)
            {
                ref Slot slot = ref value.world->slots[e];
                if (slot.state == Slot.State.Free)
                {
                    continue;
                }

                uint referenceCount = reader.ReadValue<uint>();
                if (referenceCount > 0)
                {
                    slot.flags |= Slot.Flags.ContainsReferences;
                    ref List<uint> references = ref slot.references;
                    references = new(referenceCount + 1);
                    for (uint r = 0; r < referenceCount; r++)
                    {
                        uint referencedEntity = reader.ReadValue<uint>();
                        uint createdReferencesEntity = entityMap[referencedEntity];
                        references.Add(createdReferencesEntity);
                    }
                }

                uint parent = slot.parent;
                if (parent != default)
                {
                    ref Slot parentSlot = ref value.world->slots[parent];
                    if (!parentSlot.ContainsChildren)
                    {
                        parentSlot.flags |= Slot.Flags.ContainsChildren;
                        parentSlot.children = new(4);
                    }

                    parentSlot.children.Add(e);
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