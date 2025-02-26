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
        internal const uint SizesLengthInBytes = sizeof(ushort) * BitMask.Capacity * 2;
        internal const uint TypeHashesLengthInBytes = sizeof(long) * BitMask.Capacity * 3;

        private static uint createdSchemas;

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
                Allocations.ThrowIfNull(schema);

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
                Allocations.ThrowIfNull(schema);

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
                Allocations.ThrowIfNull(schema);

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
                for (uint c = 0; c < BitMask.MaxValue; c++)
                {
                    if (ContainsComponent(c))
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
                for (uint a = 0; a < BitMask.Capacity; a++)
                {
                    if (ContainsArray(a))
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
                for (uint t = 0; t < BitMask.Capacity; t++)
                {
                    if (ContainsTag(t))
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
                for (uint c = 0; c < BitMask.Capacity; c++)
                {
                    if (ContainsComponent(c))
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
                for (uint a = 0; a < BitMask.Capacity; a++)
                {
                    if (ContainsArray(a))
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
                for (uint t = 0; t < BitMask.Capacity; t++)
                {
                    if (ContainsTag(t))
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
            ref Pointer schema = ref Allocations.Allocate<Pointer>();
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
            Allocations.ThrowIfNull(schema);

            schema->sizes.Dispose();
            schema->typeHashes.Dispose();
            Allocations.Free(ref schema);
        }

        /// <summary>
        /// Copies the state of this schema into the <paramref name="destination"/>.
        /// </summary>
        public readonly void CopyTo(Schema destination)
        {
            destination.schema->componentCount = schema->componentCount;
            destination.schema->arraysCount = schema->arraysCount;
            destination.schema->tagsCount = schema->tagsCount;
            destination.schema->tagsMask = schema->tagsMask;
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
            schema->tagsMask = source.schema->tagsMask;
            source.schema->sizes.CopyTo(schema->sizes, SizesLengthInBytes);
            source.schema->typeHashes.CopyTo(schema->typeHashes, TypeHashesLengthInBytes);
        }

        public readonly ushort GetSize(ComponentType componentType)
        {
            ThrowIfComponentIsMissing(componentType);

            return schema->sizes.Read<ushort>((uint)componentType * 2u);
        }

        public readonly ushort GetSize(ArrayElementType arrayElementType)
        {
            ThrowIfArrayElementIsMissing(arrayElementType);

            return schema->sizes.Read<ushort>(BitMask.Capacity * 2 + (uint)arrayElementType * 2u);
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

        public readonly DataType GetDataType(ComponentType componentType)
        {
            Allocations.ThrowIfNull(schema);
            ThrowIfComponentIsMissing(componentType);

            return new(componentType, GetSize(componentType));
        }

        public readonly DataType GetDataType(ArrayElementType arrayElementType)
        {
            Allocations.ThrowIfNull(schema);
            ThrowIfArrayElementIsMissing(arrayElementType);

            return new(arrayElementType, GetSize(arrayElementType));
        }

        public readonly DataType GetDataType(TagType tagType)
        {
            ThrowIfTagIsMissing(tagType);

            return new(tagType);
        }

        /// <summary>
        /// Retrieves the type layout for the given <paramref name="componentType"/>.
        /// </summary>
        public readonly TypeLayout GetComponentLayout(ComponentType componentType)
        {
            Allocations.ThrowIfNull(schema);
            ThrowIfComponentIsMissing(componentType);

            USpan<long> componentTypeHashes = schema->typeHashes.GetSpan<long>(BitMask.Capacity);
            long hash = componentTypeHashes[(uint)componentType];
            return TypeRegistry.Get(hash);
        }

        public readonly TypeLayout GetComponentLayout(uint index)
        {
            Allocations.ThrowIfNull(schema);
            ThrowIfComponentIsMissing(new ComponentType(index));

            USpan<long> componentTypeHashes = schema->typeHashes.GetSpan<long>(BitMask.Capacity);
            long hash = componentTypeHashes[index];
            return TypeRegistry.Get(hash);
        }

        /// <summary>
        /// Retrieves the type layout for the given <paramref name="arrayType"/>.
        /// </summary>
        public readonly TypeLayout GetArrayLayout(ArrayElementType arrayType)
        {
            Allocations.ThrowIfNull(schema);
            ThrowIfArrayElementIsMissing(arrayType);

            USpan<long> arrayTypeHashes = schema->typeHashes.AsSpan<long>(BitMask.Capacity, BitMask.Capacity);
            long hash = arrayTypeHashes[(uint)arrayType];
            return TypeRegistry.Get(hash);
        }

        public readonly TypeLayout GetArrayLayout(uint index)
        {
            Allocations.ThrowIfNull(schema);
            ThrowIfArrayElementIsMissing(new ArrayElementType(index));

            USpan<long> arrayTypeHashes = schema->typeHashes.AsSpan<long>(BitMask.Capacity, BitMask.Capacity);
            long hash = arrayTypeHashes[index];
            return TypeRegistry.Get(hash);
        }

        /// <summary>
        /// Retrieves the type layout for the given <paramref name="tagType"/>.
        /// </summary>
        public readonly TypeLayout GetTagLayout(TagType tagType)
        {
            Allocations.ThrowIfNull(schema);
            ThrowIfTagIsMissing(tagType);

            USpan<long> tagTypeHashes = schema->typeHashes.AsSpan<long>(BitMask.Capacity * 2u, BitMask.Capacity);
            long hash = tagTypeHashes[(uint)tagType];
            return TypeRegistry.Get(hash);
        }

        public readonly TypeLayout GetTagLayout(uint index)
        {
            Allocations.ThrowIfNull(schema);
            ThrowIfTagIsMissing(new TagType(index));

            USpan<long> tagTypeHashes = schema->typeHashes.AsSpan<long>(BitMask.Capacity * 2u, BitMask.Capacity);
            long hash = tagTypeHashes[index];
            return TypeRegistry.Get(hash);
        }

        public readonly TypeLayout GetComponentLayout<T>() where T : unmanaged
        {
            return GetComponentLayout(GetComponent<T>());
        }

        public readonly ComponentType RegisterComponent<T>() where T : unmanaged
        {
            ComponentType newComponentType = RegisterComponent(TypeRegistry.Get<T>());
            SchemaTypeCache<T>.Set(this, newComponentType);
            return newComponentType;
        }

        public readonly ComponentType RegisterComponent(TypeLayout type)
        {
            if (TryGetComponentType(type, out ComponentType existing))
            {
                return existing;
            }

            ThrowIfTooManyComponents();

            USpan<ushort> componentSizes = schema->sizes.GetSpan<ushort>(BitMask.Capacity);
            USpan<long> componentHashes = schema->typeHashes.GetSpan<long>(BitMask.Capacity);
            ComponentType componentType = new(schema->componentCount);
            componentSizes[(uint)componentType] = type.Size;
            componentHashes[(uint)componentType] = type.Hash;
            schema->componentCount++;
            return componentType;
        }

        public readonly ArrayElementType RegisterArrayElement<T>() where T : unmanaged
        {
            ArrayElementType arrayType = RegisterArrayElement(TypeRegistry.Get<T>());
            SchemaTypeCache<T>.Set(this, arrayType);
            return arrayType;
        }

        public readonly ArrayElementType RegisterArrayElement(TypeLayout type)
        {
            if (TryGetArrayElementType(type, out ArrayElementType existing))
            {
                return existing;
            }

            ThrowIfTooManyArrays();

            ArrayElementType arrayElementType = new(schema->arraysCount);
            USpan<ushort> arrayElementSizes = schema->sizes.AsSpan<ushort>(BitMask.Capacity, BitMask.Capacity);
            USpan<long> arrayElementHashes = schema->typeHashes.AsSpan<long>(BitMask.Capacity, BitMask.Capacity);
            arrayElementSizes[(uint)arrayElementType] = type.Size;
            arrayElementHashes[(uint)arrayElementType] = type.Hash;
            schema->arraysCount++;
            return arrayElementType;
        }

        public readonly TagType RegisterTag<T>() where T : unmanaged
        {
            TagType tagType = RegisterTag(TypeRegistry.Get<T>());
            SchemaTypeCache<T>.Set(this, tagType);
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
            USpan<long> tagHashes = schema->typeHashes.AsSpan<long>(BitMask.Capacity * 2, BitMask.Capacity);
            tagHashes[(uint)tagType] = type.Hash;
            schema->tagsMask.Set(tagType);
            schema->tagsCount++;
            return tagType;
        }

        public readonly bool Contains(ComponentType componentType)
        {
            return schema->sizes.Read<ushort>((uint)componentType * 2u) != default;
        }

        public readonly bool Contains(ArrayElementType arrayElementType)
        {
            return schema->sizes.Read<ushort>(BitMask.Capacity * 2 + (uint)arrayElementType * 2u) != default;
        }

        public readonly bool Contains(TagType tagType)
        {
            return schema->tagsMask.Contains(tagType);
        }

        public readonly bool ContainsComponent(byte index)
        {
            return schema->sizes.Read<ushort>(index * 2u) != default;
        }

        public readonly bool ContainsComponent(uint index)
        {
            return schema->sizes.Read<ushort>(index * 2u) != default;
        }

        public readonly bool ContainsArrayElement(byte index)
        {
            return schema->sizes.Read<ushort>(BitMask.Capacity * 2 + index * 2u) != default;
        }

        public readonly bool ContainsArray(uint index)
        {
            return schema->sizes.Read<ushort>(BitMask.Capacity * 2 + index * 2u) != default;
        }

        public readonly bool ContainsTag(byte index)
        {
            return schema->tagsMask.Contains(index);
        }

        public readonly bool ContainsTag(uint index)
        {
            return schema->tagsMask.Contains(index);
        }

        public readonly bool ContainsComponent(FixedString fullTypeName)
        {
            Allocations.ThrowIfNull(schema);

            USpan<long> componentTypeHashes = schema->typeHashes.GetSpan<long>(BitMask.Capacity);
            return componentTypeHashes.Contains(fullTypeName.GetLongHashCode());
        }

        public readonly bool ContainsComponent(TypeLayout type)
        {
            Allocations.ThrowIfNull(schema);

            USpan<long> componentTypeHashes = schema->typeHashes.GetSpan<long>(BitMask.Capacity);
            return componentTypeHashes.Contains(type.Hash);
        }

        public readonly bool TryGetComponentType(TypeLayout type, out ComponentType componentType)
        {
            Allocations.ThrowIfNull(schema);

            USpan<long> componentTypeHashes = schema->typeHashes.GetSpan<long>(BitMask.Capacity);
            bool contains = componentTypeHashes.TryIndexOf(type.Hash, out uint index);
            componentType = new((byte)index);
            return contains;
        }

        public readonly bool ContainsArrayElement(FixedString fullTypeName)
        {
            Allocations.ThrowIfNull(schema);

            USpan<long> arrayTypeHashes = schema->typeHashes.AsSpan<long>(BitMask.Capacity, BitMask.Capacity);
            return arrayTypeHashes.Contains(fullTypeName.GetLongHashCode());
        }

        public readonly bool ContainsArrayElement(TypeLayout type)
        {
            Allocations.ThrowIfNull(schema);

            USpan<long> arrayTypeHashes = schema->typeHashes.AsSpan<long>(BitMask.Capacity, BitMask.Capacity);
            return arrayTypeHashes.Contains(type.Hash);
        }

        public readonly bool TryGetArrayElementType(TypeLayout type, out ArrayElementType arrayElementType)
        {
            USpan<long> arrayTypeHashes = schema->typeHashes.AsSpan<long>(BitMask.Capacity, BitMask.Capacity);
            bool contains = arrayTypeHashes.TryIndexOf(type.Hash, out uint index);
            arrayElementType = new((byte)index);
            return contains;
        }

        public readonly bool ContainsTag(FixedString fullTypeName)
        {
            USpan<long> tagTypeHashes = schema->typeHashes.AsSpan<long>(BitMask.Capacity * 2, BitMask.Capacity);
            return tagTypeHashes.Contains(fullTypeName.GetLongHashCode());
        }

        public readonly bool ContainsTag(TypeLayout type)
        {
            USpan<long> tagTypeHashes = schema->typeHashes.AsSpan<long>(BitMask.Capacity * 2, BitMask.Capacity);
            return tagTypeHashes.Contains(type.Hash);
        }

        public readonly bool TryGetTagType(TypeLayout type, out TagType tagType)
        {
            USpan<long> tagTypeHashes = schema->typeHashes.AsSpan<long>(BitMask.Capacity * 2, BitMask.Capacity);
            bool contains = tagTypeHashes.TryIndexOf(type.Hash, out uint index);
            tagType = new((byte)index);
            return contains;
        }

        public readonly bool ContainsComponent<T>() where T : unmanaged
        {
            if (!TypeRegistry.IsRegistered<T>())
            {
                return false;
            }

            USpan<long> componentTypeHashes = schema->typeHashes.GetSpan<long>(BitMask.Capacity);
            return componentTypeHashes.Contains(TypeLayoutHashCodeCache<T>.value);
        }

        public readonly ComponentType GetComponent<T>() where T : unmanaged
        {
            ThrowIfComponentIsMissing<T>();

            if (!SchemaTypeCache<T>.TryGetComponent(this, out ComponentType componentType))
            {
                USpan<long> componentTypeHashes = schema->typeHashes.GetSpan<long>(BitMask.Capacity);
                uint index = componentTypeHashes.IndexOf(TypeLayoutHashCodeCache<T>.value);
                componentType = new((byte)index);
                SchemaTypeCache<T>.Set(this, componentType);
                Trace.WriteLine($"Cached component type for {typeof(T).FullName}");
            }

            return componentType;
        }

        public readonly DataType GetComponentDataType<T>() where T : unmanaged
        {
            ThrowIfComponentIsMissing<T>();

            if (!SchemaTypeCache<T>.TryGetComponent(this, out ComponentType componentType))
            {
                USpan<long> componentTypeHashes = schema->typeHashes.GetSpan<long>(BitMask.Capacity);
                uint index = componentTypeHashes.IndexOf(TypeLayoutHashCodeCache<T>.value);
                componentType = new((byte)index);
                SchemaTypeCache<T>.Set(this, componentType);
                Trace.WriteLine($"Cached component type for {typeof(T).FullName}");
            }

            return new(componentType, (ushort)sizeof(T));
        }

        public readonly DataType GetComponentDataType(TypeLayout type)
        {
            ThrowIfComponentIsMissing(type);

            USpan<long> componentTypeHashes = schema->typeHashes.GetSpan<long>(BitMask.Capacity);
            uint index = componentTypeHashes.IndexOf(type.Hash);
            return new((byte)index, DataType.Kind.Component, type.Size);
        }

        public readonly bool ContainsArrayElement<T>() where T : unmanaged
        {
            if (!TypeRegistry.IsRegistered<T>())
            {
                return false;
            }

            USpan<long> arrayTypeHashes = schema->typeHashes.AsSpan<long>(BitMask.Capacity, BitMask.Capacity);
            return arrayTypeHashes.Contains(TypeLayoutHashCodeCache<T>.value);
        }

        public readonly ArrayElementType GetArrayElement<T>() where T : unmanaged
        {
            ThrowIfArrayElementIsMissing<T>();

            if (!SchemaTypeCache<T>.TryGetArrayElement(this, out ArrayElementType arrayElementType))
            {
                USpan<long> arrayTypeHashes = schema->typeHashes.AsSpan<long>(BitMask.Capacity, BitMask.Capacity);
                uint index = arrayTypeHashes.IndexOf(TypeLayoutHashCodeCache<T>.value);
                arrayElementType = new((byte)index);
                SchemaTypeCache<T>.Set(this, arrayElementType);
                Trace.WriteLine($"Cached array element type for {typeof(T).FullName}");
            }

            return arrayElementType;
        }

        public readonly DataType GetArrayElementDataType<T>() where T : unmanaged
        {
            ThrowIfArrayElementIsMissing<T>();

            if (!SchemaTypeCache<T>.TryGetArrayElement(this, out ArrayElementType arrayElementType))
            {
                USpan<long> arrayTypeHashes = schema->typeHashes.AsSpan<long>(BitMask.Capacity, BitMask.Capacity);
                uint index = arrayTypeHashes.IndexOf(TypeLayoutHashCodeCache<T>.value);
                arrayElementType = new((byte)index);
                SchemaTypeCache<T>.Set(this, arrayElementType);
                Trace.WriteLine($"Cached array element type for {typeof(T).FullName}");
            }

            return new(arrayElementType, (ushort)sizeof(T));
        }

        public readonly DataType GetArrayElementDataType(TypeLayout type)
        {
            ThrowIfArrayElementIsMissing(type);

            USpan<long> arrayTypeHashes = schema->typeHashes.AsSpan<long>(BitMask.Capacity, BitMask.Capacity);
            uint index = arrayTypeHashes.IndexOf(type.Hash);
            return new((byte)index, DataType.Kind.ArrayElement, type.Size);
        }

        public readonly TagType GetTag<T>() where T : unmanaged
        {
            ThrowIfTagIsMissing<T>();

            if (!SchemaTypeCache<T>.TryGetTag(this, out TagType tagType))
            {
                USpan<long> tagTypeHashes = schema->typeHashes.AsSpan<long>(BitMask.Capacity * 2, BitMask.Capacity);
                uint index = tagTypeHashes.IndexOf(TypeLayoutHashCodeCache<T>.value);
                tagType = new((byte)index);
                SchemaTypeCache<T>.Set(this, tagType);
                Trace.WriteLine($"Cached tag type for {typeof(T).FullName}");
            }

            return tagType;
        }

        public readonly DataType GetTagDataType<T>() where T : unmanaged
        {
            ThrowIfTagIsMissing<T>();

            if (!SchemaTypeCache<T>.TryGetTag(this, out TagType tagType))
            {
                USpan<long> tagTypeHashes = schema->typeHashes.AsSpan<long>(BitMask.Capacity * 2, BitMask.Capacity);
                uint index = tagTypeHashes.IndexOf(TypeLayoutHashCodeCache<T>.value);
                tagType = new((byte)index);
                SchemaTypeCache<T>.Set(this, tagType);
                Trace.WriteLine($"Cached tag type for {typeof(T).FullName}");
            }

            return new(tagType);
        }

        public readonly bool ContainsTag<T>() where T : unmanaged
        {
            if (!TypeRegistry.IsRegistered<T>())
            {
                return false;
            }

            USpan<long> tagTypeHashes = schema->typeHashes.AsSpan<long>(BitMask.Capacity * 2, BitMask.Capacity);
            return tagTypeHashes.Contains(TypeLayoutHashCodeCache<T>.value);
        }

        public readonly BitMask GetComponents<T1>() where T1 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetComponent<T1>());
            return bitMask;
        }

        public readonly BitMask GetComponents<T1, T2>() where T1 : unmanaged where T2 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetComponent<T1>());
            bitMask.Set(GetComponent<T2>());
            return bitMask;
        }

        public readonly BitMask GetComponents<T1, T2, T3>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetComponent<T1>());
            bitMask.Set(GetComponent<T2>());
            bitMask.Set(GetComponent<T3>());
            return bitMask;
        }

        public readonly BitMask GetComponents<T1, T2, T3, T4>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetComponent<T1>());
            bitMask.Set(GetComponent<T2>());
            bitMask.Set(GetComponent<T3>());
            bitMask.Set(GetComponent<T4>());
            return bitMask;
        }

        public readonly BitMask GetComponents<T1, T2, T3, T4, T5>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetComponent<T1>());
            bitMask.Set(GetComponent<T2>());
            bitMask.Set(GetComponent<T3>());
            bitMask.Set(GetComponent<T4>());
            bitMask.Set(GetComponent<T5>());
            return bitMask;
        }

        public readonly BitMask GetComponents<T1, T2, T3, T4, T5, T6>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetComponent<T1>());
            bitMask.Set(GetComponent<T2>());
            bitMask.Set(GetComponent<T3>());
            bitMask.Set(GetComponent<T4>());
            bitMask.Set(GetComponent<T5>());
            bitMask.Set(GetComponent<T6>());
            return bitMask;
        }

        public readonly BitMask GetComponents<T1, T2, T3, T4, T5, T6, T7>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetComponent<T1>());
            bitMask.Set(GetComponent<T2>());
            bitMask.Set(GetComponent<T3>());
            bitMask.Set(GetComponent<T4>());
            bitMask.Set(GetComponent<T5>());
            bitMask.Set(GetComponent<T6>());
            bitMask.Set(GetComponent<T7>());
            return bitMask;
        }

        public readonly BitMask GetComponents<T1, T2, T3, T4, T5, T6, T7, T8>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetComponent<T1>());
            bitMask.Set(GetComponent<T2>());
            bitMask.Set(GetComponent<T3>());
            bitMask.Set(GetComponent<T4>());
            bitMask.Set(GetComponent<T5>());
            bitMask.Set(GetComponent<T6>());
            bitMask.Set(GetComponent<T7>());
            bitMask.Set(GetComponent<T8>());
            return bitMask;
        }

        public readonly BitMask GetComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetComponent<T1>());
            bitMask.Set(GetComponent<T2>());
            bitMask.Set(GetComponent<T3>());
            bitMask.Set(GetComponent<T4>());
            bitMask.Set(GetComponent<T5>());
            bitMask.Set(GetComponent<T6>());
            bitMask.Set(GetComponent<T7>());
            bitMask.Set(GetComponent<T8>());
            bitMask.Set(GetComponent<T9>());
            return bitMask;
        }

        public readonly BitMask GetComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetComponent<T1>());
            bitMask.Set(GetComponent<T2>());
            bitMask.Set(GetComponent<T3>());
            bitMask.Set(GetComponent<T4>());
            bitMask.Set(GetComponent<T5>());
            bitMask.Set(GetComponent<T6>());
            bitMask.Set(GetComponent<T7>());
            bitMask.Set(GetComponent<T8>());
            bitMask.Set(GetComponent<T9>());
            bitMask.Set(GetComponent<T10>());
            return bitMask;
        }

        public readonly BitMask GetComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetComponent<T1>());
            bitMask.Set(GetComponent<T2>());
            bitMask.Set(GetComponent<T3>());
            bitMask.Set(GetComponent<T4>());
            bitMask.Set(GetComponent<T5>());
            bitMask.Set(GetComponent<T6>());
            bitMask.Set(GetComponent<T7>());
            bitMask.Set(GetComponent<T8>());
            bitMask.Set(GetComponent<T9>());
            bitMask.Set(GetComponent<T10>());
            bitMask.Set(GetComponent<T11>());
            return bitMask;
        }

        public readonly BitMask GetComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetComponent<T1>());
            bitMask.Set(GetComponent<T2>());
            bitMask.Set(GetComponent<T3>());
            bitMask.Set(GetComponent<T4>());
            bitMask.Set(GetComponent<T5>());
            bitMask.Set(GetComponent<T6>());
            bitMask.Set(GetComponent<T7>());
            bitMask.Set(GetComponent<T8>());
            bitMask.Set(GetComponent<T9>());
            bitMask.Set(GetComponent<T10>());
            bitMask.Set(GetComponent<T11>());
            bitMask.Set(GetComponent<T12>());
            return bitMask;
        }

        public readonly BitMask GetComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetComponent<T1>());
            bitMask.Set(GetComponent<T2>());
            bitMask.Set(GetComponent<T3>());
            bitMask.Set(GetComponent<T4>());
            bitMask.Set(GetComponent<T5>());
            bitMask.Set(GetComponent<T6>());
            bitMask.Set(GetComponent<T7>());
            bitMask.Set(GetComponent<T8>());
            bitMask.Set(GetComponent<T9>());
            bitMask.Set(GetComponent<T10>());
            bitMask.Set(GetComponent<T11>());
            bitMask.Set(GetComponent<T12>());
            bitMask.Set(GetComponent<T13>());
            return bitMask;
        }

        public readonly BitMask GetComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetComponent<T1>());
            bitMask.Set(GetComponent<T2>());
            bitMask.Set(GetComponent<T3>());
            bitMask.Set(GetComponent<T4>());
            bitMask.Set(GetComponent<T5>());
            bitMask.Set(GetComponent<T6>());
            bitMask.Set(GetComponent<T7>());
            bitMask.Set(GetComponent<T8>());
            bitMask.Set(GetComponent<T9>());
            bitMask.Set(GetComponent<T10>());
            bitMask.Set(GetComponent<T11>());
            bitMask.Set(GetComponent<T12>());
            bitMask.Set(GetComponent<T13>());
            bitMask.Set(GetComponent<T14>());
            return bitMask;
        }

        public readonly BitMask GetComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged where T15 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetComponent<T1>());
            bitMask.Set(GetComponent<T2>());
            bitMask.Set(GetComponent<T3>());
            bitMask.Set(GetComponent<T4>());
            bitMask.Set(GetComponent<T5>());
            bitMask.Set(GetComponent<T6>());
            bitMask.Set(GetComponent<T7>());
            bitMask.Set(GetComponent<T8>());
            bitMask.Set(GetComponent<T9>());
            bitMask.Set(GetComponent<T10>());
            bitMask.Set(GetComponent<T11>());
            bitMask.Set(GetComponent<T12>());
            bitMask.Set(GetComponent<T13>());
            bitMask.Set(GetComponent<T14>());
            bitMask.Set(GetComponent<T15>());
            return bitMask;
        }

        public readonly BitMask GetComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged where T15 : unmanaged where T16 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetComponent<T1>());
            bitMask.Set(GetComponent<T2>());
            bitMask.Set(GetComponent<T3>());
            bitMask.Set(GetComponent<T4>());
            bitMask.Set(GetComponent<T5>());
            bitMask.Set(GetComponent<T6>());
            bitMask.Set(GetComponent<T7>());
            bitMask.Set(GetComponent<T8>());
            bitMask.Set(GetComponent<T9>());
            bitMask.Set(GetComponent<T10>());
            bitMask.Set(GetComponent<T11>());
            bitMask.Set(GetComponent<T12>());
            bitMask.Set(GetComponent<T13>());
            bitMask.Set(GetComponent<T14>());
            bitMask.Set(GetComponent<T15>());
            bitMask.Set(GetComponent<T16>());
            return bitMask;
        }

        public readonly BitMask GetArrayElements<T1>() where T1 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetArrayElement<T1>());
            return bitMask;
        }

        public readonly BitMask GetArrayElements<T1, T2>() where T1 : unmanaged where T2 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetArrayElement<T1>());
            bitMask.Set(GetArrayElement<T2>());
            return bitMask;
        }

        public readonly BitMask GetArrayElements<T1, T2, T3>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetArrayElement<T1>());
            bitMask.Set(GetArrayElement<T2>());
            bitMask.Set(GetArrayElement<T3>());
            return bitMask;
        }

        public readonly BitMask GetArrayElements<T1, T2, T3, T4>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetArrayElement<T1>());
            bitMask.Set(GetArrayElement<T2>());
            bitMask.Set(GetArrayElement<T3>());
            bitMask.Set(GetArrayElement<T4>());
            return bitMask;
        }

        public readonly BitMask GetArrayElements<T1, T2, T3, T4, T5>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetArrayElement<T1>());
            bitMask.Set(GetArrayElement<T2>());
            bitMask.Set(GetArrayElement<T3>());
            bitMask.Set(GetArrayElement<T4>());
            bitMask.Set(GetArrayElement<T5>());
            return bitMask;
        }

        public readonly BitMask GetArrayElements<T1, T2, T3, T4, T5, T6>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetArrayElement<T1>());
            bitMask.Set(GetArrayElement<T2>());
            bitMask.Set(GetArrayElement<T3>());
            bitMask.Set(GetArrayElement<T4>());
            bitMask.Set(GetArrayElement<T5>());
            bitMask.Set(GetArrayElement<T6>());
            return bitMask;
        }

        public readonly BitMask GetArrayElements<T1, T2, T3, T4, T5, T6, T7>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetArrayElement<T1>());
            bitMask.Set(GetArrayElement<T2>());
            bitMask.Set(GetArrayElement<T3>());
            bitMask.Set(GetArrayElement<T4>());
            bitMask.Set(GetArrayElement<T5>());
            bitMask.Set(GetArrayElement<T6>());
            bitMask.Set(GetArrayElement<T7>());
            return bitMask;
        }

        public readonly BitMask GetArrayElements<T1, T2, T3, T4, T5, T6, T7, T8>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetArrayElement<T1>());
            bitMask.Set(GetArrayElement<T2>());
            bitMask.Set(GetArrayElement<T3>());
            bitMask.Set(GetArrayElement<T4>());
            bitMask.Set(GetArrayElement<T5>());
            bitMask.Set(GetArrayElement<T6>());
            bitMask.Set(GetArrayElement<T7>());
            bitMask.Set(GetArrayElement<T8>());
            return bitMask;
        }

        public readonly BitMask GetArrayElements<T1, T2, T3, T4, T5, T6, T7, T8, T9>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetArrayElement<T1>());
            bitMask.Set(GetArrayElement<T2>());
            bitMask.Set(GetArrayElement<T3>());
            bitMask.Set(GetArrayElement<T4>());
            bitMask.Set(GetArrayElement<T5>());
            bitMask.Set(GetArrayElement<T6>());
            bitMask.Set(GetArrayElement<T7>());
            bitMask.Set(GetArrayElement<T8>());
            bitMask.Set(GetArrayElement<T9>());
            return bitMask;
        }

        public readonly BitMask GetArrayElements<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetArrayElement<T1>());
            bitMask.Set(GetArrayElement<T2>());
            bitMask.Set(GetArrayElement<T3>());
            bitMask.Set(GetArrayElement<T4>());
            bitMask.Set(GetArrayElement<T5>());
            bitMask.Set(GetArrayElement<T6>());
            bitMask.Set(GetArrayElement<T7>());
            bitMask.Set(GetArrayElement<T8>());
            bitMask.Set(GetArrayElement<T9>());
            bitMask.Set(GetArrayElement<T10>());
            return bitMask;
        }

        public readonly BitMask GetArrayElements<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetArrayElement<T1>());
            bitMask.Set(GetArrayElement<T2>());
            bitMask.Set(GetArrayElement<T3>());
            bitMask.Set(GetArrayElement<T4>());
            bitMask.Set(GetArrayElement<T5>());
            bitMask.Set(GetArrayElement<T6>());
            bitMask.Set(GetArrayElement<T7>());
            bitMask.Set(GetArrayElement<T8>());
            bitMask.Set(GetArrayElement<T9>());
            bitMask.Set(GetArrayElement<T10>());
            bitMask.Set(GetArrayElement<T11>());
            return bitMask;
        }

        public readonly BitMask GetArrayElements<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetArrayElement<T1>());
            bitMask.Set(GetArrayElement<T2>());
            bitMask.Set(GetArrayElement<T3>());
            bitMask.Set(GetArrayElement<T4>());
            bitMask.Set(GetArrayElement<T5>());
            bitMask.Set(GetArrayElement<T6>());
            bitMask.Set(GetArrayElement<T7>());
            bitMask.Set(GetArrayElement<T8>());
            bitMask.Set(GetArrayElement<T9>());
            bitMask.Set(GetArrayElement<T10>());
            bitMask.Set(GetArrayElement<T11>());
            bitMask.Set(GetArrayElement<T12>());
            return bitMask;
        }

        public readonly BitMask GetTags<T1>() where T1 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetTag<T1>());
            return bitMask;
        }

        public readonly BitMask GetTags<T1, T2>() where T1 : unmanaged where T2 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetTag<T1>());
            bitMask.Set(GetTag<T2>());
            return bitMask;
        }

        public readonly BitMask GetTags<T1, T2, T3>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetTag<T1>());
            bitMask.Set(GetTag<T2>());
            bitMask.Set(GetTag<T3>());
            return bitMask;
        }

        public readonly BitMask GetTags<T1, T2, T3, T4>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetTag<T1>());
            bitMask.Set(GetTag<T2>());
            bitMask.Set(GetTag<T3>());
            bitMask.Set(GetTag<T4>());
            return bitMask;
        }

        public readonly BitMask GetTags<T1, T2, T3, T4, T5>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetTag<T1>());
            bitMask.Set(GetTag<T2>());
            bitMask.Set(GetTag<T3>());
            bitMask.Set(GetTag<T4>());
            bitMask.Set(GetTag<T5>());
            return bitMask;
        }

        public readonly BitMask GetTags<T1, T2, T3, T4, T5, T6>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetTag<T1>());
            bitMask.Set(GetTag<T2>());
            bitMask.Set(GetTag<T3>());
            bitMask.Set(GetTag<T4>());
            bitMask.Set(GetTag<T5>());
            bitMask.Set(GetTag<T6>());
            return bitMask;
        }

        public readonly BitMask GetTags<T1, T2, T3, T4, T5, T6, T7>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetTag<T1>());
            bitMask.Set(GetTag<T2>());
            bitMask.Set(GetTag<T3>());
            bitMask.Set(GetTag<T4>());
            bitMask.Set(GetTag<T5>());
            bitMask.Set(GetTag<T6>());
            bitMask.Set(GetTag<T7>());
            return bitMask;
        }

        public readonly BitMask GetTags<T1, T2, T3, T4, T5, T6, T7, T8>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetTag<T1>());
            bitMask.Set(GetTag<T2>());
            bitMask.Set(GetTag<T3>());
            bitMask.Set(GetTag<T4>());
            bitMask.Set(GetTag<T5>());
            bitMask.Set(GetTag<T6>());
            bitMask.Set(GetTag<T7>());
            bitMask.Set(GetTag<T8>());
            return bitMask;
        }

        public readonly BitMask GetTags<T1, T2, T3, T4, T5, T6, T7, T8, T9>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetTag<T1>());
            bitMask.Set(GetTag<T2>());
            bitMask.Set(GetTag<T3>());
            bitMask.Set(GetTag<T4>());
            bitMask.Set(GetTag<T5>());
            bitMask.Set(GetTag<T6>());
            bitMask.Set(GetTag<T7>());
            bitMask.Set(GetTag<T8>());
            bitMask.Set(GetTag<T9>());
            return bitMask;
        }

        public readonly BitMask GetTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetTag<T1>());
            bitMask.Set(GetTag<T2>());
            bitMask.Set(GetTag<T3>());
            bitMask.Set(GetTag<T4>());
            bitMask.Set(GetTag<T5>());
            bitMask.Set(GetTag<T6>());
            bitMask.Set(GetTag<T7>());
            bitMask.Set(GetTag<T8>());
            bitMask.Set(GetTag<T9>());
            bitMask.Set(GetTag<T10>());
            return bitMask;
        }

        public readonly BitMask GetTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetTag<T1>());
            bitMask.Set(GetTag<T2>());
            bitMask.Set(GetTag<T3>());
            bitMask.Set(GetTag<T4>());
            bitMask.Set(GetTag<T5>());
            bitMask.Set(GetTag<T6>());
            bitMask.Set(GetTag<T7>());
            bitMask.Set(GetTag<T8>());
            bitMask.Set(GetTag<T9>());
            bitMask.Set(GetTag<T10>());
            bitMask.Set(GetTag<T11>());
            return bitMask;
        }

        public readonly BitMask GetTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetTag<T1>());
            bitMask.Set(GetTag<T2>());
            bitMask.Set(GetTag<T3>());
            bitMask.Set(GetTag<T4>());
            bitMask.Set(GetTag<T5>());
            bitMask.Set(GetTag<T6>());
            bitMask.Set(GetTag<T7>());
            bitMask.Set(GetTag<T8>());
            bitMask.Set(GetTag<T9>());
            bitMask.Set(GetTag<T10>());
            bitMask.Set(GetTag<T11>());
            bitMask.Set(GetTag<T12>());
            return bitMask;
        }

        public static Schema Create()
        {
            ref Pointer schema = ref Allocations.Allocate<Pointer>();
            schema = new(createdSchemas);
            createdSchemas++;

            fixed (Pointer* pointer = &schema)
            {
                return new(pointer);
            }
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
        private readonly void ThrowIfComponentIsMissing(ComponentType componentType)
        {
            ushort componentSize = schema->sizes.Read<ushort>((uint)componentType * 2u);
            if (componentSize == default)
            {
                throw new Exception($"Component size for `{componentType}` is missing from schema");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfComponentIsMissing(TypeLayout componentType)
        {
            if (!ContainsComponent(componentType))
            {
                throw new Exception($"Component `{componentType}` is missing from schema");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfComponentIsMissing<T>() where T : unmanaged
        {
            if (!ContainsComponent<T>())
            {
                throw new Exception($"Component `{typeof(T).FullName}` is missing from schema");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfArrayElementIsMissing(ArrayElementType arrayElementTypes)
        {
            ushort arrayElementSize = schema->sizes.Read<ushort>(BitMask.Capacity * 2 + (uint)arrayElementTypes * 2u);
            if (arrayElementSize == default)
            {
                throw new Exception($"Array element size for `{arrayElementTypes}` is missing from schema");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfArrayElementIsMissing(TypeLayout arrayElementType)
        {
            if (!ContainsArrayElement(arrayElementType))
            {
                throw new Exception($"Array element `{arrayElementType}` is missing from schema");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfArrayElementIsMissing<T>() where T : unmanaged
        {
            if (!ContainsArrayElement<T>())
            {
                throw new Exception($"Array element `{typeof(T).FullName}` is missing from schema");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfTagIsMissing(TagType tagType)
        {
            if (!Contains(tagType))
            {
                throw new Exception($"Tag `{tagType}` is missing from schema");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfTagIsMissing<T>() where T : unmanaged
        {
            if (!ContainsTag<T>())
            {
                throw new Exception($"Tag `{typeof(T).FullName}` is missing from schema");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfComponentAlreadyRegistered<T>() where T : unmanaged
        {
            if (ContainsComponent<T>())
            {
                throw new Exception($"Component `{typeof(T).FullName}` is already registered in schema");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfComponentAlreadyRegistered(TypeLayout type)
        {
            if (ContainsComponent(type))
            {
                throw new Exception($"Component `{type}` is already registered in schema");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfArrayElementAlreadyRegistered<T>() where T : unmanaged
        {
            if (ContainsArrayElement<T>())
            {
                throw new Exception($"Array element `{typeof(T).FullName}` is already registered in schema");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfArrayElementAlreadyRegistered(TypeLayout type)
        {
            if (ContainsArrayElement(type))
            {
                throw new Exception($"Array element `{type}` is already registered in schema");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfTagAlreadyRegistered<T>() where T : unmanaged
        {
            if (ContainsTag<T>())
            {
                throw new Exception($"Tag `{typeof(T).FullName}` is already registered in schema");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfTagAlreadyRegistered(TypeLayout type)
        {
            if (ContainsTag(type))
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
            writer.WriteValue(schema->tagsMask);
            USpan<ushort> sizes = schema->sizes.GetSpan<ushort>(BitMask.Capacity * 2);
            USpan<long> typeHashes = schema->typeHashes.GetSpan<long>(BitMask.Capacity * 3);
            writer.WriteSpan(sizes);
            writer.WriteSpan(typeHashes);
        }

        void ISerializable.Read(ByteReader reader)
        {
            ref Pointer pointer = ref Allocations.Allocate<Pointer>();
            pointer = new(createdSchemas);
            createdSchemas++;
            fixed (Pointer* p = &pointer)
            {
                schema = p;
            }

            schema->componentCount = reader.ReadValue<byte>();
            schema->arraysCount = reader.ReadValue<byte>();
            schema->tagsCount = reader.ReadValue<byte>();
            schema->tagsMask = reader.ReadValue<BitMask>();
            schema->sizes.CopyFrom(reader.ReadSpan<ushort>(BitMask.Capacity * 2), SizesLengthInBytes);
            schema->typeHashes.CopyFrom(reader.ReadSpan<long>(BitMask.Capacity * 3), TypeHashesLengthInBytes);
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
            private static ComponentType[] components;
            private static ArrayElementType[] arrayElements;
            private static TagType[] tags;
            private static uint componentCapacity;
            private static uint arrayElementCapacity;
            private static uint tagCapacity;

            static SchemaTypeCache()
            {
                components = new ComponentType[0];
                arrayElements = new ArrayElementType[0];
                tags = new TagType[0];
                componentCapacity = 0;
                arrayElementCapacity = 0;
                tagCapacity = 0;
            }

            public static void Set(Schema schema, ComponentType componentType)
            {
                if (schema.schema->schemaIndex >= componentCapacity)
                {
                    componentCapacity = schema.schema->schemaIndex + 1;
                    System.Array.Resize(ref components, (int)componentCapacity);
                }

                components[schema.schema->schemaIndex] = componentType;
            }

            public static void Set(Schema schema, ArrayElementType arrayElementType)
            {
                if (schema.schema->schemaIndex >= arrayElementCapacity)
                {
                    arrayElementCapacity = schema.schema->schemaIndex + 1;
                    System.Array.Resize(ref arrayElements, (int)arrayElementCapacity);
                }

                arrayElements[schema.schema->schemaIndex] = arrayElementType;
            }

            public static void Set(Schema schema, TagType tagType)
            {
                if (schema.schema->schemaIndex >= tagCapacity)
                {
                    tagCapacity = schema.schema->schemaIndex + 1;
                    System.Array.Resize(ref tags, (int)tagCapacity);
                }

                tags[schema.schema->schemaIndex] = tagType;
            }

            public static bool TryGetComponent(Schema schema, out ComponentType componentType)
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

            public static bool TryGetArrayElement(Schema schema, out ArrayElementType arrayElementType)
            {
                if (schema.schema->schemaIndex < arrayElementCapacity)
                {
                    arrayElementType = arrayElements[schema.schema->schemaIndex];
                    return true;
                }

                arrayElementType = default;
                return false;
            }

            public static bool TryGetTag(Schema schema, out TagType tagType)
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