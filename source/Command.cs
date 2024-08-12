using System;
using Unmanaged;

namespace Simulation
{
    /// <summary>
    /// Contains an instruction for working with <see cref="World"/>s.
    /// </summary>
    public struct Command : ISerializable
    {
        private CommandOperation operation;
        private ulong a;
        private ulong b;
        private ulong c;

        public readonly CommandOperation Operation => operation;
        public readonly ulong A => a;
        public readonly ulong B => b;
        public readonly ulong C => c;

        private Command(CommandOperation operation, ulong a, ulong b, ulong c)
        {
            this.operation = operation;
            this.a = a;
            this.b = b;
            this.c = c;
        }

        /// <summary>
        /// Creates an entity and makes it the only selection.
        /// </summary>
        public static Command CreateEntity()
        {
            Command command = new(CommandOperation.CreateEntity, 1, 0, 0);
            return command;
        }

        /// <summary>
        /// Creates multiple entities and makes that the selection.
        /// </summary>
        public static Command CreateEntity(uint count)
        {
            Command command = new(CommandOperation.CreateEntity, count, 0, 0);
            return command;
        }

        /// <summary>
        /// Destroys all selected entities.
        /// </summary>
        public static Command DestroySelection()
        {
            Command command = new(CommandOperation.DestroyEntities, 0, 0, 0);
            return command;
        }

        public static Command DestroyRange(uint start, uint count)
        {
            Command command = new(CommandOperation.DestroyEntities, start, count, 0);
            return command;
        }

        public static Command ClearSelection()
        {
            Command command = new(CommandOperation.ClearSelection, 0, 0, 0);
            return command;
        }

        /// <summary>
        /// Adds a previously created entity into the selection buffer.
        /// <para>
        /// Offset of 0 indicates the last entity created.
        /// </para>
        /// </summary>
        public static Command AddToSelection(uint relativeOffset)
        {
            Command command = new(CommandOperation.AddToSelection, 0, relativeOffset, 0);
            return command;
        }

        /// <summary>
        /// Adds an existing entity into the selection.
        /// </summary>
        public static Command AddToSelection(eint entity)
        {
            Command command = new(CommandOperation.AddToSelection, 1, entity, 0);
            return command;
        }

        /// <summary>
        /// Selects an entity at the relative index.
        /// </summary>
        public static Command SelectEntity(uint relativeOffset)
        {
            Command command = new(CommandOperation.SelectEntity, 0, relativeOffset, 0);
            return command;
        }

        /// <summary>
        /// Selects an existing entity.
        /// </summary>
        public static Command SelectEntity(eint entity)
        {
            Command command = new(CommandOperation.SelectEntity, 1, entity, 0);
            return command;
        }

        /// <summary>
        /// Assigns the parent of all entities in the selection to
        /// the entity at the relative index.
        /// </summary>
        public static Command SetParent(uint relativeOffset)
        {
            Command command = new(CommandOperation.SetParent, 0, relativeOffset, 0);
            return command;
        }

        /// <summary>
        /// Assigns the parent of all entities in the selection to
        /// the given existing entity.
        /// </summary>
        public static Command SetParent(eint entity)
        {
            Command command = new(CommandOperation.SetParent, 1, entity, 0);
            return command;
        }

        /// <summary>
        /// Adds the specified component type to all entities inside the selection.
        /// </summary>
        public static Command AddComponent<T>() where T : unmanaged
        {
            return AddComponent(new T());
        }

        /// <summary>
        /// Adds the given component to all entities inside the selection.
        /// </summary>
        public static Command AddComponent<T>(T component) where T : unmanaged
        {
            Allocation allocation = Allocation.Create(component);
            Command command = new(CommandOperation.AddComponent, RuntimeType.Get<T>().value, (ulong)allocation.Address, 0);
            return command;
        }

        public static Command AddComponent(RuntimeType componentType)
        {
            Allocation allocation = Allocation.Create(componentType.Size);
            Command command = new(CommandOperation.AddComponent, componentType.value, (ulong)allocation.Address, 0);
            return command;
        }

        public static Command AddComponent(RuntimeType componentType, ReadOnlySpan<byte> componentData)
        {
            Allocation allocation = Allocation.Create(componentData);
            Command command = new(CommandOperation.AddComponent, componentType.value, (ulong)allocation.Address, 0);
            return command;
        }

