using Collections;
using System;
using System.Diagnostics;
using Unmanaged;

namespace Worlds
{
    /// <summary>
    /// Component query for the component types specified.
    /// </summary>
    public unsafe struct ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged where C13 : unmanaged
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
            required.AddComponentTypes(world.world->schema.GetComponentTypes<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13>());
            this.world = world;
        }

        /// <summary>
        /// Specifies if disabled entities should be excluded or not.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> ExcludeDisabled(bool should)
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
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> RequireArray<T>() where T : unmanaged
        {
            required.AddArrayType<T>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given array types required.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> RequireArrays<T1, T2>() where T1 : unmanaged where T2 : unmanaged
        {
            required.AddArrayTypes<T1, T2>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given array types required.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> RequireArrays<T1, T2, T3>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            required.AddArrayTypes<T1, T2, T3>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given array types required.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> RequireArrays<T1, T2, T3, T4>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            required.AddArrayTypes<T1, T2, T3, T4>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given array types required.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> RequireArrays<T1, T2, T3, T4, T5>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
        {
            required.AddArrayTypes<T1, T2, T3, T4, T5>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given array types required.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> RequireArrays<T1, T2, T3, T4, T5, T6>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
        {
            required.AddArrayTypes<T1, T2, T3, T4, T5, T6>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given array types required.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> RequireArrays<T1, T2, T3, T4, T5, T6, T7>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged
        {
            required.AddArrayTypes<T1, T2, T3, T4, T5, T6, T7>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given array types required.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> RequireArrays<T1, T2, T3, T4, T5, T6, T7, T8>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged
        {
            required.AddArrayTypes<T1, T2, T3, T4, T5, T6, T7, T8>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given array types required.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> RequireArrays<T1, T2, T3, T4, T5, T6, T7, T8, T9>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged
        {
            required.AddArrayTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given array types required.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> RequireArrays<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged
        {
            required.AddArrayTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given array types required.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> RequireArrays<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged
        {
            required.AddArrayTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given array types required.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> RequireArrays<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged
        {
            required.AddArrayTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given tag types required.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> RequireTag<T>() where T : unmanaged
        {
            required.AddTagType<T>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given tag types required.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> RequireTags<T1, T2>() where T1 : unmanaged where T2 : unmanaged
        {
            required.AddTagTypes<T1, T2>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given tag types required.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> RequireTags<T1, T2, T3>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            required.AddTagTypes<T1, T2, T3>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given tag types required.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> RequireTags<T1, T2, T3, T4>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            required.AddTagTypes<T1, T2, T3, T4>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given tag types required.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> RequireTags<T1, T2, T3, T4, T5>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
        {
            required.AddTagTypes<T1, T2, T3, T4, T5>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given tag types required.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> RequireTags<T1, T2, T3, T4, T5, T6>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
        {
            required.AddTagTypes<T1, T2, T3, T4, T5, T6>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given tag types required.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> RequireTags<T1, T2, T3, T4, T5, T6, T7>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged
        {
            required.AddTagTypes<T1, T2, T3, T4, T5, T6, T7>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given tag types required.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> RequireTags<T1, T2, T3, T4, T5, T6, T7, T8>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged
        {
            required.AddTagTypes<T1, T2, T3, T4, T5, T6, T7, T8>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given tag types required.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> RequireTags<T1, T2, T3, T4, T5, T6, T7, T8, T9>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged
        {
            required.AddTagTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given tag types required.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> RequireTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged
        {
            required.AddTagTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given tag types required.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> RequireTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged
        {
            required.AddTagTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given tag types required.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> RequireTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged
        {
            required.AddTagTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given component types required.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> RequireComponent<T1>() where T1 : unmanaged
        {
            required.AddComponentType<T1>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given component types required.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> RequireComponents<T1, T2>() where T1 : unmanaged where T2 : unmanaged
        {
            required.AddComponentTypes<T1, T2>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given component types required.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> RequireComponents<T1, T2, T3>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            required.AddComponentTypes<T1, T2, T3>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given component types required.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> RequireComponents<T1, T2, T3, T4>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            required.AddComponentTypes<T1, T2, T3, T4>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given component types required.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> RequireComponents<T1, T2, T3, T4, T5>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
        {
            required.AddComponentTypes<T1, T2, T3, T4, T5>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given component types required.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> RequireComponents<T1, T2, T3, T4, T5, T6>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
        {
            required.AddComponentTypes<T1, T2, T3, T4, T5, T6>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given component types required.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> RequireComponents<T1, T2, T3, T4, T5, T6, T7>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged
        {
            required.AddComponentTypes<T1, T2, T3, T4, T5, T6, T7>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given component types required.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> RequireComponents<T1, T2, T3, T4, T5, T6, T7, T8>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged
        {
            required.AddComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given component types required.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> RequireComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged
        {
            required.AddComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given component types required.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> RequireComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged
        {
            required.AddComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given component types required.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> RequireComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged
        {
            required.AddComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given component types required.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> RequireComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged
        {
            required.AddComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given component types excluded.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> ExcludeComponent<T1>() where T1 : unmanaged
        {
            exclude.AddComponentType<T1>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given component types excluded.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> ExcludeComponents<T1, T2>() where T1 : unmanaged where T2 : unmanaged
        {
            exclude.AddComponentTypes<T1, T2>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given component types excluded.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> ExcludeComponents<T1, T2, T3>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            exclude.AddComponentTypes<T1, T2, T3>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given component types excluded.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> ExcludeComponents<T1, T2, T3, T4>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            exclude.AddComponentTypes<T1, T2, T3, T4>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given component types excluded.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> ExcludeComponents<T1, T2, T3, T4, T5>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
        {
            exclude.AddComponentTypes<T1, T2, T3, T4, T5>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given component types excluded.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> ExcludeComponents<T1, T2, T3, T4, T5, T6>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
        {
            exclude.AddComponentTypes<T1, T2, T3, T4, T5, T6>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given component types excluded.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> ExcludeComponents<T1, T2, T3, T4, T5, T6, T7>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged
        {
            exclude.AddComponentTypes<T1, T2, T3, T4, T5, T6, T7>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given component types excluded.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> ExcludeComponents<T1, T2, T3, T4, T5, T6, T7, T8>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged
        {
            exclude.AddComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given component types excluded.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> ExcludeComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged
        {
            exclude.AddComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given component types excluded.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> ExcludeComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged
        {
            exclude.AddComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given component types excluded.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> ExcludeComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged
        {
            exclude.AddComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given component types excluded.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> ExcludeComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged
        {
            exclude.AddComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given tag types excluded.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> ExcludeTag<T1>() where T1 : unmanaged
        {
            exclude.AddTagType<T1>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given tag types excluded.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> ExcludeTags<T1, T2>() where T1 : unmanaged where T2 : unmanaged
        {
            exclude.AddTagTypes<T1, T2>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given tag types excluded.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> ExcludeTags<T1, T2, T3>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            exclude.AddTagTypes<T1, T2, T3>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given tag types excluded.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> ExcludeTags<T1, T2, T3, T4>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            exclude.AddTagTypes<T1, T2, T3, T4>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given tag types excluded.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> ExcludeTags<T1, T2, T3, T4, T5>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
        {
            exclude.AddTagTypes<T1, T2, T3, T4, T5>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given tag types excluded.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> ExcludeTags<T1, T2, T3, T4, T5, T6>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
        {
            exclude.AddTagTypes<T1, T2, T3, T4, T5, T6>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given tag types excluded.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> ExcludeTags<T1, T2, T3, T4, T5, T6, T7>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged
        {
            exclude.AddTagTypes<T1, T2, T3, T4, T5, T6, T7>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given tag types excluded.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> ExcludeTags<T1, T2, T3, T4, T5, T6, T7, T8>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged
        {
            exclude.AddTagTypes<T1, T2, T3, T4, T5, T6, T7, T8>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given tag types excluded.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> ExcludeTags<T1, T2, T3, T4, T5, T6, T7, T8, T9>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged
        {
            exclude.AddTagTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given tag types excluded.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> ExcludeTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged
        {
            exclude.AddTagTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given tag types excluded.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> ExcludeTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged
        {
            exclude.AddTagTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given tag types excluded.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> ExcludeTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged
        {
            exclude.AddTagTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given array types excluded.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> ExcludeArray<T1>() where T1 : unmanaged
        {
            exclude.AddArrayType<T1>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given array types excluded.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> ExcludeArrays<T1, T2>() where T1 : unmanaged where T2 : unmanaged
        {
            exclude.AddArrayTypes<T1, T2>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given array types excluded.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> ExcludeArrays<T1, T2, T3>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            exclude.AddArrayTypes<T1, T2, T3>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given array types excluded.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> ExcludeArrays<T1, T2, T3, T4>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            exclude.AddArrayTypes<T1, T2, T3, T4>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given array types excluded.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> ExcludeArrays<T1, T2, T3, T4, T5>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
        {
            exclude.AddArrayTypes<T1, T2, T3, T4, T5>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given array types excluded.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> ExcludeArrays<T1, T2, T3, T4, T5, T6>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
        {
            exclude.AddArrayTypes<T1, T2, T3, T4, T5, T6>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given array types excluded.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> ExcludeArrays<T1, T2, T3, T4, T5, T6, T7>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged
        {
            exclude.AddArrayTypes<T1, T2, T3, T4, T5, T6, T7>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given array types excluded.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> ExcludeArrays<T1, T2, T3, T4, T5, T6, T7, T8>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged
        {
            exclude.AddArrayTypes<T1, T2, T3, T4, T5, T6, T7, T8>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given array types excluded.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> ExcludeArrays<T1, T2, T3, T4, T5, T6, T7, T8, T9>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged
        {
            exclude.AddArrayTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given array types excluded.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> ExcludeArrays<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged
        {
            exclude.AddArrayTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given array types excluded.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> ExcludeArrays<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged
        {
            exclude.AddArrayTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given array types excluded.
        /// </summary>
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> ExcludeArrays<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged
        {
            exclude.AddArrayTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Enumerates the query results.
        /// </summary>
        public readonly Enumerator GetEnumerator()
        {
            return new(this);
        }

        /// <inheritdoc/>
        public ref struct Enumerator
        {
            private readonly MemoryAddress chunks;
            private readonly int chunkCount;
            private readonly int version;
            private readonly ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> query;
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
            private readonly int componentType9;
            private int componentOffset9;
            private readonly int componentType10;
            private int componentOffset10;
            private readonly int componentType11;
            private int componentOffset11;
            private readonly int componentType12;
            private int componentOffset12;
            private readonly int componentType13;
            private int componentOffset13;
            private int entityIndex;
            private int entityCount;
            private int chunkIndex;
            private ReadOnlySpan<uint> entities;
            private List components;

            /// <summary>
            /// Current result.
            /// </summary>
            public readonly Chunk.Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> Current
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
                    ref C9 component9 = ref componentRow.Read<C9>(componentOffset9);
                    ref C10 component10 = ref componentRow.Read<C10>(componentOffset10);
                    ref C11 component11 = ref componentRow.Read<C11>(componentOffset11);
                    ref C12 component12 = ref componentRow.Read<C12>(componentOffset12);
                    ref C13 component13 = ref componentRow.Read<C13>(componentOffset13);
                    return new(entity, ref component1, ref component2, ref component3, ref component4, ref component5, ref component6, ref component7, ref component8, ref component9, ref component10, ref component11, ref component12, ref component13);
                }
            }

            internal unsafe Enumerator(ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13> query)
            {
                this.query = query;
                this.version = query.world.Version;
                chunkCount = 0;
                ReadOnlySpan<Chunk> allChunks = query.world.Chunks;
                Span<Chunk> chunksBuffer = stackalloc Chunk[allChunks.Length];
                foreach (Chunk chunk in allChunks)
                {
                    if (chunk.chunk->count > 0)
                    {
                        Definition key = chunk.chunk->definition;

                        //check if chunk contains inclusion
                        if (!key.componentTypes.ContainsAll(query.required.componentTypes))
                        {
                            continue;
                        }

                        if (!key.arrayTypes.ContainsAll(query.required.arrayTypes))
                        {
                            continue;
                        }

                        if (!key.tagTypes.ContainsAll(query.required.tagTypes))
                        {
                            continue;
                        }

                        //check if chunk doesnt contain exclusion
                        if (key.componentTypes.ContainsAny(query.exclude.componentTypes))
                        {
                            continue;
                        }

                        if (key.arrayTypes.ContainsAny(query.exclude.arrayTypes))
                        {
                            continue;
                        }

                        if (key.tagTypes.ContainsAny(query.exclude.tagTypes))
                        {
                            continue;
                        }

                        chunksBuffer[chunkCount++] = chunk;
                    }
                }

                entityIndex = 0;
                chunkIndex = 0;
                Schema schema = query.world.world->schema;
                componentType1 = schema.GetComponentType<C1>();
                componentType2 = schema.GetComponentType<C2>();
                componentType3 = schema.GetComponentType<C3>();
                componentType4 = schema.GetComponentType<C4>();
                componentType5 = schema.GetComponentType<C5>();
                componentType6 = schema.GetComponentType<C6>();
                componentType7 = schema.GetComponentType<C7>();
                componentType8 = schema.GetComponentType<C8>();
                componentType9 = schema.GetComponentType<C9>();
                componentType10 = schema.GetComponentType<C10>();
                componentType11 = schema.GetComponentType<C11>();
                componentType12 = schema.GetComponentType<C12>();
                componentType13 = schema.GetComponentType<C13>();
                if (chunkCount > 0)
                {
                    chunks = MemoryAddress.Allocate(chunksBuffer.Slice(0, chunkCount));
                    UpdateChunkFields(ref chunksBuffer[0]);
                }
            }

            [Conditional("DEBUG")]
            private readonly void ThrowIfVersionIsDifferent() 
            {
                if (version != query.world.Version)
                {
                    throw new($"Data in the world has changed while the component query has enumerated");
                }
            }

            /// <summary>
            /// Moves to the next result.
            /// </summary>
            public bool MoveNext()
            {
                ThrowIfVersionIsDifferent();

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

            private unsafe void UpdateChunkFields(ref Chunk chunk)
            {
                entities = new(chunk.chunk->entities.Items.Pointer, chunk.chunk->count + 1);
                entityCount = chunk.chunk->count;
                components = chunk.chunk->components;
                componentOffset1 = chunk.GetComponentOffset(componentType1);
                componentOffset2 = chunk.GetComponentOffset(componentType2);
                componentOffset3 = chunk.GetComponentOffset(componentType3);
                componentOffset4 = chunk.GetComponentOffset(componentType4);
                componentOffset5 = chunk.GetComponentOffset(componentType5);
                componentOffset6 = chunk.GetComponentOffset(componentType6);
                componentOffset7 = chunk.GetComponentOffset(componentType7);
                componentOffset8 = chunk.GetComponentOffset(componentType8);
                componentOffset9 = chunk.GetComponentOffset(componentType9);
                componentOffset10 = chunk.GetComponentOffset(componentType10);
                componentOffset11 = chunk.GetComponentOffset(componentType11);
                componentOffset12 = chunk.GetComponentOffset(componentType12);
                componentOffset13 = chunk.GetComponentOffset(componentType13);
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