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
        //todo: checking if a bitmask has this tag set more cheaply than Contains
        public const int DisabledTagType = BitMask.MaxValue;

        internal const int OffsetsLengthInBytes = sizeof(int) * BitMask.Capacity;
        internal const int SizesLengthInBytes = sizeof(int) * BitMask.Capacity * 2;
        internal const int TypeHashesLengthInBytes = sizeof(long) * BitMask.Capacity * 3;

        private static int createdSchemas;

        internal SchemaPointer* schema;

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
        private readonly TypeMetadata[] Components
        {
            get
            {
                TypeMetadata[] buffer = new TypeMetadata[schema->componentCount];
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
        private readonly TypeMetadata[] Arrays
        {
            get
            {
                TypeMetadata[] buffer = new TypeMetadata[schema->arraysCount];
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
        private readonly TypeMetadata[] Tags
        {
            get
            {
                TypeMetadata[] buffer = new TypeMetadata[schema->tagsCount];
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
            schema->definitionMask = Definition.Default;
            schema->componentOffsets = MemoryAddress.AllocateZeroed(OffsetsLengthInBytes);
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

            schema->componentOffsets.Dispose();
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
            schema->componentOffsets.CopyFrom(source.schema->componentOffsets, OffsetsLengthInBytes);
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

            return schema->componentOffsets.ReadElement<int>(GetComponentType<T>());
        }

        /// <summary>
        /// Retrieves the position in bytes where <paramref name="componentType"/>
        /// would start in a chunk component row.
        /// </summary>
        public readonly int GetComponentOffset(int componentType)
        {
            return schema->componentOffsets.ReadElement<int>(componentType);
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
        public readonly TypeMetadata GetComponentLayout(int componentType)
        {
            MemoryAddress.ThrowIfDefault(schema);
            ThrowIfComponentTypeIsMissing(componentType);

            Span<long> componentTypeHashes = new(schema->typeHashes.Pointer, BitMask.Capacity);
            long hash = componentTypeHashes[componentType];
            return new(hash);
        }

        /// <summary>
        /// Retrieves the type layout for the given <paramref name="arrayType"/>.
        /// </summary>
        public readonly TypeMetadata GetArrayLayout(int arrayType)
        {
            MemoryAddress.ThrowIfDefault(schema);
            ThrowIfArrayTypeIsMissing(arrayType);

            Span<TypeMetadata> arrayTypeHashes = schema->typeHashes.AsSpan<TypeMetadata>(BitMask.Capacity, BitMask.Capacity);
            return arrayTypeHashes[arrayType];
        }

        /// <summary>
        /// Retrieves the type layout for the given <paramref name="tagType"/>.
        /// </summary>
        public readonly TypeMetadata GetTagLayout(int tagType)
        {
            MemoryAddress.ThrowIfDefault(schema);
            ThrowIfTagIsMissing(tagType);

            Span<TypeMetadata> tagTypeHashes = schema->typeHashes.AsSpan<TypeMetadata>(BitMask.Capacity * 2, BitMask.Capacity);
            return tagTypeHashes[tagType];
        }

        /// <summary>
        /// Retrieves the type metadata for the component of type <typeparamref name="T"/>.
        /// </summary>
        public readonly TypeMetadata GetComponentLayout<T>() where T : unmanaged
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
        public readonly int RegisterComponent(TypeMetadata type)
        {
            if (TryGetComponentType(type, out int componentType))
            {
                return componentType;
            }

            ThrowIfTooManyComponents();

            componentType = schema->componentCount;
            schema->componentOffsets.WriteElement(componentType, schema->componentRowSize);
            schema->sizes.WriteElement(componentType, (int)type.Size);
            schema->typeHashes.WriteElement(componentType, type);
            schema->componentCount++;
            schema->componentRowSize += type.Size;
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
        public readonly int RegisterArray(TypeMetadata type)
        {
            if (TryGetArrayType(type, out int arrayType))
            {
                return arrayType;
            }

            ThrowIfTooManyArrays();

            arrayType = schema->arraysCount;
            Span<int> arraySizes = schema->sizes.AsSpan<int>(BitMask.Capacity, BitMask.Capacity);
            Span<TypeMetadata> arrayHashes = schema->typeHashes.AsSpan<TypeMetadata>(BitMask.Capacity, BitMask.Capacity);
            arraySizes[schema->arraysCount] = type.Size;
            arrayHashes[schema->arraysCount] = type;
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
        public readonly int RegisterTag(TypeMetadata type)
        {
            if (TryGetTagType(type, out int tagType))
            {
                return tagType;
            }

            ThrowIfTooManyTags();

            tagType = schema->tagsCount;
            Span<TypeMetadata> tagHashes = schema->typeHashes.AsSpan<TypeMetadata>(BitMask.Capacity * 2, BitMask.Capacity);
            tagHashes[tagType] = type;
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
        public readonly bool ContainsComponentType(TypeMetadata type)
        {
            MemoryAddress.ThrowIfDefault(schema);

            Span<TypeMetadata> componentTypeHashes = new(schema->typeHashes.Pointer, BitMask.Capacity);
            return componentTypeHashes.Contains(type);
        }

        /// <summary>
        /// Tries to retrieve the index of the component type <paramref name="type"/>.
        /// </summary>
        public readonly bool TryGetComponentType(TypeMetadata type, out int componentType)
        {
            MemoryAddress.ThrowIfDefault(schema);

            Span<TypeMetadata> componentTypeHashes = new(schema->typeHashes.Pointer, BitMask.Capacity);
            return componentTypeHashes.TryIndexOf(type, out componentType);
        }

        /// <summary>
        /// Tries to retrieve the index of the component type with the given <paramref name="fullTypeName"/>.
        /// </summary>
        public readonly bool TryGetComponentType(ReadOnlySpan<char> fullTypeName, out int componentType)
        {
            MemoryAddress.ThrowIfDefault(schema);

            Span<long> componentTypeHashes = new(schema->typeHashes.Pointer, BitMask.Capacity);
            return componentTypeHashes.TryIndexOf(fullTypeName.GetLongHashCode(), out componentType);
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
        public readonly bool ContainsArrayType(TypeMetadata type)
        {
            MemoryAddress.ThrowIfDefault(schema);

            Span<TypeMetadata> arrayTypeHashes = schema->typeHashes.AsSpan<TypeMetadata>(BitMask.Capacity, BitMask.Capacity);
            return arrayTypeHashes.Contains(type);
        }

        /// <summary>
        /// Tries to retrieve the index of the array type <paramref name="type"/>.
        /// </summary>
        public readonly bool TryGetArrayType(TypeMetadata type, out int arrayType)
        {
            MemoryAddress.ThrowIfDefault(schema);

            Span<TypeMetadata> arrayTypeHashes = schema->typeHashes.AsSpan<TypeMetadata>(BitMask.Capacity, BitMask.Capacity);
            return arrayTypeHashes.TryIndexOf(type, out arrayType);
        }

        /// <summary>
        /// Tries to retrieve the index of the array type with the given <paramref name="fullTypeName"/>.
        /// </summary>
        public readonly bool TryGetArrayType(ReadOnlySpan<char> fullTypeName, out int arrayType)
        {
            MemoryAddress.ThrowIfDefault(schema);

            Span<long> arrayTypeHashes = schema->typeHashes.AsSpan<long>(BitMask.Capacity, BitMask.Capacity);
            return arrayTypeHashes.TryIndexOf(fullTypeName.GetLongHashCode(), out arrayType);
        }

        /// <summary>
        /// Checks if a tag type with <paramref name="fullTypeName"/> has been registered.
        /// </summary>
        public readonly bool ContainsTagType(ASCIIText256 fullTypeName)
        {
            MemoryAddress.ThrowIfDefault(schema);

            Span<long> tagTypeHashes = schema->typeHashes.AsSpan<long>(BitMask.Capacity * 2, BitMask.Capacity);
            return tagTypeHashes.Contains(fullTypeName.GetLongHashCode());
        }

        /// <summary>
        /// Checks if <paramref name="type"/> is a registered tag.
        /// </summary>
        public readonly bool ContainsTagType(TypeMetadata type)
        {
            MemoryAddress.ThrowIfDefault(schema);

            Span<TypeMetadata> tagTypeHashes = schema->typeHashes.AsSpan<TypeMetadata>(BitMask.Capacity * 2, BitMask.Capacity);
            return tagTypeHashes.Contains(type);
        }

        /// <summary>
        /// Tries to retrieve the index of the tag type <paramref name="type"/>.
        /// </summary>
        public readonly bool TryGetTagType(TypeMetadata type, out int tagType)
        {
            MemoryAddress.ThrowIfDefault(schema);

            Span<TypeMetadata> tagTypeHashes = schema->typeHashes.AsSpan<TypeMetadata>(BitMask.Capacity * 2, BitMask.Capacity);
            return tagTypeHashes.TryIndexOf(type, out tagType);
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

            Span<TypeMetadata> componentTypeHashes = new(schema->typeHashes.Pointer, BitMask.Capacity);
            return componentTypeHashes.Contains(TypeMetadata.Get<T>());
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
        /// Retrieves the index for component of <paramref name="type"/>.
        /// </summary>
        public readonly int GetComponentType(TypeMetadata type)
        {
            ThrowIfComponentTypeIsMissing(type);

            Span<long> componentTypeHashes = new(schema->typeHashes.Pointer, BitMask.Capacity);
            return componentTypeHashes.IndexOf(type.hash);
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
        public readonly DataType GetComponentDataType(TypeMetadata type)
        {
            ThrowIfComponentTypeIsMissing(type);

            Span<long> componentTypeHashes = new(schema->typeHashes.Pointer, BitMask.Capacity);
            int componentType = componentTypeHashes.IndexOf(type.hash);
            return new(componentType, DataType.Kind.Component, type.Size);
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

            Span<TypeMetadata> arrayTypeHashes = schema->typeHashes.AsSpan<TypeMetadata>(BitMask.Capacity, BitMask.Capacity);
            return arrayTypeHashes.Contains(TypeMetadata.Get<T>());
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
        /// Retrieves the index for the given <paramref name="arrayType"/>.
        /// </summary>
        public readonly int GetArrayType(TypeMetadata arrayType)
        {
            ThrowIfArrayTypeIsMissing(arrayType);

            Span<long> arrayTypeHashes = schema->typeHashes.AsSpan<long>(BitMask.Capacity, BitMask.Capacity);
            return arrayTypeHashes.IndexOf(arrayType.hash);
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
        public readonly DataType GetArrayDataType(TypeMetadata type)
        {
            ThrowIfArrayTypeIsMissing(type);

            Span<long> arrayTypeHashes = schema->typeHashes.AsSpan<long>(BitMask.Capacity, BitMask.Capacity);
            int arrayType = arrayTypeHashes.IndexOf(type.hash);
            return new(arrayType, DataType.Kind.Array, type.Size);
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
        /// Retrieves the index for the given <paramref name="tagType"/>.
        /// </summary>
        public readonly int GetTagType(TypeMetadata tagType)
        {
            ThrowIfTagIsMissing(tagType);

            Span<long> tagTypeHashes = schema->typeHashes.AsSpan<long>(BitMask.Capacity * 2, BitMask.Capacity);
            return tagTypeHashes.IndexOf(tagType.hash);
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

            Span<TypeMetadata> tagTypeHashes = schema->typeHashes.AsSpan<TypeMetadata>(BitMask.Capacity * 2, BitMask.Capacity);
            return tagTypeHashes.Contains(TypeMetadata.Get<T>());
        }

        /// <summary>
        /// Retrieves a mask of the specified component types.
        /// </summary>
        public readonly BitMask GetComponentTypes<T1>() where T1 : unmanaged
        {
            return new(GetComponentType<T1>());
        }

        /// <summary>
        /// Retrieves a mask of the specified component types.
        /// </summary>
        public readonly BitMask GetComponentTypes<T1, T2>() where T1 : unmanaged where T2 : unmanaged
        {
            return new(GetComponentType<T1>(), GetComponentType<T2>());
        }

        /// <summary>
        /// Retrieves the specified component types.
        /// </summary>
        public readonly void GetComponentTypes<T1, T2>(out int c1, out int c2) where T1 : unmanaged where T2 : unmanaged
        {
            c1 = GetComponentType<T1>();
            c2 = GetComponentType<T2>();
        }

        /// <summary>
        /// Retrieves a mask of the specified component types.
        /// </summary>
        public readonly BitMask GetComponentTypes<T1, T2, T3>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            return new(GetComponentType<T1>(), GetComponentType<T2>(), GetComponentType<T3>());
        }

        /// <summary>
        /// Retrieves the specified component types.
        /// </summary>
        public readonly void GetComponentTypes<T1, T2, T3>(out int c1, out int c2, out int c3) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            c1 = GetComponentType<T1>();
            c2 = GetComponentType<T2>();
            c3 = GetComponentType<T3>();
        }

        /// <summary>
        /// Retrieves a mask of the specified component types.
        /// </summary>
        public readonly BitMask GetComponentTypes<T1, T2, T3, T4>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            return new(GetComponentType<T1>(), GetComponentType<T2>(), GetComponentType<T3>(), GetComponentType<T4>());
        }

        /// <summary>
        /// Retrieves the specified component types.
        /// </summary>
        public readonly void GetComponentTypes<T1, T2, T3, T4>(out int c1, out int c2, out int c3, out int c4) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            c1 = GetComponentType<T1>();
            c2 = GetComponentType<T2>();
            c3 = GetComponentType<T3>();
            c4 = GetComponentType<T4>();
        }

        /// <summary>
        /// Retrieves a mask of the specified component types.
        /// </summary>
        public readonly BitMask GetComponentTypes<T1, T2, T3, T4, T5>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
        {
            return new(GetComponentType<T1>(), GetComponentType<T2>(), GetComponentType<T3>(), GetComponentType<T4>(), GetComponentType<T5>());
        }

        /// <summary>
        /// Retrieves the specified component types.
        /// </summary>
        public readonly void GetComponentTypes<T1, T2, T3, T4, T5>(out int c1, out int c2, out int c3, out int c4, out int c5) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
        {
            c1 = GetComponentType<T1>();
            c2 = GetComponentType<T2>();
            c3 = GetComponentType<T3>();
            c4 = GetComponentType<T4>();
            c5 = GetComponentType<T5>();
        }

        /// <summary>
        /// Retrieves a mask of the specified component types.
        /// </summary>
        public readonly BitMask GetComponentTypes<T1, T2, T3, T4, T5, T6>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
        {
            return new(GetComponentType<T1>(), GetComponentType<T2>(), GetComponentType<T3>(), GetComponentType<T4>(), GetComponentType<T5>(), GetComponentType<T6>());
        }

        /// <summary>
        /// Retrieves the specified component types.
        /// </summary>
        public readonly void GetComponentTypes<T1, T2, T3, T4, T5, T6>(out int c1, out int c2, out int c3, out int c4, out int c5, out int c6) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
        {
            c1 = GetComponentType<T1>();
            c2 = GetComponentType<T2>();
            c3 = GetComponentType<T3>();
            c4 = GetComponentType<T4>();
            c5 = GetComponentType<T5>();
            c6 = GetComponentType<T6>();
        }

        /// <summary>
        /// Retrieves a mask of the specified component types.
        /// </summary>
        public readonly BitMask GetComponentTypes<T1, T2, T3, T4, T5, T6, T7>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged
        {
            return new(GetComponentType<T1>(), GetComponentType<T2>(), GetComponentType<T3>(), GetComponentType<T4>(), GetComponentType<T5>(), GetComponentType<T6>(), GetComponentType<T7>());
        }

        /// <summary>
        /// Retrieves the specified component types.
        /// </summary>
        public readonly void GetComponentTypes<T1, T2, T3, T4, T5, T6, T7>(out int c1, out int c2, out int c3, out int c4, out int c5, out int c6, out int c7) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged
        {
            c1 = GetComponentType<T1>();
            c2 = GetComponentType<T2>();
            c3 = GetComponentType<T3>();
            c4 = GetComponentType<T4>();
            c5 = GetComponentType<T5>();
            c6 = GetComponentType<T6>();
            c7 = GetComponentType<T7>();
        }

        /// <summary>
        /// Retrieves a mask of the specified component types.
        /// </summary>
        public readonly BitMask GetComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged
        {
            return new(GetComponentType<T1>(), GetComponentType<T2>(), GetComponentType<T3>(), GetComponentType<T4>(), GetComponentType<T5>(), GetComponentType<T6>(), GetComponentType<T7>(), GetComponentType<T8>());
        }

        /// <summary>
        /// Retrieves the specified component types.
        /// </summary>
        public readonly void GetComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8>(out int c1, out int c2, out int c3, out int c4, out int c5, out int c6, out int c7, out int c8) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged
        {
            c1 = GetComponentType<T1>();
            c2 = GetComponentType<T2>();
            c3 = GetComponentType<T3>();
            c4 = GetComponentType<T4>();
            c5 = GetComponentType<T5>();
            c6 = GetComponentType<T6>();
            c7 = GetComponentType<T7>();
            c8 = GetComponentType<T8>();
        }

        /// <summary>
        /// Retrieves a mask of the specified component types.
        /// </summary>
        public readonly BitMask GetComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged
        {
            return new(GetComponentType<T1>(), GetComponentType<T2>(), GetComponentType<T3>(), GetComponentType<T4>(), GetComponentType<T5>(), GetComponentType<T6>(), GetComponentType<T7>(), GetComponentType<T8>(), GetComponentType<T9>());
        }

        /// <summary>
        /// Retrieves the specified component types.
        /// </summary>
        public readonly void GetComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9>(out int c1, out int c2, out int c3, out int c4, out int c5, out int c6, out int c7, out int c8, out int c9) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged
        {
            c1 = GetComponentType<T1>();
            c2 = GetComponentType<T2>();
            c3 = GetComponentType<T3>();
            c4 = GetComponentType<T4>();
            c5 = GetComponentType<T5>();
            c6 = GetComponentType<T6>();
            c7 = GetComponentType<T7>();
            c8 = GetComponentType<T8>();
            c9 = GetComponentType<T9>();
        }

        /// <summary>
        /// Retrieves a mask of the specified component types.
        /// </summary>
        public readonly BitMask GetComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged
        {
            return new(GetComponentType<T1>(), GetComponentType<T2>(), GetComponentType<T3>(), GetComponentType<T4>(), GetComponentType<T5>(), GetComponentType<T6>(), GetComponentType<T7>(), GetComponentType<T8>(), GetComponentType<T9>(), GetComponentType<T10>());
        }

        /// <summary>
        /// Retrieves the specified component types.
        /// </summary>
        public readonly void GetComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(out int c1, out int c2, out int c3, out int c4, out int c5, out int c6, out int c7, out int c8, out int c9, out int c10) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged
        {
            c1 = GetComponentType<T1>();
            c2 = GetComponentType<T2>();
            c3 = GetComponentType<T3>();
            c4 = GetComponentType<T4>();
            c5 = GetComponentType<T5>();
            c6 = GetComponentType<T6>();
            c7 = GetComponentType<T7>();
            c8 = GetComponentType<T8>();
            c9 = GetComponentType<T9>();
            c10 = GetComponentType<T10>();
        }

        /// <summary>
        /// Retrieves a mask of the specified component types.
        /// </summary>
        public readonly BitMask GetComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged
        {
            return new(GetComponentType<T1>(), GetComponentType<T2>(), GetComponentType<T3>(), GetComponentType<T4>(), GetComponentType<T5>(), GetComponentType<T6>(), GetComponentType<T7>(), GetComponentType<T8>(), GetComponentType<T9>(), GetComponentType<T10>(), GetComponentType<T11>());
        }

        /// <summary>
        /// Retrieves the specified component types.
        /// </summary>
        public readonly void GetComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(out int c1, out int c2, out int c3, out int c4, out int c5, out int c6, out int c7, out int c8, out int c9, out int c10, out int c11) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged
        {
            c1 = GetComponentType<T1>();
            c2 = GetComponentType<T2>();
            c3 = GetComponentType<T3>();
            c4 = GetComponentType<T4>();
            c5 = GetComponentType<T5>();
            c6 = GetComponentType<T6>();
            c7 = GetComponentType<T7>();
            c8 = GetComponentType<T8>();
            c9 = GetComponentType<T9>();
            c10 = GetComponentType<T10>();
            c11 = GetComponentType<T11>();
        }

        /// <summary>
        /// Retrieves a mask of the specified component types.
        /// </summary>
        public readonly BitMask GetComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged
        {
            return new(GetComponentType<T1>(), GetComponentType<T2>(), GetComponentType<T3>(), GetComponentType<T4>(), GetComponentType<T5>(), GetComponentType<T6>(), GetComponentType<T7>(), GetComponentType<T8>(), GetComponentType<T9>(), GetComponentType<T10>(), GetComponentType<T11>(), GetComponentType<T12>());
        }

        /// <summary>
        /// Retrieves the specified component types.
        /// </summary>
        public readonly void GetComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(out int c1, out int c2, out int c3, out int c4, out int c5, out int c6, out int c7, out int c8, out int c9, out int c10, out int c11, out int c12) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged
        {
            c1 = GetComponentType<T1>();
            c2 = GetComponentType<T2>();
            c3 = GetComponentType<T3>();
            c4 = GetComponentType<T4>();
            c5 = GetComponentType<T5>();
            c6 = GetComponentType<T6>();
            c7 = GetComponentType<T7>();
            c8 = GetComponentType<T8>();
            c9 = GetComponentType<T9>();
            c10 = GetComponentType<T10>();
            c11 = GetComponentType<T11>();
            c12 = GetComponentType<T12>();
        }

        /// <summary>
        /// Retrieves a mask of the specified component types.
        /// </summary>
        public readonly BitMask GetComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged
        {
            return new(GetComponentType<T1>(), GetComponentType<T2>(), GetComponentType<T3>(), GetComponentType<T4>(), GetComponentType<T5>(), GetComponentType<T6>(), GetComponentType<T7>(), GetComponentType<T8>(), GetComponentType<T9>(), GetComponentType<T10>(), GetComponentType<T11>(), GetComponentType<T12>(), GetComponentType<T13>());
        }

        /// <summary>
        /// Retrieves the specified component types.
        /// </summary>
        public readonly void GetComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(out int c1, out int c2, out int c3, out int c4, out int c5, out int c6, out int c7, out int c8, out int c9, out int c10, out int c11, out int c12, out int c13) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged
        {
            c1 = GetComponentType<T1>();
            c2 = GetComponentType<T2>();
            c3 = GetComponentType<T3>();
            c4 = GetComponentType<T4>();
            c5 = GetComponentType<T5>();
            c6 = GetComponentType<T6>();
            c7 = GetComponentType<T7>();
            c8 = GetComponentType<T8>();
            c9 = GetComponentType<T9>();
            c10 = GetComponentType<T10>();
            c11 = GetComponentType<T11>();
            c12 = GetComponentType<T12>();
            c13 = GetComponentType<T13>();
        }

        /// <summary>
        /// Retrieves a mask of the specified component types.
        /// </summary>
        public readonly BitMask GetComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged
        {
            return new(GetComponentType<T1>(), GetComponentType<T2>(), GetComponentType<T3>(), GetComponentType<T4>(), GetComponentType<T5>(), GetComponentType<T6>(), GetComponentType<T7>(), GetComponentType<T8>(), GetComponentType<T9>(), GetComponentType<T10>(), GetComponentType<T11>(), GetComponentType<T12>(), GetComponentType<T13>(), GetComponentType<T14>());
        }

        /// <summary>
        /// Retrieves the specified component types.
        /// </summary>
        public readonly void GetComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(out int c1, out int c2, out int c3, out int c4, out int c5, out int c6, out int c7, out int c8, out int c9, out int c10, out int c11, out int c12, out int c13, out int c14) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged
        {
            c1 = GetComponentType<T1>();
            c2 = GetComponentType<T2>();
            c3 = GetComponentType<T3>();
            c4 = GetComponentType<T4>();
            c5 = GetComponentType<T5>();
            c6 = GetComponentType<T6>();
            c7 = GetComponentType<T7>();
            c8 = GetComponentType<T8>();
            c9 = GetComponentType<T9>();
            c10 = GetComponentType<T10>();
            c11 = GetComponentType<T11>();
            c12 = GetComponentType<T12>();
            c13 = GetComponentType<T13>();
            c14 = GetComponentType<T14>();
        }

        /// <summary>
        /// Retrieves a mask of the specified component types.
        /// </summary>
        public readonly BitMask GetComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged where T15 : unmanaged
        {
            return new(GetComponentType<T1>(), GetComponentType<T2>(), GetComponentType<T3>(), GetComponentType<T4>(), GetComponentType<T5>(), GetComponentType<T6>(), GetComponentType<T7>(), GetComponentType<T8>(), GetComponentType<T9>(), GetComponentType<T10>(), GetComponentType<T11>(), GetComponentType<T12>(), GetComponentType<T13>(), GetComponentType<T14>(), GetComponentType<T15>());
        }

        /// <summary>
        /// Retrieves the specified component types.
        /// </summary>
        public readonly void GetComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(out int c1, out int c2, out int c3, out int c4, out int c5, out int c6, out int c7, out int c8, out int c9, out int c10, out int c11, out int c12, out int c13, out int c14, out int c15) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged where T15 : unmanaged
        {
            c1 = GetComponentType<T1>();
            c2 = GetComponentType<T2>();
            c3 = GetComponentType<T3>();
            c4 = GetComponentType<T4>();
            c5 = GetComponentType<T5>();
            c6 = GetComponentType<T6>();
            c7 = GetComponentType<T7>();
            c8 = GetComponentType<T8>();
            c9 = GetComponentType<T9>();
            c10 = GetComponentType<T10>();
            c11 = GetComponentType<T11>();
            c12 = GetComponentType<T12>();
            c13 = GetComponentType<T13>();
            c14 = GetComponentType<T14>();
            c15 = GetComponentType<T15>();
        }

        /// <summary>
        /// Retrieves a mask of the specified component types.
        /// </summary>
        public readonly BitMask GetComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged where T15 : unmanaged where T16 : unmanaged
        {
            return new(GetComponentType<T1>(), GetComponentType<T2>(), GetComponentType<T3>(), GetComponentType<T4>(), GetComponentType<T5>(), GetComponentType<T6>(), GetComponentType<T7>(), GetComponentType<T8>(), GetComponentType<T9>(), GetComponentType<T10>(), GetComponentType<T11>(), GetComponentType<T12>(), GetComponentType<T13>(), GetComponentType<T14>(), GetComponentType<T15>(), GetComponentType<T16>());
        }

        /// <summary>
        /// Retrieves the specified component types.
        /// </summary>
        public readonly void GetComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(out int c1, out int c2, out int c3, out int c4, out int c5, out int c6, out int c7, out int c8, out int c9, out int c10, out int c11, out int c12, out int c13, out int c14, out int c15, out int c16) where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged where T15 : unmanaged where T16 : unmanaged
        {
            c1 = GetComponentType<T1>();
            c2 = GetComponentType<T2>();
            c3 = GetComponentType<T3>();
            c4 = GetComponentType<T4>();
            c5 = GetComponentType<T5>();
            c6 = GetComponentType<T6>();
            c7 = GetComponentType<T7>();
            c8 = GetComponentType<T8>();
            c9 = GetComponentType<T9>();
            c10 = GetComponentType<T10>();
            c11 = GetComponentType<T11>();
            c12 = GetComponentType<T12>();
            c13 = GetComponentType<T13>();
            c14 = GetComponentType<T14>();
            c15 = GetComponentType<T15>();
            c16 = GetComponentType<T16>();
        }

        /// <summary>
        /// Retrieves a mask of the specified array types.
        /// </summary>
        public readonly BitMask GetArrayTypes<T1>() where T1 : unmanaged
        {
            return new(GetArrayType<T1>());
        }

        /// <summary>
        /// Retrieves a mask of the specified array types.
        /// </summary>
        public readonly BitMask GetArrayTypes<T1, T2>() where T1 : unmanaged where T2 : unmanaged
        {
            return new(GetArrayType<T1>(), GetArrayType<T2>());
        }

        /// <summary>
        /// Retrieves a mask of the specified array types.
        /// </summary>
        public readonly BitMask GetArrayTypes<T1, T2, T3>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            return new(GetArrayType<T1>(), GetArrayType<T2>(), GetArrayType<T3>());
        }

        /// <summary>
        /// Retrieves a mask of the specified array types.
        /// </summary>
        public readonly BitMask GetArrayTypes<T1, T2, T3, T4>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            return new(GetArrayType<T1>(), GetArrayType<T2>(), GetArrayType<T3>(), GetArrayType<T4>());
        }

        /// <summary>
        /// Retrieves a mask of the specified array types.
        /// </summary>
        public readonly BitMask GetArrayTypes<T1, T2, T3, T4, T5>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
        {
            return new(GetArrayType<T1>(), GetArrayType<T2>(), GetArrayType<T3>(), GetArrayType<T4>(), GetArrayType<T5>());
        }

        /// <summary>
        /// Retrieves a mask of the specified array types.
        /// </summary>
        public readonly BitMask GetArrayTypes<T1, T2, T3, T4, T5, T6>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
        {
            return new(GetArrayType<T1>(), GetArrayType<T2>(), GetArrayType<T3>(), GetArrayType<T4>(), GetArrayType<T5>(), GetArrayType<T6>());
        }

        /// <summary>
        /// Retrieves a mask of the specified array types.
        /// </summary>
        public readonly BitMask GetArrayTypes<T1, T2, T3, T4, T5, T6, T7>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged
        {
            return new(GetArrayType<T1>(), GetArrayType<T2>(), GetArrayType<T3>(), GetArrayType<T4>(), GetArrayType<T5>(), GetArrayType<T6>(), GetArrayType<T7>());
        }

        /// <summary>
        /// Retrieves a mask of the specified array types.
        /// </summary>
        public readonly BitMask GetArrayTypes<T1, T2, T3, T4, T5, T6, T7, T8>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged
        {
            return new(GetArrayType<T1>(), GetArrayType<T2>(), GetArrayType<T3>(), GetArrayType<T4>(), GetArrayType<T5>(), GetArrayType<T6>(), GetArrayType<T7>(), GetArrayType<T8>());
        }

        /// <summary>
        /// Retrieves a mask of the specified array types.
        /// </summary>
        public readonly BitMask GetArrayTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged
        {
            return new(GetArrayType<T1>(), GetArrayType<T2>(), GetArrayType<T3>(), GetArrayType<T4>(), GetArrayType<T5>(), GetArrayType<T6>(), GetArrayType<T7>(), GetArrayType<T8>(), GetArrayType<T9>());
        }

        /// <summary>
        /// Retrieves a mask of the specified array types.
        /// </summary>
        public readonly BitMask GetArrayTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged
        {
            return new(GetArrayType<T1>(), GetArrayType<T2>(), GetArrayType<T3>(), GetArrayType<T4>(), GetArrayType<T5>(), GetArrayType<T6>(), GetArrayType<T7>(), GetArrayType<T8>(), GetArrayType<T9>(), GetArrayType<T10>());
        }

        /// <summary>
        /// Retrieves a mask of the specified array types.
        /// </summary>
        public readonly BitMask GetArrayTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged
        {
            return new(GetArrayType<T1>(), GetArrayType<T2>(), GetArrayType<T3>(), GetArrayType<T4>(), GetArrayType<T5>(), GetArrayType<T6>(), GetArrayType<T7>(), GetArrayType<T8>(), GetArrayType<T9>(), GetArrayType<T10>(), GetArrayType<T11>());
        }

        /// <summary>
        /// Retrieves a mask of the specified array types.
        /// </summary>
        public readonly BitMask GetArrayTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged
        {
            return new(GetArrayType<T1>(), GetArrayType<T2>(), GetArrayType<T3>(), GetArrayType<T4>(), GetArrayType<T5>(), GetArrayType<T6>(), GetArrayType<T7>(), GetArrayType<T8>(), GetArrayType<T9>(), GetArrayType<T10>(), GetArrayType<T11>(), GetArrayType<T12>());
        }

        /// <summary>
        /// Retrieves a mask of the specified tag types.
        /// </summary>
        public readonly BitMask GetTagTypes<T1>() where T1 : unmanaged
        {
            return new(GetTagType<T1>());
        }

        /// <summary>
        /// Retrieves a mask of the specified tag types.
        /// </summary>
        public readonly BitMask GetTagTypes<T1, T2>() where T1 : unmanaged where T2 : unmanaged
        {
            return new(GetTagType<T1>(), GetTagType<T2>());
        }

        /// <summary>
        /// Retrieves a mask of the specified tag types.
        /// </summary>
        public readonly BitMask GetTagTypes<T1, T2, T3>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            return new(GetTagType<T1>(), GetTagType<T2>(), GetTagType<T3>());
        }

        /// <summary>
        /// Retrieves a mask of the specified tag types.
        /// </summary>
        public readonly BitMask GetTagTypes<T1, T2, T3, T4>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            return new(GetTagType<T1>(), GetTagType<T2>(), GetTagType<T3>(), GetTagType<T4>());
        }

        /// <summary>
        /// Retrieves a mask of the specified tag types.
        /// </summary>
        public readonly BitMask GetTagTypes<T1, T2, T3, T4, T5>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
        {
            return new(GetTagType<T1>(), GetTagType<T2>(), GetTagType<T3>(), GetTagType<T4>(), GetTagType<T5>());
        }

        /// <summary>
        /// Retrieves a mask of the specified tag types.
        /// </summary>
        public readonly BitMask GetTagTypes<T1, T2, T3, T4, T5, T6>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
        {
            return new(GetTagType<T1>(), GetTagType<T2>(), GetTagType<T3>(), GetTagType<T4>(), GetTagType<T5>(), GetTagType<T6>());
        }

        /// <summary>
        /// Retrieves a mask of the specified tag types.
        /// </summary>
        public readonly BitMask GetTagTypes<T1, T2, T3, T4, T5, T6, T7>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged
        {
            return new(GetTagType<T1>(), GetTagType<T2>(), GetTagType<T3>(), GetTagType<T4>(), GetTagType<T5>(), GetTagType<T6>(), GetTagType<T7>());
        }

        /// <summary>
        /// Retrieves a mask of the specified tag types.
        /// </summary>
        public readonly BitMask GetTagTypes<T1, T2, T3, T4, T5, T6, T7, T8>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged
        {
            return new(GetTagType<T1>(), GetTagType<T2>(), GetTagType<T3>(), GetTagType<T4>(), GetTagType<T5>(), GetTagType<T6>(), GetTagType<T7>(), GetTagType<T8>());
        }

        /// <summary>
        /// Retrieves a mask of the specified tag types.
        /// </summary>
        public readonly BitMask GetTagTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged
        {
            return new(GetTagType<T1>(), GetTagType<T2>(), GetTagType<T3>(), GetTagType<T4>(), GetTagType<T5>(), GetTagType<T6>(), GetTagType<T7>(), GetTagType<T8>(), GetTagType<T9>());
        }

        /// <summary>
        /// Retrieves a mask of the specified tag types.
        /// </summary>
        public readonly BitMask GetTagTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged
        {
            return new(GetTagType<T1>(), GetTagType<T2>(), GetTagType<T3>(), GetTagType<T4>(), GetTagType<T5>(), GetTagType<T6>(), GetTagType<T7>(), GetTagType<T8>(), GetTagType<T9>(), GetTagType<T10>());
        }

        /// <summary>
        /// Retrieves a mask of the specified tag types.
        /// </summary>
        public readonly BitMask GetTagTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged
        {
            return new(GetTagType<T1>(), GetTagType<T2>(), GetTagType<T3>(), GetTagType<T4>(), GetTagType<T5>(), GetTagType<T6>(), GetTagType<T7>(), GetTagType<T8>(), GetTagType<T9>(), GetTagType<T10>(), GetTagType<T11>());
        }

        /// <summary>
        /// Retrieves a mask of the specified tag types.
        /// </summary>
        public readonly BitMask GetTagTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged
        {
            return new(GetTagType<T1>(), GetTagType<T2>(), GetTagType<T3>(), GetTagType<T4>(), GetTagType<T5>(), GetTagType<T6>(), GetTagType<T7>(), GetTagType<T8>(), GetTagType<T9>(), GetTagType<T10>(), GetTagType<T11>(), GetTagType<T12>());
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
            schema->definitionMask = Definition.Default;
            schema->componentOffsets = MemoryAddress.AllocateZeroed(OffsetsLengthInBytes);
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
                throw new Exception("Too many components types registered in schema");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfTooManyArrays()
        {
            if (schema->arraysCount == BitMask.MaxValue)
            {
                throw new Exception("Too many array types registered in schema");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfTooManyTags()
        {
            if (schema->tagsCount == BitMask.MaxValue)
            {
                throw new Exception("Too many tag types registered in schema");
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
        private readonly void ThrowIfComponentTypeIsMissing(TypeMetadata componentType)
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
        private readonly void ThrowIfArrayTypeIsMissing(TypeMetadata arrayType)
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
        private readonly void ThrowIfTagIsMissing(TypeMetadata tagType)
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
            Span<int> offsets = new(schema->componentOffsets.Pointer, BitMask.Capacity);
            Span<int> sizes = new(schema->sizes.Pointer, BitMask.Capacity * 2);
            Span<long> typeHashes = new(schema->typeHashes.Pointer, BitMask.Capacity * 3);
            writer.WriteSpan(offsets);
            writer.WriteSpan(sizes);
            writer.WriteSpan(typeHashes);
        }

        void ISerializable.Read(ByteReader reader)
        {
            schema = MemoryAddress.AllocatePointer<SchemaPointer>();
            schema->componentOffsets = MemoryAddress.AllocateZeroed(OffsetsLengthInBytes);
            schema->sizes = MemoryAddress.AllocateZeroed(SizesLengthInBytes);
            schema->typeHashes = MemoryAddress.AllocateZeroed(TypeHashesLengthInBytes);
            schema->componentCount = reader.ReadValue<byte>();
            schema->arraysCount = reader.ReadValue<byte>();
            schema->tagsCount = reader.ReadValue<byte>();
            schema->componentRowSize = reader.ReadValue<int>();
            schema->definitionMask = reader.ReadValue<Definition>();
            schema->componentOffsets.CopyFrom(reader.ReadSpan<int>(BitMask.Capacity));
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
            schema->definitionMask = Definition.Default;
            schema->componentOffsets.Clear(OffsetsLengthInBytes);
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

                    Span<TypeMetadata> componentTypes = new(schema.schema->typeHashes.Pointer, BitMask.Capacity);
                    int componentType = componentTypes.IndexOf(TypeMetadata.Get<T>());
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

                    Span<TypeMetadata> arrayTypes = new(schema.schema->typeHashes.Pointer + BitMask.Capacity * sizeof(long), BitMask.Capacity);
                    int arrayType = arrayTypes.IndexOf(TypeMetadata.Get<T>());
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

                    Span<TypeMetadata> tagTypes = new(schema.schema->typeHashes.Pointer + BitMask.Capacity * sizeof(long) * 2, BitMask.Capacity);
                    int tagType = tagTypes.IndexOf(TypeMetadata.Get<T>());
                    tags[schemaIndex] = tagType;
                    return tagType;
                }
            }
        }
    }
}