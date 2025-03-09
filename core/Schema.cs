using Collections;
using System;
using System.Diagnostics;
using Types;
using Unmanaged;
using Worlds.Functions;
using Pointer = Worlds.Pointers.Schema;

namespace Worlds
{
    public unsafe struct Schema : IDisposable, IEquatable<Schema>, ISerializable
    {
        internal const int SizesLengthInBytes = sizeof(ushort) * BitMask.Capacity * 2;
        internal const int TypeHashesLengthInBytes = sizeof(long) * BitMask.Capacity * 3;

        private static int createdSchemas;

        private Pointer* schema;

        public readonly bool IsDisposed => schema is null;
        public readonly void* Pointer => schema;
        public readonly nint Address => (nint)schema;

        /// <summary>
        /// Counts how many <see cref="ComponentType"/>s are registered.
        /// </summary>
        public readonly byte ComponentCount
        {
            get
            {
                MemoryAddress.ThrowIfDefault(schema);

                return schema->componentCount;
            }
        }

        /// <summary>
        /// Counts how many <see cref="ArrayElementType"/>s are registered.
        /// </summary>
        public readonly byte ArrayCount
        {
            get
            {
                MemoryAddress.ThrowIfDefault(schema);

                return schema->arraysCount;
            }
        }

        /// <summary>
        /// Counts how many <see cref="TagType"/>s are registered.
        /// </summary>
        public readonly byte TagCount
        {
            get
            {
                MemoryAddress.ThrowIfDefault(schema);

                return schema->tagsCount;
            }
        }

        /// <summary>
        /// All component types loaded.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly System.Collections.Generic.IEnumerable<ComponentType> ComponentTypes
        {
            get
            {
                for (int c = 0; c < BitMask.MaxValue; c++)
                {
                    if (ContainsComponentType(c))
                    {
                        yield return new(c);
                    }
                }
            }
        }

        /// <summary>
        /// All array element types loaded.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly System.Collections.Generic.IEnumerable<ArrayElementType> ArrayElementTypes
        {
            get
            {
                for (int a = 0; a < BitMask.Capacity; a++)
                {
                    if (ContainsArrayType(a))
                    {
                        yield return new(a);
                    }
                }
            }
        }

