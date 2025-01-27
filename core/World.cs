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
        /// Amount of entities that exist in the world.
        /// </summary>
        public readonly uint Count => value->states.Count - value->freeEntities.Count;

        /// <summary>
        /// The current maximum amount of referrable entities.
        /// <para>Collections of this size + 1 are guaranteed to
        /// be able to store all entity positions.</para>
        /// </summary>
        public readonly uint MaxEntityValue => value->states.Count;

        /// <summary>
        /// Checks if the world has been disposed.
        /// </summary>
        public readonly bool IsDisposed => value is null;

        /// <summary>
        /// All previously used entities that are now free.
        /// </summary>
        public readonly List<uint> Free => value->freeEntities;

        /// <summary>
        /// States of slots.
        /// </summary>
        public readonly List<EntityState> States => value->states;

        /// <summary>
        /// All chunks in the world.
        /// </summary>
        public readonly Dictionary<Definition, Chunk> Chunks => value->chunks;

        /// <summary>
        /// The schema containing all component and array types.
        /// </summary>
        public readonly Schema Schema => value->schema;

        /// <summary>
        /// All entities that exist in the world.
        /// </summary>
        public readonly System.Collections.Generic.IEnumerable<uint> Entities
        {
            get
            {
                List<uint> free = Free;
                List<EntityState> states = States;
                for (uint i = 0; i < states.Count; i++)
                {
                    uint entity = i + 1;
                    if (!free.Contains(entity))
                    {
                        yield return entity;
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
                uint i = 0;
                for (uint s = 0; s < value->states.Count; s++)
                {
                    uint entity = s + 1;
                    if (!Free.Contains(entity))
                    {
                        if (i == index)
                        {
                            return entity;
                        }

                        i++;
                    }
                }

                throw new IndexOutOfRangeException($"Index {index} is out of range");
            }
        }

#if NET
        /// <summary>
        /// Creates a new world.
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
        /// Resets the world to <c>default</c> state.
        /// </summary>
        public readonly void Clear()
        {
            Implementation.ClearEntities(value);
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

        readonly void ISerializable.Write(BinaryWriter writer)
        {
            Implementation.Serialize(value, writer);
        }

        void ISerializable.Read(BinaryReader reader)
        {
            value = Implementation.Deserialize(reader);
        }

        /// <summary>
        /// Appends entities from the given <paramref name="sourceWorld"/>.
        /// </summary>
        public readonly void Append(World sourceWorld)
        {
            Dictionary<Definition, Chunk> chunks = Chunks;
            for (uint i = 0; i < sourceWorld.States.Count; i++)
            {
                uint sourceEntity = i + 1;
                if (sourceWorld.Free.Contains(sourceEntity))
                {
                    continue;
                }

                Chunk sourceChunk = sourceWorld.value->entityChunks[i];
                Definition sourceDefinition = sourceChunk.Definition;
                uint destinationEntity = CreateEntity(sourceDefinition, out Chunk chunk, out _);
                sourceWorld.CopyComponentsTo(sourceEntity, this, destinationEntity);
                sourceWorld.CopyArraysTo(sourceEntity, this, destinationEntity);
                sourceWorld.CopyTagsTo(sourceEntity, this, destinationEntity);
            }
        }

        /// <summary>
        /// Adds a function that listens to whenever an entity is either created, or destroyed.
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

        private readonly void Perform(Instruction instruction, List<uint> selection, List<uint> entities)
        {
            Schema schema = Schema;
            if (instruction.type == Instruction.Type.CreateEntity)
            {
                selection.Clear();
                uint createCount = (uint)instruction.A;
                for (uint i = 0; i < createCount; i++)
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
                bool isRelative = instruction.A == 0;
                if (isRelative)
                {
                    uint relativeOffset = (uint)instruction.B;
                    uint entity = entities[entities.Count - 1 - relativeOffset];
                    selection.TryAdd(entity);
                }
                else
                {
                    uint entity = (uint)instruction.B;
                    selection.TryAdd(entity);
                }
            }
            else if (instruction.type == Instruction.Type.SetParent)
            {
                bool isRelative = instruction.A == 0;
                if (isRelative)
                {
                    uint relativeOffset = (uint)instruction.B;
                    uint parent = entities[entities.Count - 1 - relativeOffset];
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
                    uint referencedEntity = entities[entities.Count - 1 - relativeOffset];
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
                rint reference = (rint)instruction.A;
                for (uint i = 0; i < selection.Count; i++)
                {
                    uint entity = selection[i];
                    RemoveReference(entity, reference);
                }
            }
            else if (instruction.type == Instruction.Type.AddComponent)
            {
                ComponentType componentType = new((byte)instruction.A);
                DataType dataType = schema.GetDataType(componentType);
                Allocation allocation = new((void*)instruction.B);
                ushort componentSize = dataType.Size;
                USpan<byte> componentData = allocation.AsSpan<byte>(0, componentSize);
                for (uint i = 0; i < selection.Count; i++)
                {
                    uint entity = selection[i];
                    AddComponent(entity, dataType, componentData);
                }
            }
            else if (instruction.type == Instruction.Type.RemoveComponent)
            {
                ComponentType componentType = new((byte)instruction.A);
                for (uint i = 0; i < selection.Count; i++)
                {
                    uint entity = selection[i];
                    RemoveComponent(entity, componentType);
                }
            }
            else if (instruction.type == Instruction.Type.SetComponent)
            {
                ComponentType componentType = new((byte)instruction.A);
                Allocation allocation = new((void*)instruction.B);
                DataType dataType = schema.GetDataType(componentType);
                ushort componentSize = dataType.Size;
                USpan<byte> componentBytes = allocation.AsSpan<byte>(0, componentSize);
                for (uint i = 0; i < selection.Count; i++)
                {
                    uint entity = selection[i];
                    SetComponent(entity, dataType, componentBytes);
                }
            }
            else if (instruction.type == Instruction.Type.AddTag)
            {
                TagType tagType = new((byte)instruction.A);
                for (uint i = 0; i < selection.Count; i++)
                {
                    uint entity = selection[i];
                    AddTag(entity, tagType);
                }
            }
            else if (instruction.type == Instruction.Type.RemoveTag)
            {
                TagType tagType = new((byte)instruction.A);
                for (uint i = 0; i < selection.Count; i++)
                {
                    uint entity = selection[i];
                    RemoveTag(entity, tagType);
                }
            }
            else if (instruction.type == Instruction.Type.CreateArray)
            {
                ArrayElementType arrayElementType = new((byte)instruction.A);
                DataType dataType = schema.GetDataType(arrayElementType);
                ushort arrayElementSize = dataType.Size;
                Allocation allocation = new((void*)instruction.B);
                uint count = (uint)instruction.C;
                for (uint i = 0; i < selection.Count; i++)
                {
                    uint entity = selection[i];
                    Allocation newArray = CreateArray(entity, dataType, count);
                    allocation.CopyTo(newArray, count * arrayElementSize);
                }
            }
            else if (instruction.type == Instruction.Type.DestroyArray)
            {
                ArrayElementType arrayElementType = new((byte)instruction.A);
                for (uint i = 0; i < selection.Count; i++)
                {
                    uint entity = selection[i];
                    DestroyArray(entity, arrayElementType);
                }
            }
            else if (instruction.type == Instruction.Type.SetArrayElement)
            {
                ArrayElementType arrayElementType = new((byte)instruction.A);
                ushort arrayElementSize = schema.GetSize(arrayElementType);
                Allocation allocation = new((void*)instruction.B);
                uint elementCount = allocation.Read<uint>();
                uint start = (uint)instruction.C;
                Allocation source = new((void*)(allocation + sizeof(uint)));
                for (uint i = 0; i < selection.Count; i++)
                {
                    uint entity = selection[i];
                    Allocation array = Implementation.GetArray(value, entity, arrayElementType, out _);
                    Allocation arrayStart = new((void*)(array + arrayElementSize * start));
                    source.CopyTo(arrayStart, elementCount * arrayElementSize);
                }
            }
            else if (instruction.type == Instruction.Type.ResizeArray)
            {
                ArrayElementType arrayElementType = new((byte)instruction.A);
                ushort arrayElementSize = schema.GetSize(arrayElementType);
                uint newLength = (uint)instruction.B;
                for (uint i = 0; i < selection.Count; i++)
                {
                    uint entity = selection[i];
                    Implementation.ResizeArray(value, entity, arrayElementType, arrayElementSize, newLength);
                }
            }
            else
            {
                throw new NotImplementedException($"Unknown instruction type `{instruction.type}`");
            }
        }

        /// <summary>
        /// Performs all given <paramref name="instructions"/>.
        /// </summary>
        public readonly void Perform(USpan<Instruction> instructions)
        {
            using List<uint> selection = new(4);
            using List<uint> entities = new(4);
            foreach (Instruction instruction in instructions)
            {
                Perform(instruction, selection, entities);
            }
        }

        /// <summary>
        /// Performs all instructions in the given <paramref name="operation"/>.
        /// </summary>
        public readonly void Perform(Operation operation)
        {
            using List<uint> selection = new(4);
            using List<uint> entities = new(4);
            uint length = operation.Count;
            for (uint i = 0; i < length; i++)
            {
                Instruction instruction = operation[i];
                Perform(instruction, selection, entities);
            }
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

            return value->entityChunks[entity - 1].Definition.CopyComponentTypesTo(buffer);
        }

        /// <summary>
        /// Checks if the given <paramref name="entity"/> is enabled with respect
        /// to ancestor entities.
        /// </summary>
        public readonly bool IsEnabled(uint entity)
        {
            Implementation.ThrowIfEntityIsMissing(value, entity);

            return value->states[entity - 1] == EntityState.Enabled;
        }

        /// <summary>
        /// Checks if the given entity is enabled regardless
        /// of it's parents.
        /// </summary>
        public readonly bool IsLocallyEnabled(uint entity)
        {
            Implementation.ThrowIfEntityIsMissing(value, entity);

            ref EntityState state = ref value->states[entity - 1];
            return state == EntityState.Enabled || state == EntityState.DisabledButLocallyEnabled;
        }

        /// <summary>
        /// Assigns the enabled state of the given <paramref name="entity"/>
        /// and its descendants to the given <paramref name="enabled"/>.
        /// </summary>
        public readonly void SetEnabled(uint entity, bool enabled)
        {
            Implementation.ThrowIfEntityIsMissing(value, entity);

            ref EntityState currentState = ref value->states[entity - 1];
            EntityState newState;
            uint parent = GetParent(entity);
            if (parent != default)
            {
                EntityState parentState = value->states[parent - 1];
                if (parentState == EntityState.Disabled || parentState == EntityState.DisabledButLocallyEnabled)
                {
                    newState = enabled ? EntityState.DisabledButLocallyEnabled : EntityState.Disabled;
                }
                else
                {
                    newState = enabled ? EntityState.Enabled : EntityState.Disabled;
                }
            }
            else
            {
                newState = enabled ? EntityState.Enabled : EntityState.Disabled;
            }

            currentState = newState;

            //move to different chunk
            Dictionary<Definition, Chunk> chunks = Chunks;
            ref Chunk chunk = ref value->entityChunks[entity - 1];
            Chunk oldChunk = chunk;
            Definition oldDefinition = oldChunk.Definition;
            bool oldEnabled = !oldDefinition.TagTypes.Contains(TagType.Disabled);
            bool newEnabled = newState == EntityState.Enabled;
            if (oldEnabled != newEnabled)
            {
                Definition newDefinition = oldDefinition;
                if (newEnabled)
                {
                    newDefinition.RemoveTagType(TagType.Disabled);
                }
                else
                {
                    newDefinition.AddTagType(TagType.Disabled);
                }

                if (!chunks.TryGetValue(newDefinition, out Chunk newChunk))
                {
                    newChunk = new Chunk(newDefinition, Schema);
                    chunks.Add(newDefinition, newChunk);
                }

                chunk = newChunk;
                oldChunk.MoveEntity(entity, newChunk);
            }

            //modify descendants
            if (value->children.Count >= entity)
            {
                List<uint> children = value->children[entity - 1];
                if (!children.IsDisposed)
                {
                    using Stack<uint> stack = new(children.Count * 2u);
                    stack.PushRange(children.AsSpan());

                    while (stack.Count > 0)
                    {
                        entity = stack.Pop();
                        ref EntityState childState = ref value->states[entity - 1];
                        if (enabled && childState == EntityState.DisabledButLocallyEnabled)
                        {
                            childState = EntityState.Enabled;
                        }
                        else if (!enabled && childState == EntityState.Enabled)
                        {
                            childState = EntityState.DisabledButLocallyEnabled;
                        }

                        //move descentant to proper chunk
                        ref Chunk childChunk = ref value->entityChunks[entity - 1];
                        oldChunk = childChunk;
                        oldDefinition = oldChunk.Definition;
                        oldEnabled = !oldDefinition.TagTypes.Contains(TagType.Disabled);
                        newEnabled = childState == EntityState.Enabled;
                        if (oldEnabled != enabled)
                        {
                            Definition newDefinition = oldDefinition;
                            if (enabled)
                            {
                                newDefinition.RemoveTagType(TagType.Disabled);
                            }
                            else
                            {
                                newDefinition.AddTagType(TagType.Disabled);
                            }

                            if (!chunks.TryGetValue(newDefinition, out Chunk newChunk))
                            {
                                newChunk = new Chunk(newDefinition, Schema);
                                chunks.Add(newDefinition, newChunk);
                            }

                            childChunk = newChunk;
                            oldChunk.MoveEntity(entity, newChunk);
                        }

                        if (value->children.Count >= entity)
                        {
                            children = value->children[entity - 1];
                            if (!children.IsDisposed)
                            {
                                stack.PushRange(children.AsSpan());
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Attempts to retrieve the first found component of type <typeparamref name="T"/>.
        /// </summary>
        public readonly ref T TryGetFirstComponent<T>(out bool contains) where T : unmanaged
        {
            Dictionary<Definition, Chunk> chunks = Chunks;
            Schema schema = Schema;
            ComponentType componentType = schema.GetComponent<T>();
            foreach (Definition key in chunks.Keys)
            {
                if (key.ComponentTypes.Contains(componentType))
                {
                    ref Chunk chunk = ref chunks[key];
                    if (chunk.Count > 0)
                    {
                        contains = true;
                        return ref chunk.GetComponent<T>(0, componentType);
                    }
                }
            }

            contains = false;
            return ref *(T*)default(nint);
        }

        /// <summary>
        /// Attempts to retrieve the first entity found with a component of type <typeparamref name="T"/>.
        /// </summary>
        public readonly bool TryGetFirstComponent<T>(out uint entity) where T : unmanaged
        {
            Dictionary<Definition, Chunk> chunks = Chunks;
            ComponentType componentType = Schema.GetComponent<T>();
            foreach (Definition key in chunks.Keys)
            {
                if (key.ComponentTypes.Contains(componentType))
                {
                    ref Chunk chunk = ref chunks[key];
                    if (chunk.Count > 0)
                    {
                        entity = chunk.Entities[0];
                        return true;
                    }
                }
            }

            entity = default;
            return false;
        }

        /// <summary>
        /// Attempts to retrieve the first found entity and component of type <typeparamref name="T"/>.
        /// </summary>
        public readonly ref T TryGetFirstComponent<T>(out uint entity, out bool contains) where T : unmanaged
        {
            Dictionary<Definition, Chunk> chunks = Chunks;
            Schema schema = Schema;
            ComponentType componentType = schema.GetComponent<T>();
            foreach (Definition key in chunks.Keys)
            {
                if (key.ComponentTypes.Contains(componentType))
                {
                    ref Chunk chunk = ref chunks[key];
                    if (chunk.Count > 0)
                    {
                        entity = chunk.Entities[0];
                        contains = true;
                        return ref chunk.GetComponent<T>(0, componentType);
                    }
                }
            }

            entity = default;
            contains = false;
            return ref *(T*)default(nint);
        }

        /// <summary>
        /// Retrieves the first found entity and component of type <typeparamref name="T"/>.
        /// <para>
        /// May throw a <see cref="NullReferenceException"/> if no entity with the component is found.
        /// </para>
        /// </summary>
        /// <exception cref="NullReferenceException"></exception>"
        public readonly ref T GetFirstComponent<T>() where T : unmanaged
        {
            Dictionary<Definition, Chunk> chunks = Chunks;
            Schema schema = Schema;
            ComponentType componentType = schema.GetComponent<T>();
            foreach (Definition key in chunks.Keys)
            {
                if (key.ComponentTypes.Contains(componentType))
                {
                    ref Chunk chunk = ref chunks[key];
                    if (chunk.Count > 0)
                    {
                        return ref chunk.GetComponent<T>(0, componentType);
                    }
                }
            }

            throw new NullReferenceException($"No entity with component of type `{typeof(T)}` was found");
        }

        /// <summary>
        /// Retrieves the first found entity and component of type <typeparamref name="T"/>.
        /// <para>
        /// May throw a <see cref="NullReferenceException"/> if no entity with the component is found.
        /// </para>
        /// </summary>
        /// <exception cref="NullReferenceException"></exception>
        public readonly ref T GetFirstComponent<T>(out uint entity) where T : unmanaged
        {
            Dictionary<Definition, Chunk> chunks = Chunks;
            Schema schema = Schema;
            ComponentType componentType = schema.GetComponent<T>();
            foreach (Definition key in chunks.Keys)
            {
                if (key.ComponentTypes.Contains(componentType))
                {
                    ref Chunk chunk = ref chunks[key];
                    if (chunk.Count > 0)
                    {
                        entity = chunk.Entities[0];
                        return ref chunk.GetComponent<T>(0, componentType);
                    }
                }
            }

            throw new NullReferenceException($"No entity with component of type `{typeof(T)}` found");
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
        /// Checks if the <paramref name="entity"/> exists and is valid in this world.
        /// </summary>
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
            return Implementation.GetParent(value, entity);
        }

        /// <summary>
        /// Assigns the given <paramref name="parent"/> entity to the given <paramref name="entity"/>.
        /// </summary>
        /// <returns><c>true</c> if the given parent entity was found and assigned successfuly.</returns>
        public readonly bool SetParent(uint entity, uint parent)
        {
            return Implementation.SetParent(value, entity, parent);
        }

        /// <summary>
        /// Retrieves all children of the <paramref name="entity"/> entity.
        /// </summary>
        public readonly USpan<uint> GetChildren(uint entity)
        {
            Implementation.ThrowIfEntityIsMissing(value, entity);

            if (value->children.Count >= entity)
            {
                List<uint> children = value->children[entity - 1];
                if (children.IsDisposed)
                {
                    return default;
                }
                else
                {
                    return children.AsSpan();
                }
            }
            else
            {
                return default;
            }
        }

        /// <summary>
        /// Retrieves the number of children the given <paramref name="entity"/> has.
        /// </summary>
        public readonly uint GetChildCount(uint entity)
        {
            Implementation.ThrowIfEntityIsMissing(value, entity);

            if (value->children.Count >= entity)
            {
                List<uint> children = value->children[entity - 1];
                if (children.IsDisposed)
                {
                    return 0;
                }
                else
                {
                    return children.Count;
                }
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

            if (value->references.Count < entity)
            {
                uint toAdd = entity - value->references.Count;
                for (uint i = 0; i < toAdd; i++)
                {
                    value->references.Add(default);
                }
            }

            ref List<uint> references = ref value->references[entity - 1];
            if (references.IsDisposed)
            {
                references = new(4);
            }

            references.Add(referencedEntity);
            return (rint)references.Count;
        }

        /// <summary>
        /// Adds a new reference to the given entity.
        /// </summary>
        public readonly rint AddReference<T>(uint entity, T referencedEntity) where T : unmanaged, IEntity
        {
            return AddReference(entity, referencedEntity.Value);
        }

        /// <summary>
        /// Updates an existing reference to point towards a different entity.
        /// </summary>
        public readonly void SetReference(uint entity, rint reference, uint referencedEntity)
        {
            Implementation.ThrowIfEntityIsMissing(value, entity);
            Implementation.ThrowIfReferenceIsMissing(value, entity, reference);

            uint index = (uint)reference;
            ref List<uint> references = ref value->references[entity - 1];
            references[index - 1] = referencedEntity;
        }

        /// <summary>
        /// Assigns a new entity to an existing reference.
        /// </summary>
        public readonly void SetReference<T>(uint entity, rint reference, T referencedEntity) where T : unmanaged, IEntity
        {
            SetReference(entity, reference, referencedEntity.Value);
        }

        /// <summary>
        /// Checks if the given entity contains a reference to the given referenced entity.
        /// </summary>
        public readonly bool ContainsReference(uint entity, uint referencedEntity)
        {
            Implementation.ThrowIfEntityIsMissing(value, entity);

            ref List<uint> references = ref value->references[entity - 1];
            if (references.IsDisposed)
            {
                return false;
            }

            return references.Contains(referencedEntity);
        }

        /// <summary>
        /// Checks if the given entity contains a reference to the given referenced entity.
        /// </summary>
        public readonly bool ContainsReference<T>(uint entity, T referencedEntity) where T : unmanaged, IEntity
        {
            return ContainsReference(entity, referencedEntity.Value);
        }

        /// <summary>
        /// Checks if the given entity contains the given local <paramref name="reference"/>.
        /// </summary>
        public readonly bool ContainsReference(uint entity, rint reference)
        {
            Implementation.ThrowIfEntityIsMissing(value, entity);

            if (value->references.Count < entity)
            {
                return false;
            }

            ref List<uint> references = ref value->references[entity - 1];
            if (references.IsDisposed)
            {
                return false;
            }

            uint index = (uint)reference;
            return index > 0 && index <= references.Count;
        }

        /// <summary>
        /// Retrieves the number of references the given <paramref name="entity"/> has.
        /// </summary>
        public readonly uint GetReferenceCount(uint entity)
        {
            Implementation.ThrowIfEntityIsMissing(value, entity);

            if (value->references.Count < entity)
            {
                return 0;
            }

            ref List<uint> references = ref value->references[entity - 1];
            if (references.IsDisposed)
            {
                return 0;
            }

            return references.Count;
        }

        /// <summary>
        /// Retrieves the referenced entity at the given <paramref name="reference"/> index on <paramref name="entity"/>.
        /// </summary>
        public readonly ref uint GetReference(uint entity, rint reference)
        {
            Implementation.ThrowIfEntityIsMissing(value, entity);
            Implementation.ThrowIfReferenceIsMissing(value, entity, reference);

            ref List<uint> references = ref value->references[entity - 1];
            uint index = (uint)reference;
            return ref references[index - 1];
        }

        /// <summary>
        /// Retrieves the local reference that points to the given <paramref name="referencedEntity"/> on <paramref name="entity"/>.
        /// </summary>
        public readonly rint GetReference(uint entity, uint referencedEntity)
        {
            Implementation.ThrowIfEntityIsMissing(value, entity);
            Implementation.ThrowIfReferencedEntityIsMissing(value, entity, referencedEntity);

            ref List<uint> references = ref value->references[entity - 1];
            if (references.IsDisposed)
            {
                return default;
            }

            uint index = references.IndexOf(referencedEntity);
            return (rint)(index + 1);
        }

        /// <summary>
        /// Attempts to retrieve the referenced entity at the given <paramref name="reference"/> on <paramref name="entity"/>.
        /// </summary>
        public readonly bool TryGetReference(uint entity, rint reference, out uint referencedEntity)
        {
            Implementation.ThrowIfEntityIsMissing(value, entity);

            ref List<uint> references = ref value->references[entity - 1];
            if (references.IsDisposed)
            {
                referencedEntity = default;
                return false;
            }

            uint index = (uint)reference;
            if (index > 0 && index <= references.Count)
            {
                referencedEntity = references[index - 1];
                return true;
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

            ref List<uint> references = ref value->references[entity - 1];
            uint index = (uint)reference;
            return references.RemoveAt(index - 1);
        }

        /// <summary>
        /// Removes the <paramref name="referencedEntity"/> from <paramref name="entity"/>.
        /// </summary>
        /// <returns>The reference that was removed.</returns>
        public readonly rint RemoveReference(uint entity, uint referencedEntity)
        {
            Implementation.ThrowIfEntityIsMissing(value, entity);
            Implementation.ThrowIfReferencedEntityIsMissing(value, entity, referencedEntity);

            ref List<uint> references = ref value->references[entity - 1];
            if (references.IsDisposed)
            {
                return default;
            }

            uint index = references.IndexOf(referencedEntity);
            references.RemoveAt(index);
            return (rint)(index + 1);
        }

        /// <summary>
        /// Writes all tag types on <paramref name="entity"/> to <paramref name="destination"/>.
        /// </summary>
        public readonly byte CopyTagTypesTo(uint entity, USpan<TagType> destination)
        {
            Implementation.ThrowIfEntityIsMissing(value, entity);

            Definition definition = value->entityChunks[entity - 1].Definition;
            return definition.CopyTagTypesTo(destination);
        }

        public readonly bool ContainsTag<T>(uint entity) where T : unmanaged
        {
            TagType tagType = Schema.GetTag<T>();
            return ContainsTag(entity, tagType);
        }

        public readonly bool ContainsTag(uint entity, TagType tagType)
        {
            Implementation.ThrowIfEntityIsMissing(value, entity);

            ref Chunk chunk = ref value->entityChunks[entity - 1];
            return chunk.Definition.TagTypes.Contains(tagType);
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

            ref Chunk chunk = ref value->entityChunks[entity - 1];
            BitMask arrayElementTypes = chunk.Definition.ArrayElementTypes;
            byte count = 0;
            for (byte a = 0; a < BitMask.Capacity; a++)
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

            return value->entityChunks[entity - 1].Definition.ArrayElementTypes;
        }

        public readonly BitMask GetTagTypes(uint entity)
        {
            Implementation.ThrowIfEntityIsMissing(value, entity);

            return value->entityChunks[entity - 1].Definition.TagTypes;
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
            ushort arrayElementSize = arrayElementType.Size;
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
            return ContainsArray(entity, arrayElementType);
        }

        /// <summary>
        /// Checks if the given <paramref name="entity"/> contains an array of the given <paramref name="arrayElementType"/>.
        /// </summary>
        public readonly bool ContainsArray(uint entity, ArrayElementType arrayElementType)
        {
            return Implementation.ContainsArray(value, entity, arrayElementType);
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
            ushort arrayElementSize = arrayElementType.Size;
            return Implementation.ResizeArray(value, entity, arrayElementType, arrayElementSize, newLength);
        }

        /// <summary>
        /// Attempts to retrieve an array of type <typeparamref name="T"/> from the given <paramref name="entity"/>.
        /// </summary>
        public readonly bool TryGetArray<T>(uint entity, out USpan<T> array) where T : unmanaged
        {
            if (ContainsArray<T>(entity))
            {
                array = GetArray<T>(entity);
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
            ushort componentSize = componentType.Size;
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
        /// Adds a new component of the given <paramref name="componentType"/> with <paramref name="source"/> bytes
        /// to <paramref name="entity"/>.
        /// </summary>
        public readonly void AddComponent(uint entity, DataType componentType, USpan<byte> source)
        {
            ushort componentSize = componentType.Size;
            Allocation component = Implementation.AddComponent(value, entity, componentType, componentSize);
            source.CopyTo(component, Math.Min(componentSize, source.Length));
            Implementation.NotifyComponentAdded(this, entity, componentType);
        }

        /// <summary>
        /// Adds a <c>default</c> component to <paramref name="entity"/> and returns it by reference.
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
            Dictionary<Definition, Chunk> chunks = Chunks;
            ComponentType componentType = Schema.GetComponent<T>();
            foreach (Definition key in chunks.Keys)
            {
                if (key.ComponentTypes.Contains(componentType))
                {
                    Chunk chunk = chunks[key];
                    if (chunk.Entities.Length > 0)
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
            return ContainsComponent(entity, Schema.GetComponent<T>());
        }

        /// <summary>
        /// Checks if the given <paramref name="entity"/> contains a component of <paramref name="componentType"/>.
        /// </summary>
        public readonly bool ContainsComponent(uint entity, ComponentType componentType)
        {
            return Implementation.ContainsComponent(value, entity, componentType);
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
        /// Returns the component of the expected type if it exists, otherwise the given default
        /// value is used.
        /// </summary>
        public readonly T GetComponent<T>(uint entity, T defaultValue) where T : unmanaged
        {
            if (ContainsComponent<T>(entity))
            {
                return GetComponent<T>(entity);
            }
            else
            {
                return defaultValue;
            }
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
            ushort componentSize = componentType.Size;
            return Implementation.GetComponent(value, entity, componentType, componentSize);
        }

        /// <summary>
        /// Fetches the bytes of a component from the given <paramref name="entity"/>.
        /// </summary>
        public readonly USpan<byte> GetComponentBytes(uint entity, ComponentType componentType)
        {
            ushort componentSize = Schema.GetSize(componentType);
            Allocation component = Implementation.GetComponent(value, entity, componentType, componentSize);
            return component.AsSpan<byte>(0, componentSize);
        }

        /// <summary>
        /// Fetches the bytes of a component from the given <paramref name="entity"/>.
        /// </summary>
        public readonly USpan<byte> GetComponentBytes(uint entity, DataType componentType)
        {
            ushort componentSize = componentType.Size;
            Allocation component = Implementation.GetComponent(value, entity, componentType, componentSize);
            return component.AsSpan<byte>(0, componentSize);
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
            ref Chunk chunk = ref value->entityChunks[entity - 1];
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
            ref Chunk chunk = ref value->entityChunks[entity - 1];
            bool contains = chunk.Definition.ComponentTypes.Contains(componentType);
            if (contains)
            {
                uint index = chunk.Entities.IndexOf(entity);
                component = chunk.GetComponent<T>(index, componentType);
            }
            else
            {
                component = default;
            }

            return contains;
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

            return value->entityChunks[entity - 1];
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

            foreach (Definition key in Chunks.Keys)
            {
                if ((key.ComponentTypes & mask) == mask)
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

            Chunk sourceChunk = value->entityChunks[sourceEntity - 1];
            Definition sourceComponentTypes = sourceChunk.Definition;
            uint sourceIndex = sourceChunk.Entities.IndexOf(sourceEntity);
            Schema schema = Schema;
            for (byte c = 0; c < BitMask.Capacity; c++)
            {
                if (sourceComponentTypes.ComponentTypes.Contains(c))
                {
                    ComponentType componentType = new(c);
                    if (!destinationWorld.ContainsComponent(destinationEntity, componentType))
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
            for (byte a = 0; a < BitMask.Capacity; a++)
            {
                if (arrayElementTypes.Contains(a))
                {
                    ArrayElementType arrayElementType = new(a);
                    Allocation sourceArray = Implementation.GetArray(value, sourceEntity, arrayElementType, out uint sourceLength);
                    Allocation destinationArray;
                    ushort arrayElementSize = schema.GetSize(arrayElementType);
                    if (!destinationWorld.ContainsArray(destinationEntity, arrayElementType))
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
            for (byte t = 0; t < BitMask.Capacity; t++)
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

        /// <summary>
        /// Creates a new world.
        /// </summary>
        public static World Create()
        {
            return new(Implementation.Allocate(Schema.Create()));
        }

        /// <summary>
        /// Deserializes a world from the given <paramref name="reader"/>.
        /// </summary>
        public static World Deserialize(BinaryReader reader, ProcessSchema process = default)
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

            public readonly List<EntityState> states;
            public readonly List<Chunk> entityChunks;
            public readonly List<uint> freeEntities;
            public readonly List<uint> parents;
            public readonly List<List<uint>> children;
            public readonly List<List<uint>> references;
            public readonly List<Array<nint>> arrays;
            public readonly Dictionary<Definition, Chunk> chunks;
            public readonly Schema schema;
            public readonly List<(EntityCreatedOrDestroyed, ulong)> entityCreatedOrDestroyed;
            public readonly List<(EntityParentChanged, ulong)> entityParentChanged;
            public readonly List<(EntityDataChanged, ulong)> entityDataChanged;

            private Implementation(List<EntityState> states, List<Chunk> entityChunks, List<uint> freeEntities, List<uint> parents, List<List<uint>> children, List<List<uint>> references, List<Array<nint>> arrays, Dictionary<Definition, Chunk> chunks, Schema schema)
            {
                this.states = states;
                this.entityChunks = entityChunks;
                this.freeEntities = freeEntities;
                this.parents = parents;
                this.children = children;
                this.references = references;
                this.arrays = arrays;
                this.chunks = chunks;
                this.schema = schema;
                entityCreatedOrDestroyed = new(4);
                entityParentChanged = new(4);
                entityDataChanged = new(4);
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
                if (entity == uint.MaxValue)
                {
                    throw new InvalidOperationException($"Entity `{entity}` is not valid");
                }

                uint index = entity - 1;
                if (index >= world->states.Count)
                {
                    throw new NullReferenceException($"Entity `{entity}` not found");
                }

                ref EntityState state = ref world->states[index];
                if (state == EntityState.Free)
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
                ref List<uint> references = ref world->references[entity - 1];
                if (references.IsDisposed)
                {
                    throw new NullReferenceException($"Reference `{reference}` not found on entity `{entity}`");
                }

                uint index = (uint)reference;
                if (index == 0 || index > references.Count)
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
                ref List<uint> references = ref world->references[entity - 1];
                if (references.IsDisposed)
                {
                    throw new NullReferenceException($"Entity `{entity}` does not reference `{referencedEntity}`");
                }

                if (!references.Contains(referencedEntity))
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
                BitMask componentTypes = world->entityChunks[entity - 1].Definition.ComponentTypes;
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
                BitMask componentTypes = world->entityChunks[entity - 1].Definition.ComponentTypes;
                if (componentTypes.Contains(componentType))
                {
                    throw new InvalidOperationException($"Component `{componentType.ToString(world->schema)}` already present on `{entity}`");
                }
            }

            [Conditional("DEBUG")]
            public static void ThrowIfTagAlreadyPresent(Implementation* world, uint entity, TagType tagType)
            {
                BitMask tagTypes = world->entityChunks[entity - 1].Definition.TagTypes;
                if (tagTypes.Contains(tagType))
                {
                    throw new InvalidOperationException($"Tag `{tagType.ToString(world->schema)}` already present on `{entity}`");
                }
            }

            [Conditional("DEBUG")]
            public static void ThrowIfTagIsMissing(Implementation* world, uint entity, TagType tagType)
            {
                BitMask tagTypes = world->entityChunks[entity - 1].Definition.TagTypes;
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
                BitMask arrayElementTypes = world->entityChunks[entity - 1].Definition.ArrayElementTypes;
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
                BitMask arrayElementTypes = world->entityChunks[entity - 1].Definition.ArrayElementTypes;
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
                List<EntityState> states = new(4);
                List<Chunk> entityChunks = new(4);
                List<uint> freeEntities = new(4);
                List<uint> parents = new(4);
                List<List<uint>> children = new(4);
                List<List<uint>> references = new(4);
                List<Array<nint>> arrays = new(4);
                Dictionary<Definition, Chunk> chunks = new(4);

                Chunk defaultChunk = new(schema);
                chunks.Add(default, defaultChunk);

                Implementation* world = Allocations.Allocate<Implementation>();
                *world = new(states, entityChunks, freeEntities, parents, children, references, arrays, chunks, schema);
                return world;
            }

            /// <summary>
            /// Frees the given <paramref name="world"/> instance.
            /// </summary>
            public static void Free(ref Implementation* world)
            {
                Allocations.ThrowIfNull(world);

                ClearEntities(world);

                for (uint r = 0; r < world->references.Count; r++)
                {
                    world->references[r].Dispose();
                }

                for (uint c = 0; c < world->children.Count; c++)
                {
                    world->children[c].Dispose();
                }

                for (uint a = 0; a < world->arrays.Count; a++)
                {
                    ref Array<nint> arrays = ref world->arrays[a];
                    if (!arrays.IsDisposed)
                    {
                        for (uint i = 0; i < arrays.Length; i++)
                        {
                            Array* array = (Array*)arrays[i];
                            if (array is not null)
                            {
                                Array.Free(ref array);
                            }
                        }

                        arrays.Dispose();
                    }
                }

                world->entityCreatedOrDestroyed.Dispose();
                world->entityParentChanged.Dispose();
                world->entityDataChanged.Dispose();
                world->schema.Dispose();
                world->states.Dispose();
                world->entityChunks.Dispose();
                world->parents.Dispose();
                world->children.Dispose();
                world->references.Dispose();
                world->arrays.Dispose();
                world->freeEntities.Dispose();
                world->chunks.Dispose();
                Allocations.Free(ref world);
            }

            /// <summary>
            /// Serializes the world into the <paramref name="writer"/>.
            /// </summary>
            public static void Serialize(Implementation* value, BinaryWriter writer)
            {
                Allocations.ThrowIfNull(value);

                writer.WriteValue(new Signature(Version));
                writer.WriteObject(value->schema);

                USpan<Chunk> chunksBuffer = stackalloc Chunk[(int)value->chunks.Count];
                uint chunkCount = 0;
                foreach (Chunk chunk in value->chunks.Values)
                {
                    if (chunk.Count > 0)
                    {
                        chunksBuffer[chunkCount++] = chunk;
                    }
                }

                writer.WriteValue(chunkCount);
                for (uint i = 0; i < chunkCount; i++)
                {
                    Chunk chunk = chunksBuffer[i];
                    writer.WriteValue(chunk.Definition);
                    writer.WriteValue(chunk.Count);
                    for (byte c = 0; c < BitMask.Capacity; c++)
                    {
                        ComponentType componentType = new(c);
                        if (chunk.Definition.ComponentTypes.Contains(componentType))
                        {
                            Collections.Implementations.List* components = chunk.GetComponents(componentType);
                            USpan<byte> componentsAsBytes = Collections.Implementations.List.AsSpan<byte>(components);
                            writer.WriteSpan(componentsAsBytes);
                        }
                    }
                }

                for (uint e = 0; e < value->states.Count; e++)
                {
                    uint entity = e + 1;
                    EntityState state = value->states[e];
                    uint parent = value->parents[e];
                    writer.WriteValue(state);
                    writer.WriteValue(parent);

                    Definition definition = value->entityChunks[e].Definition;
                    for (byte a = 0; a < BitMask.Capacity; a++)
                    {
                        ArrayElementType arrayElementType = new(a);
                        if (definition.ArrayElementTypes.Contains(arrayElementType))
                        {
                            Allocation array = GetArray(value, e + 1, arrayElementType, out uint length);
                            writer.WriteValue(length);
                            writer.WriteSpan(array.AsSpan<byte>(0, length * value->schema.GetSize(arrayElementType)));
                        }
                    }

                    if (TryGetReferences(value, entity, out USpan<uint> references))
                    {
                        writer.WriteValue(true);
                        writer.WriteValue(references.Length);
                        writer.WriteSpan(references);
                    }
                    else
                    {
                        writer.WriteValue(false);
                    }
                }
            }

            /// <summary>
            /// Deserializes a new <see cref="World"/> from the data in the given <paramref name="reader"/>.
            /// <para>
            /// The <paramref name="process"/> function is optional, and allows for reintepreting the
            /// present types into ones that are compatible with the current runtime.
            /// </para>
            /// </summary>
            public static Implementation* Deserialize(BinaryReader reader, ProcessSchema process = default)
            {
                Signature signature = reader.ReadValue<Signature>();
                if (signature.Version != Version)
                {
                    throw new InvalidOperationException($"Invalid version `{signature.Version}` expected `{Version}`");
                }

                Schema schema = new();
                using Schema loadedSchema = reader.ReadObject<Schema>();
                if (process != default)
                {
                    foreach (ComponentType componentType in loadedSchema.ComponentTypes)
                    {
                        TypeLayout typeLayout = loadedSchema.GetComponentLayout(componentType);
                        process.Invoke(ref typeLayout, DataType.Kind.Component);
                        schema.RegisterComponent(typeLayout);
                    }

                    foreach (ArrayElementType arrayElementType in loadedSchema.ArrayElementTypes)
                    {
                        TypeLayout typeLayout = loadedSchema.GetArrayElementLayout(arrayElementType);
                        process.Invoke(ref typeLayout, DataType.Kind.ArrayElement);
                        schema.RegisterArrayElement(typeLayout);
                    }

                    foreach (TagType tagType in loadedSchema.TagTypes)
                    {
                        TypeLayout typeLayout = loadedSchema.GetTagLayout(tagType);
                        process.Invoke(ref typeLayout, DataType.Kind.Tag);
                        schema.RegisterTag(typeLayout);
                    }
                }
                else
                {
                    schema.CopyFrom(loadedSchema);
                }

                Implementation* value = Allocate(schema);
                World world = new(value);
                uint chunkCount = reader.ReadValue<uint>();
                for (uint i = 0; i < chunkCount; i++)
                {
                    Definition definition = reader.ReadValue<Definition>();
                    uint entityCount = reader.ReadValue<uint>();
                    for (uint e = 0; e < entityCount; e++)
                    {
                        CreateEntity(value, definition, out _, out _);
                    }

                    Chunk chunk = value->chunks[definition];
                    for (byte c = 0; c < BitMask.Capacity; c++)
                    {
                        ComponentType componentType = new(c);
                        if (definition.ComponentTypes.Contains(componentType))
                        {
                            Collections.Implementations.List* components = chunk.GetComponents(componentType);
                            USpan<byte> componentsAsBytes = Collections.Implementations.List.AsSpan<byte>(components);
                            USpan<byte> span = reader.ReadSpan<byte>(componentsAsBytes.Length);
                            span.CopyTo(componentsAsBytes);
                        }
                    }
                }

                for (uint e = 0; e < value->states.Count; e++)
                {
                    uint entity = e + 1;
                    EntityState state = reader.ReadValue<EntityState>();
                    uint parent = reader.ReadValue<uint>();
                    value->states[e] = state;
                    value->parents[e] = parent;

                    if (parent != default)
                    {
                        if (value->children.Count < parent)
                        {
                            uint toAdd = parent - value->children.Count;
                            for (uint i = 0; i < toAdd; i++)
                            {
                                value->children.Add(default);
                            }
                        }

                        ref List<uint> children = ref value->children[parent - 1];
                        if (children.IsDisposed)
                        {
                            children = new(4);
                        }

                        children.Add(entity);
                    }

                    Definition definition = value->entityChunks[entity - 1].Definition;
                    for (byte a = 0; a < BitMask.Capacity; a++)
                    {
                        ArrayElementType arrayElementType = new(a);
                        if (definition.ArrayElementTypes.Contains(arrayElementType))
                        {
                            uint length = reader.ReadValue<uint>();
                            ushort arrayElementSize = schema.GetSize(arrayElementType);
                            Allocation array = ResizeArray(value, entity, arrayElementType, arrayElementSize, length);
                            USpan<byte> span = reader.ReadSpan<byte>(length * arrayElementSize);
                            span.CopyTo(array.AsSpan<byte>(0, length * arrayElementSize));
                        }
                    }

                    bool hasReferences = reader.ReadValue<bool>();
                    if (hasReferences)
                    {
                        uint referenceCount = reader.ReadValue<uint>();
                        USpan<uint> references = reader.ReadSpan<uint>(referenceCount);
                        for (uint r = 0; r < referenceCount; r++)
                        {
                            world.AddReference(entity, references[r]);
                        }
                    }
                }

                return value;
            }

            /// <summary>
            /// Clears all entities from the given <paramref name="world"/>.
            /// </summary>
            public static void ClearEntities(Implementation* world)
            {
                Allocations.ThrowIfNull(world);

                foreach (Definition key in world->chunks.Keys)
                {
                    ref Chunk chunk = ref world->chunks[key];
                    chunk.Dispose();
                }

                for (uint c = 0; c < world->children.Count; c++)
                {
                    ref List<uint> children = ref world->children[c];
                    if (!children.IsDisposed)
                    {
                        children.Dispose();
                    }
                }

                for (uint r = 0; r < world->references.Count; r++)
                {
                    ref List<uint> references = ref world->references[r];
                    if (!references.IsDisposed)
                    {
                        references.Dispose();
                    }
                }

                for (uint a = 0; a < world->arrays.Count; a++)
                {
                    ref Array<nint> arrays = ref world->arrays[a];
                    if (!arrays.IsDisposed)
                    {
                        for (uint i = 0; i < arrays.Length; i++)
                        {
                            Array* array = (Array*)arrays[i];
                            if (array is not null)
                            {
                                Array.Free(ref array);
                            }
                        }

                        arrays.Dispose();
                    }
                }

                world->references.Clear();
                world->children.Clear();
                world->arrays.Clear();
                world->chunks.Clear();
                world->entityChunks.Clear();
                world->states.Clear();
                world->freeEntities.Clear();
                world->parents.Clear();
            }

            public static uint CreateEntity(Implementation* world, Definition definition, out Chunk chunk, out uint index)
            {
                Allocations.ThrowIfNull(world);

                if (!world->chunks.TryGetValue(definition, out chunk))
                {
                    chunk = new(definition, world->schema);
                    world->chunks.Add(definition, chunk);
                }

                uint entity;
                if (world->freeEntities.Count > 0)
                {
                    entity = world->freeEntities.RemoveAtBySwapping(0);
                    world->states[entity - 1] = EntityState.Enabled;
                    world->entityChunks[entity - 1] = chunk;
                }
                else
                {
                    entity = world->states.Count + 1;
                    world->states.Add(EntityState.Enabled);
                    world->entityChunks.Add(chunk);
                    world->parents.Add(default);
                }

                BitMask arrayElementTypes = definition.ArrayElementTypes;
                if (arrayElementTypes != default)
                {
                    if (world->arrays.Count < entity)
                    {
                        uint toAdd = entity - world->arrays.Count;
                        for (uint i = 0; i < toAdd; i++)
                        {
                            world->arrays.Add(default);
                        }
                    }

                    ref Array<nint> arrays = ref world->arrays[entity - 1];
                    arrays = new(BitMask.Capacity);
                    for (byte a = 0; a < BitMask.Capacity; a++)
                    {
                        if (arrayElementTypes.Contains(a))
                        {
                            ArrayElementType arrayElementType = new(a);
                            ushort arrayElementSize = world->schema.GetSize(arrayElementType);
                            arrays[arrayElementType] = (nint)Array.Allocate(0, arrayElementSize);
                        }
                    }
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

                if (world->children.Count >= entity)
                {
                    ref List<uint> children = ref world->children[entity - 1];
                    if (!children.IsDisposed)
                    {
                        if (destroyChildren)
                        {
                            for (uint i = 0; i < children.Count; i++)
                            {
                                uint child = children[i];
                                DestroyEntity(world, child, true);
                            }
                        }
                        else
                        {
                            for (uint i = 0; i < children.Count; i++)
                            {
                                uint child = children[i];
                                world->parents[child - 1] = default;
                            }
                        }

                        children.Dispose();
                    }
                }

                if (world->arrays.Count >= entity)
                {
                    ref Array<nint> arrays = ref world->arrays[entity - 1];
                    if (!arrays.IsDisposed)
                    {
                        for (byte a = 0; a < BitMask.Capacity; a++)
                        {
                            Array* list = (Array*)arrays[a];
                            if (list is not null)
                            {
                                Array.Free(ref list);
                            }
                        }

                        arrays.Dispose();
                    }
                }

                if (world->references.Count >= entity)
                {
                    ref List<uint> references = ref world->references[entity - 1];
                    if (!references.IsDisposed)
                    {
                        references.Dispose();
                    }
                }

                ref Chunk chunk = ref world->entityChunks[entity - 1];
                chunk.RemoveEntity(entity);
                chunk = default;
                world->states[entity - 1] = EntityState.Free;
                world->parents[entity - 1] = default;
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

                return world->parents[entity - 1];
            }

            public static bool TryGetChildren(Implementation* world, uint entity, out USpan<uint> children)
            {
                Allocations.ThrowIfNull(world);
                ThrowIfEntityIsMissing(world, entity);

                children = default;
                bool contains = world->children.Count >= entity;
                if (contains)
                {
                    List<uint> childrenList = world->children[entity - 1];
                    if (!childrenList.IsDisposed)
                    {
                        children = childrenList.AsSpan();
                    }
                }

                return contains;
            }

            public static bool TryGetReferences(Implementation* world, uint entity, out USpan<uint> references)
            {
                Allocations.ThrowIfNull(world);
                ThrowIfEntityIsMissing(world, entity);

                references = default;
                if (world->references.Count >= entity)
                {
                    List<uint> referencesList = world->references[entity - 1];
                    if (!referencesList.IsDisposed)
                    {
                        references = referencesList.AsSpan();
                        return true;
                    }
                }

                return false;
            }

            /// <summary>
            /// Assigns the given <paramref name="newParent"/> to the given <paramref name="entity"/>.
            /// </summary>
            public static bool SetParent(Implementation* world, uint entity, uint newParent)
            {
                Allocations.ThrowIfNull(world);
                ThrowIfEntityIsMissing(world, entity);

                if (entity == newParent)
                {
                    throw new InvalidOperationException("Entity cannot be its own parent");
                }

                ref uint currentParent = ref world->parents[entity - 1];
                if (currentParent == newParent)
                {
                    return false;
                }

                //remove from previous parent children
                if (currentParent != default)
                {
                    if (world->children.Count >= currentParent)
                    {
                        ref List<uint> previousParentChildren = ref world->children[currentParent - 1];
                        if (!previousParentChildren.IsDisposed)
                        {
                            previousParentChildren.TryRemoveBySwapping(entity);
                        }
                    }
                }

                ref Chunk chunk = ref world->entityChunks[entity - 1];
                ref EntityState state = ref world->states[entity - 1];
                if (newParent == default || !ContainsEntity(world, newParent))
                {
                    if (currentParent != default)
                    {
                        uint oldParent = currentParent;
                        currentParent = default;
                        Dictionary<Definition, Chunk> chunks = world->chunks;
                        Chunk oldChunk = chunk;
                        Definition oldDefinition = oldChunk.Definition;

                        if (state == EntityState.DisabledButLocallyEnabled)
                        {
                            state = EntityState.Enabled;
                        }

                        //move to different chunk if disabled state changed
                        bool enabled = state == EntityState.Enabled;
                        if (oldDefinition.TagTypes.Contains(TagType.Disabled) == enabled)
                        {
                            Definition newDefinition = oldDefinition;
                            if (enabled)
                            {
                                newDefinition.RemoveTagType(TagType.Disabled);
                            }
                            else
                            {
                                newDefinition.AddTagType(TagType.Disabled);
                            }

                            if (!chunks.TryGetValue(newDefinition, out Chunk destinationChunk))
                            {
                                destinationChunk = new(newDefinition, world->schema);
                                chunks.Add(newDefinition, destinationChunk);
                            }

                            chunk = destinationChunk;
                            oldChunk.MoveEntity(entity, destinationChunk);
                        }

                        NotifyParentChange(new(world), entity, oldParent, default);
                    }

                    return false;
                }
                else
                {
                    if (currentParent != newParent)
                    {
                        uint oldParent = currentParent;
                        currentParent = newParent;
                        Dictionary<Definition, Chunk> chunks = world->chunks;
                        Chunk oldChunk = chunk;
                        Definition oldDefinition = oldChunk.Definition;

                        //add to children list
                        if (world->children.Count < newParent)
                        {
                            uint toAdd = newParent - world->children.Count;
                            for (uint i = 0; i < toAdd; i++)
                            {
                                world->children.Add(default);
                            }
                        }

                        ref List<uint> newParentChildren = ref world->children[newParent - 1];
                        if (newParentChildren.IsDisposed)
                        {
                            newParentChildren = new(4);
                        }

                        newParentChildren.Add(entity);

                        ref EntityState newParentSlot = ref world->states[newParent - 1];
                        if (newParentSlot == EntityState.Disabled || newParentSlot == EntityState.DisabledButLocallyEnabled)
                        {
                            if (state == EntityState.Enabled)
                            {
                                state = EntityState.DisabledButLocallyEnabled;
                            }
                        }

                        //move to different chunk if disabled state changed
                        bool enabled = state == EntityState.Enabled;
                        if (oldDefinition.TagTypes.Contains(TagType.Disabled) == enabled)
                        {
                            Definition newDefinition = oldDefinition;
                            if (enabled)
                            {
                                newDefinition.RemoveTagType(TagType.Disabled);
                            }
                            else
                            {
                                newDefinition.AddTagType(TagType.Disabled);
                            }

                            if (!chunks.TryGetValue(newDefinition, out Chunk destinationChunk))
                            {
                                destinationChunk = new(newDefinition, world->schema);
                                chunks.Add(newDefinition, destinationChunk);
                            }

                            chunk = destinationChunk;
                            oldChunk.MoveEntity(entity, destinationChunk);
                        }

                        NotifyParentChange(new(world), entity, oldParent, newParent);
                    }

                    return true;
                }
            }

            [Conditional("DEBUG")]
            private static void TraceCreation(Implementation* world, uint entity)
            {
                StackTrace stackTrace = new(2, true);
                if (stackTrace.FrameCount > 0)
                {
                    string? firstFrame = stackTrace.GetFrame(0)?.GetFileName();
                    if (firstFrame != null && firstFrame.EndsWith("World.cs"))
                    {
                        stackTrace = new(3, true);
                    }

                    firstFrame = stackTrace.GetFrame(0)?.GetFileName();
                    if (firstFrame != null && firstFrame.EndsWith("Entity.cs"))
                    {
                        stackTrace = new(4, true);
                    }
                }

#if DEBUG
                createStackTraces[new Entity(new World(world), entity)] = stackTrace;
#endif
            }

            /// <summary>
            /// Checks if the given <paramref name="world"/> contains the given <paramref name="entity"/>.
            /// </summary>
            public static bool ContainsEntity(Implementation* world, uint entity)
            {
                Allocations.ThrowIfNull(world);

                uint position = entity - 1;
                if (position >= world->states.Count)
                {
                    return false;
                }

                ref EntityState state = ref world->states[position];
                return state != EntityState.Free;
            }

            /// <summary>
            /// Creates an array of the given <paramref name="arrayElementType"/> for the given <paramref name="entity"/>.
            /// </summary>
            public static Allocation CreateArray(Implementation* world, uint entity, ArrayElementType arrayElementType, ushort arrayElementSize, uint length)
            {
                Allocations.ThrowIfNull(world);
                ThrowIfEntityIsMissing(world, entity);
                ThrowIfArrayIsAlreadyPresent(world, entity, arrayElementType);

                ref Chunk chunk = ref world->entityChunks[entity - 1];
                Chunk oldChunk = chunk;
                Definition oldDefinition = oldChunk.Definition;
                if (oldDefinition.ArrayElementTypes == default)
                {
                    if (world->arrays.Count < entity)
                    {
                        uint toAdd = entity - world->arrays.Count;
                        for (uint i = 0; i < toAdd; i++)
                        {
                            world->arrays.Add(default);
                        }
                    }

                    world->arrays[entity - 1] = new(BitMask.Capacity);
                }

                Definition newDefinition = oldDefinition;
                newDefinition.AddArrayElementType(arrayElementType);

                Dictionary<Definition, Chunk> chunks = world->chunks;
                if (!chunks.TryGetValue(newDefinition, out Chunk destinationChunk))
                {
                    destinationChunk = new(newDefinition, world->schema);
                    chunks.Add(newDefinition, destinationChunk);
                }

                chunk = destinationChunk;
                oldChunk.MoveEntity(entity, destinationChunk);

                Array* newArray = Array.Allocate(length, arrayElementSize);
                ref Array<nint> arrays = ref world->arrays[entity - 1];
                arrays[arrayElementType] = (nint)newArray;
                NotifyArrayCreated(new(world), entity, arrayElementType);
                return new((void*)Array.GetStartAddress(newArray));
            }

            /// <summary>
            /// Checks if the given <paramref name="entity"/> contains an array of the given <paramref name="arrayElementType"/>.
            /// </summary>
            public static bool ContainsArray(Implementation* world, uint entity, ArrayElementType arrayElementType)
            {
                Allocations.ThrowIfNull(world);
                ThrowIfEntityIsMissing(world, entity);

                uint index = entity - 1;
                BitMask arrayElementTypes = world->entityChunks[index].Definition.ArrayElementTypes;
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

                ref Array<nint> arrays = ref world->arrays[entity - 1];
                Array* array = (Array*)arrays[arrayElementType];
                length = Array.GetLength(array);
                return new((void*)Array.GetStartAddress(array));
            }

            /// <summary>
            /// Retrieves the length of the array of the given <paramref name="arrayElementType"/> for the given <paramref name="entity"/>.
            /// </summary>
            public static uint GetArrayLength(Implementation* world, uint entity, ArrayElementType arrayElementType)
            {
                Allocations.ThrowIfNull(world);
                ThrowIfEntityIsMissing(world, entity);
                ThrowIfArrayIsMissing(world, entity, arrayElementType);

                ref Array<nint> arrays = ref world->arrays[entity - 1];
                Array* array = (Array*)arrays[arrayElementType];
                return Array.GetLength(array);
            }

            /// <summary>
            /// Resizes the array of the given <paramref name="arrayElementType"/> for the given <paramref name="entity"/>.
            /// </summary>
            public static Allocation ResizeArray(Implementation* world, uint entity, ArrayElementType arrayElementType, ushort arrayElementSize, uint newLength)
            {
                Allocations.ThrowIfNull(world);
                ThrowIfEntityIsMissing(world, entity);
                ThrowIfArrayIsMissing(world, entity, arrayElementType);

                ref Array<nint> arrays = ref world->arrays[entity - 1];
                Array* array = (Array*)arrays[arrayElementType];
                Array.Resize(array, newLength, true);
                NotifyArrayResized(new(world), entity, arrayElementType);
                return new((void*)Array.GetStartAddress(array));
            }

            /// <summary>
            /// Destroys the array of the given <paramref name="arrayElementType"/> for the given <paramref name="entity"/>.
            /// </summary>
            public static void DestroyArray(Implementation* world, uint entity, ArrayElementType arrayElementType)
            {
                Allocations.ThrowIfNull(world);
                ThrowIfEntityIsMissing(world, entity);
                ThrowIfArrayIsMissing(world, entity, arrayElementType);

                ref Array<nint> arrays = ref world->arrays[entity - 1];
                Array* array = (Array*)arrays[arrayElementType];
                Array.Free(ref array);
                arrays[arrayElementType] = default;

                ref Chunk chunk = ref world->entityChunks[entity - 1];
                Chunk oldChunk = chunk;
                Definition oldDefinition = oldChunk.Definition;
                Definition newDefinition = oldDefinition;
                newDefinition.RemoveArrayElementType(arrayElementType);

                if (newDefinition.ArrayElementTypes == default)
                {
                    arrays.Dispose();
                }

                Dictionary<Definition, Chunk> chunks = world->chunks;
                if (!chunks.TryGetValue(newDefinition, out Chunk destinationChunk))
                {
                    destinationChunk = new(newDefinition, world->schema);
                    chunks.Add(newDefinition, destinationChunk);
                }

                chunk = destinationChunk;
                oldChunk.MoveEntity(entity, destinationChunk);
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

                Dictionary<Definition, Chunk> chunks = world->chunks;
                ref Chunk chunk = ref world->entityChunks[entity - 1];
                Chunk previousChunk = chunk;
                Definition newDefinition = previousChunk.Definition;
                newDefinition.AddComponentType(componentType);

                if (!chunks.TryGetValue(newDefinition, out Chunk destinationChunk))
                {
                    destinationChunk = new(newDefinition, world->schema);
                    chunks.Add(newDefinition, destinationChunk);
                }

                chunk = destinationChunk;
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

                Dictionary<Definition, Chunk> chunks = world->chunks;
                ref Chunk chunk = ref world->entityChunks[entity - 1];
                Chunk previousChunk = chunk;
                Definition newComponentTypes = previousChunk.Definition;
                newComponentTypes.RemoveComponentType(componentType);

                if (!chunks.TryGetValue(newComponentTypes, out Chunk destinationChunk))
                {
                    Schema schema = world->schema;
                    destinationChunk = new(newComponentTypes, schema);
                    chunks.Add(newComponentTypes, destinationChunk);
                }

                chunk = destinationChunk;
                previousChunk.MoveEntity(entity, destinationChunk);
                NotifyComponentRemoved(new(world), entity, componentType);
            }

            public static void AddTag(Implementation* world, uint entity, TagType tagType)
            {
                Allocations.ThrowIfNull(world);
                ThrowIfEntityIsMissing(world, entity);
                ThrowIfTagAlreadyPresent(world, entity, tagType);

                Dictionary<Definition, Chunk> chunks = world->chunks;
                ref Chunk chunk = ref world->entityChunks[entity - 1];
                Chunk previousChunk = chunk;
                Definition newDefinition = previousChunk.Definition;
                newDefinition.AddTagType(tagType);

                if (!chunks.TryGetValue(newDefinition, out Chunk destinationChunk))
                {
                    Schema schema = world->schema;
                    destinationChunk = new(newDefinition, schema);
                    chunks.Add(newDefinition, destinationChunk);
                }

                chunk = destinationChunk;
                uint index = previousChunk.MoveEntity(entity, destinationChunk);
                NotifyTagAdded(new(world), entity, tagType);
            }

            public static void RemoveTag(Implementation* world, uint entity, TagType tagType)
            {
                Allocations.ThrowIfNull(world);
                ThrowIfEntityIsMissing(world, entity);
                ThrowIfTagIsMissing(world, entity, tagType);

                Dictionary<Definition, Chunk> chunks = world->chunks;
                ref Chunk chunk = ref world->entityChunks[entity - 1];
                Chunk previousChunk = chunk;
                Definition newComponentTypes = previousChunk.Definition;
                newComponentTypes.RemoveTagType(tagType);

                if (!chunks.TryGetValue(newComponentTypes, out Chunk destinationChunk))
                {
                    Schema schema = world->schema;
                    destinationChunk = new(newComponentTypes, schema);
                    chunks.Add(newComponentTypes, destinationChunk);
                }

                chunk = destinationChunk;
                previousChunk.MoveEntity(entity, destinationChunk);
                NotifyTagRemoved(new(world), entity, tagType);
            }

            /// <summary>
            /// Checks if the given <paramref name="entity"/> contains a component of the given <paramref name="componentType"/>.
            /// </summary>
            public static bool ContainsComponent(Implementation* world, uint entity, ComponentType componentType)
            {
                Allocations.ThrowIfNull(world);
                ThrowIfEntityIsMissing(world, entity);

                uint index = entity - 1;
                return world->entityChunks[index].Definition.ComponentTypes.Contains(componentType);
            }

            public static Allocation GetComponent(Implementation* world, uint entity, ComponentType componentType, ushort componentSize)
            {
                Allocations.ThrowIfNull(world);
                ThrowIfEntityIsMissing(world, entity);
                ThrowIfComponentMissing(world, entity, componentType);

                ref Chunk chunk = ref world->entityChunks[entity - 1];
                uint index = chunk.Entities.IndexOf(entity);
                return chunk.GetComponent(index, componentType, componentSize);
            }

            internal static void NotifyCreation(World world, uint entity)
            {
                List<(EntityCreatedOrDestroyed, ulong)> events = world.value->entityCreatedOrDestroyed;
                foreach ((EntityCreatedOrDestroyed callback, ulong userData) in events)
                {
                    callback.Invoke(world, entity, ChangeType.Added, userData);
                }
            }

            internal static void NotifyDestruction(World world, uint entity)
            {
                List<(EntityCreatedOrDestroyed, ulong)> events = world.value->entityCreatedOrDestroyed;
                foreach ((EntityCreatedOrDestroyed callback, ulong userData) in events)
                {
                    callback.Invoke(world, entity, ChangeType.Removed, userData);
                }
            }

            internal static void NotifyParentChange(World world, uint entity, uint oldParent, uint newParent)
            {
                List<(EntityParentChanged, ulong)> events = world.value->entityParentChanged;
                foreach ((EntityParentChanged callback, ulong userData) in events)
                {
                    callback.Invoke(world, entity, oldParent, newParent, userData);
                }
            }

            internal static void NotifyComponentAdded(World world, uint entity, ComponentType componentType)
            {
                List<(EntityDataChanged, ulong)> events = world.value->entityDataChanged;
                DataType type = world.Schema.GetDataType(componentType);
                foreach ((EntityDataChanged callback, ulong userData) in events)
                {
                    callback.Invoke(world, entity, type, ChangeType.Added, userData);
                }
            }

            internal static void NotifyComponentRemoved(World world, uint entity, ComponentType componentType)
            {
                List<(EntityDataChanged, ulong)> events = world.value->entityDataChanged;
                DataType type = world.Schema.GetDataType(componentType);
                foreach ((EntityDataChanged callback, ulong userData) in events)
                {
                    callback.Invoke(world, entity, type, ChangeType.Removed, userData);
                }
            }

            internal static void NotifyArrayCreated(World world, uint entity, ArrayElementType arrayElementType)
            {
                List<(EntityDataChanged, ulong)> events = world.value->entityDataChanged;
                DataType type = world.Schema.GetDataType(arrayElementType);
                foreach ((EntityDataChanged callback, ulong userData) in events)
                {
                    callback.Invoke(world, entity, type, ChangeType.Added, userData);
                }
            }

            internal static void NotifyArrayDestroyed(World world, uint entity, ArrayElementType arrayElementType)
            {
                List<(EntityDataChanged, ulong)> events = world.value->entityDataChanged;
                DataType type = world.Schema.GetDataType(arrayElementType);
                foreach ((EntityDataChanged callback, ulong userData) in events)
                {
                    callback.Invoke(world, entity, type, ChangeType.Removed, userData);
                }
            }

            internal static void NotifyArrayResized(World world, uint entity, ArrayElementType arrayElementType)
            {
                List<(EntityDataChanged, ulong)> events = world.value->entityDataChanged;
                DataType type = world.Schema.GetDataType(arrayElementType);
                foreach ((EntityDataChanged callback, ulong userData) in events)
                {
                    callback.Invoke(world, entity, type, ChangeType.Modified, userData);
                }
            }

            internal static void NotifyTagAdded(World world, uint entity, TagType tagType)
            {
                List<(EntityDataChanged, ulong)> events = world.value->entityDataChanged;
                DataType type = world.Schema.GetDataType(tagType);
                foreach ((EntityDataChanged callback, ulong userData) in events)
                {
                    callback.Invoke(world, entity, type, ChangeType.Added, userData);
                }
            }

            internal static void NotifyTagRemoved(World world, uint entity, TagType tagType)
            {
                List<(EntityDataChanged, ulong)> events = world.value->entityDataChanged;
                DataType type = world.Schema.GetDataType(tagType);
                foreach ((EntityDataChanged callback, ulong userData) in events)
                {
                    callback.Invoke(world, entity, type, ChangeType.Removed, userData);
                }
            }
        }
    }
}