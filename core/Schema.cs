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
        private Implementation* schema;

        public readonly bool IsDisposed => schema is null;
        public readonly void* Pointer => schema;
        public readonly nint Address => (nint)schema;

        /// <summary>
        /// All component types loaded.
        /// </summary>
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

            return schema->sizes.Read<ushort>(componentType.index * 2u);
        }

        public readonly ushort GetSize(ArrayElementType arrayElementType)
        {
            ThrowIfArrayElementIsMissing(arrayElementType);

            return schema->sizes.Read<ushort>(BitMask.Capacity * 2 + arrayElementType.index * 2u);
        }

        /// <summary>
        /// Loads all types from the given bank into the schema.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public readonly void Load<T>() where T : unmanaged, ISchemaBank
        {
            T bank = default;
            bank.Load(new(this, Register));
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
            long hash = componentTypeHashes[componentType];
            return TypeRegistry.Get(hash);
        }

        /// <summary>
        /// Retrieves the type layout for the given <paramref name="arrayElementType"/>.
        /// </summary>
        public readonly TypeLayout GetArrayElementLayout(ArrayElementType arrayElementType)
        {
            ThrowIfArrayElementIsMissing(arrayElementType);

            USpan<long> arrayTypeHashes = Implementation.GetArrayTypeHashes(schema);
            long hash = arrayTypeHashes[arrayElementType];
            return TypeRegistry.Get(hash);
        }

        /// <summary>
        /// Retrieves the type layout for the given <paramref name="tagType"/>.
        /// </summary>
        public readonly TypeLayout GetTagLayout(TagType tagType)
        {
            ThrowIfTagIsMissing(tagType);

            USpan<long> tagTypeHashes = Implementation.GetTagTypeHashes(schema);
            long hash = tagTypeHashes[tagType];
            return TypeRegistry.Get(hash);
        }

        public readonly TypeLayout GetComponentLayout<T>() where T : unmanaged
        {
            return GetComponentLayout(GetComponent<T>());
        }

        public readonly ComponentType RegisterComponent<T>() where T : unmanaged
        {
            return RegisterComponent(TypeRegistry.Get<T>());
        }

        public readonly ComponentType RegisterComponent(TypeLayout type)
        {
            ThrowIfTooManyComponents();
            ThrowIfComponentAlreadyRegistered(type);

            USpan<ushort> componentSizes = Implementation.GetComponentSizes(schema);
            USpan<long> componentHashes = Implementation.GetComponentTypeHashes(schema);
            ComponentType componentType = new(schema->componentCount);
            componentSizes[componentType] = type.Size;
            componentHashes[componentType] = type.Hash;
            schema->componentCount++;
            return componentType;
        }

        public readonly ArrayElementType RegisterArrayElement<T>() where T : unmanaged
        {
            return RegisterArrayElement(TypeRegistry.Get<T>());
        }

        public readonly ArrayElementType RegisterArrayElement(TypeLayout type)
        {
            ThrowIfTooManyArrays();
            ThrowIfArrayElementAlreadyRegistered(type);

            ArrayElementType arrayElementType = new(schema->arraysCount);
            USpan<ushort> arrayElementSizes = Implementation.GetArrayElementSizes(schema);
            USpan<long> arrayElementHashes = Implementation.GetArrayTypeHashes(schema);
            arrayElementSizes[arrayElementType] = type.Size;
            arrayElementHashes[arrayElementType] = type.Hash;
            schema->arraysCount++;
            return arrayElementType;
        }

        public readonly TagType RegisterTag<T>() where T : unmanaged
        {
            return RegisterTag(TypeRegistry.Get<T>());
        }

        public readonly TagType RegisterTag(TypeLayout type)
        {
            ThrowIfTooManyTags();
            ThrowIfTagAlreadyRegistered(type);

            TagType tagType = new(schema->tagsCount);
            USpan<long> tagHashes = Implementation.GetTagTypeHashes(schema);
            tagHashes[tagType] = type.Hash;
            schema->tagsMask.Set(tagType);
            schema->tagsCount++;
            return tagType;
        }

        public readonly bool Contains(ComponentType componentType)
        {
            return schema->sizes.Read<ushort>(componentType.index * 2u) != default;
        }

        public readonly bool Contains(ArrayElementType arrayElementType)
        {
            return schema->sizes.Read<ushort>(BitMask.Capacity * 2 + arrayElementType.index * 2u) != default;
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

        public readonly bool ContainsComponent<T>() where T : unmanaged
        {
            long hash = TypeLayoutHashCodeCache<T>.value;
            USpan<long> componentTypeHashes = Implementation.GetComponentTypeHashes(schema);
            return componentTypeHashes.Contains(hash);
        }

        public readonly ComponentType GetComponent<T>() where T : unmanaged
        {
            ThrowIfComponentIsMissing<T>();

            long hash = TypeLayoutHashCodeCache<T>.value;
            USpan<long> componentTypeHashes = Implementation.GetComponentTypeHashes(schema);
            uint index = componentTypeHashes.IndexOf(hash);
            return new((byte)index);
        }

        public readonly DataType GetComponentDataType<T>() where T : unmanaged
        {
            ThrowIfComponentIsMissing<T>();

            long hash = TypeLayoutHashCodeCache<T>.value;
            USpan<long> componentTypeHashes = Implementation.GetComponentTypeHashes(schema);
            uint index = componentTypeHashes.IndexOf(hash);
            ComponentType componentType = new((byte)index);
            return new(componentType, (ushort)sizeof(T));
        }

        public readonly DataType GetComponentDataType(TypeLayout type)
        {
            ThrowIfComponentIsMissing(type);

            USpan<long> componentTypeHashes = Implementation.GetComponentTypeHashes(schema);
            uint index = componentTypeHashes.IndexOf(type.Hash);
            ComponentType componentType = new((byte)index);
            return new(componentType, type.Size);
        }

        public readonly bool ContainsArrayElement<T>() where T : unmanaged
        {
            long hash = TypeLayoutHashCodeCache<T>.value;
            USpan<long> arrayTypeHashes = Implementation.GetArrayTypeHashes(schema);
            return arrayTypeHashes.Contains(hash);
        }

        public readonly ArrayElementType GetArrayElement<T>() where T : unmanaged
        {
            ThrowIfArrayElementIsMissing<T>();

            long hash = TypeLayoutHashCodeCache<T>.value;
            USpan<long> arrayTypeHashes = Implementation.GetArrayTypeHashes(schema);
            uint index = arrayTypeHashes.IndexOf(hash);
            return new((byte)index);
        }

        public readonly DataType GetArrayElementDataType<T>() where T : unmanaged
        {
            ThrowIfArrayElementIsMissing<T>();

            long hash = TypeLayoutHashCodeCache<T>.value;
            USpan<long> arrayTypeHashes = Implementation.GetArrayTypeHashes(schema);
            uint index = arrayTypeHashes.IndexOf(hash);
            ArrayElementType arrayElementType = new((byte)index);
            return new(arrayElementType, (ushort)sizeof(T));
        }

        public readonly DataType GetArrayElementDataType(TypeLayout type)
        {
            ThrowIfArrayElementIsMissing(type);

            USpan<long> arrayTypeHashes = Implementation.GetArrayTypeHashes(schema);
            uint index = arrayTypeHashes.IndexOf(type.Hash);
            ArrayElementType arrayElementType = new((byte)index);
            return new(arrayElementType, type.Size);
        }

        public readonly TagType GetTag<T>() where T : unmanaged
        {
            ThrowIfTagIsMissing<T>();

            long hash = TypeLayoutHashCodeCache<T>.value;
            USpan<long> tagTypeHashes = Implementation.GetTagTypeHashes(schema);
            uint index = tagTypeHashes.IndexOf(hash);
            return new((byte)index);
        }

        public readonly DataType GetTagDataType<T>() where T : unmanaged
        {
            ThrowIfTagIsMissing<T>();

            long hash = TypeLayoutHashCodeCache<T>.value;
            USpan<long> tagTypeHashes = Implementation.GetTagTypeHashes(schema);
            uint index = tagTypeHashes.IndexOf(hash);
            TagType tagType = new((byte)index);
            return new(tagType);
        }

        public readonly bool ContainsTag<T>() where T : unmanaged
        {
            long hash = TypeLayoutHashCodeCache<T>.value;
            USpan<long> tagTypeHashes = Implementation.GetTagTypeHashes(schema);
            return tagTypeHashes.Contains(hash);
        }

        public readonly BitMask GetComponents<T1>() where T1 : unmanaged
        {
            return new(GetComponent<T1>());
        }

        public readonly BitMask GetComponents<T1, T2>() where T1 : unmanaged where T2 : unmanaged
        {
            return new(GetComponent<T1>(), GetComponent<T2>());
        }

        public readonly BitMask GetComponents<T1, T2, T3>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            return new(GetComponent<T1>(), GetComponent<T2>(), GetComponent<T3>());
        }

        public readonly BitMask GetComponents<T1, T2, T3, T4>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            return new(GetComponent<T1>(), GetComponent<T2>(), GetComponent<T3>(), GetComponent<T4>());
        }

        public readonly BitMask GetComponents<T1, T2, T3, T4, T5>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
        {
            return new(GetComponent<T1>(), GetComponent<T2>(), GetComponent<T3>(), GetComponent<T4>(), GetComponent<T5>());
        }

        public readonly BitMask GetComponents<T1, T2, T3, T4, T5, T6>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
        {
            return new(GetComponent<T1>(), GetComponent<T2>(), GetComponent<T3>(), GetComponent<T4>(), GetComponent<T5>(), GetComponent<T6>());
        }

        public readonly BitMask GetComponents<T1, T2, T3, T4, T5, T6, T7>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged
        {
            return new(GetComponent<T1>(), GetComponent<T2>(), GetComponent<T3>(), GetComponent<T4>(), GetComponent<T5>(), GetComponent<T6>(), GetComponent<T7>());
        }

        public readonly BitMask GetComponents<T1, T2, T3, T4, T5, T6, T7, T8>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged
        {
            return new(GetComponent<T1>(), GetComponent<T2>(), GetComponent<T3>(), GetComponent<T4>(), GetComponent<T5>(), GetComponent<T6>(), GetComponent<T7>(), GetComponent<T8>());
        }

        public readonly BitMask GetComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged
        {
            return new(GetComponent<T1>(), GetComponent<T2>(), GetComponent<T3>(), GetComponent<T4>(), GetComponent<T5>(), GetComponent<T6>(), GetComponent<T7>(), GetComponent<T8>(), GetComponent<T9>());
        }

        public readonly BitMask GetComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged
        {
            return new(GetComponent<T1>(), GetComponent<T2>(), GetComponent<T3>(), GetComponent<T4>(), GetComponent<T5>(), GetComponent<T6>(), GetComponent<T7>(), GetComponent<T8>(), GetComponent<T9>(), GetComponent<T10>());
        }

        public readonly BitMask GetComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged
        {
            return new(GetComponent<T1>(), GetComponent<T2>(), GetComponent<T3>(), GetComponent<T4>(), GetComponent<T5>(), GetComponent<T6>(), GetComponent<T7>(), GetComponent<T8>(), GetComponent<T9>(), GetComponent<T10>(), GetComponent<T11>());
        }

        public readonly BitMask GetComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged
        {
            return new(GetComponent<T1>(), GetComponent<T2>(), GetComponent<T3>(), GetComponent<T4>(), GetComponent<T5>(), GetComponent<T6>(), GetComponent<T7>(), GetComponent<T8>(), GetComponent<T9>(), GetComponent<T10>(), GetComponent<T11>(), GetComponent<T12>());
        }

        public readonly BitMask GetComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged
        {
            return new(GetComponent<T1>(), GetComponent<T2>(), GetComponent<T3>(), GetComponent<T4>(), GetComponent<T5>(), GetComponent<T6>(), GetComponent<T7>(), GetComponent<T8>(), GetComponent<T9>(), GetComponent<T10>(), GetComponent<T11>(), GetComponent<T12>(), GetComponent<T13>());
        }

        public readonly BitMask GetComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged
        {
            return new(GetComponent<T1>(), GetComponent<T2>(), GetComponent<T3>(), GetComponent<T4>(), GetComponent<T5>(), GetComponent<T6>(), GetComponent<T7>(), GetComponent<T8>(), GetComponent<T9>(), GetComponent<T10>(), GetComponent<T11>(), GetComponent<T12>(), GetComponent<T13>(), GetComponent<T14>());
        }

        public readonly BitMask GetComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged where T15 : unmanaged
        {
            return new(GetComponent<T1>(), GetComponent<T2>(), GetComponent<T3>(), GetComponent<T4>(), GetComponent<T5>(), GetComponent<T6>(), GetComponent<T7>(), GetComponent<T8>(), GetComponent<T9>(), GetComponent<T10>(), GetComponent<T11>(), GetComponent<T12>(), GetComponent<T13>(), GetComponent<T14>(), GetComponent<T15>());
        }

        public readonly BitMask GetComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged where T15 : unmanaged where T16 : unmanaged
        {
            return new(GetComponent<T1>(), GetComponent<T2>(), GetComponent<T3>(), GetComponent<T4>(), GetComponent<T5>(), GetComponent<T6>(), GetComponent<T7>(), GetComponent<T8>(), GetComponent<T9>(), GetComponent<T10>(), GetComponent<T11>(), GetComponent<T12>(), GetComponent<T13>(), GetComponent<T14>(), GetComponent<T15>(), GetComponent<T16>());
        }

        public readonly BitMask GetArrayElements<T1>() where T1 : unmanaged
        {
            return new(GetArrayElement<T1>());
        }

        public readonly BitMask GetArrayElements<T1, T2>() where T1 : unmanaged where T2 : unmanaged
        {
            return new(GetArrayElement<T1>(), GetArrayElement<T2>());
        }

        public readonly BitMask GetArrayElements<T1, T2, T3>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            return new(GetArrayElement<T1>(), GetArrayElement<T2>(), GetArrayElement<T3>());
        }

        public readonly BitMask GetArrayElements<T1, T2, T3, T4>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            return new(GetArrayElement<T1>(), GetArrayElement<T2>(), GetArrayElement<T3>(), GetArrayElement<T4>());
        }

        public readonly BitMask GetArrayElements<T1, T2, T3, T4, T5>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
        {
            return new(GetArrayElement<T1>(), GetArrayElement<T2>(), GetArrayElement<T3>(), GetArrayElement<T4>(), GetArrayElement<T5>());
        }

        public readonly BitMask GetArrayElements<T1, T2, T3, T4, T5, T6>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
        {
            return new(GetArrayElement<T1>(), GetArrayElement<T2>(), GetArrayElement<T3>(), GetArrayElement<T4>(), GetArrayElement<T5>(), GetArrayElement<T6>());
        }

        public readonly BitMask GetArrayElements<T1, T2, T3, T4, T5, T6, T7>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged
        {
            return new(GetArrayElement<T1>(), GetArrayElement<T2>(), GetArrayElement<T3>(), GetArrayElement<T4>(), GetArrayElement<T5>(), GetArrayElement<T6>(), GetArrayElement<T7>());
        }

        public readonly BitMask GetArrayElements<T1, T2, T3, T4, T5, T6, T7, T8>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged
        {
            return new(GetArrayElement<T1>(), GetArrayElement<T2>(), GetArrayElement<T3>(), GetArrayElement<T4>(), GetArrayElement<T5>(), GetArrayElement<T6>(), GetArrayElement<T7>(), GetArrayElement<T8>());
        }

        public readonly BitMask GetArrayElements<T1, T2, T3, T4, T5, T6, T7, T8, T9>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged
        {
            return new(GetArrayElement<T1>(), GetArrayElement<T2>(), GetArrayElement<T3>(), GetArrayElement<T4>(), GetArrayElement<T5>(), GetArrayElement<T6>(), GetArrayElement<T7>(), GetArrayElement<T8>(), GetArrayElement<T9>());
        }

        public readonly BitMask GetArrayElements<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged
        {
            return new(GetArrayElement<T1>(), GetArrayElement<T2>(), GetArrayElement<T3>(), GetArrayElement<T4>(), GetArrayElement<T5>(), GetArrayElement<T6>(), GetArrayElement<T7>(), GetArrayElement<T8>(), GetArrayElement<T9>(), GetArrayElement<T10>());
        }

        public readonly BitMask GetArrayElements<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged
        {
            return new(GetArrayElement<T1>(), GetArrayElement<T2>(), GetArrayElement<T3>(), GetArrayElement<T4>(), GetArrayElement<T5>(), GetArrayElement<T6>(), GetArrayElement<T7>(), GetArrayElement<T8>(), GetArrayElement<T9>(), GetArrayElement<T10>(), GetArrayElement<T11>());
        }

        public readonly BitMask GetArrayElements<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged
        {
            return new(GetArrayElement<T1>(), GetArrayElement<T2>(), GetArrayElement<T3>(), GetArrayElement<T4>(), GetArrayElement<T5>(), GetArrayElement<T6>(), GetArrayElement<T7>(), GetArrayElement<T8>(), GetArrayElement<T9>(), GetArrayElement<T10>(), GetArrayElement<T11>(), GetArrayElement<T12>());
        }

        public readonly BitMask GetTags<T1>() where T1 : unmanaged
        {
            return new(GetTag<T1>());
        }

        public readonly BitMask GetTags<T1, T2>() where T1 : unmanaged where T2 : unmanaged
        {
            return new(GetTag<T1>(), GetTag<T2>());
        }

        public readonly BitMask GetTags<T1, T2, T3>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            return new(GetTag<T1>(), GetTag<T2>(), GetTag<T3>());
        }

        public readonly BitMask GetTags<T1, T2, T3, T4>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            return new(GetTag<T1>(), GetTag<T2>(), GetTag<T3>(), GetTag<T4>());
        }

        public readonly BitMask GetTags<T1, T2, T3, T4, T5>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
        {
            return new(GetTag<T1>(), GetTag<T2>(), GetTag<T3>(), GetTag<T4>(), GetTag<T5>());
        }

        public readonly BitMask GetTags<T1, T2, T3, T4, T5, T6>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
        {
            return new(GetTag<T1>(), GetTag<T2>(), GetTag<T3>(), GetTag<T4>(), GetTag<T5>(), GetTag<T6>());
        }

        public readonly BitMask GetTags<T1, T2, T3, T4, T5, T6, T7>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged
        {
            return new(GetTag<T1>(), GetTag<T2>(), GetTag<T3>(), GetTag<T4>(), GetTag<T5>(), GetTag<T6>(), GetTag<T7>());
        }

        public readonly BitMask GetTags<T1, T2, T3, T4, T5, T6, T7, T8>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged
        {
            return new(GetTag<T1>(), GetTag<T2>(), GetTag<T3>(), GetTag<T4>(), GetTag<T5>(), GetTag<T6>(), GetTag<T7>(), GetTag<T8>());
        }

        public readonly BitMask GetTags<T1, T2, T3, T4, T5, T6, T7, T8, T9>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged
        {
            return new(GetTag<T1>(), GetTag<T2>(), GetTag<T3>(), GetTag<T4>(), GetTag<T5>(), GetTag<T6>(), GetTag<T7>(), GetTag<T8>(), GetTag<T9>());
        }

        public readonly BitMask GetTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged
        {
            return new(GetTag<T1>(), GetTag<T2>(), GetTag<T3>(), GetTag<T4>(), GetTag<T5>(), GetTag<T6>(), GetTag<T7>(), GetTag<T8>(), GetTag<T9>(), GetTag<T10>());
        }

        public readonly BitMask GetTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged
        {
            return new(GetTag<T1>(), GetTag<T2>(), GetTag<T3>(), GetTag<T4>(), GetTag<T5>(), GetTag<T6>(), GetTag<T7>(), GetTag<T8>(), GetTag<T9>(), GetTag<T10>(), GetTag<T11>());
        }

        public readonly BitMask GetTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged
        {
            return new(GetTag<T1>(), GetTag<T2>(), GetTag<T3>(), GetTag<T4>(), GetTag<T5>(), GetTag<T6>(), GetTag<T7>(), GetTag<T8>(), GetTag<T9>(), GetTag<T10>(), GetTag<T11>(), GetTag<T12>());
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
            ushort componentSize = schema->sizes.Read<ushort>(componentType.index * 2u);
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
            ushort arrayElementSize = schema->sizes.Read<ushort>(BitMask.Capacity * 2 + arrayElementTypes.index * 2u);
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

        readonly void ISerializable.Write(BinaryWriter writer)
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

        void ISerializable.Read(BinaryReader reader)
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
            public readonly Allocation sizes;
            public readonly Allocation typeHashes;

            private Implementation(Allocation sizes, Allocation typeHashes)
            {
                this.componentCount = 0;
                this.arraysCount = 0;
                this.tagsCount = 0;
                this.sizes = sizes;
                this.typeHashes = typeHashes;
            }

            public static Implementation* Allocate()
            {
                Allocation sizes = new(SizesLength, true);
                Allocation typeHashes = new(TypeHashesLength, true);
                ref Implementation schema = ref Allocations.Allocate<Implementation>();
                schema = new(sizes, typeHashes);
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
                return schema->sizes.AsSpan<ushort>(0, BitMask.Capacity);
            }

            public static USpan<ushort> GetArrayElementSizes(Implementation* schema)
            {
                return schema->sizes.AsSpan<ushort>(BitMask.Capacity, BitMask.Capacity);
            }

            public static USpan<long> GetComponentTypeHashes(Implementation* schema)
            {
                return schema->typeHashes.AsSpan<long>(0, BitMask.Capacity);
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
    }
}