        /// <summary>
        /// All tag types loaded.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly System.Collections.Generic.IEnumerable<TagType> TagTypes
        {
            get
            {
                for (int t = 0; t < BitMask.Capacity; t++)
                {
                    if (ContainsTagType(t))
                    {
                        yield return new(t);
                    }
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        private readonly TypeLayout[] Components
        {
            get
            {
                TypeLayout[] buffer = new TypeLayout[schema->componentCount];
                uint count = 0;
                for (int c = 0; c < BitMask.Capacity; c++)
                {
                    if (ContainsComponentType(c))
                    {
                        buffer[count++] = GetComponentLayout(c);
                    }
                }

                return buffer;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        private readonly TypeLayout[] ArrayElements
        {
            get
            {
                TypeLayout[] buffer = new TypeLayout[schema->arraysCount];
                uint count = 0;
                for (int a = 0; a < BitMask.Capacity; a++)
                {
                    if (ContainsArrayType(a))
                    {
                        buffer[count++] = GetArrayLayout(a);
                    }
                }

                return buffer;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        private readonly TypeLayout[] Tags
        {
            get
            {
                TypeLayout[] buffer = new TypeLayout[schema->tagsCount];
                uint count = 0;
                for (int t = 0; t < BitMask.Capacity; t++)
                {
                    if (ContainsTagType(t))
                    {
                        buffer[count++] = GetTagLayout(t);
                    }
                }

                return buffer;
            }
        }

#if NET
        /// <summary>
        /// Creates a new empty schema.
        /// </summary>
        public Schema()
        {
            ref Pointer schema = ref MemoryAddress.Allocate<Pointer>();
            schema = new(createdSchemas);
            createdSchemas++;

            fixed (Pointer* pointer = &schema)
            {
                this.schema = pointer;
            }
        }
#endif

        /// <summary>
        /// Initializes an existing schema from the given <paramref name="pointer"/>.
        /// </summary>
        public Schema(void* pointer)
        {
            schema = (Pointer*)pointer;
        }

        public void Dispose()
        {
            MemoryAddress.ThrowIfDefault(schema);

            schema->sizes.Dispose();
            schema->typeHashes.Dispose();
            MemoryAddress.Free(ref schema);
        }

        /// <summary>
        /// Copies the state of this schema into the <paramref name="destination"/>.
        /// </summary>
        public readonly void CopyTo(Schema destination)
        {
            destination.schema->componentCount = schema->componentCount;
            destination.schema->arraysCount = schema->arraysCount;
            destination.schema->tagsCount = schema->tagsCount;
            destination.schema->definitionMask = schema->definitionMask;
            schema->sizes.CopyTo(destination.schema->sizes, SizesLengthInBytes);
            schema->typeHashes.CopyTo(destination.schema->typeHashes, TypeHashesLengthInBytes);
        }

        /// <summary>
        /// Copies the state of the <paramref name="source"/> schema entirely.
        /// </summary>
        public readonly void CopyFrom(Schema source)
        {
            schema->componentCount = source.schema->componentCount;
            schema->arraysCount = source.schema->arraysCount;
            schema->tagsCount = source.schema->tagsCount;
            schema->definitionMask = source.schema->definitionMask;
            source.schema->sizes.CopyTo(schema->sizes, SizesLengthInBytes);
            source.schema->typeHashes.CopyTo(schema->typeHashes, TypeHashesLengthInBytes);
        }

        public readonly ushort GetComponentTypeSize(ComponentType componentType)
        {
            ThrowIfComponentTypeIsMissing(componentType);

            return schema->sizes.Read<ushort>(componentType.index * 2);
        }

        public readonly ushort GetComponentTypeSize(int index)
        {
            ThrowIfComponentTypeIsMissing(index);

            return schema->sizes.Read<ushort>(index * 2);
        }

        public readonly ushort GetArrayTypeSize(ArrayElementType arrayElementType)
        {
            ThrowIfArrayTypeIsMissing(arrayElementType);

            return schema->sizes.Read<ushort>(BitMask.Capacity * 2 + arrayElementType.index * 2);
        }

        public readonly ushort GetArrayTypeSize(int index)
        {
            ThrowIfArrayTypeIsMissing(index);

            return schema->sizes.Read<ushort>(BitMask.Capacity * 2 + index * 2);
        }

        /// <summary>
        /// Loads all types from the given bank into the schema.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public readonly void Load<T>() where T : unmanaged, ISchemaBank
        {
            T bank = default;
            bank.Load(this);
        }

        private static void Register(RegisterDataType.Input input)
        {
            if (input.kind == DataType.Kind.Component)
            {
                input.schema.RegisterComponent(input.type);
            }
            else if (input.kind == DataType.Kind.ArrayElement)
            {
                input.schema.RegisterArrayElement(input.type);
            }
            else if (input.kind == DataType.Kind.Tag)
            {
                input.schema.RegisterTag(input.type);
            }
        }

        public readonly DataType GetComponentDataType(ComponentType componentType)
        {
            MemoryAddress.ThrowIfDefault(schema);
            ThrowIfComponentTypeIsMissing(componentType);

            return new(componentType, GetComponentTypeSize(componentType));
        }

        public readonly DataType GetArrayDataType(ArrayElementType arrayType)
        {
            MemoryAddress.ThrowIfDefault(schema);
            ThrowIfArrayTypeIsMissing(arrayType);

            return new(arrayType, GetArrayTypeSize(arrayType));
        }

        public readonly DataType GetTagDataType(TagType tagType)
        {
            ThrowIfTagIsMissing(tagType);

            return new(tagType);
        }

        /// <summary>
        /// Retrieves the type layout for the given <paramref name="componentType"/>.
        /// </summary>
        public readonly TypeLayout GetComponentLayout(ComponentType componentType)
        {
            MemoryAddress.ThrowIfDefault(schema);
            ThrowIfComponentTypeIsMissing(componentType);

            Span<long> componentTypeHashes = new(schema->typeHashes.Pointer, BitMask.Capacity);
            long hash = componentTypeHashes[componentType.index];
            return TypeRegistry.Get(hash);
        }

        public readonly TypeLayout GetComponentLayout(int index)
        {
            MemoryAddress.ThrowIfDefault(schema);
            ThrowIfComponentTypeIsMissing(index);

            Span<long> componentTypeHashes = new(schema->typeHashes.Pointer, BitMask.Capacity);
            long hash = componentTypeHashes[index];
            return TypeRegistry.Get(hash);
        }

        /// <summary>
        /// Retrieves the type layout for the given <paramref name="arrayType"/>.
        /// </summary>
        public readonly TypeLayout GetArrayLayout(ArrayElementType arrayType)
        {
            MemoryAddress.ThrowIfDefault(schema);
            ThrowIfArrayTypeIsMissing(arrayType);

            Span<long> arrayTypeHashes = schema->typeHashes.AsSpan<long>(BitMask.Capacity, BitMask.Capacity);
            long hash = arrayTypeHashes[arrayType.index];
            return TypeRegistry.Get(hash);
        }

        public readonly TypeLayout GetArrayLayout(int index)
        {
            MemoryAddress.ThrowIfDefault(schema);
            ThrowIfArrayTypeIsMissing(index);

            Span<long> arrayTypeHashes = schema->typeHashes.AsSpan<long>(BitMask.Capacity, BitMask.Capacity);
            long hash = arrayTypeHashes[index];
            return TypeRegistry.Get(hash);
        }

        /// <summary>
        /// Retrieves the type layout for the given <paramref name="tagType"/>.
        /// </summary>
        public readonly TypeLayout GetTagLayout(TagType tagType)
        {
            MemoryAddress.ThrowIfDefault(schema);
            ThrowIfTagIsMissing(tagType);

            Span<long> tagTypeHashes = schema->typeHashes.AsSpan<long>(BitMask.Capacity * 2, BitMask.Capacity);
            long hash = tagTypeHashes[tagType.index];
            return TypeRegistry.Get(hash);
        }

        public readonly TypeLayout GetTagLayout(int index)
        {
            MemoryAddress.ThrowIfDefault(schema);
            ThrowIfTagIsMissing(index);

            Span<long> tagTypeHashes = schema->typeHashes.AsSpan<long>(BitMask.Capacity * 2, BitMask.Capacity);
            long hash = tagTypeHashes[index];
            return TypeRegistry.Get(hash);
        }

        public readonly TypeLayout GetComponentLayout<T>() where T : unmanaged
        {
            return GetComponentLayout(GetComponentType<T>());
        }

        public readonly ComponentType RegisterComponent<T>() where T : unmanaged
        {
            ComponentType newComponentType = RegisterComponent(TypeRegistry.GetOrRegister<T>());
            SchemaTypeCache<T>.SetComponentType(this, newComponentType);
            return newComponentType;
        }

        public readonly ComponentType RegisterComponent(TypeLayout type)
        {
            if (TryGetComponentType(type, out ComponentType existing))
            {
                return existing;
            }

            ThrowIfTooManyComponents();

            System.Span<ushort> componentSizes = new(schema->sizes.Pointer, BitMask.Capacity);
            System.Span<long> componentHashes = new(schema->typeHashes.Pointer, BitMask.Capacity);
            ComponentType componentType = new(schema->componentCount);
            componentSizes[componentType.index] = type.Size;
            componentHashes[componentType.index] = type.Hash;
            schema->componentCount++;
            schema->definitionMask.AddComponentType(componentType);

            StoreComponentTypeForDebug(componentType, type);
            return componentType;
        }

        public readonly ArrayElementType RegisterArrayElement<T>() where T : unmanaged
        {
            ArrayElementType arrayType = RegisterArrayElement(TypeRegistry.GetOrRegister<T>());
            SchemaTypeCache<T>.SetArrayType(this, arrayType);
            return arrayType;
        }

        public readonly ArrayElementType RegisterArrayElement(TypeLayout type)
        {
            if (TryGetArrayType(type, out ArrayElementType existing))
            {
                return existing;
            }

            ThrowIfTooManyArrays();

            ArrayElementType arrayType = new(schema->arraysCount);
            System.Span<ushort> arrayElementSizes = schema->sizes.AsSpan<ushort>(BitMask.Capacity, BitMask.Capacity);
            System.Span<long> arrayElementHashes = schema->typeHashes.AsSpan<long>(BitMask.Capacity, BitMask.Capacity);
            arrayElementSizes[schema->arraysCount] = type.Size;
            arrayElementHashes[schema->arraysCount] = type.Hash;
            schema->arraysCount++;
            schema->definitionMask.AddArrayType(arrayType);
            StoreArrayTypeForDebug(arrayType, type);
            return arrayType;
        }

        public readonly TagType RegisterTag<T>() where T : unmanaged
        {
            TagType tagType = RegisterTag(TypeRegistry.GetOrRegister<T>());
            SchemaTypeCache<T>.SetTagType(this, tagType);
            return tagType;
        }

        public readonly TagType RegisterTag(TypeLayout type)
        {
            if (TryGetTagType(type, out TagType existing))
            {
                return existing;
            }

            ThrowIfTooManyTags();

            TagType tagType = new(schema->tagsCount);
            System.Span<long> tagHashes = schema->typeHashes.AsSpan<long>(BitMask.Capacity * 2, BitMask.Capacity);
            tagHashes[tagType.index] = type.Hash;
            schema->definitionMask.AddTagType(tagType);
            schema->tagsCount++;
            StoreTagTypeForDebug(tagType, type);
            return tagType;
        }

        public readonly bool ContainsComponentType(ComponentType componentType)
        {
            MemoryAddress.ThrowIfDefault(schema);

            return schema->definitionMask.ComponentTypes.Contains(componentType.index);
        }

        public readonly bool ContainsArrayType(ArrayElementType arrayType)
        {
            MemoryAddress.ThrowIfDefault(schema);

            return schema->definitionMask.ArrayTypes.Contains(arrayType.index);
        }

        public readonly bool ContainsTagType(TagType tagType)
        {
            MemoryAddress.ThrowIfDefault(schema);

            return schema->definitionMask.TagTypes.Contains(tagType.index);
        }

        public readonly bool ContainsComponentType(int index)
        {
            MemoryAddress.ThrowIfDefault(schema);

            return schema->definitionMask.ComponentTypes.Contains(index);
        }

        public readonly bool ContainsArrayType(int index)
        {
            MemoryAddress.ThrowIfDefault(schema);

            return schema->definitionMask.ArrayTypes.Contains(index);
        }

        public readonly bool ContainsTagType(int index)
        {
            MemoryAddress.ThrowIfDefault(schema);

            return schema->definitionMask.TagTypes.Contains(index);
        }

        public readonly bool ContainsComponentType(ASCIIText256 fullTypeName)
        {
            MemoryAddress.ThrowIfDefault(schema);

            Span<long> componentTypeHashes = new(schema->typeHashes.Pointer, BitMask.Capacity);
            return componentTypeHashes.Contains(fullTypeName.GetLongHashCode());
        }

        public readonly bool ContainsComponentType(TypeLayout type)
        {
            MemoryAddress.ThrowIfDefault(schema);

            Span<long> componentTypeHashes = new(schema->typeHashes.Pointer, BitMask.Capacity);
            return componentTypeHashes.Contains(type.Hash);
        }

        public readonly bool TryGetComponentType(TypeLayout type, out ComponentType componentType)
        {
            MemoryAddress.ThrowIfDefault(schema);

            Span<long> componentTypeHashes = new(schema->typeHashes.Pointer, BitMask.Capacity);
            bool contains = componentTypeHashes.TryIndexOf(type.Hash, out int index);
            componentType = new(index);
            return contains;
        }

        public readonly bool ContainsArrayType(ASCIIText256 fullTypeName)
        {
            MemoryAddress.ThrowIfDefault(schema);

            Span<long> arrayTypeHashes = schema->typeHashes.AsSpan<long>(BitMask.Capacity, BitMask.Capacity);
            return arrayTypeHashes.Contains(fullTypeName.GetLongHashCode());
        }

        public readonly bool ContainsArrayType(TypeLayout type)
        {
            MemoryAddress.ThrowIfDefault(schema);

            Span<long> arrayTypeHashes = schema->typeHashes.AsSpan<long>(BitMask.Capacity, BitMask.Capacity);
            return arrayTypeHashes.Contains(type.Hash);
        }

        public readonly bool TryGetArrayType(TypeLayout type, out ArrayElementType arrayType)
        {
            Span<long> arrayTypeHashes = schema->typeHashes.AsSpan<long>(BitMask.Capacity, BitMask.Capacity);
            bool contains = arrayTypeHashes.TryIndexOf(type.Hash, out int index);
            arrayType = new(index);
            return contains;
        }

        public readonly bool ContainsTagType(ASCIIText256 fullTypeName)
        {
            System.Span<long> tagTypeHashes = schema->typeHashes.AsSpan<long>(BitMask.Capacity * 2, BitMask.Capacity);
            return tagTypeHashes.Contains(fullTypeName.GetLongHashCode());
        }

        public readonly bool ContainsTagType(TypeLayout type)
        {
            System.Span<long> tagTypeHashes = schema->typeHashes.AsSpan<long>(BitMask.Capacity * 2, BitMask.Capacity);
            return tagTypeHashes.Contains(type.Hash);
        }

        public readonly bool TryGetTagType(TypeLayout type, out TagType tagType)
        {
            Span<long> tagTypeHashes = schema->typeHashes.AsSpan<long>(BitMask.Capacity * 2, BitMask.Capacity);
            bool contains = tagTypeHashes.TryIndexOf(type.Hash, out int index);
            tagType = new(index);
            return contains;
        }

        public readonly bool ContainsComponentType<T>() where T : unmanaged
        {
            if (!TypeRegistry.IsRegistered<T>())
            {
                return false;
            }

            System.Span<long> componentTypeHashes = new(schema->typeHashes.Pointer, BitMask.Capacity);
            return componentTypeHashes.Contains(TypeLayoutHashCodeCache<T>.value);
        }

        public readonly ComponentType GetComponentType<T>() where T : unmanaged
        {
            ThrowIfComponentTypeIsMissing<T>();

            if (!SchemaTypeCache<T>.TryGetComponentType(this, out int index))
            {
                Span<long> componentTypeHashes = new(schema->typeHashes.Pointer, BitMask.Capacity);
                index = componentTypeHashes.IndexOf(TypeLayoutHashCodeCache<T>.value);
                SchemaTypeCache<T>.SetComponentType(this, index);
                Trace.WriteLine($"Cached component type for {typeof(T).FullName}");
            }

            return new(index);
        }

        public readonly int GetComponentTypeIndex<T>() where T : unmanaged
        {
            ThrowIfComponentTypeIsMissing<T>();

            if (!SchemaTypeCache<T>.TryGetComponentType(this, out int index))
            {
                Span<long> componentTypeHashes = new(schema->typeHashes.Pointer, BitMask.Capacity);
                index = componentTypeHashes.IndexOf(TypeLayoutHashCodeCache<T>.value);
                SchemaTypeCache<T>.SetComponentType(this, index);
                Trace.WriteLine($"Cached component type for {typeof(T).FullName}");
            }

            return index;
        }

        public readonly DataType GetComponentDataType<T>() where T : unmanaged
        {
            ThrowIfComponentTypeIsMissing<T>();

            if (!SchemaTypeCache<T>.TryGetComponentType(this, out int index))
            {
                Span<long> componentTypeHashes = new(schema->typeHashes.Pointer, BitMask.Capacity);
                index = componentTypeHashes.IndexOf(TypeLayoutHashCodeCache<T>.value);
                SchemaTypeCache<T>.SetComponentType(this, index);
                Trace.WriteLine($"Cached component type for {typeof(T).FullName}");
            }

            return new(index, DataType.Kind.Component, (ushort)sizeof(T));
        }

        public readonly DataType GetComponentDataType(TypeLayout type)
        {
            ThrowIfComponentTypeIsMissing(type);

            Span<long> componentTypeHashes = new(schema->typeHashes.Pointer, BitMask.Capacity);
            int index = componentTypeHashes.IndexOf(type.Hash);
            return new(index, DataType.Kind.Component, type.Size);
        }

        public readonly DataType GetComponentDataType(int index)
        {
            ThrowIfComponentTypeIsMissing(index);

            return new(index, DataType.Kind.Component, GetComponentTypeSize(index));
        }

        public readonly bool ContainsArrayType<T>() where T : unmanaged
        {
            if (!TypeRegistry.IsRegistered<T>())
            {
                return false;
            }

            System.Span<long> arrayTypeHashes = schema->typeHashes.AsSpan<long>(BitMask.Capacity, BitMask.Capacity);
            return arrayTypeHashes.Contains(TypeLayoutHashCodeCache<T>.value);
        }

        public readonly ArrayElementType GetArrayType<T>() where T : unmanaged
        {
            ThrowIfArrayTypeIsMissing<T>();

            if (!SchemaTypeCache<T>.TryGetArrayType(this, out int index))
            {
                Span<long> arrayTypeHashes = schema->typeHashes.AsSpan<long>(BitMask.Capacity, BitMask.Capacity);
                index = arrayTypeHashes.IndexOf(TypeLayoutHashCodeCache<T>.value);
                SchemaTypeCache<T>.SetArrayType(this, index);
                Trace.WriteLine($"Cached array element type for {typeof(T).FullName}");
            }

            return new(index);
        }

        public readonly int GetArrayTypeIndex<T>() where T : unmanaged
        {
            ThrowIfArrayTypeIsMissing<T>();

            if (!SchemaTypeCache<T>.TryGetArrayType(this, out int index))
            {
                Span<long> arrayTypeHashes = schema->typeHashes.AsSpan<long>(BitMask.Capacity, BitMask.Capacity);
                index = arrayTypeHashes.IndexOf(TypeLayoutHashCodeCache<T>.value);
                SchemaTypeCache<T>.SetArrayType(this, index);
                Trace.WriteLine($"Cached array element type for {typeof(T).FullName}");
            }

            return index;
        }

        public readonly DataType GetArrayDataType<T>() where T : unmanaged
        {
            ThrowIfArrayTypeIsMissing<T>();

            if (!SchemaTypeCache<T>.TryGetArrayType(this, out int index))
            {
                Span<long> arrayTypeHashes = schema->typeHashes.AsSpan<long>(BitMask.Capacity, BitMask.Capacity);
                index = arrayTypeHashes.IndexOf(TypeLayoutHashCodeCache<T>.value);
                SchemaTypeCache<T>.SetArrayType(this, index);
                Trace.WriteLine($"Cached array element type for {typeof(T).FullName}");
            }

            return new(index, DataType.Kind.ArrayElement, (ushort)sizeof(T));
        }

        public readonly DataType GetArrayDataType(TypeLayout type)
        {
            ThrowIfArrayTypeIsMissing(type);

            Span<long> arrayTypeHashes = schema->typeHashes.AsSpan<long>(BitMask.Capacity, BitMask.Capacity);
            int index = arrayTypeHashes.IndexOf(type.Hash);
            return new(index, DataType.Kind.ArrayElement, type.Size);
        }

        public readonly DataType GetArrayDataType(int index)
        {
            ThrowIfArrayTypeIsMissing(index);

            return new(index, DataType.Kind.ArrayElement, GetArrayTypeSize(index));
        }

        public readonly TagType GetTagType<T>() where T : unmanaged
        {
            ThrowIfTagIsMissing<T>();

            if (!SchemaTypeCache<T>.TryGetTagType(this, out int index))
            {
                Span<long> tagTypeHashes = schema->typeHashes.AsSpan<long>(BitMask.Capacity * 2, BitMask.Capacity);
                index = tagTypeHashes.IndexOf(TypeLayoutHashCodeCache<T>.value);
                SchemaTypeCache<T>.SetTagType(this, index);
                Trace.WriteLine($"Cached tag type for {typeof(T).FullName}");
            }

            return new(index);
        }

        public readonly int GetTagTypeIndex<T>() where T : unmanaged
        {
            ThrowIfTagIsMissing<T>();

            if (!SchemaTypeCache<T>.TryGetTagType(this, out int index))
            {
                Span<long> tagTypeHashes = schema->typeHashes.AsSpan<long>(BitMask.Capacity * 2, BitMask.Capacity);
                index = tagTypeHashes.IndexOf(TypeLayoutHashCodeCache<T>.value);
                SchemaTypeCache<T>.SetTagType(this, index);
                Trace.WriteLine($"Cached tag type for {typeof(T).FullName}");
            }

            return index;
        }

        public readonly DataType GetTagDataType<T>() where T : unmanaged
        {
            ThrowIfTagIsMissing<T>();

            if (!SchemaTypeCache<T>.TryGetTagType(this, out int index))
            {
                Span<long> tagTypeHashes = schema->typeHashes.AsSpan<long>(BitMask.Capacity * 2, BitMask.Capacity);
                index = tagTypeHashes.IndexOf(TypeLayoutHashCodeCache<T>.value);
                SchemaTypeCache<T>.SetTagType(this, index);
                Trace.WriteLine($"Cached tag type for {typeof(T).FullName}");
            }

            return new(index, DataType.Kind.Tag, default);
        }

        public readonly DataType GetTagDataType(int index)
        {
            ThrowIfTagIsMissing(index);

            return new(index, DataType.Kind.Tag, default);
        }

        public readonly bool ContainsTagType<T>() where T : unmanaged
        {
            if (!TypeRegistry.IsRegistered<T>())
            {
                return false;
            }

            Span<long> tagTypeHashes = schema->typeHashes.AsSpan<long>(BitMask.Capacity * 2, BitMask.Capacity);
            return tagTypeHashes.Contains(TypeLayoutHashCodeCache<T>.value);
        }

        public readonly BitMask GetComponents<T1>() where T1 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetComponentTypeIndex<T1>());
            return bitMask;
        }

        public readonly BitMask GetComponents<T1, T2>() where T1 : unmanaged where T2 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetComponentTypeIndex<T1>());
            bitMask.Set(GetComponentTypeIndex<T2>());
            return bitMask;
        }

        public readonly BitMask GetComponents<T1, T2, T3>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetComponentTypeIndex<T1>());
            bitMask.Set(GetComponentTypeIndex<T2>());
            bitMask.Set(GetComponentTypeIndex<T3>());
            return bitMask;
        }

        public readonly BitMask GetComponents<T1, T2, T3, T4>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetComponentTypeIndex<T1>());
            bitMask.Set(GetComponentTypeIndex<T2>());
            bitMask.Set(GetComponentTypeIndex<T3>());
            bitMask.Set(GetComponentTypeIndex<T4>());
            return bitMask;
        }

