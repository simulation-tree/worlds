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
        public readonly System.Collections.Generic.IEnumerable<TypeLayout> ComponentTypes
        {
            get
            {
                for (byte c = 0; c < BitMask.Capacity; c++)
                {
                    TypeLayout typeLayout = GetLayout(new ComponentType(c));
                    if (typeLayout != default)
                    {
                        yield return typeLayout;
                    }
                }
            }
        }

        /// <summary>
        /// All array element types loaded.
        /// </summary>
        public readonly System.Collections.Generic.IEnumerable<TypeLayout> ArrayElementTypes
        {
            get
            {
                for (byte a = 0; a < BitMask.Capacity; a++)
                {
                    TypeLayout typeLayout = GetLayout(new ArrayElementType(a));
                    if (typeLayout != default)
                    {
                        yield return typeLayout;
                    }
                }
            }
        }

        /// <summary>
        /// All tag types loaded.
        /// </summary>
        public readonly System.Collections.Generic.IEnumerable<TypeLayout> TagTypes
        {
            get
            {
                for (byte t = 0; t < BitMask.Capacity; t++)
                {
                    TypeLayout typeLayout = GetLayout(new TagType(t));
                    if (typeLayout != default)
                    {
                        yield return typeLayout;
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

        public static void Register(RegisterDataType.Input input)
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
        public readonly TypeLayout GetLayout(ComponentType componentType)
        {
            ThrowIfComponentIsMissing(componentType);

            return Implementation.GetComponentLayouts(schema)[componentType];
        }

        /// <summary>
        /// Retrieves the type layout for the given <paramref name="arrayElementType"/>.
        /// </summary>
        public readonly TypeLayout GetLayout(ArrayElementType arrayElementType)
        {
            ThrowIfArrayElementIsMissing(arrayElementType);

            return Implementation.GetArrayLayouts(schema)[arrayElementType];
        }

        /// <summary>
        /// Retrieves the type layout for the given <paramref name="tagType"/>.
        /// </summary>
        public readonly TypeLayout GetLayout(TagType tagType)
        {
            ThrowIfTagIsMissing(tagType);

            return Implementation.GetTagLayouts(schema)[tagType];
        }

        public readonly TypeLayout GetComponentLayout<T>() where T : unmanaged
        {
            return GetLayout(GetComponent<T>());
        }

        public readonly bool TryGetComponentLayout(FixedString fullTypeName, out TypeLayout componentType)
        {
            USpan<TypeLayout> types = Implementation.GetComponentLayouts(schema);
            for (byte c = 0; c < BitMask.Capacity; c++)
            {
                TypeLayout layout = types[c];
                if (layout.FullName == fullTypeName)
                {
                    componentType = layout;
                    return true;
                }
            }

            componentType = default;
            return false;
        }

        public readonly bool TryGetArrayElementLayout(FixedString fullTypeName, out TypeLayout arrayElementType)
        {
            USpan<TypeLayout> types = Implementation.GetArrayLayouts(schema);
            for (byte a = 0; a < BitMask.Capacity; a++)
            {
                TypeLayout type = types[a];
                if (type.FullName == fullTypeName)
                {
                    arrayElementType = type;
                    return true;
                }
            }

            arrayElementType = default;
            return false;
        }

        public readonly void RegisterComponent<T>() where T : unmanaged
        {
            RegisterComponent(TypeRegistry.Get<T>());
        }

        public readonly void RegisterComponent(TypeLayout type)
        {
            ThrowIfTooManyComponents();
            ThrowIfComponentAlreadyRegistered(type);

            USpan<TypeLayout> typeLayouts = Implementation.GetComponentLayouts(schema);
            USpan<ushort> componentSizes = Implementation.GetComponentSizes(schema);
            typeLayouts[schema->components] = type;
            componentSizes[schema->components] = type.Size;
            int hashCode = type.GetHashCode();
            schema->componentIndices.Add(hashCode, schema->components);
            schema->components++;
        }

        public readonly void RegisterArrayElement<T>() where T : unmanaged
        {
            RegisterArrayElement(TypeRegistry.Get<T>());
        }

        public readonly void RegisterArrayElement(TypeLayout type)
        {
            ThrowIfTooManyArrays();
            ThrowIfArrayElementAlreadyRegistered(type);

            USpan<TypeLayout> typeLayouts = Implementation.GetArrayLayouts(schema);
            USpan<ushort> arrayElementSizes = Implementation.GetArrayElementSizes(schema);
            typeLayouts[schema->arrays] = type;
            arrayElementSizes[schema->arrays] = type.Size;
            int hashCode = type.GetHashCode();
            schema->arrayElementIndices.Add(hashCode, schema->arrays);
            schema->arrays++;
        }

        public readonly void RegisterTag<T>() where T : unmanaged
        {
            RegisterTag(TypeRegistry.Get<T>());
        }

        public readonly void RegisterTag(TypeLayout type)
        {
            ThrowIfTooManyTags();
            ThrowIfTagAlreadyRegistered(type);

            USpan<TypeLayout> typeLayouts = Implementation.GetTagLayouts(schema);
            typeLayouts[schema->tags] = type;
            schema->tagIndices.Add(type.GetHashCode(), schema->tags);
            schema->tagExistence.Write(schema->tags, true);
            schema->tags++;
        }

        public readonly bool Contains(ComponentType componentType)
        {
            return GetSize(componentType) != default;
        }

        public readonly bool Contains(ArrayElementType arrayElementType)
        {
            return GetSize(arrayElementType) != default;
        }

        public readonly bool Contains(TagType tagType)
        {
            return schema->tagExistence.Read<bool>(tagType.index);
        }

        public readonly bool ContainsComponent(FixedString fullTypeName)
        {
            USpan<TypeLayout> types = Implementation.GetComponentLayouts(schema);
            for (byte c = 0; c < BitMask.Capacity; c++)
            {
                TypeLayout type = types[c];
                if (type.FullName == fullTypeName)
                {
                    return true;
                }
            }

            return false;
        }

        public readonly bool ContainsComponent(TypeLayout type)
        {
            USpan<TypeLayout> types = Implementation.GetComponentLayouts(schema);
            return types.Contains(type);
        }

        public readonly bool ContainsArrayElement(FixedString fullTypeName)
        {
            USpan<TypeLayout> types = Implementation.GetArrayLayouts(schema);
            for (byte a = 0; a < BitMask.Capacity; a++)
            {
                TypeLayout type = types[a];
                if (type.FullName == fullTypeName)
                {
                    return true;
                }
            }

            return false;
        }

        public readonly bool ContainsArrayElement(TypeLayout type)
        {
            USpan<TypeLayout> types = Implementation.GetArrayLayouts(schema);
            return types.Contains(type);
        }

        public readonly bool ContainsTag(FixedString fullTypeName)
        {
            USpan<TypeLayout> types = Implementation.GetTagLayouts(schema);
            for (byte t = 0; t < BitMask.Capacity; t++)
            {
                TypeLayout type = types[t];
                if (type.FullName == fullTypeName)
                {
                    return true;
                }
            }

            return false;
        }

        public readonly bool ContainsTag(TypeLayout type)
        {
            USpan<TypeLayout> types = Implementation.GetTagLayouts(schema);
            return types.Contains(type);
        }

        public readonly bool ContainsComponent<T>() where T : unmanaged
        {
            int hashCode = TypeLayoutHashCodeCache<T>.value;
            return schema->componentIndices.ContainsKey(hashCode);
        }

        public readonly ComponentType GetComponent<T>() where T : unmanaged
        {
            ThrowIfComponentIsMissing<T>();

            return new(schema->componentIndices[TypeLayoutHashCodeCache<T>.value]);
        }

        public readonly DataType GetComponentDataType<T>() where T : unmanaged
        {
            ComponentType componentType = GetComponent<T>();
            return new(componentType, (ushort)sizeof(T));
        }

        public readonly bool ContainsArrayElement<T>() where T : unmanaged
        {
            int hashCode = TypeLayoutHashCodeCache<T>.value;
            return schema->arrayElementIndices.ContainsKey(hashCode);
        }

        public readonly ArrayElementType GetArrayElement<T>() where T : unmanaged
        {
            ThrowIfArrayElementIsMissing<T>();

            return new(schema->arrayElementIndices[TypeLayoutHashCodeCache<T>.value]);
        }

        public readonly DataType GetArrayElementDataType<T>() where T : unmanaged
        {
            ArrayElementType arrayElementType = GetArrayElement<T>();
            return new(arrayElementType, (ushort)sizeof(T));
        }

        public readonly TagType GetTag<T>() where T : unmanaged
        {
            ThrowIfTagIsMissing<T>();

            int hashCode = TypeLayoutHashCodeCache<T>.value;
            return new(schema->tagIndices[hashCode]);
        }

        public readonly DataType GetTagDataType<T>() where T : unmanaged
        {
            return GetDataType(GetTag<T>());
        }

        public readonly bool ContainsTag<T>() where T : unmanaged
        {
            int hashCode = TypeLayoutHashCodeCache<T>.value;
            return schema->tagIndices.ContainsKey(hashCode);
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

        public readonly BitMask GetComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged where T15 : unmanaged where T16 : unmanaged where T17 : unmanaged
        {
            return new(GetComponent<T1>(), GetComponent<T2>(), GetComponent<T3>(), GetComponent<T4>(), GetComponent<T5>(), GetComponent<T6>(), GetComponent<T7>(), GetComponent<T8>(), GetComponent<T9>(), GetComponent<T10>(), GetComponent<T11>(), GetComponent<T12>(), GetComponent<T13>(), GetComponent<T14>(), GetComponent<T15>(), GetComponent<T16>(), GetComponent<T17>());
        }

        public readonly BitMask GetComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged where T13 : unmanaged where T14 : unmanaged where T15 : unmanaged where T16 : unmanaged where T17 : unmanaged where T18 : unmanaged
        {
            return new(GetComponent<T1>(), GetComponent<T2>(), GetComponent<T3>(), GetComponent<T4>(), GetComponent<T5>(), GetComponent<T6>(), GetComponent<T7>(), GetComponent<T8>(), GetComponent<T9>(), GetComponent<T10>(), GetComponent<T11>(), GetComponent<T12>(), GetComponent<T13>(), GetComponent<T14>(), GetComponent<T15>(), GetComponent<T16>(), GetComponent<T17>(), GetComponent<T18>());
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
            if (schema->components >= BitMask.Capacity)
            {
                throw new Exception("Too many components types registered");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfTooManyArrays()
        {
            if (schema->arrays >= BitMask.Capacity)
            {
                throw new Exception("Too many arrays element types registered");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfTooManyTags()
        {
            if (schema->tags >= BitMask.Capacity)
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
            writer.WriteValue(schema->components);
            writer.WriteValue(schema->arrays);
            writer.WriteValue(schema->tags);
            USpan<ushort> componentSizes = Implementation.GetComponentSizes(schema);
            USpan<ushort> arrayElementSizes = Implementation.GetArrayElementSizes(schema);
            USpan<TypeLayout> componentLayouts = Implementation.GetComponentLayouts(schema);
            USpan<TypeLayout> arrayLayouts = Implementation.GetArrayLayouts(schema);
            USpan<TypeLayout> tagLayouts = Implementation.GetTagLayouts(schema);
            for (byte i = 0; i < BitMask.Capacity; i++)
            {
                ushort componentSize = componentSizes[i];
                writer.WriteValue(componentSize != default);
                if (componentSize != default)
                {
                    writer.WriteValue(componentSizes[i]);
                    writer.WriteObject(componentLayouts[i]);
                }

                ushort arrayElementSize = arrayElementSizes[i];
                writer.WriteValue(arrayElementSize != default);
                if (arrayElementSize != default)
                {
                    writer.WriteValue(arrayElementSizes[i]);
                    writer.WriteObject(arrayLayouts[i]);
                }

                bool hasTag = schema->tagExistence.Read<bool>(i);
                writer.WriteValue(hasTag);
                if (hasTag)
                {
                    writer.WriteObject(tagLayouts[i]);
                }
            }
        }

        void ISerializable.Read(BinaryReader reader)
        {
            schema = Implementation.Allocate();
            schema->components = reader.ReadValue<byte>();
            schema->arrays = reader.ReadValue<byte>();
            schema->tags = reader.ReadValue<byte>();
            USpan<ushort> componentSizes = Implementation.GetComponentSizes(schema);
            USpan<ushort> arrayElementSizes = Implementation.GetArrayElementSizes(schema);
            USpan<TypeLayout> componentLayouts = Implementation.GetComponentLayouts(schema);
            USpan<TypeLayout> arrayLayouts = Implementation.GetArrayLayouts(schema);
            USpan<TypeLayout> tagLayouts = Implementation.GetTagLayouts(schema);
            for (byte i = 0; i < BitMask.Capacity; i++)
            {
                bool hasComponent = reader.ReadValue<bool>();
                if (hasComponent)
                {
                    componentSizes[i] = reader.ReadValue<ushort>();
                    TypeLayout typeLayout = reader.ReadObject<TypeLayout>();
                    componentLayouts[i] = typeLayout;
                    schema->componentIndices.Add(typeLayout.GetHashCode(), i);
                }

                bool hasArrayElement = reader.ReadValue<bool>();
                if (hasArrayElement)
                {
                    arrayElementSizes[i] = reader.ReadValue<ushort>();
                    TypeLayout typeLayout = reader.ReadObject<TypeLayout>();
                    arrayLayouts[i] = typeLayout;
                    schema->arrayElementIndices.Add(typeLayout.GetHashCode(), i);
                }

                bool hasTag = reader.ReadValue<bool>();
                schema->tagExistence.Write(i, hasTag);
                if (hasTag)
                {
                    TypeLayout typeLayout = reader.ReadObject<TypeLayout>();
                    tagLayouts[i] = typeLayout;
                    schema->tagIndices.Add(typeLayout.GetHashCode(), i);
                }
            }
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
            public byte components;
            public byte arrays;
            public byte tags;
            public readonly Allocation sizes;
            public readonly Allocation tagExistence;
            public readonly Allocation typeLayouts;
            public readonly Dictionary<int, byte> componentIndices;
            public readonly Dictionary<int, byte> arrayElementIndices;
            public readonly Dictionary<int, byte> tagIndices;

            private Implementation(Allocation sizes, Allocation tagExistence, Allocation typeLayouts)
            {
                this.sizes = sizes;
                this.tagExistence = tagExistence;
                this.typeLayouts = typeLayouts;
                this.componentIndices = new(BitMask.Capacity);
                this.arrayElementIndices = new(BitMask.Capacity);
                this.tagIndices = new(BitMask.Capacity);
            }

            public static Implementation* Allocate()
            {
                Allocation sizes = new(BitMask.Capacity * 2 * 2, true);
                Allocation tagExistence = new(BitMask.Capacity, true);
                Allocation typeLayouts = new((uint)sizeof(TypeLayout) * BitMask.Capacity * 3, true);
                Implementation* schema = Allocations.Allocate<Implementation>();
                *schema = new(sizes, tagExistence, typeLayouts);
                schema->components = 0;
                schema->arrays = 0;
                return schema;
            }

            public static void Free(ref Implementation* schema)
            {
                Allocations.ThrowIfNull(schema);

                schema->tagIndices.Dispose();
                schema->componentIndices.Dispose();
                schema->arrayElementIndices.Dispose();
                schema->sizes.Dispose();
                schema->tagExistence.Dispose();
                schema->typeLayouts.Dispose();
                Allocations.Free(ref schema);
            }

            public static void Clear(Implementation* schema)
            {
                schema->components = 0;
                schema->arrays = 0;
                schema->tags = 0;
                schema->componentIndices.Clear();
                schema->arrayElementIndices.Clear();
                schema->tagIndices.Clear();
                schema->sizes.Clear(BitMask.Capacity * 2 * 2);
                schema->tagExistence.Clear(BitMask.Capacity);
                schema->typeLayouts.Clear((uint)sizeof(TypeLayout) * BitMask.Capacity * 3);
            }

            public static void CopyTo(Implementation* source, Implementation* destination)
            {
                destination->components = source->components;
                destination->arrays = source->arrays;
                destination->tags = source->tags;
                source->sizes.CopyTo(destination->sizes, BitMask.Capacity * 2 * 2);
                source->tagExistence.CopyTo(destination->tagExistence, BitMask.Capacity);
                source->typeLayouts.CopyTo(destination->typeLayouts, (uint)sizeof(TypeLayout) * BitMask.Capacity * 3);
                destination->componentIndices.Clear();
                foreach ((int hashCode, byte index) in source->componentIndices)
                {
                    destination->componentIndices.Add(hashCode, index);
                }

                destination->arrayElementIndices.Clear();
                foreach ((int hashCode, byte index) in source->arrayElementIndices)
                {
                    destination->arrayElementIndices.Add(hashCode, index);
                }

                destination->tagIndices.Clear();
                foreach ((int hashCode, byte index) in source->tagIndices)
                {
                    destination->tagIndices.Add(hashCode, index);
                }
            }

            public static USpan<ushort> GetComponentSizes(Implementation* schema)
            {
                return schema->sizes.AsSpan<ushort>(0, BitMask.Capacity);
            }

            public static USpan<ushort> GetArrayElementSizes(Implementation* schema)
            {
                return schema->sizes.AsSpan<ushort>(BitMask.Capacity, BitMask.Capacity);
            }

            public static USpan<TypeLayout> GetComponentLayouts(Implementation* schema)
            {
                return schema->typeLayouts.AsSpan<TypeLayout>(0, BitMask.Capacity);
            }

            public static USpan<TypeLayout> GetArrayLayouts(Implementation* schema)
            {
                return schema->typeLayouts.AsSpan<TypeLayout>(BitMask.Capacity, BitMask.Capacity);
            }

            public static USpan<TypeLayout> GetTagLayouts(Implementation* schema)
            {
                return schema->typeLayouts.AsSpan<TypeLayout>(BitMask.Capacity * 2, BitMask.Capacity);
            }
        }

        internal static class TypeLayoutHashCodeCache<T> where T : unmanaged
        {
            public static readonly int value = TypeRegistry.Get<T>().GetHashCode();
        }
    }
}
