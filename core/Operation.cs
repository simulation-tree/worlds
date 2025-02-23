using Collections;
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
        /// Pointer of the operation.
        /// </summary>
        public readonly Implementation* Pointer => operation;

        /// <summary>
        /// Checks if this operation has been disposed.
        /// </summary>
        public readonly bool IsDisposed => operation is null;

        /// <summary>
        /// Counts how many instructions there are.
        /// </summary>
        public readonly uint Count
        {
            get
            {
                Allocations.ThrowIfNull(operation);

                return operation->count;
            }
        }

#if NET
        /// <summary>
        /// Creates a new operation to record instructions.
        /// </summary>
        public Operation()
        {
            operation = Implementation.Allocate();
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
            Allocations.ThrowIfNull(operation);

            uint newLength = operation->bytesLength + 1;
            if (operation->bytesCapacity < newLength)
            {
                operation->bytesCapacity *= 2;
                Allocation.Resize(ref operation->buffer, operation->bytesCapacity);
            }

            operation->buffer.Write(operation->bytesLength, (byte)type);
            operation->bytesLength = newLength;
            operation->count++;
        }

        private readonly void WriteTypeLayout(TypeLayout type)
        {
            Allocations.ThrowIfNull(operation);

            const uint Add = 8;
            uint newLength = operation->bytesLength + Add;
            if (operation->bytesCapacity < newLength)
            {
                operation->bytesCapacity = Allocations.GetNextPowerOf2(newLength);
                Allocation.Resize(ref operation->buffer, operation->bytesCapacity);
            }

            operation->buffer.Write(operation->bytesLength, type.Hash);
            operation->bytesLength = newLength;
        }

        private readonly void WriteValue<T>(T value) where T : unmanaged
        {
            Allocations.ThrowIfNull(operation);

            uint add = (uint)sizeof(T);
            uint newLength = operation->bytesLength + add;
            if (operation->bytesCapacity < newLength)
            {
                operation->bytesCapacity = Allocations.GetNextPowerOf2(newLength);
                Allocation.Resize(ref operation->buffer, operation->bytesCapacity);
            }

            operation->buffer.Write(operation->bytesLength, value);
            operation->bytesLength = newLength;
        }

        private readonly void WriteSpan<T>(USpan<T> span) where T : unmanaged
        {
            Allocations.ThrowIfNull(operation);

            uint add = (uint)sizeof(T) * span.Length;
            uint newLength = operation->bytesLength + add;
            if (operation->bytesCapacity < newLength)
            {
                operation->bytesCapacity = Allocations.GetNextPowerOf2(newLength);
                Allocation.Resize(ref operation->buffer, operation->bytesCapacity);
            }

            operation->buffer.Write(operation->bytesLength, span);
            operation->bytesLength = newLength;
        }

        private readonly InstructionType ReadInstructionType(ref uint bytePosition)
        {
            Allocations.ThrowIfNull(operation);

            InstructionType type = operation->buffer.Read<InstructionType>(bytePosition);
            bytePosition++;
            return type;
        }

        private readonly TypeLayout ReadTypeLayout(ref uint bytePosition)
        {
            Allocations.ThrowIfNull(operation);

            long hash = operation->buffer.Read<long>(bytePosition);
            bytePosition += sizeof(long);
            return TypeRegistry.Get(hash);
        }

        private readonly T Read<T>(ref uint bytePosition) where T : unmanaged
        {
            Allocations.ThrowIfNull(operation);

            T value = operation->buffer.Read<T>(bytePosition);
            bytePosition += (uint)sizeof(T);
            return value;
        }

        private readonly USpan<byte> ReadBytes(uint byteLength, ref uint bytePosition)
        {
            Allocations.ThrowIfNull(operation);

            USpan<byte> bytes = operation->buffer.AsSpan(bytePosition, byteLength);
            bytePosition += byteLength;
            return bytes;
        }

        private readonly USpan<T> ReadSpan<T>(uint length, ref uint bytePosition) where T : unmanaged
        {
            Allocations.ThrowIfNull(operation);

            uint add = (uint)sizeof(T) * length;
            USpan<byte> bytes = operation->buffer.AsSpan(bytePosition, add);
            USpan<T> span = bytes.Reinterpret<T>();
            bytePosition += add;
            return span;
        }

        /// <summary>
        /// Clears the operation of all instructions.
        /// </summary>
        public readonly void Clear()
        {
            Allocations.ThrowIfNull(operation);

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
        public readonly void CreateEntities(uint count, bool select = true)
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
            WriteTypeLayout(TypeRegistry.Get<T>());
            WriteValue(component);
        }

        /// <summary>
        /// Adds a <see langword="default"/> component of type <typeparamref name="T"/> to the selected entities.
        /// </summary>
        public readonly void AddComponent<T>() where T : unmanaged
        {
            WriteInstructionType(InstructionType.AddComponent);
            WriteTypeLayout(TypeRegistry.Get<T>());
            WriteValue(default(T));
        }

        /// <summary>
        /// Assigns an existing component to all selected entities.
        /// </summary>
        public readonly void SetComponent<T>(T component) where T : unmanaged
        {
            WriteInstructionType(InstructionType.SetComponent);
            WriteTypeLayout(TypeRegistry.Get<T>());
            WriteValue(component);
        }

        /// <summary>
        /// Either adds or assigns the component to the selected entities.
        /// </summary>
        public readonly void AddOrSetComponent<T>(T component) where T : unmanaged
        {
            WriteInstructionType(InstructionType.AddOrSetComponent);
            WriteTypeLayout(TypeRegistry.Get<T>());
            WriteValue(component);
        }

        /// <summary>
        /// Removes the component of type <typeparamref name="T"/> from the selected entities.
        /// </summary>
        public readonly void RemoveComponent<T>() where T : unmanaged
        {
            WriteInstructionType(InstructionType.RemoveComponent);
            WriteTypeLayout(TypeRegistry.Get<T>());
        }

        /// <summary>
        /// Creates an array of type <typeparamref name="T"/> on the selected entities.
        /// </summary>
        public readonly void CreateArray<T>(uint length = 0) where T : unmanaged
        {
            WriteInstructionType(InstructionType.CreateArray);
            WriteTypeLayout(TypeRegistry.Get<T>());
            WriteValue(length);
        }

        /// <summary>
        /// Creates an array of type <typeparamref name="T"/> on the selected entities,
        /// initialized with the given <paramref name="values"/>.
        /// </summary>
        public readonly void CreateArray<T>(USpan<T> values) where T : unmanaged
        {
            WriteInstructionType(InstructionType.CreateAndInitializeArray);
            WriteTypeLayout(TypeRegistry.Get<T>());
            WriteValue(values.Length);
            if (values.Length > 0)
            {
                WriteSpan(values);
            }
        }

        public readonly void ResizeArray<T>(uint newLength) where T : unmanaged
        {
            WriteInstructionType(InstructionType.ResizeArray);
            WriteTypeLayout(TypeRegistry.Get<T>());
            WriteValue(newLength);
        }

        /// <summary>
        /// Modifies the array element at the given <paramref name="index"/> on the selected entities.
        /// </summary>
        public readonly void SetArrayElement<T>(uint index, T value) where T : unmanaged
        {
            WriteInstructionType(InstructionType.SetArrayElements);
            WriteValue(index);
            WriteTypeLayout(TypeRegistry.Get<T>());
            WriteValue(1u);
            WriteValue(value);
        }

        /// <summary>
        /// Updates the elements of an existing array.
        /// </summary>
        public readonly void SetArrayElements<T>(USpan<T> values) where T : unmanaged
        {
            if (values.Length > 0)
            {
                WriteInstructionType(InstructionType.SetArrayElements);
                WriteValue(0u);
                WriteTypeLayout(TypeRegistry.Get<T>());
                WriteValue(values.Length);
                WriteSpan(values);
            }
        }

        /// <summary>
        /// Updates the elements of an existing array starting at <paramref name="index"/>.
        /// </summary>
        public readonly void SetArrayElements<T>(uint index, USpan<T> values) where T : unmanaged
        {
            if (values.Length > 0)
            {
                WriteInstructionType(InstructionType.SetArrayElements);
                WriteValue(index);
                WriteTypeLayout(TypeRegistry.Get<T>());
                WriteValue(values.Length);
                WriteSpan(values);
            }
        }

        /// <summary>
        /// Updates the array to match the given <paramref name="values"/> exactly.
        /// </summary>
        public readonly void SetArray<T>(USpan<T> values) where T : unmanaged
        {
            WriteInstructionType(InstructionType.SetArray);
            WriteTypeLayout(TypeRegistry.Get<T>());
            WriteValue(values.Length);
            WriteSpan(values);
        }

        /// <summary>
        /// Creates a new array with the given <paramref name="values"/>, or updates an existing one
        /// to match exactly.
        /// </summary>
        public readonly void CreateOrSetArray<T>(USpan<T> values) where T : unmanaged
        {
            WriteInstructionType(InstructionType.CreateOrSetArray);
            WriteTypeLayout(TypeRegistry.Get<T>());
            WriteValue(values.Length);
            WriteSpan(values);
        }

        /// <summary>
        /// Adds the given <paramref name="entity"/> to the selection.
        /// </summary>
        public readonly void SelectEntity(uint entity)
        {
            WriteInstructionType(InstructionType.SelectEntities);
            WriteValue(1u);
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
        public readonly void SelectEntities(USpan<uint> entities)
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
        public readonly void SetParentToPreviouslyCreatedEntity(uint createInstructionsAgo)
        {
            WriteInstructionType(InstructionType.SetParentToPreviouslyCreatedEntity);
            WriteValue(createInstructionsAgo);
        }

        /// <summary>
        /// For all entities in the selection, adds the entity that was created <paramref name="createInstructionsAgo"/>.
        /// </summary>
        public readonly void AddReferenceTowardsPreviouslyCreatedEntity(uint createInstructionsAgo)
        {
            WriteInstructionType(InstructionType.AddReferenceToPreviouslyCreatedEntity);
            WriteValue(createInstructionsAgo);
        }

        /// <summary>
        /// Performs all recorded instructions on the given <paramref name="world"/>.
        /// </summary>
        public readonly void Perform(World world)
        {
            Allocations.ThrowIfNull(operation);

            if (operation->count > 0)
            {
                using Performing performing = new(this, world);
                performing.Do();
            }
        }

        /// <summary>
        /// Creates a new operation to record instructions into.
        /// </summary>
        public static Operation Create()
        {
            return new(Implementation.Allocate());
        }

        readonly void ISerializable.Write(ByteWriter writer)
        {
            writer.WriteValue(operation->count);
            writer.WriteValue(operation->bytesLength);
            writer.WriteValue(operation->bytesCapacity);
            writer.WriteSpan(operation->buffer.AsSpan(0, operation->bytesLength));
        }

        void ISerializable.Read(ByteReader reader)
        {
            uint count = reader.ReadValue<uint>();
            uint bytesLength = reader.ReadValue<uint>();
            uint bytesCapacity = reader.ReadValue<uint>();
            operation = Implementation.Allocate();
            operation->count = count;
            operation->bytesLength = bytesLength;
            operation->bytesCapacity = bytesCapacity;
            Allocation.Resize(ref operation->buffer, bytesCapacity);
        }

        internal struct Performing : IDisposable
        {
            private readonly List<uint> history;
            private readonly List<uint> selection;
            private readonly Operation operation;
            private readonly World world;
            private uint bytePosition;

            public Performing(Operation operation, World world)
            {
                history = new(4);
                selection = new(4);
                this.operation = operation;
                this.world = world;
                bytePosition = 0;
            }

            public readonly void Dispose()
            {
                history.Dispose();
                selection.Dispose();
            }

            private void CreateEntities()
            {
                uint count = operation.Read<uint>(ref bytePosition);
                bool select = operation.Read<bool>(ref bytePosition);
                if (select)
                {
                    for (uint i = 0; i < count; i++)
                    {
                        uint entity = world.CreateEntity();
                        history.Add(entity);
                        selection.Add(entity);
                    }
                }
                else
                {
                    for (uint i = 0; i < count; i++)
                    {
                        uint entity = world.CreateEntity();
                        history.Add(entity);
                    }
                }
            }

            private void DestroySelectedEntities()
            {
                for (uint i = 0; i < selection.Count; i++)
                {
                    uint entity = selection[i];
                    world.DestroyEntity(entity);
                    history.TryRemove(entity);
                }

                selection.Clear();
            }

            private void SelectEntities()
            {
                uint count = operation.Read<uint>(ref bytePosition);
                USpan<uint> entities = operation.ReadSpan<uint>(count, ref bytePosition);
                selection.AddRange(entities);
            }

            private void ClearSelection()
            {
                selection.Clear();
            }

            private void SetParent()
            {
                uint parent = operation.Read<uint>(ref bytePosition);
                for (uint i = 0; i < selection.Count; i++)
                {
                    world.SetParent(selection[i], parent);
                }
            }

            private void AddComponent()
            {
                TypeLayout layout = operation.ReadTypeLayout(ref bytePosition);
                DataType componentType = world.Schema.GetComponentDataType(layout);
                USpan<byte> component = operation.ReadBytes(layout.Size, ref bytePosition);
                for (uint i = 0; i < selection.Count; i++)
                {
                    world.AddComponent(selection[i], componentType, component);
                }
            }

            private void SetComponent()
            {
                TypeLayout layout = operation.ReadTypeLayout(ref bytePosition);
                DataType componentType = world.Schema.GetComponentDataType(layout);
                USpan<byte> component = operation.ReadBytes(layout.Size, ref bytePosition);
                for (uint i = 0; i < selection.Count; i++)
                {
                    world.SetComponent(selection[i], componentType, component);
                }
            }

            private void AddOrSetComponent()
            {
                TypeLayout layout = operation.ReadTypeLayout(ref bytePosition);
                DataType componentType = world.Schema.GetComponentDataType(layout);
                USpan<byte> component = operation.ReadBytes(layout.Size, ref bytePosition);
                for (uint i = 0; i < selection.Count; i++)
                {
                    uint entity = selection[i];
                    if (world.ContainsComponent(entity, componentType))
                    {
                        world.SetComponent(entity, componentType, component);
                    }
                    else
                    {
                        world.AddComponent(entity, componentType, component);
                    }
                }
            }

            private void RemoveComponent()
            {
                TypeLayout layout = operation.ReadTypeLayout(ref bytePosition);
                DataType componentType = world.Schema.GetComponentDataType(layout);
                for (uint i = 0; i < selection.Count; i++)
                {
                    world.RemoveComponent(selection[i], componentType);
                }
            }

            private void RemoveReference()
            {
                rint reference = operation.Read<rint>(ref bytePosition);
                for (uint i = 0; i < selection.Count; i++)
                {
                    world.RemoveReference(selection[i], reference);
                }
            }

            private void CreateArray()
            {
                TypeLayout layout = operation.ReadTypeLayout(ref bytePosition);
                DataType elementType = world.Schema.GetArrayElementDataType(layout);
                uint arrayLength = operation.Read<uint>(ref bytePosition);
                for (uint i = 0; i < selection.Count; i++)
                {
                    world.CreateArray(selection[i], elementType, arrayLength);
                }
            }

            private void CreateAndInitializeArray()
            {
                TypeLayout layout = operation.ReadTypeLayout(ref bytePosition);
                DataType elementType = world.Schema.GetArrayElementDataType(layout);
                uint arrayLength = operation.Read<uint>(ref bytePosition);
                if (arrayLength > 0)
                {
                    USpan<byte> elements = operation.ReadBytes(layout.Size * arrayLength, ref bytePosition);
                    for (uint i = 0; i < selection.Count; i++)
                    {
                        Allocation array = world.CreateArray(selection[i], elementType, arrayLength);
                        array.Write(0, elements);
                    }
                }
                else
                {
                    for (uint i = 0; i < selection.Count; i++)
                    {
                        world.CreateArray(selection[i], elementType);
                    }
                }
            }

            private void ResizeArray()
            {
                TypeLayout layout = operation.ReadTypeLayout(ref bytePosition);
                DataType elementType = world.Schema.GetArrayElementDataType(layout);
                uint newLength = operation.Read<uint>(ref bytePosition);
                for (uint i = 0; i < selection.Count; i++)
                {
                    world.ResizeArray(selection[i], elementType, newLength);
                }
            }

            private void SetArrayElements()
            {
                uint index = operation.Read<uint>(ref bytePosition);
                TypeLayout layout = operation.ReadTypeLayout(ref bytePosition);
                uint length = operation.Read<uint>(ref bytePosition);
                DataType elementType = world.Schema.GetArrayElementDataType(layout);
                USpan<byte> elements = operation.ReadBytes(layout.Size * length, ref bytePosition);
                for (uint i = 0; i < selection.Count; i++)
                {
                    Allocation array = world.GetArray(selection[i], elementType, out _);
                    array.Write(index * layout.Size, elements);
                }
            }

            private void SetArray()
            {
                TypeLayout layout = operation.ReadTypeLayout(ref bytePosition);
                DataType elementType = world.Schema.GetArrayElementDataType(layout);
                uint expectedArrayLength = operation.Read<uint>(ref bytePosition);
                USpan<byte> elements = operation.ReadBytes(layout.Size * expectedArrayLength, ref bytePosition);
                for (uint i = 0; i < selection.Count; i++)
                {
                    uint entity = selection[i];
                    Allocation array = world.GetArray(entity, elementType, out uint arrayLength);
                    if (arrayLength != expectedArrayLength)
                    {
                        array = world.ResizeArray(entity, elementType, expectedArrayLength);
                    }

                    array.Write(0, elements);
                }
            }

            private void CreateOrSetArray()
            {
                TypeLayout layout = operation.ReadTypeLayout(ref bytePosition);
                DataType elementType = world.Schema.GetArrayElementDataType(layout);
                uint expectedArrayLength = operation.Read<uint>(ref bytePosition);
                USpan<byte> elements = operation.ReadBytes(layout.Size * expectedArrayLength, ref bytePosition);
                for (uint i = 0; i < selection.Count; i++)
                {
                    uint entity = selection[i];
                    if (world.ContainsArray(entity, elementType))
                    {
                        Allocation array = world.GetArray(entity, elementType, out uint arrayLength);
                        if (arrayLength != expectedArrayLength)
                        {
                            array = world.ResizeArray(entity, elementType, expectedArrayLength);
                        }

                        array.Write(0, elements);
                    }
                    else
                    {
                        Allocation array = world.CreateArray(entity, elementType, expectedArrayLength);
                        array.Write(0, elements);
                    }
                }
            }

            private void SetParentToPreviouslyCreatedEntity()
            {
                uint entitiesAgo = operation.Read<uint>(ref bytePosition);
                uint parent = history[history.Count - entitiesAgo - 1];
                for (uint i = 0; i < selection.Count; i++)
                {
                    world.SetParent(selection[i], parent);
                }
            }

            private void SelectPreviouslyCreatedEntity()
            {
                uint entitiesAgo = operation.Read<uint>(ref bytePosition);
                selection.Add(history[history.Count - entitiesAgo - 1]);
            }

            private void AddReferenceToPreviouslyCreatedEntity()
            {
                uint entitiesAgo = operation.Read<uint>(ref bytePosition);
                uint referencedEntity = history[history.Count - entitiesAgo - 1];
                for (uint i = 0; i < selection.Count; i++)
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

        public struct Implementation
        {
            public uint count;
            public uint bytesLength;
            public uint bytesCapacity;
            public Allocation buffer;

            public static Implementation* Allocate(uint minimumCapacity = 4)
            {
                minimumCapacity = Math.Max(1, Allocations.GetNextPowerOf2(minimumCapacity));
                ref Implementation operation = ref Allocations.Allocate<Implementation>();
                operation.count = 0;
                operation.bytesLength = 0;
                operation.bytesCapacity = minimumCapacity;
                operation.buffer = new(minimumCapacity);
                fixed (Implementation* pointer = &operation)
                {
                    return pointer;
                }
            }

            public static void Free(ref Implementation* operation)
            {
                Allocations.ThrowIfNull(operation);

                operation->buffer.Dispose();
                Allocations.Free(ref operation);
            }
        }
    }
}