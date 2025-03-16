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
        public const int DisabledTagType = BitMask.MaxValue;

        internal const int OffsetsLengthInBytes = sizeof(int) * BitMask.Capacity;
        internal const int SizesLengthInBytes = sizeof(int) * BitMask.Capacity * 2;
        internal const int TypeHashesLengthInBytes = sizeof(long) * BitMask.Capacity * 3;

        private static int createdSchemas;

        private Pointer* schema;

        public readonly bool IsDisposed => schema is null;
        public readonly void* Pointer => schema;
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
        private readonly TypeLayout[] Arrays
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

            schema->offsets.Dispose();
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
            destination.schema->componentRowSize = schema->componentRowSize;
            destination.schema->offsets.CopyTo(schema->offsets, OffsetsLengthInBytes);
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

        public readonly int GetComponentOffset<T>() where T : unmanaged
        {
            ThrowIfComponentTypeIsMissing<T>();

            return schema->offsets.ReadElement<int>(GetComponentType<T>());
        }

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

        private static void Register(RegisterDataType.Input input)
        {
            if (input.kind == DataType.Kind.Component)
            {
                input.schema.RegisterComponent(input.type);
            }
            else if (input.kind == DataType.Kind.Array)
            {
                input.schema.RegisterArray(input.type);
            }
            else if (input.kind == DataType.Kind.Tag)
            {
                input.schema.RegisterTag(input.type);
            }
        }

        public readonly DataType GetComponentDataType(int componentType)
        {
            MemoryAddress.ThrowIfDefault(schema);
            ThrowIfComponentTypeIsMissing(componentType);

            return new(componentType, DataType.Kind.Component, GetComponentSize(componentType));
        }

        public readonly DataType GetArrayDataType(int arrayType)
        {
            MemoryAddress.ThrowIfDefault(schema);
            ThrowIfArrayTypeIsMissing(arrayType);

            return new(arrayType, DataType.Kind.Array, GetArraySize(arrayType));
        }

        public readonly DataType GetTagDataType(int tagType)
        {
            ThrowIfTagIsMissing(tagType);

            return new(tagType, DataType.Kind.Tag, 1);
        }

        /// <summary>
        /// Retrieves the type layout for the given <paramref name="componentType"/>.
        /// </summary>
        public readonly TypeLayout GetComponentLayout(int componentType)
        {
            MemoryAddress.ThrowIfDefault(schema);
            ThrowIfComponentTypeIsMissing(componentType);

            Span<long> componentTypeHashes = new(schema->typeHashes.Pointer, BitMask.Capacity);
            long hash = componentTypeHashes[componentType];
            return TypeRegistry.Get(hash);
        }

        /// <summary>
        /// Retrieves the type layout for the given <paramref name="arrayType"/>.
        /// </summary>
        public readonly TypeLayout GetArrayLayout(int arrayType)
        {
            MemoryAddress.ThrowIfDefault(schema);
            ThrowIfArrayTypeIsMissing(arrayType);

            Span<long> arrayTypeHashes = schema->typeHashes.AsSpan<long>(BitMask.Capacity, BitMask.Capacity);
            long hash = arrayTypeHashes[arrayType];
            return TypeRegistry.Get(hash);
        }

        /// <summary>
        /// Retrieves the type layout for the given <paramref name="tagType"/>.
        /// </summary>
        public readonly TypeLayout GetTagLayout(int tagType)
        {
            MemoryAddress.ThrowIfDefault(schema);
            ThrowIfTagIsMissing(tagType);

            Span<long> tagTypeHashes = schema->typeHashes.AsSpan<long>(BitMask.Capacity * 2, BitMask.Capacity);
            long hash = tagTypeHashes[tagType];
            return TypeRegistry.Get(hash);
        }

        public readonly TypeLayout GetComponentLayout<T>() where T : unmanaged
        {
            return GetComponentLayout(GetComponentType<T>());
        }

        public readonly int RegisterComponent<T>() where T : unmanaged
        {
            int newComponentType = RegisterComponent(TypeRegistry.GetOrRegister<T>());
            SchemaTypeCache<T>.SetComponentType(this, newComponentType);
            return newComponentType;
        }

        public readonly int RegisterComponent(TypeLayout type)
        {
            if (TryGetComponentType(type, out int componentType))
            {
                return componentType;
            }

            ThrowIfTooManyComponents();

            componentType = schema->componentCount;
            schema->offsets.WriteElement(componentType, schema->componentRowSize);
            schema->sizes.WriteElement(componentType, type.Size);
            schema->typeHashes.WriteElement(componentType, type.Hash);
            schema->componentCount++;
            schema->componentRowSize += type.Size;
            schema->definitionMask.AddComponentType(componentType);
            return componentType;
        }

        public readonly int RegisterArray<T>() where T : unmanaged
        {
            int arrayType = RegisterArray(TypeRegistry.GetOrRegister<T>());
            SchemaTypeCache<T>.SetArrayType(this, arrayType);
            return arrayType;
        }

        public readonly int RegisterArray(TypeLayout type)
        {
            if (TryGetArrayType(type, out int arrayType))
            {
                return arrayType;
            }

            ThrowIfTooManyArrays();

            arrayType = schema->arraysCount;
            Span<int> arraySizes = schema->sizes.AsSpan<int>(BitMask.Capacity, BitMask.Capacity);
            Span<long> arrayHashes = schema->typeHashes.AsSpan<long>(BitMask.Capacity, BitMask.Capacity);
            arraySizes[schema->arraysCount] = type.Size;
            arrayHashes[schema->arraysCount] = type.Hash;
            schema->arraysCount++;
            schema->definitionMask.AddArrayType(arrayType);
            return arrayType;
        }

        public readonly int RegisterTag<T>() where T : unmanaged
        {
            int tagType = RegisterTag(TypeRegistry.GetOrRegister<T>());
            SchemaTypeCache<T>.SetTagType(this, tagType);
            return tagType;
        }

        public readonly int RegisterTag(TypeLayout type)
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

        public readonly bool ContainsComponentType(int componentType)
        {
            MemoryAddress.ThrowIfDefault(schema);

            return schema->definitionMask.componentTypes.Contains(componentType);
        }

        public readonly bool ContainsArrayType(int arrayType)
        {
            MemoryAddress.ThrowIfDefault(schema);

            return schema->definitionMask.arrayTypes.Contains(arrayType);
        }

        public readonly bool ContainsTagType(int tagType)
        {
            MemoryAddress.ThrowIfDefault(schema);

            return schema->definitionMask.tagTypes.Contains(tagType);
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

        public readonly bool TryGetComponentType(TypeLayout type, out int componentType)
        {
            MemoryAddress.ThrowIfDefault(schema);

            Span<long> componentTypeHashes = new(schema->typeHashes.Pointer, BitMask.Capacity);
            return componentTypeHashes.TryIndexOf(type.Hash, out componentType);
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

        public readonly bool TryGetArrayType(TypeLayout type, out int arrayType)
        {
            Span<long> arrayTypeHashes = schema->typeHashes.AsSpan<long>(BitMask.Capacity, BitMask.Capacity);
            return arrayTypeHashes.TryIndexOf(type.Hash, out arrayType);
        }

        public readonly bool ContainsTagType(ASCIIText256 fullTypeName)
        {
            Span<long> tagTypeHashes = schema->typeHashes.AsSpan<long>(BitMask.Capacity * 2, BitMask.Capacity);
            return tagTypeHashes.Contains(fullTypeName.GetLongHashCode());
        }

        public readonly bool ContainsTagType(TypeLayout type)
        {
            Span<long> tagTypeHashes = schema->typeHashes.AsSpan<long>(BitMask.Capacity * 2, BitMask.Capacity);
            return tagTypeHashes.Contains(type.Hash);
        }

        public readonly bool TryGetTagType(TypeLayout type, out int tagType)
        {
            Span<long> tagTypeHashes = schema->typeHashes.AsSpan<long>(BitMask.Capacity * 2, BitMask.Capacity);
            return tagTypeHashes.TryIndexOf(type.Hash, out tagType);
        }

        public readonly bool ContainsComponentType<T>() where T : unmanaged
        {
            if (!TypeRegistry.IsRegistered<T>())
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

            if (!SchemaTypeCache<T>.TryGetComponentType(this, out int componentType))
            {
                Span<long> componentTypeHashes = new(schema->typeHashes.Pointer, BitMask.Capacity);
                componentType = componentTypeHashes.IndexOf(TypeLayoutHashCodeCache<T>.value);
                SchemaTypeCache<T>.SetComponentType(this, componentType);
                Trace.WriteLine($"Cached component type for {typeof(T).FullName}");
            }

            return componentType;
        }

        public readonly DataType GetComponentDataType<T>() where T : unmanaged
        {
            ThrowIfComponentTypeIsMissing<T>();

            if (!SchemaTypeCache<T>.TryGetComponentType(this, out int componentType))
            {
                Span<long> componentTypeHashes = new(schema->typeHashes.Pointer, BitMask.Capacity);
                componentType = componentTypeHashes.IndexOf(TypeLayoutHashCodeCache<T>.value);
                SchemaTypeCache<T>.SetComponentType(this, componentType);
                Trace.WriteLine($"Cached component type for {typeof(T).FullName}");
            }

            return new(componentType, DataType.Kind.Component, sizeof(T));
        }

        public readonly DataType GetComponentDataType(TypeLayout type)
        {
            ThrowIfComponentTypeIsMissing(type);

            Span<long> componentTypeHashes = new(schema->typeHashes.Pointer, BitMask.Capacity);
            int componentType = componentTypeHashes.IndexOf(type.Hash);
            return new(componentType, DataType.Kind.Component, type.Size);
        }

        public readonly bool ContainsArrayType<T>() where T : unmanaged
        {
            if (!TypeRegistry.IsRegistered<T>())
            {
                return false;
            }

            Span<long> arrayTypeHashes = schema->typeHashes.AsSpan<long>(BitMask.Capacity, BitMask.Capacity);
            return arrayTypeHashes.Contains(TypeLayoutHashCodeCache<T>.value);
        }

        public readonly int GetArrayType<T>() where T : unmanaged
        {
            ThrowIfArrayTypeIsMissing<T>();

            if (!SchemaTypeCache<T>.TryGetArrayType(this, out int arrayType))
            {
                Span<long> arrayTypeHashes = schema->typeHashes.AsSpan<long>(BitMask.Capacity, BitMask.Capacity);
                arrayType = arrayTypeHashes.IndexOf(TypeLayoutHashCodeCache<T>.value);
                SchemaTypeCache<T>.SetArrayType(this, arrayType);
                Trace.WriteLine($"Cached array type for {typeof(T).FullName}");
            }

            return arrayType;
        }

        public readonly int GetArrayTypeIndex<T>() where T : unmanaged
        {
            ThrowIfArrayTypeIsMissing<T>();

            if (!SchemaTypeCache<T>.TryGetArrayType(this, out int arrayType))
            {
                Span<long> arrayTypeHashes = schema->typeHashes.AsSpan<long>(BitMask.Capacity, BitMask.Capacity);
                arrayType = arrayTypeHashes.IndexOf(TypeLayoutHashCodeCache<T>.value);
                SchemaTypeCache<T>.SetArrayType(this, arrayType);
                Trace.WriteLine($"Cached array type for {typeof(T).FullName}");
            }

            return arrayType;
        }

        public readonly DataType GetArrayDataType<T>() where T : unmanaged
        {
            ThrowIfArrayTypeIsMissing<T>();

            if (!SchemaTypeCache<T>.TryGetArrayType(this, out int arrayType))
            {
                Span<long> arrayTypeHashes = schema->typeHashes.AsSpan<long>(BitMask.Capacity, BitMask.Capacity);
                arrayType = arrayTypeHashes.IndexOf(TypeLayoutHashCodeCache<T>.value);
                SchemaTypeCache<T>.SetArrayType(this, arrayType);
                Trace.WriteLine($"Cached array type for {typeof(T).FullName}");
            }

            return new(arrayType, DataType.Kind.Array, sizeof(T));
        }

        public readonly DataType GetArrayDataType(TypeLayout type)
        {
            ThrowIfArrayTypeIsMissing(type);

            Span<long> arrayTypeHashes = schema->typeHashes.AsSpan<long>(BitMask.Capacity, BitMask.Capacity);
            int arrayType = arrayTypeHashes.IndexOf(type.Hash);
            return new(arrayType, DataType.Kind.Array, type.Size);
        }

        public readonly int GetTagType<T>() where T : unmanaged
        {
            ThrowIfTagIsMissing<T>();

            if (!SchemaTypeCache<T>.TryGetTagType(this, out int tagType))
            {
                Span<long> tagTypeHashes = schema->typeHashes.AsSpan<long>(BitMask.Capacity * 2, BitMask.Capacity);
                tagType = tagTypeHashes.IndexOf(TypeLayoutHashCodeCache<T>.value);
                SchemaTypeCache<T>.SetTagType(this, tagType);
                Trace.WriteLine($"Cached tag type for {typeof(T).FullName}");
            }

            return tagType;
        }

        public readonly DataType GetTagDataType<T>() where T : unmanaged
        {
            ThrowIfTagIsMissing<T>();

            if (!SchemaTypeCache<T>.TryGetTagType(this, out int tagType))
            {
                Span<long> tagTypeHashes = schema->typeHashes.AsSpan<long>(BitMask.Capacity * 2, BitMask.Capacity);
                tagType = tagTypeHashes.IndexOf(TypeLayoutHashCodeCache<T>.value);
                SchemaTypeCache<T>.SetTagType(this, tagType);
                Trace.WriteLine($"Cached tag type for {typeof(T).FullName}");
            }

            return new(tagType, DataType.Kind.Tag, default);
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

        public readonly BitMask GetComponentTypes<T1>() where T1 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetComponentType<T1>());
            return bitMask;
        }

        public readonly BitMask GetComponentTypes<T1, T2>() where T1 : unmanaged where T2 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetComponentType<T1>());
            bitMask.Set(GetComponentType<T2>());
            return bitMask;
        }

        public readonly BitMask GetComponentTypes<T1, T2, T3>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetComponentType<T1>());
            bitMask.Set(GetComponentType<T2>());
            bitMask.Set(GetComponentType<T3>());
            return bitMask;
        }

        public readonly BitMask GetComponentTypes<T1, T2, T3, T4>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetComponentType<T1>());
            bitMask.Set(GetComponentType<T2>());
            bitMask.Set(GetComponentType<T3>());
            bitMask.Set(GetComponentType<T4>());
            return bitMask;
        }

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

        public readonly BitMask GetArrayTypes<T1>() where T1 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetArrayTypeIndex<T1>());
            return bitMask;
        }

        public readonly BitMask GetArrayTypes<T1, T2>() where T1 : unmanaged where T2 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetArrayTypeIndex<T1>());
            bitMask.Set(GetArrayTypeIndex<T2>());
            return bitMask;
        }

        public readonly BitMask GetArrayTypes<T1, T2, T3>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetArrayTypeIndex<T1>());
            bitMask.Set(GetArrayTypeIndex<T2>());
            bitMask.Set(GetArrayTypeIndex<T3>());
            return bitMask;
        }

        public readonly BitMask GetArrayTypes<T1, T2, T3, T4>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetArrayTypeIndex<T1>());
            bitMask.Set(GetArrayTypeIndex<T2>());
            bitMask.Set(GetArrayTypeIndex<T3>());
            bitMask.Set(GetArrayTypeIndex<T4>());
            return bitMask;
        }

        public readonly BitMask GetArrayTypes<T1, T2, T3, T4, T5>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetArrayTypeIndex<T1>());
            bitMask.Set(GetArrayTypeIndex<T2>());
            bitMask.Set(GetArrayTypeIndex<T3>());
            bitMask.Set(GetArrayTypeIndex<T4>());
            bitMask.Set(GetArrayTypeIndex<T5>());
            return bitMask;
        }

        public readonly BitMask GetArrayTypes<T1, T2, T3, T4, T5, T6>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
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

        public readonly BitMask GetArrayTypes<T1, T2, T3, T4, T5, T6, T7>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged
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

        public readonly BitMask GetArrayTypes<T1, T2, T3, T4, T5, T6, T7, T8>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged
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

        public readonly BitMask GetArrayTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged
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

        public readonly BitMask GetArrayTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged
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

        public readonly BitMask GetArrayTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged
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

        public readonly BitMask GetArrayTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged
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

        public readonly BitMask GetTagTypes<T1>() where T1 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetTagType<T1>());
            return bitMask;
        }

        public readonly BitMask GetTagTypes<T1, T2>() where T1 : unmanaged where T2 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetTagType<T1>());
            bitMask.Set(GetTagType<T2>());
            return bitMask;
        }

        public readonly BitMask GetTagTypes<T1, T2, T3>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetTagType<T1>());
            bitMask.Set(GetTagType<T2>());
            bitMask.Set(GetTagType<T3>());
            return bitMask;
        }

        public readonly BitMask GetTagTypes<T1, T2, T3, T4>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            BitMask bitMask = default;
            bitMask.Set(GetTagType<T1>());
            bitMask.Set(GetTagType<T2>());
            bitMask.Set(GetTagType<T3>());
            bitMask.Set(GetTagType<T4>());
            return bitMask;
        }

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
        private readonly void ThrowIfArrayTypeIsMissing(int arrayType)
        {
            if (!ContainsArrayType(arrayType))
            {
                throw new Exception($"Array type size for `{arrayType}` is missing from schema");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfArrayTypeIsMissing(TypeLayout arrayType)
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
        private readonly void ThrowIfArrayAlreadyRegistered<T>() where T : unmanaged
        {
            if (ContainsArrayType<T>())
            {
                throw new Exception($"Array `{typeof(T).FullName}` is already registered in schema");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfArrayAlreadyRegistered(TypeLayout type)
        {
            if (ContainsArrayType(type))
            {
                throw new Exception($"Array `{type}` is already registered in schema");
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
            schema->componentRowSize = reader.ReadValue<int>();
            schema->definitionMask = reader.ReadValue<Definition>();
            schema->offsets.CopyFrom(reader.ReadSpan<int>(BitMask.Capacity));
            schema->sizes.CopyFrom(reader.ReadSpan<int>(BitMask.Capacity * 2));
            schema->typeHashes.CopyFrom(reader.ReadSpan<long>(BitMask.Capacity * 3));
        }

        /// <summary>
        /// Resets the schema to its <c>default</c> state.
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
            private static int[] arrays;
            private static int[] tags;
            private static int componentCapacity;
            private static int arrayCapacity;
            private static int tagCapacity;

            static SchemaTypeCache()
            {
                components = new int[0];
                arrays = new int[0];
                tags = new int[0];
                componentCapacity = 0;
                arrayCapacity = 0;
                tagCapacity = 0;
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

            public static bool TryGetArrayType(Schema schema, out int arrayType)
            {
                if (schema.schema->schemaIndex < arrayCapacity)
                {
                    arrayType = arrays[schema.schema->schemaIndex];
                    return true;
                }

                arrayType = default;
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