using Collections;
using System;
using Unmanaged;

namespace Worlds
{
    /// <summary>
    /// Component query for the component types specified.
    /// </summary>
    public struct ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8> where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged
    {
        private Definition required;
        private Definition exclude;
        private readonly World world;

#if NET
        /// <inheritdoc/>
        [Obsolete("Default constructor not supported", true)]
        public ComponentQuery()
        {
            throw new NotSupportedException();
        }
#endif

        /// <summary>
        /// Creates a new component query.
        /// </summary>
        public ComponentQuery(World world)
        {
            required = default;
            exclude = default;
            required.AddComponentTypes(world.Schema.GetComponentTypes<C1, C2, C3, C4, C5, C6, C7, C8>());
            this.world = world;
        }

        /// <summary>
        /// Specifies if disabled entities should be excluded or not.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8> ExcludeDisabled(bool should)
        {
            if (should)
            {
                exclude.AddTagType(Schema.DisabledTagType);
            }
            else
            {
                exclude.RemoveTagType(Schema.DisabledTagType);
            }

            return this;
        }
        
        /// <summary>
        /// Makes the given array types required.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8> RequireArray<T>() where T : unmanaged
        {
            required.AddArrayType<T>(world.Schema);
            return this;
        }

        /// <summary>
        /// Makes the given array types required.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8> RequireArrays<T1, T2>() where T1 : unmanaged where T2 : unmanaged
        {
            required.AddArrayTypes<T1, T2>(world.Schema);
            return this;
        }

        /// <summary>
        /// Makes the given array types required.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8> RequireArrays<T1, T2, T3>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            required.AddArrayTypes<T1, T2, T3>(world.Schema);
            return this;
        }

