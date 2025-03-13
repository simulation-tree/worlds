﻿using Collections;
using System;

namespace Worlds
{
    public ref struct ComponentEnumerator<T> where T : unmanaged
    {
        public readonly int Length;

        private readonly List components;
        private readonly int componentOffset;
        private int index;

        public readonly ref T Current => ref components[index].Read<T>(componentOffset);
        public readonly ref T this[int index] => ref components[index + 1].Read<T>(componentOffset);

        internal ComponentEnumerator(List components, int componentOffset)
        {
            this.components = components;
            this.componentOffset = componentOffset;
            Length = components.Count - 1;
            index = 0;
        }

        public bool MoveNext()
        {
            index++;
            return index <= Length;
        }

        public void Reset()
        {
            index = 0;
        }

        /// <summary>
        /// Copies all components to the <paramref name="destination"/> span.
        /// </summary>
        public readonly void CopyTo(Span<T> destination)
        {
            for (int i = 0; i < Length; i++)
            {
                destination[i] = components[i + 1].Read<T>(componentOffset);
            }
        }
    }
}