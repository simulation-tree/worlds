using Collections.Generic;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unmanaged;

namespace Worlds
{
    /// <summary>
    /// Represents a collection of instructions for a world to perform.
    /// </summary>
    [SkipLocalsInit]
    public unsafe struct Operation : IDisposable
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
        /// Count of how many instructions are written to be performed.
        /// </summary>
        public readonly int Count
        {
            get
            {
                MemoryAddress.ThrowIfDefault(operation);

                return operation->instructionCount;
            }
        }

#if NET
        /// <inheritdoc/>
        [Obsolete("Default constructor not supported", true)]
        public Operation()
        {
        }
#endif

        /// <summary>
        /// Creates a new operation to record and perform instructions.
        /// </summary>
        public Operation(World world)
        {
            operation = MemoryAddress.AllocatePointer<Implementation>();
            operation->world = world;
            operation->instructionCount = 0;
            operation->selectedCount = 0;
            operation->createdCount = 0;
            operation->byteLength = 0;
            operation->byteCapacity = 32;
            operation->buffer = MemoryAddress.Allocate(operation->byteCapacity);
            operation->history = new(4);
            operation->selection = new(4);
        }

        /// <summary>
        /// Initializes an existing operation from the given <paramref name="pointer"/>.
        /// </summary>
        public Operation(void* pointer)
        {
            operation = (Implementation*)pointer;
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfSelectionIsEmpty()
        {
            if (operation->selectedCount == 0)
            {
                throw new InvalidOperationException("Cannot perform operation on an empty selection");
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            MemoryAddress.ThrowIfDefault(operation);

            operation->selection.Dispose();
            operation->history.Dispose();
            operation->buffer.Dispose();
            MemoryAddress.Free(ref operation);
        }

        /// <summary>
        /// Clears all instructions and resets the operation to the initial state.
        /// </summary>
        public readonly void Reset()
        {
            MemoryAddress.ThrowIfDefault(operation);

            operation->selection.Clear();
            operation->history.Clear();
            operation->byteLength = 0;
            operation->instructionCount = 0;
            operation->selectedCount = 0;
            operation->createdCount = 0;
        }

        /// <summary>
        /// Writes the entities that will be created into the <paramref name="destination"/> span.
        /// </summary>
        public readonly int GetCreatedEntities(Span<uint> destination)
        {
            MemoryAddress.ThrowIfDefault(operation);

            int count = 0;
            int bytePosition = 0;
            while (bytePosition < operation->byteLength)
            {
                InstructionType type = (InstructionType)operation->buffer.Read<byte>(bytePosition);
                bytePosition++;
                switch (type)
                {
                    case InstructionType.CreateSingleEntity:
                        destination[count] = operation->world.GetNextCreatedEntity(count);
                        count++;
                        break;
                    case InstructionType.CreateSingleEntityAndSelect:
                        destination[count] = operation->world.GetNextCreatedEntity(count);
                        count++;
                        break;
                    case InstructionType.CreateMultipleEntities:
                        {
                            uint createCount = operation->buffer.Read<uint>(bytePosition);
                            bytePosition += 4;
                            for (uint i = 0; i < createCount; i++)
                            {
                                destination[count] = operation->world.GetNextCreatedEntity(count);
                                count++;
                            }
                        }
                        break;
                    case InstructionType.CreateMultipleEntitiesAndSelect:
                        {
                            int createCount = operation->buffer.Read<int>(bytePosition);
                            bytePosition += 4;
                            for (uint i = 0; i < createCount; i++)
                            {
                                destination[count] = operation->world.GetNextCreatedEntity(count);
                                count++;
                            }
                        }
                        break;
                    case InstructionType.DestroySelectedEntities:
                        break;
                    case InstructionType.AppendMultipleEntitiesToSelection:
                        {
                            int selectLength = operation->buffer.Read<int>(bytePosition);
                            bytePosition += 4 + (selectLength * 4);
                        }
                        break;
                    case InstructionType.SetSelectedEntity:
                        bytePosition += 4;
                        break;
                    case InstructionType.AppendEntityToSelection:
                        bytePosition += 4;
                        break;
                    case InstructionType.ClearSelection:
                        break;
                    case InstructionType.SetParent:
                        bytePosition += 4;
                        break;
                    case InstructionType.EnableSelectedEntities:
                        break;
                    case InstructionType.DisableSelectedEntities:
                        break;
                    case InstructionType.AddComponentType:
                        bytePosition += 4;
                        break;
                    case InstructionType.TryAddComponentType:
                        bytePosition += 4;
                        break;
                    case InstructionType.AddComponent:
                        {
                            bytePosition += 4;
                            int componentSize = operation->buffer.Read<int>(bytePosition);
                            bytePosition += 4 + componentSize;
                        }
                        break;
                    case InstructionType.SetComponent:
                        {
                            bytePosition += 4;
                            int componentSize = operation->buffer.Read<int>(bytePosition);
                            bytePosition += 4 + componentSize;
                        }
                        break;
                    case InstructionType.AddOrSetComponent:
                        {
                            bytePosition += 4;
                            int componentSize = operation->buffer.Read<int>(bytePosition);
                            bytePosition += 4 + componentSize;
                        }
                        break;
                    case InstructionType.RemoveComponentType:
                        bytePosition += 4;
                        break;
                    case InstructionType.RemoveReference:
                        bytePosition += 4;
                        break;
                    case InstructionType.CreateArray:
                        bytePosition += 8;
                        break;
                    case InstructionType.CreateAndInitializeArray:
                        {
                            bytePosition += 4;
                            int arrayLength = operation->buffer.Read<int>(bytePosition);
                            bytePosition += 4;
                            int arrayStride = operation->buffer.Read<int>(bytePosition);
                            bytePosition += 4 + (arrayLength * arrayStride);
                        }
                        break;
                    case InstructionType.ResizeArray:
                        bytePosition += 8;
                        break;
                    case InstructionType.SetArrayElement:
                        {
                            bytePosition += 4;
                            int arrayStride = operation->buffer.Read<int>(bytePosition);
                            bytePosition += 8 + arrayStride;
                        }
                        break;
                    case InstructionType.SetArrayElements:
                        {
                            bytePosition += 4;
                            int arrayStride = operation->buffer.Read<int>(bytePosition);
                            bytePosition += 8;
                            int arrayLength = operation->buffer.Read<int>(bytePosition);
                            bytePosition += 4 + (arrayLength * arrayStride);
                        }
                        break;
                    case InstructionType.SetArray:
                        {
                            bytePosition += 4;
                            int arrayStride = operation->buffer.Read<int>(bytePosition);
                            bytePosition += 4;
                            int arrayLength = operation->buffer.Read<int>(bytePosition);
                            bytePosition += 4 + (arrayLength * arrayStride);
                        }
                        break;
                    case InstructionType.CreateOrSetArray:
                        {
                            bytePosition += 4;
                            int arrayStride = operation->buffer.Read<int>(bytePosition);
                            bytePosition += 4;
                            int arrayLength = operation->buffer.Read<int>(bytePosition);
                            bytePosition += 4 + (arrayLength * arrayStride);
                        }
                        break;
                    case InstructionType.AddTag:
                        bytePosition += 4;
                        break;
                    case InstructionType.RemoveTag:
                        bytePosition += 4;
                        break;
                    case InstructionType.SetParentToPreviouslyCreatedEntity:
                        bytePosition += 4;
                        break;
                    case InstructionType.AppendPreviouslyCreatedEntityToSelection:
                        bytePosition += 4;
                        break;
                    case InstructionType.AddReferenceToPreviouslyCreatedEntity:
                        bytePosition += 4;
                        break;
                    default:
                        throw new NotImplementedException($"Unknown instruction type `{type}`");
                }
            }

            return count;
        }

        /// <summary>
        /// Creates a new entity without selecting it.
        /// </summary>
        public readonly void CreateSingleEntity()
        {
            MemoryAddress.ThrowIfDefault(operation);

            int byteLength = operation->byteLength;
            if (operation->byteCapacity == byteLength)
            {
                MemoryAddress.ResizePowerOf2(ref operation->buffer, operation->byteCapacity);
                operation->byteCapacity *= 2;
            }

            operation->buffer.Write(byteLength, (byte)InstructionType.CreateSingleEntity);
            operation->byteLength = byteLength + 1;
            operation->instructionCount++;
            operation->createdCount++;
        }

        /// <summary>
        /// Creates a new entity and appends it to the selection.
        /// </summary>
        public readonly void CreateSingleEntityAndSelect()
        {
            MemoryAddress.ThrowIfDefault(operation);

            int byteLength = operation->byteLength;
            if (operation->byteCapacity == byteLength)
            {
                MemoryAddress.ResizePowerOf2(ref operation->buffer, operation->byteCapacity);
                operation->byteCapacity *= 2;
            }

            operation->buffer.Write(byteLength, (byte)InstructionType.CreateSingleEntityAndSelect);
            operation->byteLength = byteLength + 1;
            operation->instructionCount++;
            operation->createdCount++;
            operation->selectedCount++;
        }

        /// <summary>
        /// Creates multiple entities without selecting them.
        /// </summary>
        public readonly void CreateMultipleEntities(int count)
        {
            MemoryAddress.ThrowIfDefault(operation);

            int byteLength = operation->byteLength;
            if (operation->byteCapacity <= byteLength + 5)
            {
                //the new capacity will always be able to contain the next 5 bytes, because the initial value is 32
                MemoryAddress.ResizePowerOf2(ref operation->buffer, operation->byteCapacity);
                operation->byteCapacity *= 2;
            }

            operation->buffer.Write(byteLength, (byte)InstructionType.CreateMultipleEntities);
            operation->buffer.Write(byteLength + 1, count);
            operation->byteLength = byteLength + 5;
            operation->instructionCount++;
            operation->createdCount += count;
        }

        /// <summary>
        /// Creates multiple entities and appends them to the selection.
        /// </summary>
        public readonly void CreateMultipleEntitiesAndSelect(int count)
        {
            MemoryAddress.ThrowIfDefault(operation);

            int byteLength = operation->byteLength;
            if (operation->byteCapacity <= byteLength + 5)
            {
                //the new capacity will always be able to contain the next 5 bytes, because the initial value is 32
                MemoryAddress.ResizePowerOf2(ref operation->buffer, operation->byteCapacity);
                operation->byteCapacity *= 2;
            }

            operation->buffer.Write(byteLength, (byte)InstructionType.CreateMultipleEntitiesAndSelect);
            operation->buffer.Write(byteLength + 1, count);
            operation->byteLength = byteLength + 5;
            operation->instructionCount++;
            operation->createdCount += count;
            operation->selectedCount += count;
        }

        /// <summary>
        /// Assigns the <paramref name="parent"/> to all selected entities.
        /// </summary>
        public readonly void SetParent(uint parent)
        {
            MemoryAddress.ThrowIfDefault(operation);
            ThrowIfSelectionIsEmpty();

            int byteLength = operation->byteLength;
            if (operation->byteCapacity <= byteLength + 5)
            {
                //the new capacity will always be able to contain the next 5 bytes, because the initial value is 32
                MemoryAddress.ResizePowerOf2(ref operation->buffer, operation->byteCapacity);
                operation->byteCapacity *= 2;
            }

            operation->buffer.Write(byteLength, (byte)InstructionType.SetParent);
            operation->buffer.Write(byteLength + 1, parent);
            operation->byteLength = byteLength + 5;
            operation->instructionCount++;
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
        public readonly void DestroySelectedEntities()
        {
            MemoryAddress.ThrowIfDefault(operation);
            ThrowIfSelectionIsEmpty();

            int byteLength = operation->byteLength;
            if (operation->byteCapacity == byteLength)
            {
                MemoryAddress.ResizePowerOf2(ref operation->buffer, operation->byteCapacity);
                operation->byteCapacity *= 2;
            }

            operation->buffer.Write(byteLength, (byte)InstructionType.DestroySelectedEntities);
            operation->byteLength = byteLength + 1;
            operation->instructionCount++;
            operation->selectedCount = 0;
        }

        /// <summary>
        /// Enable selected entities.
        /// </summary>
        public readonly void EnableSelectedEntities()
        {
            MemoryAddress.ThrowIfDefault(operation);
            ThrowIfSelectionIsEmpty();

            int byteLength = operation->byteLength;
            if (operation->byteCapacity == byteLength)
            {
                MemoryAddress.ResizePowerOf2(ref operation->buffer, operation->byteCapacity);
                operation->byteCapacity *= 2;
            }

            operation->buffer.Write(byteLength, (byte)InstructionType.EnableSelectedEntities);
            operation->byteLength = byteLength + 1;
            operation->instructionCount++;
        }

        /// <summary>
        /// Disables selected entities.
        /// </summary>
        public readonly void DisableSelectedEntities()
        {
            MemoryAddress.ThrowIfDefault(operation);
            ThrowIfSelectionIsEmpty();

            int byteLength = operation->byteLength;
            if (operation->byteCapacity == byteLength)
            {
                MemoryAddress.ResizePowerOf2(ref operation->buffer, operation->byteCapacity);
                operation->byteCapacity *= 2;
            }

            operation->buffer.Write(byteLength, (byte)InstructionType.DisableSelectedEntities);
            operation->byteLength = byteLength + 1;
            operation->instructionCount++;
        }

        /// <summary>
        /// Adds the given <paramref name="component"/> to the selected entities.
        /// </summary>
        public readonly void AddComponent<T>(T component) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(operation);
            ThrowIfSelectionIsEmpty();

            int byteCapacity = operation->byteCapacity;
            int byteLength = operation->byteLength;
            int newByteLength = byteLength + 9 + sizeof(T);
            if (byteCapacity <= newByteLength)
            {
                byteCapacity = newByteLength.GetNextPowerOf2();
                MemoryAddress.Resize(ref operation->buffer, byteCapacity);
                operation->byteCapacity = byteCapacity;
            }

            operation->buffer.Write(byteLength, (byte)InstructionType.AddComponent);
            operation->buffer.Write(byteLength + 1, operation->world.world->schema.GetComponentType<T>());
            operation->buffer.Write(byteLength + 5, sizeof(T));
            operation->buffer.Write(byteLength + 9, component);
            operation->byteLength = newByteLength;
            operation->instructionCount++;
        }

        /// <summary>
        /// Adds the given <paramref name="component"/> to the selected entities.
        /// </summary>
        public readonly void AddComponent<T>(T component, int componentType) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(operation);
            ThrowIfSelectionIsEmpty();

            int byteCapacity = operation->byteCapacity;
            int byteLength = operation->byteLength;
            int newByteLength = byteLength + 9 + sizeof(T);
            if (byteCapacity <= newByteLength)
            {
                byteCapacity = newByteLength.GetNextPowerOf2();
                MemoryAddress.Resize(ref operation->buffer, byteCapacity);
                operation->byteCapacity = byteCapacity;
            }

            operation->buffer.Write(byteLength, (byte)InstructionType.AddComponent);
            operation->buffer.Write(byteLength + 1, componentType);
            operation->buffer.Write(byteLength + 5, sizeof(T));
            operation->buffer.Write(byteLength + 9, component);
            operation->byteLength = newByteLength;
            operation->instructionCount++;
        }

        /// <summary>
        /// Adds a <see langword="default"/> component of type <typeparamref name="T"/> to the selected entities.
        /// </summary>
        public readonly void AddComponentType<T>() where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(operation);
            ThrowIfSelectionIsEmpty();

            int byteLength = operation->byteLength;
            if (operation->byteCapacity <= byteLength + 5)
            {
                MemoryAddress.ResizePowerOf2(ref operation->buffer, operation->byteCapacity);
                operation->byteCapacity *= 2;
            }

            operation->buffer.Write(byteLength, (byte)InstructionType.AddComponentType);
            operation->buffer.Write(byteLength + 1, operation->world.world->schema.GetComponentType<T>());
            operation->byteLength = byteLength + 5;
            operation->instructionCount++;
        }

