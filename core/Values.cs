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
        private readonly ArrayPointer* array;

        /// <summary>
        /// Length of the array.
        /// </summary>
        public readonly int Length => array->length;

        /// <summary>
        /// Access the reference to the element at <paramref name="index"/>.
        /// </summary>
        public readonly ref T this[int index]
        {
            get
            {
                ThrowIfOutOfRange(index);

                return ref array->items.ReadElement<T>(index);
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

                return ref array->items.ReadElement<T>(index);
            }
        }

        internal Values(int length)
        {
            this.array = new Array<T>(length).Pointer;
        }

        internal Values(ReadOnlySpan<T> values)
        {
            this.array = new Array<T>(values).Pointer;
        }

        internal Values(ArrayPointer* array)
        {
            this.array = array;
        }

        [Conditional("DEBUG")]
        internal readonly void ThrowIfSizeMismatch()
        {
            if (array->stride != sizeof(T))
            {
                throw new InvalidOperationException($"Size of {typeof(T).Name} doesn't equal to the stride of {array->stride}");
            }
        }

        [Conditional("DEBUG")]
        internal static void ThrowIfSizeMismatch<X>() where X : unmanaged
        {
            if (sizeof(T) != sizeof(X))
            {
                throw new InvalidOperationException($"Size mismatch between {typeof(T).Name} and {typeof(X).Name}");
            }
        }

        [Conditional("DEBUG")]
        internal static void ThrowIfSizeMismatch(int stride)
        {
            if (sizeof(T) != stride)
            {
                throw new InvalidOperationException($"Size of {typeof(T).Name} doesn't equal to the stride of {stride}");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfOutOfRange(int index)
        {
            if (index >= array->length || index < 0)
            {
                throw new InvalidOperationException($"Index {index} is out of range for values of length {array->length}");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfOutOfRange(uint index)
        {
            if (index >= array->length)
            {
                throw new InvalidOperationException($"Index {index} is out of range for values of length {array->length}");
            }
        }

        /// <summary>
        /// Casts this array to another array of type <typeparamref name="X"/>.
        /// </summary>
        public readonly Values<X> As<X>() where X : unmanaged
        {
            ThrowIfSizeMismatch<X>();

            return new(array);
        }

        /// <summary>
        /// Retrieves the span of all elements.
        /// </summary>
        public readonly Span<X> AsSpan<X>() where X : unmanaged
        {
            ThrowIfSizeMismatch<X>();

            return new(array->items.Pointer, array->length);
        }

        /// <summary>
        /// Retrieves the span of all elements.
        /// </summary>
        public readonly Span<T> AsSpan()
        {
            return new(array->items.Pointer, array->length);
        }

        /// <summary>
        /// Retrieves the span of all elements starting at <paramref name="start"/>.
        /// </summary>
        public readonly Span<T> AsSpan(int start)
        {
            return array->items.AsSpan<T>(start * sizeof(T), array->length - start);
        }

        /// <summary>
        /// Retrieves a span of <paramref name="length"/> of elements starting at <paramref name="start"/>.
        /// </summary>
        public readonly Span<T> AsSpan(int start, int length)
        {
            return array->items.AsSpan<T>(start * sizeof(T), length);
        }

        /// <summary>
        /// Retrieves the span of all elements starting at <paramref name="start"/>.
        /// </summary>
        public readonly Span<X> AsSpan<X>(int start) where X : unmanaged
        {
            ThrowIfSizeMismatch<X>();

            return array->items.AsSpan<X>(start * sizeof(X), array->length - start);
        }

        /// <summary>
        /// Retrieves a span of <paramref name="length"/> of elements starting at <paramref name="start"/>.
        /// </summary>
        public readonly Span<X> AsSpan<X>(int start, int length) where X : unmanaged
        {
            ThrowIfSizeMismatch<X>();

            return array->items.AsSpan<X>(start * sizeof(X), length);
        }

        /// <summary>
        /// Makes the array empty.
        /// </summary>
        public readonly void Clear()
        {
            array->length = 0;
            MemoryAddress.Resize(ref array->items, 0);
        }

        /// <summary>
        /// Resizes the array to be able to store <paramref name="newLength"/> amount of elements.
        /// <para>
        /// New elements will be uninitialized.
        /// </para>
        /// </summary>
        public readonly void Resize(int newLength)
        {
            MemoryAddress.Resize(ref array->items, sizeof(T) * newLength);
            array->length = newLength;
        }

        /// <summary>
        /// Resizes the array to be able to store <paramref name="newLength"/> amount of elements.
        /// <para>
        /// New elements will be initialized to <paramref name="defaultValue"/>.
        /// </para>
        /// </summary>
        public readonly void Resize(int newLength, T defaultValue)
        {
            MemoryAddress.Resize(ref array->items, sizeof(T) * newLength);
            if (newLength > array->length)
            {
                Span<T> span = new(array->items.Pointer + array->length * sizeof(T), newLength - array->length);
                span.Fill(defaultValue);
            }

            array->length = newLength;
        }

        /// <summary>
        /// Adds the given <paramref name="item"/> to the end.
        /// </summary>
        public readonly void Add(T item)
        {
            int newLength = array->length + 1;
            MemoryAddress.Resize(ref array->items, sizeof(T) * newLength);
            array->items.WriteElement(array->length, item);
            array->length = newLength;
        }

        /// <summary>
        /// Adds a <see langword="default"/> item to the end, and retrieves
        /// it by reference.
        /// </summary>
        public readonly ref T Add()
        {
            int newLength = array->length + 1;
            MemoryAddress.Resize(ref array->items, sizeof(T) * newLength);
            array->length = newLength;
            return ref array->items.ReadElement<T>(array->length - 1);
        }

        /// <summary>
        /// Adds a <see langword="default"/> item to the end.
        /// </summary>
        public readonly void AddDefault()
        {
            int newLength = array->length + 1;
            MemoryAddress.Resize(ref array->items, sizeof(T) * newLength);
            array->items.Clear(array->length * sizeof(T), sizeof(T));
            array->length = newLength;
        }

        /// <summary>
        /// Adds a range of <see langword="default"/> items to the end.
        /// </summary>
        public readonly void AddDefault(int count)
        {
            int newLength = array->length + count;
            MemoryAddress.Resize(ref array->items, sizeof(T) * newLength);
            array->items.Clear(array->length * sizeof(T), sizeof(T) * count);
            array->length = newLength;
        }

        /// <summary>
        /// Adds the given <paramref name="items"/> to the end.
        /// </summary>
        public readonly void AddRange(ReadOnlySpan<T> items)
        {
            int newLength = array->length + items.Length;
            MemoryAddress.Resize(ref array->items, sizeof(T) * newLength);
            array->items.Write(array->length * sizeof(T), items);
            array->length = newLength;
        }

        /// <summary>
        /// Removes the elements at the given <paramref name="index"/> by swapping
        /// it with the last elements.
        /// </summary>
        public readonly void RemoveAtBySwapback(int index)
        {
            ThrowIfOutOfRange(index);

            int newLength = array->length - 1;
            this[index] = this[newLength];
            array->length = newLength;
            MemoryAddress.Resize(ref array->items, sizeof(T) * array->length);
        }

        /// <summary>
        /// Removes the elements at the given <paramref name="index"/> by shifting
        /// other elements.
        /// </summary>
        public readonly void RemoveAt(int index)
        {
            ThrowIfOutOfRange(index);

            int newLength = array->length - 1;
            if (index == 0)
            {
                AsSpan(1).CopyTo(AsSpan());
            }
            else if (index < newLength)
            {
                AsSpan(index + 1).CopyTo(AsSpan(index));
            }

            array->length = newLength;
            MemoryAddress.Resize(ref array->items, sizeof(T) * array->length);
        }

        /// <inheritdoc/>
        public readonly Span<T>.Enumerator GetEnumerator()
        {
            return new Span<T>(array->items.Pointer, array->length).GetEnumerator();
        }

        /// <summary>
        /// Copies the state of the <paramref name="source"/>.
        /// </summary>
        public readonly void CopyFrom(Span<T> source)
        {
            if (source.Length != array->length)
            {
                array->length = source.Length;
                MemoryAddress.Resize(ref array->items, sizeof(T) * array->length);
            }

            array->items.Write(source);
        }

        /// <summary>
        /// Copies the state of the <paramref name="source"/>.
        /// </summary>
        public readonly void CopyFrom(ReadOnlySpan<T> source)
        {
            if (source.Length != array->length)
            {
                array->length = source.Length;
                MemoryAddress.Resize(ref array->items, sizeof(T) * array->length);
            }

            array->items.Write(source);
        }

        /// <summary>
        /// Copies this array into the <paramref name="destination"/>.
        /// </summary>
        public readonly void CopyTo(Span<T> destination)
        {
            ThrowIfOutOfRange(destination.Length - 1);

            array->items.CopyTo(destination);
        }

        /// <inheritdoc/>
        public static implicit operator Values(Values<T> values)
        {
            return new Values(values.array);
        }

        /// <inheritdoc/>
        public static implicit operator Span<T>(Values<T> values)
        {
            return new Span<T>(values.array->items.Pointer, values.array->length);
        }

        /// <inheritdoc/>
        public static implicit operator ReadOnlySpan<T>(Values<T> values)
        {
            return new Span<T>(values.array->items.Pointer, values.array->length);
        }
    }

    /// <summary>
    /// An array of values stored on an <see cref="Entity"/>.
    /// </summary>
    public unsafe readonly struct Values : IEquatable<Values>
    {
        internal readonly ArrayPointer* array;

        /// <summary>
        /// Length of the array.
        /// </summary>
        public readonly int Length
        {
            get => array->length;
            set
            {
                if (array->length != value)
                {
                    int oldLength = array->length;
                    MemoryAddress.Resize(ref array->items, array->stride * value);
                    array->length = value;
                }
            }
        }

        /// <summary>
        /// The size of each element in the array.
        /// </summary>
        public readonly int Stride => array->stride;

        /// <summary>
        /// Access the memory address to the element at <paramref name="index"/>.
        /// </summary>
        public readonly MemoryAddress this[int index]
        {
            get
            {
                ThrowIfOutOfRange(index);

                return new(array->items.Pointer + array->stride * index);
            }
        }

        internal Values(int length, int stride)
        {
            this.array = new Array(length, stride).Pointer;
        }

        internal Values(ArrayPointer* array)
        {
            this.array = array;
        }

        internal Values(nint address)
        {
            this.array = (ArrayPointer*)address;
        }

        internal readonly void Dispose()
        {
            new Array(array).Dispose();
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfOutOfRange(int index)
        {
            if (index >= array->length)
            {
                throw new ArgumentOutOfRangeException($"Index {index} is out of range for {array->length} values");
            }
        }

        [Conditional("DEBUG")]
        private readonly void ThrowIfGreaterThanStride<T>() where T : unmanaged
        {
            if (sizeof(T) > array->stride)
            {
                throw new InvalidOperationException($"Size of {sizeof(T)} is greater than {array->stride}");
            }
        }

        [Conditional("DEBUG")]
        internal readonly void ThrowIfSizeMismatch<T>() where T : unmanaged
        {
            if (sizeof(T) != array->stride)
            {
                throw new InvalidOperationException($"Size of {sizeof(T)} does not match {array->stride}");
            }
        }

        /// <summary>
        /// Retrieves this entire array as a span of bytes.
        /// </summary>
        public readonly Span<byte> AsSpan()
        {
            return new(array->items.Pointer, array->length * array->stride);
        }

        /// <summary>
        /// Retrieves this entire array as a span of <typeparamref name="T"/>.
        /// </summary>
        public readonly Span<T> AsSpan<T>() where T : unmanaged
        {
            ThrowIfSizeMismatch<T>();

            return new(array->items.Pointer, array->length);
        }

        /// <summary>
        /// Retrieves this array as a span of bytes with the custom <paramref name="byteLength"/>.
        /// </summary>
        public readonly Span<byte> GetSpan(int byteLength)
        {
            return new(array->items.Pointer, byteLength);
        }

        /// <summary>
        /// Retrieves this array as a span of bytes with <paramref name="byteLength"/>, starting at
        /// <paramref name="bytePosition"/>.
        /// </summary>
        public readonly Span<byte> Slice(int bytePosition, int byteLength)
        {
            return new(array->items.Pointer + bytePosition, byteLength);
        }

        /// <summary>
        /// Adds the given <paramref name="item"/> to the end.
        /// </summary>
        public readonly void Add<T>(T item) where T : unmanaged
        {
            ThrowIfGreaterThanStride<T>();

            int newLength = array->length + 1;
            MemoryAddress.Resize(ref array->items, array->stride * newLength);
            array->items.WriteElement(array->length, item);
            array->length = newLength;
        }

        /// <summary>
        /// Retrieves the reference to the element at <paramref name="index"/> as
        /// type <typeparamref name="T"/>.
        /// </summary>
        public readonly ref T Get<T>(int index) where T : unmanaged
        {
            ThrowIfOutOfRange(index);
            ThrowIfGreaterThanStride<T>();

            return ref array->items.Read<T>(array->stride * index);
        }

        /// <summary>
        /// Assigns <paramref name="value"/> to the element at <paramref name="index"/> as
        /// type <typeparamref name="T"/>.
        /// </summary>
        public readonly void Set<T>(int index, T value) where T : unmanaged
        {
            ThrowIfOutOfRange(index);
            ThrowIfGreaterThanStride<T>();

            array->items.Write(array->stride * index, value);
        }

        /// <summary>
        /// Retrieves the memory address to the <paramref name="bytePosition"/>.
        /// </summary>
        public readonly MemoryAddress Read(int bytePosition)
        {
            return array->items.Read(bytePosition);
        }

        /// <summary>
        /// Retrieves the memory address to the <paramref name="bytePosition"/>.
        /// </summary>
        public readonly MemoryAddress Read(uint bytePosition)
        {
            return array->items.Read(bytePosition);
        }

        /// <summary>
        /// Copies the state from the given <paramref name="bytes"/>.
        /// </summary>
        public readonly void CopyFrom(ReadOnlySpan<byte> bytes)
        {
            int length = bytes.Length / array->stride;
            if (array->length != length)
            {
                MemoryAddress.Resize(ref array->items, array->stride * length);
                array->length = length;
            }

            array->items.Write(bytes);
        }

        /// <summary>
        /// Copies the state of the given <paramref name="values"/>.
        /// </summary>
        public readonly void CopyFrom<T>(ReadOnlySpan<T> values) where T : unmanaged
        {
            ThrowIfGreaterThanStride<T>();

            if (array->length != values.Length)
            {
                MemoryAddress.Resize(ref array->items, array->stride * values.Length);
                array->length = values.Length;
            }

            array->items.Clear(array->stride * values.Length);
            for (int i = 0; i < values.Length; i++)
            {
                array->items.WriteElement(i * array->stride, values[i]);
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
            return array == other.array;
        }

        /// <inheritdoc/>
        public readonly override int GetHashCode()
        {
            return (int)array;
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