        public static Command RemoveComponent<T>() where T : unmanaged
        {
            return RemoveComponent(RuntimeType.Get<T>());
        }

        public static Command RemoveComponent(RuntimeType componentType)
        {
            Command command = new(CommandOperation.RemoveComponent, componentType.value, 0, 0);
            return command;
        }

        public static Command SetComponent<T>(T component) where T : unmanaged
        {
            Allocation allocation = Allocation.Create(component);
            Command command = new(CommandOperation.SetComponent, RuntimeType.Get<T>().value, (ulong)allocation.Address, 0);
            return command;
        }

        public static Command SetComponent(RuntimeType componentType, ReadOnlySpan<byte> componentData)
        {
            Allocation allocation = Allocation.Create(componentData);
            Command command = new(CommandOperation.SetComponent, componentType.value, (ulong)allocation.Address, 0);
            return command;
        }

        public static Command CreateList<T>() where T : unmanaged
        {
            Command command = new(CommandOperation.CreateList, RuntimeType.Get<T>().value, 0, 0);
            return command;
        }

        public static Command CreateList(RuntimeType elementType)
        {
            Command command = new(CommandOperation.CreateList, elementType.value, 0, 0);
            return command;
        }

        public static Command DestroyList<T>() where T : unmanaged
        {
            Command command = new(CommandOperation.DestroyList, RuntimeType.Get<T>().value, 0, 0);
            return command;
        }

        public static Command DestroyList(RuntimeType elementType)
        {
            Command command = new(CommandOperation.DestroyList, elementType.value, 0, 0);
            return command;
        }

        public static Command ClearList<T>() where T : unmanaged
        {
            Command command = new(CommandOperation.ClearList, RuntimeType.Get<T>().value, 0, 0);
            return command;
        }

        public static Command ClearCollection(RuntimeType elementType)
        {
            Command command = new(CommandOperation.ClearList, elementType.value, 0, 0);
            return command;
        }

        public static Command AddElement<T>(T element) where T : unmanaged
        {
            Allocation allocation = Allocation.Create(element);
            Command command = new(CommandOperation.InsertElement, RuntimeType.Get<T>().value, (ulong)allocation.Address, uint.MaxValue);
            return command;
        }

        public static Command InsertElement<T>(T element, uint index) where T : unmanaged
        {
            Allocation allocation = Allocation.Create(element);
            Command command = new(CommandOperation.InsertElement, RuntimeType.Get<T>().value, (ulong)allocation.Address, index);
            return command;
        }

        public static Command InsertElement(RuntimeType elementType, ReadOnlySpan<byte> elementData, uint index)
        {
            Allocation allocation = Allocation.Create(elementData);
            Command command = new(CommandOperation.InsertElement, elementType.value, (ulong)allocation.Address, index);
            return command;
        }

        public static Command RemoveElement<T>(uint index) where T : unmanaged
        {
            Command command = new(CommandOperation.RemoveElement, RuntimeType.Get<T>().value, index, 0);
            return command;
        }

        public static Command RemoveElement(RuntimeType elementType, uint index)
        {
            Command command = new(CommandOperation.RemoveElement, elementType.value, index, 0);
            return command;
        }

        public static Command ModifyElement<T>(T element, uint index) where T : unmanaged
        {
            Allocation allocation = Allocation.Create(element);
            Command command = new(CommandOperation.ModifyElement, RuntimeType.Get<T>().value, (ulong)allocation.Address, index);
            return command;
        }

        public static Command ModifyElement(RuntimeType elementType, ReadOnlySpan<byte> elementData, uint index)
        {
            Allocation allocation = Allocation.Create(elementData);
            Command command = new(CommandOperation.ModifyElement, elementType.value, (ulong)allocation.Address, index);
            return command;
        }

        void ISerializable.Write(BinaryWriter writer)
        {
            writer.WriteValue(operation);
            writer.WriteValue(a);
            writer.WriteValue(b);
            writer.WriteValue(c);
        }

        void ISerializable.Read(BinaryReader reader)
        {
            operation = reader.ReadValue<CommandOperation>();
            a = reader.ReadValue<ulong>();
            b = reader.ReadValue<ulong>();
            c = reader.ReadValue<ulong>();
        }
    }
}