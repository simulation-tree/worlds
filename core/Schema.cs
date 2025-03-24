using System;
using System.Diagnostics;
using Types;
using Unmanaged;
using Worlds.Pointers;

namespace Worlds
{
    /// <summary>
    /// Describes the components, arrays, and tags that
    /// may or may not be used with a <see cref="World"/>.
    /// </summary>
    public unsafe struct Schema : IDisposable, IEquatable<Schema>, ISerializable
    {
        /// <summary>
        /// The reserved tag to describe disabled entities.
        /// </summary>
        public const int DisabledTagType = BitMask.MaxValue;

        internal const int OffsetsLengthInBytes = sizeof(int) * BitMask.Capacity;
        internal const int SizesLengthInBytes = sizeof(int) * BitMask.Capacity * 2;
        internal const int TypeHashesLengthInBytes = sizeof(long) * BitMask.Capacity * 3;

        private static int createdSchemas;

        private SchemaPointer* schema;

        /// <summary>
        /// Checks if this schema is disposed.
        /// </summary>
        public readonly bool IsDisposed => schema is null;

        /// <summary>
        /// The pointer to the schema memory address in the unmanaged heap.
        /// </summary>
        public readonly void* Pointer => schema;

        /// <summary>
        /// The native address of the schema in the unmanaged heap.
        /// </summary>
        public readonly nint Address => (nint)schema;

        /// <summary>
        /// How many component types have been registered in the schema.
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
        /// How many array types have been registered.
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
        /// How many tag types have been registered.
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
        /// The size of a row of components in bytes.
        /// </summary>
        public readonly int ComponentRowSize
        {
            get
            {
                MemoryAddress.ThrowIfDefault(schema);

                return schema->componentRowSize;
            }
        }

        /// <summary>
        /// All component types loaded.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly System.Collections.Generic.IEnumerable<int> ComponentTypes
        {
            get
            {
                for (int c = 0; c < BitMask.MaxValue; c++)
                {
                    if (ContainsComponentType(c))
                    {
                        yield return c;
                    }
                }
            }
        }

        /// <summary>
        /// All array types loaded.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly System.Collections.Generic.IEnumerable<int> ArrayTypes
        {
            get
            {
                for (int a = 0; a < BitMask.Capacity; a++)
                {
                    if (ContainsArrayType(a))
                    {
                        yield return a;
                    }
                }
            }
        }