        /// <summary>
        /// Adds a <see langword="default"/> <paramref name="componentType"/> to the selected entities.
        /// </summary>
        public readonly void AddComponentType(int componentType)
        {
            MemoryAddress.ThrowIfDefault(operation);
            ThrowIfSelectionIsEmpty();

            int byteLength = operation->byteLength;
            if (operation->byteCapacity <= byteLength + 5)
            {
                MemoryAddress.ResizePowerOf2(ref operation->buffer, operation->byteCapacity);
                operation->byteCapacity *= 2;
            }

            operation->buffer.Write(byteLength, (byte)InstructionType.AddComponentType);
            operation->buffer.Write(byteLength + 1, componentType);
            operation->byteLength = byteLength + 5;
            operation->instructionCount++;
        }

        /// <summary>
        /// Adds a <see langword="default"/> component of type <typeparamref name="T"/> to the selected entities
        /// if not already present.
        /// </summary>
        public readonly void TryAddComponentType<T>() where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(operation);
            ThrowIfSelectionIsEmpty();

            int byteLength = operation->byteLength;
            if (operation->byteCapacity <= byteLength + 5)
            {
                MemoryAddress.ResizePowerOf2(ref operation->buffer, operation->byteCapacity);
                operation->byteCapacity *= 2;
            }

