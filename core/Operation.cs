using Collections;
using Collections.Generic;
using System;
using Types;
using Unmanaged;

namespace Worlds
{
    /// <summary>
    /// Represents a collection of instructions for a world to perform.
    /// </summary>
    public unsafe struct Operation : IDisposable, ISerializable
    {
        private Implementation* operation;

        /// <summary>
        /// Native address of the operation.
        /// </summary>
        public readonly nint Address => (nint)operation;

        /// <summary>
        /// Checks if this operation has been disposed.
        /// </summary>
        public readonly bool IsDisposed => operation is null;

        /// <summary>
        /// Counts how many instructions there are.
        /// </summary>
        public readonly int Count
        {
            get
            {
                MemoryAddress.ThrowIfDefault(operation);

                return operation->count;
            }
        }

#if NET
        /// <summary>
        /// Creates a new operation to record instructions.
        /// </summary>
        public Operation()
        {
            operation = Implementation.Allocate(4);
        }
#endif

        /// <summary>
        /// Initializes an existing operation from the given <paramref name="pointer"/>.
        /// </summary>
        public Operation(void* pointer)
        {
            operation = (Implementation*)pointer;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Implementation.Free(ref operation);
        }

        private readonly void WriteInstructionType(InstructionType type)
        {
            MemoryAddress.ThrowIfDefault(operation);

            int newLength = operation->bytesLength + 1;
            if (operation->bytesCapacity < newLength)
            {
                operation->bytesCapacity *= 2;
                MemoryAddress.Resize(ref operation->buffer, operation->bytesCapacity);
            }

            operation->buffer.Write(operation->bytesLength, (byte)type);
            operation->bytesLength = newLength;
            operation->count++;
        }

        private readonly void WriteTypeLayout(Types.Type type)
        {
            MemoryAddress.ThrowIfDefault(operation);

            int newLength = operation->bytesLength + sizeof(long);
            if (operation->bytesCapacity < newLength)
            {
                operation->bytesCapacity = newLength.GetNextPowerOf2();
                MemoryAddress.Resize(ref operation->buffer, operation->bytesCapacity);
            }

            operation->buffer.Write(operation->bytesLength, type.Hash);
            operation->bytesLength = newLength;
        }

        private readonly void WriteValue<T>(T value) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(operation);

            int newLength = operation->bytesLength + sizeof(T);
            if (operation->bytesCapacity < newLength)
            {
                operation->bytesCapacity = newLength.GetNextPowerOf2();
                MemoryAddress.Resize(ref operation->buffer, operation->bytesCapacity);
            }

            operation->buffer.Write(operation->bytesLength, value);
            operation->bytesLength = newLength;
        }

        private readonly void WriteSpan<T>(ReadOnlySpan<T> span) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(operation);

            int add = sizeof(T) * span.Length;
            int newLength = operation->bytesLength + add;
            if (operation->bytesCapacity < newLength)
            {
                operation->bytesCapacity = newLength.GetNextPowerOf2();
                MemoryAddress.Resize(ref operation->buffer, operation->bytesCapacity);
            }