        /// <summary>
        /// All tag types loaded.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly System.Collections.Generic.IEnumerable<int> TagTypes
        {
            get
            {
                for (int t = 0; t < BitMask.Capacity; t++)
                {
                    if (ContainsTagType(t))
                    {
                        yield return t;
                    }
                }
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        private readonly Types.Type[] Components
        {
            get
            {
                Types.Type[] buffer = new Types.Type[schema->componentCount];
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
        private readonly Types.Type[] Arrays
        {
            get
            {
                Types.Type[] buffer = new Types.Type[schema->arraysCount];
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
        private readonly Types.Type[] Tags
        {
            get
            {
                Types.Type[] buffer = new Types.Type[schema->tagsCount];
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
            schema = MemoryAddress.AllocatePointer<SchemaPointer>();
            schema->componentCount = 0;
            schema->arraysCount = 0;
            schema->tagsCount = 0;
            schema->componentRowSize = 0;
            schema->definitionMask = default;
            schema->offsets = MemoryAddress.AllocateZeroed(OffsetsLengthInBytes);
            schema->sizes = MemoryAddress.AllocateZeroed(SizesLengthInBytes);
            schema->typeHashes = MemoryAddress.AllocateZeroed(TypeHashesLengthInBytes);
            schema->schemaIndex = createdSchemas;
            createdSchemas++;
        }
#endif

        /// <summary>
        /// Initializes an existing schema from the given <paramref name="pointer"/>.
        /// </summary>
        public Schema(void* pointer)
        {
            schema = (SchemaPointer*)pointer;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            MemoryAddress.ThrowIfDefault(schema);

            schema->offsets.Dispose();
            schema->sizes.Dispose();
            schema->typeHashes.Dispose();
            MemoryAddress.Free(ref schema);
        }

        internal readonly void CopyFrom(Schema source)
        {
            schema->componentCount = source.schema->componentCount;
            schema->arraysCount = source.schema->arraysCount;
            schema->tagsCount = source.schema->tagsCount;
            schema->definitionMask = source.schema->definitionMask;
            schema->componentRowSize = source.schema->componentRowSize;
            schema->offsets.CopyFrom(source.schema->offsets, OffsetsLengthInBytes);
            source.schema->sizes.CopyTo(schema->sizes, SizesLengthInBytes);
            source.schema->typeHashes.CopyTo(schema->typeHashes, TypeHashesLengthInBytes);
        }

        /// <summary>
        /// Retrieves the size of <paramref name="componentType"/> in bytes.
        /// </summary>
        public readonly int GetComponentSize(int componentType)
        {
            return schema->sizes.ReadElement<int>(componentType);
        }

        /// <summary>
        /// Retrieves the size of each <paramref name="arrayType"/> element in bytes.
        /// </summary>
        public readonly int GetArraySize(int arrayType)
        {
            return schema->sizes.ReadElement<int>(BitMask.Capacity + arrayType);
        }

        /// <summary>
        /// Retrieves the position in bytes where component of type <typeparamref name="T"/>
        /// would start in a chunk component row.
        /// </summary>
        public readonly int GetComponentOffset<T>() where T : unmanaged
        {
            ThrowIfComponentTypeIsMissing<T>();

            return schema->offsets.ReadElement<int>(GetComponentType<T>());
        }

        /// <summary>
        /// Retrieves the position in bytes where <paramref name="componentType"/>
        /// would start in a chunk component row.
        /// </summary>
        public readonly int GetComponentOffset(int componentType)
        {
            return schema->offsets.ReadElement<int>(componentType);
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

        /// <summary>
        /// Retrieves the type information for <paramref name="componentType"/>.
        /// </summary>
        public readonly DataType GetComponentDataType(int componentType)
        {
            MemoryAddress.ThrowIfDefault(schema);
            ThrowIfComponentTypeIsMissing(componentType);

            return new(componentType, DataType.Kind.Component, GetComponentSize(componentType));
        }

        /// <summary>
        /// Retrieves the type information for <paramref name="arrayType"/>.
        /// </summary>
        public readonly DataType GetArrayDataType(int arrayType)
        {
            MemoryAddress.ThrowIfDefault(schema);
            ThrowIfArrayTypeIsMissing(arrayType);

            return new(arrayType, DataType.Kind.Array, GetArraySize(arrayType));
        }

        /// <summary>
        /// Retrieves the type information for <paramref name="tagType"/>.
        /// </summary>
        public readonly DataType GetTagDataType(int tagType)
        {
            ThrowIfTagIsMissing(tagType);

            return new(tagType, DataType.Kind.Tag, 1);
        }

        /// <summary>
        /// Retrieves the type layout for the given <paramref name="componentType"/>.
        /// </summary>
        public readonly Types.Type GetComponentLayout(int componentType)
        {
            MemoryAddress.ThrowIfDefault(schema);
            ThrowIfComponentTypeIsMissing(componentType);

            Span<long> componentTypeHashes = new(schema->typeHashes.Pointer, BitMask.Capacity);
            long hash = componentTypeHashes[componentType];
            return MetadataRegistry.GetType(hash);
        }

        /// <summary>
        /// Retrieves the type layout for the given <paramref name="arrayType"/>.
        /// </summary>
        public readonly Types.Type GetArrayLayout(int arrayType)
        {
            MemoryAddress.ThrowIfDefault(schema);
            ThrowIfArrayTypeIsMissing(arrayType);

            Span<long> arrayTypeHashes = schema->typeHashes.AsSpan<long>(BitMask.Capacity, BitMask.Capacity);
            long hash = arrayTypeHashes[arrayType];
            return MetadataRegistry.GetType(hash);
        }

        /// <summary>
        /// Retrieves the type layout for the given <paramref name="tagType"/>.
        /// </summary>
        public readonly Types.Type GetTagLayout(int tagType)
        {
            MemoryAddress.ThrowIfDefault(schema);
            ThrowIfTagIsMissing(tagType);

            Span<long> tagTypeHashes = schema->typeHashes.AsSpan<long>(BitMask.Capacity * 2, BitMask.Capacity);
            long hash = tagTypeHashes[tagType];
            return MetadataRegistry.GetType(hash);
        }

        /// <summary>
        /// Retrieves the type metadata for the component of type <typeparamref name="T"/>.
        /// </summary>
        public readonly Types.Type GetComponentLayout<T>() where T : unmanaged
        {
            return GetComponentLayout(GetComponentType<T>());
        }

        /// <summary>
        /// Registers <typeparamref name="T"/> as a component and 
        /// retrieves its index in the schema.
        /// </summary>
        public readonly int RegisterComponent<T>() where T : unmanaged
        {
            int newComponentType = RegisterComponent(MetadataRegistry.GetOrRegisterType<T>());
            SchemaTypeCache<T>.SetComponentType(this, newComponentType);
            return newComponentType;
        }

        /// <summary>
        /// Registers <paramref name="type"/> as a component and
        /// retrieves its index in the schema.
        /// </summary>
        public readonly int RegisterComponent(Types.Type type)
        {
            if (TryGetComponentType(type, out int componentType))
            {
                return componentType;
            }

            ThrowIfTooManyComponents();

            componentType = schema->componentCount;
            schema->offsets.WriteElement(componentType, schema->componentRowSize);
            schema->sizes.WriteElement(componentType, (int)type.size);
            schema->typeHashes.WriteElement(componentType, type.Hash);
            schema->componentCount++;
            schema->componentRowSize += type.size;
            schema->definitionMask.AddComponentType(componentType);
            return componentType;
        }

        /// <summary>
        /// Registers <typeparamref name="T"/> as an array and 
        /// retrieves its index in the schema.
        /// </summary>
        public readonly int RegisterArray<T>() where T : unmanaged
        {
            int arrayType = RegisterArray(MetadataRegistry.GetOrRegisterType<T>());
            SchemaTypeCache<T>.SetArrayType(this, arrayType);
            return arrayType;
        }

        /// <summary>
        /// Registers <paramref name="type"/> as an array and
        /// retrieves its index in the schema.
        /// </summary>
        public readonly int RegisterArray(Types.Type type)
        {
            if (TryGetArrayType(type, out int arrayType))
            {
                return arrayType;
            }

            ThrowIfTooManyArrays();

            arrayType = schema->arraysCount;
            Span<int> arraySizes = schema->sizes.AsSpan<int>(BitMask.Capacity, BitMask.Capacity);
            Span<long> arrayHashes = schema->typeHashes.AsSpan<long>(BitMask.Capacity, BitMask.Capacity);
            arraySizes[schema->arraysCount] = type.size;
            arrayHashes[schema->arraysCount] = type.Hash;
            schema->arraysCount++;
            schema->definitionMask.AddArrayType(arrayType);
            return arrayType;
        }

        /// <summary>
        /// Registers <typeparamref name="T"/> as a tag and 
        /// retrieves its index in the schema.
        /// </summary>
        public readonly int RegisterTag<T>() where T : unmanaged
        {
            int tagType = RegisterTag(MetadataRegistry.GetOrRegisterType<T>());
            SchemaTypeCache<T>.SetTagType(this, tagType);
            return tagType;
        }

        /// <summary>
        /// Registers <paramref name="type"/> as a tag and
        /// retrieves its index in the schema.
        /// </summary>
        public readonly int RegisterTag(Types.Type type)
        {
            if (TryGetTagType(type, out int tagType))
            {
                return tagType;
            }

            ThrowIfTooManyTags();

            tagType = schema->tagsCount;
            Span<long> tagHashes = schema->typeHashes.AsSpan<long>(BitMask.Capacity * 2, BitMask.Capacity);
            tagHashes[tagType] = type.Hash;
            schema->definitionMask.AddTagType(tagType);
            schema->tagsCount++;
            return tagType;
        }

        /// <summary>
        /// Checks if <paramref name="componentType"/> has been registered.
        /// </summary>
        public readonly bool ContainsComponentType(int componentType)
        {
            MemoryAddress.ThrowIfDefault(schema);

            return schema->definitionMask.componentTypes.Contains(componentType);
        }

        /// <summary>
        /// Checks if <paramref name="arrayType"/> has been registered.
        /// </summary>
        public readonly bool ContainsArrayType(int arrayType)
        {
            MemoryAddress.ThrowIfDefault(schema);

            return schema->definitionMask.arrayTypes.Contains(arrayType);
        }

        /// <summary>
        /// Checks if <paramref name="tagType"/> has been registered.
        /// </summary>
        public readonly bool ContainsTagType(int tagType)
        {
            MemoryAddress.ThrowIfDefault(schema);

            return schema->definitionMask.tagTypes.Contains(tagType);
        }

        /// <summary>
        /// Checks if a component with <paramref name="fullTypeName"/> has been registered.
        /// </summary>
        public readonly bool ContainsComponentType(ASCIIText256 fullTypeName)
        {
            MemoryAddress.ThrowIfDefault(schema);

            Span<long> componentTypeHashes = new(schema->typeHashes.Pointer, BitMask.Capacity);
            return componentTypeHashes.Contains(fullTypeName.GetLongHashCode());
        }

        /// <summary>
        /// Checks if <paramref name="type"/> is a registered component.
        /// </summary>
        public readonly bool ContainsComponentType(Types.Type type)
        {
            MemoryAddress.ThrowIfDefault(schema);

            Span<long> componentTypeHashes = new(schema->typeHashes.Pointer, BitMask.Capacity);
            return componentTypeHashes.Contains(type.Hash);
        }

        /// <summary>
        /// Tries to retrieve the index of the component type <paramref name="type"/>.
        /// </summary>
        public readonly bool TryGetComponentType(Types.Type type, out int componentType)
        {
            MemoryAddress.ThrowIfDefault(schema);

            Span<long> componentTypeHashes = new(schema->typeHashes.Pointer, BitMask.Capacity);
            return componentTypeHashes.TryIndexOf(type.Hash, out componentType);
        }

        /// <summary>
        /// Checks if an array with <paramref name="fullTypeName"/> has been registered.
        /// </summary>
        public readonly bool ContainsArrayType(ASCIIText256 fullTypeName)
        {
            MemoryAddress.ThrowIfDefault(schema);

            Span<long> arrayTypeHashes = schema->typeHashes.AsSpan<long>(BitMask.Capacity, BitMask.Capacity);
            return arrayTypeHashes.Contains(fullTypeName.GetLongHashCode());
        }

        /// <summary>
        /// Checks if <paramref name="type"/> is a registered array.
        /// </summary>
        public readonly bool ContainsArrayType(Types.Type type)
        {
            MemoryAddress.ThrowIfDefault(schema);

            Span<long> arrayTypeHashes = schema->typeHashes.AsSpan<long>(BitMask.Capacity, BitMask.Capacity);
            return arrayTypeHashes.Contains(type.Hash);
        }

        /// <summary>
        /// Tries to retrieve the index of the array type <paramref name="type"/>.
        /// </summary>
        public readonly bool TryGetArrayType(Types.Type type, out int arrayType)
        {
            Span<long> arrayTypeHashes = schema->typeHashes.AsSpan<long>(BitMask.Capacity, BitMask.Capacity);
            return arrayTypeHashes.TryIndexOf(type.Hash, out arrayType);
        }

        /// <summary>
        /// Checks if a tag type with <paramref name="fullTypeName"/> has been registered.
        /// </summary>
        public readonly bool ContainsTagType(ASCIIText256 fullTypeName)
        {
            Span<long> tagTypeHashes = schema->typeHashes.AsSpan<long>(BitMask.Capacity * 2, BitMask.Capacity);
            return tagTypeHashes.Contains(fullTypeName.GetLongHashCode());
        }

        /// <summary>
        /// Checks if <paramref name="type"/> is a registered tag.
        /// </summary>
        public readonly bool ContainsTagType(Types.Type type)
        {
            Span<long> tagTypeHashes = schema->typeHashes.AsSpan<long>(BitMask.Capacity * 2, BitMask.Capacity);
            return tagTypeHashes.Contains(type.Hash);
        }

        /// <summary>
        /// Tries to retrieve the index of the tag type <paramref name="type"/>.
        /// </summary>
        public readonly bool TryGetTagType(Types.Type type, out int tagType)
        {
            Span<long> tagTypeHashes = schema->typeHashes.AsSpan<long>(BitMask.Capacity * 2, BitMask.Capacity);
            return tagTypeHashes.TryIndexOf(type.Hash, out tagType);
        }

        /// <summary>
        /// Checks if a registered component of type <typeparamref name="T"/>
        /// has been registered.
        /// </summary>
        public readonly bool ContainsComponentType<T>() where T : unmanaged
        {
            if (!MetadataRegistry.IsTypeRegistered<T>())
            {
                return false;
            }

            Span<long> componentTypeHashes = new(schema->typeHashes.Pointer, BitMask.Capacity);
            return componentTypeHashes.Contains(TypeLayoutHashCodeCache<T>.value);
        }

        /// <summary>
        /// Retrieves the index of the component type <typeparamref name="T"/>.
        /// </summary>
        public readonly int GetComponentType<T>() where T : unmanaged
        {
            ThrowIfComponentTypeIsMissing<T>();

            return SchemaTypeCache<T>.GetOrSetComponentType(this);
        }

        /// <summary>
        /// Retrieves the type information for component of type <typeparamref name="T"/>.
        /// </summary>
        public readonly DataType GetComponentDataType<T>() where T : unmanaged
        {
            ThrowIfComponentTypeIsMissing<T>();

            return new(SchemaTypeCache<T>.GetOrSetComponentType(this), DataType.Kind.Component, sizeof(T));
        }

        /// <summary>
        /// Retrieves the type information for component of <paramref name="type"/>.
        /// </summary>
        public readonly DataType GetComponentDataType(Types.Type type)
        {
            ThrowIfComponentTypeIsMissing(type);

            Span<long> componentTypeHashes = new(schema->typeHashes.Pointer, BitMask.Capacity);
            int componentType = componentTypeHashes.IndexOf(type.Hash);
            return new(componentType, DataType.Kind.Component, type.size);
        }

        /// <summary>
        /// Checks if an array of type <typeparamref name="T"/> has been registered.
        /// </summary>
        public readonly bool ContainsArrayType<T>() where T : unmanaged
        {
            if (!MetadataRegistry.IsTypeRegistered<T>())
            {
                return false;
            }

            Span<long> arrayTypeHashes = schema->typeHashes.AsSpan<long>(BitMask.Capacity, BitMask.Capacity);
            return arrayTypeHashes.Contains(TypeLayoutHashCodeCache<T>.value);
        }

        /// <summary>
        /// Retrieves the index of the array type <typeparamref name="T"/>.
        /// </summary>
        public readonly int GetArrayType<T>() where T : unmanaged
        {
            ThrowIfArrayTypeIsMissing<T>();

            return SchemaTypeCache<T>.GetOrSetArrayType(this);
        }

        /// <summary>
        /// Retrieves the type information for array of type <typeparamref name="T"/>.
        /// </summary>
        public readonly DataType GetArrayDataType<T>() where T : unmanaged
        {
            ThrowIfArrayTypeIsMissing<T>();

            return new(SchemaTypeCache<T>.GetOrSetArrayType(this), DataType.Kind.Array, sizeof(T));
        }

        /// <summary>
        /// Retrieves the type information for array of <paramref name="type"/>.
        /// </summary>
        public readonly DataType GetArrayDataType(Types.Type type)
        {
            ThrowIfArrayTypeIsMissing(type);

            Span<long> arrayTypeHashes = schema->typeHashes.AsSpan<long>(BitMask.Capacity, BitMask.Capacity);
            int arrayType = arrayTypeHashes.IndexOf(type.Hash);
            return new(arrayType, DataType.Kind.Array, type.size);
        }

        /// <summary>
        /// Retrieves the index of the tag type <typeparamref name="T"/>.
        /// </summary>
        public readonly int GetTagType<T>() where T : unmanaged
        {
            ThrowIfTagIsMissing<T>();

            return SchemaTypeCache<T>.GetOrSetTagType(this);
        }

        /// <summary>
        /// Retrieves the type information for tag of type <typeparamref name="T"/>.
        /// </summary>
        public readonly DataType GetTagDataType<T>() where T : unmanaged
        {
            ThrowIfTagIsMissing<T>();

            return new(SchemaTypeCache<T>.GetOrSetTagType(this), DataType.Kind.Tag, 1);
        }

        /// <summary>
        /// Checks if a registered tag of type <typeparamref name="T"/>
        /// exists.
        /// </summary>
        public readonly bool ContainsTagType<T>() where T : unmanaged
        {
            if (!MetadataRegistry.IsTypeRegistered<T>())
            {
                return false;
            }

            Span<long> tagTypeHashes = schema->typeHashes.AsSpan<long>(BitMask.Capacity * 2, BitMask.Capacity);
            return tagTypeHashes.Contains(TypeLayoutHashCodeCache<T>.value);
        }

        /// <summary>
        /// Retrieves a mask of the specified component types.
        /// </summary>
        public readonly BitMask GetComponentTypes<T1>() where T1 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetComponentType<T1>());
            return bitMask;
        }

        /// <summary>
        /// Retrieves a mask of the specified component types.
        /// </summary>
        public readonly BitMask GetComponentTypes<T1, T2>() where T1 : unmanaged where T2 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetComponentType<T1>());
            bitMask.Set(GetComponentType<T2>());
            return bitMask;
        }

        /// <summary>
        /// Retrieves a mask of the specified component types.
        /// </summary>
        public readonly BitMask GetComponentTypes<T1, T2, T3>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetComponentType<T1>());
            bitMask.Set(GetComponentType<T2>());
            bitMask.Set(GetComponentType<T3>());
            return bitMask;
        }

        /// <summary>
        /// Retrieves a mask of the specified component types.
        /// </summary>
        public readonly BitMask GetComponentTypes<T1, T2, T3, T4>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetComponentType<T1>());
            bitMask.Set(GetComponentType<T2>());
            bitMask.Set(GetComponentType<T3>());
            bitMask.Set(GetComponentType<T4>());
            return bitMask;
        }

