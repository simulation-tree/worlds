using Collections;
using System;
using System.Diagnostics;
using Types;
using Unmanaged;
using Worlds.Functions;
using Array = Collections.Implementations.Array;

namespace Worlds
{
    /// <summary>
    /// Contains arbitrary data sorted into groups of entities for processing.
    /// </summary>
    public unsafe struct World : IDisposable, IEquatable<World>, ISerializable
    {
        public const uint Version = 1;

        private Implementation* value;

        /// <summary>
        /// Native address of the world.
        /// </summary>
        public readonly nint Address => (nint)value;

        /// <summary>
        /// The native implementation pointer.
        /// </summary>
        public readonly Implementation* Pointer => value;

        /// <summary>
        /// Amount of entities that exist in the world.
        /// </summary>
        public readonly uint Count
        {
            get
            {
                Allocations.ThrowIfNull(value);

                return value->slots.Count - value->freeEntities.Count - 1;
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
                Allocations.ThrowIfNull(value);

                return value->slots.Count - 1;
            }
        }

        /// <summary>
        /// Checks if the world has been disposed.
        /// </summary>
        public readonly bool IsDisposed => value is null;

        /// <summary>
        /// All available slots.
        /// </summary>
        private readonly List<Slot> Slots => value->slots;

        /// <summary>
        /// All previously used entities that are now free.
        /// </summary>
        public readonly List<uint> Free
        {
            get
            {
                Allocations.ThrowIfNull(value);

                return value->freeEntities;
            }
        }

        /// <summary>
        /// Dictionary mapping <see cref="Definition"/>s to <see cref="Chunk"/>s.
        /// </summary>
        public readonly Dictionary<Definition, Chunk> ChunksMap
        {
            get
            {
                Allocations.ThrowIfNull(value);

                return value->chunksMap;
            }
        }

        /// <summary>
        /// All <see cref="Chunk"/>s in the world.
        /// </summary>
        public readonly USpan<Chunk> Chunks
        {
            get
            {
                Allocations.ThrowIfNull(value);

                return value->uniqueChunks.AsSpan();
            }
        }

        /// <summary>
        /// The schema containing all component and array types.
        /// </summary>
        public readonly Schema Schema
        {
            get
            {
                Allocations.ThrowIfNull(value);

                return value->schema;
            }
        }

