using System;
using System.Runtime.InteropServices;
using Unmanaged;

namespace Worlds
{
    public struct ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged where C13 : unmanaged where C14 : unmanaged where C15 : unmanaged where C16 : unmanaged
    {
        private Definition required;
        private Definition exclude;
        private readonly World world;

#if NET
        [Obsolete("Default constructor not supported", true)]
        public ComponentQuery()
        {
            throw new NotSupportedException();
        }
#endif

        public ComponentQuery(World world)
        {
            required.AddComponentTypes(world.Schema.GetComponents<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16>());
            this.world = world;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> ExcludeDisabled(bool should)
        {
            if (should)
            {
                exclude.AddTagType(TagType.Disabled);
            }
            else
            {
                exclude.RemoveTagType(TagType.Disabled);
            }

            return this;
        }
        
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> RequireArray<T>() where T : unmanaged
        {
            required.AddArrayElementType<T>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> RequireArrays<T1, T2>() where T1 : unmanaged where T2 : unmanaged
        {
            required.AddArrayElementTypes<T1, T2>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> RequireArrays<T1, T2, T3>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            required.AddArrayElementTypes<T1, T2, T3>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> RequireArrays<T1, T2, T3, T4>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            required.AddArrayElementTypes<T1, T2, T3, T4>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> RequireArrays<T1, T2, T3, T4, T5>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
        {
            required.AddArrayElementTypes<T1, T2, T3, T4, T5>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> RequireArrays<T1, T2, T3, T4, T5, T6>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
        {
            required.AddArrayElementTypes<T1, T2, T3, T4, T5, T6>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> RequireArrays<T1, T2, T3, T4, T5, T6, T7>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged
        {
            required.AddArrayElementTypes<T1, T2, T3, T4, T5, T6, T7>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> RequireArrays<T1, T2, T3, T4, T5, T6, T7, T8>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged
        {
            required.AddArrayElementTypes<T1, T2, T3, T4, T5, T6, T7, T8>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> RequireArrays<T1, T2, T3, T4, T5, T6, T7, T8, T9>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged
        {
            required.AddArrayElementTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> RequireArrays<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged
        {
            required.AddArrayElementTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> RequireArrays<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged
        {
            required.AddArrayElementTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> RequireArrays<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged
        {
            required.AddArrayElementTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> RequireTag<T>() where T : unmanaged
        {
            required.AddTagType<T>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> RequireTags<T1, T2>() where T1 : unmanaged where T2 : unmanaged
        {
            required.AddTagTypes<T1, T2>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> RequireTags<T1, T2, T3>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            required.AddTagTypes<T1, T2, T3>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> RequireTags<T1, T2, T3, T4>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            required.AddTagTypes<T1, T2, T3, T4>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> RequireTags<T1, T2, T3, T4, T5>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
        {
            required.AddTagTypes<T1, T2, T3, T4, T5>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> RequireTags<T1, T2, T3, T4, T5, T6>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
        {
            required.AddTagTypes<T1, T2, T3, T4, T5, T6>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> RequireTags<T1, T2, T3, T4, T5, T6, T7>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged
        {
            required.AddTagTypes<T1, T2, T3, T4, T5, T6, T7>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> RequireTags<T1, T2, T3, T4, T5, T6, T7, T8>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged
        {
            required.AddTagTypes<T1, T2, T3, T4, T5, T6, T7, T8>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> RequireTags<T1, T2, T3, T4, T5, T6, T7, T8, T9>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged
        {
            required.AddTagTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> RequireTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged
        {
            required.AddTagTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> RequireTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged
        {
            required.AddTagTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> RequireTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged
        {
            required.AddTagTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> RequireComponent<T1>() where T1 : unmanaged
        {
            required.AddComponentType<T1>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> RequireComponents<T1, T2>() where T1 : unmanaged where T2 : unmanaged
        {
            required.AddComponentTypes<T1, T2>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> RequireComponents<T1, T2, T3>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            required.AddComponentTypes<T1, T2, T3>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> RequireComponents<T1, T2, T3, T4>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            required.AddComponentTypes<T1, T2, T3, T4>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> RequireComponents<T1, T2, T3, T4, T5>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
        {
            required.AddComponentTypes<T1, T2, T3, T4, T5>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> RequireComponents<T1, T2, T3, T4, T5, T6>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
        {
            required.AddComponentTypes<T1, T2, T3, T4, T5, T6>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> RequireComponents<T1, T2, T3, T4, T5, T6, T7>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged
        {
            required.AddComponentTypes<T1, T2, T3, T4, T5, T6, T7>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> RequireComponents<T1, T2, T3, T4, T5, T6, T7, T8>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged
        {
            required.AddComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> RequireComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged
        {
            required.AddComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> RequireComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged
        {
            required.AddComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> RequireComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged
        {
            required.AddComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> RequireComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged
        {
            required.AddComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> ExcludeComponent<T1>() where T1 : unmanaged
        {
            exclude.AddComponentType<T1>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> ExcludeComponents<T1, T2>() where T1 : unmanaged where T2 : unmanaged
        {
            exclude.AddComponentTypes<T1, T2>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> ExcludeComponents<T1, T2, T3>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            exclude.AddComponentTypes<T1, T2, T3>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> ExcludeComponents<T1, T2, T3, T4>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            exclude.AddComponentTypes<T1, T2, T3, T4>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> ExcludeComponents<T1, T2, T3, T4, T5>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
        {
            exclude.AddComponentTypes<T1, T2, T3, T4, T5>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> ExcludeComponents<T1, T2, T3, T4, T5, T6>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
        {
            exclude.AddComponentTypes<T1, T2, T3, T4, T5, T6>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> ExcludeComponents<T1, T2, T3, T4, T5, T6, T7>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged
        {
            exclude.AddComponentTypes<T1, T2, T3, T4, T5, T6, T7>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> ExcludeComponents<T1, T2, T3, T4, T5, T6, T7, T8>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged
        {
            exclude.AddComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> ExcludeComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged
        {
            exclude.AddComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> ExcludeComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged
        {
            exclude.AddComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> ExcludeComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged
        {
            exclude.AddComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> ExcludeComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged
        {
            exclude.AddComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> ExcludeTag<T1>() where T1 : unmanaged
        {
            exclude.AddTagType<T1>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> ExcludeTags<T1, T2>() where T1 : unmanaged where T2 : unmanaged
        {
            exclude.AddTagTypes<T1, T2>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> ExcludeTags<T1, T2, T3>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            exclude.AddTagTypes<T1, T2, T3>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> ExcludeTags<T1, T2, T3, T4>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            exclude.AddTagTypes<T1, T2, T3, T4>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> ExcludeTags<T1, T2, T3, T4, T5>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
        {
            exclude.AddTagTypes<T1, T2, T3, T4, T5>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> ExcludeTags<T1, T2, T3, T4, T5, T6>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
        {
            exclude.AddTagTypes<T1, T2, T3, T4, T5, T6>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> ExcludeTags<T1, T2, T3, T4, T5, T6, T7>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged
        {
            exclude.AddTagTypes<T1, T2, T3, T4, T5, T6, T7>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> ExcludeTags<T1, T2, T3, T4, T5, T6, T7, T8>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged
        {
            exclude.AddTagTypes<T1, T2, T3, T4, T5, T6, T7, T8>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> ExcludeTags<T1, T2, T3, T4, T5, T6, T7, T8, T9>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged
        {
            exclude.AddTagTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> ExcludeTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged
        {
            exclude.AddTagTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> ExcludeTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged
        {
            exclude.AddTagTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> ExcludeTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged
        {
            exclude.AddTagTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> ExcludeArrayElement<T1>() where T1 : unmanaged
        {
            exclude.AddArrayElementType<T1>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> ExcludeArrayElements<T1, T2>() where T1 : unmanaged where T2 : unmanaged
        {
            exclude.AddArrayElementTypes<T1, T2>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> ExcludeArrayElements<T1, T2, T3>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            exclude.AddArrayElementTypes<T1, T2, T3>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> ExcludeArrayElements<T1, T2, T3, T4>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            exclude.AddArrayElementTypes<T1, T2, T3, T4>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> ExcludeArrayElements<T1, T2, T3, T4, T5>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
        {
            exclude.AddArrayElementTypes<T1, T2, T3, T4, T5>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> ExcludeArrayElements<T1, T2, T3, T4, T5, T6>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
        {
            exclude.AddArrayElementTypes<T1, T2, T3, T4, T5, T6>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> ExcludeArrayElements<T1, T2, T3, T4, T5, T6, T7>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged
        {
            exclude.AddArrayElementTypes<T1, T2, T3, T4, T5, T6, T7>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> ExcludeArrayElements<T1, T2, T3, T4, T5, T6, T7, T8>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged
        {
            exclude.AddArrayElementTypes<T1, T2, T3, T4, T5, T6, T7, T8>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> ExcludeArrayElements<T1, T2, T3, T4, T5, T6, T7, T8, T9>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged
        {
            exclude.AddArrayElementTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> ExcludeArrayElements<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged
        {
            exclude.AddArrayElementTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> ExcludeArrayElements<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged
        {
            exclude.AddArrayElementTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> ExcludeArrayElements<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged
        {
            exclude.AddArrayElementTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(world.Schema);
            return this;
        }

        /// <summary>
        /// Enumerates the query results.
        /// </summary>
        public readonly Enumerator GetEnumerator()
        {
            return new(required, exclude, world.Chunks, world.Schema);
        }

        public unsafe ref struct Enumerator
        {
            private static readonly uint stride = (uint)sizeof(Chunk);

            private readonly Allocation chunks;
            private readonly uint chunkCount;
            private readonly ComponentType c1;
            private readonly ComponentType c2;
            private readonly ComponentType c3;
            private readonly ComponentType c4;
            private readonly ComponentType c5;
            private readonly ComponentType c6;
            private readonly ComponentType c7;
            private readonly ComponentType c8;
            private readonly ComponentType c9;
            private readonly ComponentType c10;
            private readonly ComponentType c11;
            private readonly ComponentType c12;
            private readonly ComponentType c13;
            private readonly ComponentType c14;
            private readonly ComponentType c15;
            private readonly ComponentType c16;
            private uint entityIndex;
            private uint chunkIndex;
            private USpan<uint> entities;
            private USpan<C1> list1;
            private USpan<C2> list2;
            private USpan<C3> list3;
            private USpan<C4> list4;
            private USpan<C5> list5;
            private USpan<C6> list6;
            private USpan<C7> list7;
            private USpan<C8> list8;
            private USpan<C9> list9;
            private USpan<C10> list10;
            private USpan<C11> list11;
            private USpan<C12> list12;
            private USpan<C13> list13;
            private USpan<C14> list14;
            private USpan<C15> list15;
            private USpan<C16> list16;

            /// <summary>
            /// Current result.
            /// </summary>
            public readonly Chunk.Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12, C13, C14, C15, C16> Current
            {
                get
                {
                    uint index = entityIndex - 1;
                    uint entity = entities[index];
                    ref C1 c1 = ref list1[index];
                    ref C2 c2 = ref list2[index];
                    ref C3 c3 = ref list3[index];
                    ref C4 c4 = ref list4[index];
                    ref C5 c5 = ref list5[index];
                    ref C6 c6 = ref list6[index];
                    ref C7 c7 = ref list7[index];
                    ref C8 c8 = ref list8[index];
                    ref C9 c9 = ref list9[index];
                    ref C10 c10 = ref list10[index];
                    ref C11 c11 = ref list11[index];
                    ref C12 c12 = ref list12[index];
                    ref C13 c13 = ref list13[index];
                    ref C14 c14 = ref list14[index];
                    ref C15 c15 = ref list15[index];
                    ref C16 c16 = ref list16[index];
                    return new(entity, ref c1, ref c2, ref c3, ref c4, ref c5, ref c6, ref c7, ref c8, ref c9, ref c10, ref c11, ref c12, ref c13, ref c14, ref c15, ref c16);
                }
            }

            internal Enumerator(Definition required, Definition exclude, USpan<Chunk> allChunks, Schema schema)
            {
                chunkCount = 0;
                USpan<Chunk> chunksBuffer = stackalloc Chunk[(int)allChunks.Length];
                foreach (Chunk chunk in allChunks)
                {
                    if (chunk.Count > 0)
                    {
                        Definition key = chunk.Definition;

                        //check if chunk contains inclusion
                        if ((key.ComponentTypes & required.ComponentTypes) != required.ComponentTypes)
                        {
                            continue;
                        }

                        if ((key.ArrayElementTypes & required.ArrayElementTypes) != required.ArrayElementTypes)
                        {
                            continue;
                        }

                        if ((key.TagTypes & required.TagTypes) != required.TagTypes)
                        {
                            continue;
                        }

                        //check if chunk doesnt contain exclusion
                        if (key.ComponentTypes.ContainsAny(exclude.ComponentTypes))
                        {
                            continue;
                        }

                        if (key.ArrayElementTypes.ContainsAny(exclude.ArrayElementTypes))
                        {
                            continue;
                        }

                        if (key.TagTypes.ContainsAny(exclude.TagTypes))
                        {
                            continue;
                        }

                        chunksBuffer[chunkCount++] = chunk;
                    }
                }

                entityIndex = 0;
                chunkIndex = 0;
                if (chunkCount > 0)
                {
                    c1 = schema.GetComponent<C1>();
                    c2 = schema.GetComponent<C2>();
                    c3 = schema.GetComponent<C3>();
                    c4 = schema.GetComponent<C4>();
                    c5 = schema.GetComponent<C5>();
                    c6 = schema.GetComponent<C6>();
                    c7 = schema.GetComponent<C7>();
                    c8 = schema.GetComponent<C8>();
                    c9 = schema.GetComponent<C9>();
                    c10 = schema.GetComponent<C10>();
                    c11 = schema.GetComponent<C11>();
                    c12 = schema.GetComponent<C12>();
                    c13 = schema.GetComponent<C13>();
                    c14 = schema.GetComponent<C14>();
                    c15 = schema.GetComponent<C15>();
                    c16 = schema.GetComponent<C16>();
                    chunks = new(NativeMemory.Alloc(chunkCount * stride));
                    chunks.CopyFrom(chunksBuffer.Pointer, stride * chunkCount);
                    UpdateChunkFields(ref chunksBuffer[0]);
                }
            }

            /// <summary>
            /// Moves to the next result.
            /// </summary>
            public bool MoveNext()
            {
                if (entityIndex < entities.Length)
                {
                    entityIndex++;
                    return true;
                }
                else
                {
                    chunkIndex++;
                    if (chunkIndex < chunkCount)
                    {
                        UpdateChunkFields(ref chunks.Read<Chunk>(chunkIndex * stride));
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
                entities = chunk.Entities;
                list1 = chunk.GetComponents<C1>(c1);
                list2 = chunk.GetComponents<C2>(c2);
                list3 = chunk.GetComponents<C3>(c3);
                list4 = chunk.GetComponents<C4>(c4);
                list5 = chunk.GetComponents<C5>(c5);
                list6 = chunk.GetComponents<C6>(c6);
                list7 = chunk.GetComponents<C7>(c7);
                list8 = chunk.GetComponents<C8>(c8);
                list9 = chunk.GetComponents<C9>(c9);
                list10 = chunk.GetComponents<C10>(c10);
                list11 = chunk.GetComponents<C11>(c11);
                list12 = chunk.GetComponents<C12>(c12);
                list13 = chunk.GetComponents<C13>(c13);
                list14 = chunk.GetComponents<C14>(c14);
                list15 = chunk.GetComponents<C15>(c15);
                list16 = chunk.GetComponents<C16>(c16);
            }

            public readonly void Dispose()
            {
                if (chunkCount > 0)
                {
                    NativeMemory.Free(chunks);
                }
            }
        }
    }
}