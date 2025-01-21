using System;

namespace Worlds
{
    public struct Archetype : IEquatable<Archetype>
    {
        public Definition definition;
        public readonly Schema schema;

#if NET
        [Obsolete("Default constructor not supported", true)]
        public Archetype()
        {
            throw new NotSupportedException();
        }
#endif

        public Archetype(Definition definition, Schema schema)
        {
            this.definition = definition;
            this.schema = schema;
        }

        public Archetype(Schema schema)
        {
            this.definition = default;
            this.schema = schema;
        }

        public readonly override bool Equals(object? obj)
        {
            return obj is Archetype archetype && Equals(archetype);
        }

        public readonly bool Equals(Archetype other)
        {
            return definition.Equals(other.definition) && schema.Equals(other.schema);
        }

        public readonly override int GetHashCode()
        {
            return HashCode.Combine(definition, schema);
        }

        public void Add<T>() where T : unmanaged, IEntity
        {
            default(T).Describe(ref this);
        }

        /// <summary>
        /// Adds the component of type <typeparamref name="T"/> to the definition.
        /// </summary>
        public void AddComponentType<T>() where T : unmanaged
        {
            definition.AddComponentType<T>(schema);
        }

        /// <summary>
        /// Adds <paramref name="componentType"/> to the definition.
        /// </summary>
        public void AddComponentType(ComponentType componentType)
        {
            definition.AddComponentType(componentType);
        }

        public void AddArrayElementType<T>() where T : unmanaged
        {
            definition.AddArrayElementType<T>(schema);
        }

        public void AddArrayElementType(ArrayElementType arrayElementType)
        {
            definition.AddArrayElementType(arrayElementType);
        }

        public void AddTagType<T>() where T : unmanaged
        {
            definition.AddTagType<T>(schema);
        }

        public void AddTagType(TagType tagType)
        {
            definition.AddTagType(tagType);
        }

        /// <summary>
        /// Retrieves the definition for the specified <typeparamref name="T"/> entity type.
        /// </summary>
        public static Archetype Get<T>(Schema schema) where T : unmanaged, IEntity
        {
            Archetype archetype = new(schema);
            default(T).Describe(ref archetype);
            return archetype;
        }

        public static Archetype Get<T1, T2>(Schema schema) where T1 : unmanaged, IEntity where T2 : unmanaged, IEntity
        {
            Archetype archetype = new(schema);
            default(T1).Describe(ref archetype);
            default(T2).Describe(ref archetype);
            return archetype;
        }

        public static Archetype Get<T1, T2, T3>(Schema schema) where T1 : unmanaged, IEntity where T2 : unmanaged, IEntity where T3 : unmanaged, IEntity
        {
            Archetype archetype = new(schema);
            default(T1).Describe(ref archetype);
            default(T2).Describe(ref archetype);
            default(T3).Describe(ref archetype);
            return archetype;
        }

        public static Archetype Get<T1, T2, T3, T4>(Schema schema) where T1 : unmanaged, IEntity where T2 : unmanaged, IEntity where T3 : unmanaged, IEntity where T4 : unmanaged, IEntity
        {
            Archetype archetype = new(schema);
            default(T1).Describe(ref archetype);
            default(T2).Describe(ref archetype);
            default(T3).Describe(ref archetype);
            default(T4).Describe(ref archetype);
            return archetype;
        }

        public static Archetype Get<T1, T2, T3, T4, T5>(Schema schema) where T1 : unmanaged, IEntity where T2 : unmanaged, IEntity where T3 : unmanaged, IEntity where T4 : unmanaged, IEntity where T5 : unmanaged, IEntity
        {
            Archetype archetype = new(schema);
            default(T1).Describe(ref archetype);
            default(T2).Describe(ref archetype);
            default(T3).Describe(ref archetype);
            default(T4).Describe(ref archetype);
            default(T5).Describe(ref archetype);
            return archetype;
        }

        public static Archetype Get<T1, T2, T3, T4, T5, T6>(Schema schema) where T1 : unmanaged, IEntity where T2 : unmanaged, IEntity where T3 : unmanaged, IEntity where T4 : unmanaged, IEntity where T5 : unmanaged, IEntity where T6 : unmanaged, IEntity
        {
            Archetype archetype = new(schema);
            default(T1).Describe(ref archetype);
            default(T2).Describe(ref archetype);
            default(T3).Describe(ref archetype);
            default(T4).Describe(ref archetype);
            default(T5).Describe(ref archetype);
            default(T6).Describe(ref archetype);
            return archetype;
        }

        public static bool operator ==(Archetype left, Archetype right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Archetype left, Archetype right)
        {
            return !(left == right);
        }
    }
}