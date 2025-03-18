using Collections;
using System;

namespace Worlds
{
    /// <summary>
    /// Enumerator over all components of type <typeparamref name="T"/> in a <see cref="Chunk"/>.
    /// </summary>
    public ref struct ComponentEnumerator<T> where T : unmanaged
    {
        /// <summary>
        /// The number of components in the enumerator.
        /// </summary>
        public readonly int length;

        private readonly List components;
        private readonly int componentOffset;
        private int index;

        /// <summary>
        /// The current component in the enumerator.
        /// </summary>
        public readonly ref T Current => ref components[index].Read<T>(componentOffset);

        /// <summary>
        /// Reference to a <typeparamref name="T"/> component at the given <paramref name="index"/>.
        /// </summary>
        public readonly ref T this[int index] => ref components[index + 1].Read<T>(componentOffset);

        internal ComponentEnumerator(List components, int componentOffset)
        {
            this.components = components;
            this.componentOffset = componentOffset;
            length = components.Count - 1;
            index = 0;
        }

        /// <summary>
        /// Advances the enumerator to the next component in the list.
        /// </summary>
        public bool MoveNext()
        {
            index++;
            return index <= length;
        }

        /// <summary>
        /// Resets the enumerator to the first component in the list.
        /// </summary>
        public void Reset()
        {
            index = 0;
        }

        /// <summary>
        /// Copies all components to the <paramref name="destination"/> span.
        /// </summary>
        public readonly void CopyTo(Span<T> destination)
        {
            for (int i = 0; i < length; i++)
            {
                destination[i] = components[i + 1].Read<T>(componentOffset);
            }
        }
    }
}