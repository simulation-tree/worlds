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

        public readonly uint Length
        {
            get => pointer->length;
            set
            {

                if (pointer->length != value)
                {
                    uint oldLength = pointer->length;
                    Allocation.Resize(ref pointer->items, (uint)sizeof(T) * value);
                    pointer->length = value;
                }
            }
        }

        public readonly ref T this[uint index] => ref pointer->items.ReadElement<T>(index);

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

        public readonly USpan<X> AsSpan<X>() where X : unmanaged
        {
            ThrowIfSizeMismatch<X>();

            return new(pointer->items.Pointer, pointer->length);
        }

        public readonly USpan<T> AsSpan()
        {
            return new(pointer->items.Pointer, pointer->length);
        }

        public readonly USpan<T> AsSpan(uint start)
        {
            return pointer->items.AsSpan<T>(start, pointer->length - start);
        }

        public readonly USpan<X> AsSpan<X>(uint start) where X : unmanaged
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
        public readonly void CopyFrom(USpan<T> span)
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

        public readonly uint Length
        {
            get => pointer->length;
            set
            {
                if (pointer->length != value)
                {
                    uint oldLength = pointer->length;
                    Allocation.Resize(ref pointer->items, pointer->stride * value);
                    pointer->length = value;
                }
            }
        }

        public readonly uint Stride => pointer->stride;
        public readonly Allocation this[uint index] => new((void*)((nint)pointer->items + pointer->stride * index));

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

        public readonly USpan<byte> AsSpan()
        {
            return new(pointer->items.Pointer, pointer->length * pointer->stride);
        }

        public readonly void Write<T>(USpan<T> values) where T : unmanaged
        {
            pointer->items.Write(values);
        }

        public readonly void Write<T>(uint bytePosition, T value) where T : unmanaged
        {
            pointer->items.Write(bytePosition, value);
        }

        public readonly void Write<T>(uint bytePosition, USpan<T> values) where T : unmanaged
        {
            pointer->items.Write(bytePosition, values);
        }

        public readonly Allocation Read(uint bytePosition)
        {
            return pointer->items.Read(bytePosition);
        }

        public readonly ref T Read<T>(uint bytePosition) where T : unmanaged
        {
            return ref pointer->items.Read<T>(bytePosition);
        }

        public readonly ref T Get<T>(uint index) where T : unmanaged
        {
            return ref pointer->items.ReadElement<T>(index);
        }

        public readonly void Set<T>(uint index, T value) where T : unmanaged
        {
            pointer->items.WriteElement(index, value);
        }
    }
}