        public readonly BitMask GetComponents<T1, T2, T3, T4, T5>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetComponentTypeIndex<T1>());
            bitMask.Set(GetComponentTypeIndex<T2>());
            bitMask.Set(GetComponentTypeIndex<T3>());
            bitMask.Set(GetComponentTypeIndex<T4>());
            bitMask.Set(GetComponentTypeIndex<T5>());
            return bitMask;
        }

        public readonly BitMask GetComponents<T1, T2, T3, T4, T5, T6>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetComponentTypeIndex<T1>());
            bitMask.Set(GetComponentTypeIndex<T2>());
            bitMask.Set(GetComponentTypeIndex<T3>());
            bitMask.Set(GetComponentTypeIndex<T4>());
            bitMask.Set(GetComponentTypeIndex<T5>());
            bitMask.Set(GetComponentTypeIndex<T6>());
            return bitMask;
        }

        public readonly BitMask GetComponents<T1, T2, T3, T4, T5, T6, T7>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetComponentTypeIndex<T1>());
            bitMask.Set(GetComponentTypeIndex<T2>());
            bitMask.Set(GetComponentTypeIndex<T3>());
            bitMask.Set(GetComponentTypeIndex<T4>());
            bitMask.Set(GetComponentTypeIndex<T5>());
            bitMask.Set(GetComponentTypeIndex<T6>());
            bitMask.Set(GetComponentTypeIndex<T7>());
            return bitMask;
        }

        public readonly BitMask GetComponents<T1, T2, T3, T4, T5, T6, T7, T8>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetComponentTypeIndex<T1>());
            bitMask.Set(GetComponentTypeIndex<T2>());
            bitMask.Set(GetComponentTypeIndex<T3>());
            bitMask.Set(GetComponentTypeIndex<T4>());
            bitMask.Set(GetComponentTypeIndex<T5>());
            bitMask.Set(GetComponentTypeIndex<T6>());
            bitMask.Set(GetComponentTypeIndex<T7>());
            bitMask.Set(GetComponentTypeIndex<T8>());
            return bitMask;
        }

        public readonly BitMask GetComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetComponentTypeIndex<T1>());
            bitMask.Set(GetComponentTypeIndex<T2>());
            bitMask.Set(GetComponentTypeIndex<T3>());
            bitMask.Set(GetComponentTypeIndex<T4>());
            bitMask.Set(GetComponentTypeIndex<T5>());
            bitMask.Set(GetComponentTypeIndex<T6>());
            bitMask.Set(GetComponentTypeIndex<T7>());
            bitMask.Set(GetComponentTypeIndex<T8>());
            bitMask.Set(GetComponentTypeIndex<T9>());
            return bitMask;
        }

        public readonly BitMask GetComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetComponentTypeIndex<T1>());
            bitMask.Set(GetComponentTypeIndex<T2>());
            bitMask.Set(GetComponentTypeIndex<T3>());
            bitMask.Set(GetComponentTypeIndex<T4>());
            bitMask.Set(GetComponentTypeIndex<T5>());
            bitMask.Set(GetComponentTypeIndex<T6>());
            bitMask.Set(GetComponentTypeIndex<T7>());
            bitMask.Set(GetComponentTypeIndex<T8>());
            bitMask.Set(GetComponentTypeIndex<T9>());
            bitMask.Set(GetComponentTypeIndex<T10>());
            return bitMask;
        }

        public readonly BitMask GetComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetComponentTypeIndex<T1>());
            bitMask.Set(GetComponentTypeIndex<T2>());
            bitMask.Set(GetComponentTypeIndex<T3>());
            bitMask.Set(GetComponentTypeIndex<T4>());
            bitMask.Set(GetComponentTypeIndex<T5>());
            bitMask.Set(GetComponentTypeIndex<T6>());
            bitMask.Set(GetComponentTypeIndex<T7>());
            bitMask.Set(GetComponentTypeIndex<T8>());
            bitMask.Set(GetComponentTypeIndex<T9>());
            bitMask.Set(GetComponentTypeIndex<T10>());
            bitMask.Set(GetComponentTypeIndex<T11>());
            return bitMask;
        }

        public readonly BitMask GetComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetComponentTypeIndex<T1>());
            bitMask.Set(GetComponentTypeIndex<T2>());
            bitMask.Set(GetComponentTypeIndex<T3>());
            bitMask.Set(GetComponentTypeIndex<T4>());
            bitMask.Set(GetComponentTypeIndex<T5>());
            bitMask.Set(GetComponentTypeIndex<T6>());
            bitMask.Set(GetComponentTypeIndex<T7>());
            bitMask.Set(GetComponentTypeIndex<T8>());
            bitMask.Set(GetComponentTypeIndex<T9>());
            bitMask.Set(GetComponentTypeIndex<T10>());
            bitMask.Set(GetComponentTypeIndex<T11>());
            bitMask.Set(GetComponentTypeIndex<T12>());
            return bitMask;
        }

        public readonly BitMask GetComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetComponentTypeIndex<T1>());
            bitMask.Set(GetComponentTypeIndex<T2>());
            bitMask.Set(GetComponentTypeIndex<T3>());
            bitMask.Set(GetComponentTypeIndex<T4>());
            bitMask.Set(GetComponentTypeIndex<T5>());
            bitMask.Set(GetComponentTypeIndex<T6>());
            bitMask.Set(GetComponentTypeIndex<T7>());
            bitMask.Set(GetComponentTypeIndex<T8>());
            bitMask.Set(GetComponentTypeIndex<T9>());
            bitMask.Set(GetComponentTypeIndex<T10>());
            bitMask.Set(GetComponentTypeIndex<T11>());
            bitMask.Set(GetComponentTypeIndex<T12>());
            bitMask.Set(GetComponentTypeIndex<T13>());
            return bitMask;
        }

        public readonly BitMask GetComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetComponentTypeIndex<T1>());
            bitMask.Set(GetComponentTypeIndex<T2>());
            bitMask.Set(GetComponentTypeIndex<T3>());
            bitMask.Set(GetComponentTypeIndex<T4>());
            bitMask.Set(GetComponentTypeIndex<T5>());
            bitMask.Set(GetComponentTypeIndex<T6>());
            bitMask.Set(GetComponentTypeIndex<T7>());
            bitMask.Set(GetComponentTypeIndex<T8>());
            bitMask.Set(GetComponentTypeIndex<T9>());
            bitMask.Set(GetComponentTypeIndex<T10>());
            bitMask.Set(GetComponentTypeIndex<T11>());
            bitMask.Set(GetComponentTypeIndex<T12>());
            bitMask.Set(GetComponentTypeIndex<T13>());
            bitMask.Set(GetComponentTypeIndex<T14>());
            return bitMask;
        }

        public readonly BitMask GetComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged where T15 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetComponentTypeIndex<T1>());
            bitMask.Set(GetComponentTypeIndex<T2>());
            bitMask.Set(GetComponentTypeIndex<T3>());
            bitMask.Set(GetComponentTypeIndex<T4>());
            bitMask.Set(GetComponentTypeIndex<T5>());
            bitMask.Set(GetComponentTypeIndex<T6>());
            bitMask.Set(GetComponentTypeIndex<T7>());
            bitMask.Set(GetComponentTypeIndex<T8>());
            bitMask.Set(GetComponentTypeIndex<T9>());
            bitMask.Set(GetComponentTypeIndex<T10>());
            bitMask.Set(GetComponentTypeIndex<T11>());
            bitMask.Set(GetComponentTypeIndex<T12>());
            bitMask.Set(GetComponentTypeIndex<T13>());
            bitMask.Set(GetComponentTypeIndex<T14>());
            bitMask.Set(GetComponentTypeIndex<T15>());
            return bitMask;
        }

        public readonly BitMask GetComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged where T15 : unmanaged where T16 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetComponentTypeIndex<T1>());
            bitMask.Set(GetComponentTypeIndex<T2>());
            bitMask.Set(GetComponentTypeIndex<T3>());
            bitMask.Set(GetComponentTypeIndex<T4>());
            bitMask.Set(GetComponentTypeIndex<T5>());
            bitMask.Set(GetComponentTypeIndex<T6>());
            bitMask.Set(GetComponentTypeIndex<T7>());
            bitMask.Set(GetComponentTypeIndex<T8>());
            bitMask.Set(GetComponentTypeIndex<T9>());
            bitMask.Set(GetComponentTypeIndex<T10>());
            bitMask.Set(GetComponentTypeIndex<T11>());
            bitMask.Set(GetComponentTypeIndex<T12>());
            bitMask.Set(GetComponentTypeIndex<T13>());
            bitMask.Set(GetComponentTypeIndex<T14>());
            bitMask.Set(GetComponentTypeIndex<T15>());
            bitMask.Set(GetComponentTypeIndex<T16>());
            return bitMask;
        }

        public readonly BitMask GetArrayElements<T1>() where T1 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetArrayTypeIndex<T1>());
            return bitMask;
        }

        public readonly BitMask GetArrayElements<T1, T2>() where T1 : unmanaged where T2 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetArrayTypeIndex<T1>());
            bitMask.Set(GetArrayTypeIndex<T2>());
            return bitMask;
        }

        public readonly BitMask GetArrayElements<T1, T2, T3>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetArrayTypeIndex<T1>());
            bitMask.Set(GetArrayTypeIndex<T2>());
            bitMask.Set(GetArrayTypeIndex<T3>());
            return bitMask;
        }

        public readonly BitMask GetArrayElements<T1, T2, T3, T4>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetArrayTypeIndex<T1>());
            bitMask.Set(GetArrayTypeIndex<T2>());
            bitMask.Set(GetArrayTypeIndex<T3>());
            bitMask.Set(GetArrayTypeIndex<T4>());
            return bitMask;
        }

        public readonly BitMask GetArrayElements<T1, T2, T3, T4, T5>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetArrayTypeIndex<T1>());
            bitMask.Set(GetArrayTypeIndex<T2>());
            bitMask.Set(GetArrayTypeIndex<T3>());
            bitMask.Set(GetArrayTypeIndex<T4>());
            bitMask.Set(GetArrayTypeIndex<T5>());
            return bitMask;
        }

        public readonly BitMask GetArrayElements<T1, T2, T3, T4, T5, T6>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetArrayTypeIndex<T1>());
            bitMask.Set(GetArrayTypeIndex<T2>());
            bitMask.Set(GetArrayTypeIndex<T3>());
            bitMask.Set(GetArrayTypeIndex<T4>());
            bitMask.Set(GetArrayTypeIndex<T5>());
            bitMask.Set(GetArrayTypeIndex<T6>());
            return bitMask;
        }

        public readonly BitMask GetArrayElements<T1, T2, T3, T4, T5, T6, T7>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetArrayTypeIndex<T1>());
            bitMask.Set(GetArrayTypeIndex<T2>());
            bitMask.Set(GetArrayTypeIndex<T3>());
            bitMask.Set(GetArrayTypeIndex<T4>());
            bitMask.Set(GetArrayTypeIndex<T5>());
            bitMask.Set(GetArrayTypeIndex<T6>());
            bitMask.Set(GetArrayTypeIndex<T7>());
            return bitMask;
        }

        public readonly BitMask GetArrayElements<T1, T2, T3, T4, T5, T6, T7, T8>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetArrayTypeIndex<T1>());
            bitMask.Set(GetArrayTypeIndex<T2>());
            bitMask.Set(GetArrayTypeIndex<T3>());
            bitMask.Set(GetArrayTypeIndex<T4>());
            bitMask.Set(GetArrayTypeIndex<T5>());
            bitMask.Set(GetArrayTypeIndex<T6>());
            bitMask.Set(GetArrayTypeIndex<T7>());
            bitMask.Set(GetArrayTypeIndex<T8>());
            return bitMask;
        }

        public readonly BitMask GetArrayElements<T1, T2, T3, T4, T5, T6, T7, T8, T9>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetArrayTypeIndex<T1>());
            bitMask.Set(GetArrayTypeIndex<T2>());
            bitMask.Set(GetArrayTypeIndex<T3>());
            bitMask.Set(GetArrayTypeIndex<T4>());
            bitMask.Set(GetArrayTypeIndex<T5>());
            bitMask.Set(GetArrayTypeIndex<T6>());
            bitMask.Set(GetArrayTypeIndex<T7>());
            bitMask.Set(GetArrayTypeIndex<T8>());
            bitMask.Set(GetArrayTypeIndex<T9>());
            return bitMask;
        }

        public readonly BitMask GetArrayElements<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetArrayTypeIndex<T1>());
            bitMask.Set(GetArrayTypeIndex<T2>());
            bitMask.Set(GetArrayTypeIndex<T3>());
            bitMask.Set(GetArrayTypeIndex<T4>());
            bitMask.Set(GetArrayTypeIndex<T5>());
            bitMask.Set(GetArrayTypeIndex<T6>());
            bitMask.Set(GetArrayTypeIndex<T7>());
            bitMask.Set(GetArrayTypeIndex<T8>());
            bitMask.Set(GetArrayTypeIndex<T9>());
            bitMask.Set(GetArrayTypeIndex<T10>());
            return bitMask;
        }

        public readonly BitMask GetArrayElements<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetArrayTypeIndex<T1>());
            bitMask.Set(GetArrayTypeIndex<T2>());
            bitMask.Set(GetArrayTypeIndex<T3>());
            bitMask.Set(GetArrayTypeIndex<T4>());
            bitMask.Set(GetArrayTypeIndex<T5>());
            bitMask.Set(GetArrayTypeIndex<T6>());
            bitMask.Set(GetArrayTypeIndex<T7>());
            bitMask.Set(GetArrayTypeIndex<T8>());
            bitMask.Set(GetArrayTypeIndex<T9>());
            bitMask.Set(GetArrayTypeIndex<T10>());
            bitMask.Set(GetArrayTypeIndex<T11>());
            return bitMask;
        }

        public readonly BitMask GetArrayElements<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetArrayTypeIndex<T1>());
            bitMask.Set(GetArrayTypeIndex<T2>());
            bitMask.Set(GetArrayTypeIndex<T3>());
            bitMask.Set(GetArrayTypeIndex<T4>());
            bitMask.Set(GetArrayTypeIndex<T5>());
            bitMask.Set(GetArrayTypeIndex<T6>());
            bitMask.Set(GetArrayTypeIndex<T7>());
            bitMask.Set(GetArrayTypeIndex<T8>());
            bitMask.Set(GetArrayTypeIndex<T9>());
            bitMask.Set(GetArrayTypeIndex<T10>());
            bitMask.Set(GetArrayTypeIndex<T11>());
            bitMask.Set(GetArrayTypeIndex<T12>());
            return bitMask;
        }

        public readonly BitMask GetTags<T1>() where T1 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetTagTypeIndex<T1>());
            return bitMask;
        }

        public readonly BitMask GetTags<T1, T2>() where T1 : unmanaged where T2 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetTagTypeIndex<T1>());
            bitMask.Set(GetTagTypeIndex<T2>());
            return bitMask;
        }

        public readonly BitMask GetTags<T1, T2, T3>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetTagTypeIndex<T1>());
            bitMask.Set(GetTagTypeIndex<T2>());
            bitMask.Set(GetTagTypeIndex<T3>());
            return bitMask;
        }

        public readonly BitMask GetTags<T1, T2, T3, T4>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetTagTypeIndex<T1>());
            bitMask.Set(GetTagTypeIndex<T2>());
            bitMask.Set(GetTagTypeIndex<T3>());
            bitMask.Set(GetTagTypeIndex<T4>());
            return bitMask;
        }

        public readonly BitMask GetTags<T1, T2, T3, T4, T5>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetTagTypeIndex<T1>());
            bitMask.Set(GetTagTypeIndex<T2>());
            bitMask.Set(GetTagTypeIndex<T3>());
            bitMask.Set(GetTagTypeIndex<T4>());
            bitMask.Set(GetTagTypeIndex<T5>());
            return bitMask;
        }

        public readonly BitMask GetTags<T1, T2, T3, T4, T5, T6>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetTagTypeIndex<T1>());
            bitMask.Set(GetTagTypeIndex<T2>());
            bitMask.Set(GetTagTypeIndex<T3>());
            bitMask.Set(GetTagTypeIndex<T4>());
            bitMask.Set(GetTagTypeIndex<T5>());
            bitMask.Set(GetTagTypeIndex<T6>());
            return bitMask;
        }

        public readonly BitMask GetTags<T1, T2, T3, T4, T5, T6, T7>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetTagTypeIndex<T1>());
            bitMask.Set(GetTagTypeIndex<T2>());
            bitMask.Set(GetTagTypeIndex<T3>());
            bitMask.Set(GetTagTypeIndex<T4>());
            bitMask.Set(GetTagTypeIndex<T5>());
            bitMask.Set(GetTagTypeIndex<T6>());
            bitMask.Set(GetTagTypeIndex<T7>());
            return bitMask;
        }

        public readonly BitMask GetTags<T1, T2, T3, T4, T5, T6, T7, T8>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetTagTypeIndex<T1>());
            bitMask.Set(GetTagTypeIndex<T2>());
            bitMask.Set(GetTagTypeIndex<T3>());
            bitMask.Set(GetTagTypeIndex<T4>());
            bitMask.Set(GetTagTypeIndex<T5>());
            bitMask.Set(GetTagTypeIndex<T6>());
            bitMask.Set(GetTagTypeIndex<T7>());
            bitMask.Set(GetTagTypeIndex<T8>());
            return bitMask;
        }

        public readonly BitMask GetTags<T1, T2, T3, T4, T5, T6, T7, T8, T9>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetTagTypeIndex<T1>());
            bitMask.Set(GetTagTypeIndex<T2>());
            bitMask.Set(GetTagTypeIndex<T3>());
            bitMask.Set(GetTagTypeIndex<T4>());
            bitMask.Set(GetTagTypeIndex<T5>());
            bitMask.Set(GetTagTypeIndex<T6>());
            bitMask.Set(GetTagTypeIndex<T7>());
            bitMask.Set(GetTagTypeIndex<T8>());
            bitMask.Set(GetTagTypeIndex<T9>());
            return bitMask;
        }

        public readonly BitMask GetTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetTagTypeIndex<T1>());
            bitMask.Set(GetTagTypeIndex<T2>());
            bitMask.Set(GetTagTypeIndex<T3>());
            bitMask.Set(GetTagTypeIndex<T4>());
            bitMask.Set(GetTagTypeIndex<T5>());
            bitMask.Set(GetTagTypeIndex<T6>());
            bitMask.Set(GetTagTypeIndex<T7>());
            bitMask.Set(GetTagTypeIndex<T8>());
            bitMask.Set(GetTagTypeIndex<T9>());
            bitMask.Set(GetTagTypeIndex<T10>());
            return bitMask;
        }

        public readonly BitMask GetTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetTagTypeIndex<T1>());
            bitMask.Set(GetTagTypeIndex<T2>());
            bitMask.Set(GetTagTypeIndex<T3>());
            bitMask.Set(GetTagTypeIndex<T4>());
            bitMask.Set(GetTagTypeIndex<T5>());
            bitMask.Set(GetTagTypeIndex<T6>());
            bitMask.Set(GetTagTypeIndex<T7>());
            bitMask.Set(GetTagTypeIndex<T8>());
            bitMask.Set(GetTagTypeIndex<T9>());
            bitMask.Set(GetTagTypeIndex<T10>());
            bitMask.Set(GetTagTypeIndex<T11>());
            return bitMask;
        }

        public readonly BitMask GetTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetTagTypeIndex<T1>());
            bitMask.Set(GetTagTypeIndex<T2>());
            bitMask.Set(GetTagTypeIndex<T3>());
            bitMask.Set(GetTagTypeIndex<T4>());
            bitMask.Set(GetTagTypeIndex<T5>());
            bitMask.Set(GetTagTypeIndex<T6>());
            bitMask.Set(GetTagTypeIndex<T7>());
            bitMask.Set(GetTagTypeIndex<T8>());
            bitMask.Set(GetTagTypeIndex<T9>());
            bitMask.Set(GetTagTypeIndex<T10>());
            bitMask.Set(GetTagTypeIndex<T11>());
            bitMask.Set(GetTagTypeIndex<T12>());
            return bitMask;
        }

        public static Schema Create()
        {
            ref Pointer schema = ref MemoryAddress.Allocate<Pointer>();
            schema = new(createdSchemas);
            createdSchemas++;

            fixed (Pointer* pointer = &schema)
            {
                return new(pointer);
            }
        }

        [Conditional("DEBUG")]
        private static void StoreComponentTypeForDebug(ComponentType componentType, TypeLayout type)
        {
#if DEBUG
            ComponentType.debugCachedTypes[componentType.index] = type;
#endif
        }

        [Conditional("DEBUG")]
        private static void StoreArrayTypeForDebug(ArrayElementType arrayType, TypeLayout type)
        {
#if DEBUG
            ArrayElementType.debugCachedTypes[arrayType.index] = type;
#endif
        }

        [Conditional("DEBUG")]
        private static void StoreTagTypeForDebug(TagType tagType, TypeLayout type)
        {
#if DEBUG
            TagType.debugCachedTypes[tagType.index] = type;
#endif
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfTooManyComponents()
        {
            if (schema->componentCount == BitMask.MaxValue)
            {
                throw new Exception("Too many components types registered");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfTooManyArrays()
        {
            if (schema->arraysCount == BitMask.MaxValue)
            {
                throw new Exception("Too many arrays element types registered");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfTooManyTags()
        {
            if (schema->tagsCount == BitMask.MaxValue)
            {
                throw new Exception("Too many tag types registered");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfComponentTypeIsMissing(ComponentType componentType)
        {
            if (!ContainsComponentType(componentType))
            {
                throw new Exception($"Component size for `{componentType}` is missing from schema");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfComponentTypeIsMissing(int componentType)
        {
            if (!ContainsComponentType(componentType))
            {
                throw new Exception($"Component size for `{componentType}` is missing from schema");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfComponentTypeIsMissing(TypeLayout componentType)
        {
            if (!ContainsComponentType(componentType))
            {
                throw new Exception($"Component `{componentType}` is missing from schema");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfComponentTypeIsMissing<T>() where T : unmanaged
        {
            if (!ContainsComponentType<T>())
            {
                throw new Exception($"Component `{typeof(T).FullName}` is missing from schema");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfArrayTypeIsMissing(ArrayElementType arrayType)
        {
            if (!ContainsArrayType(arrayType))
            {
                throw new Exception($"Array element size for `{arrayType}` is missing from schema");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfArrayTypeIsMissing(int arrayType)
        {
            if (!ContainsArrayType(arrayType))
            {
                throw new Exception($"Array element size for `{arrayType}` is missing from schema");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfArrayTypeIsMissing(TypeLayout arrayElementType)
        {
            if (!ContainsArrayType(arrayElementType))
            {
                throw new Exception($"Array element `{arrayElementType}` is missing from schema");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfArrayTypeIsMissing<T>() where T : unmanaged
        {
            if (!ContainsArrayType<T>())
            {
                throw new Exception($"Array element `{typeof(T).FullName}` is missing from schema");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfTagIsMissing(TagType tagType)
        {
            if (!ContainsTagType(tagType))
            {
                throw new Exception($"Tag `{tagType}` is missing from schema");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfTagIsMissing(int tagType)
        {
            if (!ContainsTagType(tagType))
            {
                throw new Exception($"Tag `{tagType}` is missing from schema");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfTagIsMissing<T>() where T : unmanaged
        {
            if (!ContainsTagType<T>())
            {
                throw new Exception($"Tag `{typeof(T).FullName}` is missing from schema");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfComponentAlreadyRegistered<T>() where T : unmanaged
        {
            if (ContainsComponentType<T>())
            {
                throw new Exception($"Component `{typeof(T).FullName}` is already registered in schema");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfComponentAlreadyRegistered(TypeLayout type)
        {
            if (ContainsComponentType(type))
            {
                throw new Exception($"Component `{type}` is already registered in schema");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfArrayElementAlreadyRegistered<T>() where T : unmanaged
        {
            if (ContainsArrayType<T>())
            {
                throw new Exception($"Array element `{typeof(T).FullName}` is already registered in schema");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfArrayElementAlreadyRegistered(TypeLayout type)
        {
            if (ContainsArrayType(type))
            {
                throw new Exception($"Array element `{type}` is already registered in schema");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfTagAlreadyRegistered<T>() where T : unmanaged
        {
            if (ContainsTagType<T>())
            {
                throw new Exception($"Tag `{typeof(T).FullName}` is already registered in schema");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfTagAlreadyRegistered(TypeLayout type)
        {
            if (ContainsTagType(type))
            {
                throw new Exception($"Tag `{type}` is already registered in schema");
            }
        }

        public readonly override bool Equals(object? obj)
        {
            return obj is Schema schema && Equals(schema);
        }

        public readonly bool Equals(Schema other)
        {
            return schema == other.schema;
        }

        public readonly override int GetHashCode()
        {
            return ((nint)schema).GetHashCode();
        }

        readonly void ISerializable.Write(ByteWriter writer)
        {
            writer.WriteValue(schema->componentCount);
            writer.WriteValue(schema->arraysCount);
            writer.WriteValue(schema->tagsCount);
            writer.WriteValue(schema->definitionMask);
            Span<ushort> sizes = new(schema->sizes.Pointer, BitMask.Capacity * 2);
            Span<long> typeHashes = new(schema->typeHashes.Pointer, BitMask.Capacity * 3);
            writer.WriteSpan(sizes);
            writer.WriteSpan(typeHashes);
        }

        void ISerializable.Read(ByteReader reader)
        {
            ref Pointer pointer = ref MemoryAddress.Allocate<Pointer>();
            pointer = new(createdSchemas);
            createdSchemas++;
            fixed (Pointer* p = &pointer)
            {
                schema = p;
            }

            schema->componentCount = reader.ReadValue<byte>();
            schema->arraysCount = reader.ReadValue<byte>();
            schema->tagsCount = reader.ReadValue<byte>();
            schema->definitionMask = reader.ReadValue<Definition>();
            schema->sizes.CopyFrom<ushort>(reader.ReadSpan<ushort>(BitMask.Capacity * 2));
            schema->typeHashes.CopyFrom<long>(reader.ReadSpan<long>(BitMask.Capacity * 3));
        }

        /// <summary>
        /// Resets the schema to its <c>default</c> state.
        /// </summary>
        public readonly void Clear()
        {
            schema->componentCount = 0;
            schema->arraysCount = 0;
            schema->tagsCount = 0;
            schema->sizes.Clear(SizesLengthInBytes);
            schema->typeHashes.Clear(TypeHashesLengthInBytes);
        }

        public static bool operator ==(Schema left, Schema right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Schema left, Schema right)
        {
            return !(left == right);
        }

        internal static class TypeLayoutHashCodeCache<T> where T : unmanaged
        {
            public static readonly long value = TypeRegistry.Get<T>().Hash;
        }

        /// <summary>
        /// Cache of types per <see cref="Schema"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        internal static class SchemaTypeCache<T> where T : unmanaged
        {
            private static int[] components;
            private static int[] arrayElements;
            private static int[] tags;
            private static int componentCapacity;
            private static int arrayElementCapacity;
            private static int tagCapacity;

            static SchemaTypeCache()
            {
                components = new int[0];
                arrayElements = new int[0];
                tags = new int[0];
                componentCapacity = 0;
                arrayElementCapacity = 0;
                tagCapacity = 0;
            }

            public static void SetComponentType(Schema schema, int index)
            {
                if (schema.schema->schemaIndex >= componentCapacity)
                {
                    componentCapacity = schema.schema->schemaIndex + 1;
                    System.Array.Resize(ref components, componentCapacity);
                }

                components[schema.schema->schemaIndex] = index;
            }

            public static void SetArrayType(Schema schema, int index)
            {
                if (schema.schema->schemaIndex >= arrayElementCapacity)
                {
                    arrayElementCapacity = schema.schema->schemaIndex + 1;
                    System.Array.Resize(ref arrayElements, arrayElementCapacity);
                }

                arrayElements[schema.schema->schemaIndex] = index;
            }

            public static void SetTagType(Schema schema, int index)
            {
                if (schema.schema->schemaIndex >= tagCapacity)
                {
                    tagCapacity = schema.schema->schemaIndex + 1;
                    System.Array.Resize(ref tags, tagCapacity);
                }

                tags[schema.schema->schemaIndex] = index;
            }

            public static bool TryGetComponentType(Schema schema, out int componentType)
            {
                if (schema.schema->schemaIndex < componentCapacity)
                {
                    componentType = components[schema.schema->schemaIndex];
                    return true;
                }
                else
                {
                    componentType = default;
                    return false;
                }
            }

            public static bool TryGetArrayType(Schema schema, out int arrayElementType)
            {
                if (schema.schema->schemaIndex < arrayElementCapacity)
                {
                    arrayElementType = arrayElements[schema.schema->schemaIndex];
                    return true;
                }

                arrayElementType = default;
                return false;
            }

            public static bool TryGetTagType(Schema schema, out int tagType)
            {
                if (schema.schema->schemaIndex < tagCapacity)
                {
                    tagType = tags[schema.schema->schemaIndex];
                    return true;
                }

                tagType = default;
                return false;
            }
        }
    }
}