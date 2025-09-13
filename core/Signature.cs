using System;

namespace Worlds
{
    /// <summary>
    /// File signature of <see cref="World"/> binaries.
    /// </summary>
    public unsafe struct Signature : IEquatable<Signature>
    {
        /// <summary>
        /// Size of the signature in bytes.
        /// </summary>
        public const int ByteSize = 10;

        private fixed byte bytes[ByteSize];

        /// <summary>
        /// The version of the format stored.
        /// </summary>
        public readonly uint Version
        {
            get
            {
                fixed (byte* ptr = bytes)
                {
                    return *(uint*)(ptr + 6);
                }
            }
        }

#if NET
        /// <inheritdoc/>
        [Obsolete("Default constructor not supported", true)]
        public Signature()
        {
            throw new NotSupportedException();
        }
#endif

        /// <summary>
        /// Creates a new signature with the given <paramref name="version"/>.
        /// </summary>
        public Signature(uint version)
        {
            fixed (byte* ptr = bytes)
            {
                ptr[0] = (byte)'#';
                ptr[1] = (byte)'W';
                ptr[2] = (byte)'O';
                ptr[3] = (byte)'R';
                ptr[4] = (byte)'L';
                ptr[5] = (byte)'D';
                *(uint*)(ptr + 6) = version;
            }
        }

        /// <inheritdoc/>
        public readonly override string ToString()
        {
            return Version.ToString();
        }

        /// <inheritdoc/>
        public readonly override bool Equals(object? obj)
        {
            return obj is Signature signature && Equals(signature);
        }

        /// <inheritdoc/>
        public readonly bool Equals(Signature other)
        {
            return GetHashCode() == other.GetHashCode();
        }

        /// <inheritdoc/>
        public readonly override int GetHashCode()
        {
            int hash = 17;
            fixed (byte* ptr = bytes)
            {
                for (int i = 0; i < ByteSize; i++)
                {
                    hash = hash * 31 + ptr[i];
                }

                return hash;
            }
        }

        /// <inheritdoc/>
        public static bool operator ==(Signature left, Signature right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(Signature left, Signature right)
        {
            return !(left == right);
        }
    }
}