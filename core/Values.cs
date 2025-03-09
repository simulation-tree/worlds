using Collections.Generic;
using System;
using System.Diagnostics;
using Unmanaged;
using Array = Collections.Array;
using Pointer = Collections.Pointers.Array;

namespace Worlds
{
    public unsafe readonly struct Values<T> where T : unmanaged
    {
        internal readonly Pointer* pointer;

        public readonly int Length
        {
            get => pointer->length;
            set
            {

                if (pointer->length != value)
                {
                    int oldLength = pointer->length;
                    MemoryAddress.Resize(ref pointer->items, sizeof(T) * value);
                    pointer->length = value;
                }
            }
        }

        public readonly ref T this[int index] => ref pointer->items.ReadElement<T>(index);

        internal Values(Array<T> array)
        {
            this.pointer = array.Pointer;
        }

        internal Values(Pointer* array)
        {
            this.pointer = array;
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfSizeMismatch<X>() where X : unmanaged
        {
            if (sizeof(T) != sizeof(X))
            {
                throw new InvalidOperationException($"Size mismatch between {typeof(T).Name} and {typeof(X).Name}");
            }
        }
        public readonly Values<X> As<X>() where X : unmanaged
        {
            ThrowIfSizeMismatch<X>();

            return new(pointer);
        }

        public readonly Span<X> AsSpan<X>() where X : unmanaged
        {
            ThrowIfSizeMismatch<X>();

            return new(pointer->items.Pointer, pointer->length);
        }

        public readonly Span<T> AsSpan()
        {
            return new(pointer->items.Pointer, pointer->length);
        }

        public readonly Span<T> AsSpan(int start)
        {
            return pointer->items.AsSpan<T>(start, pointer->length - start);
        }

        public readonly Span<X> AsSpan<X>(int start) where X : unmanaged
        {
            ThrowIfSizeMismatch<X>();

            return pointer->items.AsSpan<X>(start, pointer->length - start);
        }

        public readonly Span<T>.Enumerator GetEnumerator()
        {
            return new Span<T>(pointer->items.Pointer, (int)pointer->length).GetEnumerator();
        }

        /// <summary>
        /// Copies data from the given <paramref name="span"/> into the array without resizing.
        /// </summary>
        public readonly void CopyFrom(Span<T> span)
        {
            pointer->items.Write(span);
        }

        /// <summary>
        /// Copies data from the given <paramref name="span"/> into the array without resizing.
        /// </summary>
        public readonly void CopyFrom(ReadOnlySpan<T> span)
        {
            pointer->items.Write(span);
        }

        public static implicit operator Values(Values<T> values)
        {
            return new Values(values.pointer);
        }
    }

    public unsafe readonly struct Values
    {
        internal readonly Pointer* pointer;

        public readonly int Length
        {
            get => pointer->length;
            set
            {
                if (pointer->length != value)
                {
                    int oldLength = pointer->length;
                    MemoryAddress.Resize(ref pointer->items, pointer->stride * value);
                    pointer->length = value;
                }
            }
        }

        public readonly int Stride => pointer->stride;
        public readonly MemoryAddress this[int index] => new(pointer->items.Pointer + pointer->stride * index);

        internal Values(Array array)
        {
            this.pointer = array.Pointer;
        }

        internal Values(Pointer* array)
        {
            this.pointer = array;
        }

        internal readonly void Dispose()
        {
            Array array = new(pointer);
            array.Dispose();
        }

        public readonly Span<byte> AsSpan()
        {
            return new(pointer->items.Pointer, pointer->length * pointer->stride);
        }

        public readonly Span<byte> Slice(int bytePosition, int byteLength)
        {
            return new(pointer->items.Pointer + bytePosition, byteLength);
        }

        public readonly void Write<T>(Span<T> values) where T : unmanaged
        {
            pointer->items.Write(values);
        }

        public readonly void Write<T>(ReadOnlySpan<T> values) where T : unmanaged
        {
            pointer->items.Write(values);
        }

        public readonly void Write<T>(int bytePosition, T value) where T : unmanaged
        {
            pointer->items.Write(bytePosition, value);
        }

        public readonly void Write<T>(int bytePosition, ReadOnlySpan<T> values) where T : unmanaged
        {
            pointer->items.Write(bytePosition, values);
        }

        public readonly void Write<T>(int bytePosition, Span<T> values) where T : unmanaged
        {
            pointer->items.Write(bytePosition, values);
        }

        public readonly MemoryAddress Read(int bytePosition)
        {
            return pointer->items.Read(bytePosition);
        }

        public readonly ref T Read<T>(int bytePosition) where T : unmanaged
        {
            return ref pointer->items.Read<T>(bytePosition);
        }

        public readonly ref T Get<T>(int index) where T : unmanaged
        {
            return ref pointer->items.ReadElement<T>(index);
        }

        public readonly void Set<T>(int index, T value) where T : unmanaged
        {
            pointer->items.WriteElement(index, value);
        }
    }
}