        /// <summary>
        /// All entities that exist in the world.
        /// </summary>
        public readonly System.Collections.Generic.IEnumerable<uint> Entities
        {
            get
            {
                List<uint> free = Free;
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
                Allocations.ThrowIfNull(value);

                uint i = 0;
                for (uint e = 1; e < value->slots.Count; e++)
                {
                    if (!Free.Contains(e))
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
            value = Implementation.Allocate(new());
        }
#endif

        /// <summary>
        /// Creates a new world with the given <paramref name="schema"/>.
        /// </summary>
        public World(Schema schema)
        {
            value = Implementation.Allocate(schema);
        }

        /// <summary>
        /// Initializes an existing world from the given <paramref name="pointer"/>.
        /// </summary>
        public World(void* pointer)
        {
            this.value = (Implementation*)pointer;
        }

        /// <summary>
        /// Disposes everything in the world and the world itself.
        /// </summary>
        public void Dispose()
        {
            Implementation.Free(ref value);
        }

        /// <summary>
        /// Resets the world to <see langword="default"/> state.
        /// </summary>
        public readonly void Clear()
        {
            Implementation.Clear(value);
        }

        /// <inheritdoc/>
        public readonly override string ToString()
        {
            if (value == default)
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
            return ((nint)value).GetHashCode();
        }

        readonly void ISerializable.Write(ByteWriter writer)
        {
            Implementation.Serialize(value, writer);
        }

        void ISerializable.Read(ByteReader reader)
        {
            value = Implementation.Deserialize(reader);
        }

        /// <summary>
        /// Appends entities from the given <paramref name="sourceWorld"/>.
        /// </summary>
        public readonly void Append(World sourceWorld)
        {
            List<Slot> sourceSlots = sourceWorld.value->slots;
            for (uint e = 1; e < sourceSlots.Count; e++)
            {
                if (sourceWorld.Free.Contains(e))
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
            value->entityCreatedOrDestroyed.Add((function, userData));
        }

        /// <summary>
        /// Adds a function that listens to when data on an entity changes.
        /// <para>
        /// Components added or removed,
        /// arrays created, destroyed or resized,
        /// tags added or removed.
        /// </para>
        /// </summary>
        public readonly void ListenToEntityDataChanges(EntityDataChanged function, ulong userData = default)
        {
            value->entityDataChanged.Add((function, userData));
        }

        /// <summary>
        /// Adds a function that listens to when an entity's parent changes.
        /// </summary>
        public readonly void ListenToEntityParentChanges(EntityParentChanged function, ulong userData = default)
        {
            value->entityParentChanged.Add((function, userData));
        }

        /// <summary>
        /// Destroys the given <paramref name="entity"/> assuming it exists.
        /// </summary>
        public readonly void DestroyEntity(uint entity, bool destroyChildren = true)
        {
            Implementation.DestroyEntity(value, entity, destroyChildren);
        }

        /// <summary>
        /// Copies component types from the given <paramref name="entity"/> to the destination <paramref name="buffer"/>.
        /// </summary>
        public readonly byte CopyComponentTypesTo(uint entity, USpan<ComponentType> buffer)
        {
            Implementation.ThrowIfEntityIsMissing(value, entity);

            return value->slots[entity].Definition.CopyComponentTypesTo(buffer);
        }

        /// <summary>
        /// Checks if the given <paramref name="entity"/> is enabled with respect
        /// to ancestor entities.
        /// </summary>
        public readonly bool IsEnabled(uint entity)
        {
            Implementation.ThrowIfEntityIsMissing(value, entity);

            return value->slots[entity].state == Slot.State.Enabled;
        }

        /// <summary>
        /// Checks if the given entity is enabled regardless
        /// of it's parents.
        /// </summary>
        public readonly bool IsLocallyEnabled(uint entity)
        {
            Implementation.ThrowIfEntityIsMissing(value, entity);

            ref Slot.State state = ref value->slots[entity].state;
            return state == Slot.State.Enabled || state == Slot.State.DisabledButLocallyEnabled;
        }

        /// <summary>
        /// Assigns the enabled state of the given <paramref name="entity"/>
        /// and its descendants to the given <paramref name="enabled"/>.
        /// </summary>
        public readonly void SetEnabled(uint entity, bool enabled)
        {
            Implementation.ThrowIfEntityIsMissing(value, entity);

            ref Slot entitySlot = ref value->slots[entity];
            uint parent = entitySlot.parent;
            if (parent != default)
            {
                Slot.State parentState = value->slots[parent].state;
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

                if (!value->chunksMap.TryGetValue(newDefinition, out Chunk newChunk))
                {
                    newChunk = new Chunk(newDefinition, Schema);
                    value->chunksMap.Add(newDefinition, newChunk);
                    value->uniqueChunks.Add(newChunk);
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
                    ref Slot currentSlot = ref value->slots[current];
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

                        if (!value->chunksMap.TryGetValue(newDefinition, out Chunk newChunk))
                        {
                            newChunk = new Chunk(newDefinition, Schema);
                            value->chunksMap.Add(newDefinition, newChunk);
                            value->uniqueChunks.Add(newChunk);
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
            return Implementation.CreateEntity(value, default, out _, out _);
        }

        /// <summary>
        /// Creates a new entity with the given <paramref name="definition"/>.
        /// </summary>
        public readonly uint CreateEntity(Definition definition)
        {
            return Implementation.CreateEntity(value, definition, out _, out _);
        }

        /// <summary>
        /// Creates a new entity with the given <paramref name="definition"/>.
        /// </summary>
        public readonly uint CreateEntity(Definition definition, out Chunk chunk, out uint index)
        {
            return Implementation.CreateEntity(value, definition, out chunk, out index);
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
            Implementation.ThrowIfEntityIsMissing(value, entity);

            ref Chunk chunk = ref value->slots[entity].chunk;
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
            Archetype archetype = new(definition, value->schema);
            Become(entity, archetype);
        }

        /// <summary>
        /// Makes the given <paramref name="entity"/> become what the <paramref name="archetype"/>
        /// argues by adding the missing components, arrays and tags.
        /// </summary>
        public readonly void Become(uint entity, Archetype archetype)
        {
            Implementation.ThrowIfEntityIsMissing(value, entity);

            Definition currentDefinition = value->slots[entity].Definition;
            for (uint i = 0; i < BitMask.Capacity; i++)
            {
                ComponentType componentType = new(i);
                if (archetype.Contains(componentType) && !currentDefinition.Contains(componentType))
                {
                    DataType dataType = new(componentType, archetype.GetSize(componentType));
                    AddComponent(entity, dataType);
                }

                ArrayElementType arrayElementType = new(i);
                if (archetype.Contains(arrayElementType) && !currentDefinition.Contains(arrayElementType))
                {
                    DataType dataType = new(arrayElementType, archetype.GetSize(arrayElementType));
                    CreateArray(entity, dataType);
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
            return Implementation.ContainsEntity(value, entity);
        }

        /// <summary>
        /// Retrieves the parent of the given entity, <see langword="default"/> if none
        /// is assigned.
        /// </summary>
        public readonly uint GetParent(uint entity)
        {
            return Implementation.GetParent(value, entity);
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
            return Implementation.SetParent(value, entity, newParent);
        }

        /// <summary>
        /// Retrieves all children of the <paramref name="entity"/> entity.
        /// </summary>
        public readonly USpan<uint> GetChildren(uint entity)
        {
            if (Implementation.TryGetChildren(value, entity, out USpan<uint> children))
            {
                return children;
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
            return Implementation.TryGetChildren(value, entity, out children);
        }

        /// <summary>
        /// Retrieves all entities referenced by <paramref name="entity"/>.
        /// </summary>
        public readonly USpan<uint> GetReferences(uint entity)
        {
            if (Implementation.TryGetReferences(value, entity, out USpan<uint> references))
            {
                return references;
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
            return Implementation.TryGetReferences(value, entity, out references);
        }

        /// <summary>
        /// Retrieves the number of children the given <paramref name="entity"/> has.
        /// </summary>
        public readonly uint GetChildCount(uint entity)
        {
            Implementation.ThrowIfEntityIsMissing(value, entity);

            ref Slot slot = ref value->slots[entity];
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
            Implementation.ThrowIfEntityIsMissing(value, entity);
            Implementation.ThrowIfEntityIsMissing(value, referencedEntity);

            ref Slot slot = ref value->slots[entity];
            if (!slot.ContainsReferences)
            {
                slot.flags |= Slot.Flags.ContainsReferences;
                slot.references = new(4);
                slot.references.Add(default); //reserved
            }
            else if (slot.ReferencesOutdated)
            {
                slot.flags &= ~Slot.Flags.ReferencesOutdated;
                slot.references.Clear();
                slot.references.Add(default); //reserved
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
            Implementation.ThrowIfEntityIsMissing(value, entity);
            Implementation.ThrowIfReferenceIsMissing(value, entity, reference);

            value->slots[entity].references[(uint)reference] = referencedEntity;
        }

        /// <summary>
        /// Checks if the given entity contains a reference to the given <paramref name="referencedEntity"/>.
        /// </summary>
        public readonly bool ContainsReference(uint entity, uint referencedEntity)
        {
            Implementation.ThrowIfEntityIsMissing(value, entity);

            ref Slot slot = ref value->slots[entity];
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
            Implementation.ThrowIfEntityIsMissing(value, entity);

            ref Slot slot = ref value->slots[entity];
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
            Implementation.ThrowIfEntityIsMissing(value, entity);

            ref Slot slot = ref value->slots[entity];
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
            Implementation.ThrowIfEntityIsMissing(value, entity);
            Implementation.ThrowIfReferenceIsMissing(value, entity, reference);

            return ref value->slots[entity].references[(uint)reference];
        }

        /// <summary>
        /// Retrieves the <see cref="rint"/> value that points to the given <paramref name="referencedEntity"/> on <paramref name="entity"/>.
        /// </summary>
        public readonly rint GetReference(uint entity, uint referencedEntity)
        {
            Implementation.ThrowIfEntityIsMissing(value, entity);
            Implementation.ThrowIfReferencedEntityIsMissing(value, entity, referencedEntity);

            ref Slot slot = ref value->slots[entity];
            uint index = slot.references.IndexOf(referencedEntity);
            return (rint)(index + 1);
        }

        /// <summary>
        /// Attempts to retrieve the referenced entity at the given <paramref name="reference"/> on <paramref name="entity"/>.
        /// </summary>
        public readonly bool TryGetReference(uint entity, rint reference, out uint referencedEntity)
        {
            Implementation.ThrowIfEntityIsMissing(value, entity);

            ref Slot slot = ref value->slots[entity];
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
            Implementation.ThrowIfEntityIsMissing(value, entity);
            Implementation.ThrowIfReferenceIsMissing(value, entity, reference);

            value->slots[entity].references.RemoveAt((uint)reference, out uint removed);
            return removed;
        }

        /// <summary>
        /// Removes the <paramref name="referencedEntity"/> from <paramref name="entity"/>.
        /// </summary>
        /// <returns>The reference that was removed.</returns>
        public readonly rint RemoveReference(uint entity, uint referencedEntity)
        {
            Implementation.ThrowIfEntityIsMissing(value, entity);
            Implementation.ThrowIfReferencedEntityIsMissing(value, entity, referencedEntity);

            ref List<uint> references = ref value->slots[entity].references;
            uint count = references.Count;
            references.RemoveAt(references.IndexOf(referencedEntity));
            return (rint)count;
        }

        /// <summary>
        /// Writes all tag types on <paramref name="entity"/> to <paramref name="destination"/>.
        /// </summary>
        public readonly byte CopyTagTypesTo(uint entity, USpan<TagType> destination)
        {
            Implementation.ThrowIfEntityIsMissing(value, entity);

            Definition definition = value->slots[entity].Definition;
            return definition.CopyTagTypesTo(destination);
        }

        public readonly bool ContainsTag<T>(uint entity) where T : unmanaged
        {
            TagType tagType = Schema.GetTag<T>();
            return Contains(entity, tagType);
        }

        public readonly bool Contains(uint entity, TagType tagType)
        {
            Implementation.ThrowIfEntityIsMissing(value, entity);

            return value->slots[entity].Definition.TagTypes.Contains(tagType);
        }

        public readonly void AddTag<T>(uint entity) where T : unmanaged
        {
            TagType tagType = Schema.GetTag<T>();
            Implementation.AddTag(value, entity, tagType);
        }

        public readonly void AddTag(uint entity, TagType tagType)
        {
            Implementation.AddTag(value, entity, tagType);
        }

        public readonly void RemoveTag<T>(uint entity) where T : unmanaged
        {
            TagType tagType = Schema.GetTag<T>();
            Implementation.RemoveTag(value, entity, tagType);
        }

        public readonly void RemoveTag(uint entity, TagType tagType)
        {
            Implementation.RemoveTag(value, entity, tagType);
        }

        /// <summary>
        /// Retrieves the types of all arrays on this entity.
        /// </summary>
        public readonly byte CopyArrayElementTypesTo(uint entity, USpan<ArrayElementType> destination)
        {
            Implementation.ThrowIfEntityIsMissing(value, entity);

            ref Chunk chunk = ref value->slots[entity].chunk;
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
            Implementation.ThrowIfEntityIsMissing(value, entity);

            return value->slots[entity].Definition.ArrayElementTypes;
        }

        public readonly BitMask GetTagTypes(uint entity)
        {
            Implementation.ThrowIfEntityIsMissing(value, entity);

            return value->slots[entity].Definition.TagTypes;
        }

        /// <summary>
        /// Creates a new uninitialized array with the given <paramref name="length"/> and <paramref name="arrayElementType"/>.
        /// </summary>
        public readonly Allocation CreateArray(uint entity, ArrayElementType arrayElementType, uint length = 0)
        {
            ushort arrayElementSize = Schema.GetSize(arrayElementType);
            return Implementation.CreateArray(value, entity, arrayElementType, arrayElementSize, length);
        }

        /// <summary>
        /// Creates a new uninitialized array with the given <paramref name="length"/> and <paramref name="arrayElementType"/>.
        /// </summary>
        public readonly Allocation CreateArray(uint entity, DataType arrayElementType, uint length = 0)
        {
            ushort arrayElementSize = arrayElementType.size;
            return Implementation.CreateArray(value, entity, arrayElementType, arrayElementSize, length);
        }

        /// <summary>
        /// Creates a new uninitialized array on this <paramref name="entity"/>.
        /// </summary>
        public readonly USpan<T> CreateArray<T>(uint entity, uint length = 0) where T : unmanaged
        {
            ArrayElementType arrayElementType = Schema.GetArrayElement<T>();
            ushort arrayElementSize = (ushort)sizeof(T);
            Allocation array = Implementation.CreateArray(value, entity, arrayElementType, arrayElementSize, length);
            return array.AsSpan<T>(0, length);
        }

        /// <summary>
        /// Creates a new array containing the given span.
        /// </summary>
        public readonly void CreateArray<T>(uint entity, USpan<T> values) where T : unmanaged
        {
            USpan<T> array = CreateArray<T>(entity, values.Length);
            values.CopyTo(array);
        }

        /// <summary>
        /// Checks if the given <paramref name="entity"/> contains an array of type <typeparamref name="T"/>.
        /// </summary>
        public readonly bool ContainsArray<T>(uint entity) where T : unmanaged
        {
            ArrayElementType arrayElementType = Schema.GetArrayElement<T>();
            return Contains(entity, arrayElementType);
        }

        /// <summary>
        /// Checks if the given <paramref name="entity"/> contains an <paramref name="arrayType"/> array.
        /// </summary>
        public readonly bool ContainsArray(uint entity, DataType arrayType)
        {
            return Implementation.Contains(value, entity, arrayType.ArrayElementType);
        }

        /// <summary>
        /// Checks if the given <paramref name="entity"/> contains an array of the given <paramref name="arrayElementType"/>.
        /// </summary>
        public readonly bool Contains(uint entity, ArrayElementType arrayElementType)
        {
            return Implementation.Contains(value, entity, arrayElementType);
        }

        /// <summary>
        /// Retrieves the array of type <typeparamref name="T"/> from the given <paramref name="entity"/>.
        /// </summary>
        public readonly USpan<T> GetArray<T>(uint entity) where T : unmanaged
        {
            ArrayElementType arrayElementType = Schema.GetArrayElement<T>();
            Allocation array = Implementation.GetArray(value, entity, arrayElementType, out uint length);
            return array.AsSpan<T>(0, length);
        }

        /// <summary>
        /// Retrieves the array of type <typeparamref name="T"/> from the given <paramref name="entity"/>.
        /// </summary>
        public readonly USpan<T> GetArray<T>(uint entity, ArrayElementType arrayType) where T : unmanaged
        {
            Allocation array = Implementation.GetArray(value, entity, arrayType, out uint length);
            return array.AsSpan<T>(0, length);
        }

        /// <summary>
        /// Retrieves the array of the given <paramref name="arrayElementType"/> from the given <paramref name="entity"/>.
        /// </summary>
        public readonly Allocation GetArray(uint entity, ArrayElementType arrayElementType, out uint length)
        {
            return Implementation.GetArray(value, entity, arrayElementType, out length);
        }

        /// <summary>
        /// Retrieves the array of type <typeparamref name="T"/> from the given <paramref name="entity"/>.
        /// </summary>
        public readonly USpan<T> ResizeArray<T>(uint entity, uint newLength) where T : unmanaged
        {
            ArrayElementType arrayElementType = Schema.GetArrayElement<T>();
            ushort arrayElementSize = (ushort)sizeof(T);
            Allocation array = Implementation.ResizeArray(value, entity, arrayElementType, arrayElementSize, newLength);
            return array.AsSpan<T>(0, newLength);
        }

        /// <summary>
        /// Retrieves the array of type <typeparamref name="T"/> from the given <paramref name="entity"/>.
        /// </summary>
        public readonly USpan<T> ResizeArray<T>(uint entity, ArrayElementType arrayElementType, uint newLength) where T : unmanaged
        {
            ushort arrayElementSize = (ushort)sizeof(T);
            Allocation array = Implementation.ResizeArray(value, entity, arrayElementType, arrayElementSize, newLength);
            return array.AsSpan<T>(0, newLength);
        }

        /// <summary>
        /// Resizes the array of type <paramref name="arrayElementType"/> on the given <paramref name="entity"/>.
        /// </summary>
        public readonly Allocation ResizeArray(uint entity, ArrayElementType arrayElementType, uint newLength)
        {
            ushort arrayElementSize = Schema.GetSize(arrayElementType);
            return Implementation.ResizeArray(value, entity, arrayElementType, arrayElementSize, newLength);
        }

        /// <summary>
        /// Resizes the array of type <paramref name="arrayElementType"/> on the given <paramref name="entity"/>.
        /// </summary>
        public readonly Allocation ResizeArray(uint entity, DataType arrayElementType, uint newLength)
        {
            ushort arrayElementSize = arrayElementType.size;
            return Implementation.ResizeArray(value, entity, arrayElementType, arrayElementSize, newLength);
        }

        /// <summary>
        /// Resizes the array of type <typeparamref name="T"/> on the given <paramref name="entity"/>.
        /// </summary>
        public readonly USpan<T> ExpandArray<T>(uint entity, int deltaChange) where T : unmanaged
        {
            ArrayElementType arrayElementType = Schema.GetArrayElement<T>();
            ushort arrayElementSize = (ushort)sizeof(T);
            uint length = Implementation.GetArrayLength(value, entity, arrayElementType);
            uint newLength = (uint)Math.Max(0, length + deltaChange);
            Allocation array = Implementation.ResizeArray(value, entity, arrayElementType, arrayElementSize, newLength);
            return array.AsSpan<T>(0, newLength);
        }

        /// <summary>
        /// Increases the length of the array of type <typeparamref name="T"/> on the given <paramref name="entity"/>.
        /// </summary>
        public readonly USpan<T> ExpandArray<T>(uint entity, uint lengthIncrement) where T : unmanaged
        {
            ArrayElementType arrayElementType = Schema.GetArrayElement<T>();
            ushort arrayElementSize = (ushort)sizeof(T);
            uint length = Implementation.GetArrayLength(value, entity, arrayElementType);
            uint newLength = length + lengthIncrement;
            Allocation array = Implementation.ResizeArray(value, entity, arrayElementType, arrayElementSize, newLength);
            return array.AsSpan<T>(0, newLength);
        }

        /// <summary>
        /// Attempts to retrieve an array of type <typeparamref name="T"/> from the given <paramref name="entity"/>.
        /// </summary>
        public readonly bool TryGetArray<T>(uint entity, out USpan<T> array) where T : unmanaged
        {
            DataType arrayElementType = Schema.GetArrayElementDataType<T>();
            if (ContainsArray(entity, arrayElementType))
            {
                array = GetArray<T>(entity, arrayElementType);
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
            Implementation.ThrowIfEntityIsMissing(value, entity);

            ArrayElementType arrayElementType = Schema.GetArrayElement<T>();
            Implementation.ThrowIfArrayIsMissing(value, entity, arrayElementType);
            Allocation array = Implementation.GetArray(value, entity, arrayElementType, out _);
            return ref array.Read<T>(index * (uint)sizeof(T));
        }

        /// <summary>
        /// Retrieves the length of an existing array on this entity.
        /// </summary>
        public readonly uint GetArrayLength<T>(uint entity) where T : unmanaged
        {
            ArrayElementType arrayElementType = Schema.GetArrayElement<T>();
            return Implementation.GetArrayLength(value, entity, arrayElementType);
        }

        /// <summary>
        /// Retrieves the length of an existing array on this entity.
        /// </summary>
        public readonly uint GetArrayLength<T>(uint entity, ArrayElementType arrayElementType) where T : unmanaged
        {
            return Implementation.GetArrayLength(value, entity, arrayElementType);
        }

        /// <summary>
        /// Destroys the array of type <typeparamref name="T"/> on the given <paramref name="entity"/>.
        /// </summary>
        public readonly void DestroyArray<T>(uint entity) where T : unmanaged
        {
            ArrayElementType arrayElementType = Schema.GetArrayElement<T>();
            DestroyArray(entity, arrayElementType);
        }

        /// <summary>
        /// Destroys the array of the given <paramref name="arrayElementType"/> on the given <paramref name="entity"/>.
        /// </summary>
        public readonly void DestroyArray(uint entity, ArrayElementType arrayElementType)
        {
            Implementation.DestroyArray(value, entity, arrayElementType);
        }

        /// <summary>
        /// Adds a new <typeparamref name="T"/> component to the given <paramref name="entity"/>.
        /// </summary>
        public readonly ref T AddComponent<T>(uint entity, T component) where T : unmanaged
        {
            ComponentType componentType = Schema.GetComponent<T>();
            ushort componentSize = (ushort)sizeof(T);
            Allocation destination = Implementation.AddComponent(value, entity, componentType, componentSize);
            destination.Write(0, component);
            Implementation.NotifyComponentAdded(this, entity, componentType);
            return ref destination.Read<T>();
        }

        /// <summary>
        /// Adds a new default component with the given type.
        /// </summary>
        public readonly void AddComponent(uint entity, ComponentType componentType)
        {
            ushort componentSize = Schema.GetSize(componentType);
            Implementation.AddComponent(value, entity, componentType, componentSize);
            Implementation.NotifyComponentAdded(this, entity, componentType);
        }

        /// <summary>
        /// Adds a new default component with the given type.
        /// </summary>
        public readonly void AddComponent(uint entity, DataType componentType)
        {
            ushort componentSize = componentType.size;
            Implementation.AddComponent(value, entity, componentType, componentSize);
            Implementation.NotifyComponentAdded(this, entity, componentType);
        }

        /// <summary>
        /// Adds a new component of the given <paramref name="componentType"/> with <paramref name="source"/> bytes
        /// to <paramref name="entity"/>.
        /// </summary>
        public readonly void AddComponent(uint entity, ComponentType componentType, USpan<byte> source)
        {
            ushort componentSize = Schema.GetSize(componentType);
            Allocation component = Implementation.AddComponent(value, entity, componentType, componentSize);
            source.CopyTo(component, Math.Min(componentSize, source.Length));
            Implementation.NotifyComponentAdded(this, entity, componentType);
        }

        /// <summary>
        /// Adds a new default <typeparamref name="T"/> component to the given <paramref name="entity"/>.
        /// </summary>
        public readonly void AddComponent<T>(uint entity, ComponentType componentType) where T : unmanaged
        {
            ushort componentSize = (ushort)sizeof(T);
            Implementation.AddComponent(value, entity, componentType, componentSize);
            Implementation.NotifyComponentAdded(this, entity, componentType);
        }

        /// <summary>
        /// Adds a new component of the given <paramref name="componentType"/> with <paramref name="source"/> bytes
        /// to <paramref name="entity"/>.
        /// </summary>
        public readonly void AddComponent(uint entity, DataType componentType, USpan<byte> source)
        {
            ushort componentSize = componentType.size;
            Allocation component = Implementation.AddComponent(value, entity, componentType, componentSize);
            source.CopyTo(component, Math.Min(componentSize, source.Length));
            Implementation.NotifyComponentAdded(this, entity, componentType);
        }

        /// <summary>
        /// Adds a <see langword="default"/> component to <paramref name="entity"/> and returns it by reference.
        /// </summary>
        public readonly ref T AddComponent<T>(uint entity) where T : unmanaged
        {
            ComponentType componentType = Schema.GetComponent<T>();
            ushort componentSize = (ushort)sizeof(T);
            Allocation destination = Implementation.AddComponent(value, entity, componentType, componentSize);
            Implementation.NotifyComponentAdded(this, entity, componentType);
            return ref destination.Read<T>();
        }

        /// <summary>
        /// Removes the component of type <typeparamref name="T"/> from the given <paramref name="entity"/>.
        /// </summary>
        public readonly void RemoveComponent<T>(uint entity) where T : unmanaged
        {
            Implementation.RemoveComponent<T>(value, entity);
        }

        /// <summary>
        /// Removes the component of the given <paramref name="componentType"/> from the given <paramref name="entity"/>.
        /// </summary>
        public readonly void RemoveComponent(uint entity, ComponentType componentType)
        {
            Implementation.RemoveComponent(value, entity, componentType);
        }

        /// <summary>
        /// Checks if any entity in this world contains a component
        /// of type <typeparamref name="T"/>.
        /// </summary>
        public readonly bool ContainsAnyComponent<T>() where T : unmanaged
        {
            ComponentType componentType = Schema.GetComponent<T>();
            foreach (Chunk chunk in value->uniqueChunks)
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
            return Contains(entity, Schema.GetComponent<T>());
        }

        /// <summary>
        /// Checks if the given <paramref name="entity"/> contains a component of the given <paramref name="componentType"/>.
        /// </summary>
        public readonly bool ContainsComponent(uint entity, DataType componentType)
        {
            return Implementation.Contains(value, entity, componentType.ComponentType);
        }

        /// <summary>
        /// Checks if the given <paramref name="entity"/> contains a component of <paramref name="componentType"/>.
        /// </summary>
        public readonly bool Contains(uint entity, ComponentType componentType)
        {
            return Implementation.Contains(value, entity, componentType);
        }

        /// <summary>
        /// Retrieves a reference to the component of type <typeparamref name="T"/> from the given <paramref name="entity"/>.
        /// </summary>
        public readonly ref T GetComponent<T>(uint entity) where T : unmanaged
        {
            ComponentType componentType = Schema.GetComponent<T>();
            ushort componentSize = (ushort)sizeof(T);
            Allocation component = Implementation.GetComponent(value, entity, componentType, componentSize);
            return ref component.Read<T>();
        }

        /// <summary>
        /// Retrieves the component of type <typeparamref name="T"/> if it exists, otherwise the given
        /// <paramref name="defaultValue"/> is returned.
        /// </summary>
        public readonly T GetComponentOrDefault<T>(uint entity, T defaultValue = default) where T : unmanaged
        {
            ComponentType componentType = Schema.GetComponent<T>();
            if (Contains(entity, componentType))
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
            ushort componentSize = Schema.GetSize(componentType);
            Allocation component = Implementation.GetComponent(value, entity, componentType, componentSize);
            return ref component.Read<T>();
        }

        /// <summary>
        /// Retrieves a reference to the component of type <typeparamref name="T"/>.
        /// </summary>
        public readonly ref T GetComponent<T>(uint entity, DataType componentType) where T : unmanaged
        {
            ushort componentSize = componentType.size;
            Allocation component = Implementation.GetComponent(value, entity, componentType, componentSize);
            return ref component.Read<T>();
        }

        /// <summary>
        /// Retrieves the component of the given <paramref name="componentType"/> from the given <paramref name="entity"/>
        /// as a pointer.
        /// </summary>
        public readonly Allocation GetComponent(uint entity, ComponentType componentType)
        {
            ushort componentSize = Schema.GetSize(componentType);
            return Implementation.GetComponent(value, entity, componentType, componentSize);
        }

        /// <summary>
        /// Retrieves the component of the given <paramref name="componentType"/> from the given <paramref name="entity"/>
        /// as a pointer.
        /// </summary>
        public readonly Allocation GetComponent(uint entity, DataType componentType)
        {
            ushort componentSize = componentType.size;
            return Implementation.GetComponent(value, entity, componentType, componentSize);
        }

        /// <summary>
        /// Fetches the bytes of a component from the given <paramref name="entity"/>.
        /// </summary>
        public readonly USpan<byte> GetComponentBytes(uint entity, ComponentType componentType)
        {
            ushort componentSize = Schema.GetSize(componentType);
            Allocation component = Implementation.GetComponent(value, entity, componentType, componentSize);
            return component.AsSpan(0, componentSize);
        }

        /// <summary>
        /// Fetches the bytes of a component from the given <paramref name="entity"/>.
        /// </summary>
        public readonly USpan<byte> GetComponentBytes(uint entity, DataType componentType)
        {
            ushort componentSize = componentType.size;
            Allocation component = Implementation.GetComponent(value, entity, componentType, componentSize);
            return component.AsSpan(0, componentSize);
        }

        /// <summary>
        /// Retrieves the component from the given <paramref name="entity"/> as an <see cref="object"/>.
        /// </summary>
        public readonly object GetComponentObject(uint entity, ComponentType componentType)
        {
            TypeLayout layout = componentType.GetLayout(Schema);
            USpan<byte> bytes = GetComponentBytes(entity, componentType);
            return layout.CreateInstance(bytes);
        }
        /// <summary>
        /// Retrieves the component from the given <paramref name="entity"/> as an <see cref="object"/>.
        /// </summary>
        public readonly object GetComponentObject(uint entity, DataType componentType)
        {
            TypeLayout layout = componentType.ComponentType.GetLayout(Schema);
            USpan<byte> bytes = GetComponentBytes(entity, componentType);
            return layout.CreateInstance(bytes);
        }

        /// <summary>
        /// Retrieves the array from the given <paramref name="entity"/> as <see cref="object"/>s.
        /// </summary>
        public readonly object[] GetArrayObject(uint entity, ArrayElementType arrayElementType)
        {
            TypeLayout layout = arrayElementType.GetLayout(Schema);
            Allocation array = GetArray(entity, arrayElementType, out uint length);
            ushort size = layout.Size;
            object[] arrayObject = new object[length];
            for (uint i = 0; i < length; i++)
            {
                USpan<byte> bytes = array.AsSpan<byte>(i * size, size);
                arrayObject[i] = layout.CreateInstance(bytes);
            }

            return arrayObject;
        }

        /// <summary>
        /// Attempts to retrieve a reference to the component of type <typeparamref name="T"/> from the given <paramref name="entity"/>.
        /// </summary>
        /// <returns><c>true</c> if the component is found.</returns>
        public readonly ref T TryGetComponent<T>(uint entity, out bool contains) where T : unmanaged
        {
            Schema schema = Schema;
            ComponentType componentType = schema.GetComponent<T>();
            ref Chunk chunk = ref value->slots[entity].chunk;
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
            Schema schema = Schema;
            ComponentType componentType = schema.GetComponent<T>();
            ref Chunk chunk = ref value->slots[entity].chunk;
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
            GetComponent<T>(entity) = component;
        }

        /// <summary>
        /// Assigns the given <paramref name="componentData"/> to the given <paramref name="entity"/>.
        /// </summary>
        public readonly void SetComponent(uint entity, ComponentType componentType, USpan<byte> componentData)
        {
            USpan<byte> bytes = GetComponentBytes(entity, componentType);
            componentData.CopyTo(bytes);
        }

        /// <summary>
        /// Assigns the given <paramref name="componentData"/> to the given <paramref name="entity"/>.
        /// </summary>
        public readonly void SetComponent(uint entity, DataType componentType, USpan<byte> componentData)
        {
            USpan<byte> bytes = GetComponentBytes(entity, componentType);
            componentData.CopyTo(bytes);
        }

        /// <summary>
        /// Returns the chunk that contains the given <paramref name="entity"/>.
        /// </summary>
        public readonly Chunk GetChunk(uint entity)
        {
            Implementation.ThrowIfEntityIsMissing(value, entity);

            return value->slots[entity].chunk;
        }

        /// <summary>
        /// Checks if this world contains a chunk with the given <paramref name="componentTypes"/>.
        /// </summary>
        public readonly bool ContainsComponentChunk(USpan<ComponentType> componentTypes)
        {
            BitMask mask = new();
            for (uint i = 0; i < componentTypes.Length; i++)
            {
                mask.Set(componentTypes[i]);
            }

            foreach (Chunk chunk in value->uniqueChunks)
            {
                if (chunk.Definition.ComponentTypes.ContainsAll(mask))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Copies components from the source entity onto the destination.
        /// <para>Components will be added if the destination entity doesnt
        /// contain them. Existing component data will be overwritten.</para>
        /// </summary>
        public readonly void CopyComponentsTo(uint sourceEntity, World destinationWorld, uint destinationEntity)
        {
            Implementation.ThrowIfEntityIsMissing(value, sourceEntity);

            Chunk sourceChunk = value->slots[sourceEntity].chunk;
            Definition sourceComponentTypes = sourceChunk.Definition;
            uint sourceIndex = sourceChunk.Entities.IndexOf(sourceEntity);
            Schema schema = Schema;
            for (uint c = 0; c < BitMask.Capacity; c++)
            {
                if (sourceComponentTypes.ComponentTypes.Contains(c))
                {
                    ComponentType componentType = new(c);
                    if (!destinationWorld.Contains(destinationEntity, componentType))
                    {
                        destinationWorld.AddComponent(destinationEntity, componentType);
                    }

                    ushort componentSize = schema.GetSize(componentType);
                    Allocation sourceComponent = sourceChunk.GetComponent(sourceIndex, componentType, componentSize);
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
            Schema schema = Schema;
            BitMask arrayElementTypes = GetArrayElementTypes(sourceEntity);
            for (uint a = 0; a < BitMask.Capacity; a++)
            {
                if (arrayElementTypes.Contains(a))
                {
                    ArrayElementType arrayElementType = new(a);
                    Allocation sourceArray = Implementation.GetArray(value, sourceEntity, arrayElementType, out uint sourceLength);
                    Allocation destinationArray;
                    ushort arrayElementSize = schema.GetSize(arrayElementType);
                    if (!destinationWorld.Contains(destinationEntity, arrayElementType))
                    {
                        destinationArray = Implementation.CreateArray(destinationWorld.value, destinationEntity, arrayElementType, arrayElementSize, sourceLength);
                    }
                    else
                    {
                        destinationArray = Implementation.ResizeArray(destinationWorld.value, destinationEntity, arrayElementType, arrayElementSize, sourceLength);
                    }

                    sourceArray.CopyTo(destinationArray, sourceLength * arrayElementSize);
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
                    if (!destinationWorld.Contains(destinationEntity, tagType))
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

        /// <summary>
        /// Creates a new world.
        /// </summary>
        public static World Create()
        {
            return new(Implementation.Allocate(Schema.Create()));
        }

        /// <summary>
        /// Creates a new world with the given <paramref name="schema"/>.
        /// </summary>
        public static World Create(Schema schema)
        {
            return new(Implementation.Allocate(schema));
        }

        /// <summary>
        /// Deserializes a world from the given <paramref name="reader"/>.
        /// </summary>
        public static World Deserialize(ByteReader reader)
        {
            return new(Implementation.Deserialize(reader));
        }

        /// <summary>
        /// Deserializes a world from the given <paramref name="reader"/>
        /// with a custom schema processor.
        /// </summary>
        public static World Deserialize(ByteReader reader, ProcessSchema process)
        {
            return new(Implementation.Deserialize(reader, process));
        }

        /// <summary>
        /// Deserializes a world from the given <paramref name="reader"/>
        /// with a custom schema processor.
        /// </summary>
        public static World Deserialize(ByteReader reader, Func<TypeLayout, DataType.Kind, TypeLayout> process)
        {
            return new(Implementation.Deserialize(reader, process));
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

        /// <summary>
        /// Opaque pointer implementation of a <see cref="World"/>.
        /// </summary>
        public readonly unsafe struct Implementation
        {
#if DEBUG
            internal static readonly System.Collections.Generic.Dictionary<Entity, StackTrace> createStackTraces = new();
#endif

            public readonly List<Slot> slots;
            public readonly List<uint> freeEntities;
            public readonly Dictionary<Definition, Chunk> chunksMap;
            public readonly List<Chunk> uniqueChunks;
            public readonly Schema schema;
            public readonly List<(EntityCreatedOrDestroyed, ulong)> entityCreatedOrDestroyed;
            public readonly List<(EntityParentChanged, ulong)> entityParentChanged;
            public readonly List<(EntityDataChanged, ulong)> entityDataChanged;

            private Implementation(Schema schema)
            {
                slots = new(4);
                slots.Add(default); //reserved

                freeEntities = new(4);
                chunksMap = new(4);
                uniqueChunks = new(4);
                this.schema = schema;
                entityCreatedOrDestroyed = new(4);
                entityParentChanged = new(4);
                entityDataChanged = new(4);

                Chunk defaultChunk = new(schema);
                chunksMap.Add(default, defaultChunk);
                uniqueChunks.Add(defaultChunk);
            }

            /// <summary>
            /// Throws an <see cref="NullReferenceException"/> if the given <paramref name="entity"/> is missing.
            /// </summary>
            /// <exception cref="InvalidOperationException"></exception>
            /// <exception cref="NullReferenceException"></exception>
            [Conditional("DEBUG")]
            public static void ThrowIfEntityIsMissing(World world, uint entity)
            {
                ThrowIfEntityIsMissing(world.value, entity);
            }

            /// <summary>
            /// Throws an <see cref="NullReferenceException"/> if the given <paramref name="entity"/> is missing.
            /// </summary>
            /// <exception cref="InvalidOperationException"></exception>
            /// <exception cref="NullReferenceException"></exception>
            [Conditional("DEBUG")]
            public static void ThrowIfEntityIsMissing(Implementation* world, uint entity)
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

            /// <summary>
            /// Throws an <see cref="NullReferenceException"/> if the given <paramref name="reference"/> is missing.
            /// </summary>
            /// <exception cref="NullReferenceException"></exception>
            [Conditional("DEBUG")]
            public static void ThrowIfReferenceIsMissing(Implementation* world, uint entity, rint reference)
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

            /// <summary>
            /// Throws an <see cref="NullReferenceException"/> if the given <paramref name="referencedEntity"/> is not 
            /// referenced by <paramref name="entity"/>.
            /// </summary>
            /// <exception cref="NullReferenceException"></exception>
            [Conditional("DEBUG")]
            public static void ThrowIfReferencedEntityIsMissing(Implementation* world, uint entity, uint referencedEntity)
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

            /// <summary>
            /// Throws an <see cref="InvalidOperationException"/> if the given <paramref name="componentType"/> is missing from <paramref name="entity"/>.
            /// </summary>
            /// <exception cref="NullReferenceException"></exception>
            [Conditional("DEBUG")]
            public static void ThrowIfComponentMissing(Implementation* world, uint entity, ComponentType componentType)
            {
                BitMask componentTypes = world->slots[entity].Definition.ComponentTypes;
                if (!componentTypes.Contains(componentType))
                {
                    Entity thisEntity = new(new(world), entity);
                    throw new NullReferenceException($"Component `{componentType.ToString(world->schema)}` not found on `{entity}`");
                }
            }

            /// <summary>
            /// Throws an <see cref="InvalidOperationException"/> if the given <paramref name="componentType"/> is already present on <paramref name="entity"/>.
            /// </summary>
            /// <exception cref="InvalidOperationException"></exception>
            [Conditional("DEBUG")]
            public static void ThrowIfComponentAlreadyPresent(Implementation* world, uint entity, ComponentType componentType)
            {
                BitMask componentTypes = world->slots[entity].Definition.ComponentTypes;
                if (componentTypes.Contains(componentType))
                {
                    throw new InvalidOperationException($"Component `{componentType.ToString(world->schema)}` already present on `{entity}`");
                }
            }

            [Conditional("DEBUG")]
            public static void ThrowIfTagAlreadyPresent(Implementation* world, uint entity, TagType tagType)
            {
                BitMask tagTypes = world->slots[entity].Definition.TagTypes;
                if (tagTypes.Contains(tagType))
                {
                    throw new InvalidOperationException($"Tag `{tagType.ToString(world->schema)}` already present on `{entity}`");
                }
            }

            [Conditional("DEBUG")]
            public static void ThrowIfTagIsMissing(Implementation* world, uint entity, TagType tagType)
            {
                BitMask tagTypes = world->slots[entity].Definition.TagTypes;
                if (!tagTypes.Contains(tagType))
                {
                    throw new NullReferenceException($"Tag `{tagType.ToString(world->schema)}` not found on `{entity}`");
                }
            }

            /// <summary>
            /// Throws an <see cref="NullReferenceException"/> if the given <paramref name="entity"/> is missing
            /// the given <paramref name="arrayElementType"/>.
            /// </summary>
            /// <exception cref="NullReferenceException"></exception>
            [Conditional("DEBUG")]
            public static void ThrowIfArrayIsMissing(Implementation* world, uint entity, ArrayElementType arrayElementType)
            {
                BitMask arrayElementTypes = world->slots[entity].Definition.ArrayElementTypes;
                if (!arrayElementTypes.Contains(arrayElementType))
                {
                    throw new NullReferenceException($"Array of type `{arrayElementType.ToString(world->schema)}` not found on entity `{entity}`");
                }
            }

            /// <summary>
            /// Throws an <see cref="InvalidOperationException"/> if the given <paramref name="entity"/> already
            /// has the given <paramref name="arrayElementType"/>.
            /// </summary>
            /// <exception cref="InvalidOperationException"></exception>
            [Conditional("DEBUG")]
            public static void ThrowIfArrayIsAlreadyPresent(Implementation* world, uint entity, ArrayElementType arrayElementType)
            {
                BitMask arrayElementTypes = world->slots[entity].Definition.ArrayElementTypes;
                if (arrayElementTypes.Contains(arrayElementType))
                {
                    throw new InvalidOperationException($"Array of type `{arrayElementType.ToString(world->schema)}` already present on `{entity}`");
                }
            }

            /// <summary>
            /// Allocates a new <see cref="Implementation"/> instance.
            /// </summary>
            public static Implementation* Allocate(Schema schema)
            {
                ref Implementation world = ref Allocations.Allocate<Implementation>();
                world = new(schema);
                fixed (Implementation* pointer = &world)
                {
                    return pointer;
                }
            }

            /// <summary>
            /// Frees the given <paramref name="world"/> instance.
            /// </summary>
            public static void Free(ref Implementation* world)
            {
                Allocations.ThrowIfNull(world);

                Clear(world);

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
                            Array* array = (Array*)slot.arrays[a];
                            if (array is not null)
                            {
                                Array.Free(ref array);
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
            /// Serializes the world into the <paramref name="writer"/>.
            /// </summary>
            public static void Serialize(Implementation* value, ByteWriter writer)
            {
                Allocations.ThrowIfNull(value);

                World world = new(value);
                writer.WriteValue(new Signature(Version));
                writer.WriteObject(value->schema);
                writer.WriteValue(world.Count);
                writer.WriteValue(world.MaxEntityValue);
                for (uint e = 1; e < value->slots.Count; e++)
                {
                    ref Slot slot = ref value->slots[e];
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
                            USpan<byte> componentBytes = world.GetComponentBytes(e, componentType);
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
                            Allocation array = world.GetArray(e, arrayElementType, out uint length);
                            writer.WriteValue(length);
                            writer.WriteSpan(array.AsSpan<byte>(0, length * value->schema.GetSize(arrayElementType)));
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
                for (uint e = 1; e < value->slots.Count; e++)
                {
                    if (value->slots[e].state == Slot.State.Free)
                    {
                        continue;
                    }

                    if (TryGetReferences(value, e, out USpan<uint> references))
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

            /// <summary>
            /// Deserializes a new <see cref="World"/> from the data in the given <paramref name="reader"/>.
            /// </summary>
            public static Implementation* Deserialize(ByteReader reader)
            {
                return Deserialize(reader, null);
            }

            /// <summary>
            /// Deserializes a new <see cref="World"/> from the data in the given <paramref name="reader"/>.
            /// <para>
            /// The <paramref name="process"/> function is optional, and allows for reintepreting the
            /// present types into ones that are compatible with the current runtime.
            /// </para>
            /// </summary>
            public static Implementation* Deserialize(ByteReader reader, ProcessSchema process)
            {
                return Deserialize(reader, (type, dataType) =>
                {
                    return process.Invoke(type, dataType);
                });
            }

            /// <summary>
            /// Deserializes a new <see cref="World"/> from the data in the given <paramref name="reader"/>.
            /// <para>
            /// The <paramref name="process"/> function is optional, and allows for reintepreting the
            /// present types into ones that are compatible with the current runtime.
            /// </para>
            /// </summary>
            public static Implementation* Deserialize(ByteReader reader, Func<TypeLayout, DataType.Kind, TypeLayout>? process)
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
                        TypeLayout typeLayout = loadedSchema.GetArrayElementLayout(arrayElementType);
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

                Implementation* value = Allocate(schema);
                uint entityCount = reader.ReadValue<uint>();
                uint slotCount = reader.ReadValue<uint>();

                //todo: this could be a stackalloc span instead
                using Array<uint> entityMap = new(slotCount + 1);

                for (uint i = 0; i < entityCount; i++)
                {
                    uint entity = reader.ReadValue<uint>();
                    Slot.State state = reader.ReadValue<Slot.State>();
                    uint parent = reader.ReadValue<uint>();

                    uint createdEntity = CreateEntity(value, default, out _, out _);
                    entityMap[entity] = createdEntity;
                    ref Slot slot = ref value->slots[createdEntity];
                    slot.state = state;
                    slot.parent = parent;

                    //read components
                    byte componentCount = reader.ReadValue<byte>();
                    for (uint c = 0; c < componentCount; c++)
                    {
                        ComponentType componentType = reader.ReadValue<ComponentType>();
                        ushort componentSize = schema.GetSize(componentType);
                        USpan<byte> componentData = reader.ReadSpan<byte>(componentSize);
                        Allocation component = AddComponent(value, createdEntity, componentType, componentSize);
                        componentData.CopyTo(component, componentSize);
                    }

                    //read arrays
                    byte arrayCount = reader.ReadValue<byte>();
                    for (uint a = 0; a < arrayCount; a++)
                    {
                        ArrayElementType arrayElementType = reader.ReadValue<ArrayElementType>();
                        uint length = reader.ReadValue<uint>();
                        Allocation array = CreateArray(value, createdEntity, arrayElementType, schema.GetSize(arrayElementType), length);
                        USpan<byte> arrayData = reader.ReadSpan<byte>(length * schema.GetSize(arrayElementType));
                        arrayData.CopyTo(array.AsSpan<byte>(0, length * schema.GetSize(arrayElementType)));
                    }

                    //read tags
                    byte tagCount = reader.ReadValue<byte>();
                    for (uint t = 0; t < tagCount; t++)
                    {
                        TagType tagType = reader.ReadValue<TagType>();
                        AddTag(value, createdEntity, tagType);
                    }
                }

                //assign references and children
                for (uint e = 1; e < value->slots.Count; e++)
                {
                    ref Slot slot = ref value->slots[e];
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
                        ref Slot parentSlot = ref value->slots[parent];
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

            /// <summary>
            /// Clears all entities from the given <paramref name="world"/>.
            /// </summary>
            public static void Clear(Implementation* world)
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

                    if (slot.ContainsArrays)
                    {
                        slot.flags |= Slot.Flags.ArraysOutdated;
                    }

                    if (slot.ContainsChildren)
                    {
                        slot.flags |= Slot.Flags.ChildrenOutdated;
                    }

                    if (slot.ContainsReferences)
                    {
                        slot.flags |= Slot.Flags.ReferencesOutdated;
                    }

                    slot.parent = default;
                    slot.chunk = default;
                    slot.state = Slot.State.Free;
                    world->freeEntities.Add(e);
                }

                world->chunksMap.Clear();
            }

            public static uint CreateEntity(Implementation* world, Definition definition, out Chunk chunk, out uint index)
            {
                Allocations.ThrowIfNull(world);

                if (!world->chunksMap.TryGetValue(definition, out chunk))
                {
                    chunk = new(definition, world->schema);
                    world->chunksMap.Add(definition, chunk);
                    world->uniqueChunks.Add(chunk);
                }

                uint entity;
                if (world->freeEntities.Count > 0)
                {
                    world->freeEntities.RemoveAtBySwapping(0, out entity);
                }
                else
                {
                    entity = world->slots.Count;
                    world->slots.Add(default);
                }

                ref Slot slot = ref world->slots[entity];
                slot.state = Slot.State.Enabled;
                slot.chunk = chunk;

                //create arrays if necessary
                BitMask arrayElementTypes = definition.ArrayElementTypes;
                if (!arrayElementTypes.IsEmpty)
                {
                    ref Array<nint> arrays = ref slot.arrays;
                    arrays = new(BitMask.Capacity);
                    for (uint a = 0; a < BitMask.Capacity; a++)
                    {
                        if (arrayElementTypes.Contains(a))
                        {
                            ArrayElementType arrayElementType = new(a);
                            ushort arrayElementSize = world->schema.GetSize(arrayElementType);
                            arrays[(uint)arrayElementType] = (nint)Array.Allocate(0, arrayElementSize, true);
                        }
                    }

                    slot.flags |= Slot.Flags.ContainsArrays;
                }

                index = chunk.AddEntity(entity);
                TraceCreation(world, entity);
                NotifyCreation(new(world), entity);
                return entity;
            }

            /// <summary>
            /// Destroys the given <paramref name="world"/> instance.
            /// </summary>
            public static void DestroyEntity(Implementation* world, uint entity, bool destroyChildren = true)
            {
                Allocations.ThrowIfNull(world);
                ThrowIfEntityIsMissing(world, entity);

                ref Slot slot = ref world->slots[entity];
                if (slot.ContainsChildren)
                {
                    slot.flags |= Slot.Flags.ChildrenOutdated;
                    ref List<uint> children = ref slot.children;
                    USpan<uint> childrenSpan = children.AsSpan();
                    if (destroyChildren)
                    {
                        //destroy children
                        for (uint i = 0; i < childrenSpan.Length; i++)
                        {
                            uint child = childrenSpan[i];
                            DestroyEntity(world, child, true);
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

                //clear arrays
                if (slot.ContainsArrays)
                {
                    slot.flags |= Slot.Flags.ArraysOutdated;
                }

                //clear references
                if (slot.ContainsReferences)
                {
                    slot.flags |= Slot.Flags.ReferencesOutdated;
                }

                //remove from parents children list
                ref uint parent = ref slot.parent;
                if (parent != default)
                {
                    ref List<uint> parentChildren = ref world->slots[parent].children;
                    parentChildren.RemoveAtBySwapping(parentChildren.IndexOf(entity));
                    parent = default;
                }

                slot.chunk.RemoveEntity(entity);
                slot.chunk = default;
                slot.state = Slot.State.Free;
                world->freeEntities.Add(entity);
                NotifyDestruction(new(world), entity);
            }

            /// <summary>
            /// Retrieves the parent of the given <paramref name="entity"/>.
            /// </summary>
            public static uint GetParent(Implementation* world, uint entity)
            {
                Allocations.ThrowIfNull(world);
                ThrowIfEntityIsMissing(world, entity);

                return world->slots[entity].parent;
            }

            public static bool TryGetChildren(Implementation* world, uint entity, out USpan<uint> children)
            {
                Allocations.ThrowIfNull(world);
                ThrowIfEntityIsMissing(world, entity);

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

            public static bool TryGetReferences(Implementation* world, uint entity, out USpan<uint> references)
            {
                Allocations.ThrowIfNull(world);
                ThrowIfEntityIsMissing(world, entity);

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
            /// Assigns the given <paramref name="newParent"/> to the given <paramref name="entity"/>.
            /// <para>
            /// If the given <paramref name="newParent"/> isn't valid, it will be set to <see langword="default"/>.
            /// </para>
            /// </summary>
            /// <returns><see langword="true"/> if parent changed.</returns>
            public static bool SetParent(Implementation* world, uint entity, uint newParent)
            {
                Allocations.ThrowIfNull(world);
                ThrowIfEntityIsMissing(world, entity);

                if (entity == newParent)
                {
                    throw new InvalidOperationException($"Entity {entity} cannot be its own parent");
                }

                if (!ContainsEntity(world, newParent))
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

                    NotifyParentChange(new(world), entity, oldParent, newParent);
                }

                return parentChanged;
            }

            [Conditional("DEBUG")]
            private static void TraceCreation(Implementation* world, uint entity)
            {
#if DEBUG
                createStackTraces[new Entity(new World(world), entity)] = new StackTrace(2, true);
#endif
            }

            /// <summary>
            /// Checks if the given <paramref name="world"/> contains the given <paramref name="entity"/>.
            /// </summary>
            public static bool ContainsEntity(Implementation* world, uint entity)
            {
                Allocations.ThrowIfNull(world);

                if (entity >= world->slots.Count)
                {
                    return false;
                }

                return world->slots[entity].state != Slot.State.Free;
            }

            /// <summary>
            /// Creates an array of the given <paramref name="arrayElementType"/> for the given <paramref name="entity"/>.
            /// </summary>
            public static Allocation CreateArray(Implementation* world, uint entity, ArrayElementType arrayElementType, ushort arrayElementSize, uint length)
            {
                Allocations.ThrowIfNull(world);
                ThrowIfEntityIsMissing(world, entity);
                ThrowIfArrayIsAlreadyPresent(world, entity, arrayElementType);

                ref Slot slot = ref world->slots[entity];
                Chunk previousChunk = slot.chunk;
                Definition previousDefinition = previousChunk.Definition;
                if (!slot.ContainsArrays)
                {
                    slot.arrays = new(BitMask.Capacity);
                    slot.flags |= Slot.Flags.ContainsArrays;
                }
                else if (slot.ArraysOutdated)
                {
                    slot.flags &= ~Slot.Flags.ArraysOutdated;
                    for (uint i = 0; i < slot.arrays.Length; i++)
                    {
                        Array* array = (Array*)slot.arrays[i];
                        if (array is not null)
                        {
                            Array.Free(ref array);
                        }
                    }
                }

                Definition newDefinition = previousDefinition;
                newDefinition.AddArrayType(arrayElementType);

                if (!world->chunksMap.TryGetValue(newDefinition, out Chunk destinationChunk))
                {
                    destinationChunk = new(newDefinition, world->schema);
                    world->chunksMap.Add(newDefinition, destinationChunk);
                    world->uniqueChunks.Add(destinationChunk);
                }

                slot.chunk = destinationChunk;
                previousChunk.MoveEntity(entity, destinationChunk);

                Array* newArray = Array.Allocate(length, arrayElementSize, true);
                slot.arrays[(uint)arrayElementType] = (nint)newArray;
                NotifyArrayCreated(new(world), entity, arrayElementType);
                return newArray->Items;
            }

            /// <summary>
            /// Checks if the given <paramref name="entity"/> contains an array of the given <paramref name="arrayElementType"/>.
            /// </summary>
            public static bool Contains(Implementation* world, uint entity, ArrayElementType arrayElementType)
            {
                Allocations.ThrowIfNull(world);
                ThrowIfEntityIsMissing(world, entity);

                BitMask arrayElementTypes = world->slots[entity].Definition.ArrayElementTypes;
                return arrayElementTypes.Contains(arrayElementType);
            }

            /// <summary>
            /// Retrieves the array of the given <paramref name="arrayElementType"/> for the given <paramref name="entity"/>.
            /// </summary>
            public static Allocation GetArray(Implementation* world, uint entity, ArrayElementType arrayElementType, out uint length)
            {
                Allocations.ThrowIfNull(world);
                ThrowIfEntityIsMissing(world, entity);
                ThrowIfArrayIsMissing(world, entity, arrayElementType);

                ref Slot slot = ref world->slots[entity];
                Array* array = (Array*)slot.arrays[(uint)arrayElementType];
                length = array->Length;
                return array->Items;
            }

            /// <summary>
            /// Retrieves the length of the array of the given <paramref name="arrayElementType"/> for the given <paramref name="entity"/>.
            /// </summary>
            public static uint GetArrayLength(Implementation* world, uint entity, ArrayElementType arrayElementType)
            {
                Allocations.ThrowIfNull(world);
                ThrowIfEntityIsMissing(world, entity);
                ThrowIfArrayIsMissing(world, entity, arrayElementType);

                ref Slot slot = ref world->slots[entity];
                Array* array = (Array*)slot.arrays[(uint)arrayElementType];
                return array->Length;
            }

            /// <summary>
            /// Resizes the array of the given <paramref name="arrayElementType"/> for the given <paramref name="entity"/>.
            /// </summary>
            public static Allocation ResizeArray(Implementation* world, uint entity, ArrayElementType arrayElementType, ushort arrayElementSize, uint newLength)
            {
                Allocations.ThrowIfNull(world);
                ThrowIfEntityIsMissing(world, entity);
                ThrowIfArrayIsMissing(world, entity, arrayElementType);

                ref Slot slot = ref world->slots[entity];
                Array* array = (Array*)slot.arrays[(uint)arrayElementType];
                Array.Resize(array, newLength, true);
                NotifyArrayResized(new(world), entity, arrayElementType);
                return array->Items;
            }

            /// <summary>
            /// Destroys the array of the given <paramref name="arrayElementType"/> for the given <paramref name="entity"/>.
            /// </summary>
            public static void DestroyArray(Implementation* world, uint entity, ArrayElementType arrayElementType)
            {
                Allocations.ThrowIfNull(world);
                ThrowIfEntityIsMissing(world, entity);
                ThrowIfArrayIsMissing(world, entity, arrayElementType);

                ref Slot slot = ref world->slots[entity];
                Array* array = (Array*)slot.arrays[(uint)arrayElementType];
                Array.Free(ref array);
                slot.arrays[(uint)arrayElementType] = default;

                Chunk previousChunk = slot.chunk;
                Definition newDefinition = previousChunk.Definition;
                newDefinition.RemoveArrayElementType(arrayElementType);

                if (!world->chunksMap.TryGetValue(newDefinition, out Chunk destinationChunk))
                {
                    destinationChunk = new(newDefinition, world->schema);
                    world->chunksMap.Add(newDefinition, destinationChunk);
                    world->uniqueChunks.Add(destinationChunk);
                }

                slot.chunk = destinationChunk;
                previousChunk.MoveEntity(entity, destinationChunk);
                NotifyArrayDestroyed(new(world), entity, arrayElementType);
            }

            /// <summary>
            /// Adds a new component of the given <paramref name="componentType"/> to the given <paramref name="entity"/>.
            /// </summary>
            public static Allocation AddComponent(Implementation* world, uint entity, ComponentType componentType, ushort componentSize)
            {
                Allocations.ThrowIfNull(world);
                ThrowIfEntityIsMissing(world, entity);
                ThrowIfComponentAlreadyPresent(world, entity, componentType);

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
                return destinationChunk.GetComponent(index, componentType, componentSize);
            }

            /// <summary>
            /// Removes the component of type <typeparamref name="T"/> from the given <paramref name="entity"/>.
            /// </summary>
            public static void RemoveComponent<T>(Implementation* world, uint entity) where T : unmanaged
            {
                ComponentType componentType = world->schema.GetComponent<T>();
                RemoveComponent(world, entity, componentType);
            }

            /// <summary>
            /// Removes the component of the given <paramref name="componentType"/> from the given <paramref name="entity"/>.
            /// </summary>
            public static void RemoveComponent(Implementation* world, uint entity, ComponentType componentType)
            {
                Allocations.ThrowIfNull(world);
                ThrowIfEntityIsMissing(world, entity);
                ThrowIfComponentMissing(world, entity, componentType);

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
                NotifyComponentRemoved(new(world), entity, componentType);
            }

            /// <summary>
            /// Adds the <paramref name="tagType"/> to the <paramref name="entity"/>.
            /// </summary>
            public static void AddTag(Implementation* world, uint entity, TagType tagType)
            {
                Allocations.ThrowIfNull(world);
                ThrowIfEntityIsMissing(world, entity);
                ThrowIfTagAlreadyPresent(world, entity, tagType);

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
                NotifyTagAdded(new(world), entity, tagType);
            }

            /// <summary>
            /// Removes the <paramref name="tagType"/> from the <paramref name="entity"/>.
            /// </summary>
            public static void RemoveTag(Implementation* world, uint entity, TagType tagType)
            {
                Allocations.ThrowIfNull(world);
                ThrowIfEntityIsMissing(world, entity);
                ThrowIfTagIsMissing(world, entity, tagType);

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
                NotifyTagRemoved(new(world), entity, tagType);
            }

            /// <summary>
            /// Checks if the given <paramref name="entity"/> contains a component of the given <paramref name="componentType"/>.
            /// </summary>
            public static bool Contains(Implementation* world, uint entity, ComponentType componentType)
            {
                Allocations.ThrowIfNull(world);
                ThrowIfEntityIsMissing(world, entity);

                return world->slots[entity].Definition.ComponentTypes.Contains(componentType);
            }

            /// <summary>
            /// Retrieves the component of the given <paramref name="componentType"/> for the given <paramref name="entity"/>.
            /// </summary>
            public static Allocation GetComponent(Implementation* world, uint entity, ComponentType componentType, ushort componentSize)
            {
                Allocations.ThrowIfNull(world);
                ThrowIfEntityIsMissing(world, entity);
                ThrowIfComponentMissing(world, entity, componentType);

                ref Chunk chunk = ref world->slots[entity].chunk;
                uint index = chunk.Entities.IndexOf(entity);
                return chunk.GetComponent(index, componentType, componentSize);
            }

            internal static void NotifyCreation(World world, uint entity)
            {
                List<(EntityCreatedOrDestroyed, ulong)> events = world.value->entityCreatedOrDestroyed;
                for (uint i = 0; i < events.Count; i++)
                {
                    (EntityCreatedOrDestroyed callback, ulong userData) = events[i];
                    callback.Invoke(world, entity, ChangeType.Added, userData);
                }
            }

            internal static void NotifyDestruction(World world, uint entity)
            {
                List<(EntityCreatedOrDestroyed, ulong)> events = world.value->entityCreatedOrDestroyed;
                for (uint i = 0; i < events.Count; i++)
                {
                    (EntityCreatedOrDestroyed callback, ulong userData) = events[i];
                    callback.Invoke(world, entity, ChangeType.Removed, userData);
                }
            }

            internal static void NotifyParentChange(World world, uint entity, uint oldParent, uint newParent)
            {
                List<(EntityParentChanged, ulong)> events = world.value->entityParentChanged;
                for (uint i = 0; i < events.Count; i++)
                {
                    (EntityParentChanged callback, ulong userData) = events[i];
                    callback.Invoke(world, entity, oldParent, newParent, userData);
                }
            }

            internal static void NotifyComponentAdded(World world, uint entity, ComponentType componentType)
            {
                List<(EntityDataChanged, ulong)> events = world.value->entityDataChanged;
                if (events.Count > 0)
                {
                    DataType type = world.Schema.GetDataType(componentType);
                    for (uint i = 0; i < events.Count; i++)
                    {
                        (EntityDataChanged callback, ulong userData) = events[i];
                        callback.Invoke(world, entity, type, ChangeType.Added, userData);
                    }
                }
            }

            internal static void NotifyComponentRemoved(World world, uint entity, ComponentType componentType)
            {
                List<(EntityDataChanged, ulong)> events = world.value->entityDataChanged;
                if (events.Count > 0)
                {
                    DataType type = world.Schema.GetDataType(componentType);
                    for (uint i = 0; i < events.Count; i++)
                    {
                        (EntityDataChanged callback, ulong userData) = events[i];
                        callback.Invoke(world, entity, type, ChangeType.Removed, userData);
                    }
                }
            }

            internal static void NotifyArrayCreated(World world, uint entity, ArrayElementType arrayElementType)
            {
                List<(EntityDataChanged, ulong)> events = world.value->entityDataChanged;
                if (events.Count > 0)
                {
                    DataType type = world.Schema.GetDataType(arrayElementType);
                    for (uint i = 0; i < events.Count; i++)
                    {
                        (EntityDataChanged callback, ulong userData) = events[i];
                        callback.Invoke(world, entity, type, ChangeType.Added, userData);
                    }
                }
            }

            internal static void NotifyArrayDestroyed(World world, uint entity, ArrayElementType arrayElementType)
            {
                List<(EntityDataChanged, ulong)> events = world.value->entityDataChanged;
                if (events.Count > 0)
                {
                    DataType type = world.Schema.GetDataType(arrayElementType);
                    for (uint i = 0; i < events.Count; i++)
                    {
                        (EntityDataChanged callback, ulong userData) = events[i];
                        callback.Invoke(world, entity, type, ChangeType.Removed, userData);
                    }
                }
            }

            internal static void NotifyArrayResized(World world, uint entity, ArrayElementType arrayElementType)
            {
                List<(EntityDataChanged, ulong)> events = world.value->entityDataChanged;
                if (events.Count > 0)
                {
                    DataType type = world.Schema.GetDataType(arrayElementType);
                    for (uint i = 0; i < events.Count; i++)
                    {
                        (EntityDataChanged callback, ulong userData) = events[i];
                        callback.Invoke(world, entity, type, ChangeType.Modified, userData);
                    }
                }
            }

            internal static void NotifyTagAdded(World world, uint entity, TagType tagType)
            {
                List<(EntityDataChanged, ulong)> events = world.value->entityDataChanged;
                if (events.Count > 0)
                {
                    DataType type = world.Schema.GetDataType(tagType);
                    for (uint i = 0; i < events.Count; i++)
                    {
                        (EntityDataChanged callback, ulong userData) = events[i];
                        callback.Invoke(world, entity, type, ChangeType.Added, userData);
                    }
                }
            }

            internal static void NotifyTagRemoved(World world, uint entity, TagType tagType)
            {
                List<(EntityDataChanged, ulong)> events = world.value->entityDataChanged;
                if (events.Count > 0)
                {
                    DataType type = world.Schema.GetDataType(tagType);
                    for (uint i = 0; i < events.Count; i++)
                    {
                        (EntityDataChanged callback, ulong userData) = events[i];
                        callback.Invoke(world, entity, type, ChangeType.Removed, userData);
                    }
                }
            }
        }
    }
}