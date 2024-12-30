using Collections;
using System;
using System.Runtime.InteropServices;
using Unmanaged;

namespace Worlds
{
    public struct ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> where C1 : unmanaged where C2 : unmanaged where C3 : unmanaged where C4 : unmanaged where C5 : unmanaged where C6 : unmanaged where C7 : unmanaged where C8 : unmanaged where C9 : unmanaged where C10 : unmanaged where C11 : unmanaged where C12 : unmanaged
    {
        private readonly BitSet includeComponents;
        private BitSet includeArrayElements;
        private BitSet includeTags;
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
            includeComponents = world.Schema.GetComponents<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12>();
            this.world = world;
        }
        
        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> IncludeArray<T>() where T : unmanaged
        {
            includeArrayElements |= world.Schema.GetArrayElement<T>();
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> IncludeArrays<T1, T2>() where T1 : unmanaged where T2 : unmanaged
        {
            includeArrayElements |= world.Schema.GetArrayElements<T1, T2>();
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> IncludeArrays<T1, T2, T3>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            includeArrayElements |= world.Schema.GetArrayElements<T1, T2, T3>();
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> IncludeArrays<T1, T2, T3, T4>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            includeArrayElements |= world.Schema.GetArrayElements<T1, T2, T3, T4>();
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> IncludeArrays<T1, T2, T3, T4, T5>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
        {
            includeArrayElements |= world.Schema.GetArrayElements<T1, T2, T3, T4, T5>();
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> IncludeArrays<T1, T2, T3, T4, T5, T6>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
        {
            includeArrayElements |= world.Schema.GetArrayElements<T1, T2, T3, T4, T5, T6>();
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> IncludeArrays<T1, T2, T3, T4, T5, T6, T7>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged
        {
            includeArrayElements |= world.Schema.GetArrayElements<T1, T2, T3, T4, T5, T6, T7>();
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> IncludeArrays<T1, T2, T3, T4, T5, T6, T7, T8>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged
        {
            includeArrayElements |= world.Schema.GetArrayElements<T1, T2, T3, T4, T5, T6, T7, T8>();
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> IncludeArrays<T1, T2, T3, T4, T5, T6, T7, T8, T9>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged
        {
            includeArrayElements |= world.Schema.GetArrayElements<T1, T2, T3, T4, T5, T6, T7, T8, T9>();
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> IncludeArrays<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged
        {
            includeArrayElements |= world.Schema.GetArrayElements<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>();
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> IncludeArrays<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged
        {
            includeArrayElements |= world.Schema.GetArrayElements<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>();
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> IncludeArrays<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged
        {
            includeArrayElements |= world.Schema.GetArrayElements<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>();
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> IncludeTag<T>() where T : unmanaged
        {
            includeTags |= world.Schema.GetTag<T>();
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> IncludeTags<T1, T2>() where T1 : unmanaged where T2 : unmanaged
        {
            includeTags |= world.Schema.GetTags<T1, T2>();
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> IncludeTags<T1, T2, T3>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            includeTags |= world.Schema.GetTags<T1, T2, T3>();
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> IncludeTags<T1, T2, T3, T4>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            includeTags |= world.Schema.GetTags<T1, T2, T3, T4>();
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> IncludeTags<T1, T2, T3, T4, T5>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
        {
            includeTags |= world.Schema.GetTags<T1, T2, T3, T4, T5>();
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> IncludeTags<T1, T2, T3, T4, T5, T6>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
        {
            includeTags |= world.Schema.GetTags<T1, T2, T3, T4, T5, T6>();
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> IncludeTags<T1, T2, T3, T4, T5, T6, T7>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged
        {
            includeTags |= world.Schema.GetTags<T1, T2, T3, T4, T5, T6, T7>();
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> IncludeTags<T1, T2, T3, T4, T5, T6, T7, T8>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged
        {
            includeTags |= world.Schema.GetTags<T1, T2, T3, T4, T5, T6, T7, T8>();
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> IncludeTags<T1, T2, T3, T4, T5, T6, T7, T8, T9>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged
        {
            includeTags |= world.Schema.GetTags<T1, T2, T3, T4, T5, T6, T7, T8, T9>();
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> IncludeTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged
        {
            includeTags |= world.Schema.GetTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>();
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> IncludeTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged
        {
            includeTags |= world.Schema.GetTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>();
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> IncludeTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged
        {
            includeTags |= world.Schema.GetTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>();
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> ExcludeComponent<T1>() where T1 : unmanaged
        {
            exclude.AddComponentType<T1>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> ExcludeComponents<T1, T2>() where T1 : unmanaged where T2 : unmanaged
        {
            exclude.AddComponentTypes<T1, T2>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> ExcludeComponents<T1, T2, T3>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            exclude.AddComponentTypes<T1, T2, T3>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> ExcludeComponents<T1, T2, T3, T4>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            exclude.AddComponentTypes<T1, T2, T3, T4>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> ExcludeComponents<T1, T2, T3, T4, T5>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
        {
            exclude.AddComponentTypes<T1, T2, T3, T4, T5>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> ExcludeComponents<T1, T2, T3, T4, T5, T6>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
        {
            exclude.AddComponentTypes<T1, T2, T3, T4, T5, T6>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> ExcludeComponents<T1, T2, T3, T4, T5, T6, T7>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged
        {
            exclude.AddComponentTypes<T1, T2, T3, T4, T5, T6, T7>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> ExcludeComponents<T1, T2, T3, T4, T5, T6, T7, T8>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged
        {
            exclude.AddComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> ExcludeComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged
        {
            exclude.AddComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> ExcludeComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged
        {
            exclude.AddComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> ExcludeComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged
        {
            exclude.AddComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> ExcludeComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged
        {
            exclude.AddComponentTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> ExcludeTag<T1>() where T1 : unmanaged
        {
            exclude.AddTagType<T1>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> ExcludeTags<T1, T2>() where T1 : unmanaged where T2 : unmanaged
        {
            exclude.AddTagTypes<T1, T2>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> ExcludeTags<T1, T2, T3>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            exclude.AddTagTypes<T1, T2, T3>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> ExcludeTags<T1, T2, T3, T4>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            exclude.AddTagTypes<T1, T2, T3, T4>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> ExcludeTags<T1, T2, T3, T4, T5>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
        {
            exclude.AddTagTypes<T1, T2, T3, T4, T5>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> ExcludeTags<T1, T2, T3, T4, T5, T6>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
        {
            exclude.AddTagTypes<T1, T2, T3, T4, T5, T6>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> ExcludeTags<T1, T2, T3, T4, T5, T6, T7>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged
        {
            exclude.AddTagTypes<T1, T2, T3, T4, T5, T6, T7>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> ExcludeTags<T1, T2, T3, T4, T5, T6, T7, T8>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged
        {
            exclude.AddTagTypes<T1, T2, T3, T4, T5, T6, T7, T8>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> ExcludeTags<T1, T2, T3, T4, T5, T6, T7, T8, T9>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged
        {
            exclude.AddTagTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> ExcludeTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged
        {
            exclude.AddTagTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> ExcludeTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged
        {
            exclude.AddTagTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> ExcludeTags<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged
        {
            exclude.AddTagTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> ExcludeArrayElement<T1>() where T1 : unmanaged
        {
            exclude.AddArrayElementType<T1>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> ExcludeArrayElements<T1, T2>() where T1 : unmanaged where T2 : unmanaged
        {
            exclude.AddArrayElementTypes<T1, T2>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> ExcludeArrayElements<T1, T2, T3>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged
        {
            exclude.AddArrayElementTypes<T1, T2, T3>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> ExcludeArrayElements<T1, T2, T3, T4>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged
        {
            exclude.AddArrayElementTypes<T1, T2, T3, T4>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> ExcludeArrayElements<T1, T2, T3, T4, T5>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged
        {
            exclude.AddArrayElementTypes<T1, T2, T3, T4, T5>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> ExcludeArrayElements<T1, T2, T3, T4, T5, T6>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged
        {
            exclude.AddArrayElementTypes<T1, T2, T3, T4, T5, T6>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> ExcludeArrayElements<T1, T2, T3, T4, T5, T6, T7>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged
        {
            exclude.AddArrayElementTypes<T1, T2, T3, T4, T5, T6, T7>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> ExcludeArrayElements<T1, T2, T3, T4, T5, T6, T7, T8>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged
        {
            exclude.AddArrayElementTypes<T1, T2, T3, T4, T5, T6, T7, T8>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> ExcludeArrayElements<T1, T2, T3, T4, T5, T6, T7, T8, T9>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged
        {
            exclude.AddArrayElementTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> ExcludeArrayElements<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged
        {
            exclude.AddArrayElementTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> ExcludeArrayElements<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged
        {
            exclude.AddArrayElementTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(world.Schema);
            return this;
        }

        public ComponentQuery<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> ExcludeArrayElements<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>() where T1 : unmanaged where T2 : unmanaged where T3 : unmanaged where T4 : unmanaged where T5 : unmanaged where T6 : unmanaged where T7 : unmanaged where T8 : unmanaged where T9 : unmanaged where T10 : unmanaged where T11 : unmanaged where T12 : unmanaged
        {
            exclude.AddArrayElementTypes<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(world.Schema);
            return this;
        }

        /// <summary>
        /// Enumerates the query results.
        /// </summary>
        public readonly Enumerator GetEnumerator()
        {
            Dictionary<Definition, Chunk> chunks = world.Chunks;
            Definition include = new(includeComponents, includeArrayElements, includeTags);
            return new(include, exclude, chunks, world.Schema);
        }

        public unsafe ref struct Enumerator
        {
            private static readonly uint stride = (uint)sizeof(Chunk);

            private readonly Allocation chunks;
            private readonly uint chunkCount;
            public readonly ComponentType c1;
            public readonly ComponentType c2;
            public readonly ComponentType c3;
            public readonly ComponentType c4;
            public readonly ComponentType c5;
            public readonly ComponentType c6;
            public readonly ComponentType c7;
            public readonly ComponentType c8;
            public readonly ComponentType c9;
            public readonly ComponentType c10;
            public readonly ComponentType c11;
            public readonly ComponentType c12;
            private uint entityCount;
            private uint entityIndex;
            private uint chunkIndex;
            private Chunk.Implementation* chunk;

            /// <summary>
            /// Current result.
            /// </summary>
            public readonly Chunk.Entity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12> Current => Chunk.Implementation.GetEntity<C1, C2, C3, C4, C5, C6, C7, C8, C9, C10, C11, C12>(chunk, entityIndex - 1, c1, c2, c3, c4, c5, c6, c7, c8, c9, c10, c11, c12);

            internal Enumerator(Definition include, Definition exclude, Dictionary<Definition, Chunk> allChunks, Schema schema)
            {
                chunkCount = 0;
                USpan<nint> chunksBuffer = stackalloc nint[(int)allChunks.Count];
                foreach (Definition key in allChunks.Keys)
                {
                    //check if chunk contains inclusion
                    if ((key.ComponentTypes & include.ComponentTypes) != include.ComponentTypes)
                    {
                        continue;
                    }

                    if ((key.ArrayElementTypes & include.ArrayElementTypes) != include.ArrayElementTypes)
                    {
                        continue;
                    }

                    if ((key.TagTypes & include.TagTypes) != include.TagTypes)
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

                    Chunk chunk = allChunks[key];
                    if (chunk.Count > 0)
                    {
                        chunksBuffer[chunkCount++] = chunk.Address;
                    }
                }

                entityIndex = 0;
                chunkIndex = 0;
                entityCount = 0;
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
                    chunks = new(NativeMemory.Alloc(chunkCount * stride));
                    chunks.CopyFrom(chunksBuffer.Pointer, stride * chunkCount);
                    chunk = (Chunk.Implementation*)chunksBuffer[0];
                    entityCount = Chunk.Implementation.GetCount(chunk);
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
                        chunk = (Chunk.Implementation*)chunks.Read<nint>(chunkIndex * stride);
                        entityCount = Chunk.Implementation.GetCount(chunk);
                        entityIndex = 1;
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
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