using Collections;
using System;
using System.Diagnostics;
using Types;
using Unmanaged;
using Worlds.Functions;

namespace Worlds
{
    public unsafe struct Schema : IDisposable, IEquatable<Schema>, ISerializable
    {
        private static int globalSchemaCount;

        private Implementation* schema;

        public readonly bool IsDisposed => schema is null;
        public readonly void* Pointer => schema;
        public readonly nint Address => (nint)schema;

        /// <summary>
        /// Counts how many <see cref="ComponentType"/>s are registered.
        /// </summary>
        public readonly byte ComponentCount => schema->componentCount;

        /// <summary>
        /// Counts how many <see cref="ArrayElementType"/>s are registered.
        /// </summary>
        public readonly byte ArrayCount => schema->arraysCount;

        /// <summary>
        /// Counts how many <see cref="TagType"/>s are registered.
        /// </summary>
        public readonly byte TagCount => schema->tagsCount;

        /// <summary>
        /// All component types loaded.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly System.Collections.Generic.IEnumerable<ComponentType> ComponentTypes
        {
            get
            {
                for (uint c = 0; c < BitMask.Capacity; c++)
                {
                    ComponentType componentType = new(c);
                    if (Contains(componentType))
                    {
                        yield return componentType;
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
                    ArrayElementType arrayElementType = new(a);
                    if (Contains(arrayElementType))
                    {
                        yield return arrayElementType;
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
                    TagType tagType = new(t);
                    if (Contains(tagType))
                    {
                        yield return tagType;
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
                    ComponentType componentType = new(c);
                    if (Contains(componentType))
                    {
                        buffer[count++] = GetComponentLayout(componentType);
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
                    ArrayElementType arrayElementType = new(a);
                    if (Contains(arrayElementType))
                    {
                        buffer[count++] = GetArrayElementLayout(arrayElementType);
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
                    TagType tagType = new(t);
                    if (Contains(tagType))
                    {
                        buffer[count++] = GetTagLayout(tagType);
                    }
                }

                return buffer;
            }
        }

#if NET
        public Schema()
        {
            schema = Implementation.Allocate();
        }
#endif

        public Schema(void* pointer)
        {
            schema = (Implementation*)pointer;
        }

        public void Dispose()
        {
            Implementation.Free(ref schema);
        }

        /// <summary>
        /// Copies the state of this schema into the <paramref name="destination"/>.
        /// </summary>
        public readonly void CopyTo(Schema destination)
        {
            Implementation.CopyTo(schema, destination.schema);
        }

        /// <summary>
        /// Copies the state of the <paramref name="source"/> schema entirely.
        /// </summary>
        public readonly void CopyFrom(Schema source)
        {
            Implementation.CopyTo(source.schema, schema);
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
            ThrowIfComponentIsMissing(componentType);

            return new(componentType, GetSize(componentType));
        }

        public readonly DataType GetDataType(ArrayElementType arrayElementType)
        {
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
            ThrowIfComponentIsMissing(componentType);

            USpan<long> componentTypeHashes = Implementation.GetComponentTypeHashes(schema);
            long hash = componentTypeHashes[(uint)componentType];
            return TypeRegistry.Get(hash);
        }

        /// <summary>
        /// Retrieves the type layout for the given <paramref name="arrayElementType"/>.
        /// </summary>
        public readonly TypeLayout GetArrayElementLayout(ArrayElementType arrayElementType)
        {
            ThrowIfArrayElementIsMissing(arrayElementType);

            USpan<long> arrayTypeHashes = Implementation.GetArrayTypeHashes(schema);
            long hash = arrayTypeHashes[(uint)arrayElementType];
            return TypeRegistry.Get(hash);
        }

        /// <summary>
        /// Retrieves the type layout for the given <paramref name="tagType"/>.
        /// </summary>
        public readonly TypeLayout GetTagLayout(TagType tagType)
        {
            ThrowIfTagIsMissing(tagType);

            USpan<long> tagTypeHashes = Implementation.GetTagTypeHashes(schema);
            long hash = tagTypeHashes[(uint)tagType];
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
            //ThrowIfComponentAlreadyRegistered(type);

            USpan<ushort> componentSizes = Implementation.GetComponentSizes(schema);
            USpan<long> componentHashes = Implementation.GetComponentTypeHashes(schema);
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
            //ThrowIfArrayElementAlreadyRegistered(type);

            ArrayElementType arrayElementType = new(schema->arraysCount);
            USpan<ushort> arrayElementSizes = Implementation.GetArrayElementSizes(schema);
            USpan<long> arrayElementHashes = Implementation.GetArrayTypeHashes(schema);
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
            //ThrowIfTagAlreadyRegistered(type);

            TagType tagType = new(schema->tagsCount);
            USpan<long> tagHashes = Implementation.GetTagTypeHashes(schema);
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

        public readonly bool ContainsComponent(FixedString fullTypeName)
        {
            long hash = fullTypeName.GetLongHashCode();
            USpan<long> componentTypeHashes = Implementation.GetComponentTypeHashes(schema);
            return componentTypeHashes.Contains(hash);
        }

        public readonly bool ContainsComponent(TypeLayout type)
        {
            long hash = type.Hash;
            USpan<long> componentTypeHashes = Implementation.GetComponentTypeHashes(schema);
            return componentTypeHashes.Contains(hash);
        }

        public readonly bool TryGetComponentType(TypeLayout type, out ComponentType componentType)
        {
            long hash = type.Hash;
            USpan<long> componentTypeHashes = Implementation.GetComponentTypeHashes(schema);
            bool contains = componentTypeHashes.TryIndexOf(hash, out uint index);
            componentType = new((byte)index);
            return contains;
        }

        public readonly bool ContainsArrayElement(FixedString fullTypeName)
        {
            long hash = fullTypeName.GetLongHashCode();
            USpan<long> arrayTypeHashes = Implementation.GetArrayTypeHashes(schema);
            return arrayTypeHashes.Contains(hash);
        }

        public readonly bool ContainsArrayElement(TypeLayout type)
        {
            long hash = type.Hash;
            USpan<long> arrayTypeHashes = Implementation.GetArrayTypeHashes(schema);
            return arrayTypeHashes.Contains(hash);
        }

        public readonly bool TryGetArrayElementType(TypeLayout type, out ArrayElementType arrayElementType)
        {
            long hash = type.Hash;
            USpan<long> arrayTypeHashes = Implementation.GetArrayTypeHashes(schema);
            bool contains = arrayTypeHashes.TryIndexOf(hash, out uint index);
            arrayElementType = new((byte)index);
            return contains;
        }

        public readonly bool ContainsTag(FixedString fullTypeName)
        {
            long hash = fullTypeName.GetLongHashCode();
            USpan<long> tagTypeHashes = Implementation.GetTagTypeHashes(schema);
            return tagTypeHashes.Contains(hash);
        }

        public readonly bool ContainsTag(TypeLayout type)
        {
            long hash = type.Hash;
            USpan<long> tagTypeHashes = Implementation.GetTagTypeHashes(schema);
            return tagTypeHashes.Contains(hash);
        }

        public readonly bool TryGetTagType(TypeLayout type, out TagType tagType)
        {
            long hash = type.Hash;
            USpan<long> tagTypeHashes = Implementation.GetTagTypeHashes(schema);
            bool contains = tagTypeHashes.TryIndexOf(hash, out uint index);
            tagType = new((byte)index);
            return contains;
        }

        public readonly bool ContainsComponent<T>() where T : unmanaged
        {
            if (!TypeRegistry.IsRegistered<T>())
            {
                return false;
            }

            long hash = TypeLayoutHashCodeCache<T>.value;
            USpan<long> componentTypeHashes = Implementation.GetComponentTypeHashes(schema);
            return componentTypeHashes.Contains(hash);
        }

        public readonly ComponentType GetComponent<T>() where T : unmanaged
        {
            ThrowIfComponentIsMissing<T>();

            if (!SchemaTypeCache<T>.TryGetComponent(this, out ComponentType componentType))
            {
                long hash = TypeLayoutHashCodeCache<T>.value;
                USpan<long> componentTypeHashes = Implementation.GetComponentTypeHashes(schema);
                uint index = componentTypeHashes.IndexOf(hash);
                componentType = new((byte)index);
                SchemaTypeCache<T>.Set(this, componentType);
            }

            return componentType;
        }

        public readonly DataType GetComponentDataType<T>() where T : unmanaged
        {
            ThrowIfComponentIsMissing<T>();

            if (!SchemaTypeCache<T>.TryGetComponent(this, out ComponentType componentType))
            {
                long hash = TypeLayoutHashCodeCache<T>.value;
                USpan<long> componentTypeHashes = Implementation.GetComponentTypeHashes(schema);
                uint index = componentTypeHashes.IndexOf(hash);
                componentType = new((byte)index);
                SchemaTypeCache<T>.Set(this, componentType);
            }

            return new(componentType, (ushort)sizeof(T));
        }

        public readonly DataType GetComponentDataType(TypeLayout type)
        {
            ThrowIfComponentIsMissing(type);

            USpan<long> componentTypeHashes = Implementation.GetComponentTypeHashes(schema);
            uint index = componentTypeHashes.IndexOf(type.Hash);
            return new((byte)index, DataType.Kind.Component, type.Size);
        }

        public readonly bool ContainsArrayElement<T>() where T : unmanaged
        {
            if (!TypeRegistry.IsRegistered<T>())
            {
                return false;
            }

            long hash = TypeLayoutHashCodeCache<T>.value;
            USpan<long> arrayTypeHashes = Implementation.GetArrayTypeHashes(schema);
            return arrayTypeHashes.Contains(hash);
        }

        public readonly ArrayElementType GetArrayElement<T>() where T : unmanaged
        {
            ThrowIfArrayElementIsMissing<T>();

            if (!SchemaTypeCache<T>.TryGetArrayElement(this, out ArrayElementType arrayElementType))
            {
                long hash = TypeLayoutHashCodeCache<T>.value;
                USpan<long> arrayTypeHashes = Implementation.GetArrayTypeHashes(schema);
                uint index = arrayTypeHashes.IndexOf(hash);
                arrayElementType = new((byte)index);
                SchemaTypeCache<T>.Set(this, arrayElementType);
            }

            return arrayElementType;
        }

        public readonly DataType GetArrayElementDataType<T>() where T : unmanaged
        {
            ThrowIfArrayElementIsMissing<T>();

            if (!SchemaTypeCache<T>.TryGetArrayElement(this, out ArrayElementType arrayElementType))
            {
                long hash = TypeLayoutHashCodeCache<T>.value;
                USpan<long> arrayTypeHashes = Implementation.GetArrayTypeHashes(schema);
                uint index = arrayTypeHashes.IndexOf(hash);
                arrayElementType = new((byte)index);
                SchemaTypeCache<T>.Set(this, arrayElementType);
            }

            return new(arrayElementType, (ushort)sizeof(T));
        }

        public readonly DataType GetArrayElementDataType(TypeLayout type)
        {
            ThrowIfArrayElementIsMissing(type);

            USpan<long> arrayTypeHashes = Implementation.GetArrayTypeHashes(schema);
            uint index = arrayTypeHashes.IndexOf(type.Hash);
            return new((byte)index, DataType.Kind.ArrayElement, type.Size);
        }

        public readonly TagType GetTag<T>() where T : unmanaged
        {
            ThrowIfTagIsMissing<T>();

            if (!SchemaTypeCache<T>.TryGetTag(this, out TagType tagType))
            {
                long hash = TypeLayoutHashCodeCache<T>.value;
                USpan<long> tagTypeHashes = Implementation.GetTagTypeHashes(schema);
                uint index = tagTypeHashes.IndexOf(hash);
                tagType = new((byte)index);
                SchemaTypeCache<T>.Set(this, tagType);
            }

            return tagType;
        }

        public readonly DataType GetTagDataType<T>() where T : unmanaged
        {
            ThrowIfTagIsMissing<T>();

            if (!SchemaTypeCache<T>.TryGetTag(this, out TagType tagType))
            {
                long hash = TypeLayoutHashCodeCache<T>.value;
                USpan<long> tagTypeHashes = Implementation.GetTagTypeHashes(schema);
                uint index = tagTypeHashes.IndexOf(hash);
                tagType = new((byte)index);
                SchemaTypeCache<T>.Set(this, tagType);
            }

            return new(tagType);
        }

        public readonly bool ContainsTag<T>() where T : unmanaged
        {
            if (!TypeRegistry.IsRegistered<T>())
            {
                return false;
            }

            long hash = TypeLayoutHashCodeCache<T>.value;
            USpan<long> tagTypeHashes = Implementation.GetTagTypeHashes(schema);
            return tagTypeHashes.Contains(hash);
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
            return new(Implementation.Allocate());
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
            USpan<ushort> componentSizes = Implementation.GetComponentSizes(schema);
            USpan<ushort> arrayElementSizes = Implementation.GetArrayElementSizes(schema);
            USpan<long> componentTypeHashes = Implementation.GetComponentTypeHashes(schema);
            USpan<long> arrayTypeHashes = Implementation.GetArrayTypeHashes(schema);
            USpan<long> tagTypeHashes = Implementation.GetTagTypeHashes(schema);
            writer.WriteSpan(componentSizes);
            writer.WriteSpan(arrayElementSizes);
            writer.WriteSpan(componentTypeHashes);
            writer.WriteSpan(arrayTypeHashes);
            writer.WriteSpan(tagTypeHashes);
        }

        void ISerializable.Read(ByteReader reader)
        {
            schema = Implementation.Allocate();
            schema->componentCount = reader.ReadValue<byte>();
            schema->arraysCount = reader.ReadValue<byte>();
            schema->tagsCount = reader.ReadValue<byte>();
            schema->tagsMask = reader.ReadValue<BitMask>();
            USpan<ushort> componentSizes = Implementation.GetComponentSizes(schema);
            USpan<ushort> arrayElementSizes = Implementation.GetArrayElementSizes(schema);
            USpan<long> componentHashes = Implementation.GetComponentTypeHashes(schema);
            USpan<long> arrayHashes = Implementation.GetArrayTypeHashes(schema);
            USpan<long> tagHashes = Implementation.GetTagTypeHashes(schema);
            componentSizes.CopyFrom(reader.ReadSpan<ushort>(BitMask.Capacity));
            arrayElementSizes.CopyFrom(reader.ReadSpan<ushort>(BitMask.Capacity));
            componentHashes.CopyFrom(reader.ReadSpan<long>(BitMask.Capacity));
            arrayHashes.CopyFrom(reader.ReadSpan<long>(BitMask.Capacity));
            tagHashes.CopyFrom(reader.ReadSpan<long>(BitMask.Capacity));
        }

        /// <summary>
        /// Resets the schema to its <c>default</c> state.
        /// </summary>
        public readonly void Clear()
        {
            Implementation.Clear(schema);
        }

        public static bool operator ==(Schema left, Schema right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Schema left, Schema right)
        {
            return !(left == right);
        }

        public struct Implementation
        {
            private const uint SizesLength = sizeof(ushort) * BitMask.Capacity * 2;
            private const uint TypeHashesLength = sizeof(long) * BitMask.Capacity * 3;

            public byte componentCount;
            public byte arraysCount;
            public byte tagsCount;
            public BitMask tagsMask;
            public readonly int schemaCount;
            public readonly Allocation sizes;
            public readonly Allocation typeHashes;

            private Implementation(Allocation sizes, Allocation typeHashes, int schemaCount)
            {
                this.componentCount = 0;
                this.arraysCount = 0;
                this.tagsCount = 0;
                this.sizes = sizes;
                this.typeHashes = typeHashes;
                this.schemaCount = schemaCount;
            }

            public static Implementation* Allocate()
            {
                Allocation sizes = Allocation.CreateZeroed(SizesLength);
                Allocation typeHashes = Allocation.CreateZeroed(TypeHashesLength);
                ref Implementation schema = ref Allocations.Allocate<Implementation>();
                schema = new(sizes, typeHashes, globalSchemaCount);
                globalSchemaCount++;

                fixed (Implementation* pointer = &schema)
                {
                    return pointer;
                }
            }

            public static void Free(ref Implementation* schema)
            {
                Allocations.ThrowIfNull(schema);

                schema->sizes.Dispose();
                schema->typeHashes.Dispose();
                Allocations.Free(ref schema);
            }

            public static void Clear(Implementation* schema)
            {
                schema->componentCount = 0;
                schema->arraysCount = 0;
                schema->tagsCount = 0;
                schema->sizes.Clear(SizesLength);
                schema->typeHashes.Clear(TypeHashesLength);
            }

            public static void CopyTo(Implementation* source, Implementation* destination)
            {
                destination->componentCount = source->componentCount;
                destination->arraysCount = source->arraysCount;
                destination->tagsCount = source->tagsCount;
                destination->tagsMask = source->tagsMask;
                source->sizes.CopyTo(destination->sizes, SizesLength);
                source->typeHashes.CopyTo(destination->typeHashes, TypeHashesLength);
            }

            public static USpan<ushort> GetComponentSizes(Implementation* schema)
            {
                return schema->sizes.GetSpan<ushort>(BitMask.Capacity);
            }

            public static USpan<ushort> GetArrayElementSizes(Implementation* schema)
            {
                return schema->sizes.AsSpan<ushort>(BitMask.Capacity, BitMask.Capacity);
            }

            public static USpan<long> GetComponentTypeHashes(Implementation* schema)
            {
                return schema->typeHashes.GetSpan<long>(BitMask.Capacity);
            }

            public static USpan<long> GetArrayTypeHashes(Implementation* schema)
            {
                return schema->typeHashes.AsSpan<long>(BitMask.Capacity, BitMask.Capacity);
            }

            public static USpan<long> GetTagTypeHashes(Implementation* schema)
            {
                return schema->typeHashes.AsSpan<long>(BitMask.Capacity * 2, BitMask.Capacity);
            }
        }

        internal static class TypeLayoutHashCodeCache<T> where T : unmanaged
        {
            public static readonly long value = TypeRegistry.Get<T>().Hash;
        }

        internal static class SchemaTypeCache<T> where T : unmanaged
        {
            private static readonly System.Collections.Generic.List<ComponentType> components = new();
            private static readonly System.Collections.Generic.List<ArrayElementType> arrayElements = new();
            private static readonly System.Collections.Generic.List<TagType> tags = new();

            public static void Set(Schema schema, ComponentType componentType)
            {
                while (schema.schema->schemaCount >= components.Count)
                {
                    components.Add(default);
                }

                components[schema.schema->schemaCount] = componentType;
            }

            public static void Set(Schema schema, ArrayElementType arrayElementType)
            {
                while (schema.schema->schemaCount >= arrayElements.Count)
                {
                    arrayElements.Add(default);
                }

                arrayElements[schema.schema->schemaCount] = arrayElementType;
            }

            public static void Set(Schema schema, TagType tagType)
            {
                while (schema.schema->schemaCount >= tags.Count)
                {
                    tags.Add(default);
                }

                tags[schema.schema->schemaCount] = tagType;
            }

            public static bool TryGetComponent(Schema schema, out ComponentType componentType)
            {
                if (schema.schema->schemaCount < components.Count)
                {
                    componentType = components[schema.schema->schemaCount];
                    return true;
                }

                componentType = default;
                return false;
            }

            public static bool TryGetArrayElement(Schema schema, out ArrayElementType arrayElementType)
            {
                if (schema.schema->schemaCount < arrayElements.Count)
                {
                    arrayElementType = arrayElements[schema.schema->schemaCount];
                    return true;
                }

                arrayElementType = default;
                return false;
            }

            public static bool TryGetTag(Schema schema, out TagType tagType)
            {
                if (schema.schema->schemaCount < tags.Count)
                {
                    tagType = tags[schema.schema->schemaCount];
                    return true;
                }

                tagType = default;
                return false;
            }
        }
    }
}