        /// <summary>
        /// Makes the given array types required.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8> RequireArrays<T1, T2, T3, T4>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            required.AddArrayTypes<T1, T2, T3, T4>(world.Schema);
            return this;
        }

        /// <summary>
        /// Makes the given array types required.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8> RequireArrays<T1, T2, T3, T4, T5>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
        {
            required.AddArrayTypes<T1, T2, T3, T4, T5>(world.Schema);
            return this;
        }

        /// <summary>
        /// Makes the given array types required.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8> RequireArrays<T1, T2, T3, T4, T5, T6>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
        {
            required.AddArrayTypes<T1, T2, T3, T4, T5, T6>(world.Schema);
            return this;
        }

        /// <summary>
        /// Makes the given array types required.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8> RequireArrays<T1, T2, T3, T4, T5, T6, T7>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged
        {
            required.AddArrayTypes<T1, T2, T3, T4, T5, T6, T7>(world.Schema);
            return this;
        }

        /// <summary>
        /// Makes the given array types required.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8> RequireArrays<T1, T2, T3, T4, T5, T6, T7, T8>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged
        {
            required.AddArrayTypes<T1, T2, T3, T4, T5, T6, T7, T8>(world.Schema);
            return this;
        }

        /// <summary>
        /// Makes the given array types required.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8> RequireArrays<T1, T2, T3, T4, T5, T6, T7, T8, T9>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged
        {
            required.AddArrayTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9>(world.Schema);
            return this;
        }

        /// <summary>
        /// Makes the given array types required.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8> RequireArrays<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged
        {
            required.AddArrayTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(world.Schema);
            return this;
        }

        /// <summary>
        /// Makes the given array types required.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8> RequireArrays<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged
        {
            required.AddArrayTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(world.Schema);
            return this;
        }

        /// <summary>
        /// Makes the given array types required.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8> RequireArrays<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged
        {
            required.AddArrayTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(world.Schema);
            return this;
        }

        /// <summary>
        /// Makes the given tag types required.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8> RequireTag<T>() where T : unmanaged
        {
            required.AddTagType<T>(world.Schema);
            return this;
        }

        /// <summary>
        /// Makes the given tag types required.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8> RequireTags<T1, T2>() where T1 : unmanaged where T2 : unmanaged
        {
            required.AddTagTypes<T1, T2>(world.Schema);
            return this;
        }

        /// <summary>
        /// Makes the given tag types required.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8> RequireTags<T1, T2, T3>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            required.AddTagTypes<T1, T2, T3>(world.Schema);
            return this;
        }

        /// <summary>
        /// Makes the given tag types required.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8> RequireTags<T1, T2, T3, T4>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            required.AddTagTypes<T1, T2, T3, T4>(world.Schema);
            return this;
        }

        /// <summary>
        /// Makes the given tag types required.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8> RequireTags<T1, T2, T3, T4, T5>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
        {
            required.AddTagTypes<T1, T2, T3, T4, T5>(world.Schema);
            return this;
        }

        /// <summary>
        /// Makes the given tag types required.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8> RequireTags<T1, T2, T3, T4, T5, T6>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
        {
            required.AddTagTypes<T1, T2, T3, T4, T5, T6>(world.Schema);
            return this;
        }

        /// <summary>
        /// Makes the given tag types required.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8> RequireTags<T1, T2, T3, T4, T5, T6, T7>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged
        {
            required.AddTagTypes<T1, T2, T3, T4, T5, T6, T7>(world.Schema);
            return this;
        }

        /// <summary>
        /// Makes the given tag types required.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8> RequireTags<T1, T2, T3, T4, T5, T6, T7, T8>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged
        {
            required.AddTagTypes<T1, T2, T3, T4, T5, T6, T7, T8>(world.Schema);
            return this;
        }

        /// <summary>
        /// Makes the given tag types required.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8> RequireTags<T1, T2, T3, T4, T5, T6, T7, T8, T9>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged
        {
            required.AddTagTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9>(world.Schema);
            return this;
        }

        /// <summary>
        /// Makes the given tag types required.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8> RequireTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged
        {
            required.AddTagTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(world.Schema);
            return this;
        }

        /// <summary>
        /// Makes the given tag types required.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8> RequireTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged
        {
            required.AddTagTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(world.Schema);
            return this;
        }

        /// <summary>
        /// Makes the given tag types required.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8> RequireTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged
        {
            required.AddTagTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(world.Schema);
            return this;
        }

        /// <summary>
        /// Makes the given component types required.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8> RequireComponent<T1>() where T1 : unmanaged
        {
            required.AddComponentType<T1>(world.Schema);
            return this;
        }

        /// <summary>
        /// Makes the given component types required.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8> RequireComponents<T1, T2>() where T1 : unmanaged where T2 : unmanaged
        {
            required.AddComponentTypes<T1, T2>(world.Schema);
            return this;
        }

        /// <summary>
        /// Makes the given component types required.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8> RequireComponents<T1, T2, T3>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            required.AddComponentTypes<T1, T2, T3>(world.Schema);
            return this;
        }

        /// <summary>
        /// Makes the given component types required.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8> RequireComponents<T1, T2, T3, T4>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            required.AddComponentTypes<T1, T2, T3, T4>(world.Schema);
            return this;
        }

        /// <summary>
        /// Makes the given component types required.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8> RequireComponents<T1, T2, T3, T4, T5>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
        {
            required.AddComponentTypes<T1, T2, T3, T4, T5>(world.Schema);
            return this;
        }

        /// <summary>
        /// Makes the given component types required.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8> RequireComponents<T1, T2, T3, T4, T5, T6>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
        {
            required.AddComponentTypes<T1, T2, T3, T4, T5, T6>(world.Schema);
            return this;
        }

        /// <summary>
        /// Makes the given component types required.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8> RequireComponents<T1, T2, T3, T4, T5, T6, T7>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged
        {
            required.AddComponentTypes<T1, T2, T3, T4, T5, T6, T7>(world.Schema);
            return this;
        }

        /// <summary>
        /// Makes the given component types required.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8> RequireComponents<T1, T2, T3, T4, T5, T6, T7, T8>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged
        {
            required.AddComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8>(world.Schema);
            return this;
        }

        /// <summary>
        /// Makes the given component types required.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8> RequireComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged
        {
            required.AddComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9>(world.Schema);
            return this;
        }

        /// <summary>
        /// Makes the given component types required.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8> RequireComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged
        {
            required.AddComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(world.Schema);
            return this;
        }

        /// <summary>
        /// Makes the given component types required.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8> RequireComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged
        {
            required.AddComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(world.Schema);
            return this;
        }

        /// <summary>
        /// Makes the given component types required.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8> RequireComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged
        {
            required.AddComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(world.Schema);
            return this;
        }

        /// <summary>
        /// Makes the given component types excluded.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8> ExcludeComponent<T1>() where T1 : unmanaged
        {
            exclude.AddComponentType<T1>(world.Schema);
            return this;
        }

        /// <summary>
        /// Makes the given component types excluded.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8> ExcludeComponents<T1, T2>() where T1 : unmanaged where T2 : unmanaged
        {
            exclude.AddComponentTypes<T1, T2>(world.Schema);
            return this;
        }

        /// <summary>
        /// Makes the given component types excluded.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8> ExcludeComponents<T1, T2, T3>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            exclude.AddComponentTypes<T1, T2, T3>(world.Schema);
            return this;
        }

        /// <summary>
        /// Makes the given component types excluded.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8> ExcludeComponents<T1, T2, T3, T4>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            exclude.AddComponentTypes<T1, T2, T3, T4>(world.Schema);
            return this;
        }

        /// <summary>
        /// Makes the given component types excluded.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8> ExcludeComponents<T1, T2, T3, T4, T5>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
        {
            exclude.AddComponentTypes<T1, T2, T3, T4, T5>(world.Schema);
            return this;
        }

        /// <summary>
        /// Makes the given component types excluded.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8> ExcludeComponents<T1, T2, T3, T4, T5, T6>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
        {
            exclude.AddComponentTypes<T1, T2, T3, T4, T5, T6>(world.Schema);
            return this;
        }

        /// <summary>
        /// Makes the given component types excluded.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8> ExcludeComponents<T1, T2, T3, T4, T5, T6, T7>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged
        {
            exclude.AddComponentTypes<T1, T2, T3, T4, T5, T6, T7>(world.Schema);
            return this;
        }

        /// <summary>
        /// Makes the given component types excluded.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8> ExcludeComponents<T1, T2, T3, T4, T5, T6, T7, T8>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged
        {
            exclude.AddComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8>(world.Schema);
            return this;
        }

        /// <summary>
        /// Makes the given component types excluded.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8> ExcludeComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged
        {
            exclude.AddComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9>(world.Schema);
            return this;
        }

        /// <summary>
        /// Makes the given component types excluded.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8> ExcludeComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged
        {
            exclude.AddComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(world.Schema);
            return this;
        }

        /// <summary>
        /// Makes the given component types excluded.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8> ExcludeComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged
        {
            exclude.AddComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(world.Schema);
            return this;
        }

        /// <summary>
        /// Makes the given component types excluded.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8> ExcludeComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged
        {
            exclude.AddComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(world.Schema);
            return this;
        }

        /// <summary>
        /// Makes the given tag types excluded.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8> ExcludeTag<T1>() where T1 : unmanaged
        {
            exclude.AddTagType<T1>(world.Schema);
            return this;
        }

        /// <summary>
        /// Makes the given tag types excluded.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8> ExcludeTags<T1, T2>() where T1 : unmanaged where T2 : unmanaged
        {
            exclude.AddTagTypes<T1, T2>(world.Schema);
            return this;
        }

        /// <summary>
        /// Makes the given tag types excluded.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8> ExcludeTags<T1, T2, T3>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            exclude.AddTagTypes<T1, T2, T3>(world.Schema);
            return this;
        }

        /// <summary>
        /// Makes the given tag types excluded.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8> ExcludeTags<T1, T2, T3, T4>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            exclude.AddTagTypes<T1, T2, T3, T4>(world.Schema);
            return this;
        }

        /// <summary>
        /// Makes the given tag types excluded.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8> ExcludeTags<T1, T2, T3, T4, T5>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
        {
            exclude.AddTagTypes<T1, T2, T3, T4, T5>(world.Schema);
            return this;
        }

        /// <summary>
        /// Makes the given tag types excluded.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8> ExcludeTags<T1, T2, T3, T4, T5, T6>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
        {
            exclude.AddTagTypes<T1, T2, T3, T4, T5, T6>(world.Schema);
            return this;
        }

        /// <summary>
        /// Makes the given tag types excluded.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8> ExcludeTags<T1, T2, T3, T4, T5, T6, T7>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged
        {
            exclude.AddTagTypes<T1, T2, T3, T4, T5, T6, T7>(world.Schema);
            return this;
        }

        /// <summary>
        /// Makes the given tag types excluded.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8> ExcludeTags<T1, T2, T3, T4, T5, T6, T7, T8>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged
        {
            exclude.AddTagTypes<T1, T2, T3, T4, T5, T6, T7, T8>(world.Schema);
            return this;
        }

        /// <summary>
        /// Makes the given tag types excluded.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8> ExcludeTags<T1, T2, T3, T4, T5, T6, T7, T8, T9>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged
        {
            exclude.AddTagTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9>(world.Schema);
            return this;
        }

        /// <summary>
        /// Makes the given tag types excluded.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8> ExcludeTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged
        {
            exclude.AddTagTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(world.Schema);
            return this;
        }

        /// <summary>
        /// Makes the given tag types excluded.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8> ExcludeTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged
        {
            exclude.AddTagTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(world.Schema);
            return this;
        }

        /// <summary>
        /// Makes the given tag types excluded.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8> ExcludeTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged
        {
            exclude.AddTagTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(world.Schema);
            return this;
        }

        /// <summary>
        /// Makes the given array types excluded.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8> ExcludeArray<T1>() where T1 : unmanaged
        {
            exclude.AddArrayType<T1>(world.Schema);
            return this;
        }

        /// <summary>
        /// Makes the given array types excluded.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8> ExcludeArrays<T1, T2>() where T1 : unmanaged where T2 : unmanaged
        {
            exclude.AddArrayTypes<T1, T2>(world.Schema);
            return this;
        }

        /// <summary>
        /// Makes the given array types excluded.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8> ExcludeArrays<T1, T2, T3>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            exclude.AddArrayTypes<T1, T2, T3>(world.Schema);
            return this;
        }

        /// <summary>
        /// Makes the given array types excluded.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8> ExcludeArrays<T1, T2, T3, T4>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            exclude.AddArrayTypes<T1, T2, T3, T4>(world.Schema);
            return this;
        }

        /// <summary>
        /// Makes the given array types excluded.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8> ExcludeArrays<T1, T2, T3, T4, T5>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
        {
            exclude.AddArrayTypes<T1, T2, T3, T4, T5>(world.Schema);
            return this;
        }

        /// <summary>
        /// Makes the given array types excluded.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8> ExcludeArrays<T1, T2, T3, T4, T5, T6>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
        {
            exclude.AddArrayTypes<T1, T2, T3, T4, T5, T6>(world.Schema);
            return this;
        }

        /// <summary>
        /// Makes the given array types excluded.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8> ExcludeArrays<T1, T2, T3, T4, T5, T6, T7>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged
        {
            exclude.AddArrayTypes<T1, T2, T3, T4, T5, T6, T7>(world.Schema);
            return this;
        }

        /// <summary>
        /// Makes the given array types excluded.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8> ExcludeArrays<T1, T2, T3, T4, T5, T6, T7, T8>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged
        {
            exclude.AddArrayTypes<T1, T2, T3, T4, T5, T6, T7, T8>(world.Schema);
            return this;
        }

        /// <summary>
        /// Makes the given array types excluded.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8> ExcludeArrays<T1, T2, T3, T4, T5, T6, T7, T8, T9>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged
        {
            exclude.AddArrayTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9>(world.Schema);
            return this;
        }

        /// <summary>
        /// Makes the given array types excluded.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8> ExcludeArrays<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged
        {
            exclude.AddArrayTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(world.Schema);
            return this;
        }

        /// <summary>
        /// Makes the given array types excluded.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8> ExcludeArrays<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged
        {
            exclude.AddArrayTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(world.Schema);
            return this;
        }

        /// <summary>
        /// Makes the given array types excluded.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8> ExcludeArrays<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged
        {
            exclude.AddArrayTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(world.Schema);
            return this;
        }

        /// <summary>
        /// Enumerates the query results.
        /// </summary>
        public readonly Enumerator GetEnumerator()
        {
            return new(required, exclude, world.Chunks, world.Schema);
        }

        /// <inheritdoc/>
        public ref struct Enumerator
        {
            private readonly MemoryAddress chunks;
            private readonly int chunkCount;
            private readonly int componentType1;
            private int componentOffset1;
            private readonly int componentType2;
            private int componentOffset2;
            private readonly int componentType3;
            private int componentOffset3;
            private readonly int componentType4;
            private int componentOffset4;
            private readonly int componentType5;
            private int componentOffset5;
            private readonly int componentType6;
            private int componentOffset6;
            private readonly int componentType7;
            private int componentOffset7;
            private readonly int componentType8;
            private int componentOffset8;
            private int entityIndex;
            private int entityCount;
            private int chunkIndex;
            private ReadOnlySpan<uint> entities;
            private List components;

            /// <summary>
            /// Current result.
            /// </summary>
            public readonly Chunk.Entity<C1, C2, C3, C4, C5, C6, C7, C8> Current
            {
                get
                {
                    uint entity = entities[entityIndex];
                    MemoryAddress componentRow = components[entityIndex];
                    ref C1 component1 = ref componentRow.Read<C1>(componentOffset1);
                    ref C2 component2 = ref componentRow.Read<C2>(componentOffset2);
                    ref C3 component3 = ref componentRow.Read<C3>(componentOffset3);
                    ref C4 component4 = ref componentRow.Read<C4>(componentOffset4);
                    ref C5 component5 = ref componentRow.Read<C5>(componentOffset5);
                    ref C6 component6 = ref componentRow.Read<C6>(componentOffset6);
                    ref C7 component7 = ref componentRow.Read<C7>(componentOffset7);
                    ref C8 component8 = ref componentRow.Read<C8>(componentOffset8);
                    return new(entity, ref component1, ref component2, ref component3, ref component4, ref component5, ref component6, ref component7, ref component8);
                }
            }

            internal Enumerator(Definition required, Definition exclude, ReadOnlySpan<Chunk> allChunks, Schema schema)
            {
                chunkCount = 0;
                Span<Chunk> chunksBuffer = stackalloc Chunk[allChunks.Length];
                foreach (Chunk chunk in allChunks)
                {
                    if (chunk.Count > 0)
                    {
                        Definition key = chunk.Definition;

                        //check if chunk contains inclusion
                        if ((key.componentTypes & required.componentTypes) != required.componentTypes)
                        {
                            continue;
                        }

                        if ((key.arrayTypes & required.arrayTypes) != required.arrayTypes)
                        {
                            continue;
                        }

                        if ((key.tagTypes & required.tagTypes) != required.tagTypes)
                        {
                            continue;
                        }

                        //check if chunk doesnt contain exclusion
                        if (key.componentTypes.ContainsAny(exclude.componentTypes))
                        {
                            continue;
                        }

                        if (key.arrayTypes.ContainsAny(exclude.arrayTypes))
                        {
                            continue;
                        }

                        if (key.tagTypes.ContainsAny(exclude.tagTypes))
                        {
                            continue;
                        }

                        chunksBuffer[chunkCount++] = chunk;
                    }
                }

                entityIndex = 0;
                chunkIndex = 0;
                componentType1 = schema.GetComponentType<C1>();
                componentType2 = schema.GetComponentType<C2>();
                componentType3 = schema.GetComponentType<C3>();
                componentType4 = schema.GetComponentType<C4>();
                componentType5 = schema.GetComponentType<C5>();
                componentType6 = schema.GetComponentType<C6>();
                componentType7 = schema.GetComponentType<C7>();
                componentType8 = schema.GetComponentType<C8>();
                if (chunkCount > 0)
                {
                    chunks = MemoryAddress.Allocate(chunksBuffer.Slice(0, chunkCount));
                    UpdateChunkFields(ref chunksBuffer[0]);
                }
            }

            /// <summary>
            /// Moves to the next result.
            /// </summary>
            public bool MoveNext()
            {
                if (entityIndex < entityCount)
                {
                    entityIndex++;
                    return true;
                }
                else
                {
                    chunkIndex++;
                    if (chunkIndex < chunkCount)
                    {
                        UpdateChunkFields(ref chunks.ReadElement<Chunk>(chunkIndex));
                        entityIndex = 1;
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            private void UpdateChunkFields(ref Chunk chunk)
            {
                entities = chunk.EntitiesList;
                entityCount = chunk.Count;
                components = chunk.Components;
                componentOffset1 = chunk.GetComponentOffset(componentType1);
                componentOffset2 = chunk.GetComponentOffset(componentType2);
                componentOffset3 = chunk.GetComponentOffset(componentType3);
                componentOffset4 = chunk.GetComponentOffset(componentType4);
                componentOffset5 = chunk.GetComponentOffset(componentType5);
                componentOffset6 = chunk.GetComponentOffset(componentType6);
                componentOffset7 = chunk.GetComponentOffset(componentType7);
                componentOffset8 = chunk.GetComponentOffset(componentType8);
            }

            /// <inheritdoc/>
            public readonly void Dispose()
            {
                if (chunkCount > 0)
                {
                    chunks.Dispose();
                }
            }
        }
    }
}