using Collections;
using System;
using System.Diagnostics;
using Unmanaged;

namespace Worlds
{
    /// <summary>
    /// Component query for the component types specified.
    /// </summary>
    public unsafe struct ComponentQuery<C1, C2> where C1 : unmanaged where C2 : unmanaged
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
            required.AddComponentTypes(world.world->schema.GetComponentTypes<C1, C2>());
            this.world = world;
        }

        /// <summary>
        /// Specifies if disabled entities should be excluded or not.
        /// </summary>
        public ComponentQuery<C1, C2> ExcludeDisabled(bool should)
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
        public ComponentQuery<C1, C2> RequireArray<T>() where T : unmanaged
        {
            required.AddArrayType<T>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given array types required.
        /// </summary>
        public ComponentQuery<C1, C2> RequireArrays<T1, T2>() where T1 : unmanaged where T2 : unmanaged
        {
            required.AddArrayTypes<T1, T2>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given array types required.
        /// </summary>
        public ComponentQuery<C1, C2> RequireArrays<T1, T2, T3>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            required.AddArrayTypes<T1, T2, T3>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given array types required.
        /// </summary>
        public ComponentQuery<C1, C2> RequireArrays<T1, T2, T3, T4>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            required.AddArrayTypes<T1, T2, T3, T4>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given array types required.
        /// </summary>
        public ComponentQuery<C1, C2> RequireArrays<T1, T2, T3, T4, T5>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
        {
            required.AddArrayTypes<T1, T2, T3, T4, T5>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given array types required.
        /// </summary>
        public ComponentQuery<C1, C2> RequireArrays<T1, T2, T3, T4, T5, T6>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
        {
            required.AddArrayTypes<T1, T2, T3, T4, T5, T6>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given array types required.
        /// </summary>
        public ComponentQuery<C1, C2> RequireArrays<T1, T2, T3, T4, T5, T6, T7>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged
        {
            required.AddArrayTypes<T1, T2, T3, T4, T5, T6, T7>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given array types required.
        /// </summary>
        public ComponentQuery<C1, C2> RequireArrays<T1, T2, T3, T4, T5, T6, T7, T8>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged
        {
            required.AddArrayTypes<T1, T2, T3, T4, T5, T6, T7, T8>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given array types required.
        /// </summary>
        public ComponentQuery<C1, C2> RequireArrays<T1, T2, T3, T4, T5, T6, T7, T8, T9>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged
        {
            required.AddArrayTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given array types required.
        /// </summary>
        public ComponentQuery<C1, C2> RequireArrays<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged
        {
            required.AddArrayTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given array types required.
        /// </summary>
        public ComponentQuery<C1, C2> RequireArrays<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged
        {
            required.AddArrayTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given array types required.
        /// </summary>
        public ComponentQuery<C1, C2> RequireArrays<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged
        {
            required.AddArrayTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given tag types required.
        /// </summary>
        public ComponentQuery<C1, C2> RequireTag<T>() where T : unmanaged
        {
            required.AddTagType<T>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given tag types required.
        /// </summary>
        public ComponentQuery<C1, C2> RequireTags<T1, T2>() where T1 : unmanaged where T2 : unmanaged
        {
            required.AddTagTypes<T1, T2>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given tag types required.
        /// </summary>
        public ComponentQuery<C1, C2> RequireTags<T1, T2, T3>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            required.AddTagTypes<T1, T2, T3>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given tag types required.
        /// </summary>
        public ComponentQuery<C1, C2> RequireTags<T1, T2, T3, T4>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            required.AddTagTypes<T1, T2, T3, T4>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given tag types required.
        /// </summary>
        public ComponentQuery<C1, C2> RequireTags<T1, T2, T3, T4, T5>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
        {
            required.AddTagTypes<T1, T2, T3, T4, T5>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given tag types required.
        /// </summary>
        public ComponentQuery<C1, C2> RequireTags<T1, T2, T3, T4, T5, T6>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
        {
            required.AddTagTypes<T1, T2, T3, T4, T5, T6>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given tag types required.
        /// </summary>
        public ComponentQuery<C1, C2> RequireTags<T1, T2, T3, T4, T5, T6, T7>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged
        {
            required.AddTagTypes<T1, T2, T3, T4, T5, T6, T7>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given tag types required.
        /// </summary>
        public ComponentQuery<C1, C2> RequireTags<T1, T2, T3, T4, T5, T6, T7, T8>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged
        {
            required.AddTagTypes<T1, T2, T3, T4, T5, T6, T7, T8>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given tag types required.
        /// </summary>
        public ComponentQuery<C1, C2> RequireTags<T1, T2, T3, T4, T5, T6, T7, T8, T9>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged
        {
            required.AddTagTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given tag types required.
        /// </summary>
        public ComponentQuery<C1, C2> RequireTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged
        {
            required.AddTagTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given tag types required.
        /// </summary>
        public ComponentQuery<C1, C2> RequireTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged
        {
            required.AddTagTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given tag types required.
        /// </summary>
        public ComponentQuery<C1, C2> RequireTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged
        {
            required.AddTagTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given component types required.
        /// </summary>
        public ComponentQuery<C1, C2> RequireComponent<T1>() where T1 : unmanaged
        {
            required.AddComponentType<T1>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given component types required.
        /// </summary>
        public ComponentQuery<C1, C2> RequireComponents<T1, T2>() where T1 : unmanaged where T2 : unmanaged
        {
            required.AddComponentTypes<T1, T2>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given component types required.
        /// </summary>
        public ComponentQuery<C1, C2> RequireComponents<T1, T2, T3>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            required.AddComponentTypes<T1, T2, T3>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given component types required.
        /// </summary>
        public ComponentQuery<C1, C2> RequireComponents<T1, T2, T3, T4>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            required.AddComponentTypes<T1, T2, T3, T4>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given component types required.
        /// </summary>
        public ComponentQuery<C1, C2> RequireComponents<T1, T2, T3, T4, T5>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
        {
            required.AddComponentTypes<T1, T2, T3, T4, T5>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given component types required.
        /// </summary>
        public ComponentQuery<C1, C2> RequireComponents<T1, T2, T3, T4, T5, T6>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
        {
            required.AddComponentTypes<T1, T2, T3, T4, T5, T6>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given component types required.
        /// </summary>
        public ComponentQuery<C1, C2> RequireComponents<T1, T2, T3, T4, T5, T6, T7>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged
        {
            required.AddComponentTypes<T1, T2, T3, T4, T5, T6, T7>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given component types required.
        /// </summary>
        public ComponentQuery<C1, C2> RequireComponents<T1, T2, T3, T4, T5, T6, T7, T8>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged
        {
            required.AddComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given component types required.
        /// </summary>
        public ComponentQuery<C1, C2> RequireComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged
        {
            required.AddComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given component types required.
        /// </summary>
        public ComponentQuery<C1, C2> RequireComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged
        {
            required.AddComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given component types required.
        /// </summary>
        public ComponentQuery<C1, C2> RequireComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged
        {
            required.AddComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given component types required.
        /// </summary>
        public ComponentQuery<C1, C2> RequireComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged
        {
            required.AddComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given component types excluded.
        /// </summary>
        public ComponentQuery<C1, C2> ExcludeComponent<T1>() where T1 : unmanaged
        {
            exclude.AddComponentType<T1>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given component types excluded.
        /// </summary>
        public ComponentQuery<C1, C2> ExcludeComponents<T1, T2>() where T1 : unmanaged where T2 : unmanaged
        {
            exclude.AddComponentTypes<T1, T2>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given component types excluded.
        /// </summary>
        public ComponentQuery<C1, C2> ExcludeComponents<T1, T2, T3>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            exclude.AddComponentTypes<T1, T2, T3>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given component types excluded.
        /// </summary>
        public ComponentQuery<C1, C2> ExcludeComponents<T1, T2, T3, T4>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            exclude.AddComponentTypes<T1, T2, T3, T4>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given component types excluded.
        /// </summary>
        public ComponentQuery<C1, C2> ExcludeComponents<T1, T2, T3, T4, T5>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
        {
            exclude.AddComponentTypes<T1, T2, T3, T4, T5>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given component types excluded.
        /// </summary>
        public ComponentQuery<C1, C2> ExcludeComponents<T1, T2, T3, T4, T5, T6>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
        {
            exclude.AddComponentTypes<T1, T2, T3, T4, T5, T6>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given component types excluded.
        /// </summary>
        public ComponentQuery<C1, C2> ExcludeComponents<T1, T2, T3, T4, T5, T6, T7>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged
        {
            exclude.AddComponentTypes<T1, T2, T3, T4, T5, T6, T7>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given component types excluded.
        /// </summary>
        public ComponentQuery<C1, C2> ExcludeComponents<T1, T2, T3, T4, T5, T6, T7, T8>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged
        {
            exclude.AddComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given component types excluded.
        /// </summary>
        public ComponentQuery<C1, C2> ExcludeComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged
        {
            exclude.AddComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given component types excluded.
        /// </summary>
        public ComponentQuery<C1, C2> ExcludeComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged
        {
            exclude.AddComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given component types excluded.
        /// </summary>
        public ComponentQuery<C1, C2> ExcludeComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged
        {
            exclude.AddComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given component types excluded.
        /// </summary>
        public ComponentQuery<C1, C2> ExcludeComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged
        {
            exclude.AddComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given tag types excluded.
        /// </summary>
        public ComponentQuery<C1, C2> ExcludeTag<T1>() where T1 : unmanaged
        {
            exclude.AddTagType<T1>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given tag types excluded.
        /// </summary>
        public ComponentQuery<C1, C2> ExcludeTags<T1, T2>() where T1 : unmanaged where T2 : unmanaged
        {
            exclude.AddTagTypes<T1, T2>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given tag types excluded.
        /// </summary>
        public ComponentQuery<C1, C2> ExcludeTags<T1, T2, T3>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            exclude.AddTagTypes<T1, T2, T3>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given tag types excluded.
        /// </summary>
        public ComponentQuery<C1, C2> ExcludeTags<T1, T2, T3, T4>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            exclude.AddTagTypes<T1, T2, T3, T4>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given tag types excluded.
        /// </summary>
        public ComponentQuery<C1, C2> ExcludeTags<T1, T2, T3, T4, T5>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
        {
            exclude.AddTagTypes<T1, T2, T3, T4, T5>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given tag types excluded.
        /// </summary>
        public ComponentQuery<C1, C2> ExcludeTags<T1, T2, T3, T4, T5, T6>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
        {
            exclude.AddTagTypes<T1, T2, T3, T4, T5, T6>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given tag types excluded.
        /// </summary>
        public ComponentQuery<C1, C2> ExcludeTags<T1, T2, T3, T4, T5, T6, T7>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged
        {
            exclude.AddTagTypes<T1, T2, T3, T4, T5, T6, T7>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given tag types excluded.
        /// </summary>
        public ComponentQuery<C1, C2> ExcludeTags<T1, T2, T3, T4, T5, T6, T7, T8>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged
        {
            exclude.AddTagTypes<T1, T2, T3, T4, T5, T6, T7, T8>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given tag types excluded.
        /// </summary>
        public ComponentQuery<C1, C2> ExcludeTags<T1, T2, T3, T4, T5, T6, T7, T8, T9>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged
        {
            exclude.AddTagTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given tag types excluded.
        /// </summary>
        public ComponentQuery<C1, C2> ExcludeTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged
        {
            exclude.AddTagTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given tag types excluded.
        /// </summary>
        public ComponentQuery<C1, C2> ExcludeTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged
        {
            exclude.AddTagTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given tag types excluded.
        /// </summary>
        public ComponentQuery<C1, C2> ExcludeTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged
        {
            exclude.AddTagTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given array types excluded.
        /// </summary>
        public ComponentQuery<C1, C2> ExcludeArray<T1>() where T1 : unmanaged
        {
            exclude.AddArrayType<T1>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given array types excluded.
        /// </summary>
        public ComponentQuery<C1, C2> ExcludeArrays<T1, T2>() where T1 : unmanaged where T2 : unmanaged
        {
            exclude.AddArrayTypes<T1, T2>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given array types excluded.
        /// </summary>
        public ComponentQuery<C1, C2> ExcludeArrays<T1, T2, T3>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            exclude.AddArrayTypes<T1, T2, T3>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given array types excluded.
        /// </summary>
        public ComponentQuery<C1, C2> ExcludeArrays<T1, T2, T3, T4>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            exclude.AddArrayTypes<T1, T2, T3, T4>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given array types excluded.
        /// </summary>
        public ComponentQuery<C1, C2> ExcludeArrays<T1, T2, T3, T4, T5>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
        {
            exclude.AddArrayTypes<T1, T2, T3, T4, T5>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given array types excluded.
        /// </summary>
        public ComponentQuery<C1, C2> ExcludeArrays<T1, T2, T3, T4, T5, T6>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
        {
            exclude.AddArrayTypes<T1, T2, T3, T4, T5, T6>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given array types excluded.
        /// </summary>
        public ComponentQuery<C1, C2> ExcludeArrays<T1, T2, T3, T4, T5, T6, T7>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged
        {
            exclude.AddArrayTypes<T1, T2, T3, T4, T5, T6, T7>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given array types excluded.
        /// </summary>
        public ComponentQuery<C1, C2> ExcludeArrays<T1, T2, T3, T4, T5, T6, T7, T8>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged
        {
            exclude.AddArrayTypes<T1, T2, T3, T4, T5, T6, T7, T8>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given array types excluded.
        /// </summary>
        public ComponentQuery<C1, C2> ExcludeArrays<T1, T2, T3, T4, T5, T6, T7, T8, T9>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged
        {
            exclude.AddArrayTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given array types excluded.
        /// </summary>
        public ComponentQuery<C1, C2> ExcludeArrays<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged
        {
            exclude.AddArrayTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given array types excluded.
        /// </summary>
        public ComponentQuery<C1, C2> ExcludeArrays<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged
        {
            exclude.AddArrayTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(world.world->schema);
            return this;
        }

        /// <summary>
        /// Makes the given array types excluded.
        /// </summary>
        public ComponentQuery<C1, C2> ExcludeArrays<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged
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
            private readonly ReadOnlySpan<Chunk> chunks;
            private readonly ComponentQuery<C1, C2> query;
            private readonly int componentType1;
            private readonly uint componentOffset1;
            private readonly int componentType2;
            private readonly uint componentOffset2;
            private int version;
            private int entityIndex;
            private int entityCount;
            private int chunkIndex;
            private ReadOnlySpan<uint> entities;
            private List components;

            /// <summary>
            /// Current result.
            /// </summary>
            public readonly Chunk.Entity<C1, C2> Current
            {
                get
                {
                    uint entity = entities[entityIndex];
                    MemoryAddress componentRow = components[entityIndex];
                    ref C1 component1 = ref componentRow.Read<C1>(componentOffset1);
                    ref C2 component2 = ref componentRow.Read<C2>(componentOffset2);
                    return new(entity, ref component1, ref component2);
                }
            }

            internal unsafe Enumerator(ComponentQuery<C1, C2> query)
            {
                this.query = query;
                int chunkCount = 0;
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
                if (chunkCount > 0)
                {
                    MemoryAddress chunksMemory = MemoryAddress.Allocate(chunksBuffer.Slice(0, chunkCount));
                    chunks = chunksMemory.GetSpan<Chunk>(chunkCount);
                    Chunk chunk = chunks[0];
                    version = chunk.chunk->version;
                    entities = new(chunk.chunk->entities.Items.Pointer, chunk.chunk->count + 1);
                    entityCount = chunk.chunk->count;
                    components = chunk.chunk->components;
                    Span<uint> componentOffsets = new(schema.schema->componentOffsets, BitMask.Capacity);
                    componentOffset1 = componentOffsets[componentType1];
                    componentOffset2 = componentOffsets[componentType2];
                }
                else
                {
                    entities = default;
                }
            }

            [Conditional("DEBUG")]
            private readonly void ThrowIfVersionIsDifferent() 
            {
                Chunk chunk = chunks[chunkIndex];
                if (version != chunk.Version)
                {
                    throw new ChunkModifiedWhileIteratingException(chunk);
                }
            }

            /// <summary>
            /// Moves to the next result.
            /// </summary>
            public bool MoveNext()
            {
                if (entityIndex < entityCount)
                {
                    ThrowIfVersionIsDifferent();

                    entityIndex++;
                    return true;
                }
                else
                {
                    chunkIndex++;
                    if (chunkIndex < chunks.Length)
                    {
                        Chunk chunk = chunks[chunkIndex];
                        version = chunk.chunk->version;
                        entities = new(chunk.chunk->entities.Items.Pointer, chunk.chunk->count + 1);
                        entityCount = chunk.chunk->count;
                        components = chunk.chunk->components;
                        entityIndex = 1;
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            /// <inheritdoc/>
            public readonly void Dispose()
            {
                if (chunks.Length > 0)
                {
                    void* pointer = chunks.GetPointer();
                    MemoryAddress.Free(ref pointer);
                }
            }
        }
    }
}