            operation->buffer.Write(operation->bytesLength, span);
            operation->bytesLength = newLength;
        }

        private readonly void WriteSpan<T>(Span<T> span) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(operation);

            int add = sizeof(T) * span.Length;
            int newLength = operation->bytesLength + add;
            if (operation->bytesCapacity < newLength)
            {
                operation->bytesCapacity = newLength.GetNextPowerOf2();
                MemoryAddress.Resize(ref operation->buffer, operation->bytesCapacity);
            }

            operation->buffer.Write(operation->bytesLength, span);
            operation->bytesLength = newLength;
        }

        private readonly InstructionType ReadInstructionType(ref int bytePosition)
        {
            MemoryAddress.ThrowIfDefault(operation);

            InstructionType type = operation->buffer.Read<InstructionType>(bytePosition);
            bytePosition++;
            return type;
        }

        private readonly Types.Type ReadTypeLayout(ref int bytePosition)
        {
            MemoryAddress.ThrowIfDefault(operation);

            long hash = operation->buffer.Read<long>(bytePosition);
            bytePosition += sizeof(long);
            return MetadataRegistry.GetType(hash);
        }

        private readonly T Read<T>(ref int bytePosition) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(operation);

            T value = operation->buffer.Read<T>(bytePosition);
            bytePosition += sizeof(T);
            return value;
        }

        private readonly ReadOnlySpan<byte> ReadBytes(int byteLength, ref int bytePosition)
        {
            MemoryAddress.ThrowIfDefault(operation);

            ReadOnlySpan<byte> bytes = operation->buffer.AsSpan(bytePosition, byteLength);
            bytePosition += byteLength;
            return bytes;
        }

        private readonly ReadOnlySpan<T> ReadSpan<T>(int length, ref int bytePosition) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(operation);

            int add = sizeof(T) * length;
            Span<byte> bytes = operation->buffer.AsSpan(bytePosition, add);
            Span<T> span = bytes.Reinterpret<byte, T>();
            bytePosition += add;
            return span;
        }

        /// <summary>
        /// Clears the operation of all instructions.
        /// </summary>
        public readonly void Clear()
        {
            MemoryAddress.ThrowIfDefault(operation);

            operation->bytesLength = 0;
            operation->count = 0;
        }

        /// <summary>
        /// Creates a new entity and optionally appends it to the selection.
        /// </summary>
        public readonly void CreateEntity(bool select = true)
        {
            WriteInstructionType(InstructionType.CreateEntities);
            WriteValue(1u);
            WriteValue(select);
        }

        /// <summary>
        /// Creates multiple entities and optionally appends them to the selection.
        /// </summary>
        public readonly void CreateEntities(int count, bool select = true)
        {
            if (count > 0)
            {
                WriteInstructionType(InstructionType.CreateEntities);
                WriteValue(count);
                WriteValue(select);
            }
        }

        /// <summary>
        /// Assigns the <paramref name="parent"/> to all selected entities.
        /// </summary>
        /// <param name="parent"></param>
        public readonly void SetParent(uint parent)
        {
            WriteInstructionType(InstructionType.SetParent);
            WriteValue(parent);
        }

        /// <summary>
        /// Assigns the <paramref name="parent"/> to all selected entities.
        /// </summary>
        public readonly void SetParent<T>(T parent) where T : unmanaged, IEntity
        {
            SetParent(parent.GetEntityValue());
        }

        /// <summary>
        /// Destroys all selected entities.
        /// </summary>
        public readonly void DestroySelected()
        {
            WriteInstructionType(InstructionType.DestroySelectedEntities);
        }

        /// <summary>
        /// Adds the given <paramref name="component"/> to the selected entities.
        /// </summary>
        public readonly void AddComponent<T>(T component) where T : unmanaged
        {
            WriteInstructionType(InstructionType.AddComponent);
            WriteTypeLayout(MetadataRegistry.GetType<T>());
            WriteValue(component);
        }

        /// <summary>
        /// Adds a <see langword="default"/> component of type <typeparamref name="T"/> to the selected entities.
        /// </summary>
        public readonly void AddComponent<T>() where T : unmanaged
        {
            WriteInstructionType(InstructionType.AddComponent);
            WriteTypeLayout(MetadataRegistry.GetType<T>());
            WriteValue(default(T));
        }

        /// <summary>
        /// Assigns an existing component to all selected entities.
        /// </summary>
        public readonly void SetComponent<T>(T component) where T : unmanaged
        {
            WriteInstructionType(InstructionType.SetComponent);
            WriteTypeLayout(MetadataRegistry.GetType<T>());
            WriteValue(component);
        }

        /// <summary>
        /// Either adds or assigns the component to the selected entities.
        /// </summary>
        public readonly void AddOrSetComponent<T>(T component) where T : unmanaged
        {
            WriteInstructionType(InstructionType.AddOrSetComponent);
            WriteTypeLayout(MetadataRegistry.GetType<T>());
            WriteValue(component);
        }

        /// <summary>
        /// Removes the component of type <typeparamref name="T"/> from the selected entities.
        /// </summary>
        public readonly void RemoveComponent<T>() where T : unmanaged
        {
            WriteInstructionType(InstructionType.RemoveComponent);
            WriteTypeLayout(MetadataRegistry.GetType<T>());
        }

        /// <summary>
        /// Creates an array of type <typeparamref name="T"/> on the selected entities.
        /// </summary>
        public readonly void CreateArray<T>(int length = 0) where T : unmanaged
        {
            WriteInstructionType(InstructionType.CreateArray);
            WriteTypeLayout(MetadataRegistry.GetType<T>());
            WriteValue(length);
        }

        /// <summary>
        /// Creates an array of type <typeparamref name="T"/> on the selected entities,
        /// initialized with the given <paramref name="values"/>.
        /// </summary>
        public readonly void CreateArray<T>(Span<T> values) where T : unmanaged
        {
            WriteInstructionType(InstructionType.CreateAndInitializeArray);
            WriteTypeLayout(MetadataRegistry.GetType<T>());
            WriteValue(values.Length);
            if (values.Length > 0)
            {
                WriteSpan(values);
            }
        }

        /// <summary>
        /// Creates an array of type <typeparamref name="T"/> on the selected entities,
        /// initialized with the given <paramref name="values"/>.
        /// </summary>
        public readonly void CreateArray<T>(ReadOnlySpan<T> values) where T : unmanaged
        {
            WriteInstructionType(InstructionType.CreateAndInitializeArray);
            WriteTypeLayout(MetadataRegistry.GetType<T>());
            WriteValue(values.Length);
            if (values.Length > 0)
            {
                WriteSpan(values);
            }
        }

        /// <summary>
        /// Resizes an existing array of type <typeparamref name="T"/> to have
        /// the <paramref name="newLength"/>.
        /// </summary>
        public readonly void ResizeArray<T>(int newLength) where T : unmanaged
        {
            WriteInstructionType(InstructionType.ResizeArray);
            WriteTypeLayout(MetadataRegistry.GetType<T>());
            WriteValue(newLength);
        }

        /// <summary>
        /// Modifies the array element at the given <paramref name="index"/> on the selected entities.
        /// </summary>
        public readonly void SetArrayElement<T>(int index, T value) where T : unmanaged
        {
            WriteInstructionType(InstructionType.SetArrayElements);
            WriteValue(index);
            WriteTypeLayout(MetadataRegistry.GetType<T>());
            WriteValue(1);
            WriteValue(value);
        }

        /// <summary>
        /// Updates the elements of an existing array.
        /// </summary>
        public readonly void SetArrayElements<T>(ReadOnlySpan<T> values) where T : unmanaged
        {
            if (values.Length > 0)
            {
                WriteInstructionType(InstructionType.SetArrayElements);
                WriteValue(0);
                WriteTypeLayout(MetadataRegistry.GetType<T>());
                WriteValue(values.Length);
                WriteSpan(values);
            }
        }

        /// <summary>
        /// Updates the elements of an existing array starting at <paramref name="index"/>.
        /// </summary>
        public readonly void SetArrayElements<T>(int index, ReadOnlySpan<T> values) where T : unmanaged
        {
            if (values.Length > 0)
            {
                WriteInstructionType(InstructionType.SetArrayElements);
                WriteValue(index);
                WriteTypeLayout(MetadataRegistry.GetType<T>());
                WriteValue(values.Length);
                WriteSpan(values);
            }
        }

        /// <summary>
        /// Updates the array to match the given <paramref name="values"/> exactly.
        /// </summary>
        public readonly void SetArray<T>(ReadOnlySpan<T> values) where T : unmanaged
        {
            WriteInstructionType(InstructionType.SetArray);
            WriteTypeLayout(MetadataRegistry.GetType<T>());
            WriteValue(values.Length);
            WriteSpan(values);
        }

        /// <summary>
        /// Creates a new array with the given <paramref name="values"/>, or updates an existing one
        /// to match exactly.
        /// </summary>
        public readonly void CreateOrSetArray<T>(ReadOnlySpan<T> values) where T : unmanaged
        {
            WriteInstructionType(InstructionType.CreateOrSetArray);
            WriteTypeLayout(MetadataRegistry.GetType<T>());
            WriteValue(values.Length);
            WriteSpan(values);
        }

        /// <summary>
        /// Creates a new array with the given <paramref name="values"/>, or updates an existing one
        /// to match exactly.
        /// </summary>
        public readonly void CreateOrSetArray<T>(Span<T> values) where T : unmanaged
        {
            WriteInstructionType(InstructionType.CreateOrSetArray);
            WriteTypeLayout(MetadataRegistry.GetType<T>());
            WriteValue(values.Length);
            WriteSpan(values);
        }

        /// <summary>
        /// Adds the given <paramref name="entity"/> to the selection.
        /// </summary>
        public readonly void SelectEntity(uint entity)
        {
            WriteInstructionType(InstructionType.SelectEntities);
            WriteValue(1);
            WriteValue(entity);
        }

        /// <summary>
        /// Adds the given <paramref name="entity"/> to the selection.
        /// </summary>
        public readonly void SelectEntity<T>(T entity) where T : unmanaged, IEntity
        {
            SelectEntity(entity.GetEntityValue());
        }

        /// <summary>
        /// Adds the given <paramref name="entities"/> to selection.
        /// </summary>
        public readonly void SelectEntities(ReadOnlySpan<uint> entities)
        {
            if (entities.Length > 0)
            {
                WriteInstructionType(InstructionType.SelectEntities);
                WriteValue(entities.Length);
                WriteSpan(entities);
            }
        }

        /// <summary>
        /// Selects the entity that was created <paramref name="createInstructionsAgo"/>.
        /// </summary>
        public readonly void SelectPreviouslyCreatedEntity(uint createInstructionsAgo)
        {
            WriteInstructionType(InstructionType.SelectPreviouslyCreatedEntity);
            WriteValue(createInstructionsAgo);
        }

        /// <summary>
        /// Clears the selection.
        /// </summary>
        public readonly void ClearSelection()
        {
            WriteInstructionType(InstructionType.ClearSelection);
        }

        /// <summary>
        /// Removes an existing <paramref name="reference"/> from all selected entities.
        /// </summary>
        public readonly void RemoveReference(rint reference)
        {
            WriteInstructionType(InstructionType.RemoveReference);
            WriteValue(reference);
        }

        /// <summary>
        /// For all entities in the selection, assigns the parent to the entity
        /// that was created <paramref name="createInstructionsAgo"/>.
        /// </summary>
        public readonly void SetParentToPreviouslyCreatedEntity(int createInstructionsAgo)
        {
            WriteInstructionType(InstructionType.SetParentToPreviouslyCreatedEntity);
            WriteValue(createInstructionsAgo);
        }

        /// <summary>
        /// For all entities in the selection, adds the entity that was created <paramref name="createInstructionsAgo"/>.
        /// </summary>
        public readonly void AddReferenceTowardsPreviouslyCreatedEntity(int createInstructionsAgo)
        {
            WriteInstructionType(InstructionType.AddReferenceToPreviouslyCreatedEntity);
            WriteValue(createInstructionsAgo);
        }

        /// <summary>
        /// Performs all recorded instructions on the given <paramref name="world"/>.
        /// </summary>
        public readonly void Perform(World world)
        {
            MemoryAddress.ThrowIfDefault(operation);

            if (operation->count > 0)
            {
                using List<uint> history = new(4);
                using List<uint> selection = new(4);
                Performing performing = new(this, world, history, selection);
                performing.Do();
            }
        }

        /// <summary>
        /// Creates a new operation to record instructions into.
        /// </summary>
        public static Operation Create()
        {
            return new(Implementation.Allocate(4));
        }

        readonly void ISerializable.Write(ByteWriter writer)
        {
            writer.WriteValue(operation->count);
            writer.WriteValue(operation->bytesLength);
            writer.WriteValue(operation->bytesCapacity);
            writer.WriteSpan(new Span<byte>(operation->buffer.Pointer, operation->bytesLength));
        }

        void ISerializable.Read(ByteReader reader)
        {
            int count = reader.ReadValue<int>();
            int bytesLength = reader.ReadValue<int>();
            int bytesCapacity = reader.ReadValue<int>();
            operation = Implementation.Allocate(bytesCapacity);
            operation->count = count;
            operation->bytesLength = bytesLength;
            operation->bytesCapacity = bytesCapacity;
            reader.ReadSpan<byte>(bytesLength).CopyTo(operation->buffer.GetSpan(bytesLength));
        }

        internal ref struct Performing
        {
            private readonly List<uint> history;
            private readonly List<uint> selection;
            private readonly Operation operation;
            private readonly World world;
            private int bytePosition;

            public Performing(Operation operation, World world, List<uint> history, List<uint> selection)
            {
                this.operation = operation;
                this.world = world;
                this.history = history;
                this.selection = selection;
                bytePosition = 0;
            }

            private void CreateEntities()
            {
                int count = operation.Read<int>(ref bytePosition);
                if (operation.Read<bool>(ref bytePosition))
                {
                    for (int i = 0; i < count; i++)
                    {
                        uint entity = world.CreateEntity();
                        history.Add(entity);
                        selection.Add(entity);
                    }
                }
                else
                {
                    for (int i = 0; i < count; i++)
                    {
                        uint entity = world.CreateEntity();
                        history.Add(entity);
                    }
                }
            }

            private void DestroySelectedEntities()
            {
                for (int i = 0; i < selection.Count; i++)
                {
                    uint entity = selection[i];
                    world.DestroyEntity(entity);
                    history.TryRemove(entity);
                }

                selection.Clear();
            }

            private void SelectEntities()
            {
                int count = operation.Read<int>(ref bytePosition);
                ReadOnlySpan<uint> entities = operation.ReadSpan<uint>(count, ref bytePosition);
                selection.AddRange(entities);
            }

            private readonly void ClearSelection()
            {
                selection.Clear();
            }

            private void SetParent()
            {
                uint parent = operation.Read<uint>(ref bytePosition);
                for (int i = 0; i < selection.Count; i++)
                {
                    world.SetParent(selection[i], parent);
                }
            }

            private void AddComponent()
            {
                Types.Type layout = operation.ReadTypeLayout(ref bytePosition);
                DataType dataType = world.Schema.GetComponentDataType(layout);
                int componentType = dataType.index;
                ReadOnlySpan<byte> component = operation.ReadBytes(dataType.size, ref bytePosition);
                for (int i = 0; i < selection.Count; i++)
                {
                    world.AddComponentBytes(selection[i], componentType, component);
                }
            }

            private void SetComponent()
            {
                Types.Type layout = operation.ReadTypeLayout(ref bytePosition);
                DataType dataType = world.Schema.GetComponentDataType(layout);
                int componentType = dataType.index;
                ReadOnlySpan<byte> component = operation.ReadBytes(dataType.size, ref bytePosition);
                for (int i = 0; i < selection.Count; i++)
                {
                    world.SetComponentBytes(selection[i], componentType, component);
                }
            }

            private void AddOrSetComponent()
            {
                Types.Type layout = operation.ReadTypeLayout(ref bytePosition);
                DataType dataType = world.Schema.GetComponentDataType(layout);
                int componentType = dataType.index;
                ReadOnlySpan<byte> component = operation.ReadBytes(dataType.size, ref bytePosition);
                for (int i = 0; i < selection.Count; i++)
                {
                    uint entity = selection[i];
                    if (world.ContainsComponent(entity, componentType))
                    {
                        world.SetComponentBytes(entity, componentType, component);
                    }
                    else
                    {
                        world.AddComponentBytes(entity, componentType, component);
                    }
                }
            }

            private void RemoveComponent()
            {
                Types.Type layout = operation.ReadTypeLayout(ref bytePosition);
                DataType dataType = world.Schema.GetComponentDataType(layout);
                int componentType = dataType.index;
                for (int i = 0; i < selection.Count; i++)
                {
                    world.RemoveComponent(selection[i], componentType);
                }
            }

            private void RemoveReference()
            {
                rint reference = operation.Read<rint>(ref bytePosition);
                for (int i = 0; i < selection.Count; i++)
                {
                    world.RemoveReference(selection[i], reference);
                }
            }

            private void CreateArray()
            {
                Types.Type layout = operation.ReadTypeLayout(ref bytePosition);
                DataType dataType = world.Schema.GetArrayDataType(layout);
                int arrayType = dataType.index;
                int arrayLength = operation.Read<int>(ref bytePosition);
                for (int i = 0; i < selection.Count; i++)
                {
                    world.CreateArray(selection[i], arrayType, arrayLength);
                }
            }

            private void CreateAndInitializeArray()
            {
                Types.Type layout = operation.ReadTypeLayout(ref bytePosition);
                DataType dataType = world.Schema.GetArrayDataType(layout);
                int arrayType = dataType.index;
                int length = operation.Read<int>(ref bytePosition);
                if (length > 0)
                {
                    ReadOnlySpan<byte> bytes = operation.ReadBytes(dataType.size * length, ref bytePosition);
                    for (int i = 0; i < selection.Count; i++)
                    {
                        Values array = world.CreateArray(selection[i], arrayType, length);
                        array.CopyFrom(bytes);
                    }
                }
                else
                {
                    for (int i = 0; i < selection.Count; i++)
                    {
                        world.CreateArray(selection[i], arrayType);
                    }
                }
            }

            private void ResizeArray()
            {
                Types.Type layout = operation.ReadTypeLayout(ref bytePosition);
                DataType dataType = world.Schema.GetArrayDataType(layout);
                int arrayType = dataType.index;
                int length = operation.Read<int>(ref bytePosition);
                for (int i = 0; i < selection.Count; i++)
                {
                    Values array = world.GetArray(selection[i], arrayType);
                    array.Length = length;
                }
            }

            private void SetArrayElements()
            {
                int index = operation.Read<int>(ref bytePosition);
                Types.Type layout = operation.ReadTypeLayout(ref bytePosition);
                int length = operation.Read<int>(ref bytePosition);
                DataType dataType = world.Schema.GetArrayDataType(layout);
                int arrayType = dataType.index;
                int stride = dataType.size;
                ReadOnlySpan<byte> bytes = operation.ReadBytes(stride * length, ref bytePosition);
                for (int i = 0; i < selection.Count; i++)
                {
                    Values array = world.GetArray(selection[i], arrayType);
                    MemoryAddress memory = array.Read(index * stride);
                    memory.CopyFrom(bytes);
                }
            }

            private void SetArray()
            {
                Types.Type layout = operation.ReadTypeLayout(ref bytePosition);
                DataType dataType = world.Schema.GetArrayDataType(layout);
                int arrayType = dataType.index;
                int length = operation.Read<int>(ref bytePosition);
                ReadOnlySpan<byte> bytes = operation.ReadBytes(dataType.size * length, ref bytePosition);
                for (int i = 0; i < selection.Count; i++)
                {
                    uint entity = selection[i];
                    Values array = world.GetArray(entity, arrayType);
                    array.CopyFrom(bytes);
                }
            }

            private void CreateOrSetArray()
            {
                Types.Type layout = operation.ReadTypeLayout(ref bytePosition);
                DataType dataType = world.Schema.GetArrayDataType(layout);
                int arrayType = dataType.index;
                int length = operation.Read<int>(ref bytePosition);
                ReadOnlySpan<byte> bytes = operation.ReadBytes(dataType.size * length, ref bytePosition);
                for (int i = 0; i < selection.Count; i++)
                {
                    uint entity = selection[i];
                    Values array;
                    if (world.ContainsArray(entity, arrayType))
                    {
                        array = world.GetArray(entity, arrayType);
                    }
                    else
                    {
                        array = world.CreateArray(entity, arrayType, length);
                    }

                    array.CopyFrom(bytes);
                }
            }

            private void SetParentToPreviouslyCreatedEntity()
            {
                int entitiesAgo = operation.Read<int>(ref bytePosition);
                uint parent = history[history.Count - entitiesAgo - 1];
                for (int i = 0; i < selection.Count; i++)
                {
                    world.SetParent(selection[i], parent);
                }
            }

            private void SelectPreviouslyCreatedEntity()
            {
                int entitiesAgo = operation.Read<int>(ref bytePosition);
                selection.Add(history[history.Count - entitiesAgo - 1]);
            }

            private void AddReferenceToPreviouslyCreatedEntity()
            {
                int entitiesAgo = operation.Read<int>(ref bytePosition);
                uint referencedEntity = history[history.Count - entitiesAgo - 1];
                for (int i = 0; i < selection.Count; i++)
                {
                    world.AddReference(selection[i], referencedEntity);
                }
            }

            public void Do()
            {
                while (bytePosition < operation.operation->bytesLength)
                {
                    InstructionType type = operation.ReadInstructionType(ref bytePosition);
                    switch (type)
                    {
                        case InstructionType.CreateEntities:
                            CreateEntities();
                            break;
                        case InstructionType.DestroySelectedEntities:
                            DestroySelectedEntities();
                            break;
                        case InstructionType.SelectEntities:
                            SelectEntities();
                            break;
                        case InstructionType.ClearSelection:
                            ClearSelection();
                            break;
                        case InstructionType.SetParent:
                            SetParent();
                            break;
                        case InstructionType.AddComponent:
                            AddComponent();
                            break;
                        case InstructionType.SetComponent:
                            SetComponent();
                            break;
                        case InstructionType.AddOrSetComponent:
                            AddOrSetComponent();
                            break;
                        case InstructionType.RemoveComponent:
                            RemoveComponent();
                            break;
                        case InstructionType.RemoveReference:
                            RemoveReference();
                            break;
                        case InstructionType.CreateArray:
                            CreateArray();
                            break;
                        case InstructionType.CreateAndInitializeArray:
                            CreateAndInitializeArray();
                            break;
                        case InstructionType.ResizeArray:
                            ResizeArray();
                            break;
                        case InstructionType.SetArrayElements:
                            SetArrayElements();
                            break;
                        case InstructionType.SetArray:
                            SetArray();
                            break;
                        case InstructionType.CreateOrSetArray:
                            CreateOrSetArray();
                            break;
                        case InstructionType.SetParentToPreviouslyCreatedEntity:
                            SetParentToPreviouslyCreatedEntity();
                            break;
                        case InstructionType.SelectPreviouslyCreatedEntity:
                            SelectPreviouslyCreatedEntity();
                            break;
                        case InstructionType.AddReferenceToPreviouslyCreatedEntity:
                            AddReferenceToPreviouslyCreatedEntity();
                            break;
                        default:
                            throw new NotImplementedException($"Unknown instruction type `{type}`");
                    }
                }
            }
        }

        internal struct Implementation
        {
            public int count;
            public int bytesLength;
            public int bytesCapacity;
            public MemoryAddress buffer;

            public static Implementation* Allocate(int minimumCapacity)
            {
                Implementation* operation = MemoryAddress.AllocatePointer<Implementation>();
                operation->count = 0;
                operation->bytesLength = 0;
                operation->bytesCapacity = minimumCapacity;
                operation->buffer = MemoryAddress.Allocate(minimumCapacity);
                return operation;
            }

            public static void Free(ref Implementation* operation)
            {
                MemoryAddress.ThrowIfDefault(operation);

                operation->buffer.Dispose();
                MemoryAddress.Free(ref operation);
            }
        }
    }
}