            operation->buffer.Write(byteLength, (byte)InstructionType.TryAddComponentType);
            operation->buffer.Write(byteLength + 1, operation->world.world->schema.GetComponentType<T>());
            operation->byteLength = byteLength + 5;
            operation->instructionCount++;
        }

        /// <summary>
        /// Adds a <see langword="default"/> <paramref name="componentType"/> to the selected entities
        /// if not already present.
        /// </summary>
        public readonly void TryAddComponentType(int componentType)
        {
            MemoryAddress.ThrowIfDefault(operation);
            ThrowIfSelectionIsEmpty();

            int byteLength = operation->byteLength;
            if (operation->byteCapacity <= byteLength + 5)
            {
                MemoryAddress.ResizePowerOf2(ref operation->buffer, operation->byteCapacity);
                operation->byteCapacity *= 2;
            }

            operation->buffer.Write(byteLength, (byte)InstructionType.TryAddComponentType);
            operation->buffer.Write(byteLength + 1, componentType);
            operation->byteLength = byteLength + 5;
            operation->instructionCount++;
        }

        /// <summary>
        /// Assigns an existing component to all selected entities.
        /// </summary>
        public readonly void SetComponent<T>(T component) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(operation);
            ThrowIfSelectionIsEmpty();

            int byteCapacity = operation->byteCapacity;
            int byteLength = operation->byteLength;
            int newByteLength = byteLength + 9 + sizeof(T);
            if (byteCapacity <= newByteLength)
            {
                byteCapacity = newByteLength.GetNextPowerOf2();
                MemoryAddress.Resize(ref operation->buffer, byteCapacity);
                operation->byteCapacity = byteCapacity;
            }

            operation->buffer.Write(byteLength, (byte)InstructionType.SetComponent);
            operation->buffer.Write(byteLength + 1, operation->world.world->schema.GetComponentType<T>());
            operation->buffer.Write(byteLength + 5, sizeof(T));
            operation->buffer.Write(byteLength + 9, component);
            operation->byteLength = newByteLength;
            operation->instructionCount++;
        }

        /// <summary>
        /// Assigns an existing component to all selected entities.
        /// </summary>
        public readonly void SetComponent<T>(T component, int componentType) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(operation);
            ThrowIfSelectionIsEmpty();

            int byteCapacity = operation->byteCapacity;
            int byteLength = operation->byteLength;
            int newByteLength = byteLength + 9 + sizeof(T);
            if (byteCapacity <= newByteLength)
            {
                byteCapacity = newByteLength.GetNextPowerOf2();
                MemoryAddress.Resize(ref operation->buffer, byteCapacity);
                operation->byteCapacity = byteCapacity;
            }

            operation->buffer.Write(byteLength, (byte)InstructionType.SetComponent);
            operation->buffer.Write(byteLength + 1, componentType);
            operation->buffer.Write(byteLength + 5, sizeof(T));
            operation->buffer.Write(byteLength + 9, component);
            operation->byteLength = newByteLength;
            operation->instructionCount++;
        }

        /// <summary>
        /// Either adds or assigns the component to the selected entities.
        /// </summary>
        public readonly void AddOrSetComponent<T>(T component) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(operation);
            ThrowIfSelectionIsEmpty();

            int byteCapacity = operation->byteCapacity;
            int byteLength = operation->byteLength;
            int newByteLength = byteLength + 9 + sizeof(T);
            if (byteCapacity <= newByteLength)
            {
                byteCapacity = newByteLength.GetNextPowerOf2();
                MemoryAddress.Resize(ref operation->buffer, byteCapacity);
                operation->byteCapacity = byteCapacity;
            }

            operation->buffer.Write(byteLength, (byte)InstructionType.AddOrSetComponent);
            operation->buffer.Write(byteLength + 1, operation->world.world->schema.GetComponentType<T>());
            operation->buffer.Write(byteLength + 5, sizeof(T));
            operation->buffer.Write(byteLength + 9, component);
            operation->byteLength = newByteLength;
            operation->instructionCount++;
        }

        /// <summary>
        /// Either adds or assigns the component to the selected entities.
        /// </summary>
        public readonly void AddOrSetComponent<T>(T component, int componentType) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(operation);
            ThrowIfSelectionIsEmpty();

            int byteCapacity = operation->byteCapacity;
            int byteLength = operation->byteLength;
            int newByteLength = byteLength + 9 + sizeof(T);
            if (byteCapacity <= newByteLength)
            {
                byteCapacity = newByteLength.GetNextPowerOf2();
                MemoryAddress.Resize(ref operation->buffer, byteCapacity);
                operation->byteCapacity = byteCapacity;
            }

            operation->buffer.Write(byteLength, (byte)InstructionType.AddOrSetComponent);
            operation->buffer.Write(byteLength + 1, componentType);
            operation->buffer.Write(byteLength + 5, sizeof(T));
            operation->buffer.Write(byteLength + 9, component);
            operation->byteLength = newByteLength;
            operation->instructionCount++;
        }

        /// <summary>
        /// Removes the component of type <typeparamref name="T"/> from the selected entities.
        /// </summary>
        public readonly void RemoveComponentType<T>() where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(operation);
            ThrowIfSelectionIsEmpty();

            int byteLength = operation->byteLength;
            if (operation->byteCapacity <= byteLength + 5)
            {
                MemoryAddress.ResizePowerOf2(ref operation->buffer, operation->byteCapacity);
                operation->byteCapacity *= 2;
            }

            operation->buffer.Write(byteLength, (byte)InstructionType.RemoveComponentType);
            operation->buffer.Write(byteLength + 1, operation->world.world->schema.GetComponentType<T>());
            operation->byteLength = byteLength + 5;
            operation->instructionCount++;
        }

        /// <summary>
        /// Removes the <paramref name="componentType"/> from the selected entities.
        /// </summary>
        public readonly void RemoveComponentType(int componentType)
        {
            MemoryAddress.ThrowIfDefault(operation);
            ThrowIfSelectionIsEmpty();

            int byteLength = operation->byteLength;
            if (operation->byteCapacity <= byteLength + 5)
            {
                MemoryAddress.ResizePowerOf2(ref operation->buffer, operation->byteCapacity);
                operation->byteCapacity *= 2;
            }

            operation->buffer.Write(byteLength, (byte)InstructionType.RemoveComponentType);
            operation->buffer.Write(byteLength + 1, componentType);
            operation->byteLength = byteLength + 5;
            operation->instructionCount++;
        }

        /// <summary>
        /// Creates an array of type <typeparamref name="T"/> on the selected entities.
        /// </summary>
        public readonly void CreateArray<T>(int length = 0) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(operation);
            ThrowIfSelectionIsEmpty();

            int byteLength = operation->byteLength;
            if (operation->byteCapacity <= byteLength + 9)
            {
                MemoryAddress.ResizePowerOf2(ref operation->buffer, operation->byteCapacity);
                operation->byteCapacity *= 2;
            }

            operation->buffer.Write(byteLength, (byte)InstructionType.CreateArray);
            operation->buffer.Write(byteLength + 1, operation->world.world->schema.GetArrayType<T>());
            operation->buffer.Write(byteLength + 5, length);
            operation->byteLength = byteLength + 9;
            operation->instructionCount++;
        }

        /// <summary>
        /// Creates a <paramref name="arrayType"/> array on the selected entities.
        /// </summary>
        public readonly void CreateArray(int arrayType, int length = 0)
        {
            MemoryAddress.ThrowIfDefault(operation);
            ThrowIfSelectionIsEmpty();

            int byteLength = operation->byteLength;
            if (operation->byteCapacity <= byteLength + 9)
            {
                MemoryAddress.ResizePowerOf2(ref operation->buffer, operation->byteCapacity);
                operation->byteCapacity *= 2;
            }

            operation->buffer.Write(byteLength, (byte)InstructionType.CreateArray);
            operation->buffer.Write(byteLength + 1, arrayType);
            operation->buffer.Write(byteLength + 5, length);
            operation->byteLength = byteLength + 9;
            operation->instructionCount++;
        }

        /// <summary>
        /// Creates an array of type <typeparamref name="T"/> on the selected entities,
        /// initialized with the given <paramref name="values"/>.
        /// </summary>
        public readonly void CreateArray<T>(ReadOnlySpan<T> values) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(operation);
            ThrowIfSelectionIsEmpty();

            int byteCapacity = operation->byteCapacity;
            int byteLength = operation->byteLength;
            int newByteLength = byteLength + 13 + (sizeof(T) * values.Length);
            if (byteCapacity <= newByteLength)
            {
                byteCapacity = newByteLength.GetNextPowerOf2();
                MemoryAddress.Resize(ref operation->buffer, byteCapacity);
                operation->byteCapacity = byteCapacity;
            }

            operation->buffer.Write(byteLength, (byte)InstructionType.CreateAndInitializeArray);
            operation->buffer.Write(byteLength + 1, operation->world.world->schema.GetArrayType<T>());
            operation->buffer.Write(byteLength + 5, sizeof(T));
            operation->buffer.Write(byteLength + 9, values.Length);
            operation->buffer.Write(byteLength + 13, values);
            operation->byteLength = newByteLength;
            operation->instructionCount++;
        }

        /// <summary>
        /// Creates an array of type <typeparamref name="T"/> on the selected entities,
        /// initialized with the given <paramref name="values"/>.
        /// </summary>
        public readonly void CreateArray<T>(Span<T> values) where T : unmanaged
        {
            CreateArray((ReadOnlySpan<T>)values);
        }

        /// <summary>
        /// Creates an array of type <typeparamref name="T"/> on the selected entities,
        /// initialized with the given <paramref name="values"/>.
        /// </summary>
        public readonly void CreateArray<T>(ReadOnlySpan<T> values, int arrayType) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(operation);
            ThrowIfSelectionIsEmpty();

            int byteCapacity = operation->byteCapacity;
            int byteLength = operation->byteLength;
            int newByteLength = byteLength + 13 + (sizeof(T) * values.Length);
            if (byteCapacity <= newByteLength)
            {
                byteCapacity = newByteLength.GetNextPowerOf2();
                MemoryAddress.Resize(ref operation->buffer, byteCapacity);
                operation->byteCapacity = byteCapacity;
            }

            operation->buffer.Write(byteLength, (byte)InstructionType.CreateAndInitializeArray);
            operation->buffer.Write(byteLength + 1, arrayType);
            operation->buffer.Write(byteLength + 5, sizeof(T));
            operation->buffer.Write(byteLength + 9, values.Length);
            operation->buffer.Write(byteLength + 13, values);
            operation->byteLength = newByteLength;
            operation->instructionCount++;
        }

        /// <summary>
        /// Creates an array of type <typeparamref name="T"/> on the selected entities,
        /// initialized with the given <paramref name="values"/>.
        /// </summary>
        public readonly void CreateArray<T>(Span<T> values, int arrayType) where T : unmanaged
        {
            CreateArray((ReadOnlySpan<T>)values, arrayType);
        }

        /// <summary>
        /// Resizes an existing array of type <typeparamref name="T"/> to have
        /// the <paramref name="newLength"/>.
        /// </summary>
        public readonly void ResizeArray<T>(int newLength) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(operation);
            ThrowIfSelectionIsEmpty();

            int byteLength = operation->byteLength;
            if (operation->byteCapacity <= byteLength + 9)
            {
                MemoryAddress.ResizePowerOf2(ref operation->buffer, operation->byteCapacity);
                operation->byteCapacity *= 2;
            }

            operation->buffer.Write(byteLength, (byte)InstructionType.ResizeArray);
            operation->buffer.Write(byteLength + 1, operation->world.world->schema.GetArrayType<T>());
            operation->buffer.Write(byteLength + 5, newLength);
            operation->byteLength = byteLength + 9;
            operation->instructionCount++;
        }

        /// <summary>
        /// Resizes an existing array of type <typeparamref name="T"/> to have
        /// the <paramref name="newLength"/>.
        /// </summary>
        public readonly void ResizeArray<T>(int newLength, int arrayType) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(operation);
            ThrowIfSelectionIsEmpty();

            int byteLength = operation->byteLength;
            if (operation->byteCapacity <= byteLength + 9)
            {
                MemoryAddress.ResizePowerOf2(ref operation->buffer, operation->byteCapacity);
                operation->byteCapacity *= 2;
            }

            operation->buffer.Write(byteLength, (byte)InstructionType.ResizeArray);
            operation->buffer.Write(byteLength + 1, arrayType);
            operation->buffer.Write(byteLength + 5, newLength);
            operation->byteLength = byteLength + 9;
            operation->instructionCount++;
        }

        /// <summary>
        /// Modifies the array element at the given <paramref name="index"/> on the selected entities.
        /// </summary>
        public readonly void SetArrayElement<T>(int index, T value) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(operation);
            ThrowIfSelectionIsEmpty();

            int byteCapacity = operation->byteCapacity;
            int byteLength = operation->byteLength;
            int newByteLength = byteLength + 13 + sizeof(T);
            if (byteCapacity <= newByteLength)
            {
                byteCapacity = newByteLength.GetNextPowerOf2();
                MemoryAddress.Resize(ref operation->buffer, byteCapacity);
                operation->byteCapacity = byteCapacity;
            }

            operation->buffer.Write(byteLength, (byte)InstructionType.SetArrayElement);
            operation->buffer.Write(byteLength + 1, operation->world.world->schema.GetArrayType<T>());
            operation->buffer.Write(byteLength + 5, sizeof(T));
            operation->buffer.Write(byteLength + 9, index);
            operation->buffer.Write(byteLength + 13, value);
            operation->byteLength = newByteLength;
            operation->instructionCount++;
        }

        /// <summary>
        /// Modifies the array element at the given <paramref name="index"/> on the selected entities.
        /// </summary>
        public readonly void SetArrayElement<T>(int index, T value, int arrayType) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(operation);
            ThrowIfSelectionIsEmpty();

            int byteCapacity = operation->byteCapacity;
            int byteLength = operation->byteLength;
            int newByteLength = byteLength + 13 + sizeof(T);
            if (byteCapacity <= newByteLength)
            {
                byteCapacity = newByteLength.GetNextPowerOf2();
                MemoryAddress.Resize(ref operation->buffer, byteCapacity);
                operation->byteCapacity = byteCapacity;
            }

            operation->buffer.Write(byteLength, (byte)InstructionType.SetArrayElement);
            operation->buffer.Write(byteLength + 1, arrayType);
            operation->buffer.Write(byteLength + 5, sizeof(T));
            operation->buffer.Write(byteLength + 9, index);
            operation->buffer.Write(byteLength + 13, value);
            operation->byteLength = newByteLength;
            operation->instructionCount++;
        }

        /// <summary>
        /// Updates the elements of an existing array.
        /// </summary>
        public readonly void SetArrayElements<T>(ReadOnlySpan<T> values) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(operation);
            ThrowIfSelectionIsEmpty();

            int byteCapacity = operation->byteCapacity;
            int byteLength = operation->byteLength;
            int newByteLength = byteLength + 17 + (sizeof(T) * values.Length);
            if (byteCapacity <= newByteLength)
            {
                byteCapacity = newByteLength.GetNextPowerOf2();
                MemoryAddress.Resize(ref operation->buffer, byteCapacity);
                operation->byteCapacity = byteCapacity;
            }

            operation->buffer.Write(byteLength, (byte)InstructionType.SetArrayElements);
            operation->buffer.Write(byteLength + 1, operation->world.world->schema.GetArrayType<T>());
            operation->buffer.Write(byteLength + 5, sizeof(T));
            operation->buffer.Write(byteLength + 9, 0);
            operation->buffer.Write(byteLength + 13, values.Length);
            operation->buffer.Write(byteLength + 17, values);
            operation->byteLength = newByteLength;
            operation->instructionCount++;
        }

        /// <summary>
        /// Updates the elements of an existing array.
        /// </summary>
        public readonly void SetArrayElements<T>(Span<T> values) where T : unmanaged
        {
            SetArrayElements((ReadOnlySpan<T>)values);
        }

        /// <summary>
        /// Updates the elements of an existing array.
        /// </summary>
        public readonly void SetArrayElements<T>(ReadOnlySpan<T> values, int arrayType) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(operation);
            ThrowIfSelectionIsEmpty();

            int byteCapacity = operation->byteCapacity;
            int byteLength = operation->byteLength;
            int newByteLength = byteLength + 17 + (sizeof(T) * values.Length);
            if (byteCapacity <= newByteLength)
            {
                byteCapacity = newByteLength.GetNextPowerOf2();
                MemoryAddress.Resize(ref operation->buffer, byteCapacity);
                operation->byteCapacity = byteCapacity;
            }

            operation->buffer.Write(byteLength, (byte)InstructionType.SetArrayElements);
            operation->buffer.Write(byteLength + 1, arrayType);
            operation->buffer.Write(byteLength + 5, sizeof(T));
            operation->buffer.Write(byteLength + 9, 0);
            operation->buffer.Write(byteLength + 13, values.Length);
            operation->buffer.Write(byteLength + 17, values);
            operation->byteLength = newByteLength;
            operation->instructionCount++;
        }

        /// <summary>
        /// Updates the elements of an existing array.
        /// </summary>
        public readonly void SetArrayElements<T>(Span<T> values, int arrayType) where T : unmanaged
        {
            SetArrayElements((ReadOnlySpan<T>)values, arrayType);
        }

        /// <summary>
        /// Updates the elements of an existing array starting at <paramref name="index"/>.
        /// </summary>
        public readonly void SetArrayElements<T>(int index, ReadOnlySpan<T> values) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(operation);
            ThrowIfSelectionIsEmpty();

            int byteCapacity = operation->byteCapacity;
            int byteLength = operation->byteLength;
            int newByteLength = byteLength + 17 + (sizeof(T) * values.Length);
            if (byteCapacity <= newByteLength)
            {
                byteCapacity = newByteLength.GetNextPowerOf2();
                MemoryAddress.Resize(ref operation->buffer, byteCapacity);
                operation->byteCapacity = byteCapacity;
            }

            operation->buffer.Write(byteLength, (byte)InstructionType.SetArrayElements);
            operation->buffer.Write(byteLength + 1, operation->world.world->schema.GetArrayType<T>());
            operation->buffer.Write(byteLength + 5, sizeof(T));
            operation->buffer.Write(byteLength + 9, index);
            operation->buffer.Write(byteLength + 13, values.Length);
            operation->buffer.Write(byteLength + 17, values);
            operation->byteLength = newByteLength;
            operation->instructionCount++;
        }

        /// <summary>
        /// Updates the elements of an existing array starting at <paramref name="index"/>.
        /// </summary>
        public readonly void SetArrayElements<T>(int index, Span<T> values) where T : unmanaged
        {
            SetArrayElements(index, (ReadOnlySpan<T>)values);
        }

        /// <summary>
        /// Updates the elements of an existing array starting at <paramref name="index"/>.
        /// </summary>
        public readonly void SetArrayElements<T>(int index, ReadOnlySpan<T> values, int arrayType) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(operation);
            ThrowIfSelectionIsEmpty();

            int byteCapacity = operation->byteCapacity;
            int byteLength = operation->byteLength;
            int newByteLength = byteLength + 17 + (sizeof(T) * values.Length);
            if (byteCapacity <= newByteLength)
            {
                byteCapacity = newByteLength.GetNextPowerOf2();
                MemoryAddress.Resize(ref operation->buffer, byteCapacity);
                operation->byteCapacity = byteCapacity;
            }

            operation->buffer.Write(byteLength, (byte)InstructionType.SetArrayElements);
            operation->buffer.Write(byteLength + 1, arrayType);
            operation->buffer.Write(byteLength + 5, sizeof(T));
            operation->buffer.Write(byteLength + 9, index);
            operation->buffer.Write(byteLength + 13, values.Length);
            operation->buffer.Write(byteLength + 17, values);
            operation->byteLength = newByteLength;
            operation->instructionCount++;
        }

        /// <summary>
        /// Updates the elements of an existing array starting at <paramref name="index"/>.
        /// </summary>
        public readonly void SetArrayElements<T>(int index, Span<T> values, int arrayType) where T : unmanaged
        {
            SetArrayElements(index, (ReadOnlySpan<T>)values, arrayType);
        }

        /// <summary>
        /// Updates and resizes the array to match the given <paramref name="values"/> exactly.
        /// </summary>
        public readonly void SetArray<T>(ReadOnlySpan<T> values) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(operation);
            ThrowIfSelectionIsEmpty();

            int byteCapacity = operation->byteCapacity;
            int byteLength = operation->byteLength;
            int newByteLength = byteLength + 13 + (sizeof(T) * values.Length);
            if (byteCapacity <= newByteLength)
            {
                byteCapacity = newByteLength.GetNextPowerOf2();
                MemoryAddress.Resize(ref operation->buffer, byteCapacity);
                operation->byteCapacity = byteCapacity;
            }

            operation->buffer.Write(byteLength, (byte)InstructionType.SetArray);
            operation->buffer.Write(byteLength + 1, operation->world.world->schema.GetArrayType<T>());
            operation->buffer.Write(byteLength + 5, sizeof(T));
            operation->buffer.Write(byteLength + 9, values.Length);
            operation->buffer.Write(byteLength + 13, values);
            operation->byteLength = newByteLength;
            operation->instructionCount++;
        }

        /// <summary>
        /// Updates and resizes the array to match the given <paramref name="values"/> exactly.
        /// </summary>
        public readonly void SetArray<T>(Span<T> values) where T : unmanaged
        {
            SetArray((ReadOnlySpan<T>)values);
        }

        /// <summary>
        /// Updates and resizes the array to match the given <paramref name="values"/> exactly.
        /// </summary>
        public readonly void SetArray<T>(ReadOnlySpan<T> values, int arrayType) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(operation);
            ThrowIfSelectionIsEmpty();

            int byteCapacity = operation->byteCapacity;
            int byteLength = operation->byteLength;
            int newByteLength = byteLength + 13 + (sizeof(T) * values.Length);
            if (byteCapacity <= newByteLength)
            {
                byteCapacity = newByteLength.GetNextPowerOf2();
                MemoryAddress.Resize(ref operation->buffer, byteCapacity);
                operation->byteCapacity = byteCapacity;
            }

            operation->buffer.Write(byteLength, (byte)InstructionType.SetArray);
            operation->buffer.Write(byteLength + 1, arrayType);
            operation->buffer.Write(byteLength + 5, sizeof(T));
            operation->buffer.Write(byteLength + 9, values.Length);
            operation->buffer.Write(byteLength + 13, values);
            operation->byteLength = newByteLength;
            operation->instructionCount++;
        }

        /// <summary>
        /// Updates and resizes the array to match the given <paramref name="values"/> exactly.
        /// </summary>
        public readonly void SetArray<T>(Span<T> values, int arrayType) where T : unmanaged
        {
            SetArray((ReadOnlySpan<T>)values, arrayType);
        }

        /// <summary>
        /// Creates a new array with the given <paramref name="values"/>, or updates and resizes an existing one
        /// to match exactly.
        /// </summary>
        public readonly void CreateOrSetArray<T>(ReadOnlySpan<T> values) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(operation);
            ThrowIfSelectionIsEmpty();

            int byteCapacity = operation->byteCapacity;
            int byteLength = operation->byteLength;
            int newByteLength = byteLength + 13 + (sizeof(T) * values.Length);
            if (byteCapacity <= newByteLength)
            {
                byteCapacity = newByteLength.GetNextPowerOf2();
                MemoryAddress.Resize(ref operation->buffer, byteCapacity);
                operation->byteCapacity = byteCapacity;
            }

            operation->buffer.Write(byteLength, (byte)InstructionType.CreateOrSetArray);
            operation->buffer.Write(byteLength + 1, operation->world.world->schema.GetArrayType<T>());
            operation->buffer.Write(byteLength + 5, sizeof(T));
            operation->buffer.Write(byteLength + 9, values.Length);
            operation->buffer.Write(byteLength + 13, values);
            operation->byteLength = newByteLength;
            operation->instructionCount++;
        }

        /// <summary>
        /// Creates a new array with the given <paramref name="values"/>, or updates and resizes an existing one
        /// to match exactly.
        /// </summary>
        public readonly void CreateOrSetArray<T>(Span<T> values) where T : unmanaged
        {
            CreateOrSetArray((ReadOnlySpan<T>)values);
        }

        /// <summary>
        /// Creates a new array with the given <paramref name="values"/>, or updates and resizes an existing one
        /// to match exactly.
        /// </summary>
        public readonly void CreateOrSetArray<T>(ReadOnlySpan<T> values, int arrayType) where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(operation);
            ThrowIfSelectionIsEmpty();

            int byteCapacity = operation->byteCapacity;
            int byteLength = operation->byteLength;
            int newByteLength = byteLength + 13 + (sizeof(T) * values.Length);
            if (byteCapacity <= newByteLength)
            {
                byteCapacity = newByteLength.GetNextPowerOf2();
                MemoryAddress.Resize(ref operation->buffer, byteCapacity);
                operation->byteCapacity = byteCapacity;
            }

            operation->buffer.Write(byteLength, (byte)InstructionType.CreateOrSetArray);
            operation->buffer.Write(byteLength + 1, arrayType);
            operation->buffer.Write(byteLength + 5, sizeof(T));
            operation->buffer.Write(byteLength + 9, values.Length);
            operation->buffer.Write(byteLength + 13, values);
            operation->byteLength = newByteLength;
            operation->instructionCount++;
        }

        /// <summary>
        /// Creates a new array with the given <paramref name="values"/>, or updates and resizes an existing one
        /// to match exactly.
        /// </summary>
        public readonly void CreateOrSetArray<T>(Span<T> values, int arrayType) where T : unmanaged
        {
            CreateOrSetArray((ReadOnlySpan<T>)values, arrayType);
        }

        /// <summary>
        /// Adds a tag of type <typeparamref name="T"/> to selected entities.
        /// </summary>
        public readonly void AddTag<T>() where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(operation);
            ThrowIfSelectionIsEmpty();

            int byteLength = operation->byteLength;
            if (operation->byteCapacity <= byteLength + 5)
            {
                MemoryAddress.ResizePowerOf2(ref operation->buffer, operation->byteCapacity);
                operation->byteCapacity *= 2;
            }

            operation->buffer.Write(byteLength, (byte)InstructionType.AddTag);
            operation->buffer.Write(byteLength + 1, operation->world.world->schema.GetTagType<T>());
            operation->byteLength = byteLength + 5;
            operation->instructionCount++;
        }

        /// <summary>
        /// Adds the <paramref name="tagType"/> to selected entities.
        /// </summary>
        public readonly void AddTag(int tagType)
        {
            MemoryAddress.ThrowIfDefault(operation);
            ThrowIfSelectionIsEmpty();

            int byteLength = operation->byteLength;
            if (operation->byteCapacity <= byteLength + 5)
            {
                MemoryAddress.ResizePowerOf2(ref operation->buffer, operation->byteCapacity);
                operation->byteCapacity *= 2;
            }

            operation->buffer.Write(byteLength, (byte)InstructionType.AddTag);
            operation->buffer.Write(byteLength + 1, tagType);
            operation->byteLength = byteLength + 5;
            operation->instructionCount++;
        }

        /// <summary>
        /// Removes the tag of type <typeparamref name="T"/> from the selected entities.
        /// </summary>
        public readonly void RemoveTag<T>() where T : unmanaged
        {
            MemoryAddress.ThrowIfDefault(operation);
            ThrowIfSelectionIsEmpty();

            int byteLength = operation->byteLength;
            if (operation->byteCapacity <= byteLength + 5)
            {
                MemoryAddress.ResizePowerOf2(ref operation->buffer, operation->byteCapacity);
                operation->byteCapacity *= 2;
            }

            operation->buffer.Write(byteLength, (byte)InstructionType.RemoveTag);
            operation->buffer.Write(byteLength + 1, operation->world.world->schema.GetTagType<T>());
            operation->byteLength = byteLength + 5;
            operation->instructionCount++;
        }

        /// <summary>
        /// Removes the <paramref name="tagType"/> from the selected entities.
        /// </summary>
        public readonly void RemoveTag(int tagType)
        {
            MemoryAddress.ThrowIfDefault(operation);
            ThrowIfSelectionIsEmpty();

            int byteLength = operation->byteLength;
            if (operation->byteCapacity <= byteLength + 5)
            {
                MemoryAddress.ResizePowerOf2(ref operation->buffer, operation->byteCapacity);
                operation->byteCapacity *= 2;
            }

            operation->buffer.Write(byteLength, (byte)InstructionType.RemoveTag);
            operation->buffer.Write(byteLength + 1, tagType);
            operation->byteLength = byteLength + 5;
            operation->instructionCount++;
        }

        /// <summary>
        /// Adds the given <paramref name="entity"/> to the selection.
        /// </summary>
        public readonly void AppendEntityToSelection(uint entity)
        {
            MemoryAddress.ThrowIfDefault(operation);
            operation->world.ThrowIfEntityIsMissing(entity);

            int byteLength = operation->byteLength;
            if (operation->byteCapacity <= byteLength + 5)
            {
                MemoryAddress.ResizePowerOf2(ref operation->buffer, operation->byteCapacity);
                operation->byteCapacity *= 2;
            }

            operation->buffer.Write(byteLength, (byte)InstructionType.AppendEntityToSelection);
            operation->buffer.Write(byteLength + 1, entity);
            operation->byteLength = byteLength + 5;
            operation->instructionCount++;
            operation->selectedCount++;
        }

        /// <summary>
        /// Adds the given <paramref name="entity"/> to the selection.
        /// </summary>
        public readonly void AppendEntityToSelection<T>(T entity) where T : unmanaged, IEntity
        {
            AppendEntityToSelection(entity.GetEntityValue());
        }

        /// <summary>
        /// Sets <paramref name="entity"/> as the only selected entity.
        /// </summary>
        public readonly void SetSelectedEntity(uint entity)
        {
            MemoryAddress.ThrowIfDefault(operation);
            operation->world.ThrowIfEntityIsMissing(entity);

            int byteLength = operation->byteLength;
            if (operation->byteCapacity <= byteLength + 5)
            {
                MemoryAddress.ResizePowerOf2(ref operation->buffer, operation->byteCapacity);
                operation->byteCapacity *= 2;
            }

            operation->buffer.Write(byteLength, (byte)InstructionType.SetSelectedEntity);
            operation->buffer.Write(byteLength + 1, entity);
            operation->byteLength = byteLength + 5;
            operation->instructionCount++;
            operation->selectedCount = 1;
        }

        /// <summary>
        /// Sets <paramref name="entity"/> as the only selected entity.
        /// </summary>
        public readonly void SetSelectedEntity<T>(T entity) where T : unmanaged, IEntity
        {
            SetSelectedEntity(entity.GetEntityValue());
        }

        /// <summary>
        /// Adds the given <paramref name="entities"/> to selection.
        /// </summary>
        public readonly void AppendMultipleEntitiesToSelection(ReadOnlySpan<uint> entities)
        {
            MemoryAddress.ThrowIfDefault(operation);

            int selectLength = entities.Length;
            int byteCapacity = operation->byteCapacity;
            int byteLength = operation->byteLength;
            int newByteLength = byteLength + 5 + (sizeof(uint) * selectLength);
            if (byteCapacity <= newByteLength)
            {
                byteCapacity = newByteLength.GetNextPowerOf2();
                MemoryAddress.Resize(ref operation->buffer, byteCapacity);
                operation->byteCapacity = byteCapacity;
            }

            operation->buffer.Write(byteLength, (byte)InstructionType.AppendMultipleEntitiesToSelection);
            operation->buffer.Write(byteLength + 1, selectLength);
            operation->buffer.Write(byteLength + 5, entities);
            operation->byteLength = newByteLength;
            operation->instructionCount++;
            operation->selectedCount += selectLength;
        }

        /// <summary>
        /// Adds the given <paramref name="entities"/> to selection.
        /// </summary>
        public readonly void AppendMultipleEntitiesToSelection(Span<uint> entities)
        {
            AppendMultipleEntitiesToSelection((ReadOnlySpan<uint>)entities);
        }

        /// <summary>
        /// Selects the entity that was created <paramref name="createInstructionsAgo"/>.
        /// </summary>
        public readonly void SelectPreviouslyCreatedEntity(uint createInstructionsAgo)
        {
            MemoryAddress.ThrowIfDefault(operation);

            int byteLength = operation->byteLength;
            if (operation->byteCapacity <= byteLength + 5)
            {
                MemoryAddress.ResizePowerOf2(ref operation->buffer, operation->byteCapacity);
                operation->byteCapacity *= 2;
            }

            operation->buffer.Write(byteLength, (byte)InstructionType.AppendPreviouslyCreatedEntityToSelection);
            operation->buffer.Write(byteLength + 1, createInstructionsAgo);
            operation->byteLength = byteLength + 5;
            operation->instructionCount++;
            operation->selectedCount++;
        }

        /// <summary>
        /// Clears the selection.
        /// </summary>
        public readonly void ClearSelection()
        {
            MemoryAddress.ThrowIfDefault(operation);

            int byteLength = operation->byteLength;
            if (operation->byteCapacity == byteLength)
            {
                MemoryAddress.ResizePowerOf2(ref operation->buffer, operation->byteCapacity);
                operation->byteCapacity *= 2;
            }

            operation->buffer.Write(byteLength, (byte)InstructionType.ClearSelection);
            operation->byteLength = byteLength + 1;
            operation->instructionCount++;
            operation->selectedCount = 0;
        }

        /// <summary>
        /// Removes an existing <paramref name="reference"/> from all selected entities.
        /// </summary>
        public readonly void RemoveReference(rint reference)
        {
            MemoryAddress.ThrowIfDefault(operation);
            ThrowIfSelectionIsEmpty();

            int byteLength = operation->byteLength;
            if (operation->byteCapacity <= byteLength + 5)
            {
                MemoryAddress.ResizePowerOf2(ref operation->buffer, operation->byteCapacity);
                operation->byteCapacity *= 2;
            }

            operation->buffer.Write(byteLength, (byte)InstructionType.RemoveReference);
            operation->buffer.Write(byteLength + 1, reference);
            operation->byteLength = byteLength + 5;
            operation->instructionCount++;
        }

        /// <summary>
        /// For all entities in the selection, assigns the parent to the entity
        /// that was created <paramref name="createInstructionsAgo"/>.
        /// </summary>
        public readonly void SetParentToPreviouslyCreatedEntity(int createInstructionsAgo)
        {
            MemoryAddress.ThrowIfDefault(operation);
            ThrowIfSelectionIsEmpty();

            int byteLength = operation->byteLength;
            if (operation->byteCapacity <= byteLength + 5)
            {
                MemoryAddress.ResizePowerOf2(ref operation->buffer, operation->byteCapacity);
                operation->byteCapacity *= 2;
            }

            operation->buffer.Write(byteLength, (byte)InstructionType.SetParentToPreviouslyCreatedEntity);
            operation->buffer.Write(byteLength + 1, createInstructionsAgo);
            operation->byteLength = byteLength + 5;
            operation->instructionCount++;
        }

        /// <summary>
        /// For all entities in the selection, adds the entity that was created <paramref name="createInstructionsAgo"/>.
        /// </summary>
        public readonly void AddReferenceTowardsPreviouslyCreatedEntity(int createInstructionsAgo)
        {
            MemoryAddress.ThrowIfDefault(operation);
            ThrowIfSelectionIsEmpty();

            int byteLength = operation->byteLength;
            if (operation->byteCapacity <= byteLength + 5)
            {
                MemoryAddress.ResizePowerOf2(ref operation->buffer, operation->byteCapacity);
                operation->byteCapacity *= 2;
            }

            operation->buffer.Write(byteLength, (byte)InstructionType.AddReferenceToPreviouslyCreatedEntity);
            operation->buffer.Write(byteLength + 1, createInstructionsAgo);
            operation->byteLength = byteLength + 5;
            operation->instructionCount++;
        }

        /// <summary>
        /// Performs all recorded instructions.
        /// </summary>
        public readonly void Perform()
        {
            MemoryAddress.ThrowIfDefault(operation);

            operation->selection.Clear();
            operation->history.Clear();
            new Performing(operation).Do();
        }

        /// <summary>
        /// Performs all recorded instructions.
        /// </summary>
        /// <returns><see langword="true"/> if instructions were performed.</returns>
        public readonly bool TryPerform()
        {
            MemoryAddress.ThrowIfDefault(operation);

            if (operation->instructionCount > 0)
            {
                operation->selection.Clear();
                operation->history.Clear();
                new Performing(operation).Do();
                return true;
            }
            else
            {
                return false;
            }
        }

        private ref struct Performing
        {
            private readonly Implementation* operation;
            private int bytePosition;

            public Performing(Implementation* operation)
            {
                this.operation = operation;
                bytePosition = 0;
            }

            private void CreateEntities()
            {
                int count = operation->buffer.Read<int>(bytePosition);
                Span<uint> entities = stackalloc uint[count];
                operation->world.CreateEntities(entities);
                operation->history.AddRange(entities);
                bytePosition += 4;
            }

            private void CreateMultipleEntitiesAndSelect()
            {
                int count = operation->buffer.Read<int>(bytePosition);
                Span<uint> entities = stackalloc uint[count];
                operation->world.CreateEntities(entities);
                operation->history.AddRange(entities);
                operation->selection.AddRange(entities);
                bytePosition += 4;
            }

            private readonly void DestroySelectedEntities()
            {
                ReadOnlySpan<uint> selection = operation->selection.AsSpan();
                for (int i = 0; i < selection.Length; i++)
                {
                    uint entity = selection[i];
                    operation->world.DestroyEntity(entity);
                    operation->history.TryRemove(entity);
                }

                operation->selection.Clear();
            }

            private void SelectEntities()
            {
                int count = operation->buffer.Read<int>(bytePosition);
                bytePosition += 4;
                ReadOnlySpan<uint> entities = new(operation->buffer.Pointer + bytePosition, count);
                bytePosition += count * sizeof(uint);
                operation->selection.AddRange(entities);
            }

            private void AppendEntityToSelection()
            {
                uint entity = operation->buffer.Read<uint>(bytePosition);
                operation->selection.Add(entity);
                bytePosition += 4;
            }

            private void SetSelectedEntity()
            {
                uint entity = operation->buffer.Read<uint>(bytePosition);
                operation->selection.Clear();
                operation->selection.Add(entity);
                bytePosition += 4;
            }

            private void SetParent()
            {
                uint parent = operation->buffer.Read<uint>(bytePosition);
                bytePosition += 4;
                ReadOnlySpan<uint> selection = operation->selection.AsSpan();
                for (int i = 0; i < selection.Length; i++)
                {
                    operation->world.SetParent(selection[i], parent);
                }
            }

            private readonly void EnableEntities()
            {
                ReadOnlySpan<uint> selection = operation->selection.AsSpan();
                for (int i = 0; i < selection.Length; i++)
                {
                    operation->world.SetEnabled(selection[i], true);
                }
            }

            private readonly void DisableEntities()
            {
                ReadOnlySpan<uint> selection = operation->selection.AsSpan();
                for (int i = 0; i < selection.Length; i++)
                {
                    operation->world.SetEnabled(selection[i], false);
                }
            }

            private void AddComponent()
            {
                Span<Slot> slots = operation->world.world->slots.AsSpan();
                int componentType = operation->buffer.Read<int>(bytePosition);
                int componentSize = operation->buffer.Read<int>(bytePosition + 4);
                bytePosition += 8;
                ReadOnlySpan<byte> componentBytes = operation->buffer.AsSpan(bytePosition, componentSize);
                bytePosition += componentSize;
                ReadOnlySpan<uint> selection = operation->selection.AsSpan();
                for (int i = 0; i < selection.Length; i++)
                {
                    //same as World.AddComponentBytes
                    uint entity = selection[i];
                    ref Slot slot = ref slots[(int)entity];
                    Definition definition = slot.chunk.chunk->definition;
                    definition.AddComponentType(componentType);
                    Chunk destinationChunk = operation->world.world->chunks.GetOrCreate(definition);
                    World.MoveEntityTo(slots, entity, ref slot, destinationChunk);
                    operation->world.NotifyComponentAdded(entity, componentType);
                    unchecked
                    {
                        Span<byte> component = new(slot.row.Pointer + operation->world.world->schema.schema->componentOffsets[(uint)componentType], componentBytes.Length);
                        componentBytes.CopyTo(component);
                    }
                }
            }

            private void AddComponentType()
            {
                Span<Slot> slots = operation->world.world->slots.AsSpan();
                int componentType = operation->buffer.Read<int>(bytePosition);
                bytePosition += 4;
                ReadOnlySpan<uint> selection = operation->selection.AsSpan();
                for (int i = 0; i < selection.Length; i++)
                {
                    //same as World.AddComponentType
                    uint entity = selection[i];
                    ref Slot slot = ref slots[(int)entity];
                    Definition definition = slot.chunk.chunk->definition;
                    definition.AddComponentType(componentType);
                    Chunk destinationChunk = operation->world.world->chunks.GetOrCreate(definition);
                    World.MoveEntityTo(slots, entity, ref slot, destinationChunk);
                    operation->world.NotifyComponentAdded(entity, componentType);
                }
            }

            private void TryAddComponentType()
            {
                Span<Slot> slots = operation->world.world->slots.AsSpan();
                int componentType = operation->buffer.Read<int>(bytePosition);
                bytePosition += 4;
                ReadOnlySpan<uint> selection = operation->selection.AsSpan();
                for (int i = 0; i < selection.Length; i++)
                {
                    uint entity = selection[i];
                    ref Slot slot = ref slots[(int)entity];
                    Definition definition = slot.chunk.chunk->definition;
                    if (!definition.componentTypes.Contains(componentType))
                    {
                        definition.AddComponentType(componentType);
                        Chunk destinationChunk = operation->world.world->chunks.GetOrCreate(definition);
                        World.MoveEntityTo(slots, entity, ref slot, destinationChunk);
                        operation->world.NotifyComponentAdded(entity, componentType);
                    }
                }
            }

            private void SetComponent()
            {
                Span<Slot> slots = operation->world.world->slots.AsSpan();
                int componentType = operation->buffer.Read<int>(bytePosition);
                int componentSize = operation->buffer.Read<int>(bytePosition + 4);
                bytePosition += 8;
                ReadOnlySpan<byte> componentBytes = operation->buffer.AsSpan(bytePosition, componentSize);
                bytePosition += componentSize;
                ReadOnlySpan<uint> selection = operation->selection.AsSpan();
                for (int i = 0; i < selection.Length; i++)
                {
                    //same as World.SetComponentBytes
                    unchecked
                    {
                        uint entity = selection[i];
                        Span<byte> component = new(slots[(int)entity].row.Pointer + operation->world.world->schema.schema->componentOffsets[(uint)componentType], componentBytes.Length);
                        componentBytes.CopyTo(component);
                    }
                }
            }

            private void AddOrSetComponent()
            {
                Span<Slot> slots = operation->world.world->slots.AsSpan();
                int componentType = operation->buffer.Read<int>(bytePosition);
                int componentSize = operation->buffer.Read<int>(bytePosition + 4);
                bytePosition += 8;
                ReadOnlySpan<byte> componentBytes = operation->buffer.AsSpan(bytePosition, componentSize);
                bytePosition += componentSize;
                ReadOnlySpan<uint> selection = operation->selection.AsSpan();
                for (int i = 0; i < selection.Length; i++)
                {
                    uint entity = selection[i];
                    ref Slot slot = ref slots[(int)entity];
                    if (slot.chunk.chunk->definition.componentTypes.Contains(componentType))
                    {
                        //same as World.SetComponentBytes
                        unchecked
                        {
                            Span<byte> component = new(slots[(int)entity].row.Pointer + operation->world.world->schema.schema->componentOffsets[(uint)componentType], componentBytes.Length);
                            componentBytes.CopyTo(component);
                        }
                    }
                    else
                    {
                        //same as World.AddComponentBytes
                        Definition definition = slot.chunk.chunk->definition;
                        definition.AddComponentType(componentType);
                        Chunk destinationChunk = operation->world.world->chunks.GetOrCreate(definition);
                        World.MoveEntityTo(slots, entity, ref slot, destinationChunk);
                        operation->world.NotifyComponentAdded(entity, componentType);
                        unchecked
                        {
                            Span<byte> component = new(slot.row.Pointer + operation->world.world->schema.schema->componentOffsets[(uint)componentType], componentBytes.Length);
                            componentBytes.CopyTo(component);
                        }
                    }
                }
            }

            private void RemoveComponentType()
            {
                Span<Slot> slots = operation->world.world->slots.AsSpan();
                int componentType = operation->buffer.Read<int>(bytePosition);
                bytePosition += 4;
                ReadOnlySpan<uint> selection = operation->selection.AsSpan();
                for (int i = 0; i < selection.Length; i++)
                {
                    //same as World.RemoveComponentType
                    uint entity = selection[i];
                    ref Slot slot = ref slots[(int)entity];
                    Definition definition = slot.chunk.chunk->definition;
                    definition.RemoveComponentType(componentType);
                    Chunk destinationChunk = operation->world.world->chunks.GetOrCreate(definition);
                    World.MoveEntityTo(slots, entity, ref slot, destinationChunk);
                    operation->world.NotifyComponentRemoved(entity, componentType);
                }
            }

            private void RemoveReference()
            {
                rint reference = operation->buffer.Read<rint>(bytePosition);
                bytePosition += sizeof(rint);
                ReadOnlySpan<uint> selection = operation->selection.AsSpan();
                for (int i = 0; i < selection.Length; i++)
                {
                    operation->world.RemoveReference(selection[i], reference);
                }
            }

            private void CreateArray()
            {
                int arrayType = operation->buffer.Read<int>(bytePosition);
                int arrayLength = operation->buffer.Read<int>(bytePosition + 4);
                bytePosition += 8;
                ReadOnlySpan<uint> selection = operation->selection.AsSpan();
                for (int i = 0; i < selection.Length; i++)
                {
                    operation->world.CreateArray(selection[i], arrayType, arrayLength);
                }
            }

            private void CreateAndInitializeArray()
            {
                int arrayType = operation->buffer.Read<int>(bytePosition);
                int arrayStride = operation->buffer.Read<int>(bytePosition + 4);
                int arrayLength = operation->buffer.Read<int>(bytePosition + 8);
                bytePosition += 12;
                ReadOnlySpan<uint> selection = operation->selection.AsSpan();
                if (arrayLength > 0)
                {
                    ReadOnlySpan<byte> bytes = operation->buffer.AsSpan(bytePosition, arrayLength * arrayStride);
                    bytePosition += arrayLength * arrayStride;
                    for (int i = 0; i < selection.Length; i++)
                    {
                        Values array = operation->world.CreateArray(selection[i], arrayType, arrayLength);
                        array.CopyFrom(bytes);
                    }
                }
                else
                {
                    for (int i = 0; i < selection.Length; i++)
                    {
                        operation->world.CreateArray(selection[i], arrayType);
                    }
                }
            }

            private void ResizeArray()
            {
                int arrayType = operation->buffer.Read<int>(bytePosition);
                int arrayLength = operation->buffer.Read<int>(bytePosition + 4);
                bytePosition += 8;
                ReadOnlySpan<uint> selection = operation->selection.AsSpan();
                for (int i = 0; i < selection.Length; i++)
                {
                    Values array = operation->world.GetArray(selection[i], arrayType);
                    array.Length = arrayLength;
                }
            }

            private void SetArrayElement()
            {
                int arrayType = operation->buffer.Read<int>(bytePosition);
                int arrayStride = operation->buffer.Read<int>(bytePosition + 4);
                int index = operation->buffer.Read<int>(bytePosition + 8);
                bytePosition += 12;
                ReadOnlySpan<byte> bytes = operation->buffer.AsSpan(bytePosition, arrayStride);
                bytePosition += arrayStride;
                ReadOnlySpan<uint> selection = operation->selection.AsSpan();
                for (int i = 0; i < selection.Length; i++)
                {
                    Values array = operation->world.GetArray(selection[i], arrayType);
                    MemoryAddress element = array[index];
                    element.CopyFrom(bytes);
                }
            }

            private void SetArrayElements()
            {
                int arrayType = operation->buffer.Read<int>(bytePosition);
                int arrayStride = operation->buffer.Read<int>(bytePosition + 4);
                int index = operation->buffer.Read<int>(bytePosition + 8);
                int arrayLength = operation->buffer.Read<int>(bytePosition + 12);
                bytePosition += 16;
                ReadOnlySpan<byte> bytes = operation->buffer.AsSpan(bytePosition, arrayLength * arrayStride);
                bytePosition += arrayLength * arrayStride;
                ReadOnlySpan<uint> selection = operation->selection.AsSpan();
                for (int i = 0; i < selection.Length; i++)
                {
                    Values array = operation->world.GetArray(selection[i], arrayType);
                    MemoryAddress element = array.Read(index * arrayStride);
                    element.CopyFrom(bytes);
                }
            }

            private void SetArray()
            {
                int arrayType = operation->buffer.Read<int>(bytePosition);
                int arrayStride = operation->buffer.Read<int>(bytePosition + 4);
                int arrayLength = operation->buffer.Read<int>(bytePosition + 8);
                bytePosition += 12;
                ReadOnlySpan<uint> selection = operation->selection.AsSpan();
                if (arrayLength > 0)
                {
                    ReadOnlySpan<byte> bytes = operation->buffer.AsSpan(bytePosition, arrayLength * arrayStride);
                    bytePosition += arrayLength * arrayStride;
                    for (int i = 0; i < selection.Length; i++)
                    {
                        uint entity = selection[i];
                        Values array = operation->world.GetArray(entity, arrayType);
                        array.CopyFrom(bytes);
                    }
                }
                else
                {
                    for (int i = 0; i < selection.Length; i++)
                    {
                        uint entity = selection[i];
                        Values array = operation->world.GetArray(entity, arrayType);
                        array.Length = 0;
                    }
                }
            }

            private void CreateOrSetArray()
            {
                int arrayType = operation->buffer.Read<int>(bytePosition);
                int arrayLength = operation->buffer.Read<int>(bytePosition + 4);
                int arrayStride = operation->buffer.Read<int>(bytePosition + 8);
                bytePosition += 12;
                ReadOnlySpan<byte> bytes = operation->buffer.AsSpan(bytePosition, arrayLength * arrayStride);
                bytePosition += arrayLength * arrayStride;
                ReadOnlySpan<uint> selection = operation->selection.AsSpan();
                for (int i = 0; i < selection.Length; i++)
                {
                    uint entity = selection[i];
                    Values array;
                    if (operation->world.ContainsArray(entity, arrayType))
                    {
                        array = operation->world.GetArray(entity, arrayType);
                    }
                    else
                    {
                        array = operation->world.CreateArray(entity, arrayType, arrayLength);
                    }

                    //todo: this copy from operation does a resize if length doesnt match, this is duplicate work
                    //in the case of array creation
                    array.CopyFrom(bytes);
                }
            }

            private void AddTag()
            {
                int tagType = operation->buffer.Read<int>(bytePosition);
                bytePosition += 4;
                ReadOnlySpan<uint> selection = operation->selection.AsSpan();
                for (int i = 0; i < selection.Length; i++)
                {
                    operation->world.AddTag(selection[i], tagType);
                }
            }

            private void RemoveTag()
            {
                int tagType = operation->buffer.Read<int>(bytePosition);
                bytePosition += 4;
                ReadOnlySpan<uint> selection = operation->selection.AsSpan();
                for (int i = 0; i < selection.Length; i++)
                {
                    operation->world.RemoveTag(selection[i], tagType);
                }
            }

            private void SetParentToPreviouslyCreatedEntity()
            {
                int entitiesAgo = operation->buffer.Read<int>(bytePosition);
                bytePosition += 4;
                uint parent = operation->history[operation->history.Count - entitiesAgo - 1];
                ReadOnlySpan<uint> selection = operation->selection.AsSpan();
                for (int i = 0; i < selection.Length; i++)
                {
                    operation->world.SetParent(selection[i], parent);
                }
            }

            private void SelectPreviouslyCreatedEntity()
            {
                int entitiesAgo = operation->buffer.Read<int>(bytePosition);
                bytePosition += 4;
                operation->selection.Add(operation->history[operation->history.Count - entitiesAgo - 1]);
            }

            private void AddReferenceToPreviouslyCreatedEntity()
            {
                int entitiesAgo = operation->buffer.Read<int>(bytePosition);
                bytePosition += 4;
                uint referencedEntity = operation->history[operation->history.Count - entitiesAgo - 1];
                ReadOnlySpan<uint> selection = operation->selection.AsSpan();
                for (int i = 0; i < selection.Length; i++)
                {
                    operation->world.AddReference(selection[i], referencedEntity);
                }
            }

            public void Do()
            {
                while (bytePosition < operation->byteLength)
                {
                    InstructionType type = (InstructionType)operation->buffer.Read<byte>(bytePosition);
                    bytePosition++;
                    switch (type)
                    {
                        case InstructionType.CreateSingleEntity:
                            operation->history.Add(operation->world.CreateEntity());
                            break;
                        case InstructionType.CreateSingleEntityAndSelect:
                            {
                                uint entity = operation->world.CreateEntity();
                                operation->history.Add(entity);
                                operation->selection.Add(entity);
                            }
                            break;
                        case InstructionType.CreateMultipleEntities:
                            CreateEntities();
                            break;
                        case InstructionType.CreateMultipleEntitiesAndSelect:
                            CreateMultipleEntitiesAndSelect();
                            break;
                        case InstructionType.DestroySelectedEntities:
                            DestroySelectedEntities();
                            break;
                        case InstructionType.AppendMultipleEntitiesToSelection:
                            SelectEntities();
                            break;
                        case InstructionType.SetSelectedEntity:
                            SetSelectedEntity();
                            break;
                        case InstructionType.AppendEntityToSelection:
                            AppendEntityToSelection();
                            break;
                        case InstructionType.ClearSelection:
                            operation->selection.Clear();
                            break;
                        case InstructionType.SetParent:
                            SetParent();
                            break;
                        case InstructionType.EnableSelectedEntities:
                            EnableEntities();
                            break;
                        case InstructionType.DisableSelectedEntities:
                            DisableEntities();
                            break;
                        case InstructionType.AddComponentType:
                            AddComponentType();
                            break;
                        case InstructionType.TryAddComponentType:
                            TryAddComponentType();
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
                        case InstructionType.RemoveComponentType:
                            RemoveComponentType();
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
                        case InstructionType.SetArrayElement:
                            SetArrayElement();
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
                        case InstructionType.AddTag:
                            AddTag();
                            break;
                        case InstructionType.RemoveTag:
                            RemoveTag();
                            break;
                        case InstructionType.SetParentToPreviouslyCreatedEntity:
                            SetParentToPreviouslyCreatedEntity();
                            break;
                        case InstructionType.AppendPreviouslyCreatedEntityToSelection:
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
            public World world;
            public int instructionCount;
            public int selectedCount;
            public int createdCount;
            public int byteLength;
            public int byteCapacity;
            public MemoryAddress buffer;
            public List<uint> history;
            public List<uint> selection;
        }
    }
}