        /// <summary>
        /// Retrieves a mask of the specified component types.
        /// </summary>
        public readonly BitMask GetComponentTypes<T1, T2, T3, T4, T5>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetComponentType<T1>());
            bitMask.Set(GetComponentType<T2>());
            bitMask.Set(GetComponentType<T3>());
            bitMask.Set(GetComponentType<T4>());
            bitMask.Set(GetComponentType<T5>());
            return bitMask;
        }

        /// <summary>
        /// Retrieves a mask of the specified component types.
        /// </summary>
        public readonly BitMask GetComponentTypes<T1, T2, T3, T4, T5, T6>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetComponentType<T1>());
            bitMask.Set(GetComponentType<T2>());
            bitMask.Set(GetComponentType<T3>());
            bitMask.Set(GetComponentType<T4>());
            bitMask.Set(GetComponentType<T5>());
            bitMask.Set(GetComponentType<T6>());
            return bitMask;
        }

        /// <summary>
        /// Retrieves a mask of the specified component types.
        /// </summary>
        public readonly BitMask GetComponentTypes<T1, T2, T3, T4, T5, T6, T7>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetComponentType<T1>());
            bitMask.Set(GetComponentType<T2>());
            bitMask.Set(GetComponentType<T3>());
            bitMask.Set(GetComponentType<T4>());
            bitMask.Set(GetComponentType<T5>());
            bitMask.Set(GetComponentType<T6>());
            bitMask.Set(GetComponentType<T7>());
            return bitMask;
        }

        /// <summary>
        /// Retrieves a mask of the specified component types.
        /// </summary>
        public readonly BitMask GetComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetComponentType<T1>());
            bitMask.Set(GetComponentType<T2>());
            bitMask.Set(GetComponentType<T3>());
            bitMask.Set(GetComponentType<T4>());
            bitMask.Set(GetComponentType<T5>());
            bitMask.Set(GetComponentType<T6>());
            bitMask.Set(GetComponentType<T7>());
            bitMask.Set(GetComponentType<T8>());
            return bitMask;
        }

        /// <summary>
        /// Retrieves a mask of the specified component types.
        /// </summary>
        public readonly BitMask GetComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetComponentType<T1>());
            bitMask.Set(GetComponentType<T2>());
            bitMask.Set(GetComponentType<T3>());
            bitMask.Set(GetComponentType<T4>());
            bitMask.Set(GetComponentType<T5>());
            bitMask.Set(GetComponentType<T6>());
            bitMask.Set(GetComponentType<T7>());
            bitMask.Set(GetComponentType<T8>());
            bitMask.Set(GetComponentType<T9>());
            return bitMask;
        }

        /// <summary>
        /// Retrieves a mask of the specified component types.
        /// </summary>
        public readonly BitMask GetComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetComponentType<T1>());
            bitMask.Set(GetComponentType<T2>());
            bitMask.Set(GetComponentType<T3>());
            bitMask.Set(GetComponentType<T4>());
            bitMask.Set(GetComponentType<T5>());
            bitMask.Set(GetComponentType<T6>());
            bitMask.Set(GetComponentType<T7>());
            bitMask.Set(GetComponentType<T8>());
            bitMask.Set(GetComponentType<T9>());
            bitMask.Set(GetComponentType<T10>());
            return bitMask;
        }

        /// <summary>
        /// Retrieves a mask of the specified component types.
        /// </summary>
        public readonly BitMask GetComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetComponentType<T1>());
            bitMask.Set(GetComponentType<T2>());
            bitMask.Set(GetComponentType<T3>());
            bitMask.Set(GetComponentType<T4>());
            bitMask.Set(GetComponentType<T5>());
            bitMask.Set(GetComponentType<T6>());
            bitMask.Set(GetComponentType<T7>());
            bitMask.Set(GetComponentType<T8>());
            bitMask.Set(GetComponentType<T9>());
            bitMask.Set(GetComponentType<T10>());
            bitMask.Set(GetComponentType<T11>());
            return bitMask;
        }

        /// <summary>
        /// Retrieves a mask of the specified component types.
        /// </summary>
        public readonly BitMask GetComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetComponentType<T1>());
            bitMask.Set(GetComponentType<T2>());
            bitMask.Set(GetComponentType<T3>());
            bitMask.Set(GetComponentType<T4>());
            bitMask.Set(GetComponentType<T5>());
            bitMask.Set(GetComponentType<T6>());
            bitMask.Set(GetComponentType<T7>());
            bitMask.Set(GetComponentType<T8>());
            bitMask.Set(GetComponentType<T9>());
            bitMask.Set(GetComponentType<T10>());
            bitMask.Set(GetComponentType<T11>());
            bitMask.Set(GetComponentType<T12>());
            return bitMask;
        }

        /// <summary>
        /// Retrieves a mask of the specified component types.
        /// </summary>
        public readonly BitMask GetComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetComponentType<T1>());
            bitMask.Set(GetComponentType<T2>());
            bitMask.Set(GetComponentType<T3>());
            bitMask.Set(GetComponentType<T4>());
            bitMask.Set(GetComponentType<T5>());
            bitMask.Set(GetComponentType<T6>());
            bitMask.Set(GetComponentType<T7>());
            bitMask.Set(GetComponentType<T8>());
            bitMask.Set(GetComponentType<T9>());
            bitMask.Set(GetComponentType<T10>());
            bitMask.Set(GetComponentType<T11>());
            bitMask.Set(GetComponentType<T12>());
            bitMask.Set(GetComponentType<T13>());
            return bitMask;
        }

        /// <summary>
        /// Retrieves a mask of the specified component types.
        /// </summary>
        public readonly BitMask GetComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetComponentType<T1>());
            bitMask.Set(GetComponentType<T2>());
            bitMask.Set(GetComponentType<T3>());
            bitMask.Set(GetComponentType<T4>());
            bitMask.Set(GetComponentType<T5>());
            bitMask.Set(GetComponentType<T6>());
            bitMask.Set(GetComponentType<T7>());
            bitMask.Set(GetComponentType<T8>());
            bitMask.Set(GetComponentType<T9>());
            bitMask.Set(GetComponentType<T10>());
            bitMask.Set(GetComponentType<T11>());
            bitMask.Set(GetComponentType<T12>());
            bitMask.Set(GetComponentType<T13>());
            bitMask.Set(GetComponentType<T14>());
            return bitMask;
        }

        /// <summary>
        /// Retrieves a mask of the specified component types.
        /// </summary>
        public readonly BitMask GetComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged where T15 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetComponentType<T1>());
            bitMask.Set(GetComponentType<T2>());
            bitMask.Set(GetComponentType<T3>());
            bitMask.Set(GetComponentType<T4>());
            bitMask.Set(GetComponentType<T5>());
            bitMask.Set(GetComponentType<T6>());
            bitMask.Set(GetComponentType<T7>());
            bitMask.Set(GetComponentType<T8>());
            bitMask.Set(GetComponentType<T9>());
            bitMask.Set(GetComponentType<T10>());
            bitMask.Set(GetComponentType<T11>());
            bitMask.Set(GetComponentType<T12>());
            bitMask.Set(GetComponentType<T13>());
            bitMask.Set(GetComponentType<T14>());
            bitMask.Set(GetComponentType<T15>());
            return bitMask;
        }

        /// <summary>
        /// Retrieves a mask of the specified component types.
        /// </summary>
        public readonly BitMask GetComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged where T15 : unmanaged where T16 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetComponentType<T1>());
            bitMask.Set(GetComponentType<T2>());
            bitMask.Set(GetComponentType<T3>());
            bitMask.Set(GetComponentType<T4>());
            bitMask.Set(GetComponentType<T5>());
            bitMask.Set(GetComponentType<T6>());
            bitMask.Set(GetComponentType<T7>());
            bitMask.Set(GetComponentType<T8>());
            bitMask.Set(GetComponentType<T9>());
            bitMask.Set(GetComponentType<T10>());
            bitMask.Set(GetComponentType<T11>());
            bitMask.Set(GetComponentType<T12>());
            bitMask.Set(GetComponentType<T13>());
            bitMask.Set(GetComponentType<T14>());
            bitMask.Set(GetComponentType<T15>());
            bitMask.Set(GetComponentType<T16>());
            return bitMask;
        }

        /// <summary>
        /// Retrieves a mask of the specified array types.
        /// </summary>
        public readonly BitMask GetArrayTypes<T1>() where T1 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetArrayType<T1>());
            return bitMask;
        }

        /// <summary>
        /// Retrieves a mask of the specified array types.
        /// </summary>
        public readonly BitMask GetArrayTypes<T1, T2>() where T1 : unmanaged where T2 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetArrayType<T1>());
            bitMask.Set(GetArrayType<T2>());
            return bitMask;
        }

        /// <summary>
        /// Retrieves a mask of the specified array types.
        /// </summary>
        public readonly BitMask GetArrayTypes<T1, T2, T3>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetArrayType<T1>());
            bitMask.Set(GetArrayType<T2>());
            bitMask.Set(GetArrayType<T3>());
            return bitMask;
        }

        /// <summary>
        /// Retrieves a mask of the specified array types.
        /// </summary>
        public readonly BitMask GetArrayTypes<T1, T2, T3, T4>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetArrayType<T1>());
            bitMask.Set(GetArrayType<T2>());
            bitMask.Set(GetArrayType<T3>());
            bitMask.Set(GetArrayType<T4>());
            return bitMask;
        }

        /// <summary>
        /// Retrieves a mask of the specified array types.
        /// </summary>
        public readonly BitMask GetArrayTypes<T1, T2, T3, T4, T5>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetArrayType<T1>());
            bitMask.Set(GetArrayType<T2>());
            bitMask.Set(GetArrayType<T3>());
            bitMask.Set(GetArrayType<T4>());
            bitMask.Set(GetArrayType<T5>());
            return bitMask;
        }

        /// <summary>
        /// Retrieves a mask of the specified array types.
        /// </summary>
        public readonly BitMask GetArrayTypes<T1, T2, T3, T4, T5, T6>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetArrayType<T1>());
            bitMask.Set(GetArrayType<T2>());
            bitMask.Set(GetArrayType<T3>());
            bitMask.Set(GetArrayType<T4>());
            bitMask.Set(GetArrayType<T5>());
            bitMask.Set(GetArrayType<T6>());
            return bitMask;
        }

        /// <summary>
        /// Retrieves a mask of the specified array types.
        /// </summary>
        public readonly BitMask GetArrayTypes<T1, T2, T3, T4, T5, T6, T7>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetArrayType<T1>());
            bitMask.Set(GetArrayType<T2>());
            bitMask.Set(GetArrayType<T3>());
            bitMask.Set(GetArrayType<T4>());
            bitMask.Set(GetArrayType<T5>());
            bitMask.Set(GetArrayType<T6>());
            bitMask.Set(GetArrayType<T7>());
            return bitMask;
        }

        /// <summary>
        /// Retrieves a mask of the specified array types.
        /// </summary>
        public readonly BitMask GetArrayTypes<T1, T2, T3, T4, T5, T6, T7, T8>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetArrayType<T1>());
            bitMask.Set(GetArrayType<T2>());
            bitMask.Set(GetArrayType<T3>());
            bitMask.Set(GetArrayType<T4>());
            bitMask.Set(GetArrayType<T5>());
            bitMask.Set(GetArrayType<T6>());
            bitMask.Set(GetArrayType<T7>());
            bitMask.Set(GetArrayType<T8>());
            return bitMask;
        }

        /// <summary>
        /// Retrieves a mask of the specified array types.
        /// </summary>
        public readonly BitMask GetArrayTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetArrayType<T1>());
            bitMask.Set(GetArrayType<T2>());
            bitMask.Set(GetArrayType<T3>());
            bitMask.Set(GetArrayType<T4>());
            bitMask.Set(GetArrayType<T5>());
            bitMask.Set(GetArrayType<T6>());
            bitMask.Set(GetArrayType<T7>());
            bitMask.Set(GetArrayType<T8>());
            bitMask.Set(GetArrayType<T9>());
            return bitMask;
        }

        /// <summary>
        /// Retrieves a mask of the specified array types.
        /// </summary>
        public readonly BitMask GetArrayTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetArrayType<T1>());
            bitMask.Set(GetArrayType<T2>());
            bitMask.Set(GetArrayType<T3>());
            bitMask.Set(GetArrayType<T4>());
            bitMask.Set(GetArrayType<T5>());
            bitMask.Set(GetArrayType<T6>());
            bitMask.Set(GetArrayType<T7>());
            bitMask.Set(GetArrayType<T8>());
            bitMask.Set(GetArrayType<T9>());
            bitMask.Set(GetArrayType<T10>());
            return bitMask;
        }

        /// <summary>
        /// Retrieves a mask of the specified array types.
        /// </summary>
        public readonly BitMask GetArrayTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetArrayType<T1>());
            bitMask.Set(GetArrayType<T2>());
            bitMask.Set(GetArrayType<T3>());
            bitMask.Set(GetArrayType<T4>());
            bitMask.Set(GetArrayType<T5>());
            bitMask.Set(GetArrayType<T6>());
            bitMask.Set(GetArrayType<T7>());
            bitMask.Set(GetArrayType<T8>());
            bitMask.Set(GetArrayType<T9>());
            bitMask.Set(GetArrayType<T10>());
            bitMask.Set(GetArrayType<T11>());
            return bitMask;
        }

        /// <summary>
        /// Retrieves a mask of the specified array types.
        /// </summary>
        public readonly BitMask GetArrayTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetArrayType<T1>());
            bitMask.Set(GetArrayType<T2>());
            bitMask.Set(GetArrayType<T3>());
            bitMask.Set(GetArrayType<T4>());
            bitMask.Set(GetArrayType<T5>());
            bitMask.Set(GetArrayType<T6>());
            bitMask.Set(GetArrayType<T7>());
            bitMask.Set(GetArrayType<T8>());
            bitMask.Set(GetArrayType<T9>());
            bitMask.Set(GetArrayType<T10>());
            bitMask.Set(GetArrayType<T11>());
            bitMask.Set(GetArrayType<T12>());
            return bitMask;
        }

        /// <summary>
        /// Retrieves a mask of the specified tag types.
        /// </summary>
        public readonly BitMask GetTagTypes<T1>() where T1 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetTagType<T1>());
            return bitMask;
        }

        /// <summary>
        /// Retrieves a mask of the specified tag types.
        /// </summary>
        public readonly BitMask GetTagTypes<T1, T2>() where T1 : unmanaged where T2 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetTagType<T1>());
            bitMask.Set(GetTagType<T2>());
            return bitMask;
        }

        /// <summary>
        /// Retrieves a mask of the specified tag types.
        /// </summary>
        public readonly BitMask GetTagTypes<T1, T2, T3>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetTagType<T1>());
            bitMask.Set(GetTagType<T2>());
            bitMask.Set(GetTagType<T3>());
            return bitMask;
        }

        /// <summary>
        /// Retrieves a mask of the specified tag types.
        /// </summary>
        public readonly BitMask GetTagTypes<T1, T2, T3, T4>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetTagType<T1>());
            bitMask.Set(GetTagType<T2>());
            bitMask.Set(GetTagType<T3>());
            bitMask.Set(GetTagType<T4>());
            return bitMask;
        }

        /// <summary>
        /// Retrieves a mask of the specified tag types.
        /// </summary>
        public readonly BitMask GetTagTypes<T1, T2, T3, T4, T5>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetTagType<T1>());
            bitMask.Set(GetTagType<T2>());
            bitMask.Set(GetTagType<T3>());
            bitMask.Set(GetTagType<T4>());
            bitMask.Set(GetTagType<T5>());
            return bitMask;
        }

        /// <summary>
        /// Retrieves a mask of the specified tag types.
        /// </summary>
        public readonly BitMask GetTagTypes<T1, T2, T3, T4, T5, T6>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetTagType<T1>());
            bitMask.Set(GetTagType<T2>());
            bitMask.Set(GetTagType<T3>());
            bitMask.Set(GetTagType<T4>());
            bitMask.Set(GetTagType<T5>());
            bitMask.Set(GetTagType<T6>());
            return bitMask;
        }

        /// <summary>
        /// Retrieves a mask of the specified tag types.
        /// </summary>
        public readonly BitMask GetTagTypes<T1, T2, T3, T4, T5, T6, T7>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetTagType<T1>());
            bitMask.Set(GetTagType<T2>());
            bitMask.Set(GetTagType<T3>());
            bitMask.Set(GetTagType<T4>());
            bitMask.Set(GetTagType<T5>());
            bitMask.Set(GetTagType<T6>());
            bitMask.Set(GetTagType<T7>());
            return bitMask;
        }

        /// <summary>
        /// Retrieves a mask of the specified tag types.
        /// </summary>
        public readonly BitMask GetTagTypes<T1, T2, T3, T4, T5, T6, T7, T8>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetTagType<T1>());
            bitMask.Set(GetTagType<T2>());
            bitMask.Set(GetTagType<T3>());
            bitMask.Set(GetTagType<T4>());
            bitMask.Set(GetTagType<T5>());
            bitMask.Set(GetTagType<T6>());
            bitMask.Set(GetTagType<T7>());
            bitMask.Set(GetTagType<T8>());
            return bitMask;
        }

        /// <summary>
        /// Retrieves a mask of the specified tag types.
        /// </summary>
        public readonly BitMask GetTagTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetTagType<T1>());
            bitMask.Set(GetTagType<T2>());
            bitMask.Set(GetTagType<T3>());
            bitMask.Set(GetTagType<T4>());
            bitMask.Set(GetTagType<T5>());
            bitMask.Set(GetTagType<T6>());
            bitMask.Set(GetTagType<T7>());
            bitMask.Set(GetTagType<T8>());
            bitMask.Set(GetTagType<T9>());
            return bitMask;
        }

        /// <summary>
        /// Retrieves a mask of the specified tag types.
        /// </summary>
        public readonly BitMask GetTagTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetTagType<T1>());
            bitMask.Set(GetTagType<T2>());
            bitMask.Set(GetTagType<T3>());
            bitMask.Set(GetTagType<T4>());
            bitMask.Set(GetTagType<T5>());
            bitMask.Set(GetTagType<T6>());
            bitMask.Set(GetTagType<T7>());
            bitMask.Set(GetTagType<T8>());
            bitMask.Set(GetTagType<T9>());
            bitMask.Set(GetTagType<T10>());
            return bitMask;
        }

        /// <summary>
        /// Retrieves a mask of the specified tag types.
        /// </summary>
        public readonly BitMask GetTagTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetTagType<T1>());
            bitMask.Set(GetTagType<T2>());
            bitMask.Set(GetTagType<T3>());
            bitMask.Set(GetTagType<T4>());
            bitMask.Set(GetTagType<T5>());
            bitMask.Set(GetTagType<T6>());
            bitMask.Set(GetTagType<T7>());
            bitMask.Set(GetTagType<T8>());
            bitMask.Set(GetTagType<T9>());
            bitMask.Set(GetTagType<T10>());
            bitMask.Set(GetTagType<T11>());
            return bitMask;
        }

        /// <summary>
        /// Retrieves a mask of the specified tag types.
        /// </summary>
        public readonly BitMask GetTagTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetTagType<T1>());
            bitMask.Set(GetTagType<T2>());
            bitMask.Set(GetTagType<T3>());
            bitMask.Set(GetTagType<T4>());
            bitMask.Set(GetTagType<T5>());
            bitMask.Set(GetTagType<T6>());
            bitMask.Set(GetTagType<T7>());
            bitMask.Set(GetTagType<T8>());
            bitMask.Set(GetTagType<T9>());
            bitMask.Set(GetTagType<T10>());
            bitMask.Set(GetTagType<T11>());
            bitMask.Set(GetTagType<T12>());
            return bitMask;
        }

        /// <summary>
        /// Creates a new empty schema.
        /// </summary>
        public static Schema Create()
        {
            SchemaPointer* schema = MemoryAddress.AllocatePointer<SchemaPointer>();
            schema->componentCount = 0;
            schema->arraysCount = 0;
            schema->tagsCount = 0;
            schema->componentRowSize = 0;
            schema->definitionMask = default;
            schema->offsets = MemoryAddress.AllocateZeroed(OffsetsLengthInBytes);
            schema->sizes = MemoryAddress.AllocateZeroed(SizesLengthInBytes);
            schema->typeHashes = MemoryAddress.AllocateZeroed(TypeHashesLengthInBytes);
            schema->schemaIndex = createdSchemas;
            createdSchemas++;
            return new Schema(schema);
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
        private readonly void ThrowIfComponentTypeIsMissing(int componentType)
        {
            if (!ContainsComponentType(componentType))
            {
                throw new Exception($"Component size for `{componentType}` is missing from schema");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfComponentTypeIsMissing(Types.Type componentType)
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
        private readonly void ThrowIfArrayTypeIsMissing(int arrayType)
        {
            if (!ContainsArrayType(arrayType))
            {
                throw new Exception($"Array type size for `{arrayType}` is missing from schema");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfArrayTypeIsMissing(Types.Type arrayType)
        {
            if (!ContainsArrayType(arrayType))
            {
                throw new Exception($"Array type `{arrayType}` is missing from schema");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfArrayTypeIsMissing<T>() where T : unmanaged
        {
            if (!ContainsArrayType<T>())
            {
                throw new Exception($"Array type `{typeof(T).FullName}` is missing from schema");
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

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj)
        {
            return obj is Schema schema && Equals(schema);
        }

        /// <inheritdoc/>
        public readonly bool Equals(Schema other)
        {
            return schema == other.schema;
        }

        /// <inheritdoc/>
        public readonly override int GetHashCode()
        {
            return ((nint)schema).GetHashCode();
        }

        readonly void ISerializable.Write(ByteWriter writer)
        {
            writer.WriteValue(schema->componentCount);
            writer.WriteValue(schema->arraysCount);
            writer.WriteValue(schema->tagsCount);
            writer.WriteValue(schema->componentRowSize);
            writer.WriteValue(schema->definitionMask);
            Span<int> offsets = new(schema->offsets.Pointer, BitMask.Capacity);
            Span<int> sizes = new(schema->sizes.Pointer, BitMask.Capacity * 2);
            Span<long> typeHashes = new(schema->typeHashes.Pointer, BitMask.Capacity * 3);
            writer.WriteSpan(offsets);
            writer.WriteSpan(sizes);
            writer.WriteSpan(typeHashes);
        }

        void ISerializable.Read(ByteReader reader)
        {
            schema = MemoryAddress.AllocatePointer<SchemaPointer>();
            schema->offsets = MemoryAddress.AllocateZeroed(OffsetsLengthInBytes);
            schema->sizes = MemoryAddress.AllocateZeroed(SizesLengthInBytes);
            schema->typeHashes = MemoryAddress.AllocateZeroed(TypeHashesLengthInBytes);
            schema->componentCount = reader.ReadValue<byte>();
            schema->arraysCount = reader.ReadValue<byte>();
            schema->tagsCount = reader.ReadValue<byte>();
            schema->componentRowSize = reader.ReadValue<int>();
            schema->definitionMask = reader.ReadValue<Definition>();
            schema->offsets.CopyFrom(reader.ReadSpan<int>(BitMask.Capacity));
            schema->sizes.CopyFrom(reader.ReadSpan<int>(BitMask.Capacity * 2));
            schema->typeHashes.CopyFrom(reader.ReadSpan<long>(BitMask.Capacity * 3));
            schema->schemaIndex = createdSchemas;
            createdSchemas++;
        }

        /// <summary>
        /// Resets the schema to <see langword="default"/> state.
        /// </summary>
        public readonly void Clear()
        {
            schema->componentCount = 0;
            schema->arraysCount = 0;
            schema->tagsCount = 0;
            schema->componentRowSize = 0;
            schema->definitionMask = default;
            schema->offsets.Clear(OffsetsLengthInBytes);
            schema->sizes.Clear(SizesLengthInBytes);
            schema->typeHashes.Clear(TypeHashesLengthInBytes);
        }

        /// <inheritdoc/>
        public static bool operator ==(Schema left, Schema right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(Schema left, Schema right)
        {
            return !(left == right);
        }

        private static class TypeLayoutHashCodeCache<T> where T : unmanaged
        {
            public static readonly long value = MetadataRegistry.GetType<T>().Hash;
        }

        /// <summary>
        /// Cache of types per <see cref="Schema"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        private static class SchemaTypeCache<T> where T : unmanaged
        {
            private static int[] components;
            private static int[] arrays;
            private static int[] tags;
            private static int componentCapacity;
            private static int arrayCapacity;
            private static int tagCapacity;

            static SchemaTypeCache()
            {
                componentCapacity = 0;
                arrayCapacity = 0;
                tagCapacity = 0;
                components = new int[componentCapacity];
                arrays = new int[arrayCapacity];
                tags = new int[tagCapacity];
            }

            public static void SetComponentType(Schema schema, int componentType)
            {
                if (schema.schema->schemaIndex >= componentCapacity)
                {
                    componentCapacity = schema.schema->schemaIndex + 1;
                    Array.Resize(ref components, componentCapacity);
                }

                components[schema.schema->schemaIndex] = componentType;
            }

            public static void SetArrayType(Schema schema, int arrayType)
            {
                if (schema.schema->schemaIndex >= arrayCapacity)
                {
                    arrayCapacity = schema.schema->schemaIndex + 1;
                    Array.Resize(ref arrays, arrayCapacity);
                }

                arrays[schema.schema->schemaIndex] = arrayType;
            }

            public static void SetTagType(Schema schema, int tagType)
            {
                if (schema.schema->schemaIndex >= tagCapacity)
                {
                    tagCapacity = schema.schema->schemaIndex + 1;
                    Array.Resize(ref tags, tagCapacity);
                }

                tags[schema.schema->schemaIndex] = tagType;
            }

            public static int GetOrSetComponentType(Schema schema)
            {
                int schemaIndex = schema.schema->schemaIndex;
                if (schemaIndex < componentCapacity)
                {
                    return components[schemaIndex];
                }
                else
                {
                    componentCapacity = schemaIndex + 1;
                    Array.Resize(ref components, componentCapacity);

                    Span<long> componentTypeHashes = new(schema.schema->typeHashes.Pointer, BitMask.Capacity);
                    int componentType = componentTypeHashes.IndexOf(TypeLayoutHashCodeCache<T>.value);
                    components[schemaIndex] = componentType;
                    return componentType;
                }
            }

            public static int GetOrSetArrayType(Schema schema)
            {
                int schemaIndex = schema.schema->schemaIndex;
                if (schemaIndex < arrayCapacity)
                {
                    return arrays[schemaIndex];
                }
                else
                {
                    arrayCapacity = schemaIndex + 1;
                    Array.Resize(ref arrays, arrayCapacity);

                    Span<long> arrayTypeHashes = new(schema.schema->typeHashes.Pointer + BitMask.Capacity * sizeof(long), BitMask.Capacity);
                    int arrayType = arrayTypeHashes.IndexOf(TypeLayoutHashCodeCache<T>.value);
                    arrays[schemaIndex] = arrayType;
                    return arrayType;
                }
            }

            public static int GetOrSetTagType(Schema schema)
            {
                int schemaIndex = schema.schema->schemaIndex;
                if (schemaIndex < tagCapacity)
                {
                    return tags[schemaIndex];
                }
                else
                {
                    tagCapacity = schemaIndex + 1;
                    Array.Resize(ref tags, tagCapacity);

                    Span<long> tagTypeHashes = new(schema.schema->typeHashes.Pointer + BitMask.Capacity * sizeof(long) * 2, BitMask.Capacity);
                    int tagType = tagTypeHashes.IndexOf(TypeLayoutHashCodeCache<T>.value);
                    tags[schemaIndex] = tagType;
                    return tagType;
                }
            }
        }
    }
}