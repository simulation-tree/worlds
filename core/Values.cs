using Collections.Generic;
using Collections.Pointers;
using System;
using System.Diagnostics;
using Unmanaged;
using Array = Collections.Array;

namespace Worlds
{
    /// <summary>
    /// An array of <typeparamref name="T"/> values stored on an <see cref="Entity"/>.
    /// </summary>
    public unsafe readonly struct Values<T> where T : unmanaged
    {
        private readonly ArrayPointer* pointer;

        /// <summary>
        /// Length of the array.
        /// </summary>
        public readonly int Length => pointer->length;

        /// <summary>
        /// Access the reference to the element at <paramref name="index"/>.
        /// </summary>
        public readonly ref T this[int index]
        {
            get
            {
                ThrowIfOutOfRange(index);

                return ref pointer->items.ReadElement<T>(index);
            }
        }

        /// <summary>
        /// Access the reference to the element at <paramref name="index"/>.
        /// </summary>
        public readonly ref T this[uint index]
        {
            get
            {
                ThrowIfOutOfRange(index);

                return ref pointer->items.ReadElement<T>(index);
            }
        }

        internal Values(int length)
        {
            this.pointer = new Array<T>(length).Pointer;
        }

        internal Values(ReadOnlySpan<T> values)
        {
            this.pointer = new Array<T>(values).Pointer;
        }

        internal Values(ArrayPointer* array)
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

        [Conditional("DEBUG")]
        private readonly void ThrowIfOutOfRange(int index)
        {
            if (index >= pointer->length || index < 0)
            {
                throw new InvalidOperationException($"Index {index} is out of range for values of length {pointer->length}");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfOutOfRange(uint index)
        {
            if (index >= pointer->length)
            {
                throw new InvalidOperationException($"Index {index} is out of range for values of length {pointer->length}");
            }
        }

        /// <summary>
        /// Casts this array to another array of type <typeparamref name="X"/>.
        /// </summary>
        public readonly Values<X> As<X>() where X : unmanaged
        {
            ThrowIfSizeMismatch<X>();

            return new(pointer);
        }

        /// <summary>
        /// Retrieves the span of all elements.
        /// </summary>
        public readonly Span<X> AsSpan<X>() where X : unmanaged
        {
            ThrowIfSizeMismatch<X>();

            return new(pointer->items.Pointer, pointer->length);
        }

        /// <summary>
        /// Retrieves the span of all elements.
        /// </summary>
        public readonly Span<T> AsSpan()
        {
            return new(pointer->items.Pointer, pointer->length);
        }

        /// <summary>
        /// Retrieves the span of all elements starting at <paramref name="start"/>.
        /// </summary>
        public readonly Span<T> AsSpan(int start)
        {
            return pointer->items.AsSpan<T>(start, pointer->length - start);
        }

        /// <summary>
        /// Retrieves a span of <paramref name="length"/> of elements starting at <paramref name="start"/>.
        /// </summary>
        public readonly Span<T> AsSpan(int start, int length)
        {
            return pointer->items.AsSpan<T>(start, length);
        }

        /// <summary>
        /// Retrieves the span of all elements starting at <paramref name="start"/>.
        /// </summary>
        public readonly Span<X> AsSpan<X>(int start) where X : unmanaged
        {
            ThrowIfSizeMismatch<X>();

            return pointer->items.AsSpan<X>(start, pointer->length - start);
        }

        /// <summary>
        /// Retrieves a span of <paramref name="length"/> of elements starting at <paramref name="start"/>.
        /// </summary>
        public readonly Span<X> AsSpan<X>(int start, int length) where X : unmanaged
        {
            ThrowIfSizeMismatch<X>();

            return pointer->items.AsSpan<X>(start, length);
        }

        /// <summary>
        /// Makes the array empty.
        /// </summary>
        public readonly void Clear()
        {
            pointer->length = 0;
            MemoryAddress.Resize(ref pointer->items, sizeof(T) * pointer->length);
        }

        /// <summary>
        /// Resizes the array to be able to store <paramref name="newLength"/> amount of elements.
        /// <para>
        /// New elements will be uninitialized.
        /// </para>
        /// </summary>
        public readonly void Resize(int newLength)
        {
            MemoryAddress.Resize(ref pointer->items, sizeof(T) * newLength);
            pointer->length = newLength;
        }

        /// <summary>
        /// Resizes the array to be able to store <paramref name="newLength"/> amount of elements.
        /// <para>
        /// New elements will be initialized to <paramref name="defaultValue"/>.
        /// </para>
        /// </summary>
        public readonly void Resize(int newLength, T defaultValue)
        {
            MemoryAddress.Resize(ref pointer->items, sizeof(T) * newLength);
            if (newLength > pointer->length)
            {
                Span<T> span = new(pointer->items.Pointer + pointer->length * sizeof(T), newLength - pointer->length);
                span.Fill(defaultValue);
            }

            pointer->length = newLength;
        }

        /// <summary>
        /// Adds the given <paramref name="item"/> to the end.
        /// </summary>
        public readonly void Add(T item)
        {
            int newLength = pointer->length + 1;
            MemoryAddress.Resize(ref pointer->items, sizeof(T) * newLength);
            pointer->items.WriteElement(pointer->length, item);
            pointer->length = newLength;
        }

        /// <summary>
        /// Adds a <see langword="default"/> item to the end, and retrieves
        /// it by reference.
        /// </summary>
        public readonly ref T Add()
        {
            int newLength = pointer->length + 1;
            MemoryAddress.Resize(ref pointer->items, sizeof(T) * newLength);
            pointer->length = newLength;
            return ref pointer->items.ReadElement<T>(pointer->length - 1);
        }

        /// <summary>
        /// Adds a <see langword="default"/> item to the end.
        /// </summary>
        public readonly void AddDefault()
        {
            int newLength = pointer->length + 1;
            MemoryAddress.Resize(ref pointer->items, sizeof(T) * newLength);
            pointer->items.Clear(pointer->length * sizeof(T), sizeof(T));
            pointer->length = newLength;
        }

        /// <summary>
        /// Adds a range of <see langword="default"/> items to the end.
        /// </summary>
        public readonly void AddDefault(int count)
        {
            int newLength = pointer->length + count;
            MemoryAddress.Resize(ref pointer->items, sizeof(T) * newLength);
            pointer->items.Clear(pointer->length * sizeof(T), sizeof(T) * count);
            pointer->length = newLength;
        }

        /// <summary>
        /// Adds the given <paramref name="items"/> to the end.
        /// </summary>
        public readonly void AddRange(ReadOnlySpan<T> items)
        {
            int newLength = pointer->length + items.Length;
            MemoryAddress.Resize(ref pointer->items, sizeof(T) * newLength);
            pointer->items.Write(pointer->length * sizeof(T), items);
            pointer->length = newLength;
        }

        /// <summary>
        /// Removes the elements at the given <paramref name="index"/> by swapping
        /// it with the last elements.
        /// </summary>
        public readonly void RemoveAtBySwapback(int index)
        {
            ThrowIfOutOfRange(index);

            int newLength = pointer->length - 1;
            this[index] = this[newLength];
            pointer->length = newLength;
            MemoryAddress.Resize(ref pointer->items, sizeof(T) * pointer->length);
        }

        /// <summary>
        /// Removes the elements at the given <paramref name="index"/> by shifting
        /// other elements.
        /// </summary>
        public readonly void RemoveAt(int index)
        {
            ThrowIfOutOfRange(index);

            int newLength = pointer->length - 1;
            if (index == 0)
            {
                AsSpan(1).CopyTo(AsSpan());
            }
            else if (index < newLength)
            {
                AsSpan(index + 1).CopyTo(AsSpan(index));
            }

            pointer->length = newLength;
            MemoryAddress.Resize(ref pointer->items, sizeof(T) * pointer->length);
        }

        /// <inheritdoc/>
        public readonly Span<T>.Enumerator GetEnumerator()
        {
            return new Span<T>(pointer->items.Pointer, pointer->length).GetEnumerator();
        }

        /// <summary>
        /// Copies the state of the <paramref name="source"/>.
        /// </summary>
        public readonly void CopyFrom(Span<T> source)
        {
            if (source.Length != pointer->length)
            {
                pointer->length = source.Length;
                MemoryAddress.Resize(ref pointer->items, sizeof(T) * pointer->length);
            }

            pointer->items.Write(source);
        }

        /// <summary>
        /// Copies the state of the <paramref name="source"/>.
        /// </summary>
        public readonly void CopyFrom(ReadOnlySpan<T> source)
        {
            if (source.Length != pointer->length)
            {
                pointer->length = source.Length;
                MemoryAddress.Resize(ref pointer->items, sizeof(T) * pointer->length);
            }

            pointer->items.Write(source);
        }

        /// <summary>
        /// Copies this array into the <paramref name="destination"/>.
        /// </summary>
        public readonly void CopyTo(Span<T> destination)
        {
            ThrowIfOutOfRange(destination.Length - 1);

            pointer->items.CopyTo(destination);
        }

        /// <inheritdoc/>
        public static implicit operator Values(Values<T> values)
        {
            return new Values(values.pointer);
        }

        /// <inheritdoc/>
        public static implicit operator Span<T>(Values<T> values)
        {
            return new Span<T>(values.pointer->items.Pointer, values.pointer->length);
        }

        /// <inheritdoc/>
        public static implicit operator ReadOnlySpan<T>(Values<T> values)
        {
            return new Span<T>(values.pointer->items.Pointer, values.pointer->length);
        }
    }

    /// <summary>
    /// An array of values stored on an <see cref="Entity"/>.
    /// </summary>
    public unsafe readonly struct Values : IEquatable<Values>
    {
        internal readonly ArrayPointer* pointer;

        /// <summary>
        /// Length of the array.
        /// </summary>
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

        /// <summary>
        /// The size of each element in the array.
        /// </summary>
        public readonly int Stride => pointer->stride;

        /// <summary>
        /// Access the memory address to the element at <paramref name="index"/>.
        /// </summary>
        public readonly MemoryAddress this[int index]
        {
            get
            {
                ThrowIfOutOfRange(index);

                return new(pointer->items.Pointer + pointer->stride * index);
            }
        }

        internal Values(int length, int stride)
        {
            this.pointer = new Array(length, stride).Pointer;
        }

        internal Values(ArrayPointer* array)
        {
            this.pointer = array;
        }

        internal readonly void Dispose()
        {
            Array array = new(pointer);
            array.Dispose();
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfOutOfRange(int index)
        {
            if (index >= pointer->length)
            {
                throw new ArgumentOutOfRangeException($"Index {index} is out of range for {pointer->length} values");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfGreaterThanStride<T>() where T : unmanaged
        {
            if (sizeof(T) > pointer->stride)
            {
                throw new InvalidOperationException($"Size of {sizeof(T)} is greater than {pointer->stride}");
            }
        }

        /// <summary>
        /// Retrieves this entire array as a span of bytes.
        /// </summary>
        public readonly Span<byte> AsSpan()
        {
            return new(pointer->items.Pointer, pointer->length * pointer->stride);
        }

        /// <summary>
        /// Retrieves this array as a span of bytes with the custom <paramref name="byteLength"/>.
        /// </summary>
        public readonly Span<byte> GetSpan(int byteLength)
        {
            return new(pointer->items.Pointer, byteLength);
        }

        /// <summary>
        /// Retrieves this array as a span of bytes with <paramref name="byteLength"/>, starting at
        /// <paramref name="bytePosition"/>.
        /// </summary>
        public readonly Span<byte> Slice(int bytePosition, int byteLength)
        {
            return new(pointer->items.Pointer + bytePosition, byteLength);
        }

        /// <summary>
        /// Adds the given <paramref name="item"/> to the end.
        /// </summary>
        public readonly void Add<T>(T item) where T : unmanaged
        {
            ThrowIfGreaterThanStride<T>();

            int newLength = pointer->length + 1;
            MemoryAddress.Resize(ref pointer->items, pointer->stride * newLength);
            pointer->items.WriteElement(pointer->length, item);
            pointer->length = newLength;
        }

        /// <summary>
        /// Retrieves the reference to the element at <paramref name="index"/> as
        /// type <typeparamref name="T"/>.
        /// </summary>
        public readonly ref T Get<T>(int index) where T : unmanaged
        {
            ThrowIfOutOfRange(index);
            ThrowIfGreaterThanStride<T>();

            return ref pointer->items.Read<T>(pointer->stride * index);
        }

        /// <summary>
        /// Assigns <paramref name="value"/> to the element at <paramref name="index"/> as
        /// type <typeparamref name="T"/>.
        /// </summary>
        public readonly void Set<T>(int index, T value) where T : unmanaged
        {
            ThrowIfOutOfRange(index);
            ThrowIfGreaterThanStride<T>();

            pointer->items.Write(pointer->stride * index, value);
        }

        /// <summary>
        /// Retrieves the memory address to the <paramref name="bytePosition"/>.
        /// </summary>
        public readonly MemoryAddress Read(int bytePosition)
        {
            return pointer->items.Read(bytePosition);
        }

        /// <summary>
        /// Retrieves the memory address to the <paramref name="bytePosition"/>.
        /// </summary>
        public readonly MemoryAddress Read(uint bytePosition)
        {
            return pointer->items.Read(bytePosition);
        }

        /// <summary>
        /// Copies the state from the given <paramref name="bytes"/>.
        /// </summary>
        public readonly void CopyFrom(ReadOnlySpan<byte> bytes)
        {
            int length = bytes.Length / pointer->stride;
            if (pointer->length != length)
            {
                MemoryAddress.Resize(ref pointer->items, pointer->stride * length);
                pointer->length = length;
            }

            pointer->items.Write(bytes);
        }

        /// <summary>
        /// Copies the state of the given <paramref name="values"/>.
        /// </summary>
        public readonly void CopyFrom<T>(ReadOnlySpan<T> values) where T : unmanaged
        {
            ThrowIfGreaterThanStride<T>();

            if (pointer->length != values.Length)
            {
                MemoryAddress.Resize(ref pointer->items, pointer->stride * values.Length);
                pointer->length = values.Length;
            }

            pointer->items.Clear(pointer->stride * values.Length);
            for (int i = 0; i < values.Length; i++)
            {
                pointer->items.WriteElement(i * pointer->stride, values[i]);
            }
        }

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj)
        {
            return obj is Values values && Equals(values);
        }

        /// <inheritdoc/>
        public readonly bool Equals(Values other)
        {
            return pointer == other.pointer;
        }

        /// <inheritdoc/>
        public readonly override int GetHashCode()
        {
            return (int)pointer;
        }

        /// <inheritdoc/>
        public static bool operator ==(Values left, Values right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(Values left, Values right)
        {
            return !(left == right);
        }
    }
}